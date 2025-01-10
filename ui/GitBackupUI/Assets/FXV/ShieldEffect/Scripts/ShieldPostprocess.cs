using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor;
using System;

namespace FXV
{
#if UNITY_EDITOR
    [ExecuteInEditMode]
    [InitializeOnLoad]
#endif
    [RequireComponent(typeof(Camera))]
    public class ShieldPostprocess : MonoBehaviour
    {
        static List<ShieldPostprocess> instances;

        internal enum GiNumSmplesCount
        {
            Low = 0,
            Medium,
            High
        };

        internal enum BlurMethod
        {
            GaussianAndBox = 0,
            GaussianOnly
        };

        internal enum BuiltInDrawOrder
        {
            AfterOpaque = 0,
            AfterTransparents
        };

        internal class CameraBufferEvent
        {
            public CommandBuffer cmd;
            public CameraEvent camEvent;
        };

        internal class PostprocessContext
        {
            public RenderTargetIdentifier currentTarget;
            public RenderTargetIdentifier[] tempTarget;
            public RenderTargetIdentifier ssgiTarget;
            public RenderTargetIdentifier ssgiTarget2;
            public RenderTargetIdentifier positionFrontTarget;
            public RenderTargetIdentifier positionBackTarget;
            public Vector2[] tmpTargetSizes;
            public int downSampleSteps = 0;
            public float downSampleRate = 0.0f;
            public int targetWidth;
            public int targetHeight;
            public Matrix4x4 viewProjectInverse;
            public bool blitToScreen = true;
            public Mesh fullScreenMesh = null;
            public bool isTwoEyeVR = false;
            public bool isDeferred = false;
        }

        internal class InstancedPropertyArray
        {
            public InstancedPropertyArray(int propId) => propertyId = propId;

            public int propertyId;

            public virtual void SetTo(MaterialPropertyBlock properties)
            {

            }
        }

        internal class InstancedPropertyArrayFloat : InstancedPropertyArray
        {
            public InstancedPropertyArrayFloat(int propId) : base(propId)
            {
            }

            public float[] values = new float[256];

            public override void SetTo(MaterialPropertyBlock properties)
            {
                properties.SetFloatArray(propertyId, values);
            }
        }

        internal class InstancedPropertyArrayVector : InstancedPropertyArray
        {
            public InstancedPropertyArrayVector(int propId) : base(propId)
            {
            }

            public Vector4[] values = new Vector4[256];

            public override void SetTo(MaterialPropertyBlock properties)
            {
                properties.SetVectorArray(propertyId, values);
            }
        }

        internal class RenderInstanceInfo
        {
            public Mesh mesh;
            public int submeshIndex = 0;
            public Material material;
            public int shaderPass = 0;
            public Matrix4x4[] matrices = new Matrix4x4[256];
            public MaterialPropertyBlock properties = new MaterialPropertyBlock();
            public InstancedPropertyArray[] propertyArrays = new InstancedPropertyArray[16];
            int propertyArraysCount = 0;

            public int matricesCount = 0;

            internal void SetProperty(int arrayId, int propertyIndex, float value)
            {
                ((InstancedPropertyArrayFloat)propertyArrays[propertyIndex]).values[arrayId] = value;
            }

            internal void SetProperty(int arrayId, int propertyIndex, Vector4 value)
            {
                ((InstancedPropertyArrayVector)propertyArrays[propertyIndex]).values[arrayId] = value;
            }

            internal void RegisterFloatProperyArray(int index, int propId)
            {
                propertyArrays[index] = new InstancedPropertyArrayFloat(propId);
                propertyArraysCount = index+1;
            }

            internal void RegisterVectorProperyArray(int index, int propId)
            {
                propertyArrays[index] = new InstancedPropertyArrayVector(propId);
                propertyArraysCount = index+1;
            }

            internal void UpdateMaterialPropertyBlock()
            {
                for (int i = 0; i < propertyArraysCount; ++i)
                {
                    propertyArrays[i].SetTo(properties);
                }
            }

            internal bool IsFull()
            {
                return matricesCount >= matrices.Length;
            }
        }

        private static readonly int OPAQUE_TEXTURE_FOR_REFRACTION_ID = Shader.PropertyToID("_CameraOpaqueTextureBuiltin");

        private static string BUILTIN_GRAB_CMD_BUFFER_NAME = "ShieldPostprocess_Grab";
        private static string BUILTIN_DRAW_CMD_BUFFER_NAME = "ShieldPostprocess_Draw";
        

        [fxvHeader("Postprocess Params")]
        [Tooltip("Postproces draw order in built in render pipeline."), SerializeField]
        internal BuiltInDrawOrder drawOrder = BuiltInDrawOrder.AfterTransparents;

        [Tooltip("Postprocess blur function - select the one ."), SerializeField]
        internal BlurMethod blurMethod = BlurMethod.GaussianAndBox;

        [Tooltip("Multiplier of postprocess intensity."), SerializeField, Range(0.001f, 10.0f)]
        internal float postprocessPower = 3.0f;

        [SerializeField, Range(1, 8)]
        [Tooltip("Number of blur iterations.")]
        internal int numberOfIterations = 4;

        [SerializeField, Range(0.0f, 0.5f)]
        [Tooltip("Number of blur iterations.")]
        internal float downSampleRate = 0.3f;

        [SerializeField]
        bool separateAxes = false;

