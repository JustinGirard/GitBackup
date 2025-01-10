using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FXV
{
    [ExecuteInEditMode]
    public class fxvRenderObject : MonoBehaviour
    {
        internal protected class RenderChunk
        {
            public RenderChunk(int instancingKeysCount)
            {
                renderInstancingKey = new int[instancingKeysCount];
                for (int i = 0; i < instancingKeysCount; ++i)
                {
                    renderInstancingKey[0] = 0;
                }
            }
            public List<Renderer> renderers = new List<Renderer>();
            public Mesh renderMesh = null;
            public Material renderMaterial = null;
            public fxvRenderObject parentRenderObject = null;
            public int subMeshIndex = 0;
            public int[] renderInstancingKey = null;
            public int visibleRenderers = 0;
            public int materialIndex = 0;
            public bool isExclusiveMaterial = true;
            public bool isInstancingSupported = false;
            public bool isInRenderList = false;



            public bool IsInstancingSupported() { return isInstancingSupported; }
        };

        protected List<RenderChunk> renderChunks = new List<RenderChunk>();
        protected List<(Renderer, int, bool)> properRenderers = new List<(Renderer, int, bool)>();

        protected List<RenderChunk> thisRendererChunks = new List<RenderChunk>();

        protected bool isPrepared = false;
        protected bool forceDisableInstancing = false;

        bool instancingKeysNeedRefresh = true;
        bool anyRendererEnabled = false;

        int renderersStateHash = 0;

        internal protected ShieldPostprocess owner = null;

        private void Awake()
        {
            if (!isPrepared)
            {
                Prepare();
            }
        }

        protected virtual void OnDestroy()
        {
            for (int i = 0; i < renderChunks.Count; i++)
            {
                renderChunks[i].renderers = null;
                renderChunks[i].renderMesh = null;
                renderChunks[i].renderMaterial = null;
                renderChunks[i].parentRenderObject = null;
            }
            renderChunks.Clear();

            thisRendererChunks.Clear();
            thisRendererChunks = null;

            CleanupMaterial();
        }

        internal static void DestroyAsset(Object assetObject)
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

        internal virtual void CleanupMaterial()
        {

        }

        internal void SetOwner(ShieldPostprocess o)
        {
            owner = o;
        }

        internal void SetRenderersEnabled(bool enabled)
        {
            int chunks = renderChunks.Count;
            for (int i = 0; i < chunks; i++)
            {
                RenderChunk ch = renderChunks[i];
                if (!ch.isExclusiveMaterial)
                {
                    continue;
                }

                int rendCount = ch.renderers.Count;
                for (int j = 0; j < rendCount; ++j)
                {
                    ch.renderers[j].enabled = enabled;
                }
            }
            anyRendererEnabled = enabled;
        }

        internal void SetRenderersPropertyBlock(MaterialPropertyBlock props)
        {
            int chunks = renderChunks.Count;
            for (int i = 0; i < chunks; i++)
            {
                RenderChunk ch = renderChunks[i];
                int rendCount = ch.renderers.Count;
                for (int j = 0; j < rendCount; ++j)
                {
                    ch.renderers[j].SetPropertyBlock(props);
                }
            }
        }
        internal void SetRenderersSortingLayer(string sortingLayer)
        {
            int chunks = renderChunks.Count;
            for (int i = 0; i < chunks; i++)
            {
                RenderChunk ch = renderChunks[i];
                int rendCount = ch.renderers.Count;
                for (int j = 0; j < rendCount; ++j)
                {
                    ch.renderers[j].sortingLayerName = sortingLayer;
                }
            }
        }
        internal void SetRenderersSortingOrder(int sortingOrder)
        {
            int chunks = renderChunks.Count;
            for (int i = 0; i < chunks; i++)
            {
                RenderChunk ch = renderChunks[i];
                int rendCount = ch.renderers.Count;
                for (int j = 0; j < rendCount; ++j)
                {
                    ch.renderers[j].sortingOrder = sortingOrder;
                }
            }
        }

        internal bool IsAnyRendererEnabled()
        {
            return anyRendererEnabled;
        }

        internal virtual bool IsProperRenderer(Renderer r, out int[] materialIndexes, out bool isExclusive)
        {
            materialIndexes = null;
            isExclusive = true;

            return r != null;
        }

        internal virtual RenderChunk CreateRenderChunk()
        {
            return new RenderChunk(1);
        }

        internal virtual void PrepareRenderers()
        {
            Renderer rootRenderer = GetComponent<Renderer>();

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            renderersStateHash = GetRenderersStateHash(renderers);

            properRenderers.Clear();
            anyRendererEnabled = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (IsProperRenderer(renderers[i], out int[] matIndexes, out bool exclusiveMaterial))
                {
                    for (int m = 0; m < matIndexes.Length; m++)
                    {
                        properRenderers.Add((renderers[i], matIndexes[m], exclusiveMaterial));
                        if (renderers[i].enabled)
                        {
                            anyRendererEnabled = true;
                        }
                    }
                }
            }
        }

        int GetRenderersStateHash(Renderer[] renderers)
        {
            var hash = new System.HashCode();
            for (int i = 0; i < renderers.Length; i++)
            {
                hash.Add(renderers[i].GetInstanceID());

                for (int m = 0; m < renderers[i].sharedMaterials.Length; ++m)
                {
                    if (renderers[i].sharedMaterials[m] == null)
                    {
                        hash.Add(0);
                    }
                    else
                    {
                        hash.Add(renderers[i].sharedMaterials[m].GetInstanceID());
                    }
                }
            }
            return hash.ToHashCode();
        }

        public Transform GetRootTransform()
        {
            if (properRenderers.Count > 0)
            {
                Renderer r = properRenderers[0].Item1;
                if (r is SkinnedMeshRenderer)
                {
                    return ((SkinnedMeshRenderer)r).rootBone;
                }
            }

            return transform;
        }

        internal Bounds GetLocalBounds()
        {
            return properRenderers[0].Item1.localBounds;
        }

        internal virtual void Prepare()
        {
            PrepareRenderers();

            forceDisableInstancing = false;

            Vector3 scale = transform.lossyScale;
            if (scale.x <= 0.0f || scale.y <= 0.0f || scale.z <= 0.0f)
            {
                forceDisableInstancing = true;
            }

            UpdateInstancingKeys();

            isPrepared = true;
        }

        internal virtual void RegisterInstancedProperties(ShieldPostprocess.RenderInstanceInfo rii, int instancingRenderKey, int instancingKeyIndex)
        {

        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                CleanupMaterial();
                Prepare();
            }
#endif
        }

        protected virtual void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                foreach (GameObject go in Selection.gameObjects)
                {
                    if (go == gameObject || go.transform.IsChildOf(transform))
                    {
                        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                        int newStateHash = GetRenderersStateHash(renderers);

                        if (newStateHash != renderersStateHash)
                        {
                            PrepareRenderers();
                            UpdateInstancingKeys();
                        }
                    }
                }
            }
