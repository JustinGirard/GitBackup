using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FXV
{
    internal class ShieldHit
    {
        static ShieldHit()
        {
            HIT_COLOR_PROP = Shader.PropertyToID("_HitColor");
            HIT_TEX_COLOR_PROP = Shader.PropertyToID("_HitTexColor");
            HIT_T_PROP = Shader.PropertyToID("_HitT");
            HIT_RADIUS_PROP = Shader.PropertyToID("_HitRadius");
            HIT_POS_PROP = Shader.PropertyToID("_HitPos");
            HIT_POWER_PROP = Shader.PropertyToID("_HitPower");
            HIT_TAN1_PROP = Shader.PropertyToID("_HitTan1");
            HIT_TAN2_PROP = Shader.PropertyToID("_HitTan2");
        }

        private static int HIT_COLOR_PROP;
        private static int HIT_TEX_COLOR_PROP;
        private static int HIT_T_PROP;
        private static int HIT_RADIUS_PROP;
        private static int HIT_POS_PROP;
        private static int HIT_POWER_PROP;
        private static int HIT_TAN1_PROP;
        private static int HIT_TAN2_PROP;

        private float lifeTime = 0.5f;
        private float lifeStart = 0.5f;

        private float radius = 1.0f;

        private AnimationCurve hitDecalFadeAnimation;
        private AnimationCurve hitDecalSizeAnimation;

        private Color color;
        private Color texColor;

        private Color propVal_finalHitColor;
        private float propVal_hitT = 0.0f;
        private float propVal_hitRadius = 0.0f;
        private Vector4 propVal_hitPos;
        private Vector4 propVal_hitTan1;
        private Vector4 propVal_hitTan2;

        private MaterialPropertyBlock propertyBlock;

        private bool previewMode = false;

        private Shield parentShield;

        private Transform parentTransform = null;

        private Vector3 hitLocalPosition = Vector3.zero;

        internal static void RegisterInstancedProperties(ShieldPostprocess.RenderInstanceInfo rii)
        {
            rii.RegisterFloatProperyArray(0, Shield.ACTIVATION_TIME_PROP);
            rii.RegisterFloatProperyArray(1, Shield.ACTIVATION_TIME_PROP01);
            rii.RegisterFloatProperyArray(2, Shield.HIT_EFFECT_VALUE_PROP);
            rii.RegisterVectorProperyArray(3, Shield.SHIELD_DIRECTION_PROP);
            rii.RegisterVectorProperyArray(4, Shield.SHIELD_BOUNDS_PROP);
            rii.RegisterFloatProperyArray(5, Shield.VISUALLY_ACTIVE_PROP);

            rii.RegisterVectorProperyArray(6, HIT_COLOR_PROP);
            rii.RegisterVectorProperyArray(7, HIT_TEX_COLOR_PROP);
            rii.RegisterFloatProperyArray(8, HIT_T_PROP);
            rii.RegisterFloatProperyArray(9, HIT_RADIUS_PROP);
            rii.RegisterVectorProperyArray(10, HIT_POS_PROP);
            rii.RegisterVectorProperyArray(11, HIT_TAN1_PROP);
            rii.RegisterVectorProperyArray(12, HIT_TAN2_PROP);
        }

        internal void AddRenderInstanceInfo(ShieldPostprocess.RenderInstanceInfo info, fxvRenderObject.RenderChunk chunk)
        {
            info.mesh = chunk.renderMesh;
            info.material = ((Shield.ShieldRenderChunk)chunk).shieldMaterials.hitMaterial;
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

                info.SetProperty(info.matricesCount, 0/*Shield.ACTIVATION_TIME_PROP*/, parentShield.propVal_activationT);
                info.SetProperty(info.matricesCount, 1/*Shield.ACTIVATION_TIME_PROP01*/, parentShield.shieldActivationTime);
                info.SetProperty(info.matricesCount, 2/*Shield.HIT_EFFECT_VALUE_PROP*/, parentShield.propVal_hitEffectT);
                info.SetProperty(info.matricesCount, 3/*Shield.SHIELD_DIRECTION_PROP*/, parentShield.propVal_shieldDirection);
                info.SetProperty(info.matricesCount, 4/*Shield.SHIELD_BOUNDS_PROP*/, parentShield.propVal_shieldBoundsSize);
                info.SetProperty(info.matricesCount, 5/*Shield.VISUALLY_ACTIVE_PROP*/, 1.0f);

                info.SetProperty(info.matricesCount, 6/*HIT_COLOR_PROP*/, propVal_finalHitColor);
                info.SetProperty(info.matricesCount, 7/*HIT_TEX_COLOR_PROP*/, texColor);
                info.SetProperty(info.matricesCount, 8/*HIT_T_PROP*/, propVal_hitT);
                info.SetProperty(info.matricesCount, 9/*HIT_RADIUS_PROP*/, propVal_hitRadius);
                info.SetProperty(info.matricesCount, 10/*HIT_POS_PROP*/, propVal_hitPos);
                info.SetProperty(info.matricesCount, 11/*HIT_TAN1_PROP*/, propVal_hitTan1);
                info.SetProperty(info.matricesCount, 12/*HIT_TAN2_PROP*/, propVal_hitTan2);

                info.matrices[info.matricesCount] = r.localToWorldMatrix;
                info.matricesCount++;
            }
        }

        internal void RenderNoInstancing(CommandBuffer cmd, fxvRenderObject.RenderChunk chunk)
        {
            Shield.ShieldRenderChunk ch = ((Shield.ShieldRenderChunk)chunk);

            RenderParams rp = new RenderParams(ch.shieldMaterials.hitMaterial);
            rp.matProps = propertyBlock;

            int rendCount = ch.renderers.Count;
            for (int j = 0; j < rendCount; ++j)
            {
                Renderer r = ch.renderers[j];
                if (!r.isVisible)
                {
                    continue;
                }

                if (r is SkinnedMeshRenderer)
                {
                    cmd.SetGlobalVector(HIT_COLOR_PROP, propVal_finalHitColor);
                    cmd.SetGlobalVector(HIT_TEX_COLOR_PROP, texColor);
                    cmd.SetGlobalFloat(HIT_T_PROP, propVal_hitT);
                    cmd.SetGlobalFloat(HIT_RADIUS_PROP, propVal_hitRadius);
                    cmd.SetGlobalVector(HIT_POS_PROP, propVal_hitPos);
                    cmd.SetGlobalVector(HIT_TAN1_PROP, propVal_hitTan1);
                    cmd.SetGlobalVector(HIT_TAN2_PROP, propVal_hitTan2);

                    cmd.DrawRenderer(r, ch.shieldMaterials.hitMaterial, ch.subMeshIndex, 0);
                }
                else
                {
                    cmd.DrawMesh(ch.renderMesh, r.localToWorldMatrix, ch.shieldMaterials.hitMaterial, ch.subMeshIndex, 0, propertyBlock);
                }
            }
        }

        internal void Update()
        {
            if (!previewMode)
            {
                lifeTime -= Time.deltaTime;
            }

            float anim = hitDecalFadeAnimation.Evaluate(1.0f - GetLifeT());

            propVal_finalHitColor = color;
            propVal_finalHitColor.a = Mathf.Max(0.0f, anim);
            propertyBlock.SetColor(HIT_COLOR_PROP, propVal_finalHitColor);

            propVal_hitT = (1.0f - GetLifeT());
            propertyBlock.SetFloat(HIT_T_PROP, propVal_hitT);

            float size = Mathf.Max(0.0f, hitDecalSizeAnimation.Evaluate(1.0f - GetLifeT()));
            propVal_hitRadius = radius * size;
            propertyBlock.SetFloat(HIT_RADIUS_PROP, propVal_hitRadius);

            propVal_hitPos = parentTransform.TransformPoint(hitLocalPosition);

            propertyBlock.SetVector(HIT_POS_PROP, propVal_hitPos);

            if (lifeTime <= 0.0f)
            {
                lifeTime = 0.0f;
            }
        }

        internal void SetEditorPreviewT(float t)
        {
#if UNITY_EDITOR
            lifeTime = t;
            lifeStart = 1.0f;
            previewMode = true;
#endif
        }

        internal bool IsFinished()
        {
#if UNITY_EDITOR
            if (previewMode)
            {
                return false;
            }
#endif
            return lifeTime <= 0.0f;
        }

        internal float GetLifeT()
        {
            return lifeTime / lifeStart;
        }

        internal Vector3 GetLocalPosition()
        {
            return hitLocalPosition;
        }

        internal Vector3 GetWorldPosition()
        {
            return parentTransform.TransformPoint(hitLocalPosition);
        }

        internal void DestroyHitEffect()
        {
            parentShield = null;
            parentTransform = null;
            hitDecalFadeAnimation = null;
            hitDecalSizeAnimation = null;
            propertyBlock = null;
        }

        internal Transform FindBestBone(Transform root, Vector3 point)
        {
            Vector3 rootPos = root.position;

            Transform best = null;
            float bestDist2 = float.MaxValue;

            (best, bestDist2) = FindBestBoneRecurrent(root, point, best, bestDist2);

            return best;
        }

        internal (Transform, float) FindBestBoneRecurrent(Transform root, Vector3 point, Transform best, float bestDist2)
        {
            Vector3 rootPos = root.position;

            for (int i = 0; i < root.childCount; ++i)
            {
                Transform child = root.GetChild(i);
                Vector3 boneDiff = child.position - rootPos;
                Vector3 boneDir = boneDiff.normalized;

                float d1 = Vector3.Dot(boneDir, point - rootPos);
                float d2 = Vector3.Dot(boneDir, point - child.position);

                if (d1 * d2 < 0.0f)
                {
                    Vector3 closestPoint = rootPos + boneDir * d1;

                    float dist2 = (closestPoint - point).sqrMagnitude;
                    if (dist2 < bestDist2)
                    {
                        bestDist2 = dist2;
                        best = root;
                    }
                }

                (best, bestDist2) = FindBestBoneRecurrent(child, point, best, bestDist2);
            }

            return (best, bestDist2);
        }

        internal bool StartHitFX(Shield parent, Vector3 hitWorldPosition, Vector3 tangent1World, Vector3 tangent2World, Color hitColor, Color hitTextureColor, float power, float time, float radius, AnimationCurve fadeAnim, AnimationCurve sizeAnim)
        {
            parentShield = parent;

            propertyBlock = new MaterialPropertyBlock();

            List<fxvRenderObject.RenderChunk> renderChunks = parentShield.GetRenderChunks();

            if (renderChunks.Count == 0)
            {
                return false;
            }

            Renderer r = renderChunks[0].renderers[0];
            r.GetPropertyBlock(propertyBlock);

            parentTransform = null;
            if (r is SkinnedMeshRenderer)
            {
                SkinnedMeshRenderer smr = (SkinnedMeshRenderer)r;
                parentTransform = FindBestBone(smr.rootBone, hitWorldPosition);
            }

            if (parentTransform == null)
            {
                parentTransform = parentShield.transform;
            }

            hitLocalPosition = parentTransform.InverseTransformPoint(hitWorldPosition);

            propVal_hitPos = hitWorldPosition;
            propVal_hitTan1 = tangent1World;
            propVal_hitTan2 = tangent2World;

            propertyBlock.SetVector(HIT_POS_PROP, propVal_hitPos);
            propertyBlock.SetVector(HIT_TAN1_PROP, propVal_hitTan1);
            propertyBlock.SetVector(HIT_TAN2_PROP, propVal_hitTan2);

            this.radius = radius;

            hitDecalFadeAnimation = fadeAnim;
            hitDecalSizeAnimation = sizeAnim;

            lifeTime = lifeStart = time;

            float anim = hitDecalFadeAnimation.Evaluate(0.0f);

            color = hitColor;
            propVal_finalHitColor = color;
            propVal_finalHitColor.a = anim;
            propertyBlock.SetColor(HIT_COLOR_PROP, propVal_finalHitColor);

            texColor = hitTextureColor;
            propertyBlock.SetColor(HIT_TEX_COLOR_PROP, hitTextureColor);

            propVal_hitT = 0.0f;
            propertyBlock.SetFloat(HIT_T_PROP, propVal_hitT);

            float size = Mathf.Max(0.0f, hitDecalSizeAnimation.Evaluate(0.0f));
            propVal_hitRadius = radius * size;
            propertyBlock.SetFloat(HIT_RADIUS_PROP, propVal_hitRadius);

            return true;
        }
    }

}