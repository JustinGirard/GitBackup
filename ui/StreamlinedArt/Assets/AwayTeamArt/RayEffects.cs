using UnityEngine;
using RayFire;
using UnityEngine;  // Core Unity engine
using System;       // For general C# system utilities (if needed)
using System.Collections;  // For coroutine support (if expanding later)
using System.Collections.Generic;  // For lists or dictionaries (if needed)
using MDPackage;
using Unity.VisualScripting;
using UnityMeshSimplifier;
using Unity.VisualScripting.Dependencies.NCalc;
using System.Net.Http.Headers;
using System.Linq;
class MeshEffect
{
    public static List<RayfireRigid>  DoExplode(GameObject target, float explosionForce, float fragmentFraction, bool doRecurse)
    {
        GameObject cube = target.gameObject;
        RayfireRigid rfRigid = cube.GetComponent<RayfireRigid>();
        if (rfRigid == null)
            rfRigid = cube.AddComponent<RayfireRigid>();

        rfRigid.demolitionType = DemolitionType.Runtime;
        rfRigid.activation.act = true;
        rfRigid.activation.imp = true;
        rfRigid.demolitionEvent.LocalEvent += (rigid) => MeshEffect.ApplyExplosionForce(rigid,explosionForce,fragmentFraction,doRecurse);

        rfRigid.Initialize();
        rfRigid.Demolish();
        return rfRigid.HasFragments ? rfRigid.fragments : new List<RayfireRigid>();        
    }

