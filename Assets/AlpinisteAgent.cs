using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.Sentis;
using UnityEngine;



public class AlpinisteAgent : Agent
{
    private bool nouvEpisode = false;
    private bool decisionPossible = false;

    //pour le depart
    private float departY = 0f;
    private float departX;
    private float pos0 = -8f;
    private float largeurNiveau = 15f;
    private float hauteurNiveau = 25f;

    //pour les récompenses :
    private float distanceObjectifAgentMax = 10;
    private float hauteurMaxAtteinte = 0f;
    private Vector3 deltaObjectifAgent;
    private float recompense = 0;
    private float dHauteurMax = 5f;

    private float recompenseMax = 0;

    private float palier = 0.5f;
    private int dernierPalier = 0;
    private int palierActuel = 0;
    private int palierMax;



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
    private float forceSautMax;

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

    private bool toucheG = false;
    private bool toucheD = false;
    private bool toucheSaut = false;

    

    void Start()
    {
        rBody = this.GetComponent<Rigidbody2D>();
        rBody.sharedMaterial = matPasRebond;
        chargeSaut = chargeMin;
        dtRetourSol = dtRetourSolMin + 1;
        forceSautMax = chargeMax * multiplicateurSaut;
    }
    private void FixedUpdate()
    {
        bool decisionPossible = (IsPiedCollisionSol() && rBody.linearVelocityY <= 0);
        if (decisionPossible)//quand au sol on demande une decision
        {
            RequestDecision();

        }

    }


    private void Update()
    {
        //pour les récompenses
        deltaObjectifAgent = (objectif.position - transform.position);//obtient un vecteur de delta position
        float distanceObjectifAgent = deltaObjectifAgent.y;
        palierActuel = palierMax - Mathf.FloorToInt(distanceObjectifAgent / palier);

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
            /**
             * si on relâche la touche saut et qu'on était en chargement on redevient pas en chargement et on devient prêt à sauter 
             * quand on depaase la limite de chargement on saute
             */
            if (sautEnChargement && (saut != 1) || chargeSaut >= chargeMax)
            {
                sautEnChargement = false;

            }

            chargerSaut();

        }


        //pour voir l'angle et la direction du saut

