using System.Net.NetworkInformation;
using Unity.VisualScripting;
using UnityEngine;
using SciFiShipController;
using System.Collections;
using System.Collections.Generic;


public class SimpleShipController : MonoBehaviour
{
    [Header("=== Orientation/Aim Controls ===")]
    [Tooltip("GameObject to aim at (rotation only) by the given time.")]
    public GameObject aimAtObject;         // Instead of directly having a Transform
    [Tooltip("Time remaining (seconds) to achieve aim target. If zero => constant/best effort.")]
    public float aimTime = 0f;            
    [Tooltip("Allowed rotational drift (in degrees, for example) before we apply thruster corrections.")]
    public float aimDrift = 1f;           

    [Header("=== Position/Move Controls ===")]
    [Tooltip("GameObject to move to (position only) by the given time.")]
    public GameObject positionObject;      // Instead of directly having a Transform
    [Tooltip("Time remaining (seconds) to achieve position target. If zero => constant/best effort.")]
    public float positionTime = 0f;        
    [Tooltip("Desired velocity at the target (could be zero for full stop). If time=0, interpret as best effort.")]
    public float positionTargetVelocity = 0f;  
    [Tooltip("Allowed positional drift (in meters, for example) before we apply thruster corrections.")]
    public float moveDrift = 0.1f;         

    // Example placeholder for an external thruster system (from Sci-Fi Ship Controller, etc.)
    // private SciFiShipThrusterSystem thrusterSystem;
    private PlayerInputModule playerInputModule;
    private ShipControlModule shipControlModule;
    private ShipInput shipInput;
    float modeTimer = 0f;
    bool isPositionMode = false;
    List<ShipInput> __inputStack = new List<ShipInput> ();

