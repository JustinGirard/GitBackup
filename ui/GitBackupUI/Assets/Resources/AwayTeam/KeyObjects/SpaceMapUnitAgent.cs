using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class ProjectileEmitter
{
    public ProjectileEmitter(){}
    public ProjectileEmitter(GameObject obj, string n){
        gameObject = obj;
        name = n;
    }

    [SerializeField] 
    public GameObject gameObject;

    [SerializeField] 
    public string name;
}

public class SpaceMapUnitAgentRenamed : MonoBehaviour, IPausable
{
    [SerializeField]
    private List<ProjectileEmitter> projectileEmitters = new List<ProjectileEmitter>();

    // Private target variables
    private Vector3 targetLookAt;
    private Vector3 targetTravelTo;

    // Public speed and smoothness settings
    public float rotationSpeed = 1.0f;
    public float travelSpeed = 0.5f;
    public float rotationSmoothTime = 0.3f;
    public float positionSmoothTime = 0.3f;
    public float dampenerSpeed = 1.0f; // Speed at which the dampener scales up

    // Internal variables for smoothing
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 rotationVelocity = Vector3.zero;

    // Dampeners for easing
    private float lookAtDampener = 0f;
    private float travelDampener = 1f;
    private float timer = 0f; // Timer to track elapsed time
    private float mapHeight = 0f;

    public float maxTravelSpeed = 1.0f; // Maximum travel speed
    public float acceleration = 0.5f; // Rate of acceleration
    public float decelerationDistance = 1.0f; // Distance to start decelerating
    public float stopDistance = 0.05f; // Distance to snap to the target
    private Vector3 velocity = Vector3.zero;

    [SerializeField]
    Transform targetTransform;
    
    [SerializeField]
    Transform mapMarker;
    
    private LineRenderer lineRenderer; // Reference to the LineRenderer    

    private Vector3 rootTravelTo;
    private Vector3 rootLookAt;

    Rigidbody rb;
    SphereCollider detectionCollider;
    float perceptionRadius = 100f;
    float maxForce = 1f;
    public List<ProjectileEmitter> GetEmitters()
    {
        return projectileEmitters;
    }

    public void SafeDestroy()
    {
        // Debug.Log($"DESTROYING MYSELF: {name}");
        GameObject.Destroy(this.gameObject);
    }
    void OnDestroy()
    {
        // Debug.Log($"NOTIFICATION MYSELF: {name}");
        EncounterSquad unit = GetComponentInParent<EncounterSquad>();
        if( unit != null)
            unit.NotifyDestroy(this.gameObject
            );
    }    

    public ProjectileEmitter GetEmitter(string emitterName)
    {
        return projectileEmitters.FirstOrDefault(emitter => emitter.name == emitterName);
    }
    public List<int> GetEmitterIds()
    {
        List<int> ids = new List<int>();
        for (int i = 0; i < projectileEmitters.Count; i++)
        {
            ids.Add(i);
        }
        return ids;
    }

    private bool __is_running = false;
    public void Run()
    {
        __is_running = true;
    }
    public void Pause()
    {
        __is_running = false;

    }
    public bool IsRunning()
    {
        return __is_running;
    }    
    public void SetMapMarkerTransform(Transform mapMarker)
    {
        this.mapMarker = mapMarker;
    }
    public void SetShipTransform(Transform targetTransform)
    {
        this.targetTransform = targetTransform;
    }

    void Start()
    {
        //rootTravelTo = new Vector3(-100,-100,-100);
    }
    
