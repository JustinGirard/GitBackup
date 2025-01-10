#define FXV_SHIELD_USE_PROPERTY_BLOCKS

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Serialization;
using System;
using UnityEditor;

namespace FXV
{
    public class ShieldDraggablePoint : PropertyAttribute { }

    public partial class Shield : fxvRenderObject
    {
        [ExecuteInEditMode]
        static Shield()
        {
            ACTIVATION_TIME_PROP = Shader.PropertyToID("_ActivationTime");
            ACTIVATION_TIME_PROP01 = Shader.PropertyToID("_ActivationTime01");
            ACTIVATION_RIM_PROP = Shader.PropertyToID("_ActivationRim");
            SHIELD_DIRECTION_PROP = Shader.PropertyToID("_ShieldDirection");
            HIT_EFFECT_VALUE_PROP = Shader.PropertyToID("_HitEffectValue");
            SHIELD_BOUNDS_PROP = Shader.PropertyToID("_ShieldBounds");
            VISUALLY_ACTIVE_PROP = Shader.PropertyToID("_VisuallyActive");

            MAIN_TEX_COLOR_PROP = Shader.PropertyToID("_Color");
            TEX_COLOR_PROP = Shader.PropertyToID("_TextureColor");
            PATTERN_COLOR_PROP = Shader.PropertyToID("_PatternColor");

            RenderPipelineManager.beginCameraRendering -= _RenderWithCameraFirst;
            RenderPipelineManager.beginCameraRendering += _RenderWithCameraFirst;
        }

        internal static int ACTIVATION_TIME_PROP;
        internal static int ACTIVATION_TIME_PROP01;
        internal static int ACTIVATION_RIM_PROP;
        internal static int SHIELD_DIRECTION_PROP;
        internal static int HIT_EFFECT_VALUE_PROP;
        internal static int SHIELD_BOUNDS_PROP;
        internal static int VISUALLY_ACTIVE_PROP;

        internal static bool HITS_PREVIEW_ENABLED = false;

        private static int MAIN_TEX_COLOR_PROP;
        private static int TEX_COLOR_PROP;
        private static int PATTERN_COLOR_PROP;

        internal protected class ShieldRenderChunk : RenderChunk
        {
            public ShieldRenderChunk(int instancingKeysCount) : base(instancingKeysCount)
            {

            }

            public ShieldMaterials shieldMaterials = null;
        };

        internal protected class ShieldMaterials
        {
            public Material baseMaterial = null;
            public Material activationMaterial = null;
            public Material instancedMaterial = null;
            public Material hitMaterial = null;
            public bool isForSkinnedMesh = false;
            public bool needRebuild = true;

            public void DestroyMaterials()
            {
                if (activationMaterial)
                {
                    fxvRenderObject.DestroyAsset(activationMaterial);
                }
                if (hitMaterial)
                {
                    fxvRenderObject.DestroyAsset(hitMaterial);
                }
                if (instancedMaterial)
                {
                    fxvRenderObject.DestroyAsset(instancedMaterial);
                }

                baseMaterial = null;
                activationMaterial = null;
                instancedMaterial = null;
            }
        }

        #region ShieldProperties
        [fxvHeader("Shield Config")]
        [Tooltip("Is shield active at start. This will have effect when entering play mode."), SerializeField]
        bool shieldActive = true;

        [Tooltip("How fast shield activation animation is."), SerializeField]
        float shieldActivationSpeed = 1.0f;

        [Tooltip("Specify the range of for activation time so that shield is invisible at time 0, and fully visible at time 1."), SerializeField]
        Vector2 shieldActivationRange = new Vector2(0.0f, 1.0f);

        [fxvHeader("Optional Config")]
        [Tooltip("(Optional) Material variant that can be switched runtime with SetMaterialVariant(int index)."), SerializeField]
        List<Material> materialVariants;

        [SerializeField, HideInInspector]
        Collider shieldCollider;

        [Tooltip("(Optional) Manually specify colliders if it's not in the same game object. Shield component will handle enabling/disabling those collider when shield is turned on/off"), SerializeField]
        Collider[] shieldColliders = null;

        [Tooltip("(Optional) Sometimes you dont want to make shield a child of an object - specify GameObject here you want the shield to follow, without being a child of it."), SerializeField]
        GameObject followObject;

        [Tooltip("(Optional) Specify the light object the shield should affect when turned on/off."), SerializeField]
        Light shieldLight;

        private float shieldActivationRim = 0.2f;
        #endregion ShieldProperties

        #region RenderingProperties

        [fxvHeader("Rendering"), SerializeField, FXV.Internal.Shield.fxvSortingLayerAttribute]
        string sortingLayer = "Default";

        [SerializeField]
        int sortingOrder = 0;

        [Tooltip("Use this to disable main shield effect but still have hit effects rendered."), SerializeField]
        bool isVisuallyActive = true;
        #endregion RenderingProperties

        #region HitProperties
        [fxvHeader("Hit Config"), FormerlySerializedAs("hitRippleTexture"), SerializeField]
        Texture2D hitDecalTexture = null;

        [FormerlySerializedAs("hitRippleDistortion"), SerializeField]
        float hitEffectDistortion = 1.0f;

        [SerializeField]
        Texture2D hitVariationTexture = null;

        [SerializeField]
        float hitVariationScale = 1.0f;

        [SerializeField]
        float hitVariationColor = 0.0f;

        [SerializeField]
        float hitColorPower = 1.0f;

        [SerializeField]
        AnimationCurve hitShieldAnimation = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

        [FormerlySerializedAs("hitDecalAnimation"), SerializeField]
        AnimationCurve hitDecalFadeAnimation = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 0.0f);

        [SerializeField]
        AnimationCurve hitDecalSizeAnimation = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 0.0f);

        [SerializeField]
        float hitDurationModifier = 1.0f;

        [SerializeField]
        bool hitAffectsColor = false;

        [SerializeField]
        Color hitColor = Color.white;

        [SerializeField]
        bool hitAffectsRimTexture = true;

        [SerializeField]
        bool hitAffectsPatternTexture = true;

        [SerializeField]
        Color hitTextureColor = Color.white;

        #endregion HitProperties



        #region PreviewProperties
        [fxvHeader("Preview Options (Edit Mode Only)")]
        [SerializeField, ShieldDraggablePoint, Tooltip("Use gizmo in scene view to change position of fade point. This is only used when material have Direction Based Visibility Enabled.")]
        private Vector3 ShieldFadePoint = Vector3.up;

        [SerializeField, Range(0.0f, 1.0f), Tooltip("Use this slider for activation animation preview in edit mode.")]
        float activationAnimationPreview = 1.0f;

