
using UnityEngine;
using RayFire;

public class PlaneSlice : MonoBehaviour
{
    public GameObject slicingPlane;  // Plane to slice the object
    public float delay = 1.0f;
    private float timer = 0.0f;
    private bool hasDemolished = false;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= delay && !hasDemolished)
        {
            if (slicingPlane != null)
            {
                SliceWithPlane(slicingPlane.transform);
                hasDemolished = true;
            }
        }
    }

    // Slice the object using the plane's transform
    void SliceWithPlane(Transform planeTransform)
    {
        RayfireRigid rfRigid = GetComponent<RayfireRigid>();

        if (rfRigid == null)
        {
            rfRigid = gameObject.AddComponent<RayfireRigid>();
            rfRigid.demolitionType = DemolitionType.Runtime;


        }

        // Ensure RayFire is initialized
        rfRigid.Initialize();

      Vector3 planeOrigin = planeTransform.position;
        Vector3 planeNormal = planeTransform.up;

        // Add slicing plane to Rayfire Rigid
        Vector3[] slicePlane = new Vector3[2] { planeOrigin, planeNormal };
        rfRigid.AddSlicePlane(slicePlane);

        rfRigid.demolitionEvent.LocalEvent += (rigid) => ApplyExplosionForce(rigid);
        // Perform the slice
        rfRigid.Slice();



        Debug.Log($"Object sliced using plane at {planeTransform.position}");
    }

    void ApplyNoGrav(RayfireRigid rigid)
    {
        foreach (RayfireRigid fragment in rigid.fragments)
        {
            Rigidbody rb = fragment.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;     
            }
        }   
    }


    void ApplyExplosionForce(RayfireRigid rigid)
    {
        Vector3 explosionPosition = slicingPlane.transform.position;
        
      //Vector3 planeOrigin = planeTransform.position;
        //Vector3 planeNormal = planeTransform.up;


        foreach (RayfireRigid fragment in rigid.fragments)
        {
            Rigidbody rb = fragment.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = 50f;
                rb.drag = 0.5f;
                rb.angularDrag = 0.1f;
                rb.useGravity = false;

                // Calculate direction from impact point to fragment
                Vector3 direction = (rb.position - explosionPosition).normalized;

                // Apply force outward from the impact point
                rb.AddForce(direction * 50f, ForceMode.Impulse);
            }
            float rval = Random.value;

        }
    }

}


/*

*/