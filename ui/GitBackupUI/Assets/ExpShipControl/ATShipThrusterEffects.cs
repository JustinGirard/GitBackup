using System.Net.NetworkInformation;
using Unity.VisualScripting;
using UnityEngine;
using SciFiShipController;
using System.Collections;
using System.Collections.Generic;


// 'ATShipControlEffects' is missing the class attribute 'ExtensionOfNativeClass'!
public  class ATShipControlEffects
{
    public static float TransDist(Transform shipTransform, Transform target)
    {
        // Calculate the distance between the ship's position and the target's position
        return Vector3.Distance(shipTransform.position, target.position);
    }    
    public static float AngDist( // RANGE: 
        Transform shipTransform,
        Transform source,
        Transform target
    )
    {
        // Calculate direction from source to target
        Vector3 toTarget = target.position - source.position;
        
        // Calculate the target rotation needed to aim at the target
        Quaternion targetRotation = Quaternion.LookRotation(toTarget, Vector3.up);
        
        // Calculate the angular difference between the current ship rotation and target rotation
        float angle = Quaternion.Angle(shipTransform.rotation, targetRotation);
        
        return angle;
    }


    private static IEnumerator AIControlSequence(SimpleShipController ship)
    {

        yield return new WaitForSeconds(0.5f); // Brief delay to ensure smooth transition

        // 2. Move the ship up for 2 seconds
        ShipInput shipInput = new ShipInput { vertical = 0.5f };  // Apply upward thrust
        for (float t = 0; t < 2f; t += Time.deltaTime)
        {
            Debug.Log($"Ship Input: [ Longitudinal: {shipInput.longitudinal:F2}, Vertical: {shipInput.vertical:F2}, Horizontal: {shipInput.horizontal:F2} ]");
            yield return new WaitForSeconds(0.5f); // Brief delay to ensure smooth transition
            ship.SendInput(shipInput);
            yield return null;
        }

        // Stop vertical thrust
        shipInput.vertical = 0f;
        ship.SendInput(shipInput);

        // 3. Rotate 90 degrees on Y-axis
        Quaternion targetRotation = ship.transform.rotation * Quaternion.Euler(0, 90, 0);
        while (Quaternion.Angle(ship.transform.rotation, targetRotation) > 0.1f)
        {
            ship.transform.rotation = Quaternion.RotateTowards(ship.transform.rotation, targetRotation, 45f * Time.deltaTime);
            yield return null;
        }

        // 4. Land (descend)
        shipInput.vertical = -1.0f;  // Apply downward thrust
        for (float t = 0; t < 2f; t += Time.deltaTime)
        {
            ship.SendInput(shipInput);
            yield return null;
        }
        shipInput.vertical = 0f;
        ship.SendInput(shipInput);

    }
    //public static bool __AdjustOrientationSlerpOsc = false; 
    public static IEnumerator AdjustOrientationSlerpOsc(
        Transform shipTransform,
        Transform source,
        Transform target,
        float slerpSpeed,
        float maxOscAmplitude,
        float oscFrequency,
        System.Func<bool> onComplete // Function to determine exit condition
    )
    {
        string semaName = shipTransform.gameObject.name+"_AdjustOrientationSlerpOsc";
        bool canRun = Sema.TryAcquireLock(semaName);
        if (!canRun)
            yield break;
        try
        {
            float oscTimer = 0f;  // Internal timer for oscillation
            bool isOscillating = true;  // Start in oscillation mode

            while (!onComplete())  // Loop until onComplete returns true
            {
                // === 1) Core Slerp to face the target ===
                Vector3 toTarget = target.position - source.position;
                Quaternion targetRotation = Quaternion.LookRotation(toTarget, Vector3.up);

                // === 2) Oscillation Decay and Calculation ===
                if (isOscillating)
                {
                    oscTimer += Time.deltaTime;

                    // Calculate the oscillation offset
                    float circleAngle = oscFrequency * oscTimer;
                    float amplitude = maxOscAmplitude * Mathf.Exp(-oscTimer);  // Exponential decay

                    // Calculate offset in pitch (X) and yaw (Y) for circular wobble
                    float offsetX = amplitude * Mathf.Sin(circleAngle);
                    float offsetY = amplitude * Mathf.Cos(circleAngle);

                    // Generate local rotation offset around forward axis
                    Quaternion offsetRotation = Quaternion.Euler(offsetX, offsetY, 0f);

                    // Combine offset with main rotation target
                    targetRotation = targetRotation * offsetRotation;
                }

                // === 3) Perform Slerp towards the computed rotation ===
                shipTransform.rotation = Quaternion.Slerp(
                    shipTransform.rotation,
                    targetRotation,
                    slerpSpeed * Time.deltaTime
                );
                yield return null;  // Wait until the next frame
            }
        }
        finally
        {
            Sema.ReleaseLock(semaName);
        }
    }    

    ////
    ///
    
