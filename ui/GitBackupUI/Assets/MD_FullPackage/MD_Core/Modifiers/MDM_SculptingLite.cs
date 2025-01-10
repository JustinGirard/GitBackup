using System.Threading;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

using MDPackage.Utilities;
using System.Collections;

namespace MDPackage.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Sculpting Lite.
    /// A lite version of a complete sculpting solution for mesh renderers in Unity editor / at runtime. Visit https://assetstore.unity.com/packages/tools/modeling/sculpting-pro-201873 for pro version.
    /// Written by Matej Vanco, 2018
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAG_PATH_NAME + "Modifiers/Sculpting Lite")]
    public sealed class MDM_SculptingLite : MD_ModifierBase, MD_ModifierBase.IMDThreadingSupport
    {
        // IMDThreading implementation
        public bool ThreadUseMultithreading { get => _threadUseMultithreading; set => _threadUseMultithreading = value; }
        [SerializeField] private bool _threadUseMultithreading;
        public bool ThreadEditorThreadSupported { get => _threadEditorThreadSupported; private set => _threadEditorThreadSupported = value; }
        [SerializeField] private bool _threadEditorThreadSupported;
        public bool ThreadIsDone { get; private set; }
        public int ThreadSleep { get => _threadSleep; set => _threadSleep = value; }
        [SerializeField, Range(5, 60)] private int _threadSleep = 20;

        // Modifier serialized & public fields
        public bool sculptingMobileSupport = false;

        public bool sculptingRecalculateNormalsOnRelease = true;

        public bool sculptingUseBrushProjection = true;
        public GameObject sculptingBrushProjection;
        public bool sculptingBrushProjectionAutomaticallyCreated = false;

        public float sculptingBrushSize = 0.5f;
        public float sculptingBrushIntensity = 0.05f;

        /// <summary>
        /// Wraps all the thread-safe data
        /// </summary>
        private class ThreadWrapper
        {
            public Vector3 mth_WorldPoint;
            public float mth_Radius;
            public float mth_Strength;
            public BrushStatus mth_State;
            public Vector3 mth_WorldPos;
            public Quaternion mth_WorldRot;
            public Vector3 mth_WorldScale;
            public Vector3 mth_Direction;
            public float mth_StylizeIntensity;
            public float mth_SmoothIntensity;
            public int mth_SmoothingType;
            public NoiseTypes mth_NoiseDirs;
            public InterpolationType SS_SculptingType;

            /// <summary>
            /// Setup all the params in one method
            /// </summary>
            public void SetParams(Vector3 worldPoint, float Radius, float Strength, Vector3 Dir, BrushStatus State, Vector3 RealPos, Vector3 RealScale, Quaternion RealRot, NoiseTypes NoiseDirs, InterpolationType SS_SculptingType__, float stylizedIntens, float smoothIntens, int smoothType)
            {
                mth_WorldPoint = worldPoint;
                mth_Radius = Radius;
                mth_Strength = Strength;
                mth_State = State;
                mth_WorldPos = RealPos;
                mth_WorldRot = RealRot;
                mth_WorldScale = RealScale;
                mth_Direction = Dir;
                mth_NoiseDirs = NoiseDirs;
                SS_SculptingType = SS_SculptingType__;
                mth_StylizeIntensity = stylizedIntens;
                mth_SmoothIntensity = smoothIntens;
                mth_SmoothingType = smoothType;
            }
        }
        private readonly ThreadWrapper threadWrapper = new ThreadWrapper();

        public struct SculptingBrushData
        {
            public Vector3 worldSpacePosition;
            public float radius;
            public float intensity;

            public Vector3 matPos;
            public Quaternion matRot;
            public Vector3 matScale;

            public bool isNotInitialized;
        }

        public enum BrushStatus : int { None = 0, Raise = 1, Lower = 2, Revert = 3, Noise = 4, Smooth = 5, Stylize = 6 };
        public BrushStatus sculptingBrushStatus = BrushStatus.None;

        public enum SculptingMode { VerticesDirection, BrushDirection, CustomDirection, CustomDirectionObject, InternalScriptDirection };
        //VerticesDirection         Sets the direction by vertice normals
        //BrushDirection            Sets the direction by brush rotation
        //CustomDirection           Sets the direction by custom euler values
        //CustomDirectionObject     Sets the direction by specific object's local direction
        //InternalScriptDirection   Sets the direction by internal script (programmer may declare an input for the direction right in the Sculpting method)
        public SculptingMode sculptingMode = SculptingMode.BrushDirection;

        public Vector3 sculptingCustomDirection;

        public bool sculptingEnableHeightLimitations = false;
        public Vector2 sculptingHeightLimitations;
        public bool sculptingEnableDistanceLimitations = false;
        public float sculptingDistanceLimitation = 1.0f;

        public enum CustomDirObjDirection { Up, Down, Forward, Back, Right, Left};
        public CustomDirObjDirection sculptingCustomDirObjDirection;
        public GameObject sculptingCustomDirectionObject;
        public bool sculptingUpdateColliderAfterRelease = true;
        public enum InterpolationType { Linear, Exponential};
        public InterpolationType sculptingInterpolationType = InterpolationType.Exponential;

        // > Runtime settings - input
        public bool sculptingUseInput = true;
        public bool sculptingVRInput = false;
        public bool sculptingUseRaiseFunct = true;
        public bool sculptingUseLowerFunct = true;
        public bool sculptingUseRevertFunct = false;
        public bool sculptingUseNoiseFunct = false;
        public bool sculptingUseSmoothFunct = false;
        public bool sculptingUseStylizeFunct = false;

        public enum NoiseTypes { XYZ, XZ, XY, YZ, Z, Y, X, Centrical};
        public NoiseTypes sculptingNoiseTypes = NoiseTypes.XYZ;
        [Range(0.01f,0.99f)]    
        public float sculptingStylizeIntensity = 0.65f;
        [Range(0.01f, 1f)]      
        public float sculptingSmoothIntensity = 0.5f;
        public enum SmoothingType : int { HcFilter = 0, LaplacianFilter = 1 };
        public SmoothingType sculptingSmoothingType = SmoothingType.HcFilter;

        // > Other essentials
        public Camera sculptingCameraCache;
        public bool sculptingFromCursor = true;
        public Transform sculptingOriginTransform;
        public LayerMask sculptingLayerMask = ~0;

        public bool sculptingRecordHistory = false;
        [Range(1,20), Tooltip("It's recommended to have limited history records due to performance & memory")]
        [SerializeField] private int sculptingMaxHistoryRecords = 5;
        public struct HistoryRecords
        {
            public string historyNotes;
            public Vector3[] vertexPositions;
        }
        public List<HistoryRecords> SculptingHistoryRecords { get; private set; }

        public int SculptingMaxHistoryRecords { get => sculptingMaxHistoryRecords; set => sculptingMaxHistoryRecords = Mathf.Max(1, value); }

        #region Input Hookup

        /// <summary>
        /// Use this input hookup for cursor/touch world screen position
        /// </summary>
        public Vector3 InputHook_CursorScreenPosition { get; set; }

        public bool InputHook_Lower { get; set; }
        public bool InputHook_Raise { get; set; }
        public bool InputHook_Revert { get; set; }
        public bool InputHook_Noise { get; set; }
        public bool InputHook_Smooth { get; set; }
        public bool InputHook_Stylize { get; set; }

        public bool InputHook_GenericNonPCButton
        {
            get => genericInputDown;

            set
            {
                bool prev = genericInputDown;
                genericInputDown = value;
                if (prev != genericInputDown)
                {
                    if (!genericInputDown)
                        Sculpting_RecordControlUp();
                    else
                        Sculpting_RecordControlDown();
                }
            }
        }

        private readonly MD_BackendGenericButton buttonBacked_Lower = new MD_BackendGenericButton();
        private readonly MD_BackendGenericButton buttonBacked_Raise = new MD_BackendGenericButton();
        private readonly MD_BackendGenericButton buttonBacked_Revert = new MD_BackendGenericButton();
        private readonly MD_BackendGenericButton buttonBacked_Noise = new MD_BackendGenericButton();
        private readonly MD_BackendGenericButton buttonBacked_Smooth = new MD_BackendGenericButton();
        private readonly MD_BackendGenericButton buttonBacked_Stylize = new MD_BackendGenericButton();

        #endregion

        // > Privates

        private bool controlInputDown = false;
        private bool genericInputDown = false;
        private readonly RaycastHit[] nonAllocRaycastStorage = new RaycastHit[1];

        #region Event Subscription

        protected override void OnEnable()
        {
            base.OnEnable();
            OnMeshSubdivided += ResetSculptingParams;
            OnMeshSmoothed += ResetSculptingParams;
            OnMeshBaked += ResetSculptingParams;
            OnMeshRestored += ResetSculptingParams;
            OnNewMeshReferenceCreated += ResetSculptingParams;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnMeshSubdivided -= ResetSculptingParams;
            OnMeshSmoothed -= ResetSculptingParams;
            OnMeshBaked -= ResetSculptingParams;
            OnMeshRestored -= ResetSculptingParams;
            OnNewMeshReferenceCreated -= ResetSculptingParams;
        }

        protected override void OnDestroy()
        {
            if(sculptingBrushProjection && sculptingBrushProjectionAutomaticallyCreated)
            {
                if (!Application.isPlaying)
                    DestroyImmediate(sculptingBrushProjection);
                else
                    Destroy(sculptingBrushProjection);
            }
            if (mc)
            {
                if (!Application.isPlaying)
                    DestroyImmediate(mc);
                else
                    Destroy(mc);
            }
            base.OnDestroy();
        }

        private void ResetSculptingParams()
        {
            if (sculptingRecordHistory)
                SculptingHistoryRecords = new List<HistoryRecords>();
            Sculpting_RefreshMeshCollider();
        }

        #endregion

        #region Base overrides

        protected override IEnumerator Start()
        {
            if (sculptingBrushProjection == null && sculptingUseBrushProjection)
            {
                GameObject brushProj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Material m = new Material(MD_Utilities.Specific.GetProperPipelineDefaultShader());
                m.color = new Color(0, 128, 0, 0.2f);
                brushProj.GetComponent<Renderer>().sharedMaterial = m;
                if (Application.isPlaying)
                    Destroy(brushProj.GetComponent<Collider>());
                else
                    DestroyImmediate(brushProj.GetComponent<Collider>());

                brushProj.name = "SculptingBrushObject";
                brushProj.SetActive(false);
                sculptingBrushProjection = brushProj;

                sculptingBrushProjectionAutomaticallyCreated = true;
            }

            yield return base.Start();

            if (!Application.isPlaying)
                yield break;

            if (!sculptingCameraCache)
            {
                if (sculptingMobileSupport || (!sculptingMobileSupport && sculptingFromCursor))
                {
                    sculptingCameraCache = Camera.main;
                    if (!sculptingCameraCache)
                        Debug.LogError("There is no main camera assigned!");
                }
            }

            if (ThreadUseMultithreading)
            {
                MDModifierThreading_StartThread();
                ThreadEvent?.Reset();
            }
        }

        /// <summary>
        /// Base modifier initialization
        /// </summary>
        protected override void MDModifier_InitializeBase(MeshReferenceType meshReferenceType = MeshReferenceType.GetFromPreferences, bool forceInitialization = false, bool affectUpdateEveryFrameField = true)
        {
            if (MbIsInitialized && !forceInitialization)
                return;

            base.MDModifier_InitializeBase(meshReferenceType, forceInitialization, affectUpdateEveryFrameField);

            MDModifier_InitializeMeshData();

            if (!GetComponent<Collider>())
                mc = gameObject.AddComponent<MeshCollider>();

            // This modifier supports using the multithreading right in the editor
            ThreadEditorThreadSupported = true;
        }

        /// <summary>
        /// Process the Mesh Effector base update function (use 'Effector_UpdateMesh' method for more customized setting)
        /// </summary>
        public override void MDModifier_ProcessModifier()
        {
            if (!Application.isPlaying)
                return;
            if (!sculptingUseInput)
                return;
            if (!MbIsInitialized)
                return;
            if (!MbWorkingMeshData.MbDataInitialized())
                return;

            buttonBacked_Raise.SyncButton(InputHook_Raise);
            buttonBacked_Lower.SyncButton(InputHook_Lower);
            buttonBacked_Revert.SyncButton(InputHook_Revert);
            buttonBacked_Noise.SyncButton(InputHook_Noise);
            buttonBacked_Smooth.SyncButton(InputHook_Smooth);
            buttonBacked_Stylize.SyncButton(InputHook_Stylize);

            bool eventSystemOverObject = false;
            if (EventSystem.current != null)
                eventSystemOverObject = EventSystem.current.IsPointerOverGameObject();

            Ray r = new Ray();
            if (sculptingCameraCache)
            {
                if (!sculptingMobileSupport)
                {
                    if (sculptingFromCursor && !sculptingVRInput)
                        r = sculptingCameraCache.ScreenPointToRay(InputHook_CursorScreenPosition);
                    else
                        r = new Ray(sculptingOriginTransform.transform.position, sculptingOriginTransform.transform.forward);
                }
                else
                    r = sculptingCameraCache.ScreenPointToRay(InputHook_CursorScreenPosition);
            }

            if (Physics.RaycastNonAlloc(r, nonAllocRaycastStorage, Mathf.Infinity, sculptingLayerMask) > 0 && !eventSystemOverObject)
            {
                RaycastHit hit = nonAllocRaycastStorage[0];
                if (hit.collider.gameObject == gameObject)
                {
                    Internal_SetActive(sculptingBrushProjection, true);
                    sculptingBrushProjection.transform.position = hit.point;
                    sculptingBrushProjection.transform.rotation = Quaternion.FromToRotation(-Vector3.forward, hit.normal);
                    sculptingBrushProjection.transform.localScale = new Vector3(sculptingBrushSize, sculptingBrushSize, sculptingBrushSize);

                    Internal_ManageControls(hit);
                }
                else
                    Internal_SetActive(sculptingBrushProjection, false);
            }
            else
                Internal_SetActive(sculptingBrushProjection, false);

            if (buttonBacked_Raise.IsUp && sculptingUseRaiseFunct)
                Sculpting_RecordControlUp();
            if (buttonBacked_Lower.IsUp && sculptingUseLowerFunct)
                Sculpting_RecordControlUp();
            if (buttonBacked_Revert.IsUp && sculptingUseRevertFunct)
                Sculpting_RecordControlUp();
            if (buttonBacked_Noise.IsUp && sculptingUseNoiseFunct)
                Sculpting_RecordControlUp();
            if (buttonBacked_Smooth.IsUp && sculptingUseSmoothFunct)
                Sculpting_RecordControlUp();
            if (buttonBacked_Stylize.IsUp && sculptingUseStylizeFunct)
                Sculpting_RecordControlUp();

            buttonBacked_Raise.ResetButton(false);
            buttonBacked_Lower.ResetButton(false);
            buttonBacked_Revert.ResetButton(false);
            buttonBacked_Noise.ResetButton(false);
            buttonBacked_Smooth.ResetButton(false);
            buttonBacked_Stylize.ResetButton(false);
        }

        #endregion

        protected override void Update()
        {
            if(sculptingUseInput)
                base.Update();
        }

        private void Internal_SetActive(GameObject target, bool state)
        {
            if (!sculptingUseBrushProjection)
            {
                if(target.activeSelf != sculptingUseBrushProjection)
                    target.SetActive(false);
                return;
            }

            if (target != null && target.activeSelf != state)
                target.SetActive(state);
        }

        private void Internal_ManageControls(RaycastHit hit)
        {
            if(!sculptingMobileSupport && !sculptingVRInput)
            {
                if (Internal_GetControlInput(InputHook_Raise) && sculptingUseRaiseFunct)
                {
                    Sculpting_DoSculpting(hit.point, sculptingBrushProjection.transform.forward, sculptingBrushSize, sculptingBrushIntensity, BrushStatus.Raise);
                    if (!sculptingUpdateColliderAfterRelease)
                        Sculpting_RefreshMeshCollider();
                }
                else if (Internal_GetControlInput(InputHook_Lower) && sculptingUseLowerFunct)
                {
                    Sculpting_DoSculpting(hit.point, sculptingBrushProjection.transform.forward, sculptingBrushSize, sculptingBrushIntensity, BrushStatus.Lower);
                    if (!sculptingUpdateColliderAfterRelease)
                        Sculpting_RefreshMeshCollider();
                }
                else if (Internal_GetControlInput(InputHook_Revert) && sculptingUseRevertFunct)
                {
                    Sculpting_DoSculpting(hit.point, sculptingBrushProjection.transform.forward, sculptingBrushSize, sculptingBrushIntensity, BrushStatus.Revert);
                    if (!sculptingUpdateColliderAfterRelease)
                        Sculpting_RefreshMeshCollider();
                }
                else if (Internal_GetControlInput(InputHook_Noise) && sculptingUseNoiseFunct)
                {
                    Sculpting_DoSculpting(hit.point, sculptingBrushProjection.transform.forward, sculptingBrushSize, sculptingBrushIntensity, BrushStatus.Noise);
                    if (!sculptingUpdateColliderAfterRelease)
                        Sculpting_RefreshMeshCollider();
                }
                else if (Internal_GetControlInput(InputHook_Smooth) && sculptingUseSmoothFunct)
                {
                    Sculpting_DoSculpting(hit.point, sculptingBrushProjection.transform.forward, sculptingBrushSize, sculptingBrushIntensity, BrushStatus.Smooth);
                    if (!sculptingUpdateColliderAfterRelease)
                        Sculpting_RefreshMeshCollider();
                }
                else if (Internal_GetControlInput(InputHook_Stylize) && sculptingUseStylizeFunct)
                {
                    Sculpting_DoSculpting(hit.point, sculptingBrushProjection.transform.forward, sculptingBrushSize, sculptingBrushIntensity, BrushStatus.Stylize);
                    if (!sculptingUpdateColliderAfterRelease)
                        Sculpting_RefreshMeshCollider();
                }
            }
            else
            {
                if (genericInputDown)
                {
                    Sculpting_DoSculpting(hit.point, sculptingBrushProjection.transform.forward, sculptingBrushSize, sculptingBrushIntensity, sculptingBrushStatus);
                    if (!sculptingUpdateColliderAfterRelease)
                        Sculpting_RefreshMeshCollider();
                }
            }
        }

        private bool Internal_GetControlInput(bool key)
        {
            if (key) 
                Sculpting_RecordControlDown();

            return key;
        }

        #region Essential Functions

        private void Sculpting_UpdateMesh()
        {
            MbMeshFilter.sharedMesh.vertices = MbWorkingMeshData.vertices;
            if(!sculptingRecalculateNormalsOnRelease)
                MDMeshBase_RecalculateMesh();
        }

        private readonly System.Random privRandom = new System.Random();
        private void Sculpting_ProcessSculpting()
        {
            if (threadWrapper.mth_WorldScale == Vector3.zero)
                return;

            if (threadWrapper.mth_State == BrushStatus.Smooth)
            {
                Vector3[] smoothVerts = null;
                switch (threadWrapper.mth_SmoothingType)
                {
                    case 0:
                        smoothVerts = MD_Utilities.VertexSmoothingHCFilter.HCFilter(MbWorkingMeshData.vertices, MbWorkingMeshData.triangles, new SculptingBrushData()
                        {
                            radius = threadWrapper.mth_Radius,
                            matPos = threadWrapper.mth_WorldPos,
                            matRot = threadWrapper.mth_WorldRot,
                            matScale = threadWrapper.mth_WorldScale,
                            worldSpacePosition = threadWrapper.mth_WorldPoint
                        });
                        break;
                    case 1:
                        smoothVerts = MD_Utilities.VertexSmoothingLaplacianFilter.LaplacianFilter(MbWorkingMeshData.vertices, MbWorkingMeshData.triangles, threadWrapper.mth_SmoothIntensity, new SculptingBrushData()
                        {
                            radius = threadWrapper.mth_Radius,
                            matPos = threadWrapper.mth_WorldPos,
                            matRot = threadWrapper.mth_WorldRot,
                            matScale = threadWrapper.mth_WorldScale,
                            worldSpacePosition = threadWrapper.mth_WorldPoint
                        });
                        break;
                }
                if (smoothVerts != null)
                    for (int i = 0; i < smoothVerts.Length; i++)
                        MbWorkingMeshData.vertices[i] = smoothVerts[i];
            }
            else
            {
                int i = 0;
                while (i < MbWorkingMeshData.vertices.Length)
                {
                    Vector3 wp = MD_Utilities.Transformations.TransformPoint(threadWrapper.mth_WorldPos, threadWrapper.mth_WorldRot, threadWrapper.mth_WorldScale, MbWorkingMeshData.vertices[i]);
                    if (Vector3.Distance(wp, threadWrapper.mth_WorldPoint) < threadWrapper.mth_Radius)
                    {
                        if (threadWrapper.mth_State == BrushStatus.Stylize)
                        {
                            Vector3 v = Vector3.positiveInfinity;
                            float minD = Mathf.Infinity;
                            for (int x = 0; x < MbWorkingMeshData.vertices.Length; x++)
                            {
                                if (x == i) continue;
                                float dist = Vector3.Distance(MbWorkingMeshData.vertices[x], MbWorkingMeshData.vertices[i]);
                                if (dist < minD)
                                {
                                    minD = dist;
                                    v = MbWorkingMeshData.vertices[x];
                                }
                            }
                            Vector3 ttt = MD_Utilities.Math3D.CustomLerp(MbWorkingMeshData.vertices[i], v, threadWrapper.mth_StylizeIntensity);
                            MbWorkingMeshData.vertices[i] = ttt;
                            i++;
                            continue;
                        }

                        float str = threadWrapper.mth_Strength;
                        if (threadWrapper.mth_State == BrushStatus.Lower)
                            str *= -1;

                        if (threadWrapper.mth_State == BrushStatus.Revert)
                            MbWorkingMeshData.vertices[i] = Vector3.Lerp(MbWorkingMeshData.vertices[i], MbBackupMeshData.vertices[i], 0.1f);

                        Vector3 wpStorage = wp;

                        if (threadWrapper.mth_State != BrushStatus.Revert)
                        {
                            Vector3 Dir = threadWrapper.mth_Direction;

                            if (sculptingMode == SculptingMode.CustomDirection)
                                Dir = sculptingCustomDirection;
                            else if (sculptingMode == SculptingMode.VerticesDirection)
                                Dir = MD_Utilities.Transformations.TransformDirection(threadWrapper.mth_WorldPos, threadWrapper.mth_WorldRot, threadWrapper.mth_WorldScale, -MbWorkingMeshData.vertices[i]);

                            if (sculptingEnableHeightLimitations)
                            {
                                if (wp.y < sculptingHeightLimitations.x && threadWrapper.mth_State == BrushStatus.Lower)
                                    str = 0;
                                if (wp.y > sculptingHeightLimitations.y && threadWrapper.mth_State == BrushStatus.Raise)
                                    str = 0;
                            }

                            if (threadWrapper.mth_State == BrushStatus.Noise)
                            {
                                float rand_x = (float)GetRandomNumber(-0.01f, 0.01f);
                                float rand_y = (float)GetRandomNumber(-0.01f, 0.01f);
                                float rand_z = (float)GetRandomNumber(-0.01f, 0.01f);
                                switch (threadWrapper.mth_NoiseDirs)
                                {
                                    case NoiseTypes.X:
                                        wp.x += rand_x * str;
                                        break;
                                    case NoiseTypes.Y:
                                        wp.y += rand_y * str;
                                        break;
                                    case NoiseTypes.Z:
                                        wp.z += rand_z * str;
                                        break;

                                    case NoiseTypes.XY:
                                        wp.x += rand_x * str;
                                        wp.y += rand_y * str;
                                        break;
                                    case NoiseTypes.XZ:
                                        wp.x += rand_x * str;
                                        wp.z += rand_z * str;
                                        break;

                                    case NoiseTypes.YZ:
                                        wp.y += rand_y * str;
                                        wp.z += rand_z * str;
                                        break;
                                    case NoiseTypes.XYZ:
                                        wp.x += rand_x * str;
                                        wp.y += rand_y * str;
                                        wp.z += rand_z * str;
                                        break;
                                    case NoiseTypes.Centrical:
                                        float ran = (float)GetRandomNumber(-0.01f, 0.01f);
                                        Vector3 v = (wp - threadWrapper.mth_WorldPos) * ran;
                                        wp.x += v.x * str;
                                        wp.y += v.y * str;
                                        wp.z += v.z * str;
                                        break;
                                }
                            }
                            else
                            {
                                str *= 0.05f;
                                if (threadWrapper.SS_SculptingType == InterpolationType.Exponential)
                                    str *= -(threadWrapper.mth_Radius - Vector3.Distance(wp, threadWrapper.mth_WorldPoint));
                                else
                                    str *= -1;
                                wp += Dir * str;
                            }

                            if (sculptingEnableDistanceLimitations)
                            {
                                Vector3 cur = MbWorkingMeshData.vertices[i];
                                Vector3 stor = MbBackupMeshData.vertices[i];
                                float curDist = Vector3.Distance(cur, stor);
                                bool inrange = (curDist > sculptingDistanceLimitation);
                                if (inrange && Vector3.Distance(cur + (Dir * str), stor) > curDist)
                                    wp = wpStorage;
                            }

                            MbWorkingMeshData.vertices[i] = MD_Utilities.Transformations.TransformPointInverse(threadWrapper.mth_WorldPos, threadWrapper.mth_WorldRot, threadWrapper.mth_WorldScale, wp);
                        }
                    }
                    i++;
                }
            }
            double GetRandomNumber(double minimum, double maximum)
            {
                return privRandom.NextDouble() * (maximum - minimum) + minimum;
            }
        }

        /// <summary>
        /// Sculpt current mesh by specific parameters
        /// </summary>
        public void Sculpting_DoSculpting(Vector3 worldPosition, Vector3 directionLocal, float radius, float intensity, BrushStatus brushState)
        {
            if (brushState == BrushStatus.None)
                return;

            if (sculptingMode == SculptingMode.CustomDirectionObject)
            {
                if (sculptingCustomDirObjDirection == CustomDirObjDirection.Up)
                    directionLocal = sculptingCustomDirectionObject.transform.up;
                else if (sculptingCustomDirObjDirection == CustomDirObjDirection.Down)
                    directionLocal = -sculptingCustomDirectionObject.transform.up;
                else if (sculptingCustomDirObjDirection == CustomDirObjDirection.Forward)
                    directionLocal = sculptingCustomDirectionObject.transform.forward;
                else if (sculptingCustomDirObjDirection == CustomDirObjDirection.Back)
                    directionLocal = -sculptingCustomDirectionObject.transform.forward;
                else if (sculptingCustomDirObjDirection == CustomDirObjDirection.Right)
                    directionLocal = sculptingCustomDirectionObject.transform.right;
                else if (sculptingCustomDirObjDirection == CustomDirObjDirection.Left)
                    directionLocal = -sculptingCustomDirectionObject.transform.right;
            }
            threadWrapper.SetParams(worldPosition, radius, intensity, directionLocal, brushState, transform.position, transform.localScale, transform.rotation, sculptingNoiseTypes, sculptingInterpolationType, sculptingStylizeIntensity, sculptingSmoothIntensity, (int)sculptingSmoothingType);

            if (ThreadUseMultithreading)
            {
                if (ThreadIsDone) 
                    Sculpting_UpdateMesh();
                return;
            }

            Sculpting_ProcessSculpting();
            Sculpting_UpdateMesh();
        }

        #endregion

        /// <summary>
        /// Main separate thread worker for this modifier
        /// </summary>
        public void MDThreading_ProcessThreadWorker()
        {
            while (true)
            {
                ThreadIsDone = false;
                ThreadEvent?.WaitOne();

                Sculpting_ProcessSculpting();

                ThreadIsDone = true;
                Thread.Sleep(ThreadSleep);
            }
        }

        public override void MDModifierThreading_StartThread(string threadName = "DefaultMDThread")
        {
            base.MDModifierThreading_StartThread(threadName);
            ThreadEvent?.Reset();
        }

        #region Public Methods

        // Input Controls

        /// <summary>
        /// Capture & record any button control UP/has been released to update mesh collider, recalculate bounds/normals and pause thread (if possible)
        /// </summary>
        public void Sculpting_RecordControlUp()
        {
            if (!controlInputDown)
                return;

            controlInputDown = false;

            if (sculptingUpdateColliderAfterRelease)
                Sculpting_RefreshMeshCollider();
            if (sculptingRecalculateNormalsOnRelease)
                MDMeshBase_RecalculateMesh();
            if (ThreadUseMultithreading)
                ThreadEvent?.Reset();
        }

        /// <summary>
        /// Capture & record any button control DOWN/has been pressed to record history and resume thread (if possible)
        /// </summary>
        public void Sculpting_RecordControlDown()
        {
            if (controlInputDown) 
                return;

            controlInputDown = true;

            if (sculptingRecordHistory)
                Sculpting_RecordHistory();
            if (ThreadUseMultithreading)
                ThreadEvent?.Set();
        }

        // History Management

        /// <summary>
        /// Record current vertex positions to the history
        /// </summary>
        public void Sculpting_RecordHistory()
        {
            if (SculptingHistoryRecords == null)
                SculptingHistoryRecords = new List<HistoryRecords>();

            if (SculptingHistoryRecords.Count > sculptingMaxHistoryRecords)
                SculptingHistoryRecords.RemoveAt(0);

            HistoryRecords h = new HistoryRecords() { historyNotes = "History " + SculptingHistoryRecords.Count.ToString(), vertexPositions = new Vector3[MbWorkingMeshData.vertices.Length] };
            System.Array.Copy(MbWorkingMeshData.vertices, h.vertexPositions, MbWorkingMeshData.vertices.Length);
            SculptingHistoryRecords.Add(h);
        }

        /// <summary>
        /// Make a step forward/backward in the history by the specified 'jumpToRecord' index. Type -1 for default = jump to the latest history record
        /// </summary>
        public void Sculpting_Undo(int jumpToRecordIndex)
        {
            if (SculptingHistoryRecords == null) return;
            if (SculptingHistoryRecords.Count == 0) return;

            int ind = jumpToRecordIndex == -1 ? SculptingHistoryRecords.Count - 1 : Mathf.Clamp(jumpToRecordIndex, 0, SculptingHistoryRecords.Count - 1);

            Vector3[] verts = SculptingHistoryRecords[ind].vertexPositions;
            MbMeshFilter.sharedMesh.SetVertices(verts);
            MDModifier_InitializeMeshData(false, false);
            MDMeshBase_RecalculateMesh();
            Sculpting_RefreshMeshCollider();
            SculptingHistoryRecords.RemoveAt(ind);
        }

        /// <summary>
        /// Default undo method - make a step backward by ^1
        /// </summary>
        public void Sculpting_Undo()
        {
            Sculpting_Undo(-1);
        }

        // Mesh Collider Refresh & Bake

        private MeshCollider mc;
        /// <summary>
        /// Refresh mesh collider at runtime
        /// </summary>
        public void Sculpting_RefreshMeshCollider()
        {
            if (!MbMeshFilter)
                return;

            if (!mc) mc = GetComponent<MeshCollider>();
            if (!mc) mc = gameObject.AddComponent<MeshCollider>();
            
            mc.sharedMesh = MbMeshFilter.sharedMesh;
        }

        // Public available methods for essential parameters such as Size, Intensity, Stylize Intensity, Smoothing Intensity etc

        /// <summary>
        /// Change brush size by Slider value
        /// </summary>
        /// <param name="size"></param>
        public void Sculpting_ChangeBrushSize(UnityEngine.UI.Slider size)
        {
            sculptingBrushSize = size.value;
        }

        /// <summary>
        /// Change brush strength by Slider value
        /// </summary>
        public void Sculpting_ChangeBrushIntensity(UnityEngine.UI.Slider strength)
        {
            sculptingBrushIntensity = strength.value;
        }

        /// <summary>
        /// Change stylize intensity by Slider value
        /// </summary>
        public void Sculpting_ChangeStylizeIntensity(UnityEngine.UI.Slider intens)
        {
            sculptingStylizeIntensity = intens.value;
        }

        /// <summary>
        /// Change smooth intensity by Slider value
        /// </summary>
        public void Sculpting_ChangeSmoothIntensity(UnityEngine.UI.Slider intens)
        {
            sculptingSmoothIntensity = intens.value;
        }


        /// <summary>
        /// Change brush state by index value [None = 0, Raise = 1, Lower = 2, Revert = 3, Noise = 4, Smooth = 5, Stylize = 6]
        /// </summary>
        public void Sculpting_ChangeBrushStatus(int stateIndex)
        {
            sculptingBrushStatus = (BrushStatus)stateIndex;
        }

        /// <summary>
        /// Change brush state by index value [None = 0, Raise = 1, Lower = 2, Revert = 3, Noise = 4, Smooth = 5, Stylize = 6]
        /// </summary>
        public void Sculpting_ChangeBrushStatus(BrushStatus state)
        {
            sculptingBrushStatus = state;
        }

        /// <summary>
        /// Change sculpting mode by index value [VerticesDirection = 0, BrushDirection = 1, CustomDirection = 2, CustomDirectionObject = 3, InternalScriptDirection = 4]
        /// </summary>
        public void Sculpting_ChangeSculptingMode(int modeIndex)
        {
            sculptingMode = (SculptingMode)modeIndex;
        }

        /// <summary>
        /// Change sculpting mode by index value [VerticesDirection = 0, BrushDirection = 1, CustomDirection = 2, CustomDirectionObject = 3, InternalScriptDirection = 4]
        /// </summary>
        public void Sculpting_ChangeSculptingMode(SculptingMode mode)
        {
            sculptingMode = mode;
        }



        /// <summary>
        /// Setup essential sculpting fields manually (This is helpful if you don't wont to go through field-by-field)
        /// </summary>
        public void Sculpting_SetupEssentials(float radius, float intensity, bool showBrush, Vector3 brushPosition, Vector3 brushRotationDirection, BrushStatus status, SculptingMode mode)
        {
            sculptingBrushSize = radius;
            sculptingBrushIntensity = intensity;
            sculptingBrushProjection.transform.position = brushPosition;
            sculptingBrushProjection.transform.rotation = Quaternion.FromToRotation(-Vector3.forward, brushRotationDirection);
            sculptingBrushProjection.transform.localScale = new Vector3(sculptingBrushSize, sculptingBrushSize, sculptingBrushSize);
            if (sculptingBrushProjection && sculptingUseBrushProjection)
                    sculptingBrushProjection.SetActive(showBrush);
            Sculpting_ChangeSculptingMode(mode);
            Sculpting_ChangeBrushStatus(status);
        }

        #endregion
    }
}