        Debug.DrawRay(transform.position, calculerVecteurSaut(calculerForceSaut()), Color.red);






    }


    //GESTION DE L'AGENT CI-DESSOUS :

    public override void OnEpisodeBegin()
    {
        recompense = 0;
        palierActuel = 0;
        dernierPalier = 0;
        nouvEpisode = true;
        //génère une position aléatoire de l'agent au début de chaque épisode
        departX = randomDebutX();
        this.transform.position = new Vector2(departX, departY);
        rBody.linearVelocity = Vector3.zero;
        hauteurMaxAtteinte = departY;
        calculDistanceMax();
        palierMax = Mathf.FloorToInt(distanceObjectifAgentMax / palier) + 1;




    }



    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);//3 + 3
        sensor.AddObservation(objectif.position);
        //ajouterObservationVecteurNormalise(sensor, transform.position);//2
        //ajouterObservationVecteurNormalise(sensor, objectif.position);//2


        //TOUS 1 OBSERVATION par ligne = 5
        sensor.AddObservation(rBody.linearVelocityX / (forceSautMax * Mathf.Cos(angleSaut)));
        sensor.AddObservation(rBody.linearVelocityY / (forceSautMax * Mathf.Sin(angleSaut)));
        //sensor.AddObservation(rBody.linearVelocityX);
        //sensor.AddObservation(rBody.linearVelocityY);

        sensor.AddObservation(orientation);
        sensor.AddObservation(decisionPossible);
        sensor.AddObservation(enSaut);


    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = 0;
        moveX = deplacementAgent(actions, moveX);

        //logique pour chargement du saut
        var actionChargement = ScaleAction(actions.ContinuousActions[0], 0, 1);
        float chargeAgent = 0;


        if (isHeuristic)
        {
            chargeAgent = actionChargement;
        }
        else
        {
            chargeAgent = chargeMin + (chargeMax - chargeMin) * actionChargement;
            if (chargeAgent > chargeMin && moveX == 0)
            {
                toucheSaut = true;
                chargeSaut = chargeAgent;
                sautEnChargement = false;
            }
            else toucheSaut = false;

        }

        if (!enSaut && (chargeSaut > chargeMin) && !sautEnChargement)
        {
            forceSaut = calculerForceSaut();
            sauter();
        }

        /**
         * RÉCOMPENSES
         */
        float distanceObjectifAgent = deltaObjectifAgent.y;

        /**
        if ((hauteurMaxAtteinte - dHauteurMax) > rBody.position.y)//quand on descend trop bas de la hauteur max atteinte
        {
            SetReward(-1f);
            recompense = -1f;
            EndEpisode();
        }
        */
        float recompensePalier = (float)palier / distanceObjectifAgentMax;

        if (dernierPalier != palierActuel && distanceObjectifAgent != 0)
        {
            int palierParcouru = palierActuel - dernierPalier;//peut être négative 
            ajouterRecompense(recompensePalier * palierParcouru);
            dernierPalier = palierActuel;
        }
        if (MaxStep != 0) ajouterRecompense(-1f / MaxStep);



    }

    private float deplacementAgent (ActionBuffers actions, float moveX)
    {
        switch (actions.DiscreteActions[0])
        {//donne la direction du mouvement en x
            case 0:
                {
                    moveX = -1;
                    toucheG = true;
                    toucheD = false;
                    break;
                }
            case 1:
                {
                    moveX = 0;
                    toucheG = false;
                    toucheD = false;
                    break;
                }
            case 2:
                {
                    moveX = 1;
                    toucheG = false;
                    toucheD = true;
                    break;
                }
        }
        enMouvement = (moveX != 0);
        if (!enSaut && !sautEnChargement)//si l'agent tombe sans avoir sauté il peut bougé
        {
            rBody.linearVelocityX = moveX * vitesseX;

        }
        return moveX;
    }

    //pour changer le model
    public void configurerModel(ModelAsset model)
    {
        model.GetHashCode();
        SetModel("BehaviorAgentAlpiniste", model, InferenceDevice.Default);
        activerInference();


    }

    public void activerHeuristic()
    {
        var parametres = GetComponent<BehaviorParameters>();
        parametres.BehaviorType = BehaviorType.HeuristicOnly;
        isHeuristic = true;
        MaxStep = 0;
    }
    public void activerInference()
    {
        var parametres = GetComponent<BehaviorParameters>();
        parametres.BehaviorType = BehaviorType.InferenceOnly;
        isHeuristic = false;
        MaxStep = 2000;
    }




    //heuristic = on peut contrôler l'agent
    public override void Heuristic(in ActionBuffers actionsOut)//quand on peut contrôler l'agent par nous-même
    {
        isHeuristic = true;
        //permet de contrôler l'agent par le clavier
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 1 + (int)Input.GetAxisRaw("Horizontal");//-1,0,1 -> 0,1,2

        var continuousActionsOut = actionsOut.ContinuousActions;
        if (!sautEnChargement && chargeSaut > chargeMin)//permet de passer la charge qu'on a chargé
        {
            continuousActionsOut[0] = (chargeSaut - chargeMin) / (chargeMax - chargeMin) ;
        }
        else
        {
            continuousActionsOut[0] = 0;
        }


    }

    private void ajouterRecompense(float recompense)
    {
        AddReward(recompense);
        this.recompense += recompense;
        recompenseMax = Mathf.Max(recompenseMax, recompense);

        if (recompense <= -1f)
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
            toucheSaut = true;
        }
        else toucheSaut = false;


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
            recompense = 1f;
            recompenseMax = recompenseObjectif;
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
    public float getRecompense()
    {
        return recompense;
    }
    public float getRecompenseMax() { return recompenseMax; }

    public bool GetToucheG()
    {
        return toucheG;
    }

    public bool GetToucheD()
    {
        return toucheD;
    }

    public bool GetToucheSaut()
    {
        return toucheSaut;
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
