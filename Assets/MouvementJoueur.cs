using Unity.VisualScripting;
using UnityEngine;

public class MouvementJoueur : MonoBehaviour
{
    
    [SerializeField] Rigidbody2D corps = null;
    [SerializeField] float positionXinitial = -6, positionYinitial = -2;
    [SerializeField] float vitesse = 10;
    [SerializeField] float forceSaut = 1;
    public float mvtX;
 private bool auSol;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.position = new Vector2(positionXinitial,positionYinitial);
        auSol = false;
    }

    // Update is called once per frame
    void Update()
    {
         mvtX = Input.GetAxis("Horizontal") * vitesse * Time.deltaTime;

        if (Input.GetKey(KeyCode.Space))
        {
            corps.AddForce(new Vector2(0, forceSaut));
        }

        corps.AddForce(new Vector2(mvtX, 0));
        
    }
    void OnCollisionStay()
    {
        auSol = true; 
    }
}