    public bool SetGoalPosition(Vector3 goalPosition, bool immediate = false)
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("SetGoalPosition has null targetTransform ");
            return false;
        }
        //Debug.Break();
        rootTravelTo = goalPosition;
        if (immediate == true)
        {
            Debug.Log($"Setting Position of {this.transform.gameObject.name}:");
            targetTransform.position = goalPosition;
            this.transform.position = goalPosition;
        }
        //GameObject cube = CreateCube(rootTravelTo,Color.green);
        //cube.transform.parent = this.gameObject.transform;   


        //Debug.Break();
        return true;

    }
    private void UpdateLineRenderer()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("UpdateLineRenderer has null targetTransform ");
            return;
        }

        if (lineRenderer != null && mapMarker != null && targetTransform != null)
        {
            // Set the positions of the line
            lineRenderer.SetPosition(0, targetTransform.position); // Ship position
            lineRenderer.SetPosition(1, mapMarker.position);       // Map marker position
        }
    }

    void Update()
    {
        //Debug.Log()
        //return;
        if (__is_running == false)
            return;
        timer += Time.deltaTime;
        float driftScale = 0.05f;
        // Reassign random targets every 5 seconds
        if (timer >= 1f + UnityEngine.Random.Range(0.2f, 0.7f))
        {
            timer = 0f; // Reset timer
            

            Vector3 randomTravelTo = new Vector3(
                rootTravelTo.x + UnityEngine.Random.Range(-2, 2f)*driftScale,
                rootTravelTo.y + UnityEngine.Random.Range(-4f, 4f)*driftScale,
                rootTravelTo.z + UnityEngine.Random.Range(-1f, 1f)*driftScale
            );
            SetTravelTarget(randomTravelTo);
            SetLookAtTarget(rootLookAt);
        }

        UpdateDampeners();
        SmoothLookAt();
        SmoothTravel();
        UpdateMapMarker();
        UpdateLineRenderer();                
    }
    // Public accessors to set targets and reset dampeners
    public void SetRootLookAt(Vector3 newTargetLookAt,bool immediate = false)
    {
        //Debug.Log($"Setting new look at for {this.name}");
        rootLookAt = newTargetLookAt;
        if (immediate == true)
        {
            targetLookAt = rootLookAt;
            this.transform.LookAt(newTargetLookAt);
        }
    }
    // Public accessors to set targets and reset dampeners
    private void SetLookAtTarget(Vector3 newTargetLookAt)
    {
        targetLookAt = newTargetLookAt;
        if (lookAtDampener >= 0.9f)
            lookAtDampener = 0f; // Reset dampener
    }

    public void SetTravelTarget(Vector3 newTargetTravelTo)
    {
        targetTravelTo = newTargetTravelTo;
        //if (lookAtDampener >= 0.9f)
        //    travelDampener = 0f; // Reset dampener
    }

    private void UpdateDampeners()
    {
        // Gradually increase dampeners to 1
        lookAtDampener = Mathf.Clamp01(lookAtDampener + Time.deltaTime * dampenerSpeed);
        travelDampener = Mathf.Clamp01(travelDampener + Time.deltaTime * dampenerSpeed);
    }

    private void SmoothLookAt()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("SmoothLookAt has null targetTransform ");
            return;
        }

        // Calculate the target direction
        Vector3 direction = targetLookAt - targetTransform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            // Smoothly interpolate the rotation with dampening
            targetTransform.rotation = Quaternion.Lerp(
                targetTransform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed * lookAtDampener
            );
        }
    }
   

    private void SmoothTravel()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("SmoothTravel has null targetTransform ");
            return;
        }        
        Vector3 direction = targetTravelTo - targetTransform.position;
        float distance = direction.magnitude;

        direction.Normalize();

        // Determine the target speed based on distance
        float targetSpeed = maxTravelSpeed;

        if (distance < decelerationDistance)
        {
            targetSpeed = Mathf.Lerp(0, maxTravelSpeed, distance / decelerationDistance);
        }

        // Update velocity
        velocity = Vector3.MoveTowards(velocity, direction * targetSpeed, acceleration * Time.deltaTime);

        // Apply velocity
        targetTransform.position += velocity * Time.deltaTime;

    }    
    /*
    private Vector3 CalculateSeparation()
    {
        
        Vector3 separationForce = Vector3.zero;
        Collider[] neighbors = Physics.OverlapSphere(targetTransform.position, perceptionRadius);

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject)
            {
                // Calculate direction away from the neighbor
                Vector3 difference = targetTransform.position - neighbor.transform.position;
                float distance = difference.magnitude;

                // Add force inversely proportional to the distance
                if (distance > 0)
                {
                    separationForce += difference.normalized / distance;
                }
            }
        }
        return separationForce.normalized * maxForce;
    }
    */

    private void SetupPhysics()
    {
        // Add Rigidbody if not already attached
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Set Rigidbody to kinematic to prevent collision responses
        rb.useGravity = false; 
        rb.isKinematic = true; // Kinematic prevents physics-based collision responses

        // Add Sphere Collider for detection
        detectionCollider = GetComponent<SphereCollider>();
        if (detectionCollider == null)
        {
            detectionCollider = gameObject.AddComponent<SphereCollider>();
        }

        // Configure Sphere Collider as a trigger for detection
        detectionCollider.isTrigger = true;
        detectionCollider.radius = perceptionRadius; // Set radius to double the size of the ship
    }


    private void OnDrawGizmos()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("OnDrawGizmos has null targetTransform ");
            return;
        }           
        // Draw gizmos to visualize target positions
        //Gizmos.color = Color.red;
        //Gizmos.DrawSphere(targetLookAt, 0.5f); // LookAt target
        //Gizmos.color = Color.green;
        //Gizmos.DrawSphere(targetTravelTo, 0.5f); // TravelTo target
        //Gizmos.color = Color.blue;
        //Gizmos.DrawSphere(rootTravelTo, 0.5f); // TravelTo target
        //Gizmos.color = Color.green;
        //Gizmos.DrawLine(transform.position, targetTravelTo);

    }

    private void UpdateMapMarker()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("UpdateMapMarker has null targetTransform ");
            return;
        }           
        mapHeight = -5f;
        if (mapMarker != null)
        {
            // Clamp MapMarker to ship's XZ coordinates with constant Y
            Vector3 markerPosition = new Vector3(targetTransform.position.x, mapHeight, targetTransform.position.z);
            mapMarker.position = markerPosition;
        }
    }

}



