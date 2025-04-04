using System;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Sensors.Reflection;
using UnityEngine;
using UnityEngine.UI;


public class AlpinisteAgent : Agent
{

    private int numEpisode = 1;
    private bool nouvEpisode = false;



    private float departY = 0f;
    private float departX;
    private float pos0 = -8f;
    private float largeurNiveau = 15f;

    private float xMax = 12;//-12 � 12
    private float yMax = 68;
    
    //pour les r�compenses :
    private float distanceObjectifAgentMax;
    private float hauteurMaxAtteinte = 0f;
    private Vector3 deltaObjectifAgent;



    //donn�es pour la physique de l'agent:
    [SerializeField] private float chargeMax = 2.8f;
    [SerializeField] private float chargeMin = 1f;
    [SerializeField] private float multiplicateurSaut = 7f;// multiplie la charge pour obtenir la force
    
    //dt :
    private float dtSaut = 0;
    private float dtSautMin = 0.05f;

    private float dtRetourSol = 0;
    private float dtRetourSolMin = 0.5f;


    [SerializeField] Transform objectif;
    private float vitesseX = 3f;

    //physique de saut :

    private float vitesseYMax = 8f;//doit correspondre avec la vitesse de la camera

    private float chargeSaut;
    private float forceSaut;


    private float angleSaut = 48.98f * Mathf.Deg2Rad; // deg converti en radians pour faciliter la visualisation
    private bool orientationVersGauche = false; // si agent orient� vers la gauche ou pas (la droite) pour d�finir le sens de l'angle
    private float orientation = 1;


    private bool enSaut;
    private bool sautEnChargement;
    private bool enMouvement;

    private bool isHeuristic = false;//important pour savoir si l'agent peut �tre contr�l�

    Rigidbody2D rBody;

    //mat�riaux
    [SerializeField] PhysicsMaterial2D matRebond;
    [SerializeField] PhysicsMaterial2D matPasRebond;

    [SerializeField] SpriteRenderer fond;

    void Start()
    {
        rBody = this.GetComponent<Rigidbody2D>();
        rBody.sharedMaterial = matPasRebond;
        chargeSaut = chargeMin;
        dtRetourSol = dtRetourSolMin + 1;
        

    }

    private void calculDistanceMax()
    {
        Vector2 vecteurDistance = (objectif.position - new Vector3(0, departY, transform.position.z));
        distanceObjectifAgentMax = vecteurDistance.y;
    }

    private void Update()
    {
        calculDistanceMax();
        
        hauteurMaxAtteinte = Mathf.Max(transform.position.y,hauteurMaxAtteinte);

        //changer le materiel si au sol ou pas
        if (IsPiedCollisionSol())
        {
            rBody.sharedMaterial = matPasRebond;

        }
        else
        {
            rBody.sharedMaterial = matRebond;
        }

        //en tombant, il ne faut pas que la vitesse soit trop �lev�e
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
        if (dtSaut < dtSautMin && enSaut && dtRetourSol >= dtRetourSolMin)//maintenir la vitesse en x quand trop pr�t du sol pour �viter un frottement 
        {
            rBody.linearVelocityX = calculerVecteurSaut(forceSaut).x;
        }

        //direction du saut (G/D) en -1 ou 1
        orientation = orientationVersGauche ? -1 : 1;

        if (enSaut) dtSaut += Time.deltaTime;

        if (dtSaut > dtSautMin && enSaut && IsPiedCollisionSol())//quand agent encore au sol ou agent vient de revenir au sol apr�s un saut
        {
            dtRetourSol += Time.deltaTime;

            if (rBody.linearVelocityX != 0) rBody.linearVelocityX = 0;//pour �viter glissement au sol en att�rissant au sol
            if (rBody.linearVelocityY != 0) rBody.linearVelocityY = 0;//pour �viter rebond accidentel

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
            if (sautEnChargement && (saut != 1))//si on rel�che la touche saut et qu'on �tait en chargement on redevient pas en chargement et on devient pr�t � sauter
            {
                sautEnChargement = false;
            }
            if (chargeSaut >= chargeMax)//quand on depaase la limite de chargement on saute
            {
                sautEnChargement = false;

            }
            chargerSaut();            

        }

        Debug.Log("Episode: " + CompletedEpisodes);


        //pour voir l'angle et la direction du saut

        Debug.DrawRay(transform.position, calculerVecteurSaut(calculerForceSaut()), Color.red);

    }


