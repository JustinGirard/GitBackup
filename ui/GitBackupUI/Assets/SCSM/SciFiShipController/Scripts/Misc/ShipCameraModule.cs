﻿using UnityEngine;
using UnityEngine.Events;
using System.Collections;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    [AddComponentMenu("Sci-Fi Ship Controller/Misc/Ship Camera Module")]
    [HelpURL("http://scsmmedia.com/ssc-documentation")]
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(Rigidbody))]
    public class ShipCameraModule : MonoBehaviour
    {
        #region Public Enumerations

        public enum CameraUpdateType
        {
            /// <summary>
            /// The update occurs during FixedUpdate. Recommended for rigidbodies with Interpolation set to None.
            /// </summary>
            FixedUpdate = 0,
            /// <summary>
            /// The update occurs during LateUpdate. Recommended for rigidbodies with Interpolation set to Interpolate.
            /// </summary>
            LateUpdate = 1,
            /// <summary>
            /// When the update occurs is automatically determined.
            /// </summary>
            Automatic = 2
        }

        public enum CameraRotationMode
        {
            /// <summary>
            /// The camera rotates to face in the direction the ship is moving in.
            /// </summary>
            FollowVelocity = 0,
            /// <summary>
            /// The camera rotates to face the direction the ship is facing in.
            /// </summary>
            FollowTargetRotation = 1,
            /// <summary>
            /// The camera rotates to face towards the ship itself.
            /// </summary>
            AimAtTarget = 2,
            /// <summary>
            /// The camera faces downwards and rotates so that the top of the screen is in the direction
            /// the ship is moving in.
            /// </summary>
            TopDownFollowVelocity = 5,
            /// <summary>
            /// The camera faces downwards and rotates so that the top of the screen is in the direction
            /// the ship is facing in.
            /// </summary>
            TopDownFollowTargetRotation = 6,
            /// <summary>
            /// The camera rotation is fixed.
            /// </summary>
            Fixed = 20
        }

        public enum TargetOffsetCoordinates
        {
            /// <summary>
            /// The target offset is relative to the rotation of the camera.
            /// </summary>
            CameraRotation = 0,
            /// <summary>
            /// The target offset is relative to the rotation of the target.
            /// </summary>
            TargetRotation = 1,
            /// <summary>
            /// The target offset is relative to the flat rotation of the target.
            /// </summary>
            TargetRotationFlat = 2,
            /// <summary>
            /// The target offset is relative to the world coordinate system.
            /// </summary>
            World = 10
        }

        #endregion

        #region Public Variables - General

        /// <summary>
        /// Start the camera rendering when it is initialised.
        /// See also StartCamera() and StopCamera()
        /// </summary>
        public bool startOnInitialise = true;

        /// <summary>
        /// Enable camera movement when it is initialised and startOnInitialise is true.
        /// See also EnableCamera() and DisableCamera()
        /// </summary>
        public bool enableOnInitialise = true;

        /// <summary>
        /// The target ship for this camera to follow. At runtime call GetTarget() or SetTarget(newTarget)
        /// </summary>
        public ShipControlModule target;

        /// <summary>
        /// The offset from the target (in local space) for the camera to aim for.
        /// </summary>
        public Vector3 targetOffset = Vector3.zero;

        /// <summary>
        /// The coordinate system used to interpret the target offset.
        /// CameraRotation: The target offset is relative to the rotation of the camera.
        /// TargetRotation: The target offset is relative to the rotation of the target.
        /// TargetRotationFlat: The target offset is relative to the flat rotation of the target.
        /// World: The target offset is relative to the world coordinate system.
        /// </summary>
        public TargetOffsetCoordinates targetOffsetCoordinates = TargetOffsetCoordinates.CameraRotation; 

        /// <summary>
        /// If enabled, the camera will stay locked to the optimal camera position.
        /// </summary>
        public bool lockToTargetPosition = false;
        /// <summary>
        /// How quickly the camera moves towards the optimal camera position. Only relevant when lockToTargetPosition is disabled.
        /// </summary>
        [Range(1f, 100f)] public float moveSpeed = 15f;

        /// <summary>
        /// Damp or modify the target position offset based upon the ship pitch and yaw inputs
        /// </summary>
        public bool targetOffsetDamping = false;
        /// <summary>
        /// The rate at which Target Offset Y is modified by ship pitch input. Higher values are more responsive.
        /// </summary>
        [Range(0.01f, 1f)] public float dampingPitchRate = 0.25f;
        /// <summary>
        /// The rate at which the Target Offset Y returns to normal when there is no ship pitch input. Higher values are more responsive.
        /// </summary>
        [Range(0.01f, 1f)] public float dampingPitchGravity = 0.25f;
        /// <summary>
        /// The rate at which Target Offset X is modified by ship yaw input. Higher values are more responsive.
        /// </summary>
        [Range(0.01f, 1f)] public float dampingYawRate = 0.25f;
        /// <summary>
        /// The rate at which the Target Offset X returns to normal when there is no ship yaw input. Higher values are more responsive.
        /// </summary>
        [Range(0.01f, 1f)] public float dampingYawGravity = 0.25f;
        /// <summary>
        /// The damping maximum pitch Target Offset Up (y-axis)
        /// </summary>
        public float dampingMaxPitchOffsetUp = 2f;
        /// <summary>
        /// The damping maximum pitch Target Offset Down (y-axis)
        /// </summary>
        public float dampingMaxPitchOffsetDown = -2f;
        /// <summary>
        /// The damping maximum yaw Target Offset right (x-axis)
        /// </summary>
        public float dampingMaxYawOffsetRight = 2f;
        /// <summary>
        /// The damping maximum yaw Target Offset left (x-axis)
        /// </summary>
        public float dampingMaxYawOffsetLeft = -2f;

        /// <summary>
        /// If enabled, the camera will stay locked to the optimal camera rotation.
        /// </summary>
        public bool lockToTargetRotation = false;
        /// <summary>
        /// How quickly the camera turns towards the optimal camera rotation. Only relevant when lockToTargetRotation is disabled.
        /// </summary>
        [Range(1f, 100f)] public float turnSpeed = 15f;
        /// <summary>
        /// When cameraRotationMode is Aim At Target, enabling this will enable the camera to track the target
        /// without moving in the scene.
        /// </summary>
        public bool lockCameraPosition = false;
        /// <summary>
        /// How the camera rotation is determined.
        /// FollowVelocity: The camera rotates to face in the direction the ship is moving in.
        /// FollowTargetRotation: The camera rotates to face the direction the ship is facing in.
        /// AimAtTarget: The camera rotates to face towards the ship itself.
        /// </summary>
        public CameraRotationMode cameraRotationMode = CameraRotationMode.FollowTargetRotation;
        /// <summary>
        /// Below this velocity (in metres per second) the forwards direction of the target will be followed instead of the velocity.
        /// Only relevant when cameraRotationMode is set to FollowVelocity or TopDownFollowVelocity.
        /// </summary>
        public float followVelocityThreshold = 10f;
        /// <summary>
        /// If enabled, the camera will orient with respect to the world up direction rather than the target's up direction.
        /// </summary>
        public bool orientUpwards = false;
        /// <summary>
        /// The rotation of the camera. Only relevant when cameraRotationMode is set to Fixed.
        /// </summary>
        public Vector3 cameraFixedRotation = Vector3.zero;

        /// <summary>
        /// When the camera position/rotation is updated.
        /// FixedUpdate: The update occurs during FixedUpdate. Recommended for rigidbodies with Interpolation set to None.
        /// LateUpdate: The update occurs during LateUpdate. Recommended for rigidbodies with Interpolation set to Interpolate.
        /// Automatic: When the update occurs is automatically determined.
        /// </summary>
        public CameraUpdateType updateType = CameraUpdateType.Automatic;

        /// <summary>
        /// The maximum strength of the camera shake. Smaller numbers are better.
        /// This can be overridden by calling ShakeCamera(duration,strength)
        /// If modifying at runtime, you must call ReinitialiseTargetVariables().
        /// </summary>
        [Range(0f,0.5f)] public float maxShakeStrength = 0f;

        /// <summary>
        /// The maximum duration (in seconds) the camera will shake per incident.
        /// This can be overridden by calling ShakeCamera(duration,strength).
        /// If modifying at runtime, you must call ReinitialiseTargetVariables().
        /// </summary>
        [Range(0.1f, 5f)] public float maxShakeDuration = 0.2f;

        /// <summary>
        /// [INTERNAL ONLY]
        /// </summary>
        [HideInInspector] public bool allowRepaint = false;

        #endregion

        #region Public Variables - Object Clipping

        /// <summary>
        /// Adjust the camera position to attempt to avoid the camera flying through objects between the ship and the camera.
        /// </summary>
        public bool clipObjects = false;

        /// <summary>
        /// The minimum speed the camera will move to avoid flying through objects between the ship and the camera.
        /// High values make clipping more effective. Lower values will make it smoother.
        /// Currently this has no effect if Lock to Target Position is enabled.
        /// </summary>
        [Range(1f, 100f)] public float minClipMoveSpeed = 10f;

        /// <summary>
        /// When clipObjects is true, the minimum distance the camera can be from the Ship (target) position.
        /// Typically this is the spheric radius of the ship. If the ship has colliders that do not overlay the
        /// target position, this value should be set, else set to 0 to improve performance.
        /// </summary>
        [Range(0f,1000f)] public float clipMinDistance = 0f;

        /// <summary>
        /// The minimum offset on the x-axis, in metres, the camera can be from the Ship (target) when object clipping. This should be less than or equal to the Target Offset X value. 
        /// </summary>
        [Range(0f, 50f)] public float clipMinOffsetX = 0f;

        /// <summary>
        /// The minimum offset on the y-axis, in metres, the camera can be from the Ship (target) when object clipping. This should be less than or equal to the Target Offset Y value.
        /// </summary>
        [Range(0f, 50)] public float clipMinOffsetY = 0f;

        /// <summary>
        /// Clip objects in the selected Unity Layers.
        /// Start with Nothing (0) and call ResetClipObjectSettings()
        /// </summary>
        public LayerMask clipObjectMask = 0;

        #endregion

        #region Public Variables - Orbit

        /// <summary>
        /// The distance from the target ship transform position to the ship camera position
        /// </summary>
        [Range(2f, 2000f)] public float orbitDistance = 25f;

        /// <summary>
        /// The amount of damping applied when starting or stopping camera horizontal orbit
        /// </summary>
        [Range(0f, 1f)] public float orbitHorizDamping = 0.1f;

        /// <summary>
        /// The amount of damping applied when starting or stopping camera vertical orbit
        /// </summary>
        [Range(0f, 1f)] public float orbitVertDamping = 0.1f;

        /// <summary>
        /// The speed or rate at which the camera orbits round the ship
        /// </summary>
        [Range(0.001f, 1f)] public float orbitSpeed = 1f;

        /// <summary>
        /// Ground detection in the selected Unity Layers.
        /// </summary>
        public LayerMask orbitGroundMask = DefaultOrbitGroundMask;

        /// <summary>
        /// The local space offset from the target ship position to begin detecting the ground.
        /// Typically, you'll want to use a negative Y value for ships with multiple colliders.
        /// </summary>
        public Vector3 orbitGroundDetectOffset = Vector3.zero;

        /// <summary>
        /// Automatically restrict the vertical orbit if there are objects immediately beneath the ship.
        /// Currently this assumes the ground or collider under the ship is flat and can be detected
        /// directly under the position of the ship.
        /// </summary>
        public bool isOrbitGroundDetection = false;

        /// <summary>
        /// These methods get called when the orbit feature is turned off
        /// </summary>
        public UnityEvent onOrbitDisable = null;

        /// <summary>
        /// These methods get called when the orbit feature is turned on
        /// </summary>
        public UnityEvent onOrbitEnable = null;

        #endregion

        #region Public Variables - Zoom

        /// <summary>
        /// The time, in seconds, to zoom fully in or out
        /// </summary>
        [Range(0.1f, 20f)] public float zoomDuration = 3f;

        /// <summary>
        /// The delay, in seconds, before zoom starts to return to the non-zoomed position
        /// </summary>
        [Range(0f, 3600f)] public float unzoomDelay = 0f;

        /// <summary>
        /// The camera field-of-view when no zoom is applied
        /// </summary>
        [Range(20f, 85f)] public float unzoomedFoV = 60f;

        /// <summary>
        /// The camera field-of-view when the camera is fully zoomed in.
        /// </summary>
        [Range(1f, 50f)] public float zoomedInFoV = 10f;

        /// <summary>
        /// The camera field-of-view when the camera is fully zoomed out.
        /// </summary>
        [Range(40f, 150f)] public float zoomedOutFoV = 90f;

        /// <summary>
        /// The amount of damping applied when starting or stopping camera zoom
        /// </summary>
        [Range(0f, 1f)] public float zoomDamping = 0.1f;

        #endregion

        #region Public Member Properties

        /// <summary>
        /// Is the camera being moved using FixedUpdate()?
        /// </summary>
        public bool IsCameraInFixedUpdate { get; private set; }

        /// <summary>
        /// Is camera started and rendering?
        /// </summary>
        public bool IsCameraStarted { get { return camera1 == null ? false : camera1.enabled; } }

        /// <summary>
        /// Is the camera enabled for movement?
        /// </summary>
        public bool IsCameraEnabled { get { return cameraIsEnabled; } }

        /// <summary>
        /// Is zoom currently enabled?
        /// </summary>
        public bool IsZoomEnabled { get { return isZoomEnabled; } }

        /// <summary>
        /// Get the camera being used by this module
        /// </summary>
        public Camera GetCamera1 { get { return camera1; } }

        #endregion

        #region Public Static Properties

        /// <summary>
        /// Everything, except TransparentFX (1), IgnoreRaycast (2),  UI (5)
        /// </summary>
        public static LayerMask DefaultClipObjectMask { get { return ~((1 << 1) | (1 << 2) | (1 << 5)); } }

        /// <summary>
        /// Everything, except TransparentFX (1), IgnoreRaycast (2),  UI (5)
        /// </summary>
        public static LayerMask DefaultOrbitGroundMask { get { return ~((1 << 1) | (1 << 2) | (1 << 5)); } }

        #endregion

        #region Private Variables

        private Rigidbody rBody;

        private Camera camera1;

        private int sceneHandle = 0;

        private Transform targetTrfm;
        private Rigidbody targetRBody;
        private Vector3 currentPosition;

        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private Vector3 targetTrfmUp;
        private Vector3 targetTrfmFwd;

        private Vector3 targetOffsetDamped = Vector3.zero;
        private float dampingOffsetAdjPitch = 0f, dampingOffsetAdjYaw = 0f;

        private Vector3 optimalCameraPosition;
        private Vector3 previousOptimalCameraPosition = Vector3.zero;
        private Quaternion optimalCameraRotation;
        private Vector3 optimalCameraForward;
        private Vector3 optimalCameraUp;

        //private Vector3 currentMoveDampVelocity = Vector3.zero;
        private Vector3 currentTurnDampVelocity = Vector3.zero;
        private Vector3 optimalCameraRotationEulerAngles;
        private Vector3 currentCameraRotationEulerAngles;

        private bool cameraIsEnabled = true;

        // Camera shake variables - See MoveCamera() and ShakeCamera(..).
        private bool isShaking = false;
        private float shakeStrength = 1f;
        private float shakeTimer = 0f;

        // When initialised, was there an audioListener component present and enabled?
        // This may not be the case when it is a point-of-interest camera in the scene,
        // like in TechDemo2.
        private bool isAudioListenerConfigured = false;
        private AudioListener audioListener = null;

        // Object clipping
        private Vector3 viewHalfExtents = Vector3.zero;

        // Orbit
        private float currentOrbitHorizInput = 0f;
        private float previousOrbitHorizInput = 0f;
        private float currentOrbitVertInput = 0f;
        private float previousOrbitVertInput = 0f;
        private Vector2 orbitAmount = Vector2.zero;
        /// Orbit Pitch angle in degrees (up +ve, down -ve). Range -360 to +360.
        private float currentOrbitPitchAngle = 0f;
        /// <summary>
        /// Orbit Yaw angle in degrees (right +ve, left -ve). Range -360 to +360.
        /// </summary>
        private float currentOrbitYawAngle = 0f;

        /// <summary>
        /// Used when auto return to centre (not implemented yet)
        /// </summary>
        private bool isOrbitHorizInputProcessed = false;
        /// <summary>
        /// Used when auto return to centre (not implemented yet)
        /// </summary>
        private bool isOrbitVertInputProcessed = false;

        [SerializeField] private bool isOrbitEnabled = false;

        // Zoom
        private float currentZoomInput = 0f;
        private float previousZoomInput = 0f;
        private float zoomFactor = 0f;
        private float unzoomTimer = 0f;
        private bool isZoomInputProcessed = false;

        [SerializeField] private bool isZoomEnabled = false;

        // Celestials (stars) synch
        [System.NonSerialized] private Celestials celestials = null;

        /// <summary>
        /// Sometimes the stars lag the camera. This may be required,
        /// to force the stars to update after the camera has rotated.
        /// Do NOT enable, if you don't have the Celestials component in the scene
        /// as it will incur unnecessary performance overhead.
        /// </summary>
        [SerializeField] private bool isOverrideSyncStars = false;

        #endregion

        #region Private Variables - Editor

        [SerializeField] private int selectedTabInt = 0;

        #endregion

        #region Awake Method

        // Use this for initialization
        void Awake()
        {
            sceneHandle = gameObject.scene.handle;

            // Get the rigidbody attached to the camera
            // Caters for the situation when script was already attached, but without a rigidbody
            if (!TryGetComponent(out rBody))
            {
                rBody = gameObject.AddComponent<Rigidbody>();
                #if UNITY_EDITOR
                Debug.Log("ShipCameraModule - Adding missing Rigidbody to " + name);
                #endif
            }

            // Set up the camera rigidbody
            rBody.mass = 1f;
            rBody.isKinematic = true;
            rBody.interpolation = RigidbodyInterpolation.None;

            // Initialise target variables
            if (target != null) { ReinitialiseTargetVariables(); }

            isShaking = false;

            audioListener = GetComponent<AudioListener>();
            isAudioListenerConfigured = audioListener != null && audioListener.enabled;

            ReinitialiseCameraSettings();

            if (startOnInitialise)
            {
                StartCamera();
                if (!enableOnInitialise) { DisableCamera(); }
            }
            else { StopCamera(); }

            // Keep compiler happy
            if (selectedTabInt < 0) { }

            previousOptimalCameraPosition = transform.position;
        }

        #endregion

        #region Update Methods

        // FixedUpdate is called once per physics update
        void FixedUpdate ()
        {
            // Move camera in FixedUpdate if either:
            // - We are in FixedUpdate mode
            // - We are in Automatic mode and the target rigidbody has interpolation disabled
            if (cameraIsEnabled && target != null && targetRBody != null && (updateType == CameraUpdateType.FixedUpdate || (updateType == CameraUpdateType.Automatic &&
                targetRBody.interpolation == RigidbodyInterpolation.None)))
            {
                IsCameraInFixedUpdate = true;
                MoveCameraInternal(false);
            }
        }

        // LateUpdate is called once per frame, after all update functions have been called
        void LateUpdate ()
        {
            // Move camera in LateUpdate if either:
            // - We are in LateUpdate mode
            // - We are in Automatic mode and the target rigidbody has interpolation enabled
            if (cameraIsEnabled && target != null && targetRBody != null && (updateType == CameraUpdateType.LateUpdate || (updateType == CameraUpdateType.Automatic &&
                targetRBody.interpolation != RigidbodyInterpolation.None)))
            {
                IsCameraInFixedUpdate = false;
                MoveCameraInternal(false);
            }

            if (cameraIsEnabled && isZoomEnabled)
            {
                CalcZoomFactor();
                ZoomCamera();
            }
        }

        #endregion

        #region Private and Internal Non-Static Methods

        /// <summary>
        /// Calculate orbit amoutn based on the orbit input.
        /// TODO - auto recentre value with some kind of delay timer
        /// SEE CalcZoomFactor()
        /// </summary>
        private void CalcOrbitAmount()
        {
            //Debug.Log("[DEBUG] orbitVertIn: " + currentOrbitVertInput.ToString("0.00") + " T:" + Time.time);

            // If there is no input reset the previous input.
            if (currentOrbitVertInput == 0f)
            {
                previousOrbitVertInput = 0f;
                isOrbitVertInputProcessed = true;
            }
            else if (!isOrbitVertInputProcessed)
            {
                orbitAmount.y += currentOrbitVertInput * Time.deltaTime * orbitSpeed / 2f;

                // Wrap values around
                if (orbitAmount.y > 1f)
                {
                    orbitAmount.y = 1f;
                    //orbitAmount.x = -orbitAmount.x;
                }
                else if (orbitAmount.y < -1f)
                {
                    orbitAmount.y = -1f;
                    //orbitAmount.x = -orbitAmount.x;
                }

                // Reset after use
                isOrbitVertInputProcessed = true;
                currentOrbitVertInput = 0f;
            }

            if (currentOrbitHorizInput == 0f)
            {
                previousOrbitHorizInput = 0f;
                isOrbitHorizInputProcessed = true;
            }
            else if (!isOrbitHorizInputProcessed)
            {
                orbitAmount.x += currentOrbitHorizInput * Time.deltaTime * orbitSpeed;

                // Wrap values around
                if (orbitAmount.x > 1f) { orbitAmount.x = -1f; }
                else if (orbitAmount.x < -1f) { orbitAmount.x = 1f; }

                // Reset after use
                isOrbitHorizInputProcessed = true;
                currentOrbitHorizInput = 0f;
            }
        }

        /// <summary>
        /// Calculate the zoom factor based on zoom input and any unzoom delay.
        /// </summary>
        private void CalcZoomFactor()
        {
            // Is the user (or program) attempting to manually zoom in or out
            if (currentZoomInput != 0f)
            {
                zoomFactor += currentZoomInput * Time.deltaTime / zoomDuration;

                if (zoomFactor > 1f) { zoomFactor = 1f; }
                else if (zoomFactor < -1f) { zoomFactor = -1f; }

                unzoomTimer = unzoomDelay;

                // Reset zoom input after use
                isZoomInputProcessed = true;
                currentZoomInput = 0f;
            }
            else if (unzoomTimer <= 0f)
            {
                previousZoomInput = 0f;

                // +ve zoom, start reducing zoom towards no zoom
                if (zoomFactor > 0f)
                {
                    // If it has almost returned to 0, stop zooming
                    if (zoomFactor < Vector3.kEpsilon) { zoomFactor = 0f; }
                    else
                    {
                        zoomFactor -= Time.deltaTime / zoomDuration;
                        if (zoomFactor < 0f) { zoomFactor = 0f; }
                    }
                }
                // -ve zoom, start increasing zoom towards no zoom
                else
                {
                    // If it has almost returned to 0, stop zooming
                    if (-zoomFactor < Vector3.kEpsilon) { zoomFactor = 0f; }
                    else
                    {
                        zoomFactor += Time.deltaTime / zoomDuration;
                        if (zoomFactor > 0f) { zoomFactor = 0f; }
                    }
                }
            }
            else
            {
                unzoomTimer -= Time.deltaTime;
                if (unzoomTimer < 0f) { unzoomTimer = 0f; }
            }
        }

        /// <summary>
        /// Enable or Disable the orbit feature
        /// TODO - smooth enable/disable orbit camera
        /// </summary>
        /// <param name="isEnabled"></param>
        private void EnableOrDisableOrbit(bool isEnabled)
        {
            // Check if it needs to be changed
            if (isEnabled != isOrbitEnabled)
            {
                currentOrbitHorizInput = 0f;
                currentOrbitVertInput = 0f;
                previousOrbitHorizInput = 0f;
                previousOrbitVertInput = 0f;

                if (isEnabled)
                {
                    SetOrbitAmount(Vector2.zero);
                }
                else
                {
                    SetOrbitAmount(Vector2.zero);
                    OrbitCamera(ref optimalCameraPosition, ref optimalCameraRotation);
                }

                isOrbitEnabled = isEnabled;

                if (isOrbitEnabled) { if (onOrbitEnable != null) { onOrbitEnable.Invoke(); } }
                else if (onOrbitDisable != null) { onOrbitDisable.Invoke(); }
            }
        }

        /// <summary>
        /// Enable or Disable the zoom feature
        /// </summary>
        /// <param name="isEnabled"></param>
        private void EnableOrDisableZoom(bool isEnabled)
        {
            if (isEnabled)
            {
                SetZoomAmount(0f);
                currentZoomInput = 0f;
                previousZoomInput = 0f;
            }
            else
            {
                SetZoomAmount(0f);
                ZoomCamera();
            }

            isZoomEnabled = isEnabled;
        }

        /// <summary>
        /// Move and rotate the camera. Optionally snap the camera immediately to
        /// position and rotation.
        /// </summary>
        /// <param name="isSnapCamera"></param>
        private void MoveCameraInternal (bool isSnapCamera)
        {
            #region Initialisation

            targetPosition = targetTrfm.position;
            targetRotation = targetTrfm.rotation;

            // Get current position
            currentPosition = transform.position;

            // Get target transform up and forwards directions
            targetTrfmUp = targetRotation * Vector3.up;
            targetTrfmFwd = targetRotation * Vector3.forward;

            // By default this always off. Is only available below
            // when CameraRotationMode is AimAtTarget
            bool isLockCameraPosition = false;

            // Is camera still shaking?
            if (isShaking)
            {
                shakeTimer -= Time.deltaTime;
                if (shakeTimer <= 0f) { StopCameraShake(); }
            }

            #endregion

            if (isOrbitEnabled)
            {
                CalcOrbitAmount();
                OrbitCamera(ref optimalCameraPosition, ref optimalCameraRotation);
            }
            else
            {
                #region Find Optimal Camera Rotation

                // In late update mode, use the interpolated world velocity of the rigidbody instead of the normal world velocity
                Vector3 shipWorldVelocity = Vector3.zero;
                if (cameraRotationMode == CameraRotationMode.FollowVelocity || cameraRotationMode == CameraRotationMode.TopDownFollowVelocity)
                {
                    if (IsCameraInFixedUpdate)
                    {
                        shipWorldVelocity = target.shipInstance.WorldVelocity;
                    }
                    else
                    {
                        shipWorldVelocity = target.shipInstance.GetInterpolatedWorldVelocity();
                    }
                }

                // Find the optimal camera up direction
                if (cameraRotationMode == CameraRotationMode.TopDownFollowVelocity)
                {
                    // When above a given velocity, this is the ship's flattened velocity vector
                    if (shipWorldVelocity.sqrMagnitude > followVelocityThreshold * followVelocityThreshold)
                    {
                        optimalCameraUp = shipWorldVelocity;
                        optimalCameraUp.y = 0f;
                        optimalCameraUp.Normalize();
                    }
                    // When below that velocity, this is the ship's flattened forward vector
                    else
                    {
                        optimalCameraUp = targetTrfmFwd;
                        optimalCameraUp.y = 0f;
                        optimalCameraUp.Normalize();
                    }
                }
                else if (cameraRotationMode == CameraRotationMode.TopDownFollowTargetRotation)
                {
                    // If we are using the top down follow target rotation mode, this is the ship's flattened forward vector
                    optimalCameraUp = targetTrfmFwd;
                    optimalCameraUp.y = 0f;
                    optimalCameraUp.Normalize();
                }
                else if (cameraRotationMode == CameraRotationMode.Fixed)
                {
                    // If we are using the fixed mode, this is the camera rotation's up vector
                    optimalCameraUp = Quaternion.Euler(cameraFixedRotation) * Vector3.up;
                }
                else
                {
                    // If we are orienting upwards, this is the world up direction
                    if (orientUpwards) { optimalCameraUp = Vector3.up; }
                    // Otherwise this is the target up direction
                    else { optimalCameraUp = targetTrfmUp; }
                }

                // Find the optimal camera forwards direction
                if (cameraRotationMode == CameraRotationMode.AimAtTarget)
                {
                    // Aim at target mode: Forwards is towards the target
                    optimalCameraForward = (targetPosition - currentPosition).normalized;
                    isLockCameraPosition = lockCameraPosition;
                }
                else if (cameraRotationMode == CameraRotationMode.FollowVelocity)
                {
                    // Follow velocity mode: When above a given velocity, forwards is in direction of target velocity
                    if (shipWorldVelocity.sqrMagnitude > followVelocityThreshold * followVelocityThreshold)
                    {
                        optimalCameraForward = shipWorldVelocity.normalized;
                    }
                    // When below that velocity, forwards is in direction of target forwards direction
                    else
                    {
                        optimalCameraForward = targetTrfmFwd;
                    }
                }
                else if (cameraRotationMode == CameraRotationMode.FollowTargetRotation)
                {
                    // Follow target rotation mode: Forwards is in direction of target forwards direction
                    optimalCameraForward = targetTrfmFwd;
                }
                else if (cameraRotationMode == CameraRotationMode.Fixed)
                {
                    // Fixed mode: Forwards is camera rotation's forwards vector
                    optimalCameraForward = Quaternion.Euler(cameraFixedRotation) * Vector3.forward;
                }
                else
                {
                    // Top down modes: Forwards is world down direction 
                    optimalCameraForward = Vector3.down;
                }

                // Set optimal camera rotation from optimal camera forwards and up directions
                optimalCameraRotation = Quaternion.LookRotation(optimalCameraForward, optimalCameraUp);

                #endregion

                #region Find Optimal Camera Position

                if (!isLockCameraPosition)
                {
                    if (targetOffsetDamping && !isSnapCamera) { TargetOffsetPositionDamping(); }
                    else { targetOffsetDamped = targetOffset; }

                    // Optimal camera position is always target position plus some offset
                    if (targetOffsetCoordinates == TargetOffsetCoordinates.TargetRotation)
                    {
                        // Offset is applied with respect to target rotation
                        optimalCameraPosition = targetPosition + (targetRotation * targetOffsetDamped);
                    }
                    else if (targetOffsetCoordinates == TargetOffsetCoordinates.TargetRotationFlat)
                    {
                        // Offset is applied with respect to target rotation, but only rotation on the world y-axis
                        Vector3 targetTrfmFwdFlat = targetTrfmFwd;
                        targetTrfmFwdFlat.y = 0f;
                        optimalCameraPosition = targetPosition + (Quaternion.LookRotation(targetTrfmFwdFlat, Vector3.up) * targetOffsetDamped);
                    }
                    else if (targetOffsetCoordinates == TargetOffsetCoordinates.CameraRotation)
                    {
                        // Offset is applied with respect to optimal camera rotation forwards and up direction vectors
                        optimalCameraPosition = targetPosition + (targetOffsetDamped.z * optimalCameraForward) + (targetOffsetDamped.y * optimalCameraUp);
                        if (targetOffsetDamped.x != 0f)
                        {
                            optimalCameraPosition += targetOffsetDamped.x * Vector3.Cross(optimalCameraUp, optimalCameraForward);
                        }
                    }
                    else
                    {
                        // Offset is applied with respect to the world coordinate system
                        optimalCameraPosition = targetPosition + targetOffsetDamped;
                    }
                }

                #endregion
            }

            #region Object Clipping
            bool isClippingThisFrame = false;
            if (clipObjects) { isClippingThisFrame = ObjectClipping(targetPosition, targetRotation, ref optimalCameraPosition, ref optimalCameraRotation); }
            #endregion

            #region Compute Movement Towards Optimal Camera Position / Rotation

            if (isOrbitEnabled)
            {
                transform.SetPositionAndRotation(optimalCameraPosition, optimalCameraRotation);
            }
            else
            {
                if (isLockCameraPosition && isShaking)
                {
                    // Might be a way of doing this in one step
                    //transform.position = previousOptimalCameraPosition;
                    //transform.Translate(Random.insideUnitCircle * shakeStrength);
                }
                if (!isLockCameraPosition)
                {
                    // Move immediately to optimal camera position
                    if (lockToTargetPosition || isSnapCamera)
                    {
                        if (isShaking && !isSnapCamera)
                        {
                            // Might be a way of doing this in one step
                            transform.position = optimalCameraPosition;
                            transform.Translate(Random.insideUnitCircle * shakeStrength);
                        }
                        else { transform.position = optimalCameraPosition; }

                        previousOptimalCameraPosition = optimalCameraPosition;
                    }
                    // Move gradually towards optimal camera position
                    else
                    {
                        // Smooth follow code (substitute for Lerp)
                        // [IMPORTANT] If you get a NaN error message, it means you didn't call DisableCamera() before setting Time.timeScale = 0.
                        // When clipObjects is enabled, and something is blocking camera, move quickly to target position
                        transform.position = SmoothFollowPosition(currentPosition, previousOptimalCameraPosition, optimalCameraPosition, Time.deltaTime, isClippingThisFrame && moveSpeed < minClipMoveSpeed ? minClipMoveSpeed : moveSpeed);

                        if (isShaking)
                        {
                            transform.Translate(Random.insideUnitCircle * shakeStrength);
                        }

                        previousOptimalCameraPosition = optimalCameraPosition;
                    }
                }

                // Move immediately to optimal camera rotation
                if (lockToTargetRotation || isSnapCamera) { transform.rotation = optimalCameraRotation; }
                // Move gradually towards optimal camera rotation
                else
                {
                    // Smooth damp code (substitute for Lerp) - modified to work with rotation
                    // First, convert quaternions into Euler angles vectors
                    currentCameraRotationEulerAngles = transform.rotation.eulerAngles;
                    optimalCameraRotationEulerAngles = optimalCameraRotation.eulerAngles;
                    // Then, use SmoothDampAngle on each of the three axes separately
                    currentCameraRotationEulerAngles.x = Mathf.SmoothDampAngle(currentCameraRotationEulerAngles.x, optimalCameraRotationEulerAngles.x,
                        ref currentTurnDampVelocity.x, 1f / turnSpeed, Mathf.Infinity, Time.deltaTime);
                    currentCameraRotationEulerAngles.y = Mathf.SmoothDampAngle(currentCameraRotationEulerAngles.y, optimalCameraRotationEulerAngles.y,
                        ref currentTurnDampVelocity.y, 1f / turnSpeed, Mathf.Infinity, Time.deltaTime);
                    currentCameraRotationEulerAngles.z = Mathf.SmoothDampAngle(currentCameraRotationEulerAngles.z, optimalCameraRotationEulerAngles.z,
                        ref currentTurnDampVelocity.z, 1f / turnSpeed, Mathf.Infinity, Time.deltaTime);
                    // Finally, convert Euler angles vector back to quaternion
                    transform.rotation = Quaternion.Euler(currentCameraRotationEulerAngles);
                }
            }
            #endregion

            // If we need to manually sync the stars with the camera rotation,
            // do it here.
            if (isOverrideSyncStars)
            {
                Celestials.SyncStars();
            }
        }

        /// <summary>
        /// Orbit the camera around the ship using the orbitAmount
        /// </summary>
        /// <param name="camPosition"></param>
        /// <param name="camRotation"></param>
        private void OrbitCamera(ref Vector3 camPosition, ref Quaternion camRotation)
        {
            bool isShipNearGround = false;
            float shipDistanceToGround = 1f;

            if (isOrbitGroundDetection)
            {
                RaycastHit raycastHit;

                // Raise the offset up slightly in case the bottom of the ship is resting on the ground
                Vector3 localOffset = orbitGroundDetectOffset.y < 0f ? orbitGroundDetectOffset + new Vector3(0f, 0.001f, 0f) : orbitGroundDetectOffset;

                Ray ray = new Ray(targetTrfm.position + (targetTrfm.rotation * localOffset), Vector3.down);

                // Use a single line raycast - FUTURE offer a boxcast and/or multiple points on the base of the ship
                // to increase ground detection accuracy.
                if (SSCUtils.GetClosestCollider(ray, Vector3.down, orbitDistance, orbitGroundMask, out raycastHit))
                {
                    // Add the offset on local Y axis back to the detected collider distance
                    shipDistanceToGround = raycastHit.distance + localOffset.y;
                    isShipNearGround = true;
                }
            }

            #region Modify Target Rotation

            // Modify target pitch and yaw using orbit -1 to 1 value derived from input - see CalcOrbitAmount()
            currentOrbitPitchAngle = orbitAmount.y * 360f;
            // Yaw angle needs to be inverted
            currentOrbitYawAngle = -orbitAmount.x * 360f;

            // Yaw wraps back around
            if (currentOrbitYawAngle > 180f) { currentOrbitYawAngle -= 360f; }
            else if (currentOrbitYawAngle < -180f) { currentOrbitYawAngle += 360f; }

            // Clamp pitch angle if ship has landed
            float lowestOrbitPitchAngle = 90f;
            if (isShipNearGround && orbitDistance > 0f)
            {
                lowestOrbitPitchAngle = Mathf.Atan((shipDistanceToGround - camera1.nearClipPlane) / orbitDistance) * Mathf.Rad2Deg;
            }

            // Fix gimbal lock by clamping pitch angle between -(min pitch angle) and +90 degrees
            if (currentOrbitPitchAngle > 90f) { currentOrbitPitchAngle = 90f; }
            else if (currentOrbitPitchAngle < -lowestOrbitPitchAngle) { currentOrbitPitchAngle = -lowestOrbitPitchAngle; }

            #endregion

            #region Calculate Target Rotation / Position

            // Calculate local rotation using pitch and yaw angles
            Quaternion orbitTargetRotation = Quaternion.Euler(currentOrbitPitchAngle, currentOrbitYawAngle, 0f);
            // Convert into world-space rotation
            orbitTargetRotation = targetTrfm.rotation * orbitTargetRotation;

            // Calculate target position by moving away from the ship in the opposite direction to the target rotation
            Vector3 orbitTargetPosition = targetTrfm.position + (orbitTargetRotation * -Vector3.forward * orbitDistance);

            #endregion

            #region Set optimal Camera position and rotation
            camRotation = orbitTargetRotation;
            camPosition = orbitTargetPosition;
            #endregion
        }

        private Vector3 SmoothFollowPosition(Vector3 currentPosition, Vector3 previousTargetPosition, Vector3 currentTargetPosition, float deltaTime, float speed)
        {
            // Avoid NaN when speed = 0.
            if (speed < 0.001f) { return currentPosition; }
            else
            {
                float speedTimeProduct = deltaTime * speed;
                Vector3 v = (currentTargetPosition - previousTargetPosition) / speedTimeProduct;
                Vector3 f = currentPosition - previousTargetPosition + v;
                return currentTargetPosition - v + f * Mathf.Exp(-speedTimeProduct);
            }
        }

        /// <summary>
        /// This is called by a ship when it is hit.
        /// The shakeAmount should be proportion of the maximum duration and strength as a
        /// normalised value (between 0.0 and 1.0).
        /// </summary>
        /// <param name="shakeAmountNormalised">Normalised amount between 0.0 and 1.0</param>
        internal void ShakeCameraInternal(float shakeAmountNormalised)
        {
            ShakeCamera(maxShakeDuration * shakeAmountNormalised, maxShakeStrength * shakeAmountNormalised);
        }

        /// <summary>
        /// Attempt to not fly through objects in the scene that are in the clipObjectMask layer(s)
        /// </summary>
        private bool ObjectClipping(Vector3 aimAtPosition, Quaternion targetRot, ref Vector3 cameraPosition, ref Quaternion cameraRotation)
        {
            // Get the direction looking from the target to the camera.
            Vector3 clipLookDirection = (cameraPosition - aimAtPosition).normalized;

            Vector3 clipStartPosition = clipMinDistance > 0f ? aimAtPosition + (clipLookDirection * clipMinDistance) : aimAtPosition;

            // The distance between the ship (or ship + clipMinDistance) and the camera LESS the camera near clip plane (don't need
            // to check what camera does not render).
            float clipCheckDistance = Vector3.Distance(clipStartPosition, cameraPosition) - camera1.nearClipPlane;

            RaycastHit clipHit;
            if (Physics.BoxCast(clipStartPosition, viewHalfExtents, clipLookDirection, out clipHit,
                cameraRotation, clipCheckDistance, clipObjectMask, QueryTriggerInteraction.Ignore))
            {
                // The near clip plane should be placed in front of the obstacle. By not adding the nearClipPlane
                // onto the hit distance, it should cater for situations where the obstacle normal is not aligned with the clipLookDirection.
                cameraPosition = clipStartPosition + (clipLookDirection * clipHit.distance);

                if (clipMinOffsetX > 0f || clipMinOffsetY > 0f)
                {
                    // Get local space offset
                    Vector3 currentOffset = Quaternion.Inverse(targetRot) * (cameraPosition - aimAtPosition);

                    if (currentOffset.y < clipMinOffsetY || currentOffset.x < clipMinOffsetX)
                    {
                        cameraPosition = aimAtPosition + (targetRot * new Vector3(currentOffset.x < clipMinOffsetX ? clipMinOffsetX : currentOffset.x, currentOffset.y < clipMinOffsetY ? clipMinOffsetY : currentOffset.y, currentOffset.z));
                    }
                }

                return true;
            }
            else { return false; }
        }

        //Vector3 clipStartPosition = Vector3.zero, clipLookDirection = Vector3.zero;
        //float clipCheckDistance;

        //private void OnDrawGizmos()
        //{
        //    if (camera1 != null)
        //    {
        //        // Test Object Clipping
        //        //Gizmos.color = Color.yellow;
        //        //Gizmos.DrawRay(optimalCameraPosition, clipLookDirection * 20f);

        //        Gizmos.color = Color.blue;
        //        Gizmos.DrawRay(clipStartPosition, clipLookDirection * (Vector3.Distance(clipStartPosition, optimalCameraPosition) - camera1.nearClipPlane));
        //        Gizmos.DrawWireCube(clipStartPosition, viewHalfExtents * 2f);
        //    }
        //}

        /// <summary>
        /// Adjust the target position based on the player or AI input.
        /// This should only be called when target is not null and
        /// isLockCameraPosition is not true.
        /// </summary>
        private void TargetOffsetPositionDamping()
        {
            // Get pitch and yaw input from the ship
            Vector3 momentInput = target.pilotMomentInput;
            float _dTFactor = Time.deltaTime * 10f;

            // Pitch - ship goes toward bottom of screen while pointing up
            if (momentInput.x > 0f)
            {
                dampingOffsetAdjPitch += -momentInput.x * dampingPitchRate * _dTFactor;
                if (dampingOffsetAdjPitch < dampingMaxPitchOffsetDown)
                {
                    dampingOffsetAdjPitch = dampingMaxPitchOffsetDown;
                }
            }
            // Pitch - ship goes toward top of screen while pointing down
            else if (momentInput.x < 0f)
            {
                dampingOffsetAdjPitch += -momentInput.x * dampingPitchRate * _dTFactor;
                if (dampingOffsetAdjPitch > dampingMaxPitchOffsetUp)
                {
                    dampingOffsetAdjPitch = dampingMaxPitchOffsetUp;
                }
            }
            else
            {
                if (dampingOffsetAdjPitch > targetOffset.y)
                {
                    dampingOffsetAdjPitch -= dampingPitchGravity * _dTFactor;
                    if (dampingOffsetAdjPitch < targetOffset.y)
                    {
                        dampingOffsetAdjPitch = targetOffset.y;
                    }
                }
                else if (dampingOffsetAdjPitch < targetOffset.y)
                {
                    dampingOffsetAdjPitch += dampingPitchGravity * _dTFactor;
                    if (dampingOffsetAdjPitch > targetOffset.y)
                    {
                        dampingOffsetAdjPitch = targetOffset.y;
                    }
                }
            }

            //dampingOffsetAdjPitch = targetOffset.y;

            // Yaw - ship goes right, camera goes left
            if (momentInput.y > 0f)
            {
                dampingOffsetAdjYaw += momentInput.y * dampingYawRate * _dTFactor;
                if (dampingOffsetAdjYaw > dampingMaxYawOffsetRight)
                {
                    dampingOffsetAdjYaw = dampingMaxYawOffsetRight;
                }
            }
            // Yaw - ship goes left, camera goes right
            else if (momentInput.y < 0f)
            {
                dampingOffsetAdjYaw += momentInput.y * dampingYawRate * _dTFactor;
                if (dampingOffsetAdjYaw < dampingMaxYawOffsetLeft)
                {
                    dampingOffsetAdjYaw = dampingMaxYawOffsetLeft;
                }
            }
            else
            {
                if (dampingOffsetAdjYaw > targetOffset.x)
                {
                    dampingOffsetAdjYaw -= dampingYawGravity * _dTFactor;
                    if (dampingOffsetAdjYaw < targetOffset.x)
                    {
                        dampingOffsetAdjYaw = targetOffset.x;
                    }
                }
                else if (dampingOffsetAdjYaw < targetOffset.x)
                {
                    dampingOffsetAdjYaw += dampingYawGravity * _dTFactor;
                    if (dampingOffsetAdjYaw > targetOffset.x)
                    {
                        dampingOffsetAdjYaw = targetOffset.x;
                    }
                }
            }

            targetOffsetDamped.x = dampingOffsetAdjYaw;
            targetOffsetDamped.y = dampingOffsetAdjPitch;
            targetOffsetDamped.z = targetOffset.z;

            //Debug.Log("[DEBUG] targetOffsetDamped: " + targetOffsetDamped + " T:" + Time.time);
        }

        #endregion

        #region Public API Non-Static Methods - General

        /// <summary>
        /// Attempt to apply camera settings from a ScriptableObject to this ShipCameraModule.
        /// See also ExportCameraSettings(..)
        /// </summary>
        /// <param name="shipCameraSettings"></param>
        public virtual void ApplyCameraSettings (ShipCameraSettings shipCameraSettings)
        {
            if (shipCameraSettings != null)
            {
                #region General settings
                cameraFixedRotation = shipCameraSettings.cameraFixedRotation;
                cameraRotationMode = shipCameraSettings.cameraRotationMode;
                dampingMaxPitchOffsetDown = shipCameraSettings.dampingMaxPitchOffsetDown;
                dampingMaxPitchOffsetUp = shipCameraSettings.dampingMaxPitchOffsetUp;
                dampingMaxYawOffsetLeft = shipCameraSettings.dampingMaxYawOffsetLeft;
                dampingMaxYawOffsetRight = shipCameraSettings.dampingMaxYawOffsetRight;
                dampingPitchGravity = shipCameraSettings.dampingPitchGravity;
                dampingPitchRate = shipCameraSettings.dampingPitchRate;
                dampingYawGravity = shipCameraSettings.dampingYawGravity;
                dampingYawRate = shipCameraSettings.dampingYawRate;
                followVelocityThreshold = shipCameraSettings.followVelocityThreshold;
                lockCameraPosition = shipCameraSettings.lockCameraPosition;
                lockToTargetPosition = shipCameraSettings.lockToTargetPosition;
                lockToTargetRotation = shipCameraSettings.lockToTargetRotation;
                maxShakeDuration = shipCameraSettings.maxShakeDuration;
                maxShakeStrength = shipCameraSettings.maxShakeStrength;
                moveSpeed = shipCameraSettings.moveSpeed;
                orientUpwards = shipCameraSettings.orientUpwards;
                targetOffset = shipCameraSettings.targetOffset;
                targetOffsetCoordinates = shipCameraSettings.targetOffsetCoordinates;
                targetOffsetDamping = shipCameraSettings.targetOffsetDamping;
                turnSpeed = shipCameraSettings.turnSpeed;
                updateType = shipCameraSettings.updateType;
                #endregion

                #region Clip Object settings
                clipMinDistance = shipCameraSettings.clipMinDistance;
                clipMinOffsetX = shipCameraSettings.clipMinOffsetX;
                clipMinOffsetY = shipCameraSettings.clipMinOffsetY;
                clipObjectMask = shipCameraSettings.clipObjectMask;
                clipObjects = shipCameraSettings.clipObjects;
                minClipMoveSpeed = shipCameraSettings.minClipMoveSpeed;
                #endregion

                #region Zoom Settings
                isZoomEnabled = shipCameraSettings.isZoomEnabled;
                unzoomDelay = shipCameraSettings.unzoomDelay;
                unzoomedFoV = shipCameraSettings.unzoomedFoV;
                zoomDamping = shipCameraSettings.zoomDamping;
                zoomDuration = shipCameraSettings.zoomDuration;
                zoomedInFoV = shipCameraSettings.zoomedInFoV;
                zoomedOutFoV = shipCameraSettings.zoomedOutFoV;
                #endregion

                if (shipCameraSettings.isSnapToTarget) { SnapToTarget(); }
            }
        }

        /// <summary>
        /// Disables the camera, preventing it from moving (does not stop the camera rendering).
        /// </summary>
        public void DisableCamera()
        {
            cameraIsEnabled = false;
        }

        /// <summary>
        /// Turn off the orbit feature
        /// </summary>
        public void DisableOrbit()
        {
            EnableOrDisableOrbit(false);
        }

        /// <summary>
        /// Turn off the zoom feature and reset the camera field of view to the default setting.
        /// </summary>
        public void DisableZoom()
        {
            EnableOrDisableZoom(false);
        }

        /// <summary>
        /// Enables the camera, allowing it to move.
        /// See StartCamera() to allow it to render.
        /// </summary>
        public void EnableCamera()
        {
            cameraIsEnabled = true;
        }

        /// <summary>
        /// Turn on the orbit feature
        /// </summary>
        public void EnableOrbit()
        {
            EnableOrDisableOrbit(true);
        }

        /// <summary>
        /// Turn on the zoom feature
        /// </summary>
        public void EnableZoom()
        {
            EnableOrDisableZoom(true);
        }

        /// <summary>
        /// Attempt to export the camera setings from this ShipCameraModule to a pre-created ShipCameraSettings ScriptableObject.
        /// See also ApplyCameraSettings(..)
        /// </summary>
        /// <param name="camSettings"></param>
        public virtual void ExportCameraSettings (ref ShipCameraSettings camSettings)
        {
            if (camSettings != null)
            {
                #region General settings
                camSettings.targetOffset = targetOffset;
                camSettings.targetOffsetCoordinates = targetOffsetCoordinates;
                camSettings.lockToTargetPosition = lockToTargetPosition;
                camSettings.moveSpeed = moveSpeed;
                camSettings.targetOffsetDamping = targetOffsetDamping;
                camSettings.dampingPitchRate = dampingPitchRate;
                camSettings.dampingPitchGravity = dampingPitchGravity;
                camSettings.dampingYawRate = dampingYawRate;
                camSettings.dampingYawGravity = dampingYawGravity;
                camSettings.dampingMaxPitchOffsetUp = dampingMaxPitchOffsetUp;
                camSettings.dampingMaxPitchOffsetDown = dampingMaxPitchOffsetDown;
                camSettings.dampingMaxYawOffsetRight = dampingMaxYawOffsetRight;
                camSettings.dampingMaxYawOffsetLeft = dampingMaxYawOffsetLeft;
                camSettings.lockToTargetRotation = lockToTargetRotation;
                camSettings.turnSpeed = turnSpeed;
                camSettings.lockCameraPosition = lockCameraPosition;
                camSettings.cameraRotationMode = cameraRotationMode;
                camSettings.followVelocityThreshold = followVelocityThreshold;
                camSettings.orientUpwards = orientUpwards;
                camSettings.cameraFixedRotation = cameraFixedRotation;
                camSettings.updateType = updateType;
                camSettings.maxShakeStrength = maxShakeStrength;
                camSettings.maxShakeDuration = maxShakeDuration;
                #endregion

                #region Object Clipping settings
                camSettings.clipObjects = clipObjects;
                camSettings.minClipMoveSpeed = minClipMoveSpeed;
                camSettings.clipMinDistance = clipMinDistance;
                camSettings.clipMinOffsetX = clipMinOffsetX;
                camSettings.clipMinOffsetY = clipMinOffsetY;
                camSettings.clipObjectMask = clipObjectMask;
                #endregion

                #region Zoom settings
                camSettings.isZoomEnabled = isZoomEnabled;
                camSettings.zoomDuration = zoomDuration;
                camSettings.unzoomDelay = unzoomDelay;
                camSettings.unzoomedFoV = unzoomedFoV;
                camSettings.zoomedInFoV = zoomedInFoV;
                camSettings.zoomedOutFoV = zoomedOutFoV;
                camSettings.zoomDamping = zoomDamping;
                #endregion
            }
        }

        /// <summary>
        /// The amount of orbit currently applied.
        /// x = local horizontal (rotate around local y axis)
        /// y = local vertical (rotate around local x axis)
        /// 0,0 is no orbit.
        /// 1.0 is fully orbited right or pitched up.
        /// -1.0 is fully orbited left or pitched down
        /// </summary>
        public Vector2 GetOrbitAmount()
        {
            return orbitAmount;
        }

        /// <summary>
        /// Get the current target ship for the camera. If the camera is currently not assigned to a ship, it will return null.
        /// </summary>
        /// <returns></returns>
        public ShipControlModule GetTarget()
        {
            return target;
        }

        /// <summary>
        /// Get the current target Ship Id. If no target, will return -1.
        /// </summary>
        /// <returns></returns>
        public int GetTargetShipId()
        {
            return target != null ? target.GetShipId : -1;
        }

        /// <summary>
        /// The amount of zoom currently used.
        /// 0 is no zoom.
        /// 1 is fully zoomed in.
        /// -1 is fully zoomed out.
        /// </summary>
        /// <param name="zoomAmount"></param>
        public float GetZoomAmount()
        {
            return zoomFactor;
        }

        /// <summary>
        /// Start the camera rendering. If the camera is disabled, it will remain so.
        /// Call EnableCamera() to allow it to also move.
        /// If there was an Audio Listener component attached and enabled when this
        /// module was initialised, the listener will be re-enabled.
        /// </summary>
        public void StartCamera()
        {
            if (camera1 != null)
            {
                if (!camera1.enabled)
                {
                    camera1.enabled = true;
                    if (isAudioListenerConfigured && audioListener != null) { audioListener.enabled = true; }
                }

                // Should celestials (stars) be synch'd from the ship camera,
                // rather from the Celestials component?
                if (isOverrideSyncStars)
                {
                    if (celestials == null)
                    {
                        celestials = Celestials.GetStars(sceneHandle);
                    }

                    if (celestials != null)
                    {
                        celestials.SetTimingType(Celestials.TimingType.Manual);
                    }
                }
            }
        }

        /// <summary>
        /// Stops the camera from rendering. Also disables the camera from moving.
        /// If there was an Audio Listener component attached and enabled when this
        /// module was initialised, the listener will be disabled.
        /// </summary>
        public void StopCamera()
        {
            StopCameraShake();
            cameraIsEnabled = false;
            if (camera1 != null && camera1.enabled) { camera1.enabled = false; }
            if (isAudioListenerConfigured && audioListener != null) { audioListener.enabled = false; }
        }

        /// <summary>
        /// Set's a new target for the camera and calls ReinitialiseTargetVariables().
        /// </summary>
        /// <param name="shipControlModule"></param>
        public void SetTarget(ShipControlModule shipControlModule)
        {
            // reset the previous target's camera shake callback method
            if (target != null && target.shipInstance != null) { target.shipInstance.callbackOnCameraShake = null; }

            target = shipControlModule;
            ReinitialiseTargetVariables();
        }

        /// <summary>
        /// Moves the camera. Should be called during an update function (Update, LateUpdate or FixedUpdate).
        /// Typically, you should not call this yourself, as it is called automatically each frame. However,
        /// it can prove useful in the case where you need to force a camera movement update for this frame.
        /// </summary>
        public void MoveCamera()
        {
            MoveCameraInternal(false);
        }

        /// <summary>
        /// Tele-port the camera to a new position.
        /// Rotation is xyz euler angles in degrees.
        /// See also TelePort(..)
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public void MoveTo (Vector3 position, Vector3 rotation)
        {
            // Should be the same as setting the transform directly but
            // we'll update the variables too.

            previousOptimalCameraPosition = position;
            optimalCameraRotation = Quaternion.Euler(rotation);

            transform.SetPositionAndRotation(position, optimalCameraRotation);
        }

        /// <summary>
        /// Call this whenever changing camera properties
        /// </summary>
        public void ReinitialiseCameraSettings()
        {
            if (camera1 == null) { camera1 = GetComponent<Camera>(); }

            if (camera1 != null)
            {
                // Calculate the half width, height and depth of the camera view. This can be used with
                // Physics.Boxcast to "see" objects in front of the camera.
                viewHalfExtents = Vector3.zero;
                // fieldOfView is in degrees. Convert to radians.
                viewHalfExtents.y = camera1.nearClipPlane * Mathf.Tan(0.5f * Mathf.Deg2Rad * camera1.fieldOfView);
                viewHalfExtents.x = viewHalfExtents.y * camera1.aspect;

                // Reset zoom
                SetZoomAmount(0f);
            }

            // The start value is set to Nothing (0) intentially. We then set it to something sensible
            if (clipObjectMask == 0) { ResetClipObjectSettings(); }
        }

        /// <summary>
        /// Reset the clip object settings to their default values
        /// </summary>
        public void ResetClipObjectSettings()
        {
            // Everything, except TransparentFX (1), IgnoreRaycast (2),  UI (5)
            //clipObjectMask = ~((1 << 1) | (1 << 2) | (1 << 5));
            clipObjectMask = DefaultClipObjectMask;
        }

        /// <summary>
        /// Re-initialises variables related to the target. 
        /// Call after modifying target or shake public variables.
        /// This will stop camera from shaking.
        /// </summary>
        public void ReinitialiseTargetVariables()
        {
            if (target != null)
            {
                targetTrfm = target.transform;
                targetRBody = target.GetComponent<Rigidbody>();

                if (target.shipInstance != null)
                {
                    if (maxShakeStrength > 0f && maxShakeDuration > 0f) { target.shipInstance.callbackOnCameraShake = ShakeCameraInternal; }
                    else { target.shipInstance.callbackOnCameraShake = null; }
                }
            }
            else
            {
                targetTrfm = null;
                targetRBody = null;
            }

            dampingOffsetAdjPitch = 0f;
            dampingOffsetAdjYaw = 0f;

            StopCameraShake();
        }

        /// <summary>
        /// Shake the camera for maxShakeDuration seconds which maxShakeStrength or force.
        /// If the camera is not enabled or the duration and/or strength are 0 or less,
        /// StopCameraShake() will be automatically called and the inputs ignored.
        /// </summary>
        public void ShakeCamera()
        {
            ShakeCamera(maxShakeDuration, maxShakeStrength);
        }

        /// <summary>
        /// Shake the camera with strength and duration relative to
        /// the current maxShakeDuration and maxShakeStrength.
        /// Range should be between 0.0 and 1.0.
        /// </summary>
        /// <param name="relativeStrength">Normalised value between 0.0 and 1.0</param>
        public void ShakeCamera (float relativeStrength)
        {
            if (relativeStrength < 0.0001f) { StopCameraShake(); }
            else if (relativeStrength >= 1f) { ShakeCamera(maxShakeDuration, maxShakeStrength); }
            else { ShakeCameraInternal(relativeStrength); }
        }

        /// <summary>
        /// Shake the camera for specified seconds which the given relative strength or force.
        /// If the camera is not enabled or the duration and/or strength are 0 or less,
        /// StopCameraShake() will be automatically called and the inputs ignored.
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="strength"></param>
        public void ShakeCamera (float duration, float strength)
        {
            if (cameraIsEnabled && duration > 0f && strength > 0f)
            {              
                isShaking = true;
                shakeStrength = strength;
                shakeTimer = duration;
            }
            else
            {
                StopCameraShake();
            }
        }

        /// <summary>
        /// Shake the camera after initial delay in seconds, with the
        /// current maxShakeDuration and maxShakeStrength.
        /// </summary>
        /// <param name="delayTime"></param>
        public void ShakeCameraDelayed (float delayTime)
        {
            if (delayTime > 0f)
            {
                Invoke("ShakeCamera", delayTime);
            }
            else { ShakeCamera(maxShakeDuration, maxShakeStrength); }
        }

        /// <summary>
        /// Send orbit horizontal input to the camera. It is ignored if Orbit is not enabled.
        /// Values should be between -1.0 and 1.0.
        /// Orbit right for +ve values. Orbit left for -ve values.
        /// The value is automatically reset to 0 after use.
        /// </summary>
        /// <param name="orbitInput"></param>
        public void SendOrbitHorizInput (float orbitInput)
        {
            if (isOrbitEnabled)
            {
                currentOrbitHorizInput = orbitInput < -1f ? -1f : orbitInput > 1f ? 1f : orbitInput;

                currentOrbitHorizInput = SSCMath.DampValue(previousOrbitHorizInput, currentOrbitHorizInput, Time.deltaTime, orbitHorizDamping);
                previousOrbitHorizInput= currentOrbitHorizInput;

                // This is used when (if) we only want to update the orbit amount after the last input has been used.
                if (isOrbitHorizInputProcessed) { isOrbitHorizInputProcessed = false; }
            }
        }

        /// <summary>
        /// Send orbit vertical input to the camera. It is ignored if Orbit is not enabled.
        /// Values should be between -1.0 and 1.0.
        /// Orbit up for +ve values. Orbit down for -ve values.
        /// The value is automatically reset to 0 after use.
        /// </summary>
        /// <param name="orbitInput"></param>
        public void SendOrbitVertInput (float orbitInput)
        {
            if (isOrbitEnabled)
            {
                currentOrbitVertInput = orbitInput < -1f ? -1f : orbitInput > 1f ? 1f : orbitInput;

                //currentOrbitVertInput = SSCMath.DampValue(previousOrbitVertInput, currentOrbitVertInput, Time.deltaTime, orbitVertDamping);
                previousOrbitVertInput = currentOrbitVertInput;

                // This is used when (if) we only want to update the orbit amount after the last input has been used.
                if (isOrbitVertInputProcessed) { isOrbitVertInputProcessed = false; }
            }
        }

        /// <summary>
        /// Send zoom input to the camera. It is ignored if Zoom is not enabled.
        /// Values should be between -1.0 and 1.0.
        /// Zoom in for +ve values. Zoom out for -ve values.
        /// The value is automatically reset to 0 after use.
        /// </summary>
        /// <param name="zoomInput"></param>
        public void SendZoomInput (float zoomInput)
        {
            if (isZoomEnabled)
            {
                currentZoomInput = zoomInput < -1f ? -1f : zoomInput > 1f ? 1f : zoomInput;

                currentZoomInput = SSCMath.DampValue(previousZoomInput, currentZoomInput, Time.deltaTime, zoomDamping);
                previousZoomInput = currentZoomInput;

                if (isZoomInputProcessed) { isZoomInputProcessed = false; }
            }
        }

        /// <summary>
        /// Set the unzoom delay, in seconds, for the camera.
        /// </summary>
        /// <param name="newValue"></param>
        public void SetUnzoomDelay(float newValue)
        {
            if (newValue < 0f) { newValue = 0f; }

            unzoomDelay = newValue;
            if (newValue < unzoomTimer) { unzoomTimer = newValue; }
        }

        /// <summary>
        /// Set the camera to use a particular display or screen. Displays or monitors are numbered from 1 to 8.
        /// </summary>
        /// <param name="displayNumber">1 to 8</param>
        public void SetCameraTargetDisplay(int displayNumber)
        {
            Camera camera = GetComponent<Camera>();

            if (camera != null && SSCUtils.VerifyTargetDisplay(displayNumber, true)) { camera.targetDisplay = displayNumber - 1; }
        }

        /// <summary>
        /// The amount of orbit to apply.
        /// x = local horizontal (rotate around local y axis)
        /// y = local vertical (rotate around local x axis)
        /// 0,0 is no orbit.
        /// 1.0 is fully orbited right or pitched up.
        /// -1.0 is fully orbited left or pitched down
        /// </summary>
        /// <param name="orbitValue"></param>
        public void SetOrbitAmount (Vector2 orbitValue)
        {
            if (orbitValue.x > 1f) { orbitAmount.x = 1f; }
            else if (orbitValue.x < -1f) { orbitAmount.x = 1f; }
            else { orbitAmount.x = orbitValue.x; }

            if (orbitValue.y > 1f) { orbitAmount.y = 1f; }
            else if (orbitValue.y < -1f) { orbitAmount.y = 1f; }
            else { orbitAmount.y = orbitValue.y; }
        }

        /// <summary>
        /// Attempt to set a new maxShakeDuration
        /// </summary>
        /// <param name="newDuration"></param>
        public void SetMaxShakeDuration (float newDuration)
        {
            if (newDuration > 0f)
            {
                maxShakeDuration = newDuration;
            }
        }

        /// <summary>
        /// Attempt to set a new maxShakeStrenth
        /// </summary>
        /// <param name="newStrength"></param>
        public void SetMaxShakeStrength (float newStrength)
        {
            if (newStrength > 0f)
            {
                maxShakeStrength = newStrength;
            }
        }

        /// <summary>
        /// The amount of zoom to apply.
        /// 0 is no zoom.
        /// 1 is fully zoomed in.
        /// -1 is fully zoomed out.
        /// </summary>
        /// <param name="zoomAmount"></param>
        public void SetZoomAmount(float zoomAmount)
        {
            if (zoomAmount > 1f) { zoomFactor = 1f; }
            else if (zoomAmount < -1f) { zoomFactor = 1f; }
            else { zoomFactor = zoomAmount; }
        }

        /// <summary>
        /// Snap or instantly move the camera to the target position and rotation
        /// </summary>
        public void SnapToTarget()
        {
            // Reset damping
            dampingOffsetAdjYaw = targetOffset.x;
            dampingOffsetAdjPitch = targetOffset.y;
            MoveCameraInternal(true);
        }

        /// <summary>
        /// Stop the camera from shaking
        /// </summary>
        public void StopCameraShake()
        {
            isShaking = false;
            shakeTimer = 0f;
        }

        /// <summary>
        /// Teleport the camera to a new location by moving by an amount
        /// in the x, y and z directions. This could be useful if changing
        /// the origin or centre of your world to compensate for float-point
        /// error.
        /// </summary>
        /// <param name="delta"></param>
        public void TelePort (Vector3 delta)
        {
            previousOptimalCameraPosition += delta;
            transform.position += delta;
        }

        /// <summary>
        /// Teleport the camera to a new location with given rotation in world space
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="newRotation"></param>
        public void TelePort (Vector3 newPosition, Quaternion newRotation)
        {
            Vector3 delta = newPosition - transform.position;
            previousOptimalCameraPosition += delta;
            transform.position = newPosition;
            transform.rotation = newRotation;
        }

        /// <summary>
        /// Attempt to toggle orbit camera on or off.
        /// </summary>
        public void ToggleOrbit()
        {
            EnableOrDisableOrbit(!isOrbitEnabled);
        }

        /// <summary>
        /// Attempt to toggle zoom on or off
        /// </summary>
        public void ToggleZoom()
        {
            EnableOrDisableZoom(!isZoomEnabled);
        }

        /// <summary>
        /// Apply the current zoom amount to the camera.
        /// This gets automatically applied in LateUpdate()
        /// when zoom is enabled.
        /// </summary>
        public void ZoomCamera()
        {
            if (camera1 != null)
            {
                if (zoomFactor == 0f)
                {
                    camera1.fieldOfView = unzoomedFoV;
                }
                else if (zoomFactor > 0f)
                {
                    camera1.fieldOfView = unzoomedFoV - (zoomFactor * (unzoomedFoV - zoomedInFoV));
                }
                else
                {
                    camera1.fieldOfView = unzoomedFoV - (zoomFactor * (zoomedOutFoV - unzoomedFoV));
                }
            }
        }

        #endregion

        #region Public API Methods - Events

        /// <summary>
        /// Call this when you wish to remove any custom event listeners, like
        /// after creating them in code and then destroying the object.
        /// You could add this to your game play OnDestroy code.
        /// </summary>
        public void RemoveListeners()
        {
            if (onOrbitEnable != null) { onOrbitEnable.RemoveAllListeners(); }
            if (onOrbitDisable != null) { onOrbitDisable.RemoveAllListeners(); }
        }

        #endregion
    }
}
