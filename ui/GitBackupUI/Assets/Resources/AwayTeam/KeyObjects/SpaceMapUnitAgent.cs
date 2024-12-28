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
    /*    
            public class ProjectileEmitter
            {
                [SerializeField] 
                public GameObject gameObject;

                [SerializeField] 
                public string name;
            }
    */
    /*GameObject CreateCube(Vector3 position, Color debugColor)
    {
        // Create a cube
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Set position
        cube.transform.position = position;
        cube.transform.localScale = cube.transform.localScale*0.5f;
        // Add a material and color it yellow
        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = debugColor;
        return cube;
    }*/    
    public bool SetGoalPosition(Vector3 goalPosition, bool immediate = false)
    {
        //Debug.Break();
        rootTravelTo = goalPosition;
        if (immediate == true)
        {
            //if (targetTransform != null)
            targetTransform.position = goalPosition;
        }
        //GameObject cube = CreateCube(rootTravelTo,Color.green);
        //cube.transform.parent = this.gameObject.transform;   


        //Debug.Break();
        return true;

    }
    private void UpdateLineRenderer()
    {
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
            
            // Generate random positions for look at and travel
            Vector3 randomLookAt = new Vector3(
                rootLookAt.x + UnityEngine.Random.Range(-0.1f, 0.1f),
                rootLookAt.y + UnityEngine.Random.Range(-0.1f, 0.1f),
                rootLookAt.x + UnityEngine.Random.Range(-0.1f, 0.1f)
            );

            Vector3 randomTravelTo = new Vector3(
                rootTravelTo.x + UnityEngine.Random.Range(-2, 2f)*driftScale,
                rootTravelTo.y + UnityEngine.Random.Range(-4f, 4f)*driftScale,
                rootTravelTo.z + UnityEngine.Random.Range(-1f, 1f)*driftScale
            );
            //GameObject cube = CreateCube(rootTravelTo + new Vector3(0,-0.5f,0),Color.red);
            //cube.transform.parent = this.gameObject.transform;                
            // Set the new targets using accessor methods
            SetLookAtTarget(randomLookAt);
            SetTravelTarget(randomTravelTo);
        }

        UpdateDampeners();
        SmoothLookAt();
        SmoothTravel();
        UpdateMapMarker();
        UpdateLineRenderer();                
    }

    // Public accessors to set targets and reset dampeners
    public void SetLookAtTarget(Vector3 newTargetLookAt)
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
    /*
    private void SmoothTravel()
    {
        Vector3 direction = targetTravelTo - targetTransform.position;
        float distance = direction.magnitude;

        direction.Normalize();

        // Determine the target speed based on distance
        float targetSpeed = maxTravelSpeed;

        if (distance < decelerationDistance)
        {
            targetSpeed = Mathf.Lerp(0, maxTravelSpeed, distance / decelerationDistance);
        }
        velocity = targetSpeed*direction;

        // Apply velocity
        targetTransform.position += velocity * Time.deltaTime;
        //targetTransform.
        //Vector3 separation = CalculateSeparation();
        //Vector3 steeringForce = separation*4f;
        //steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);

        // Apply forces
        // Update velocity
        //velocity = Vector3.MoveTowards(velocity, direction * targetSpeed, acceleration * Time.deltaTime);
        //velocity = new Vector3(0f,0f,0f);
        //velocity +=   steeringForce * Time.deltaTime;
        //targetTransform.position += velocity * Time.deltaTime;


    }    */

    private void SmoothTravel()
    {
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

/*
    private void SetupPhysics()
    {
        // Add Rigidbody if not already attached
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false; // Disable gravity
        rb.isKinematic = false; // Allow physics interaction

        // Add Sphere Collider for detection
        detectionCollider = GetComponent<SphereCollider>();
        if (detectionCollider == null)
        {
            detectionCollider = gameObject.AddComponent<SphereCollider>();
        }
        detectionCollider.isTrigger = true; // Set as trigger for detection
        detectionCollider.radius = perceptionRadius; // Set radius to double the size of the ship
    }    */
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
        // Draw gizmos to visualize target positions
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetLookAt, 0.5f); // LookAt target
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(targetTravelTo, 0.5f); // TravelTo target
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(rootTravelTo, 0.5f); // TravelTo target

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, targetTravelTo);

    }

    private void UpdateMapMarker()
    {
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceMapUnitAgent : MonoBehaviour
{
    // Private target variables
    private Vector3 targetLookAt = new Vector3(0, 0, 10);
    private Vector3 targetTravelTo = new Vector3(10, 0, 10);

    // Public speed and smoothness settings
    public float rotationSpeed = 1.0f;
    public float travelSpeed = 0.5f;
    public float maxForce = 10.0f; // Maximum steering force
    public float perceptionRadius = 10.0f; // Detection range for other ships
    public float maxSpeed = 5.0f; // Maximum speed

    private Rigidbody rb; // Rigidbody for physics-based movement
    private SphereCollider detectionCollider; // Sphere collider for neighbor detection
    private Vector3 velocity;

    void Start()
    {
        // Initialize components
        SetupPhysics();
    }

    void Update()
    {
        // Calculate movement forces
        Vector3 separation = CalculateSeparation();
        Vector3 seek = Seek(targetTravelTo);

        // Combine forces
        Vector3 force = separation + seek;
        force = Vector3.ClampMagnitude(force, maxForce);

        // Apply force to Rigidbody
        rb.AddForce(force);

        // Rotate to face movement direction
        if (rb.velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(rb.velocity.normalized);
            rb.rotation = Quaternion.Lerp(rb.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private void SetupPhysics()
    {
        // Add Rigidbody if not already attached
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false; // Disable gravity
        rb.isKinematic = false; // Allow physics interaction

        // Add Sphere Collider for detection
        detectionCollider = GetComponent<SphereCollider>();
        if (detectionCollider == null)
        {
            detectionCollider = gameObject.AddComponent<SphereCollider>();
        }
        detectionCollider.isTrigger = true; // Set as trigger for detection
        detectionCollider.radius = perceptionRadius; // Set radius to double the size of the ship
    }

    private Vector3 Seek(Vector3 target)
    {
        // Calculate desired velocity to the target
        Vector3 desired = target - transform.position;
        desired = desired.normalized * maxSpeed;

        // Calculate steering force
        return desired - rb.velocity;
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 separationForce = Vector3.zero;
        Collider[] neighbors = Physics.OverlapSphere(transform.position, perceptionRadius);

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject && neighbor.CompareTag("Vehicle"))
            {
                // Calculate direction away from the neighbor
                Vector3 difference = transform.position - neighbor.transform.position;
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
}
*/