    public static void DoAdjustVelocityTowardsTarget(
        Transform shipTransform,
        Rigidbody shipRigidbody,
        Vector3 powerScaler,
        Vector3 considerationWeight,
        Transform target,
        float dampingFactorDivide,
        float dampingFactorPower
    )
    {
                // Calculate direction and distance to the target
                Vector3 toTarget = target.position - shipTransform.position;
                float distanceToTarget = toTarget.magnitude;
                Vector3 targetDirection = toTarget.normalized;

                // Get current velocity
                Vector3 currentVelocity = shipRigidbody.velocity;

                // === Distance-Based Damping (Non-linear Deceleration) ===
                float dampingFactor = Mathf.Clamp01(distanceToTarget / 
                    (dampingFactorDivide + dampingFactorDivide * currentVelocity.sqrMagnitude));
                dampingFactor = Mathf.Pow(dampingFactor, dampingFactorPower);

                // Calculate desired velocity change
                Vector3 desiredVelocityChange = targetDirection * powerScaler.magnitude * dampingFactor;

                // Calculate velocity correction considering weights
                Vector3 velocityCorrection = desiredVelocityChange - currentVelocity;
                velocityCorrection = Vector3.Scale(velocityCorrection, considerationWeight);

                // Convert correction to local ship frame
                Vector3 localVelocityCorrection = shipTransform.InverseTransformDirection(velocityCorrection);

                // Generate thruster inputs
                ShipInput shipInput = new ShipInput
                {
                    longitudinal = Mathf.Clamp(localVelocityCorrection.z, -100f, 100f),  // Forward/backward thrust
                    vertical = Mathf.Clamp(localVelocityCorrection.y, -100f, 100f),       // Up/down thrust
                    horizontal = Mathf.Clamp(localVelocityCorrection.x, -100f, 100f)      // Left/right thrust
                };

                //Debug.Log($"Ship Input: [ Longitudinal: {shipInput.longitudinal:F2}, Vertical: {shipInput.vertical:F2}, Horizontal: {shipInput.horizontal:F2} ]");
                
                shipTransform.GetComponent<SimpleShipController>().SendInput(shipInput);
    }


    public static void DoSmoothAdjustVelocityTowardsTarget(
        Transform shipTransform,
        Rigidbody shipRigidbody,
        Vector3 powerScaler,
        Vector3 considerationWeight,
        Transform target,
        float dampingFactorDivide,
        float dampingFactorPower,
        ref Vector3 smoothVelocity,
        float smoothVelocityTime
    )
    {
        // Calculate direction and distance to the target

        Vector3 toTarget = target.position - shipTransform.position;
        toTarget = Vector3.SmoothDamp(Vector3.zero,toTarget, ref smoothVelocity, smoothVelocityTime);               
        float distanceToTarget = toTarget.magnitude;
        Vector3 targetDirection = toTarget.normalized;

        // Get current velocity
        Vector3 currentVelocity = shipRigidbody.velocity;

        // === Distance-Based Damping (Non-linear Deceleration) ===
        float dampingFactor = Mathf.Clamp01(distanceToTarget / 
            (dampingFactorDivide + dampingFactorDivide * currentVelocity.sqrMagnitude));
        dampingFactor = Mathf.Pow(dampingFactor, dampingFactorPower);

        // Calculate desired velocity change
        Vector3 desiredVelocityChange = targetDirection * powerScaler.magnitude * dampingFactor;

        // Calculate velocity correction considering weights
        Vector3 velocityCorrection = desiredVelocityChange - currentVelocity;
        velocityCorrection = Vector3.Scale(velocityCorrection, considerationWeight);

        // Convert correction to local ship frame
        Vector3 localVelocityCorrection = shipTransform.InverseTransformDirection(velocityCorrection);

        // Generate thruster inputs
        ShipInput shipInput = new ShipInput
        {
            longitudinal = Mathf.Clamp(localVelocityCorrection.z, -100f, 100f),  // Forward/backward thrust
            vertical = Mathf.Clamp(localVelocityCorrection.y, -100f, 100f),       // Up/down thrust
            horizontal = Mathf.Clamp(localVelocityCorrection.x, -100f, 100f)      // Left/right thrust
        };

        //Debug.Log($"Ship Input: [ Longitudinal: {shipInput.longitudinal:F2}, Vertical: {shipInput.vertical:F2}, Horizontal: {shipInput.horizontal:F2} ]");
        
        shipTransform.GetComponent<SimpleShipController>().SendInput(shipInput);
    }    
    public static IEnumerator AdjustVelocityTowardsTarget(
        Transform shipTransform,
        Rigidbody shipRigidbody,
        Vector3 powerScaler,
        Vector3 considerationWeight,
        Transform target,
        float dampingFactorDivide,
        float dampingFactorPower,
        System.Func<bool> onComplete // Exit condition
    )
    {
        bool canRun = Sema.TryAcquireLock(shipTransform.gameObject.name+"_AdjustVelocityTowardsTarget");
        if (!canRun)
            yield break; 
        //if (__AdjustVelocityTowardsTargetRunning)
        //    yield break; // Prevent duplicate coroutines
        //__AdjustVelocityTowardsTargetRunning = true;

        try
        {
            while (!onComplete())
            {
                DoAdjustVelocityTowardsTarget(
                    shipTransform,
                    shipRigidbody,
                    powerScaler,
                    considerationWeight,
                    target,
                    dampingFactorDivide,
                    dampingFactorPower);

                yield return null; // Wait for the next frame
            }
        }
        finally
        {
            Sema.ReleaseLock(shipTransform.gameObject.name+"_AdjustVelocityTowardsTarget");
            //__AdjustVelocityTowardsTargetRunning = false; // Ensure the flag resets after coroutine completion
        }
    }

}