using System.Collections;
using UnityEngine;

public class EffectPulseSphere : MonoBehaviour
{
    public AnimationCurve pulseCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    public float pulseDuration = 2f;

    private float pulseTimer = 0f;
    public Vector3 coreScale;
    
    void Start()
    {
        coreScale = transform.localScale;
    }
    
    void Update()
    {
        PulseSphere();
    }

    /// <summary>
    /// Pulses the sphere's scale using an AnimationCurve.
    /// </summary>
    private void PulseSphere()
    {
        // Increment timer and loop if it exceeds the duration
        pulseTimer += Time.deltaTime;
        if (pulseTimer > pulseDuration)
        {
            pulseTimer -= pulseDuration;
        }

        // Evaluate the curve at the normalized time
        float scaleFactor = pulseCurve.Evaluate(pulseTimer / pulseDuration);

        // Apply the same scale across all axes
        transform.localScale = coreScale * scaleFactor;
    }
}