        [SerializeField, Range(1, 7)]
        [Tooltip("Blur kernel radius.")]
        int kernelRadius = 5;
        [SerializeField, Range(0.1f, 10.0f)]
        [Tooltip("Blur shape parameters.")]
        float sigma = 4.0f;
        [SerializeField, Range(0.1f, 2.0f)]
        [Tooltip("Sampling step factor.")]
        float sampleStep = 1.0f;

        [SerializeField, Range(1, 7)]
        [Tooltip("Blur kernel radius on vertical axis.")]
        int kernelRadiusVertical = 5;
        [SerializeField, Range(0.1f, 10.0f)]
        [Tooltip("Blur shape parameters on vertical axis.")]
        float sigmaVertical = 4.0f;
        [SerializeField, Range(0.1f, 2.0f)]
        [Tooltip("Sampling step factor on vertical axis.")]
        float sampleStepVertical = 1.0f;

        float[] gauss_coeff_H;
        float[] gauss_coeff_V;

        [SerializeField]
        [fxvHeader("Global Illumination (experimental, deferred only)")]
        [Tooltip("Enable global illumination from shield objects (experimental, deferred only).")]
        internal bool globalIllumination = false;

        [SerializeField, Range(0.1f, 7.0f)]
        [Tooltip("")]
        internal float giSampleRadius = 2.5f;

        [SerializeField]
        [Tooltip("")]
        internal GiNumSmplesCount giNumberOfSamples = GiNumSmplesCount.High;

        [SerializeField, Range(0.01f, 2.0f)]
        [Tooltip("")]
        internal float giIntensity = 0.35f;

        [SerializeField, Range(0.1f, 10.0f)]
        [Tooltip("")]
        internal float giLightRange = 7.0f;

        [SerializeField, Range(0.0f, 10.0f)]
        [Tooltip("Fade out gi on geometry directly behind shield")]
        internal float giFadeBehind = 4.0f;

        [SerializeField]
        [Tooltip("")]
        internal float giDenoiseStepWidth = 2.5f;

        [SerializeField]
        [Tooltip("")]
        internal float giDenoiseStepChange = 0.7f;

        [SerializeField, Range(0, 8)]
        [Tooltip("")]
        internal int giDenoiseIterations = 5;

        [SerializeField, Range(0, 4)]
        [Tooltip("")]
        internal int giAtIteration = 1;

        internal bool forceDisableGI = false;

        private Material blendAddMaterial;
        private Material postprocessMaterial;

        private Texture2D normalNoiseTexture;

        private List<fxvRenderObject> allRenderObjects = new List<fxvRenderObject>();

        private Dictionary<int, RenderInstanceInfo> renderInstancingInfos = new Dictionary<int, RenderInstanceInfo>();

        private List<fxvRenderObject.RenderChunk> renderInstancedChunks = new List<fxvRenderObject.RenderChunk>();
        private List<fxvRenderObject.RenderChunk> renderChunks = new List<fxvRenderObject.RenderChunk>();

        private Color clearToTransparentColor;

        private Camera myCamera;

        private CameraEvent postprocessGrabOpaqueEvent = CameraEvent.AfterForwardOpaque;

        internal static ShieldPostprocess GetMainInstance()
        {
#if UNITY_EDITOR
            if (instances == null)
            {
                instances = new List<ShieldPostprocess>(FindObjectsByType<ShieldPostprocess>(FindObjectsSortMode.None));
            }
#endif

            if (instances == null)
            {
                return null;
            }

            Transform camTrans = (Camera.main != null) ? Camera.main.transform : null;

            if (camTrans)
            {
                for (int i = 0; i < instances.Count; ++i)
                {
                    if (instances[i].transform == camTrans)
                    {
                        return instances[i];
                    }
                }
            }

            return instances.Count > 0 ? instances[0] : null;
        }

        internal static ShieldPostprocess GetInstance(Camera cam)
        {
#if UNITY_EDITOR
            if (instances == null)
            {
                instances = new List<ShieldPostprocess>(FindObjectsByType<ShieldPostprocess>(FindObjectsSortMode.None));
            }
#endif

            if (instances == null)
            {
                return null;
            }

            Transform camTrans = (cam != null) ? cam.transform : null;

            if (camTrans)
            {
                for (int i = 0; i < instances.Count; ++i)
                {
                    if (instances[i].transform == camTrans)
                    {
                        return instances[i];
                    }
                }
            }

            return instances.Count > 0 ? instances[0] : null;
        }

        internal static void OnPipelineChanged()
        {
#if UNITY_EDITOR
            if (instances == null)
            {
                instances = new List<ShieldPostprocess>(FindObjectsByType<ShieldPostprocess>(FindObjectsSortMode.None));
            }
#endif

            if (instances == null)
            {
                return;
            }

            for (int i = 0; i < instances.Count; ++i)
            {
                instances[i].Prepare();
            }
        }

        internal static int GetMaxDownSampleSteps()
        {
#if UNITY_EDITOR
            if (instances == null)
            {
                instances = new List<ShieldPostprocess>(FindObjectsByType<ShieldPostprocess>(FindObjectsSortMode.None));
            }
#endif

            if (instances == null)
            {
                return 1;
            }

            int maxSteps = 0;
            for (int i = 0; i < instances.Count; ++i)
            {
                maxSteps = Mathf.Max(instances[i].numberOfIterations, maxSteps);
            }

            return maxSteps;
        }

