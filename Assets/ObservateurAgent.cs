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
    private float episode = 0;
    private float stepsEpisode = 0;
    private float recompense = 0;

    [SerializeField] private AlpinisteAgent agent;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        episode = agent.CompletedEpisodes;
        stepsEpisode = agent.StepCount;
        recompense = agent.getRecompense();
        var strRecompense = recompense.ToString("F3");


        texteEpisode.SetText("Épisodes: " + episode);
        texteSteps.SetText("Steps: " + stepsEpisode);
        texteRecompense.SetText("Récompenses: " + strRecompense);

        if (recompense >= 1f) indicateur.color = Color.green;
        if (recompense <= -1f) indicateur.color = Color.red;
    }
}