#endif
        }

        protected Mesh GetMeshFromRenderer(Renderer r)
        {
            if (r is SkinnedMeshRenderer)
            {
                return ((SkinnedMeshRenderer)r).sharedMesh;
            }

            MeshFilter mf = r.GetComponent<MeshFilter>();
            return mf != null ? mf.sharedMesh : null;
        }

        protected virtual void OnInstancingKeyUpdate(int keyIndex, int renderKey)
        {
            if (owner)
            {
                owner.OnInstancingKeyUpdate(this, keyIndex, renderKey);
            }
        }

        internal protected virtual void OnAddToRenderList(RenderChunk rc)
        {
            if (owner)
            {
                owner.AddToRenderList(rc);
            }
            else
            {
                rc.isInRenderList = true;
            }
        }

        internal protected virtual void OnRemoveFromRenderList(RenderChunk rc)
        {
            if (owner)
            {

            }
            else
            {
                rc.isInRenderList = false;
            }
        }

        protected virtual void UpdateInstancingKeys()
        {
            for (int i = 0; i < renderChunks.Count; i++)
            {
                renderChunks[i].renderers.Clear();
                renderChunks[i].visibleRenderers = 0;
            }
            thisRendererChunks.Clear();

            int count = properRenderers.Count;
            for (int i = 0; i < properRenderers.Count; i++)
            {
                Renderer r = properRenderers[i].Item1;
                int materialIdx = properRenderers[i].Item2;
                bool isExclusive = properRenderers[i].Item3;

                Mesh mesh = GetMeshFromRenderer(r);
                if (mesh)
                {
                    bool isRoot = r.gameObject == gameObject;
                    int instancingKey = GetInstancingKey(r, materialIdx,  mesh);
                    RenderChunk rc = GetRenderChunk(instancingKey);
                    if (rc != null)
                    {
                        rc.renderers.Add(r);
                        rc.visibleRenderers = r.isVisible ? (rc.visibleRenderers + 1) : rc.visibleRenderers;
                        if (rc.isExclusiveMaterial != isExclusive)
                        {
                            Debug.LogError("render chunks exclusive is different");
                        }
                    }
                    else
                    {
                        rc = CreateRenderChunk();
                        rc.parentRenderObject = this;
                        rc.renderers.Add(r);
                        rc.renderMesh = mesh;
                        rc.subMeshIndex = materialIdx % mesh.subMeshCount;
                        rc.renderMaterial = r.sharedMaterials[materialIdx];
                        rc.renderInstancingKey[0] = instancingKey;
                        rc.visibleRenderers = r.isVisible ? (rc.visibleRenderers + 1) : rc.visibleRenderers;
                        rc.isExclusiveMaterial = isExclusive;
                        rc.materialIndex = materialIdx;
                        rc.isInstancingSupported = !forceDisableInstancing && rc.renderMaterial.enableInstancing && !(r is SkinnedMeshRenderer);

                        OnInstancingKeyUpdate(0, instancingKey);

                        for (int k = 1; k < rc.renderInstancingKey.Length; k++)
                        {
                            rc.renderInstancingKey[k] = GetInstancingKey(r, materialIdx, mesh, k);
                            OnInstancingKeyUpdate(k, rc.renderInstancingKey[k]);
                        }

                        renderChunks.Add(rc);
                    }

                    if (!rc.isInRenderList && rc.visibleRenderers != 0)
                    {
                        OnAddToRenderList(rc);
                    }

                    if (isRoot)
                    {
                        if (!thisRendererChunks.Contains(rc))
                        {
                            thisRendererChunks.Add(rc);
                        }
                    }
                    else
                    {
                        fxvChunkRenderer cr = r.gameObject.GetComponent<fxvChunkRenderer>();
                        if (cr == null)
                        {
                            cr = r.gameObject.AddComponent<fxvChunkRenderer>();
                            cr.renderChunk = rc;
                            cr.renderObject = this;
                            cr.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
                        }
                    }
                }
            }

            for (int i = 0; i < renderChunks.Count; i++)
            {
                if (renderChunks[i].renderers.Count == 0)
                {
                    renderChunks[i].parentRenderObject = null;
                    renderChunks[i].renderMesh = null;
                    renderChunks[i].renderMaterial = null;
                    renderChunks.RemoveAt(i);
                    i--;
                }
            }

            instancingKeysNeedRefresh = false;
        }

        protected RenderChunk GetRenderChunk(int instancingKey)
        {
            int count = renderChunks.Count;
            for (int i = 0; i < count; i++)
            {
                if (renderChunks[i].renderInstancingKey[0] == instancingKey)
                {
                    return renderChunks[i];
                }
            }

            return null;
        }

        protected virtual int GetInstancingKey(Renderer renderer, int materialIndex, Mesh mesh, int materialVersion = 0)
        {
            string renderInstancingKey = null;
            int renderInstancingHash = 0;

            if (renderer == null || mesh == null)
            {
                return 0;
            }

            Material[] mats = renderer.sharedMaterials;
            if (mats == null || materialIndex >= mats.Length)
            {
                return 0;
            }

            Material material = mats[materialIndex];
            if (material == null)
            { 
                return 0;
            }

            renderInstancingKey = mesh.GetInstanceID().ToString();

            renderInstancingKey += "_" + materialVersion + "_" + materialIndex;

            if (material != null)
            {
                renderInstancingKey += "_" + material.name;
            }

            if (renderInstancingKey != null && renderInstancingKey.Length > 0)
            {
                renderInstancingHash = renderInstancingKey.GetHashCode();
            }

            return renderInstancingHash;
        }

        internal List<RenderChunk> GetRenderChunks()
        {
            return renderChunks;
        }

        internal virtual void AddRenderInstanceInfo(int instancingKeyIndex, ShieldPostprocess.RenderInstanceInfo info, RenderChunk chunk)
        {
            info.mesh = chunk.renderMesh;
            info.material = chunk.renderMaterial;
            info.submeshIndex = chunk.subMeshIndex;

            for (int i = 0; i < chunk.renderers.Count; i++)
            {
                Renderer r = chunk.renderers[i];

                if (!r.isVisible)
                {
                    continue;
                }

                info.matrices[info.matricesCount] = r.localToWorldMatrix;
                info.matricesCount++;
            }
        }

        internal virtual void RenderNoInstancing(int instancingKeyIndex, CommandBuffer cmd, RenderChunk chunk)
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

        private void OnBecameVisible()
        {
            if (!isPrepared)
            {
                Prepare();
            }

            if (instancingKeysNeedRefresh)
            {
                UpdateInstancingKeys();
            }

            for (int i = 0; i < thisRendererChunks.Count; i++)
            {
                RenderChunk rc = thisRendererChunks[i];
                rc.visibleRenderers++;

                if (!rc.isInRenderList)
                {
                    OnAddToRenderList(rc);
                }
            }
        }

        private void OnBecameInvisible()
        {
            for (int i = 0; i < thisRendererChunks.Count; i++)
            {
                RenderChunk rc = thisRendererChunks[i];
                rc.visibleRenderers--;

                if (rc.visibleRenderers == 0)
                {
                    OnRemoveFromRenderList(rc);
                }
            }
        }
    }
}
