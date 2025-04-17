using Unity.Sentis;
using UnityEngine;

public class ChargementModel : MonoBehaviour
{
    [SerializeField] AlpinisteAgent alpinisteAgent;

    public void chargerCerveau(ModelAsset model)
    {

        alpinisteAgent.changerModel(model);

    }
}
