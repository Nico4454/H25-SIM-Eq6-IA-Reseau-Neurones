using System;
using Unity.Sentis;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChargementScene: MonoBehaviour 
{
    public void charger(String scene)
    {
        SceneManager.LoadScene(scene);
    }

   
    public void afficherElement(GameObject element)
    {
        element.SetActive(!element.activeInHierarchy);
    }

    
}