    /*
    private static (Vector3 closestPoint, int hitCount, List<GameObject> hit, List<GameObject> missed) CalculateClosestPoint_Collider(Vector3 spherePosition, GameObject target, Collider[] colliders)
    {
        //Vector3 averagePoint = Vector3.zero;
        int hitCount = 0;
        List<GameObject> hit = new List<GameObject>();
        List<GameObject> missed = new List<GameObject>();
        Vector3 closestPoint = new Vector3(0,0,0);
        foreach (Collider col in colliders)
        {
            if (col.transform.IsChildOf(target.transform))
            {
                closestPoint = col.ClosestPoint(spherePosition);
                //averagePoint += closestPoint;
                hitCount++;
                hit.Add(col.gameObject);
            }
            else
            {
                missed.Add(col.gameObject);
            }
        }

        return (closestPoint, hitCount, hit, missed);
    }*/
    private static (Dictionary<Collider, Vector3> closestPoints, int hitCount, List<GameObject> hit, List<GameObject> missed) CalculateClosestPoint_Collider(Vector3 spherePosition, GameObject target, Collider[] colliders)
    {
        int hitCount = 0;
        List<GameObject> hit = new List<GameObject>();
        List<GameObject> missed = new List<GameObject>();
        Dictionary<Collider, Vector3> closestPoints = new Dictionary<Collider, Vector3>();

        foreach (Collider col in colliders)
        {
            if (col.transform.IsChildOf(target.transform))
            {
                Vector3 point = col.ClosestPoint(spherePosition);
                closestPoints[col] = point;
                hitCount++;
                hit.Add(col.gameObject);
            }
            else
            {
                missed.Add(col.gameObject);
            }
        }

        closestPoints = closestPoints
            .OrderBy(pair => Vector3.Distance(spherePosition, pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        return (closestPoints, hitCount, hit, missed);
    }    
    /*
    private static (Vector3 averagePoint, int hitCount, List<GameObject> hit, List<GameObject> missed) CalculateClosestPoint_Mesh(Vector3 spherePosition, float sphereRadius, GameObject target, Collider[] colliders)
    {
        Vector3 averagePoint = Vector3.zero;
        int hitCount = 0;
        List<GameObject> hit = new List<GameObject>();
        List<GameObject> missed = new List<GameObject>();

        Collider[] overlappingColliders = Physics.OverlapSphere(spherePosition, sphereRadius);

        foreach (Collider col in overlappingColliders)
        {
            if (col.transform.IsChildOf(target.transform))
            {
                Vector3 closestPoint = col.ClosestPoint(spherePosition);
                averagePoint += closestPoint;
                hitCount++;
                hit.Add(col.gameObject);
            }
            else
            {
                missed.Add(col.gameObject);
            }
        }

        return (averagePoint / Mathf.Max(hitCount, 1), hitCount, hit, missed);
    }*/



    public static GameObject DoSphereDamage(GameObject target, 
                                            GameObject sphere, 
                                            int fragmentNumber,  
                                            bool allColliders , 
                                            float explosionForce , 
                                            int layersMax)
    {
        // fractureLevel
        // Debug.Log($"Doing Damage on -----{target}");
        if (sphere == null)
        {
            Debug.LogWarning("No sphere assigned for damage calculation.");
            return target;
        }

        RayfireRigid rfRigid = target.GetComponent<RayfireRigid>();        
        Collider sphereCollider = sphere.GetComponent<Collider>();

        Vector3 spherePosition = sphere.transform.position;
        float sphereRadius = sphereCollider.bounds.extents.x*1.3f+0.5f;

        // Get all colliders that overlap with the sphere
        Collider[] colliders = Physics.OverlapSphere(spherePosition, sphereRadius);
        
        if (colliders.Length > 0)
        {
            // Calculate the average point of overlap
            /*
            Vector3 averagePoint = Vector3.zero;
            int hitCount = 0;

            Collider[] targetColliders = target.GetComponentsInChildren<Collider>();
            List<GameObject> hit = new List<GameObject>();
            List<GameObject> missed = new List<GameObject>();
            foreach (Collider col in colliders)
            {
                // Use Transform hierarchy to check if the collider belongs to the target
                if (col.transform.IsChildOf(target.transform))
                {
                    Vector3 closestPoint = col.ClosestPoint(spherePosition);
                    averagePoint += closestPoint;
                    hitCount++;
                    hit.Add(col.gameObject);
                }
                else
                {
                    missed.Add(col.gameObject);
                }

            }*/
            (Dictionary<Collider,Vector3 >closestPoints, 
            int hitCount, 
            List<GameObject> hit, 
            List<GameObject> missed) =  CalculateClosestPoint_Collider( 
                                            spherePosition,  
                                            target, 
                                            colliders);


            Debug.Log($"-------------");
            Debug.Log($"--------------------------");
            Debug.Log($"---------------------------------------");
            Debug.Log($"target.name: {target.name}");
            Debug.Log($"colliders.Length: {colliders.Length}");
            Debug.Log($"hit.Count: {hit.Count}");
            Debug.Log($"missed.Count: {missed.Count}");
            if (hitCount <= 0)
            {
                Debug.Log($"No collision - {sphere.name}: {target.name}, colliders.Length:{colliders.Length}");
                foreach (GameObject obj in missed)
                {
                    Debug.Log($"missed - {obj.name}");
                }
            }
            else
            {
                    Debug.Log($"HIT");
            }

            //Debug.Log($"Working with {sphere.name} collision with {target.name} on fracture level {layersMax}");

            if (hitCount > 0)
            {
                allColliders = false;
                if (allColliders==true)
                {
                    foreach (Collider col in colliders)
                    {
                        if (col.transform.IsChildOf(target.transform))
                        {
                            //Vector3 point = col.ClosestPoint(spherePosition);
                            // Dictionary<Collider,Vector3 >closestPoints,                             
                            foreach (Collider collider in closestPoints.Keys)
                            {
                                Vector3 closestPoint = closestPoints[collider];
                                target = MeshEffect.DoPointDamage(target.gameObject, fragmentNumber, closestPoint, sphereRadius, null,explosionForce,layersMax);

                            }
                        }
                    }
                    return target;  // Return target after applying to all colliders*/
                }
                else
                {
                    //closestPoint /= hitCount;  // Average the overlap point
                    var firstElement = closestPoints.First();
                    Collider firstCollider = firstElement.Key;
                    Vector3 firstPoint = firstElement.Value;
                    return MeshEffect.DoPointDamage(target.gameObject, fragmentNumber, firstPoint, sphereRadius, null,explosionForce,layersMax);
                }
            }
            else
            {
                Debug.Log($"No valid collision points on the target. between {sphere.name} and {target.name}");
                return target;
            }
        }
        else
        {
            Debug.LogWarning("No overlap detected for sphere damage.");
        }
        return target;

    }
    
    public static Mesh WeldVertices(Mesh mesh, float simplificationQuality = 0.5f)
    {
        // Initialize the mesh simplifier
        var simplifier = new UnityMeshSimplifier.MeshSimplifier();
        simplifier.Initialize(mesh);

        // Apply simplification to reduce vertex count
        simplifier.SimplifyMesh(simplificationQuality);  // 0.5f = 50% simplification

        // Generate the simplified mesh
        Mesh simplifiedMesh = simplifier.ToMesh();

        // Replace original mesh with the simplified version
        //mesh.Clear();
        //mesh.vertices = simplifiedMesh.vertices;
        //mesh.triangles = simplifiedMesh.triangles;
        //mesh.normals = simplifiedMesh.normals;
        //mesh.uv = simplifiedMesh.uv;
        //mesh.RecalculateBounds();
        return simplifiedMesh;
        Debug.Log($"Mesh Simplified: Vertices {mesh.vertexCount}, Triangles {mesh.triangles.Length / 3}");
    }




    public static GameObject MergeFragments(List<RayfireRigid> fragments, RayfireRigid excludeFragment = null)
    {
        throw new System.Exception("Disabled");
        if (fragments == null || fragments.Count == 0)
            return null;

        // Create a parent object for the merged fragments
        GameObject mergedObject = new GameObject("MergedFragments");

        // Use the first fragment as the anchor point for positioning
        Vector3 mergedPosition = fragments[0].transform.position;
        mergedObject.transform.position = mergedPosition;
        mergedObject.transform.rotation = fragments[0].transform.rotation;

        List<MeshFilter> meshFilters = new List<MeshFilter>();

        foreach (RayfireRigid fragment in fragments)
        {
            if (fragment == excludeFragment)
                continue;

            MeshFilter mf = fragment.GetComponent<MeshFilter>();
            if (mf != null)
            {
                meshFilters.Add(mf);
            }
        }

        // Combine meshes into a single mesh, transforming vertices to mergedObject's local space
        CombineInstance[] combine = new CombineInstance[meshFilters.Count];
        for (int i = 0; i < meshFilters.Count; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix * mergedObject.transform.worldToLocalMatrix;
        }

        MeshFilter mergedMeshFilter = mergedObject.AddComponent<MeshFilter>();
        MeshRenderer mergedRenderer = mergedObject.AddComponent<MeshRenderer>();

        Mesh mergedMesh = new Mesh();
        mergedMesh.CombineMeshes(combine, true, true);  // Merge with transform apply
        
        WeldVertices(mesh:mergedMesh, simplificationQuality:0.5f);

        Mesh simpleMesh = mergedMeshFilter.mesh = mergedMesh;

        // Copy material from the first fragment
        if (meshFilters.Count > 0)
        {
            mergedRenderer.material = meshFilters[0].GetComponent<MeshRenderer>().material;
        }

        // Apply MeshCollider to register in physics
        MeshCollider meshCollider = mergedObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = simpleMesh;
        meshCollider.convex = true;  // Set convex if dynamic physics is needed

        // Optionally add a kinematic Rigidbody to the merged object
        Rigidbody mergedRb = mergedObject.AddComponent<Rigidbody>();
        mergedRb.isKinematic = true;
        mergedRb.useGravity = false;

        // Destroy original fragments to clean up the scene
        foreach (RayfireRigid fragment in fragments)
        {
            if (fragment != excludeFragment)
            {
                GameObject.Destroy(fragment.gameObject);
            }
        }

        return mergedObject;
    }




    public static GameObject DPD_MergeFragmentsVirtual(List<RayfireRigid> fragments)
    {
        if (fragments == null || fragments.Count == 0)
            return null;

        // Create a parent object for the merged fragments
        GameObject mergedObject = new GameObject("MergedFragments");
        mergedObject.transform.position = fragments[0].transform.position;
        mergedObject.transform.rotation = fragments[0].transform.rotation;

        foreach (RayfireRigid fragment in fragments)
        {
            fragment.transform.SetParent(mergedObject.transform);
            //MeshCollider meshCollider = mergedObject.AddComponent<MeshCollider>();
            //Mesh simpleMesh =  fragment.GetComponent<MeshFilter>()?.mesh; 
            //meshCollider.sharedMesh = simpleMesh;
            //meshCollider.convex = true;
            fragment.AddComponent<UnitFragment>();     
            UnitFragment cn = fragment.GetComponent<UnitFragment>();
            cn.SetLayer(1);
            // Disable or make rigidbody kinematic
            Rigidbody rb = fragment.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        // Optionally add a kinematic Rigidbody to the merged object
        //Rigidbody mergedRb = mergedObject.AddComponent<Rigidbody>();
        //mergedRb.isKinematic = true;
        //mergedRb.useGravity = false;

        return mergedObject;
    }
    private static List<RayfireRigid> DPD_GetTargetFragments(GameObject target)
    {
        List<RayfireRigid> fragments = new List<RayfireRigid> {};
        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            RayfireRigid rfRigid = target.GetComponent<RayfireRigid>();
            if (rfRigid == null)
                rfRigid = target.AddComponent<RayfireRigid>();
            rfRigid.demolitionType = DemolitionType.Runtime;
            rfRigid.activation.act = true;
            rfRigid.activation.imp = true;
            fragments = new List<RayfireRigid> {rfRigid};
        }
        else
        {
            RayfireRigid[] childRigids = target.GetComponentsInChildren<RayfireRigid>();
            if (childRigids.Length > 0)
            {
                fragments.AddRange(childRigids);
            }
            
        }
        return fragments;
    }

    private static (GameObject, List<RayfireRigid>) _DPD_GetL1ShatteredObject(List<RayfireRigid> fragments, GameObject target, int layersMax, int fragmentNumber)
    {
        // Do an initial shatter of the 
        if (fragments.Count == 1 && layersMax > 0)
        {
            Debug.Log($"Initial Shatter -- setting up for more fragmentation");
            RayfireRigid closestFragment = fragments[0];
            closestFragment.mshDemol.am = fragmentNumber;  // Limit to 10 fragments
            closestFragment.demolitionEvent.LocalEvent += (rigid) => MeshEffect.RemoveGravity(rigid);
            closestFragment.Initialize();
            closestFragment.Demolish();
            fragments = closestFragment.fragments;
            GameObject newObj = DPD_MergeFragmentsVirtual(fragments);
            newObj.transform.parent = target.transform.parent;
            newObj.name = target.name + "_ix";
            return (newObj,fragments);
        }
        return (target, fragments);
    }

    private static List<RayfireRigid> _DPD_GetAffectedFragments(List<RayfireRigid> fragments, GameObject target,Vector3 damagePoint, int layersMax, int fragmentNumber)
    {
        float closestDistance = 100000.0f;
        RayfireRigid closestFragment = null;
        foreach (RayfireRigid fragment in fragments)
        {
            float dist = Vector3.Distance(fragment.transform.position, damagePoint);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestFragment = fragment;
            }
        }
        List<RayfireRigid>  affectedFrags = new List<RayfireRigid>(){closestFragment};
        return affectedFrags;
    }

    private static GameObject DoPointDamage(
        GameObject target, 
        int fragmentNumber, 
        Vector3 damagePoint, 
        float damageRadius, 
        Collider coll , 
        float explosionForce,
        int layersMax
        )
    {
        
        List<RayfireRigid> allFragments =  DPD_GetTargetFragments( target);
        (target,allFragments) = _DPD_GetL1ShatteredObject(allFragments, target, layersMax, fragmentNumber);

        List<RayfireRigid> affectedFragments  = _DPD_GetAffectedFragments(allFragments, target, damagePoint,  layersMax, fragmentNumber);

        foreach (var fragment in affectedFragments)
        {
            Renderer renderer = fragment.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            }
        }

        // affectedFragments;

        return target;
        /*
        if (fragments.Count <= 0)
            Debug.LogError("EMPTY FRAGMENTS");
            if (closestFragment.HasFragments)
            {
                fragments = closestFragment.fragments;
            }
            float closestDistance = float.MaxValue;
            foreach (RayfireRigid fragment in fragments)
            {
                float dist = Vector3.Distance(fragment.transform.position, damagePoint);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestFragment = fragment;
                }
            }
*/

        // Ensure Rayfire is properly initialized
        /*
        RayfireRigid closestFragment = null;
        float closestDistance = float.MaxValue;

        foreach (RayfireRigid fragment in fragments)
        {
            float dist = Vector3.Distance(fragment.transform.position, damagePoint);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestFragment = fragment;
            }
        }

        /// Merge & all fragments but the closest together, to perserve the intgrity of the object
        
        if (closestFragment != null)
        {
            closestFragment.transform.parent = target.transform.parent;
            closestFragment.name = target.name + "x_closestFragmentLabel";
            //ApplyExplosionForce(closestFragment, 200f, 0f, true);  // Explode only the closest fragment
            
            bool doRecurse = false;
            float fragmentFraction = 0.0f;
            //MeshEffect.DoExplode(closestFragment.gameObject,explosionForce,fragmentFraction,doRecurse);
            Debug.Log($"Exploded fragment {closestFragment.name}");
        }
    
        return newObj;       */ 
    }