    public void SendInput(ShipInput input)
    {
        __inputStack.Add(input); 
    }
    public void ProcessInput()
    {
        ShipInput inputTotal = new ShipInput();
        foreach(ShipInput inputSlice in __inputStack)
        {
            inputTotal.longitudinal += inputSlice.longitudinal;
            inputTotal.vertical += inputSlice.vertical;
            inputTotal.horizontal += inputSlice.horizontal;
            inputTotal.pitch += inputSlice.pitch;
            inputTotal.yaw += inputSlice.yaw;
            inputTotal.roll += inputSlice.roll;
        }
        shipControlModule.SendInput(inputTotal);
        __inputStack.Clear();
    }
    public float __orientTime = 0f;
    public float __velocityTime = 0f;
    private void Update()
    {
        float angDist = ATShipControlEffects.AngDist(shipTransform:this.transform, source: transform, target: aimAtObject.transform);
        if (angDist > 5f)  
            StartCoroutine(ATShipControlEffects.AdjustOrientationSlerpOsc(
                shipTransform:this.transform,
                source: transform,
                target: aimAtObject.transform,
                slerpSpeed:5f,//2f,          // Slerp speed
                maxOscAmplitude:angDist/2,//20f,         // Max oscillation amplitude
                oscFrequency:5.2f,          // Oscillation frequency
                () => { 
                    __orientTime = __orientTime + Time.deltaTime;
                    if (__orientTime > 1)
                    {
                        __orientTime = 0f;
                        return true;
                    }
                    return false;
                }
            ));

        if (ATShipControlEffects.TransDist(shipTransform: this.transform, target: positionObject.transform) > 1f)
            StartCoroutine(ATShipControlEffects.AdjustVelocityTowardsTarget(
                shipTransform: this.transform,
                shipRigidbody: GetComponent<Rigidbody>(),  // Access Rigidbody for velocity
                powerScaler: new Vector3(30f, 30f, 30f),  // Uniform thrust scaling
                considerationWeight: new Vector3(1f, 1f, 1f),  // Emphasize all axes
                target: positionObject.transform,  // Target position
                dampingFactorDivide: 1f,
                dampingFactorPower: 1.0f,
                () => {
                    __velocityTime += Time.deltaTime;
                    if (__velocityTime > 5f)
                    {
                        __velocityTime = 0f;
                        return true;
                    }
                    return false;
                }
            ));

        ProcessInput();
    }
    /*
    public ShipInput AdjustVelocityTowardsTarget(Vector3 powerScaler, Vector3 considerationWeight, Transform target, float dampingFactorDivide, float dampingFactorPower)
    {

        // Calculate direction and distance to the target
        Vector3 toTarget = target.position - transform.position;
        float distanceToTarget = toTarget.magnitude;
        Vector3 targetDirection = toTarget.normalized;

        // Calculate the current velocity in world space
        Vector3 currentVelocity = GetComponent<Rigidbody>().velocity;
        
        // === Distance-Based Damping (Non-linear Deceleration) ===
        float dampingFactor = Mathf.Clamp01(distanceToTarget / (dampingFactorDivide+dampingFactorDivide*currentVelocity.sqrMagnitude));  // Smooth deceleration within 50 units
        dampingFactor = Mathf.Pow(dampingFactor, dampingFactorPower);  // Apply non-linear damping for smoother convergence

        // Calculate the desired velocity based on distance
        Vector3 desiredVelocityChange = targetDirection * powerScaler.magnitude * dampingFactor;

        // Calculate correction, considering existing velocity and weights
        Vector3 velocityCorrection = desiredVelocityChange - currentVelocity;
        velocityCorrection = Vector3.Scale(velocityCorrection, considerationWeight);

        // Convert correction to ship's local frame
        Vector3 localVelocityCorrection = transform.InverseTransformDirection(velocityCorrection);

        // Generate thrust inputs within limits
        ShipInput shipInput = new ShipInput
        {
            longitudinal = Mathf.Clamp(localVelocityCorrection.z, -100f, 100f),  // Forward/backward thrust
            vertical = Mathf.Clamp(localVelocityCorrection.y, -100f, 100f),       // Up/down thrust
            horizontal = Mathf.Clamp(localVelocityCorrection.x, -100f, 100f)      // Left/right thrust
        };
        Debug.Log($"Ship Input: [ Longitudinal: {shipInput.longitudinal:F2}, Vertical: {shipInput.vertical:F2}, Horizontal: {shipInput.horizontal:F2} ]");
        return shipInput;

    }
    
    public ShipInput AdjustOrientationTowardsTarget(Vector3 powerScaler, Vector3 considerationWeight, Transform source,Transform target, float dampingFactorDivide, float dampingFactorPower)
    {
        // Calculate target rotation to face the target while maintaining world up
        Vector3 toTarget = target.position - source.position;
        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        Quaternion currentRotation = transform.rotation;

        // Smoothly nudge ship's up towards world up
        Quaternion upConstraintRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
        targetRotation = Quaternion.Slerp(targetRotation, upConstraintRotation * targetRotation, 0.2f);  // 20% world up correction

        // === Interpolate Towards Target Rotation (Avoid Gimbal Lock) ===
        float distanceToTarget = toTarget.magnitude;
        float dampingFactor = Mathf.Clamp01(distanceToTarget / dampingFactorDivide);
        dampingFactor = Mathf.Pow(dampingFactor, dampingFactorPower);  // Non-linear smoothing

        // Incrementally rotate towards target using Slerp for stability
        Quaternion incrementalRotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * dampingFactor * powerScaler.magnitude);

        // Calculate rotation difference after applying incremental adjustment
        Quaternion deltaRotation = incrementalRotation * Quaternion.Inverse(currentRotation);

        // Convert deltaRotation to local angular velocity (no angle extraction needed)
        Vector3 angularCorrection = new Vector3(
            deltaRotation.x,
            deltaRotation.y,
            deltaRotation.z
        ) * 2f * Mathf.Rad2Deg;  // Quaternion to degrees

        angularCorrection = Vector3.Scale(angularCorrection, considerationWeight);

        // Generate thruster inputs based on angular correction
        ShipInput shipInput = new ShipInput
        {
            pitch = Mathf.Clamp(angularCorrection.x, -100f, 100f),
            yaw = Mathf.Clamp(angularCorrection.y, -100f, 100f),
            roll = Mathf.Clamp(angularCorrection.z, -100f, 100f)
        };

        Debug.Log($"Rotation Input: [ Pitch: {shipInput.pitch:F2}, Yaw: {shipInput.yaw:F2}, Roll: {shipInput.roll:F2} ]");
        return shipInput;
    }
    public ShipInput AdjustOrientationTowardsTargetSlerp(Transform source,Transform target, float slerpSpeed)
    {
        Vector3 toTarget = target.position - source.position;
        Quaternion targetRotation = Quaternion.LookRotation(toTarget, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, slerpSpeed * Time.deltaTime);

        return new ShipInput();  // Return zero input if fully locked to rotation
    }*/
    


    
    private void Start()
    {
        playerInputModule = GetComponent<PlayerInputModule>();
        shipControlModule = GetComponent<ShipControlModule>();
        playerInputModule.DisableInput();
    }
    