        public static void AddShield(Shield s)
        {
#if UNITY_EDITOR
            if (instances == null)
            {
                instances = new List<ShieldPostprocess>(FindObjectsByType<ShieldPostprocess>(FindObjectsSortMode.None));
            }
#endif

            if (instances == null)
            {
                return;
            }

            for (int i = 0; i < instances.Count; ++i)
            {
                instances[i].TryAddRenderObject(s);
            }
        }

        public static void RemoveShield(Shield s)
        {
#if UNITY_EDITOR
            if (instances == null)
            {
                instances = new List<ShieldPostprocess>(FindObjectsByType<ShieldPostprocess>(FindObjectsSortMode.None));
            }
#endif

            if (instances == null)
            {
                return;
            }

            for (int i = 0; i < instances.Count; ++i)
            {
                instances[i].TryRemoveRenderObject(s);
            }
        }

        public static void AddRenderObject(fxvRenderObject ro)
        {
#if UNITY_EDITOR
            if (instances == null)
            {
                instances = new List<ShieldPostprocess>(FindObjectsByType<ShieldPostprocess>(FindObjectsSortMode.None));
            }
#endif

            if (instances == null)
            {
                return;
            }

            for (int i = 0; i < instances.Count; ++i)
            {
                instances[i].TryAddRenderObject(ro);
            }
        }

        public static void RemoveRenderObject(fxvRenderObject ro)
        {
#if UNITY_EDITOR
            if (instances == null)
            {
                instances = new List<ShieldPostprocess>(FindObjectsByType<ShieldPostprocess>(FindObjectsSortMode.None));
            }
#endif

            if (instances == null)
            {
                return;
            }

            for (int i = 0; i < instances.Count; ++i)
            {
                instances[i].TryRemoveRenderObject(ro);
            }
        }

        public static void UpdateAllObjects()
        {
#if UNITY_EDITOR
            if (instances == null)
            {
                instances = new List<ShieldPostprocess>(FindObjectsByType<ShieldPostprocess>(FindObjectsSortMode.None));
            }
#endif

            if (instances == null)
            {
                return;
            }

            ShieldPostprocess[] postporcess = UnityEngine.Object.FindObjectsByType<ShieldPostprocess>(FindObjectsSortMode.None);
            for (int i = 0; i < postporcess.Length; ++i)
            {
                postporcess[i]._UpdateAllObjects();
            }
        }

        void Awake()
        {
            Prepare();

            if (instances == null)
            {
                instances = new List<ShieldPostprocess>();
            }

            instances.Add(this);
        }

        void Prepare()
        {
            myCamera = GetComponent<Camera>();

            context = null;

            if (blendAddMaterial == null)
            {
                blendAddMaterial = new Material(Shader.Find("Hidden/FXV/FXVPostprocessBlitAdd"));
            }
            blendAddMaterial.SetFloat("_ColorMultiplier", postprocessPower);

            if (postprocessMaterial == null)
            {
                postprocessMaterial = new Material(Shader.Find("Hidden/FXV/FXVPostprocessShield"));
                UpdateSSGIPostprocessMaterial();
            }

            if (normalNoiseTexture == null)
            {
                normalNoiseTexture = Resources.Load<Texture2D>("Textures/RandomNoise");
            }

            ssgiSampleDirections = new float[128];
            GenerateSSGIRayDirections();

            UpdateGaussianCoeff();

            clearToTransparentColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

            Camera.onPreRender -= RenderWithCamera;
            Camera.onPostRender -= AfterRenderWithCamera;

            if (fxvShieldAssetConfig.ActiveRenderPipeline == fxvShieldAssetConfig.Pipeline.FXV_SHIELD_BUILTIN)
            {
                Camera.onPreRender += RenderWithCamera;
                Camera.onPostRender += AfterRenderWithCamera;
            }
        }

        void GenerateSSGIRayDirections()
        {
            int numVecs = ssgiSampleDirections.Length / 2;
            for (int s = 0; s < numVecs; ++s)
            {
                // Vector2 dir = UnityEngine.Random.insideUnitCircle;
                //dir.Normalize();
                float angle = 2.0f * s * (Mathf.PI / numVecs);
                Vector2 dir = new Vector3(MathF.Sin(angle), Mathf.Cos(angle));
                ssgiSampleDirections[s * 2] = dir.x;
                ssgiSampleDirections[s * 2 + 1] = dir.y;
            }
        }

