using System;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Sensors.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.UI;
using UnityEngine.UIElements;


public class AlpinisteAgent : Agent
{
    private bool nouvEpisode = false;

    //pour le depart
    private float departY = 0f;
    private float departX;
    private float pos0 = -8f;
    private float largeurNiveau = 15f;

    private float xMax = 12;//-12 à 12
    private float yMax = 68;

    //pour les récompenses :
    private float distanceObjectifAgentMax = 10;
    private float hauteurMaxAtteinte = 0f;
    private float limiteDistanceHauteur = 15;
    private Vector3 deltaObjectifAgent;
    private float rec = 0;

    //dt :
    private float dtSaut = 0;
    private float dtSautMin = 0.05f;

    private float dtRetourSol = 0;
    private float dtRetourSolMin = 0.5f;

    //position objectif
    [SerializeField] Transform objectif;

    //données pour la physique de l'agent:
    [SerializeField] private float chargeMax = 3f;
    [SerializeField] private float chargeMin = 1f;
    [SerializeField] private float multiplicateurSaut = 7f;// multiplie la charge pour obtenir la force
    
    private float vitesseX = 3f;
    private float vitesseYMax = 8f;//doit correspondre avec la vitesse de la camera

    private float chargeSaut;
    private float forceSaut;

    private float angleSaut = 48.98f * Mathf.Deg2Rad; // deg converti en radians pour faciliter la visualisation
    private bool orientationVersGauche = false; // si agent orienté vers la gauche ou pas (la droite) pour définir le sens de l'angle
    private float orientation = 1;

    private bool enSaut;
    private bool sautEnChargement;
    private bool enMouvement;

    //heuristic
    private bool isHeuristic = false;//important pour savoir si l'agent peut être contrôlé

    //le rigidbody de l'agent
    Rigidbody2D rBody;

    //matériaux
    [SerializeField] PhysicsMaterial2D matRebond;
    [SerializeField] PhysicsMaterial2D matPasRebond;


    void Start()
    {
        rBody = this.GetComponent<Rigidbody2D>();
        rBody.sharedMaterial = matPasRebond;
        chargeSaut = chargeMin;
        dtRetourSol = dtRetourSolMin + 1;


    }


    private void Update()
    {

        hauteurMaxAtteinte = Mathf.Max(transform.position.y, hauteurMaxAtteinte);

        //changer le materiel si au sol ou pas
        if (IsPiedCollisionSol())
        {
            rBody.sharedMaterial = matPasRebond;

        }
        else
        {
            rBody.sharedMaterial = matRebond;
        }

        //en tombant, il ne faut pas que la vitesse soit trop élevée
        if (rBody.linearVelocityY < 0.1f)
        {
            rBody.linearVelocityY = Mathf.Clamp(rBody.linearVelocityY, -vitesseYMax, 0);
        }

        //indiquer l'orientation gauche/droite
        if (rBody.linearVelocityX < -0.1f)// vers la gauche
        {
            orientationVersGauche = true;
            this.gameObject.GetComponent<SpriteRenderer>().flipX = true;

        }
        if (rBody.linearVelocityX > 0.1f)// vers la droite
        {
            orientationVersGauche = false;
            this.gameObject.GetComponent<SpriteRenderer>().flipX = false;

        }
        

        if (dtSaut < dtSautMin && enSaut && dtRetourSol >= dtRetourSolMin)//maintenir la vitesse en x quand trop prêt du sol pour éviter un frottement 
        {
            rBody.linearVelocityX = calculerVecteurSaut(forceSaut).x;
        }

        //direction du saut (G/D) en -1 ou 1
        orientation = orientationVersGauche ? -1 : 1;

        if (enSaut) dtSaut += Time.deltaTime;

        if (dtSaut > dtSautMin && enSaut && IsPiedCollisionSol())//quand agent encore au sol ou agent vient de revenir au sol après un saut
        {
            dtRetourSol += Time.deltaTime;

            if (rBody.linearVelocityX != 0) rBody.linearVelocityX = 0;//pour éviter glissement au sol en attérissant au sol
            if (rBody.linearVelocityY != 0) rBody.linearVelocityY = 0;//pour éviter rebond accidentel

            if (dtRetourSol >= dtRetourSolMin)
            {
                enSaut = false;//il pourra sauter (ou re-sauter)
                chargeSaut = chargeMin;
            }
            dtSaut = 0;
        }




        if (isHeuristic)// effectue le chargement du saut seulement en heuristic
        {
            float saut = 0;
            if (IsPiedCollisionSol() && isPretASauter())
            {
                saut = Input.GetAxisRaw("Jump");

            }
            if (saut == 1 && isPretASauter())//aucun mouvement et pas en saut pour charger le saut (en activant le bool, car le chargement en tant que telle se fait dans le update)
            {
                sautEnChargement = true;

            }
            if (sautEnChargement && (saut != 1))//si on relâche la touche saut et qu'on était en chargement on redevient pas en chargement et on devient prêt à sauter
            {
                sautEnChargement = false;
            }
            if (chargeSaut >= chargeMax)//quand on depaase la limite de chargement on saute
            {
                sautEnChargement = false;

            }
            chargerSaut();

        }

        Debug.Log("Episode: " + CompletedEpisodes + "rec: " + rec);


        //pour voir l'angle et la direction du saut

        Debug.DrawRay(transform.position, calculerVecteurSaut(calculerForceSaut()), Color.red);






    }


