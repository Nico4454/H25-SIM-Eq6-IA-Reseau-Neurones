using UnityEngine;

public class DetectionSol : MonoBehaviour
{
    private bool piedCollisionAvecSol = false;
    private bool teteCollisionAvecSol = false;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (this.gameObject.name == "Tete") teteCollisionAvecSol = (other.gameObject.CompareTag("Sol"));
        if (this.gameObject.name == "Pied") piedCollisionAvecSol = (other.gameObject.CompareTag("Sol"));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Sol"))
        {
            if (this.gameObject.name == "Pied") piedCollisionAvecSol = false;
            if (this.gameObject.name == "Tete") teteCollisionAvecSol = false;
        }
    }

    public bool getPiedCollisionAvecSol() { return piedCollisionAvecSol; }
    public bool getTeteCollisionAvecSol() { return teteCollisionAvecSol; }





}
