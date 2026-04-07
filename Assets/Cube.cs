using UnityEngine;

public class Cube : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Input.GetAxis("Horizontal") * 2f*Time.deltaTime, 0f,
            Input.GetAxis("Vertical") * 2f*Time.deltaTime);
        
    }
}