#if UNITY_EDITOR
        [SerializeField, Tooltip("Check this to enable hit effect preview.")]
        bool hitEffectPreviewEnabled = false;

        [SerializeField, Range(0.0f, 1.0f), Tooltip("Use this slider for hit effect preview.")]
        float hitEffectPreviewTime = 0.5f;

        [SerializeField, Range(0.0f, 5.0f), Tooltip("Use this slider for hit effect preview size.")]
        float hitEffectPreviewSize = 1.0f;

        Vector3 hitEffectPreviewPosition = Vector3.zero;
        Vector3 hitEffectPreviewNormal = Vector3.zero;
#endif
        #endregion PreviewProperties

        internal float shieldActivationTime = 1.0f;
        internal float shieldActivationDir = 0.0f;

        private Color lightColor;

        private Dictionary<int, ShieldMaterials> shieldMaterials = new Dictionary<int, ShieldMaterials>();

        private Collider[] myColliders;

#if FXV_SHIELD_USE_PROPERTY_BLOCKS
        private MaterialPropertyBlock propertyBlock;
        private bool isDirty = false;
#endif

        private int currentHitIndex = 1;

        private float enabledTimer = 0.0f;

        private List<ShieldHit> activeHits = new List<ShieldHit>();

        internal float propVal_activationT = 1.0f;
        internal float propVal_hitEffectT = 1.0f;
        internal Vector4 propVal_shieldDirection;
        internal Vector4 propVal_shieldBoundsSize;

#if UNITY_EDITOR
        public Shield copyHitParamsFrom;

        internal void CopyHitValuesFrom(Shield shield)
        {
            hitColor = shield.hitColor;
            hitTextureColor = shield.hitTextureColor;
            hitDecalTexture = shield.hitDecalTexture;
            hitVariationTexture = shield.hitVariationTexture;
            hitVariationScale = shield.hitVariationScale;
            hitVariationColor = shield.hitVariationColor;
            hitEffectDistortion = shield.hitEffectDistortion;
            hitColorPower = shield.hitColorPower;
            hitShieldAnimation = new AnimationCurve(shield.hitShieldAnimation.keys);
            hitDecalFadeAnimation = new AnimationCurve(shield.hitDecalFadeAnimation.keys);
            hitDecalSizeAnimation = new AnimationCurve(shield.hitDecalSizeAnimation.keys);
            hitDurationModifier = shield.hitDurationModifier;
            hitAffectsColor = shield.hitAffectsColor;
            hitAffectsRimTexture = shield.hitAffectsRimTexture;
            hitAffectsPatternTexture = shield.hitAffectsPatternTexture;
        }
