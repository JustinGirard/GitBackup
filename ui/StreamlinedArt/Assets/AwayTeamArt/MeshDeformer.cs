using UnityEngine;
using RayFire;



public class MeshDeformer : MonoBehaviour
{
    public GameObject hitSphere;
    public  float deformationForce=10f;
    public float timer = 0f;
    public float delay = 1f;
    private bool hasDemolished = false;
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= delay && !hasDemolished)
        {
            Debug.Log("Bang");
            //MeshEffect.ApplyMeshDeformation(hitSphere, this.gameObject,  deformationForce);
            MeshEffect.ApplyDeformationToChildren(hitSphere, this.gameObject,  deformationForce);
            hasDemolished = true;
        }
    }
}

