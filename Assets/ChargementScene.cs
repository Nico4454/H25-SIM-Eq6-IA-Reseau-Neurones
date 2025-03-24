using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChargementScene: MonoBehaviour 
{
    public void charger(String scene)
    {
        SceneManager.LoadScene(scene);
    }
}
