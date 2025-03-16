using UnityEngine;

public class SuiviCamera : MonoBehaviour
{
    [SerializeField] Transform transformAgent;
    [SerializeField] float dyExtra = 3f;
    [SerializeField] float vitesseCamera = 6f;
    private float posYcam = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialiserPosYCamera();
        this.transform.position = new Vector3(0, posYcam, transform.position.z);

    }

    // Update is called once per frame
    void Update()
    {
        initialiserPosYCamera();
        transform.position = Vector3.MoveTowards(transform.position, new Vector3(0, posYcam, transform.position.z), vitesseCamera*Time.deltaTime);
        if (transformAgent.gameObject.GetComponent<AlpinisteAgent>().isNewEpisode())
        {
            transform.position = new Vector3(0, posYcam, transform.position.z);
        }


    }

    private void initialiserPosYCamera()
    {
        posYcam = transformAgent.position.y + dyExtra;
    }
}