        internal void DestroyAsset(UnityEngine.Object assetObject)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(assetObject);
            }
            else
#endif
            {
                Destroy(assetObject);
            }
        }

        private void OnDestroy()
        {
            if (instances != null)
            {
                instances.Remove(this);
            }

            Camera.onPreRender -= RenderWithCamera;
            Camera.onPostRender -= AfterRenderWithCamera;

            DestroyBuffers(shieldCommandBuffers);
            DestroyBuffers(grabCommandBuffers);

            if (blendAddMaterial)
            {
                DestroyAsset(blendAddMaterial);

                blendAddMaterial = null;
            }
            if (postprocessMaterial)
            {
                DestroyAsset(postprocessMaterial);

                postprocessMaterial = null;
            }

            if (normalNoiseTexture == null)
            {
                DestroyAsset(normalNoiseTexture);

                normalNoiseTexture = null;
            }

            ssgiSampleDirections = null;
        }

        private void OnValidate()
        {
            if (postprocessMaterial)
            {
                UpdateSSGIPostprocessMaterial();
            }

            UpdateGaussianCoeff();
        }

        internal bool TryAddRenderObject(fxvRenderObject ro)
        {
            if (!allRenderObjects.Contains(ro))
            {
                if (myCamera == null)
                {
                    myCamera = GetComponent<Camera>();
                }

                if ((myCamera.cullingMask & (1 << ro.gameObject.layer)) != 0)
                {
                    allRenderObjects.Add(ro);

                    ro.SetOwner(this);

                    return true;
                }
            }

            return false;
        }

        internal bool TryRemoveRenderObject(fxvRenderObject ro)
        {
            if (allRenderObjects.Contains(ro))
            {
                allRenderObjects.Remove(ro);

                ro.SetOwner(null);

                return true;
            }

            return false;
        }

        void OnEnable()
        {
            Prepare();

            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

            _UpdateAllObjects();

            UpdateGaussianCoeff();
        }

        private void OnDisable()
        {
            if (fxvShieldAssetConfig.ActiveRenderPipeline == fxvShieldAssetConfig.Pipeline.FXV_SHIELD_BUILTIN)
            {
                Camera.onPreRender -= RenderWithCamera;
                Camera.onPostRender -= AfterRenderWithCamera;

                DestroyBuffers(shieldCommandBuffers);
                DestroyBuffers(grabCommandBuffers);
            }
        }

        internal void _UpdateAllObjects()
        {
            allRenderObjects.Clear();

            int count = renderInstancedChunks.Count;
            for (int i = 0; i < count; ++i)
            {
                fxvRenderObject.RenderChunk ch = renderInstancedChunks[i];
                ch.isInRenderList = false;
            }
            renderInstancedChunks.Clear();

            count = renderChunks.Count;
            for (int i = 0; i < count; ++i)
            {
                fxvRenderObject.RenderChunk ch = renderChunks[i];
                ch.isInRenderList = false;
            }
            renderChunks.Clear();

            fxvRenderObject[] renderObjectsOnScene = UnityEngine.Object.FindObjectsByType<fxvRenderObject>(FindObjectsSortMode.None);
            foreach (fxvRenderObject ro in renderObjectsOnScene)
            {
                if (TryAddRenderObject(ro))
                {
                    ro.Prepare();
                }
            }
        }

        void UpdateSSGIPostprocessMaterial()
        {
            postprocessMaterial.DisableKeyword("FXV_NUM_SAMPLES_LOW");
            postprocessMaterial.DisableKeyword("FXV_NUM_SAMPLES_MEDIUM");
            postprocessMaterial.DisableKeyword("FXV_NUM_SAMPLES_HIGH");

            if (giNumberOfSamples == GiNumSmplesCount.Low)
            {
                postprocessMaterial.EnableKeyword("FXV_NUM_SAMPLES_LOW");
            }
            else if (giNumberOfSamples == GiNumSmplesCount.Medium)
            {
                postprocessMaterial.EnableKeyword("FXV_NUM_SAMPLES_MEDIUM");
            }
            else if (giNumberOfSamples == GiNumSmplesCount.High)
            {
                postprocessMaterial.EnableKeyword("FXV_NUM_SAMPLES_HIGH");
            }
        }

        void UpdateGaussianCoeff()
        {
            int kernelSizeH = kernelRadius * 2 + 1;
            if (kernelSizeH > 16) kernelSizeH = 16;

            gauss_coeff_H = gaussian_kernel(kernelSizeH, sigma);

            if (PostprocesOnSeparateAxes())
            {
                int kernelSizeV = kernelRadiusVertical * 2 + 1;
                if (kernelSizeV > 16) kernelSizeV = 16;

                gauss_coeff_V = gaussian_kernel(kernelSizeV, sigmaVertical);
            }
            else
            {
                gauss_coeff_V = gauss_coeff_H;
            }
        }

        internal void _SetGaussianParams(Material mat)
        {
            int kernelSizeH = kernelRadius * 2 + 1;
            if (kernelSizeH > 16) kernelSizeH = 16;

            int kernelRadiusV = kernelRadius;
            float sampleStepV = sampleStep;

            if (PostprocesOnSeparateAxes())
            {
                kernelRadiusV = kernelRadiusVertical;
                sampleStepV = sampleStepVertical;
            }

            mat.SetFloatArray("GAUSSIAN_COEFF_H", gauss_coeff_H);
            mat.SetFloatArray("GAUSSIAN_COEFF_V", gauss_coeff_V);
            mat.SetInt("GAUSSIAN_KERNEL_RADIUS_H", kernelRadius);
            mat.SetInt("GAUSSIAN_KERNEL_RADIUS_V", kernelRadiusV);
            mat.SetFloat("GAUSSIAN_TEXEL_SIZE_H", sampleStep);
            mat.SetFloat("GAUSSIAN_TEXEL_SIZE_V", sampleStepV);
        }

        RenderInstanceInfo GetRenderList(int renderKey)
        {
            return renderInstancingInfos[renderKey];
        }

        internal void AddToRenderList(fxvRenderObject ro)
        {
            List<fxvRenderObject.RenderChunk> chunks = ro.GetRenderChunks();

            for (int i = 0; i < chunks.Count; ++i)
            {
                AddToRenderList(chunks[i]);
            }
        }

        internal void AddToRenderList(fxvRenderObject.RenderChunk ch)
        {
            if (ch.IsInstancingSupported())
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (renderInstancedChunks.Contains(ch))
                    {
                        Debug.Log("duplicate render chunk found");
                        return;
                    }
                }

                if (UnityEditor.GameObjectUtility.GetStaticEditorFlags(ch.parentRenderObject.gameObject).HasFlag(UnityEditor.StaticEditorFlags.BatchingStatic))
                {
                    Debug.Log("[FXV.ShieldPostprocess] Rendering FXV object that is static " + ch.parentRenderObject.gameObject.name, ch.parentRenderObject.gameObject);
                    return;
                }