    /*
    rfRigid.ApplyDamage
        public bool ApplyDamage (float damageValue, Vector3 damagePoint, float damageRadius = 0f, Collider coll = null)
        {
            return RFDamage.ApplyDamage (this, damageValue, damagePoint, damageRadius, coll);
        }
            
    */
    // For destroying a fragment further
    public static void ApplyExplosionForce(RayfireRigid rigid,float explosionForce, float fragmentFraction, bool doFracture)
    {
        Vector3 explosionPosition = rigid.transform.position;
        // 
        foreach (RayfireRigid fragment in rigid.fragments)
        {
            Rigidbody rb = fragment.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = 50f;
                rb.drag = 2.5f;
                rb.angularDrag = 0.1f;
                rb.useGravity = false;
                Vector3 direction = (rb.position - explosionPosition).normalized;
                rb.AddForce(direction * explosionForce, ForceMode.Impulse);
            }
            //float rval = UnityEngine.Random.value;
            if (doFracture == true)// && UnityEngine.Random.value <= fragmentFraction)
            {
                //Debug.Log("Should recurse");
               
                List<RayfireRigid>  frags = MeshEffect.DoExplode(fragment.gameObject,explosionForce,fragmentFraction,false);
                //Color randomColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                Color randomColor = new Color(1f, UnityEngine.Random.value,UnityEngine.Random.value);
                foreach (RayfireRigid frag in frags)
                {
                    Renderer renderer = frag.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = randomColor;
                    }
                }
            }
        }
    }

    public static void RemoveGravity(RayfireRigid rigid)
    {
        foreach (RayfireRigid fragment in rigid.fragments)
        {
            Rigidbody rb = fragment.GetComponent<Rigidbody>();
            rb.useGravity = false;
        }
    }    

    /*
    API EXAMPLE
using UnityEngine;
using MDPackage.Modifiers;

[RequireComponent(typeof(MeshFilter))]
public class SampleScript : MonoBehaviour
{
 // Cache main camera in the inspector!
 public Camera mainCam;
 [Space]
 // Edit damage parameters
 public float damageRadius = 0.25f;
 public float damageForce = 0.5f;
 public bool continuousEffect = true;

 private MDM_MeshDamage damageable;

 private void Start()
 {
  // Add/Create a Mesh Damage modifier to this object right after start
  damageable = MD_ModifierBase.CreateModifier<MDM_MeshDamage>(gameObject, MD_ModifierBase.MeshReferenceType.CreateNewReference);
  // Add MeshCollider for raycast detection
  damageable.gameObject.AddComponent<MDPackage.MD_MeshColliderRefresher>();
 }

 private void Update()
 {
  // Main camera must be initialized
  if (!mainCam)
   return;

  // Sample extension - if R is held, restore mesh to its original state by the specific restoration speed
  if (Input.GetKey(KeyCode.R))
   damageable.MeshDamage_RestoreMesh(0.01f);

  if (Input.GetMouseButtonDown(0))
  {
   // Create raycast from camera to the cursor position
   Ray r = mainCam.ScreenPointToRay(Input.mousePosition);
   bool hit = Physics.Raycast(r, out RaycastHit h) && h.transform == damageable.transform;
   // Checking for potential hit - if the hit collider equals the damageable, damage the mesh
   if (hit) // Modify damageable mesh by the hit point, damage radius, damage force, direction and continuous effect
    damageable.MeshDamage_ModifyMesh(h.point, Mathf.Abs(damageRadius), -Mathf.Abs(damageForce), h.normal, continuousEffect);
  }
 }
}    
    */
    /// <summary>
    /*

    public static void ApplyMeshDeformation(GameObject hitSphere,GameObject target, float deformationForce)
    {
        // Ensure the target has a MeshFilter and shared mesh
        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("ApplyMeshDeformation: No mesh found on target object.");
            return;
        }

        // Clone the mesh to avoid modifying the shared mesh across all instances
        if (meshFilter.mesh == meshFilter.sharedMesh)
        {
            Mesh newMesh = UnityEngine.Object.Instantiate(meshFilter.sharedMesh);

            // Mesh newMesh = Instantiate(meshFilter.sharedMesh);
            newMesh.name = target.name + "_Deformed";
            meshFilter.mesh = newMesh;
        }

        // Ensure MeshDeformer component exists
        MDPackage.Modifiers.MDM_MeshDamage deformer = target.GetComponent<MDPackage.Modifiers.MDM_MeshDamage>();
        if (deformer == null)
        {
            deformer = target.AddComponent<MDPackage.Modifiers.MDM_MeshDamage>();
            deformer.MDModifier_InitializeBase(MD_ModifierBase.MeshReferenceType.CreateNewReference, true);            
        }
        // Apply deformation at the object's center
        // ADD DEFORMATION CODE HERE
        Vector3 hitPoint = hitSphere.transform.position;
        float radius = hitSphere.GetComponent<SphereCollider>().radius * hitSphere.transform.lossyScale.x;
        Debug.Log("Calling Deform!");
        deformer.MeshDamage_ModifyMesh(hitPoint, radius, deformationForce, Vector3.down);


    }*/

    public static void ApplyMeshDeformation(GameObject hitSphere, GameObject target, float deformationForce)
    {
        // Ensure the target has a MeshFilter and shared mesh
        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("ApplyMeshDeformation: No mesh found on target object.");
            return;
        }

        // Clone the mesh to avoid modifying the shared mesh across all instances
        if (meshFilter.mesh == meshFilter.sharedMesh)
        {
            Mesh newMesh = UnityEngine.Object.Instantiate(meshFilter.sharedMesh);
            newMesh.name = target.name + "_Deformed";
            meshFilter.mesh = newMesh;
        }

        // Ensure MDM_MeshDamage component exists and is initialized correctly
        MDPackage.Modifiers.MDM_MeshDamage deformer = target.GetComponent<MDPackage.Modifiers.MDM_MeshDamage>();
        if (deformer == null)
        {
            // Use the correct factory method to initialize the modifier
            deformer = MDPackage.Modifiers.MD_ModifierBase.CreateModifier<MDPackage.Modifiers.MDM_MeshDamage>(
                target, 
                MDPackage.Modifiers.MD_ModifierBase.MeshReferenceType.CreateNewReference
            );

            // Add collider refresher for accurate raycast detection if needed
            if (target.GetComponent<MDPackage.MD_MeshColliderRefresher>() == null)
            {
                target.AddComponent<MDPackage.MD_MeshColliderRefresher>();
            }
        }

        // Calculate the deformation hit point and radius based on the sphere
        Vector3 hitPoint = hitSphere.transform.position;
        float radius = hitSphere.GetComponent<SphereCollider>().radius * hitSphere.transform.lossyScale.x;

        // Apply mesh deformation (negative force pulls inward)
        Debug.Log("Applying Mesh Deformation!");
        deformer.MeshDamage_ModifyMesh(hitPoint, radius, deformationForce, Vector3.down, true);
    }


    public static void ApplyDeformationToChildren(GameObject hitSphere, GameObject parent, float deformationForce)
    {
        SphereCollider sphereCollider = hitSphere.GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            Debug.LogWarning("ApplyDeformationToChildren: Sphere does not have a SphereCollider.");
            return;
        }

        Vector3 spherePosition = hitSphere.transform.position;
        float sphereRadius = sphereCollider.radius * hitSphere.transform.lossyScale.x;
        Collider[] overlappingColliders = Physics.OverlapSphere(spherePosition, sphereRadius);

        foreach (Transform child in parent.transform)
        {
            Collider childCollider = child.GetComponent<Collider>();
            if (childCollider == null) 
                continue;

            if (Array.Exists(overlappingColliders, collider => collider == childCollider))
            {
                Debug.Log($"Deforming child: {child.name}");
                ApplyMeshDeformation(hitSphere, child.gameObject, deformationForce);
            }
        }
    }



}