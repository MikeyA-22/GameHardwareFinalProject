using UnityEngine;
using System.Collections;

public class SmellDiffuserController : MonoBehaviour
{
    public ParticleSystem particleEffect;

    private MicroBit microbit;

    private bool isActive = false;

    private bool prepped = false;

    void Start()
    {
        microbit = GetComponent<MicroBit>();

       
    }

    void Update()
    {
        if (microbit == null) return;

        HandleParticles();
    }

    void HandleParticles()
    {
        bool shouldBeActive = microbit.A && microbit.B;
        
        if (shouldBeActive && !isActive)
        {
            Debug.Log("Smell Diffuser prepped");
            isActive = true;
        }
        else if (!shouldBeActive && isActive)
        {
            Debug.Log("Smell Diffuser active");

            isActive = false;

            StartCoroutine(SmellTimer());
        }
    }

    IEnumerator SmellTimer()
    {
        particleEffect.Play();
        yield return new WaitForSeconds(3);

        particleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}