    /*
    public ShipInput AdjustOrientationSlerpOsc(
        Transform source,
        Transform target,
        float slerpSpeed,
        float maxOscDelay,     // total duration of oscillation
        float maxOscAmplitude, // how wide the oscillation is at t=0
        float oscFrequency     // how fast the nose circles
    )
    {
        // If not oscillating, you can start a new sequence
        // Or check a condition to "reset" the timer, etc.

        // === 1) Core Slerp to face the target ===
        Vector3 toTarget = target.position - source.position;
        Quaternion targetRotation = Quaternion.LookRotation(toTarget, Vector3.up);

        // === 2) Update the oscillation timer ===
        oscTimer += Time.deltaTime;
        if (oscTimer >= maxOscDelay)
        {
            // Once we pass the time limit, we clamp
            // and can mark as done (so no more offset).
            oscTimer = maxOscDelay;
            isOscillating = false;
        }

        // === 3) Calculate decay factor: from 1 (start) down to 0 (end) ===
        float remainingFraction = 1f - (oscTimer / maxOscDelay);

        // === 4) Compute a sinusoidal offset that gradually decays ===
        // We'll do a small circle around the forward axis
        float circleAngle = oscFrequency * oscTimer; // revolve at frequency
        float amplitude = maxOscAmplitude * remainingFraction; // decays to 0
        // We’ll define local up/down and side vector in local space:
        //   offset in local X and Y, no Z so it "circles" around forward
        float offsetX = amplitude * Mathf.Sin(circleAngle); 
        float offsetY = amplitude * Mathf.Cos(circleAngle);

        // === 5) Convert that local offset into a quaternion offset ===
        // We'll treat ship’s "forward" as the main axis, so the offset is around forward
        //  - local X = pitch axis
        //  - local Y = yaw axis
        // Actually, let's define an offset rotation about local pitch and yaw 
        // so it wobbles around forward.
        Quaternion offsetRotation = Quaternion.Euler(offsetX, offsetY, 0f);

        // Combine target rotation + offset:
        //   The offset is in local space relative to the "forward" direction,
        //   so we multiply offsetRotation * targetRotation (or vice versa).
        //   This yields a new "slightly off" target to produce a spiral approach.
        Quaternion wobblyTargetRotation = targetRotation * offsetRotation;

        // === 6) Slerp to the wobbly target ===
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            wobblyTargetRotation,
            slerpSpeed * Time.deltaTime
        );

        // === 7) Return zero ShipInput (since we directly set rotation) ===
        return new ShipInput();
    }*/


    public bool __debugMode = true;

    private void OnDrawGizmos()
    {
        if (!__debugMode) return;

        Vector3 targetPos = positionObject.transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetPos, 2.5f); // 0.5f radius for visibility

        // Use aimAtObject's forward direction as the "aim vector"
        Transform aimTarget = aimAtObject.transform;
        Vector3 directionToTarget = aimTarget.position - positionObject.transform.position;        
        Vector3 lineEnd = targetPos + directionToTarget * 10f;
        Gizmos.DrawLine(targetPos, lineEnd);

        aimTarget = aimAtObject.transform;
        directionToTarget = aimTarget.position - this.transform.position;        
        lineEnd = this.transform.position + directionToTarget * 10f;
        Gizmos.DrawLine(this.transform.position, lineEnd);


    }    
}
