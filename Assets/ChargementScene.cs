using System;
using Unity.Sentis;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChargementScene: MonoBehaviour 
{
    public void charger(string scene)
    {
        if (scene != null)
        {
            SceneManager.LoadScene(scene);
        }
        
    }

   
    public void afficherElement(GameObject element)
    {
        element.SetActive(!element.activeInHierarchy);
    }

    public void quitterJeu()
    {
        Application.Quit();
    }

    
}
