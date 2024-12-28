using UnityEngine;
using RayFire;
using System.Collections.Generic;

public class HolePunch : MonoBehaviour
{
    public GameObject slicingCube;  // Cube to slice around the object
    public float delay = 1.0f;
    private float timer = 0.0f;
    private bool hasDemolished = false;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= delay && !hasDemolished)
        {
            if (slicingCube != null)
            {
                SliceWithCube(slicingCube.transform);
                hasDemolished = true;
            }
        }
    }


/*
    // Slice the object using the cube's bounds
    void SliceWithCube(Transform cubeTransform)
    {
        RayfireRigid rfRigid = GetComponent<RayfireRigid>();

        if (rfRigid == null)
        {
            rfRigid = gameObject.AddComponent<RayfireRigid>();
            rfRigid.demolitionType = DemolitionType.Runtime;
        }

        // Ensure RayFire is initialized
        rfRigid.Initialize();

        // Get cube bounds
        Bounds bounds = slicingCube.GetComponent<Collider>().bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        // Generate six slicing planes (box slice)
        Vector3[] slicePlanes = new Vector3[12]
        {
            // Top and bottom planes
            center + Vector3.up * extents.y, Vector3.down,
            center - Vector3.up * extents.y, Vector3.up,

            // Front and back planes
            center + Vector3.forward * extents.z, Vector3.back,
            center - Vector3.forward * extents.z, Vector3.forward,

            // Left and right planes
            center + Vector3.right * extents.x, Vector3.left,
            center - Vector3.right * extents.x, Vector3.right
        };

        // Apply slicing planes
        for (int i = 0; i < slicePlanes.Length; i += 2)
        {
            Vector3[] plane = new Vector3[2] { slicePlanes[i], slicePlanes[i + 1] };
            rfRigid.AddSlicePlane(plane);
        }

        rfRigid.demolitionEvent.LocalEvent += (rigid) => ApplyNoGrav(rigid);
        rfRigid.Slice();
        ApplyLinearJoints(rfRigid, slicingCube.GetComponent<Collider>());
        ApplyExplosionToBounds(slicingCube.GetComponent<Collider>(),rfRigid);
        Debug.Log($"Object sliced using cube at {cubeTransform.position}");
    }*/

    void SliceWithCube(Transform cubeTransform)
    {
        RayfireRigid rfRigid = GetComponent<RayfireRigid>();

        if (rfRigid == null)
        {
            rfRigid = gameObject.AddComponent<RayfireRigid>();
            rfRigid.demolitionType = DemolitionType.Runtime;
        }

        // Ensure RayFire is initialized
        rfRigid.Initialize();

        Collider cubeCollider = slicingCube.GetComponent<Collider>();

        // Generate slicing planes using the collider's world points
        Vector3[] slicePlanes = CalculateSlicePlanes(cubeCollider);

        // Apply slicing planes
        for (int i = 0; i < slicePlanes.Length; i += 2)
        {
            Vector3[] plane = new Vector3[2] { slicePlanes[i], slicePlanes[i + 1] };
            rfRigid.AddSlicePlane(plane);
        }

        rfRigid.demolitionEvent.LocalEvent += (rigid) => ApplyNoGrav(rigid);
        rfRigid.Slice();
        ApplyLinearJoints(rfRigid, cubeCollider);
        ApplyExplosionToBounds(cubeCollider, rfRigid);
        Debug.Log($"Object sliced using cube at {cubeTransform.position}");
    }
    Vector3[] CalculateSlicePlanes(Collider collider)
    {
        // Get world space points from collider
        if (collider is BoxCollider box)
        {
            Vector3[] planes = new Vector3[12];
            Vector3 center = box.transform.TransformPoint(box.center);
            Vector3 size = box.size * 0.25f;
            Quaternion rotation = box.transform.rotation;

            // Local axes adjusted by rotation
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 right = rotation * Vector3.right;

            // Create slicing planes in world space
            planes[0]  = center + up * size.y;  planes[1]  = -up;   // Top
            planes[2]  = center - up * size.y;  planes[3]  = up;    // Bottom
            planes[4]  = center + forward * size.z;  planes[5]  = -forward;  // Front
            planes[6]  = center - forward * size.z;  planes[7]  = forward;   // Back
            planes[8]  = center + right * size.x;    planes[9]  = -right;    // Right
            planes[10] = center - right * size.x;    planes[11] = right;     // Left

            return planes;
        }
        
        Debug.LogWarning("Collider type not supported for slicing.");
        return new Vector3[0];
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
    void ApplyExplosionToBounds(Collider slicingCube, RayfireRigid rigid)
    {
        Bounds sliceBounds = slicingCube.bounds;

        foreach (RayfireRigid fragment in rigid.fragments)
        {
            if (fragment != null && fragment.GetComponent<Collider>() != null)
            {
                Bounds fragmentBounds = fragment.GetComponent<Collider>().bounds;

                // Check if the fragment is within the slicing cube bounds
                if (sliceBounds.Contains(fragmentBounds.center))
                {
                    // Prepare the fragment for demolition
                    fragment.demolitionType = DemolitionType.Runtime;
                    fragment.activation.act = true;
                    fragment.activation.imp = true;

                    fragment.demolitionEvent.LocalEvent += (rigid) => ApplyExplosionForce(rigid);

                    fragment.Initialize();
                    fragment.Demolish();
                }
            }
        }
    }

    void ApplyExplosionForce(RayfireRigid rigid)
    {
        foreach (RayfireRigid frag in rigid.fragments)
        {
            Rigidbody rb = frag.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 explosionCenter = rigid.transform.position;
                rb.useGravity = false;
                rb.AddExplosionForce(2.2f, explosionCenter, 0.005f, 1.0f, ForceMode.Impulse);
            }
        }
    }




    /*void ApplyClusterize(RayfireRigid rigid, Collider slicingCube)
    {
        RayfireConnectivity connectivity = rigid.GetComponent<RayfireConnectivity>();
        if (connectivity == null)
        {
            connectivity = rigid.gameObject.AddComponent<RayfireConnectivity>();
            connectivity.type = ConnectivityType.ByBoundingBox;
            connectivity.clusterize = true;  // Enable clustering
            connectivity.demolishable = false;  // Keep the outer shell intact
        }

        bool wasInactive = !rigid.gameObject.activeSelf;
        
        if (wasInactive)
            rigid.gameObject.SetActive(true);  // Temporarily activate
        
        connectivity.Initialize();            
        
        if (wasInactive)
            rigid.gameObject.SetActive(false);  // Revert to inactive state        

    }*/