#endif
                for (int k = 0; k < ch.renderInstancingKey.Length; ++k)
                {
                    int renderKey = ch.renderInstancingKey[k];
                    if (!renderInstancingInfos.ContainsKey(renderKey))
                    {
                        RenderInstanceInfo rii = new RenderInstanceInfo();
                        renderInstancingInfos.Add(renderKey, rii);
                        ch.parentRenderObject.RegisterInstancedProperties(rii, renderKey, k);
                    }
                }

                ch.isInRenderList = true;
                renderInstancedChunks.Add(ch);
            }
            else
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (renderChunks.Contains(ch))
                    {
                        return;
                    }
                }
#endif
                ch.isInRenderList = true;
                renderChunks.Add(ch);
            }
        }

        internal void OnInstancingKeyUpdate(fxvRenderObject ro, int keyIndex, int renderKey)
        {
            if (!renderInstancingInfos.ContainsKey(renderKey))
            {
                RenderInstanceInfo rii = new RenderInstanceInfo();
                renderInstancingInfos.Add(renderKey, rii);
                ro.RegisterInstancedProperties(rii, renderKey, keyIndex);
            }
        }

        internal void _PushRenderObjectsToCommandBuffer(CommandBuffer cmd)
        {
            cmd.ClearRenderTarget(false, true, clearToTransparentColor);

            int count = renderInstancedChunks.Count;
            for (int i = 0; i < count; ++i)
            {
                fxvRenderObject.RenderChunk ch = renderInstancedChunks[i];

                if (ch.parentRenderObject == null || ch.visibleRenderers == 0)
                {
                    ch.isInRenderList = false;
                    renderInstancedChunks.RemoveAt(i);
                    i--;
                    count--;
                    continue;
                }

                for (int k = 0; k < ch.renderInstancingKey.Length; ++k)
                {
                    RenderInstanceInfo rii = GetRenderList(ch.renderInstancingKey[k]);
                    if (!rii.IsFull())
                    {
                        ch.parentRenderObject.AddRenderInstanceInfo(k, rii, ch);
#if UNITY_EDITOR
                        if (rii.matricesCount > 0 && (rii.material == null || rii.mesh == null))
                        {
                            Debug.Log("ShieldPostprocess material null in object " + ch.parentRenderObject.gameObject.name, ch.parentRenderObject.gameObject);
                        }
#endif
                    }
                }
            }

           // int renderedNonInstances = 0;

            count = renderChunks.Count;
            for (int i = 0; i < count; ++i)
            {
                fxvRenderObject.RenderChunk ch = renderChunks[i];

                if (ch.parentRenderObject == null || ch.visibleRenderers == 0)
                {
                    ch.isInRenderList = false;
                    renderChunks.RemoveAt(i);
                    i--;
                    count--;
                    continue;
                }

                for (int k = 0; k < ch.renderInstancingKey.Length; ++k)
                {
                    //renderedNonInstances++;

                    ch.parentRenderObject.RenderNoInstancing(k, cmd, ch);
                }
            }

           // int renderedInstances = 0;

            foreach (RenderInstanceInfo rii in renderInstancingInfos.Values)
            {
                if (rii.matricesCount == 0)
                {
                    continue;
                }

                rii.UpdateMaterialPropertyBlock();

                cmd.DrawMeshInstanced(rii.mesh, rii.submeshIndex, rii.material, rii.shaderPass, rii.matrices, rii.matricesCount, rii.properties);

                rii.matricesCount = 0;

                //renderedInstances++;
            }

            //Debug.Log("renderedInstances " + renderedInstances + " rendered nonInstanced " + renderedNonInstances);
        }

        internal void _SetSSGIProperties(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture("_GI_RandomNoise", normalNoiseTexture);
            cmd.SetGlobalFloat("_GI_RandomSize", normalNoiseTexture.height);
            cmd.SetGlobalFloat("_GI_SampleRadius", giSampleRadius);
            cmd.SetGlobalFloat("_GI_Intensity", giIntensity);
            cmd.SetGlobalFloat("_GI_LightRange", giLightRange);
            cmd.SetGlobalFloat("_GI_FadeBehind", giFadeBehind);

            cmd.SetGlobalFloatArray("_GI_SampleDirections", ssgiSampleDirections);

            cmd.SetGlobalFloat("_GI_c_phi", 1.0f);
            cmd.SetGlobalFloat("_GI_n_phi", 1.0f);
            cmd.SetGlobalFloat("_GI_p_phi", 1.0f);
        }

        bool isTwoEyeVR = false;
        internal void _AddPostprocessToCommandBuffer(CommandBuffer cmd, PostprocessContext context)
        {
            isTwoEyeVR = context.isTwoEyeVR;

            _SetGaussianParams(postprocessMaterial);

            cmd.SetGlobalVector("_CameraDepthTexture_TexelSize", new Vector4(1.0f / context.targetWidth, 1.0f / context.targetHeight, context.targetWidth, context.targetHeight));
            cmd.SetGlobalMatrix("_ViewProjectInverse", context.viewProjectInverse);

            if (IsGloballIlluminationEnabled())
            {
                _SetSSGIProperties(cmd);
            }

            int pass = 2;
            for (int i = 0; i < context.downSampleSteps; ++i)
            {
                int ipp = i + 1;

                cmd.SetGlobalVector("_GI_FrameSize", new Vector4(1.0f / context.tmpTargetSizes[ipp].x, 1.0f / context.tmpTargetSizes[ipp].y, context.tmpTargetSizes[ipp].x, context.tmpTargetSizes[ipp].y));

                FXVBlit(cmd, context.fullScreenMesh, context.tempTarget[i], context.tmpTargetSizes[i], context.tempTarget[ipp], postprocessMaterial, pass);
                pass = pass == 2 ? 3 : 2;
            }

            if (IsGloballIlluminationEnabled())
            {
                cmd.SetGlobalVector("_GI_FrameSize", new Vector4(1.0f / context.tmpTargetSizes[giAtIteration].x, 1.0f / context.tmpTargetSizes[giAtIteration].y, context.tmpTargetSizes[giAtIteration].x, context.tmpTargetSizes[giAtIteration].y));
                cmd.Blit(context.tempTarget[giAtIteration], context.ssgiTarget, postprocessMaterial, 5);

                for (int i = 0; i < giDenoiseIterations; ++i)
                {
                    cmd.SetGlobalFloat("_GI_Stepwidth", Mathf.Pow(giDenoiseStepWidth, (float)i * giDenoiseStepChange));
                    cmd.Blit(context.ssgiTarget, context.ssgiTarget2, postprocessMaterial, 6);
                    cmd.Blit(context.ssgiTarget2, context.ssgiTarget, postprocessMaterial, 6);
                }
            }

            int upscalePass = 1; //Box
            int upscaleSGGIPass = 7; //Box + SSGI
            if (blurMethod == BlurMethod.GaussianOnly)
            {
                upscalePass = 8;
                upscaleSGGIPass = 9;
            }

            for (int i = 0; i < context.downSampleSteps; ++i)
            {
                int ipp = numberOfIterations - i - 1;

                cmd.SetGlobalVector("_GI_FrameSize", new Vector4(1.0f / context.tmpTargetSizes[ipp].x, 1.0f / context.tmpTargetSizes[ipp].y, context.tmpTargetSizes[ipp].x, context.tmpTargetSizes[ipp].y));

                FXVBlit(cmd, context.fullScreenMesh, context.tempTarget[numberOfIterations - i], context.tmpTargetSizes[numberOfIterations - 1], context.tempTarget[ipp], postprocessMaterial, (IsGloballIlluminationEnabled() && ipp == 0) ? upscaleSGGIPass : upscalePass);
            }

            if (context.blitToScreen)
            {
                blendAddMaterial.SetFloat("_ColorMultiplier", postprocessPower);

                FXVBlit(cmd, context.fullScreenMesh, context.tempTarget[0], context.tmpTargetSizes[0], context.currentTarget, blendAddMaterial, 0);
            }
        }

        internal void FXVBlit(CommandBuffer cmd, Mesh fullScreenMesh, RenderTargetIdentifier source, Vector2 sourceSize, RenderTargetIdentifier destination, Material material, int pass)
        {
            if (isTwoEyeVR)
            {
                cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
                cmd.SetGlobalTexture("_MainTex", source);
                material.SetVector("_MainTex_TexelSize", new Vector4(1.0f / sourceSize.x, 1.0f / sourceSize.y, sourceSize.x, sourceSize.y));
                cmd.DrawMesh(fullScreenMesh, Matrix4x4.identity, material, 0, pass);
            }
            else
            {
                cmd.Blit(source, destination, material, pass);
            }
        }

        PostprocessContext context;
        int[] tempTargetTextureId;
        int ssgiTargetTextureId;
        int ssgi2TargetTextureId;
        float[] ssgiSampleDirections;

        int positionFrontTargetTextureId;
        int positionBackTargetTextureId;

        private static readonly List<(Camera, CameraBufferEvent)> shieldCommandBuffers = new List<(Camera, CameraBufferEvent)>();
        private static readonly List<(Camera, CameraBufferEvent)> grabCommandBuffers = new List<(Camera, CameraBufferEvent)>();

        RenderTexture opaqueRT = null;
        RenderTargetIdentifier opaqueTextureRT;

        internal static CameraBufferEvent GetBuffer(List<(Camera, CameraBufferEvent)> map, Camera cam, CameraEvent camEvent, string eventName)
        {
            CameraBufferEvent buffer = null;
            int count = map.Count;
            int idx = -1;
            for (int i = 0; i < map.Count; ++i)
            {
                if (map[i].Item1 == cam)
                {
                    buffer = map[i].Item2;
                    idx = i;
                    break;
                }
            }

            if (buffer != null)
            {
                if (buffer.camEvent != camEvent)
                {
                    cam.RemoveCommandBuffer(buffer.camEvent, buffer.cmd);
                    cam.AddCommandBuffer(camEvent, buffer.cmd);
                    buffer.camEvent = camEvent;
                }
            }
            else
            {
                CommandBuffer[] allBuffers = cam.GetCommandBuffers(camEvent);
                CommandBuffer cmd = null;
                for (int i = 0; i < allBuffers.Length; ++i)
                {
                    if (allBuffers[i].name == eventName)
                    {
                        cmd = allBuffers[i];
                        break;
                    }
                }

                buffer = new CameraBufferEvent();
                buffer.camEvent = camEvent;

                if (cmd != null)
                {
                    buffer.cmd = cmd;
                    map.Add((cam, buffer));
                    return buffer;
                }

                cmd = new CommandBuffer();
                cmd.name = eventName;
                buffer.cmd = cmd;
                map.Add((cam, buffer));

                cam.AddCommandBuffer(camEvent, cmd);
            }

            return buffer;
        }

        internal static void DestroyBuffers(List<(Camera, CameraBufferEvent)> map)
        {
            foreach (var entry in map)
            {
                Camera cam = entry.Item1;
                CameraBufferEvent buffer = entry.Item2;
                if (cam != null)
                {
                    cam.RemoveCommandBuffer(buffer.camEvent, buffer.cmd);
                }
                buffer.cmd.Clear();
            }

            map.Clear();
        }

        public void RenderWithCamera(Camera cam)
        {
            if (!cam)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (myCamera == null)
                {
                    Prepare();
                }

                UpdateGaussianCoeff();
            }
