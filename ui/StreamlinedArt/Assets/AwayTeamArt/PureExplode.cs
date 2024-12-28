using UnityEngine;
using RayFire;



public class PureExplode : MonoBehaviour
{
    public float explosionForce = 0.00f;
    public float explosionRadius = 1.00f;
    public float upwardsModifier = -1f;
    public float delay = 1.0f; // Delay before first explosion
    public float fragmentFraction = 0.5f; // Fraction of fragments to reapply script (0 to 1)
    public int maxDepth = 3; // Max recursion depth
    private float timer = 0.0f;
    private bool hasDemolished = false;
    public bool doRecurse = true;
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= delay && !hasDemolished)
        {
            MeshEffect.DoExplode(this.gameObject,explosionForce,fragmentFraction,doRecurse);
            hasDemolished = true;
        }
    }
}

