using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ObservateurAgent : MonoBehaviour
{
    [SerializeField] private Image indicateur;

    [SerializeField] private TextMeshProUGUI texteEpisode;
    [SerializeField] private TextMeshProUGUI texteSteps;
    [SerializeField] private TextMeshProUGUI texteRecompense;
    [SerializeField] private TextMeshProUGUI texteRecompenseMax;
    [SerializeField] private Image G;
    [SerializeField] private Image D;
    [SerializeField] private Image Saut;


    private float episode = 0;
    private float stepsEpisode = 0;
    private float recompense = 0;
    private float recompenseMax = 0;

    [SerializeField] private AlpinisteAgent agent;
    
    void Update()
    {
        //pour l'affichage des episodes, steps et recompense
        episode = agent.CompletedEpisodes;
        stepsEpisode = agent.StepCount;
        recompense = agent.getRecompense();
        recompenseMax = agent.getRecompenseMax();
        afficherDonnees();

        //pour l'affichage des touches activées
        bool toucheG = agent.GetToucheG();
        bool toucheD = agent.GetToucheD();
        bool toucheSaut = agent.GetToucheSaut();

        activerTouche(toucheG, G);
        activerTouche(toucheD, D);
        activerTouche(toucheSaut, Saut);



    }

    private void afficherDonnees()
    {
        var strRecompense = recompense.ToString("F3");
        texteEpisode.SetText("Épisodes: " + episode);
        texteSteps.SetText("Steps: " + stepsEpisode);
        texteRecompense.SetText("Récompenses: " + strRecompense);
        texteRecompenseMax.SetText("Max: " + recompenseMax.ToString("F3"));

        if (recompense >= 1f) indicateur.color = Color.green;
        if (recompense <= -1f) indicateur.color = Color.red;
    }

    private void activerTouche(bool touche, Image image)
    {
        if (touche)
        {
            image.color = Color.white;
        } else
        {
            image.color = Color.grey;
        }
    }
}