    //GESTION DE L'AGENT CI-DESSOUS :

    public override void OnEpisodeBegin()
    {
        //fond.color = Color.clear;
        nouvEpisode = true;
        numEpisode++;
        //g�n�re une position al�atoire de l'agent au d�but de chaque �pisode
        departX = randomDebutX();
        this.transform.position = new Vector2(departX, departY);
        rBody.linearVelocity = Vector3.zero;

        
        


    }
    
    
    public override void Heuristic(in ActionBuffers actionsOut)//quand on peut contr�ler l'agent par nous-m�me
    {
        isHeuristic = true;
        //permet de contr�ler l'agent par le clavier
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 1 + (int)Input.GetAxisRaw("Horizontal");//-1,0,1 -> 0,1,2

        var continuousActionsOut = actionsOut.ContinuousActions;
        if (!sautEnChargement && chargeSaut > chargeMin)//permet de passer la charge qu'on a charg�
        {
            continuousActionsOut[0] = chargeSaut;
        }
        else
        {
            continuousActionsOut[0] = 0;
        }


    }

    //fait 9 observations
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);// 3 observations
        sensor.AddObservation(objectif.position);// 3 observations

        sensor.AddObservation(rBody.linearVelocityX);//2
        sensor.AddObservation(rBody.linearVelocityY);

        sensor.AddObservation(forceSaut);//1
        sensor.AddObservation(deltaObjectifAgent);
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


        if (!enSaut && !sautEnChargement)//si l'agent tombe sans avoir saut� il peut boug�
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

        //on veut calculer la r�compense � donner en fonction de la distance de l'agent avec l'objectif
        deltaObjectifAgent = (objectif.position - transform.position);
        float distanceObjectifAgent = deltaObjectifAgent.y;
        float recompenseDistance = 0;
        if (transform.position.y > 0.5f)
        {
            recompenseDistance = (distanceObjectifAgentMax - distanceObjectifAgent) / distanceObjectifAgentMax + 0.1f;
        }
        if (hauteurMaxAtteinte > transform.position.y)
        {
            AddReward((transform.position.y - hauteurMaxAtteinte) / (hauteurMaxAtteinte - 0.01f) );
        }
        if (recompenseDistance > 0 && recompenseDistance < 1)
        {
            SetReward(recompenseDistance);//plus l'agent se rapproche de l'objectif, plus il sera r�compens� mais toujours entre 0 et 1
        }




    }

    private void sauter()
    {

        rBody.linearVelocity = calculerVecteurSaut(forceSaut);//on applique la force charg�e
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
         * y'a un minimum de force de saut cr�� avec chargeMin
         * on clamp la force pour pas pouvoir trop sauter
         * la force de saut est le module de la vitesse du saut pour l'instant
         */
        return Mathf.Clamp(multiplicateurSaut * chargeSaut, chargeMin * multiplicateurSaut, chargeMax * multiplicateurSaut);


    }

    private Vector2 calculerVecteurSaut(float forceSaut)
    {
        /**
         * on applique la force de saut comme �tant le module? oui
         * l'angle change? non, il est fixe
         * 
         */
        //on modifie le vecteur saut
        return calculerVecteurComposantesAvecAngle(angleSaut, orientation) * forceSaut;
    }

    private static Vector3 calculerVecteurComposantesAvecAngle(float angle, float orientation)
    {
        return new Vector3(orientation * Mathf.Cos(angle), Mathf.Sin(angle)) ;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.name == "Objectif")
        {
            SetReward(1f);
            //fond.color = Color.green;
            EndEpisode();
        }
        if (other.gameObject.name == "Bordure")
        {
            SetReward(-1f);
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