/*
void ApplyClusterize(RayfireRigid rigid, Collider slicingCube)
{
    RayfireConnectivity connectivity = rigid.GetComponent<RayfireConnectivity>();
    if (connectivity == null)
    {
        connectivity = rigid.gameObject.AddComponent<RayfireConnectivity>();
        connectivity.type = ConnectivityType.ByBoundingBox;
        connectivity.clusterize = true;
        connectivity.demolishable = false;
    }

    // Ensure object is active for coroutine start
    bool wasInactive = !rigid.gameObject.activeSelf;
    if (wasInactive)
        rigid.gameObject.SetActive(true);

    connectivity.Initialize();

    // Detach fragments inside slicing cube
    Bounds sliceBounds = slicingCube.bounds;
    List<RayfireRigid> outerFragments = new List<RayfireRigid>();

    foreach (RayfireRigid frag in rigid.fragments)
    {
        if (sliceBounds.Contains(frag.transform.position))
        {
            frag.activation.loc = false;  // Exclude from connectivity
        }
        else
        {
            outerFragments.Add(frag);
        }
    }

    // Rebuild connectivity with outer fragments
    connectivity.rigidList = outerFragments;
    connectivity.Initialize();

    if (wasInactive)
        rigid.gameObject.SetActive(false);
}*/
/*
void ApplyLinearJoints(RayfireRigid rigid, Collider slicingBounds)
{
    List<RayfireRigid> fragments = rigid.fragments;
    
    RayfireRigid previousFragment = null;

    foreach (var fragment in fragments)
    {
        Rigidbody rb = fragment.GetComponent<Rigidbody>();
        if (rb == null) continue;

        // Skip if the fragment is inside the slicing cube
        if (slicingBounds.bounds.Contains(rb.position)) 
            continue;

        if (previousFragment != null)
        {
            Rigidbody prevRb = previousFragment.GetComponent<Rigidbody>();
            if (prevRb != null)
            {
                FixedJoint joint = rb.gameObject.AddComponent<FixedJoint>();
                joint.connectedBody = prevRb;
                joint.breakForce = Mathf.Infinity;
                joint.breakTorque = Mathf.Infinity;
            }
        }

        // Update previous fragment
        previousFragment = fragment;
    }
}*/


void ApplyLinearJoints(RayfireRigid rigid, Collider slicingBounds)
{
    List<RayfireRigid> fragments = rigid.fragments;
    
    Queue<Rigidbody> previousFragments = new Queue<Rigidbody>(3);

    foreach (var fragment in fragments)
    {
        Rigidbody rb = fragment.GetComponent<Rigidbody>();
        if (rb == null) continue;

        // Skip if the fragment is inside the slicing cube
        if (slicingBounds.bounds.Contains(rb.position)) 
            continue;

        // Connect to previous three fragments
        foreach (Rigidbody prevRb in previousFragments)
        {
            if (prevRb != null)
            {
                FixedJoint joint = rb.gameObject.AddComponent<FixedJoint>();
                joint.connectedBody = prevRb;
                joint.breakForce = Mathf.Infinity;
                joint.breakTorque = Mathf.Infinity;
            }
        }

        // Track the last three fragments
        previousFragments.Enqueue(rb);
        if (previousFragments.Count > 3)
            previousFragments.Dequeue();
    }
}




   
}