#endif

            bool isProperCamera = false;
#if UNITY_EDITOR
            isProperCamera = (cam.cameraType == CameraType.SceneView);
#endif
            isProperCamera = cam == myCamera || isProperCamera;

            if (!isProperCamera)
            {
                return;
            }

            var rtW = cam.scaledPixelWidth;
            var rtH = cam.scaledPixelHeight;

            if (opaqueRT)
            {
                RenderTexture.ReleaseTemporary(opaqueRT);
                opaqueRT = null;
            }

            postprocessGrabOpaqueEvent = (cam.actualRenderingPath == RenderingPath.Forward || cam.orthographic) ? CameraEvent.AfterForwardOpaque : CameraEvent.AfterFinalPass;
            CameraEvent postprocessDrawEvent = (cam.actualRenderingPath == RenderingPath.Forward || cam.orthographic) ? CameraEvent.AfterImageEffects : CameraEvent.AfterForwardAlpha;
            if (drawOrder == BuiltInDrawOrder.AfterOpaque)
            {
                postprocessDrawEvent = (cam.actualRenderingPath == RenderingPath.Forward || cam.orthographic) ? CameraEvent.AfterForwardOpaque : CameraEvent.AfterFinalPass;
            }

            CameraBufferEvent bufferHitsEvent = GetBuffer(Shield.hitCommandBuffers, cam, CameraEvent.AfterForwardAlpha, Shield.HITS_CMD_BUFFER_NAME); // just get it so it's before ShieldPostprocess_Draw

            CameraBufferEvent grabBuffer = GetBuffer(grabCommandBuffers, cam, postprocessGrabOpaqueEvent, BUILTIN_GRAB_CMD_BUFFER_NAME);
            grabBuffer.cmd.Clear();
            opaqueRT = RenderTexture.GetTemporary(rtW, rtH, 0);
            opaqueTextureRT = new RenderTargetIdentifier(opaqueRT);
            grabBuffer.cmd.Blit(BuiltinRenderTextureType.CurrentActive, opaqueTextureRT);

            Shader.SetGlobalTexture(OPAQUE_TEXTURE_FOR_REFRACTION_ID, opaqueRT);

            CameraBufferEvent bufferEvent = GetBuffer(shieldCommandBuffers, cam, postprocessDrawEvent, BUILTIN_DRAW_CMD_BUFFER_NAME);

            bufferEvent.cmd.Clear();

            if (context == null || context.downSampleSteps != numberOfIterations || context.downSampleRate != downSampleRate)
            {
                context = new ShieldPostprocess.PostprocessContext();
                context.tempTarget = new RenderTargetIdentifier[numberOfIterations + 1];
                context.tmpTargetSizes = new Vector2[numberOfIterations + 1];

                tempTargetTextureId = new int[numberOfIterations + 1];
                for (int i = 0; i < tempTargetTextureId.Length; i++)
                {
                    tempTargetTextureId[i] = Shader.PropertyToID("_FXVTemporaryBuffer_" + i);
                }

                positionFrontTargetTextureId = Shader.PropertyToID("_FXVPositionFrontBuffer");
                positionBackTargetTextureId = Shader.PropertyToID("_FXVPositionBackBuffer");
                ssgiTargetTextureId = Shader.PropertyToID("_SSGIBuffer");
                ssgi2TargetTextureId = Shader.PropertyToID("_SSGIBuffer2");
            }

            context.downSampleSteps = numberOfIterations;
            context.downSampleRate = downSampleRate;
            context.targetWidth = rtW;
            context.targetHeight = rtH;
            context.currentTarget = BuiltinRenderTextureType.CameraTarget;
            context.viewProjectInverse = (cam.projectionMatrix * cam.worldToCameraMatrix).inverse;

            bufferEvent.cmd.GetTemporaryRT(tempTargetTextureId[0], new RenderTextureDescriptor(rtW, rtH, RenderTextureFormat.Default, 0));
            context.tempTarget[0] = new RenderTargetIdentifier(tempTargetTextureId[0]);
            context.tmpTargetSizes[0] = new Vector2(rtW, rtH);


            int w = context.targetWidth;
            int h = context.targetHeight;

            float downsample = (1.0f - downSampleRate);

            for (int i = 1; i < tempTargetTextureId.Length; i++)
            {
                w = (int)(w * downsample);
                h = (int)(h * downsample);

                bufferEvent.cmd.GetTemporaryRT(tempTargetTextureId[i], w, h, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                context.tempTarget[i] = new RenderTargetIdentifier(tempTargetTextureId[i]);

                context.tmpTargetSizes[i] = new Vector2(w, h);
            }

            if (IsGloballIlluminationEnabled())
            {
                int giW = (int)context.tmpTargetSizes[giAtIteration].x;
                int giH = (int)context.tmpTargetSizes[giAtIteration].y;

                bufferEvent.cmd.GetTemporaryRT(ssgiTargetTextureId, new RenderTextureDescriptor(giW, giH, RenderTextureFormat.DefaultHDR, 0), FilterMode.Bilinear);
                context.ssgiTarget = new RenderTargetIdentifier(ssgiTargetTextureId);

                bufferEvent.cmd.GetTemporaryRT(ssgi2TargetTextureId, new RenderTextureDescriptor(giW, giH, RenderTextureFormat.DefaultHDR, 0));
                context.ssgiTarget2 = new RenderTargetIdentifier(ssgi2TargetTextureId);

                bufferEvent.cmd.GetTemporaryRT(positionFrontTargetTextureId, new RenderTextureDescriptor(rtW, rtH, RenderTextureFormat.DefaultHDR, 0));
                context.positionFrontTarget = new RenderTargetIdentifier(positionFrontTargetTextureId);

                bufferEvent.cmd.GetTemporaryRT(positionBackTargetTextureId, new RenderTextureDescriptor(rtW, rtH, RenderTextureFormat.DefaultHDR, 0));
                context.positionBackTarget = new RenderTargetIdentifier(positionBackTargetTextureId);
            }

            if (IsGloballIlluminationEnabled())
            {
                bufferEvent.cmd.SetRenderTarget(new RenderTargetIdentifier[] {
                                                context.tempTarget[0],
                                                context.positionFrontTarget,
                                                context.positionBackTarget,
                                            }, new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
            }
            else
            {
                bufferEvent.cmd.SetRenderTarget(context.tempTarget[0], new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
            }

            _PushRenderObjectsToCommandBuffer(bufferEvent.cmd);

            _AddPostprocessToCommandBuffer(bufferEvent.cmd, context);

            for (int i = 0; i < tempTargetTextureId.Length; i++)
            {
                bufferEvent.cmd.ReleaseTemporaryRT(tempTargetTextureId[i]);
            }

            if (IsGloballIlluminationEnabled())
            {
                bufferEvent.cmd.ReleaseTemporaryRT(positionFrontTargetTextureId);
                bufferEvent.cmd.ReleaseTemporaryRT(positionBackTargetTextureId);
                bufferEvent.cmd.ReleaseTemporaryRT(ssgiTargetTextureId);
                bufferEvent.cmd.ReleaseTemporaryRT(ssgi2TargetTextureId);
            }
        }

        void AfterRenderWithCamera(Camera cam)
        {
            if (!cam)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (myCamera == null)
                {
                    Prepare();
                }

                UpdateGaussianCoeff();
            }
#endif
            if (opaqueRT)
            {
                RenderTexture.ReleaseTemporary(opaqueRT);
                opaqueRT = null;

                Shader.SetGlobalTexture(OPAQUE_TEXTURE_FOR_REFRACTION_ID, Texture2D.blackTexture);
            }
        }

        public void SetGloabalIlluminationEnabled(bool enabled)
        {
            globalIllumination = enabled;
        }

        public bool PostprocesOnSeparateAxes()
        {
            return separateAxes;
        }

        public bool IsGloballIlluminationEnabled()
        {
            return globalIllumination && IsGloballIlluminationSupported();
        }

        public bool IsGloballIlluminationSupported()
        {
            if (fxvShieldAssetConfig.ActiveRenderPipeline == fxvShieldAssetConfig.Pipeline.FXV_SHIELD_BUILTIN)
            {
                if (myCamera == null)
                {
                    myCamera = GetComponent<Camera>();
                    if (myCamera == null)
                    {
                        return false;
                    }
                }

                return (myCamera.actualRenderingPath == RenderingPath.DeferredShading);
            }

            return !forceDisableGI;
        }

        float erf(float x)
        {
            const float a1 = 0.254829592f;
            const float a2 = -0.284496736f;
            const float a3 = 1.421413741f;
            const float a4 = -1.453152027f;
            const float a5 = 1.061405429f;
            const float p = 0.3275911f;

            float t = 1.0f / (1.0f + p * Mathf.Abs(x));
            float y = 1.0f - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Mathf.Exp(-x * x);

            return Mathf.Sign(x) * y;
        }

        float def_int_gaussian(float x, float mu, float sigma)
        {
            return 0.5f * erf((x - mu) / (1.41421356237f * sigma));
        }

        float[] gaussian_kernel(int kernel_size = 5, float sigma = 1.0f, float mu = 0.0f, int step = 1)
        {
            float end = 0.5f * kernel_size;
            float start = -end;
            List<float> coeff = new List<float>();

            float sum = 0.0f;
            float x = start;
            float last_int = def_int_gaussian(x, mu, sigma);

            while (x < end)
            {
                x += step;
                float new_int = def_int_gaussian(x, mu, sigma);
                float c = new_int - last_int;
                coeff.Add(c);
                sum += c;
                last_int = new_int;
            }

            sum = 1 / sum;
            for (int i = 0; i < coeff.Count; i++)
            {
                coeff[i] *= sum;
            }

            while (coeff.Count < 16)
            {
                coeff.Add(0.0f);
            }

            return coeff.ToArray();
        }
    }
}