#endif

        internal override void Prepare()
        {
            base.Prepare();

            TransferColliderToArray();

#if FXV_SHIELD_USE_PROPERTY_BLOCKS
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
#endif
            Camera.onPreCull -= _RenderWithCamera_BuiltIn;
            Camera.onPostRender -= _AfterRenderWithCamera_BuiltIn;

            Camera.onPreCull -= RenderWithCamera_BuiltIn;

            if (fxvShieldAssetConfig.ActiveRenderPipeline == fxvShieldAssetConfig.Pipeline.FXV_SHIELD_BUILTIN)
            {
                Camera.onPreCull += RenderWithCamera_BuiltIn;

                Camera.onPreCull += _RenderWithCamera_BuiltIn;        // make static version always call last
                Camera.onPostRender += _AfterRenderWithCamera_BuiltIn;  // make static version always call last
            }

            RenderPipelineManager.beginCameraRendering -= _RenderWithCameraLast;
            RenderPipelineManager.beginCameraRendering -= RenderWithCamera;

            if (fxvShieldAssetConfig.ActiveRenderPipeline == fxvShieldAssetConfig.Pipeline.FXV_SHIELD_URP || fxvShieldAssetConfig.ActiveRenderPipeline == fxvShieldAssetConfig.Pipeline.FXV_SHIELD_HDRP)
            {
                RenderPipelineManager.beginCameraRendering += RenderWithCamera;
                RenderPipelineManager.beginCameraRendering += _RenderWithCameraLast;
            }

            UpdateSortingLayer();

            ShieldPostprocess.AddShield(this);

            shieldActivationDir = 0.0f;

            if (shieldLight)
            {
                lightColor = shieldLight.color;
            }

            if (shieldColliders != null && shieldColliders.Length > 0 && shieldColliders[0] != null)
            {
                myColliders = shieldColliders;
            }
            else
            {
                Collider collider = transform.GetComponent<Collider>();
                if (collider != null)
                {
                    myColliders = new Collider[1];
                    myColliders[0] = collider;
                }
            }

            if (Application.isPlaying)
            {
                if (shieldActive)
                {
                    shieldActivationTime = 1.0f;
                    if (myColliders != null)
                    {
                        for (int i = 0; i < myColliders.Length; i++)
                        {
                            myColliders[i].enabled = true;
                        }
                    }
                    SetRenderersEnabled(true);
                }
                else
                {
                    shieldActivationTime = 0.0f;
                    if (myColliders != null)
                    {
                        for (int i = 0; i < myColliders.Length; i++)
                        {
                            myColliders[i].enabled = false;
                        }
                    }
                    SetRenderersEnabled(false);
                }
            }
            else
            {
                shieldActivationTime = activationAnimationPreview;
            }

            if (shieldLight && Application.isPlaying)
            {
                shieldLight.color = Color.Lerp(Color.black, lightColor, shieldActivationTime);
            }

            List<fxvRenderObject.RenderChunk> renderChunks = GetRenderChunks();
            int chunksCount = renderChunks.Count;
            for (int i = 0; i < chunksCount; i++)
            {
                ShieldRenderChunk ch = (ShieldRenderChunk)renderChunks[i];
                int rendCount = ch.renderers.Count;
                for (int j = 0; j < rendCount; ++j)
                {
                    ShieldMaterials sm = SetMaterial(ch.renderers[j], ch.materialIndex, ch.renderMaterial, ch.renderMaterial.GetInstanceID());
#if UNITY_EDITOR
                    if (ch.shieldMaterials != null && ch.shieldMaterials != sm)
                    {
                        Debug.LogError("Trying to set different material for Renderchunk, this shouldnt happen.");
                    }
#endif
                    ch.shieldMaterials = sm;
                }
            }

            UpdateActivationTimeProps();
            SetShieldEffectDirection(ShieldFadePoint);
            UpdateSortingLayer();
            SetShieldVisuallyActive(isVisuallyActive);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Camera.onPreCull -= RenderWithCamera_BuiltIn;

            RenderPipelineManager.beginCameraRendering -= RenderWithCamera;

            ShieldPostprocess.RemoveShield(this);

#if !FXV_SHIELD_USE_PROPERTY_BLOCKS
            if (Application.isPlaying)
            {
                if (baseMaterial)
                {
                    DestroyAsset(baseMaterial);
                }
            }
#endif

            foreach (var sm in shieldMaterials)
            {
                sm.Value.DestroyMaterials();
            }
            shieldMaterials.Clear();

            for (int i = 0; i < activeHits.Count; ++i)
            {
                activeHits[i].DestroyHitEffect();
            }
            activeHits.Clear();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            TransferColliderToArray();

            if (copyHitParamsFrom != null)
            {
                CopyHitValuesFrom(copyHitParamsFrom);
                copyHitParamsFrom = null;
            }
#endif
            if (!Application.isPlaying)
            {
                shieldActivationTime = activationAnimationPreview;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += _DelayedEditorValidate;
#endif

            if (shieldColliders != null && shieldColliders.Length > 0 && shieldColliders[0] != null)
            {
                myColliders = shieldColliders;
            }

            UpdateActivationTimeProps();
            SetShieldEffectDirection(ShieldFadePoint);
            UpdateSortingLayer();
            SetShieldVisuallyActive(isVisuallyActive);
        }

        internal void TransferColliderToArray()
        {
            if (shieldCollider != null && (shieldColliders == null || shieldColliders.Length == 0 || (shieldColliders.Length == 1 && shieldColliders[0] == null)))
            {
                shieldColliders = new Collider[1];
                shieldColliders[0] = shieldCollider;

                shieldCollider = null;
            }
        }

        internal override bool IsProperRenderer(Renderer r, out int[] materialIndexes, out bool isExclusive)
        {
            if (r == null)
            {
                materialIndexes = null;
                isExclusive = false;
                return false;
            }

            List<int> materialIndexesList = new List<int>();
            int matCount = r.sharedMaterials != null ? r.sharedMaterials.Length : 1;
            if (matCount > 1)
            {
                for (int i = 0; i < matCount; i++)
                {
                    if (r.sharedMaterials[i].shader.FindPropertyIndex("_ShieldDirection") != -1)
                    {
                        materialIndexesList.Add(i);
                    }
                }
            }
            else
            {
                if (r.sharedMaterial == null)
                {
                    materialIndexes = null;
                    isExclusive = false;
                    return false;
                }

                if (r.sharedMaterial.shader.FindPropertyIndex("_ShieldDirection") != -1)
                {
                    materialIndexesList.Add(0);
                }
            }

            materialIndexes = materialIndexesList.ToArray();
            isExclusive = materialIndexesList.Count == r.sharedMaterials.Length;

            return materialIndexesList.Count != 0;
        }

        internal override RenderChunk CreateRenderChunk()
        {
            return new ShieldRenderChunk(2);
        }

        internal void _DelayedEditorValidate()
        {
#if UNITY_EDITOR
            if (hitEffectPreviewEnabled && this)
            {
                HITS_PREVIEW_ENABLED = true;

                for (int i = 0; i < activeHits.Count; ++i)
                {
                    activeHits[i].DestroyHitEffect();
                }
                activeHits.Clear();

                if (activeHits.Count == 0)
                {
                    SceneView sv = SceneView.lastActiveSceneView;
                    if (sv && sv.camera)
                    {
                        if (properRenderers.Count == 0)
                        {
                            Prepare();
                        }

                        Bounds b = GetLocalBounds();
                        Vector3 center = GetRootTransform().TransformPoint(b.center);
                        Ray ray = new Ray(sv.camera.transform.position, (center - sv.camera.transform.position).normalized);
                        RaycastHit rhi;

                        if (myColliders != null)
                        {
                            for (int i = 0; i < myColliders.Length; ++i)
                            {
                                if (hitEffectPreviewPosition == Vector3.zero && myColliders[i] && myColliders[i].Raycast(ray, out rhi, 100.0f))
                                {
                                    hitEffectPreviewPosition = rhi.point;
                                    hitEffectPreviewNormal = rhi.normal;
                                }
                            }
                        }
                        else
                        {
                            Debug.Log("No colliders specified for hit effect");
                        }

                        foreach (var sm in shieldMaterials)
                        {
                            RebuildHitMaterial(sm.Value);
                        }
                        OnHit(hitEffectPreviewPosition, hitEffectPreviewNormal, hitEffectPreviewSize, 1.0f);
                        activeHits[0].SetEditorPreviewT(1.0f - hitEffectPreviewTime);
                    }
                }
                else
                {
                    activeHits[0].SetEditorPreviewT(1.0f - hitEffectPreviewTime);
                }
            }
            else
            {
                if (this && Selection.activeGameObject == gameObject)
                {
                    HITS_PREVIEW_ENABLED = false;
                }

                hitEffectPreviewPosition = Vector3.zero;
                hitEffectPreviewNormal = Vector3.zero;
                for (int i = 0; i < activeHits.Count; ++i)
                {
                    activeHits[i].DestroyHitEffect();
                }
                activeHits.Clear();
            }
#endif
        }

        internal void _DisableHitPreview()
        {
#if UNITY_EDITOR
            HITS_PREVIEW_ENABLED = false;

            hitEffectPreviewEnabled = false;
            hitEffectPreviewPosition = Vector3.zero;
            hitEffectPreviewNormal = Vector3.zero;
            for (int i = 0; i < activeHits.Count; ++i)
            {
                activeHits[i].DestroyHitEffect();
            }
            activeHits.Clear();
#endif
        }

        public void SetMaterialVariant(int index)
        {
            if (materialVariants == null || materialVariants.Count <= index)
            {
#if UNITY_EDITOR
                Debug.Log("SetMaterialVariant index not found " + index);
#endif
                return;
            }

            List<fxvRenderObject.RenderChunk> renderChunks = GetRenderChunks();
            int chunksCount = renderChunks.Count;
            for (int i = 0; i < chunksCount; i++)
            {
                ShieldRenderChunk ch = (ShieldRenderChunk)renderChunks[i];
                ch.shieldMaterials = null;
                int rendCount = ch.renderers.Count;
                for (int j = 0; j < rendCount; ++j)
                {
                    ShieldMaterials sm = SetMaterial(ch.renderers[j], ch.materialIndex, materialVariants[index], materialVariants[index].GetInstanceID());
#if UNITY_EDITOR
                    if (ch.shieldMaterials != null && ch.shieldMaterials != sm)
                    {
                        Debug.LogError("Trying to set different material for Renderchunk, this shouldnt happen.");
                    }
#endif
                    ch.shieldMaterials = sm;
                    ch.renderMaterial = ch.renderers[j].sharedMaterials[ch.materialIndex];
                }
            }
        }

        protected ShieldMaterials SetMaterial(Renderer renderer, int materialIndex, Material newMat, int materialKey)
        {
            if (newMat == null)
            {
                return null;
            }

            ShieldMaterials materialsInfo = null;
            if (shieldMaterials.ContainsKey(materialKey))
            {
                materialsInfo = shieldMaterials[materialKey];
            }
            else
            {
                materialsInfo = new ShieldMaterials();
                shieldMaterials.Add(materialKey, materialsInfo);
            }

            if (materialsInfo.needRebuild)
            {
                if (materialsInfo.activationMaterial)
                {
                    DestroyAsset(materialsInfo.activationMaterial);
                }

                materialsInfo.baseMaterial = newMat;
#if FXV_SHIELD_USE_PROPERTY_BLOCKS
                materialsInfo.baseMaterial.EnableKeyword("USE_MATERIAL_PROPERTY_BLOCKS");
#endif
                materialsInfo.baseMaterial.EnableKeyword("ACTIVATION_EFFECT_ON");
                materialsInfo.baseMaterial.DisableKeyword("HIT_EFFECT_ON");


                materialsInfo.baseMaterial.SetShaderPassEnabled("Postprocess", false);

                materialsInfo.activationMaterial = new Material(materialsInfo.baseMaterial);
                materialsInfo.activationMaterial.EnableKeyword("ACTIVATION_EFFECT_ON");

                materialsInfo.isForSkinnedMesh = renderer is SkinnedMeshRenderer;
                RebuildHitMaterial(materialsInfo);

                materialsInfo.needRebuild = false;
            }

            float t = shieldActivationRange.x + shieldActivationTime * (shieldActivationRange.y - shieldActivationRange.x);

            propVal_activationT = t;
            propVal_shieldDirection = new Vector4(ShieldFadePoint.x, ShieldFadePoint.y, ShieldFadePoint.z, 0.0f);
            propVal_shieldBoundsSize = GetLocalBounds().size;
#if FXV_SHIELD_USE_PROPERTY_BLOCKS
            propertyBlock.SetFloat(ACTIVATION_TIME_PROP, propVal_activationT);
            propertyBlock.SetFloat(ACTIVATION_TIME_PROP01, shieldActivationTime);
            propertyBlock.SetFloat(HIT_EFFECT_VALUE_PROP, propVal_hitEffectT);
            propertyBlock.SetVector(SHIELD_DIRECTION_PROP, propVal_shieldDirection);
            propertyBlock.SetVector(SHIELD_BOUNDS_PROP, propVal_shieldBoundsSize);
            propertyBlock.SetFloat(VISUALLY_ACTIVE_PROP, isVisuallyActive ? 1.0f : 0.0f);

            renderer.SetPropertyBlock(propertyBlock);
#else
            if (Application.isPlaying)
            {
                if (baseMaterial)
                {
                    DestroyAsset(baseMaterial);
                }

                baseMaterial = new Material(newMat);
                baseMaterial.SetFloat(ACTIVATION_TIME_PROP, t);
                baseMaterial.DisableKeyword("HIT_EFFECT_ON");
            }
            else
            {
                baseMaterial = newMat;
                baseMaterial.SetFloat(ACTIVATION_TIME_PROP, t);
            }
#endif

            shieldActivationRim = materialsInfo.activationMaterial.GetFloat(ACTIVATION_RIM_PROP);

            Material[] mats = renderer.sharedMaterials;
            mats[materialIndex] = materialsInfo.baseMaterial;
            renderer.sharedMaterials = mats;

            SetShieldEffectDirection(ShieldFadePoint);

            return materialsInfo;
        }

        public void SetMaterial(Material newMaterial)
        {
            List<fxvRenderObject.RenderChunk> renderChunks = GetRenderChunks();
            int chunksCount = renderChunks.Count;
            for (int i = 0; i < chunksCount; i++)
            {
                ShieldRenderChunk ch = (ShieldRenderChunk)renderChunks[i];
                ch.shieldMaterials = null;
                int rendCount = ch.renderers.Count;
                for (int j = 0; j < rendCount; ++j)
                {
                    ShieldMaterials sm = SetMaterial(ch.renderers[j], ch.materialIndex, newMaterial, newMaterial.GetInstanceID());
#if UNITY_EDITOR
                    if (ch.shieldMaterials != null && ch.shieldMaterials != sm)
                    {
                        Debug.LogError("Trying to set different material for Renderchunk, this shouldnt happen.");
                    }
#endif
                    ch.shieldMaterials = sm;
                    ch.renderMaterial = ch.renderers[j].sharedMaterials[ch.materialIndex];
                }
            }
        }

        internal void RebuildHitMaterial(ShieldMaterials shieldMaterials)
        {
            if (shieldMaterials.hitMaterial)
            {
                DestroyAsset(shieldMaterials.hitMaterial);
            }

            shieldMaterials.hitMaterial = new Material(shieldMaterials.baseMaterial);
            shieldMaterials.hitMaterial.DisableKeyword("USE_MAIN_TEXTURE_ANIMATION");
            shieldMaterials.hitMaterial.DisableKeyword("USE_PATTERN_TEXTURE_ANIMATION");
            if (!hitAffectsRimTexture)
            {
                shieldMaterials.hitMaterial.DisableKeyword("USE_MAIN_TEXTURE");
            }

            if (!hitAffectsPatternTexture)
            {
                shieldMaterials.hitMaterial.DisableKeyword("USE_PATTERN_TEXTURE");
            }

            if (shieldMaterials.isForSkinnedMesh)
            {
                shieldMaterials.hitMaterial.EnableKeyword("HIT_EFFECT_SKINNED_MESH");
            }

            shieldMaterials.hitMaterial.EnableKeyword("HIT_EFFECT_ON");
            shieldMaterials.hitMaterial.SetInt("_BlendSrcMode", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            shieldMaterials.hitMaterial.SetInt("_BlendDstMode", (int)UnityEngine.Rendering.BlendMode.One);
            shieldMaterials.hitMaterial.SetFloat("_HitPower", hitColorPower);

            if (hitDecalTexture != null)
            {
                shieldMaterials.hitMaterial.EnableKeyword("USE_HIT_RIPPLE");
                shieldMaterials.hitMaterial.SetTexture("_HitRippleTex", hitDecalTexture);
                shieldMaterials.hitMaterial.SetFloat("_HitRippleDistortion", hitEffectDistortion);
                shieldMaterials.hitMaterial.SetFloat("_HitColorAffect", hitAffectsColor ? 1.0f : 0.0f);
            }

            if (hitVariationTexture != null)
            {
                shieldMaterials.hitMaterial.EnableKeyword("USE_HIT_VARIATION");
                shieldMaterials.hitMaterial.SetTexture("_HitVariationTex", hitVariationTexture);
                shieldMaterials.hitMaterial.SetFloat("_HitVariationScale", hitVariationScale);
                shieldMaterials.hitMaterial.SetFloat("_HitVariationColor", hitVariationColor);
            }

            shieldMaterials.hitMaterial.renderQueue = shieldMaterials.hitMaterial.renderQueue + currentHitIndex;
        }

        internal void CreateInstancedMaterialIfNeeded()
        {
            List<fxvRenderObject.RenderChunk> renderChunks = GetRenderChunks();
            int chunksCount = renderChunks.Count;
            for (int i = 0; i < chunksCount; i++)
            {
                RenderChunk ch = renderChunks[i];
                int rendCount = ch.renderers.Count;
                for (int j = 0; j < rendCount; ++j)
                {
                    SetupInstanceMaterialForRenderer(ch.renderers[j], ch.materialIndex);
                }
            }
        }

        internal void SetupInstanceMaterialForRenderer(Renderer r, int materialIndex)
        {
            if (r.sharedMaterial == null)
            {
                return;
            }

            int materialKey = r.sharedMaterial.GetInstanceID();
            if (shieldMaterials.ContainsKey(materialKey))
            {
                ShieldMaterials sm = shieldMaterials[materialKey];
                if (sm.instancedMaterial == null)
                {
                    sm.instancedMaterial = new Material(sm.baseMaterial);
                    sm.needRebuild = true;
                }
                SetMaterial(r, materialIndex, sm.instancedMaterial, materialKey);
            }
        }

        public void SetRimColor(Color c)
        {
            CreateInstancedMaterialIfNeeded();

            foreach (var sm in shieldMaterials)
            {
                sm.Value.activationMaterial.color = c;
                sm.Value.baseMaterial.color = c;
            }
        }

        public void SetTextureRimColor(Color c)
        {
            CreateInstancedMaterialIfNeeded();

            foreach (var sm in shieldMaterials)
            {
                sm.Value.activationMaterial.SetColor(TEX_COLOR_PROP, c);
                sm.Value.baseMaterial.SetColor(TEX_COLOR_PROP, c);
            }
        }

        public void SetPatternColor(Color c)
        {
            CreateInstancedMaterialIfNeeded();

            foreach (var sm in shieldMaterials)
            {
                sm.Value.activationMaterial.SetColor(PATTERN_COLOR_PROP, c);
                sm.Value.baseMaterial.SetColor(PATTERN_COLOR_PROP, c);
            }
        }

        public void SetHitColor(Color c)
        {
            hitColor = c;
        }

        public void SetHitTextureColor(Color c)
        {
            hitTextureColor = c;
        }

        public Color GetHitColor()
        {
            return hitColor;
        }

        public Color GetHitTextureColor()
        {
            return hitTextureColor;
        }

        void SwitchToActivationMaterial()
        {
            List<fxvRenderObject.RenderChunk> renderChunks = GetRenderChunks();
            int chunksCount = renderChunks.Count;
            for (int i = 0; i < chunksCount; i++)
            {
                ShieldRenderChunk ch = (ShieldRenderChunk)renderChunks[i];
                int rendCount = ch.renderers.Count;
                for (int j = 0; j < rendCount; ++j)
                {
                    ch.renderers[j].sharedMaterials[ch.materialIndex] = ch.shieldMaterials.activationMaterial;
                }
            }
        }

        void SwitchToBaseMaterial()
        {
            List<fxvRenderObject.RenderChunk> renderChunks = GetRenderChunks();
            int chunksCount = renderChunks.Count;
            for (int i = 0; i < chunksCount; i++)
            {
                ShieldRenderChunk ch = (ShieldRenderChunk)renderChunks[i];
                int rendCount = ch.renderers.Count;
                for (int j = 0; j < rendCount; ++j)
                {
                    ch.renderers[j].sharedMaterials[ch.materialIndex] = ch.shieldMaterials.baseMaterial;
                }
            }
        }

        public bool IsHitDecalTextureEnabled()
        {
            return hitDecalTexture != null;
        }

        protected override void Update()
        {
            base.Update();

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || Selection.activeGameObject != gameObject)
            {
                if (hitEffectPreviewEnabled)
                {
                    _DisableHitPreview();
                }
            }
#endif

         /*   if (Application.isPlaying)
            {
                if ((shieldActivationTime == 1.0f) || (shieldActivationDir != 0.0f))
                {
                    if (IsAnyRendererEnabled() != isVisuallyActive)
                    {
                        SetRenderersEnabled(isVisuallyActive);
                    }
                }
            }*/

            if (shieldActivationDir > 0.0f)
            {
                shieldActivationTime += shieldActivationSpeed * Time.deltaTime;
                if (shieldActivationTime >= 1.0f)
                {
                    shieldActivationTime = 1.0f;
                    shieldActivationDir = 0.0f;
                    SwitchToBaseMaterial();
                }

                UpdateActivationTimeProps();

                if (shieldLight && Application.isPlaying)
                {
                    shieldLight.color = Color.Lerp(Color.black, lightColor, shieldActivationTime);
                }
            }
            else if (shieldActivationDir < 0.0f)
            {
                shieldActivationTime -= shieldActivationSpeed * Time.deltaTime;
                if (shieldActivationTime <= -shieldActivationRim)
                {
                    shieldActivationTime = -shieldActivationRim;
                    shieldActivationDir = 0.0f;
                    SetRenderersEnabled(false);
                    SwitchToBaseMaterial();
                }

                UpdateActivationTimeProps();

                if (shieldLight && Application.isPlaying)
                {
                    shieldLight.color = Color.Lerp(Color.black, lightColor, shieldActivationTime);
                }
            }

            if (followObject)
            {
                transform.position = followObject.transform.position;
            }

            if (GetIsShieldActive())
            {
                enabledTimer += Time.deltaTime;
            }
            else
            {
                enabledTimer = 0.0f;
            }
        }

        protected void LateUpdate()
        {
            int count = activeHits.Count;
            if (count > 0)
            {
                float maxHitT = 0.0f;
                for (int i = 0; i < count; ++i)
                {
                    ShieldHit sh = activeHits[i];
                    sh.Update();
                    if (sh.IsFinished())
                    {
                        sh.DestroyHitEffect();
                        activeHits.RemoveAt(i);
                        i--;
                        count--;
                    }
                    else
                    {
                        maxHitT = Mathf.Max(maxHitT, sh.GetLifeT());
                    }
                }

                UpdateHitEffectProps(hitShieldAnimation.Evaluate(1.0f - maxHitT));
            }

#if FXV_SHIELD_USE_PROPERTY_BLOCKS
            if (isDirty)
            {
                SetRenderersPropertyBlock(propertyBlock);
                isDirty = false;
            }
#endif
        }

        void UpdateHitEffectProps(float v)
        {

#if FXV_SHIELD_USE_PROPERTY_BLOCKS
            if (propertyBlock != null)
            {
                propVal_hitEffectT = v;

                propertyBlock.SetFloat(HIT_EFFECT_VALUE_PROP, propVal_hitEffectT);

                isDirty = true;
            }
#else
            myRenderer.sharedMaterial.SetFloat(HIT_EFFECT_VALUE_PROP, v);
            postprocessActivationMaterial.SetFloat(HIT_EFFECT_VALUE_PROP, v);
#endif
        }

        void UpdateSortingLayer()
        {
            SetRenderersSortingLayer(sortingLayer);
            SetRenderersSortingOrder(sortingOrder);
        }

        void UpdateActivationTimeProps()
        {
            float t = shieldActivationRange.x + shieldActivationTime * (shieldActivationRange.y - shieldActivationRange.x);

#if FXV_SHIELD_USE_PROPERTY_BLOCKS
            if (propertyBlock != null)
            {
                propVal_activationT = t;
                propertyBlock.SetFloat(ACTIVATION_TIME_PROP, propVal_activationT);
                propertyBlock.SetFloat(ACTIVATION_TIME_PROP01, shieldActivationTime);

                isDirty = true;
            }
#else
            if (myRenderer != null)
            {
                myRenderer.sharedMaterial.SetFloat(ACTIVATION_TIME_PROP, t);
            }
            if (postprocessActivationMaterial)
            {
                postprocessActivationMaterial.SetFloat(ACTIVATION_TIME_PROP, t);
            }
#endif
        }

        public bool GetIsShieldActive()
        {
            return (shieldActivationTime == 1.0f) || (shieldActivationDir == 1.0f);
        }

        public bool GetIsDuringActivationAnim()
        {
            return shieldActivationDir != 0.0f;
        }

        public float GetShieldEnabledTimer()
        {
            return enabledTimer;
        }

        public void SetShieldVisuallyActive(bool active)
        {
            isVisuallyActive = active;

#if FXV_SHIELD_USE_PROPERTY_BLOCKS
            if (propertyBlock != null)
            {
                propertyBlock.SetFloat(VISUALLY_ACTIVE_PROP, isVisuallyActive ? 1.0f : 0.0f);

                isDirty = true;
            }
#endif
        }

        public void SetShieldActive(bool active, bool animated = true)
        {
            if (active && !GetIsShieldActive())
            {
                enabledTimer = 0.0f;
            }

            if (!isPrepared) //let Awake set begin state
            {
                Prepare();
            }

            if (animated)
            {
                shieldActivationDir = (active) ? 1.0f : -1.0f;

                SwitchToActivationMaterial();
                UpdateActivationTimeProps();

                if (active)
                {
                    SetRenderersEnabled(true);
                }
            }
            else
            {
                shieldActivationTime = (active) ? 1.0f : 0.0f;
                shieldActivationDir = 0.0f;

                SetRenderersEnabled(active);
                UpdateActivationTimeProps();

                if (shieldLight && Application.isPlaying)
                {
                    shieldLight.color = Color.Lerp(Color.black, lightColor, shieldActivationTime);
                }
            }

            if (myColliders != null)
            {
                for (int i = 0; i < myColliders.Length; i++)
                {
                    myColliders[i].enabled = active;
                }
            }
        }

        public void SetShieldEffectFadePointPosition(Vector3 localPos)
        {
            ShieldFadePoint = localPos;

            SetShieldEffectDirection(new Vector4(localPos.x, localPos.y, localPos.z, 0.0f));
        }

        void SetShieldEffectDirection(Vector4 dir)
        {
#if FXV_SHIELD_USE_PROPERTY_BLOCKS
            if (propertyBlock != null)
            {
                propVal_shieldDirection = dir;

                propertyBlock.SetVector(SHIELD_DIRECTION_PROP, propVal_shieldDirection);

                isDirty = true;
            }
#else
            if (myRenderer != null)
            {
                myRenderer.sharedMaterial.SetVector(SHIELD_DIRECTION_PROP, dir);
            }
            if (baseMaterial != null)
            {
                baseMaterial.SetVector(SHIELD_DIRECTION_PROP, dir);
                activationMaterial.SetVector(SHIELD_DIRECTION_PROP, dir);
                postprocessMaterial.SetVector(SHIELD_DIRECTION_PROP, dir);
                postprocessActivationMaterial.SetVector(SHIELD_DIRECTION_PROP, dir);
            }
#endif
        }

        public void OnHit(Vector3 hitPos, Vector3 hitNormal, float hitScale, float hitDuration)
        {
            AddHitMeshAtPos(hitPos, hitNormal, hitScale, hitDuration);
        }

        private void AddHitMeshAtPos(Vector3 hitPos, Vector3 hitNormal, float hitScale, float hitDuration)
        {
            Vector3 tangent1;
            Vector3 t1 = Vector3.Cross(hitNormal, Vector3.forward);
            Vector3 t2 = Vector3.Cross(hitNormal, Vector3.up);
            if (t1.magnitude > t2.magnitude)
            {
                tangent1 = t1;
            }
            else
            {
                tangent1 = t2;
            }
            Vector3 tangent2 = Vector3.Cross(hitNormal, tangent1);

            ShieldHit hit = new ShieldHit();
            hit.StartHitFX(this, hitPos, tangent1, tangent2, hitColor, hitTextureColor, hitColorPower, hitDuration * hitDurationModifier, hitScale, hitDecalFadeAnimation, hitDecalSizeAnimation);

            activeHits.Add(hit);

            currentHitIndex++;
            if (currentHitIndex > 100)
            {
                currentHitIndex = 1;
            }
        }

        internal protected override void OnAddToRenderList(RenderChunk rc)
        {
            base.OnAddToRenderList(rc);

            if (!owner)
            {
                Shield.RegisterShieldHitInstancedProperties(rc.renderInstancingKey[1]);
            }
        }

        internal protected override void OnRemoveFromRenderList(RenderChunk rc)
        {
            base.OnRemoveFromRenderList(rc);

        }

        internal override void RegisterInstancedProperties(ShieldPostprocess.RenderInstanceInfo rii, int instancingRenderKey, int instancingKeyIndex)
        {
            if (instancingKeyIndex == 0) // shield object
            {
#if FXV_SHIELD_USE_PROPERTY_BLOCKS
                rii.RegisterFloatProperyArray(0, ACTIVATION_TIME_PROP);
                rii.RegisterFloatProperyArray(1, ACTIVATION_TIME_PROP01);
                rii.RegisterFloatProperyArray(2, HIT_EFFECT_VALUE_PROP);
                rii.RegisterVectorProperyArray(3, SHIELD_DIRECTION_PROP);
                rii.RegisterVectorProperyArray(4, SHIELD_BOUNDS_PROP);
                rii.RegisterFloatProperyArray(5, VISUALLY_ACTIVE_PROP);
#endif
            }
            else if (instancingKeyIndex == 1) // hit object
            {
                ShieldHit.RegisterInstancedProperties(rii);

                RegisterShieldHitInstancedProperties(instancingRenderKey);
            }
        }

        internal static void RegisterShieldHitInstancedProperties(int instancingRenderKey)
        {
            if (!renderInstancingInfos.ContainsKey(instancingRenderKey))
            {
                ShieldPostprocess.RenderInstanceInfo riiHit = new ShieldPostprocess.RenderInstanceInfo();
                renderInstancingInfos.Add(instancingRenderKey, riiHit);
                ShieldHit.RegisterInstancedProperties(riiHit);
            }
        }

        internal override void AddRenderInstanceInfo(int instancingKeyIndex, ShieldPostprocess.RenderInstanceInfo info, RenderChunk chunk)
        {
            if (instancingKeyIndex == 0) // shield object
            {
                info.mesh = chunk.renderMesh;
                info.material = chunk.renderMaterial;
                info.shaderPass = 1;
                info.submeshIndex = chunk.subMeshIndex;

                int count = chunk.renderers.Count;
                for (int i = 0; i < count; i++)
                {
                    Renderer r = chunk.renderers[i];

                    if (!r.isVisible)
                    {
                        continue;
                    }

#if FXV_SHIELD_USE_PROPERTY_BLOCKS
                    info.SetProperty(info.matricesCount, 0/*ACTIVATION_TIME_PROP*/, propVal_activationT);
                    info.SetProperty(info.matricesCount, 1/*ACTIVATION_TIME_PROP01*/, shieldActivationTime);
                    info.SetProperty(info.matricesCount, 2/*HIT_EFFECT_VALUE_PROP*/, propVal_hitEffectT);
                    info.SetProperty(info.matricesCount, 3/*SHIELD_DIRECTION_PROP*/, propVal_shieldDirection);
                    info.SetProperty(info.matricesCount, 4/*SHIELD_BOUNDS_PROP*/, propVal_shieldBoundsSize);
                    info.SetProperty(info.matricesCount, 5/*VISUALLY_ACTIVE_PROP*/, isVisuallyActive ? 1.0f : 0.0f);
#endif
                    info.matrices[info.matricesCount] = r.localToWorldMatrix;
                    info.matricesCount++;
                }
            }
            else if (instancingKeyIndex == 1) // hit object
            {
                for (int i = 0; i < activeHits.Count; ++i)
                {
                    activeHits[i].AddRenderInstanceInfo(info, chunk);
                }
            }
        }

        internal override void RenderNoInstancing(int instancingKeyIndex, CommandBuffer cmd, RenderChunk chunk)
        {
            if (instancingKeyIndex == 0) // shield object
            {
                for (int i = 0; i < chunk.renderers.Count; i++)
                {
                    Renderer r = chunk.renderers[i];

                    if (!r.isVisible)
                    {
                        continue;
                    }

                    cmd.DrawRenderer(r, chunk.renderMaterial, chunk.subMeshIndex, 1);
                }
            }
            else if (instancingKeyIndex == 1) // hit object
            {
                for (int i = 0; i < activeHits.Count; ++i)
                {
                    activeHits[i].RenderNoInstancing(cmd, chunk);
                }
            }
        }

        internal static readonly Dictionary<int, ShieldPostprocess.RenderInstanceInfo> renderInstancingInfos = new Dictionary<int, ShieldPostprocess.RenderInstanceInfo>();
        internal static readonly List<(ShieldHit, fxvRenderObject.RenderChunk)> renderNonInstancedChunks = new List<(ShieldHit, fxvRenderObject.RenderChunk)>();

        #region Hits_SRP
        static CommandBuffer hitsCommandBuffer_SRP = null;

        static partial void SetupHitsPassURP(Camera camera);
        static partial void SetupHitsPassHDRP(Camera camera);

        void RenderWithCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!camera || renderInstancingInfos.Count == 0)
            {
                return;
            }

            for (int h = 0; h < activeHits.Count; ++h)
            {
                int chunksCount = renderChunks.Count;
                for (int i = 0; i < chunksCount; i++)
                {
                    fxvRenderObject.RenderChunk ch = renderChunks[i];
                    if (!ch.isInRenderList)
                    {
                        continue;
                    }

                    if (ch.IsInstancingSupported())
                    {
                        activeHits[h].AddRenderInstanceInfo(renderInstancingInfos[ch.renderInstancingKey[1]], ch); // renderInstancingKey[1] is for hit effect
                    }
                    else
                    {
                        renderNonInstancedChunks.Add((activeHits[h], ch));
                    }
                }
            }
        }

        static void _RenderWithCameraFirst(ScriptableRenderContext context, Camera camera)
        {
            SetupHitsPassURP(camera);
            SetupHitsPassHDRP(camera);
        }

        static void _RenderWithCameraLast(ScriptableRenderContext context, Camera camera)
        {
            _DrawInstancedHitsWithGraphics();

            if (hitsCommandBuffer_SRP == null)
            {
                if (fxvShieldAssetConfig.ActiveRenderPipeline == fxvShieldAssetConfig.Pipeline.FXV_SHIELD_HDRP)
                {
                    _CleanupHitsWithoutRender();
                }

                return;
            }

            if (fxvShieldAssetConfig.ActiveRenderPipeline == fxvShieldAssetConfig.Pipeline.FXV_SHIELD_URP)
            {
                _AddHitsToCommandBuffer(hitsCommandBuffer_SRP);
            }

            hitsCommandBuffer_SRP = null;
        }
        #endregion Hits_SRP

        #region Hits_BuiltIn
        void RenderWithCamera_BuiltIn(Camera cam)
        {
            if (!cam || renderInstancingInfos.Count == 0)
            {
                return;
            }

            for (int h = 0; h < activeHits.Count; ++h)
            {
                int chunksCount = renderChunks.Count;
                for (int i = 0; i < chunksCount; i++)
                {
                    fxvRenderObject.RenderChunk ch = renderChunks[i];
                    if (!ch.isInRenderList)
                    {
                        continue;
                    }

                    if (ch.IsInstancingSupported())
                    {
                        activeHits[h].AddRenderInstanceInfo(renderInstancingInfos[ch.renderInstancingKey[1]], ch); // renderInstancingKey[1] is for hit effect
                    }
                    else
                    {
                        ShieldPostprocess.CameraBufferEvent bufferEvent = ShieldPostprocess.GetBuffer(hitCommandBuffers, cam, CameraEvent.AfterForwardAlpha, HITS_CMD_BUFFER_NAME);
                        activeHits[h].RenderNoInstancing(bufferEvent.cmd, ch);
                    }
                }
            }
        }

        internal static string HITS_CMD_BUFFER_NAME = "ShieldHits_Draw";
        internal static readonly List<(Camera, ShieldPostprocess.CameraBufferEvent)> hitCommandBuffers = new List<(Camera, ShieldPostprocess.CameraBufferEvent)>();

        static void _RenderWithCamera_BuiltIn(Camera cam)
        {
            if (!cam)
            {
                return;
            }

            _DrawInstancedHitsWithGraphics();

            ShieldPostprocess.CameraBufferEvent bufferEvent = ShieldPostprocess.GetBuffer(hitCommandBuffers, cam, CameraEvent.AfterForwardAlpha, HITS_CMD_BUFFER_NAME);

            _AddHitsToCommandBuffer(bufferEvent.cmd);
        }

        static void _AfterRenderWithCamera_BuiltIn(Camera cam)
        {
            if (!cam)
            {
                return;
            }

            foreach (var cmdBuff in hitCommandBuffers)
            {
                cmdBuff.Item2.cmd.Clear();
            }
        }
        #endregion Hits_BuiltIn

        internal static void _DrawInstancedHitsWithGraphics()
        {
            foreach (ShieldPostprocess.RenderInstanceInfo rii in renderInstancingInfos.Values)
            {
                if (rii.matricesCount == 0)
                {
                    continue;
                }

                rii.UpdateMaterialPropertyBlock();

                RenderParams rp = new RenderParams(rii.material);
                rp.matProps = rii.properties;
                Graphics.RenderMeshInstanced(rp, rii.mesh, rii.submeshIndex, rii.matrices, rii.matricesCount);

                rii.matricesCount = 0;
            }
        }

        internal static void _AddHitsToCommandBuffer(CommandBuffer cmd)
        {
            int count = renderNonInstancedChunks.Count;
            for (int i = 0; i < count; ++i)
            {
                (ShieldHit hit, RenderChunk rc) = renderNonInstancedChunks[i];
                hit.RenderNoInstancing(cmd, rc);
            }
            renderNonInstancedChunks.Clear();

            foreach (ShieldPostprocess.RenderInstanceInfo rii in renderInstancingInfos.Values)
            {
                if (rii.matricesCount == 0)
                {
                    continue;
                }

                rii.UpdateMaterialPropertyBlock();

                cmd.DrawMeshInstanced(rii.mesh, rii.submeshIndex, rii.material, rii.shaderPass, rii.matrices, rii.matricesCount, rii.properties);

                rii.matricesCount = 0;
            }
        }

        internal static void _CleanupHitsWithoutRender()
        {
            renderNonInstancedChunks.Clear();

            foreach (ShieldPostprocess.RenderInstanceInfo rii in renderInstancingInfos.Values)
            {
                rii.matricesCount = 0;
            }
        }

        private void OnDrawGizmos()
        {
            int count = activeHits.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; ++i)
                {
                    ShieldHit sh = activeHits[i];
                    Gizmos.DrawSphere(sh.GetWorldPosition(), 0.01f);
                }
            }
        }
    }
}