    //GESTION DE L'AGENT CI-DESSOUS :

    public override void OnEpisodeBegin()
    {
        nouvEpisode = true;
        //génère une position aléatoire de l'agent au début de chaque épisode
        departX = randomDebutX();
        this.transform.position = new Vector2(departX, departY);
        rBody.linearVelocity = Vector3.zero;
        hauteurMaxAtteinte = departY;
        calculDistanceMax();




    }


    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);// 3 observations
        sensor.AddObservation(objectif.position);// 3 observations

        sensor.AddObservation(rBody.linearVelocityX/vitesseX);//2
        sensor.AddObservation(rBody.linearVelocityY/vitesseYMax);

        sensor.AddObservation(forceSaut);//3
        sensor.AddObservation(enSaut);
        sensor.AddObservation(orientation);

    }


    public override void OnActionReceived(ActionBuffers actions)
    {


        float moveX = 0;
        switch (actions.DiscreteActions[0])
        {//donne la direction du mouvement en x
            case 0: moveX = -1; break;
            case 1: moveX = 0; break;
            case 2: moveX = 1; break;
        }
        enMouvement = (moveX != 0);


        if (!enSaut && !sautEnChargement)//si l'agent tombe sans avoir sauté il peut bougé
        {
            rBody.linearVelocityX = moveX * vitesseX;


        }

        float chargeAgent = 0;
        if (isHeuristic)
        {
            chargeAgent = actions.ContinuousActions[0];
        }
        else
        {
            chargeAgent = chargeMin + (chargeMax - chargeMin) * Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
            if (chargeAgent > chargeMin && moveX == 0)
            {
                chargeSaut = chargeAgent;
                sautEnChargement = false;
            }

        }

        if (!enSaut && (chargeSaut > chargeMin) && !sautEnChargement)
        {
            forceSaut = calculerForceSaut();
            sauter();
        }

        /**
         * RÉCOMPENSES
         */
        //on veut calculer la récompense à donner en fonction de la distance de l'agent avec l'objectif
        deltaObjectifAgent = (objectif.position - transform.position);//obtient un vecteur de delta position
        float distanceObjectifAgent = (deltaObjectifAgent.y);
        float recompenseDistance = 0;
        float distanceInverse = distanceObjectifAgentMax - Mathf.Abs(distanceObjectifAgent);
        if (distanceInverse > 0 && distanceInverse < distanceObjectifAgentMax )//pas de 0 ni de recompense >= 1 car réservé à l'atteinte de l'objectif
        {
            recompenseDistance = distanceInverse / distanceObjectifAgentMax;
        } 
        if ( recompenseDistance < 1 ) setRecompense(recompenseDistance);//plus on s'approche de l'objectif plus la récompense est élevé
        
        


        //if (hauteurMaxAtteinte - limiteDistanceHauteur > transform.position.y)//quand l'agent est trop bas vis-à-vis de son hauteur maximum, la recompense est négative pour décourager à l'agent de tomber en bas
        //{
        //    setRecompense(-1f); 
        //}

        /**
         * On veut récompenser à chaque fois que l'agent dépasse un certain palier et reste au dessus
         * exemple, chaque 2 métres on ajoute 0.1 de recompenses
         * 1 palier = x m de parcouru (x = distance palier)
         */
        //float recompensePalier = distancePalier / distanceObjectifAgentMax;
        //if (((transform.position.y - departY) % distancePalier) == 0 && transform.position.y >= hauteurMaxAtteinte )        //atteint une nouvelle hauteur maximum et la hauteur est divisible par le palier donc a atteint le palier
        //{
        //    AddReward(recompensePalier);
        //    rec += recompensePalier;
        //}


    }
    public override void Heuristic(in ActionBuffers actionsOut)//quand on peut contrôler l'agent par nous-même
    {
        isHeuristic = true;
        //permet de contrôler l'agent par le clavier
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 1 + (int)Input.GetAxisRaw("Horizontal");//-1,0,1 -> 0,1,2

        var continuousActionsOut = actionsOut.ContinuousActions;
        if (!sautEnChargement && chargeSaut > chargeMin)//permet de passer la charge qu'on a chargé
        {
            continuousActionsOut[0] = chargeSaut;
        }
        else
        {
            continuousActionsOut[0] = 0;
        }


    }

    private void setRecompense(float recompense)
    {
        SetReward(recompense);
        rec = recompense;

        if (recompense <= -1)
        {
            EndEpisode();
        }
    }

    private void calculDistanceMax()
    {
        Vector2 vecteurDistance = (objectif.position - new Vector3(departX, departY, transform.position.z));
        distanceObjectifAgentMax = Mathf.Abs(vecteurDistance.y);
    }
    private void sauter()
    {

        rBody.linearVelocity = calculerVecteurSaut(forceSaut);//on applique la force chargée
        chargeSaut = chargeMin;
        enSaut = true;
        sautEnChargement = false;
    }
    private void chargerSaut()
    {
        if (sautEnChargement)
        {
            chargeSaut += Time.deltaTime;//on charge en fonction du temps de maintien
            chargeSaut = Mathf.Clamp(chargeSaut, chargeMin, chargeMax);
        }


    }
    private float calculerForceSaut()
    {
        /**
         * y'a un minimum de force de saut créé avec chargeMin
         * on clamp la force pour pas pouvoir trop sauter
         * la force de saut est le module de la vitesse du saut pour l'instant
         */
        return Mathf.Clamp(multiplicateurSaut * chargeSaut, chargeMin * multiplicateurSaut, chargeMax * multiplicateurSaut);


    }

    private Vector2 calculerVecteurSaut(float forceSaut)
    {
        /**
         * on applique la force de saut comme étant le module? oui
         * l'angle change? non, il est fixe
         * 
         */
        //on modifie le vecteur saut
        return calculerVecteurComposantesAvecAngle(angleSaut, orientation) * forceSaut;
    }

    private static Vector3 calculerVecteurComposantesAvecAngle(float angle, float orientation)
    {
        return new Vector3(orientation * Mathf.Cos(angle), Mathf.Sin(angle));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Goal")
        {
            float recompenseObjectif=1f;
            if (other.GetComponent<Objectif>() != null)
            {
                recompenseObjectif = other.GetComponent<Objectif>().recompense;
            }
            SetReward(recompenseObjectif);
            EndEpisode();
        }
        if (other.gameObject.name == "Bordure")
        {
            setRecompense(-1f);
            EndEpisode();
        }

    }

    private bool isPretASauter()
    {
        return !enSaut && !enMouvement && dtRetourSol >= dtRetourSolMin;
    }




    private bool IsTeteCollisionSol()
    {
        DetectionSol[] detectionSols = this.GetComponentsInChildren<DetectionSol>();
        foreach (DetectionSol s in detectionSols)
        {

            if (s.gameObject.name == "Tete")
            {
                return s.getTeteCollisionAvecSol();
            }



        }
        return false;
    }
    private bool IsPiedCollisionSol()
    {



        DetectionSol[] detectionSols = this.GetComponentsInChildren<DetectionSol>();
        foreach (DetectionSol s in detectionSols)
        {
            if (s.gameObject.name == "Pied")
            {
                return s.getPiedCollisionAvecSol();
            }
            else
            {
                return false;
            }


        }
        return false;



    }
    private float randomDebutX()
    {
        return UnityEngine.Random.Range(pos0, pos0 + largeurNiveau);
    }
    public bool isNewEpisode()
    {
        if (nouvEpisode)
        {
            nouvEpisode = false;
            return true;
        }
        else return false;
    }
}