/*
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class ProjectileEmitter
{
    [SerializeField] 
    public GameObject gameObject;

    [SerializeField] 
    public string name;
}

public class SpaceMapUnitAgent : MonoBehaviour, IPausable
{
    [SerializeField]
    private List<ProjectileEmitter> projectileEmitters = new List<ProjectileEmitter>();

    // Private target variables
    private Vector3 targetLookAt;
    private Vector3 targetTravelTo;

    // Public speed and smoothness settings
    public float rotationSpeed = 1.0f;
    public float travelSpeed = 0.5f;
    public float rotationSmoothTime = 0.3f;
    public float positionSmoothTime = 0.3f;
    public float dampenerSpeed = 1.0f; // Speed at which the dampener scales up

    // Internal variables for smoothing
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 rotationVelocity = Vector3.zero;

    // Dampeners for easing
    private float lookAtDampener = 0f;
    private float travelDampener = 1f;
    private float timer = 0f; // Timer to track elapsed time
    private float mapHeight = 0f;

    public float maxTravelSpeed = 1.0f; // Maximum travel speed
    public float acceleration = 0.5f; // Rate of acceleration
    public float decelerationDistance = 1.0f; // Distance to start decelerating
    public float stopDistance = 0.05f; // Distance to snap to the target
    private Vector3 velocity = Vector3.zero;

    [SerializeField]
    Transform targetTransform;
    
    [SerializeField]
    Transform mapMarker;
    
    private LineRenderer lineRenderer; // Reference to the LineRenderer    

    private Vector3 rootTravelTo;
    private Vector3 rootLookAt;

    Rigidbody rb;
    SphereCollider detectionCollider;
    float perceptionRadius = 100f;
    float maxForce = 1f;
    public List<ProjectileEmitter> GetEmitters()
    {
        return projectileEmitters;
    }

    public void SafeDestroy()
    {
        // Debug.Log($"DESTROYING MYSELF: {name}");
        GameObject.Destroy(this.gameObject);
    }
    void OnDestroy()
    {
        // Debug.Log($"NOTIFICATION MYSELF: {name}");
        EncounterSquad unit = GetComponentInParent<EncounterSquad>();
        if( unit != null)
            unit.NotifyDestroy(this.gameObject
            );
    }    

    public ProjectileEmitter GetEmitter(string emitterName)
    {
        return projectileEmitters.FirstOrDefault(emitter => emitter.name == emitterName);
    }
    public List<int> GetEmitterIds()
    {
        List<int> ids = new List<int>();
        for (int i = 0; i < projectileEmitters.Count; i++)
        {
            ids.Add(i);
        }
        return ids;
    }

    private bool __is_running = false;
    public void Run()
    {
        __is_running = true;
    }
    public void Pause()
    {
        __is_running = false;

    }
    public bool IsRunning()
    {
        return __is_running;
    }    
    public void SetMapMarkerTransform(Transform mapMarker)
    {
        this.mapMarker = mapMarker;
    }
    public void SetShipTransform(Transform targetTransform)
    {
        this.targetTransform = targetTransform;
    }

    void Start()
    {
        //rootTravelTo = new Vector3(-100,-100,-100);
    }
    
    public bool SetGoalPosition(Vector3 goalPosition, bool immediate = false)
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("SetGoalPosition has null targetTransform ");
            return false;
        }
        //Debug.Break();
        rootTravelTo = goalPosition;
        if (immediate == true)
        {
            Debug.Log($"Setting Position of {this.transform.gameObject.name}:");
            targetTransform.position = goalPosition;
            this.transform.position = goalPosition;
        }
        //GameObject cube = CreateCube(rootTravelTo,Color.green);
        //cube.transform.parent = this.gameObject.transform;   


        //Debug.Break();
        return true;

    }
    private void UpdateLineRenderer()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("UpdateLineRenderer has null targetTransform ");
            return;
        }

        if (lineRenderer != null && mapMarker != null && targetTransform != null)
        {
            // Set the positions of the line
            lineRenderer.SetPosition(0, targetTransform.position); // Ship position
            lineRenderer.SetPosition(1, mapMarker.position);       // Map marker position
        }
    }

    void Update()
    {
        //Debug.Log()
        //return;
        if (__is_running == false)
            return;
        timer += Time.deltaTime;
        float driftScale = 0.05f;
        // Reassign random targets every 5 seconds
        if (timer >= 1f + UnityEngine.Random.Range(0.2f, 0.7f))
        {
            timer = 0f; // Reset timer
            

            Vector3 randomTravelTo = new Vector3(
                rootTravelTo.x + UnityEngine.Random.Range(-2, 2f)*driftScale,
                rootTravelTo.y + UnityEngine.Random.Range(-4f, 4f)*driftScale,
                rootTravelTo.z + UnityEngine.Random.Range(-1f, 1f)*driftScale
            );
            SetTravelTarget(randomTravelTo);
            SetLookAtTarget(rootLookAt);
        }

        UpdateDampeners();
        SmoothLookAt();
        SmoothTravel();
        UpdateMapMarker();
        UpdateLineRenderer();                
    }
    // Public accessors to set targets and reset dampeners
    public void SetRootLookAt(Vector3 newTargetLookAt,bool immediate = false)
    {
        //Debug.Log($"Setting new look at for {this.name}");
        rootLookAt = newTargetLookAt;
        if (immediate == true)
        {
            targetLookAt = rootLookAt;
            this.transform.LookAt(newTargetLookAt);
        }
    }
    // Public accessors to set targets and reset dampeners
    private void SetLookAtTarget(Vector3 newTargetLookAt)
    {
        targetLookAt = newTargetLookAt;
        if (lookAtDampener >= 0.9f)
            lookAtDampener = 0f; // Reset dampener
    }

    public void SetTravelTarget(Vector3 newTargetTravelTo)
    {
        targetTravelTo = newTargetTravelTo;
        //if (lookAtDampener >= 0.9f)
        //    travelDampener = 0f; // Reset dampener
    }

    private void UpdateDampeners()
    {
        // Gradually increase dampeners to 1
        lookAtDampener = Mathf.Clamp01(lookAtDampener + Time.deltaTime * dampenerSpeed);
        travelDampener = Mathf.Clamp01(travelDampener + Time.deltaTime * dampenerSpeed);
    }

    private void SmoothLookAt()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("SmoothLookAt has null targetTransform ");
            return;
        }

        // Calculate the target direction
        Vector3 direction = targetLookAt - targetTransform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            // Smoothly interpolate the rotation with dampening
            targetTransform.rotation = Quaternion.Lerp(
                targetTransform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed * lookAtDampener
            );
        }
    }
   

    private void SmoothTravel()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("SmoothTravel has null targetTransform ");
            return;
        }        
        Vector3 direction = targetTravelTo - targetTransform.position;
        float distance = direction.magnitude;

        direction.Normalize();

        // Determine the target speed based on distance
        float targetSpeed = maxTravelSpeed;

        if (distance < decelerationDistance)
        {
            targetSpeed = Mathf.Lerp(0, maxTravelSpeed, distance / decelerationDistance);
        }

        // Update velocity
        velocity = Vector3.MoveTowards(velocity, direction * targetSpeed, acceleration * Time.deltaTime);

        // Apply velocity
        targetTransform.position += velocity * Time.deltaTime;

    }    

    private void SetupPhysics()
    {
        // Add Rigidbody if not already attached
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Set Rigidbody to kinematic to prevent collision responses
        rb.useGravity = false; 
        rb.isKinematic = true; // Kinematic prevents physics-based collision responses

        // Add Sphere Collider for detection
        detectionCollider = GetComponent<SphereCollider>();
        if (detectionCollider == null)
        {
            detectionCollider = gameObject.AddComponent<SphereCollider>();
        }

        // Configure Sphere Collider as a trigger for detection
        detectionCollider.isTrigger = true;
        detectionCollider.radius = perceptionRadius; // Set radius to double the size of the ship
    }


    private void OnDrawGizmos()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("OnDrawGizmos has null targetTransform ");
            return;
        }           


    }

    private void UpdateMapMarker()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("UpdateMapMarker has null targetTransform ");
            return;
        }           
        mapHeight = -5f;
        if (mapMarker != null)
        {
            // Clamp MapMarker to ship's XZ coordinates with constant Y
            Vector3 markerPosition = new Vector3(targetTransform.position.x, mapHeight, targetTransform.position.z);
            mapMarker.position = markerPosition;
        }
    }

}
*/