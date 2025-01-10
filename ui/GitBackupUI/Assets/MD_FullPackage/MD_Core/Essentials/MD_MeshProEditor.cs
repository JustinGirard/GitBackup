using System;
using System.Collections.Generic;
using System.Collections;

using UnityEngine;

using MDPackage.Modifiers;
using MDPackage.Geometry;
using MDPackage.Utilities;

using Random = UnityEngine.Random;

namespace MDPackage
{
    /// <summary>
    /// MD(Mesh Deformation): Mesh Pro Editor.
    /// Essential component for general mesh processing and cross-bridge between Mesh Deformation elements.
    /// Written by Matej Vanco, 2013
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAG_PATH_NAME + "Mesh Pro Editor")]
    public sealed class MD_MeshProEditor : MonoBehaviour
    {
        // >>> >>> >>> Serialized & public fields

        public Action onMeshProEditorInitialized;

        public bool meshCreateNewReferenceAfterCopy = true;
        public bool meshUpdateEveryFrame = true;
        public bool meshAnimationMode = false;
        public bool meshSmoothAngleNormalsRecalc = false;
        public float meshNormalsSmoothAngle = 90.0f;

        // Vertex Editor
        [SerializeField] private List<Transform> meshWorkingPoints = new List<Transform>();

        [SerializeField] private Transform vertModifGeneratedPointsRootContainer;
        [SerializeField] private bool vertModifUseCustomPattern = false;
        [SerializeField] private GameObject vertModifCustomPatternReference;
        [SerializeField] private bool vertModifUseCustomPatternColor = false;
        [SerializeField] private Color vertModifCustomPatternColor = Color.red;
        [SerializeField] private float vertModifPointSizeMultiplier = 1.0f;

        // Mesh info
        [SerializeField] private string meshInfoMeshName;
        [SerializeField] private Mesh meshInitialMesh;

        [SerializeField] private Material meshDefaultMaterial;

        [SerializeField] private bool meshAlreadyAwake;
        [SerializeField] private bool meshBornAsSkinnedMesh = false;
        [SerializeField] private MeshFilter meshCachedMeshFilter;

        // >>> >>> >>> Properties
        public bool MeshAlreadyAwake { get => meshAlreadyAwake; private set => meshAlreadyAwake = value; }
        public bool MeshBornAsSkinnedMesh { get => meshBornAsSkinnedMesh; private set => meshBornAsSkinnedMesh = value; }
        public MeshFilter MeshCachedMeshFilter { get => meshCachedMeshFilter; private set => meshCachedMeshFilter = value; }

        public IReadOnlyList<Transform> GeneratedMeshPoints => meshWorkingPoints;
        public Transform GeneratedPointsRootContainer => vertModifGeneratedPointsRootContainer;
        public bool HasGeneratedVerticePoints => GeneratedPointsRootContainer != null && meshWorkingPoints != null && meshWorkingPoints.Count > 0;

        public bool HasCustomPointPatternReference => vertModifUseCustomPattern;
        public bool HasCustomPointPatternColor => vertModifUseCustomPatternColor;

#if UNITY_EDITOR
        private void Reset()
        {
            MeshCachedMeshFilter = GetComponent<MeshFilter>();
        }
#endif
        private void Awake()
        {
            if (!MeshCachedMeshFilter)
                MeshCachedMeshFilter = GetComponent<MeshFilter>();

            if (!MeshCachedMeshFilter.sharedMesh)
            {
                MD_Debug.Debug(this, "Mesh Filter doesn't contain any mesh data. The behaviour will be destroyed", MD_Debug.DebugType.Error);
                if (Application.isPlaying)
                    Destroy(this);
                else
                    DestroyImmediate(this);
            }

            if (Application.isPlaying)
                return;

            if (!Restricted())
                Init();
        }

        private void OnDestroy()
        {
            meshAnimationMode = false;
            MPE_ClearPointsEditor();
        }

        private IEnumerator Start()
        {
            if (!Application.isPlaying)
                yield break;

            yield return null;

            if (!Restricted())
                Init();
        }

        private bool Restricted()
        {
            if (!MD_Utilities.Specific.RestrictFromOtherTypes(gameObject, GetType(), new Type[] { typeof(MD_GeometryBase), typeof(MD_ModifierBase) }))
            {
                MD_Debug.Debug(this, "The mesh-editor cannot be applied to this object, because the object already contains other modifiers or components that work with mesh-vertices. Please remove the existing mesh-related-components to access the mesh editor");
                if (Application.isPlaying)
                    Destroy(this);
                else
                    DestroySelf();
                return true;
            }
            return false;
        }

        private async void DestroySelf()
        {
            await System.Threading.Tasks.Task.Delay(64);
            DestroyImmediate(this);
        }

        private void Init()
        {
            if (meshDefaultMaterial == null)
                meshDefaultMaterial = new Material(MD_Utilities.Specific.GetProperPipelineDefaultShader());

            var preferences = MD_Preferences.SelectPreferencesAsset();

            if (!MeshAlreadyAwake)
            {
                if (string.IsNullOrEmpty(meshInfoMeshName))
                    meshInfoMeshName = "NewMesh" + Random.Range(1, 99999).ToString();
                meshSmoothAngleNormalsRecalc = preferences.SmoothAngleNormalsRecalculationAsDefault;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (preferences.PopupEditorWindowIfAnyNotification)
                    {
                        if (UnityEditor.EditorUtility.DisplayDialog("Create a New Reference?", "Would you like to create a new reference? If yes (recommended), a brand new mesh reference will be created. If no, existing mesh references will share the same data as this mesh reference", "Yes", "No"))
                            Internal_MPE_ResetReference();
                    }
                    else if (preferences.CreateNewMeshReferenceAsDefault)
                        Internal_MPE_ResetReference();
                }
                else if (!meshAnimationMode && preferences.CreateNewMeshReferenceAsDefault)
                    Internal_MPE_ResetReference();
#else
                if (!meshAnimationMode && preferences.CreateNewMeshReferenceAsDefault)
                     Internal_MPE_ResetReference();
#endif
                Internal_MPE_ReceiveMeshInfo();

                MeshAlreadyAwake = true;
            }
            else if (meshCreateNewReferenceAfterCopy)
                Internal_MPE_ResetReference();

            onMeshProEditorInitialized?.Invoke();
        }

        private void Internal_MPE_ResetReference()
        {
            if (MeshCachedMeshFilter == null) 
                return;

            if (HasGeneratedVerticePoints)
                MPE_CreatePointsEditor();
            else
                MPE_ClearPointsEditor();

            Mesh newMesh = MD_Utilities.Specific.CreateNewMeshReference(MeshCachedMeshFilter.sharedMesh);
            newMesh.name = meshInfoMeshName;
            MeshCachedMeshFilter.sharedMesh = newMesh;

            Internal_MPE_ReceiveMeshInfo();
        }

        private void Internal_MPE_ReceiveMeshInfo(bool passAlreadyAwake = false)
        {
            if (!MeshCachedMeshFilter || !MeshCachedMeshFilter.sharedMesh)
                return;

            if (!MeshAlreadyAwake || passAlreadyAwake)
            {
                Mesh myMesh = MeshCachedMeshFilter.sharedMesh;
                meshInitialMesh = Instantiate(myMesh);
                meshInitialMesh.name = myMesh.name;
            }
        }

        private void Update() 
        {
            if (meshUpdateEveryFrame)
                MPE_UpdateMesh();
        }

        #region Public methods

        // Mesh-essentials

        /// <summary>
        /// Update current mesh state (sync generated points with the mesh vertices) & recalculate normals + bounds
        /// </summary>
        public void MPE_UpdateMesh()
        {
            if (MeshCachedMeshFilter == null || MeshCachedMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            if (!HasGeneratedVerticePoints) 
                return;
            if (MeshCachedMeshFilter.sharedMesh.vertexCount != meshWorkingPoints.Count)
                return;

            Vector3[] meshWorkingVertices = MeshCachedMeshFilter.sharedMesh.vertices;

            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            for (int i = 0; i < meshWorkingVertices.Length; i++)
            {
                if (meshWorkingPoints.Count > i)
                {
                    if (meshWorkingPoints[i] != null)
                        meshWorkingVertices[i] = new Vector3(
                            meshWorkingPoints[i].position.x - MeshCachedMeshFilter.transform.position.x,
                            meshWorkingPoints[i].position.y - MeshCachedMeshFilter.transform.position.y,
                            meshWorkingPoints[i].position.z - MeshCachedMeshFilter.transform.position.z);
                }
            }

            MeshCachedMeshFilter.sharedMesh.vertices = meshWorkingVertices;

            MPE_RecalculateMeshNormalsAndBounds();
        }

        /// <summary>
        /// Recalculate current mesh normals and bounds
        /// </summary>
        /// <param name="ignoreOptimizeMesh">Ignore optimization? If yes, the mesh gets always recalculated</param>
        public void MPE_RecalculateMeshNormalsAndBounds()
        {
            if (MeshCachedMeshFilter == null || MeshCachedMeshFilter.sharedMesh == null)
                return;

            if (!meshSmoothAngleNormalsRecalc)
                MeshCachedMeshFilter.sharedMesh.RecalculateNormals();
            else
                MD_Utilities.RecalculateNormalsSmoothingAngle.RecalculateNormals(MeshCachedMeshFilter.sharedMesh, meshNormalsSmoothAngle);

            MeshCachedMeshFilter.sharedMesh.RecalculateBounds();
        }

        // Mesh Editor Vertices

        /// <summary>
        /// Hide/Show generated points on the mesh
        /// </summary>
        public void MPE_ShowHideGeneratedPoints(bool activeState)
        {
            if (!HasGeneratedVerticePoints)
                return;

            foreach(var p in meshWorkingPoints)
            {
                if (p != null && p.TryGetComponent(out Renderer r))
                    r.enabled = activeState;
            }
        }

        /// <summary>
        /// Set 'Ignore Raycast' layer to all generated points
        /// </summary>
        public void MPE_IgnoreRaycastForGeneratedPoints(bool ignoreRaycast)
        {
            if (!HasGeneratedVerticePoints)
                return;
            foreach (var p in meshWorkingPoints)
            {
                if (p != null)
                    p.gameObject.layer = ignoreRaycast ? 2 : 0;
            }
        }

        /// <summary>
        /// Create a points/vertex editor on the current mesh
        /// </summary>
        /// <param name="bypassVertexLimit">Notification box will popup if the vertex limit is over the specific value</param>
        public void MPE_CreatePointsEditor(bool bypassVertexLimit = false)
        {
            if (MeshCachedMeshFilter == null || MeshCachedMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            if (meshAnimationMode) 
                return;

            MPE_ClearPointsEditor();

            if (!bypassVertexLimit && MeshCachedMeshFilter.sharedMesh.vertexCount > MD_Preferences.SelectPreferencesAsset().VertexLimit)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying && UnityEditor.EditorUtility.DisplayDialog("Error", $"The mesh has more vertices than recommended vertex count ({MeshCachedMeshFilter.sharedMesh.vertexCount}>{MD_Preferences.SelectPreferencesAsset().VertexLimit}). " +
                    $"Vertex editor won't be generated... If you would like to generate the vertex editor, increase the vertex limit count in the preferences or manually create the vertex editor and bypass the vertex limit", "OK"))
                    return;
#endif
                MD_Debug.Debug(this, $"The mesh has more vertices than recommended vertex count ({MeshCachedMeshFilter.sharedMesh.vertexCount}>{MD_Preferences.SelectPreferencesAsset().VertexLimit}). " +
                    $"Vertex editor won't be generated... If you would like to generate the vertex editor, increase the vertex limit count in the preferences or manually create the vertex editor and bypass the vertex limit", MD_Debug.DebugType.Info);
                return;
            }

            transform.parent = null;

            Vector3 lastScale = transform.localScale;
            Quaternion lastRotation = transform.rotation;

            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            GameObject vertRootContainer = new GameObject(name + "_VertexRoot");
            vertModifGeneratedPointsRootContainer = vertRootContainer.transform;
            vertRootContainer.transform.position = Vector3.zero;

            if (meshWorkingPoints == null)
                meshWorkingPoints = new List<Transform>();

            meshWorkingPoints.Clear();

            Vector3[] vertices = MeshCachedMeshFilter.sharedMesh.vertices;
            Material matInstance = new Material(MD_Utilities.Specific.GetProperPipelineDefaultShader());
            if (vertModifUseCustomPatternColor)
                matInstance.color = vertModifCustomPatternColor;
            else
                matInstance.color = Color.red;

            for (int i = 0; i < vertices.Length; i++)
            {
                GameObject gm;

                if (vertModifUseCustomPattern && vertModifCustomPatternReference != null)
                {
                    gm = Instantiate(vertModifCustomPatternReference);
                    if (gm.TryGetComponent(out Renderer rend) && vertModifUseCustomPatternColor)
                        rend.sharedMaterial.color = vertModifCustomPatternColor;
                }
                else
                {
                    gm = MDG_Octahedron.CreateGeometryAndDispose<MDG_Octahedron>();
                    gm.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                    gm.GetComponentInChildren<Renderer>().material = matInstance;
                }
				
                if(vertModifPointSizeMultiplier != 1)
                    gm.transform.localScale = Vector3.one * vertModifPointSizeMultiplier;
					
                gm.transform.parent = vertRootContainer.transform;

                gm.transform.position = vertices[i];
                meshWorkingPoints.Add(gm.transform);

                gm.name = "P" + i.ToString();
            }

            int counter = 0;
            foreach (Transform onePoint in meshWorkingPoints)
            {
                if (onePoint.gameObject.activeInHierarchy == false)
                    continue;
                foreach (Transform secondPoint in meshWorkingPoints)
                {
                    if (secondPoint.name == onePoint.name)
                        continue;
                    if (secondPoint.transform.position == onePoint.transform.position)
                    {
                        secondPoint.hideFlags = HideFlags.HideInHierarchy;
                        secondPoint.transform.parent = onePoint.transform;
                        secondPoint.gameObject.SetActive(false);
                    }
                }
                counter++;
                onePoint.hideFlags = HideFlags.None;
                onePoint.gameObject.SetActive(true);
                onePoint.gameObject.AddComponent<SphereCollider>();
                onePoint.name = "P" + counter.ToString();
            }

            MeshCachedMeshFilter.sharedMesh.vertices = vertices;
            MeshCachedMeshFilter.sharedMesh.MarkDynamic();

            vertRootContainer.transform.parent = MeshCachedMeshFilter.transform;
            vertRootContainer.transform.localPosition = Vector3.zero;

            if (!MeshBornAsSkinnedMesh)
            {
                vertRootContainer.transform.localScale = lastScale;
                vertRootContainer.transform.rotation = lastRotation;
            }

            Internal_MPE_ReceiveMeshInfo();
        }

        /// <summary>
        /// Create a points/vertex editor on the current mesh with custom point object pattern
        /// </summary>
        public void MPE_CreatePointsEditor(GameObject customPatternObjectReference, float sizeMultiplier = 1f, bool bypassVertexLimit = false)
        {
            vertModifUseCustomPattern = true;
            vertModifCustomPatternReference = customPatternObjectReference;
            vertModifPointSizeMultiplier = sizeMultiplier;
            MPE_CreatePointsEditor(bypassVertexLimit);
        }

        /// <summary>
        /// Create a points/vertex editor on the current mesh with custom point color
        /// </summary>
        public void MPE_CreatePointsEditor(Color customPointColor, float sizeMupliplier = 1f, bool bypassVertexLimit = false)
        {
            vertModifUseCustomPatternColor = true;
            vertModifCustomPatternColor = customPointColor;
            vertModifPointSizeMultiplier = sizeMupliplier;
            MPE_CreatePointsEditor(bypassVertexLimit);
        }

        /// <summary>
        /// Clear points/vertex editor if possible
        /// </summary>
        public void MPE_ClearPointsEditor()
        {
            if (meshAnimationMode) 
                return;

            if (meshWorkingPoints != null && meshWorkingPoints.Count > 0)
            {
                for (int i = meshWorkingPoints.Count - 1; i >= 0; i--)
                {
                    if (meshWorkingPoints[i] == null) continue;
                    if (!Application.isPlaying)
                        DestroyImmediate(meshWorkingPoints[i].gameObject);
                    else
                        Destroy(meshWorkingPoints[i].gameObject);
                }
                meshWorkingPoints.Clear();
            }

            if (vertModifGeneratedPointsRootContainer != null)
            {
                if(!Application.isPlaying)
                    DestroyImmediate(vertModifGeneratedPointsRootContainer.gameObject);
                else
                    Destroy(vertModifGeneratedPointsRootContainer.gameObject);
            }
        }

        // Mesh Combine

        /// <summary>
        /// Combine all sub-meshes with the current mesh. This will create a brand new gameObject & notification will popup
        /// </summary>
        public void MPE_CombineMeshAndCreateNewInstance()
        {
            if (MeshCachedMeshFilter == null || MeshCachedMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            MPE_ClearPointsEditor();

#if UNITY_EDITOR
            if(!Application.isPlaying)
            {
                if(!UnityEditor.EditorUtility.DisplayDialog("Are you sure to combine meshes?", "If you combine the mesh with it's sub-meshes, materials and all the components will be lost. Are you sure to combine meshes? Undo won't record this process.", "Yes, proceed", "No, cancel"))
                    return;
            }
#endif
            transform.parent = null;
            Vector3 lastPosition = transform.position;
            transform.position = Vector3.zero;

            MeshFilter[] meshes_ = GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combiners_ = new CombineInstance[meshes_.Length];

            int counter_ = 0;
            while (counter_ < meshes_.Length)
            {
                combiners_[counter_].mesh = meshes_[counter_].sharedMesh;
                combiners_[counter_].transform = meshes_[counter_].transform.localToWorldMatrix;
                if (meshes_[counter_].gameObject != gameObject)
                {
                    if (!Application.isPlaying)
                        DestroyImmediate(meshes_[counter_].gameObject);
                    else
                        Destroy(meshes_[counter_].gameObject);
                }
                counter_++;
            }

            GameObject newgm = new GameObject();
            MeshFilter f = newgm.AddComponent<MeshFilter>();
            newgm.AddComponent<MeshRenderer>();
            newgm.name = name;

            Mesh newMesh = new Mesh();
            newMesh.CombineMeshes(combiners_);

            f.sharedMesh = newMesh;
            f.sharedMesh.name = meshInfoMeshName;
            newgm.GetComponent<Renderer>().material = meshDefaultMaterial;
            newgm.AddComponent<MD_MeshProEditor>().meshInfoMeshName = meshInfoMeshName;

            newgm.transform.position = lastPosition;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Selection.activeGameObject = newgm;
                UnityEditor.EditorUtility.DisplayDialog("Successfully combined!", "If your mesh has been successfully combined, please notice that the prefab of the 'old' mesh in the Assets Folder is no more valid for the new one. " +
                    "If you want to store the new mesh, you have to save your mesh prefab again.", "OK");
            }
#endif
            if(!Application.isPlaying)
                DestroyImmediate(this.gameObject);
            else
                Destroy(this.gameObject);
        }

        /// <summary>
        /// Combine all sub-meshes with current the mesh. This will NOT create a new gameObject & notification will not popup
        /// </summary>
        public void MPE_CombineMeshImmediately()
        {
            if (MeshCachedMeshFilter == null || MeshCachedMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            MPE_ClearPointsEditor();

            transform.parent = null;

            Vector3 lastPosition = transform.position;
            transform.position = Vector3.zero;

            MeshFilter[] meshes_ = GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combiners_ = new CombineInstance[meshes_.Length];

            long counter_ = 0;
            while (counter_ < meshes_.Length)
            {
                combiners_[counter_].mesh = meshes_[counter_].sharedMesh;
                combiners_[counter_].transform = meshes_[counter_].transform.localToWorldMatrix;
                if (meshes_[counter_].gameObject != this.gameObject)
                {
                    if (!Application.isPlaying)
                        DestroyImmediate(meshes_[counter_].gameObject);
                    else
                        Destroy(meshes_[counter_].gameObject);
                }
                counter_++;
            }

            Mesh newMesh = new Mesh();
            newMesh.CombineMeshes(combiners_);

            MeshCachedMeshFilter.sharedMesh = newMesh;
            MeshCachedMeshFilter.sharedMesh.name = meshInfoMeshName;

            transform.position = lastPosition;
            Internal_MPE_ReceiveMeshInfo();
        }

        // Mesh References

        /// <summary>
        /// Create a brand new object with a new mesh reference. All your components on the object will be lost!
        /// </summary>
        public void MPE_CreateNewReference()
        {
            if (MeshCachedMeshFilter == null || MeshCachedMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            MPE_ClearPointsEditor();

            GameObject newgm = new GameObject();
            MeshFilter f = newgm.AddComponent<MeshFilter>();
            newgm.AddComponent<MeshRenderer>();
            newgm.name = name;

            Material[] materials = GetComponent<Renderer>().sharedMaterials;

            Vector3 Last_POS = transform.position;
            transform.position = Vector3.zero;

            CombineInstance[] combine = new CombineInstance[1];
            combine[0].mesh = MeshCachedMeshFilter.sharedMesh;
            combine[0].transform = MeshCachedMeshFilter.transform.localToWorldMatrix;

            Mesh newMesh = new Mesh();
            newMesh.CombineMeshes(combine);

            f.sharedMesh = newMesh;
            newgm.SetActive(false);
            newgm.AddComponent<MD_MeshProEditor>().MeshAlreadyAwake = true;
            newgm.GetComponent<MD_MeshProEditor>().meshInfoMeshName = meshInfoMeshName;
            f.sharedMesh.name = meshInfoMeshName;

            if (materials.Length > 0)
                newgm.GetComponent<Renderer>().sharedMaterials = materials;
            newgm.transform.position = Last_POS;
            newgm.SetActive(true);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Selection.activeGameObject = newgm;
                if(MD_Preferences.SelectPreferencesAsset().PopupEditorWindowIfAnyNotification)
                     UnityEditor.EditorUtility.DisplayDialog("Warning", "If you change the reference of your mesh, please notice that the prefab of the 'old' mesh in Assets Folder is no more valid for the new one. " +
                    "If you would like to store a new mesh, you have to save your mesh prefab again.", "OK");
            }
#endif
            newgm.GetComponent<MD_MeshProEditor>().MeshCachedMeshFilter = f;
            newgm.GetComponent<MD_MeshProEditor>().Internal_MPE_ReceiveMeshInfo(true);

            if (!Application.isPlaying)
                DestroyImmediate(this.gameObject);
            else
                Destroy(this.gameObject);
        }

        /// <summary>
        /// Restore current mesh to its initial state
        /// </summary>
        public void MPE_RestoreMeshToOriginal()
        {
            if (MeshCachedMeshFilter == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter", MD_Debug.DebugType.Error);
                return;
            }

            if(meshInitialMesh == null)
            {
                MD_Debug.Debug(this, "Couldn't restore the original mesh data, because the initial mesh is null for some reason!", MD_Debug.DebugType.Error);
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if(meshAnimationMode)
                {
                    UnityEditor.EditorUtility.DisplayDialog("Can't continue", "Couldn't restore the original mesh data, because the Animation Mode is enabled.", "OK");
                    return;
                }
                if(!UnityEditor.EditorUtility.DisplayDialog("Are you sure?", "Are you sure to restore the original mesh data?", "Restore", "Cancel"))
                    return;
            }
#endif
            if (meshAnimationMode)
            {
                MD_Debug.Debug(this, "Couldn't restore the original mesh data, because the Animation Mode is enabled", MD_Debug.DebugType.Warning);
                return;
            }

            MPE_ClearPointsEditor();

            MeshCachedMeshFilter.sharedMesh = meshInitialMesh;
            MPE_RecalculateMeshNormalsAndBounds();

            Internal_MPE_ReceiveMeshInfo();
        }

        /// <summary>
        /// Convert skinned mesh renderer to a mesh renderer & mesh filter. This will create a new object, so none of the components will remain
        /// </summary>
        public void MPE_ConvertFromSkinnedToFilter()
        {
            SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
            if (!smr)
                return;
            if (smr.sharedMesh == null)
                return;

            GameObject newgm = new GameObject();
            MeshFilter f = newgm.AddComponent<MeshFilter>();
            newgm.AddComponent<MeshRenderer>();
            newgm.name = name + "_ConvertedMesh";

            Material[] mater = null;

            if (smr.sharedMaterials.Length > 0)
                mater = GetComponent<Renderer>().sharedMaterials;

            Vector3 lastPosition = transform.root.transform.position;
            Vector3 lastScale = transform.localScale;
            Quaternion lastRotation = transform.rotation;

            transform.position = Vector3.zero;

            Mesh newMesh = smr.sharedMesh;

            f.sharedMesh = newMesh;
            f.sharedMesh.name = meshInfoMeshName;
            if (mater.Length != 0)
                newgm.GetComponent<Renderer>().sharedMaterials = mater;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Selection.activeGameObject = newgm;
                if(MD_Preferences.SelectPreferencesAsset().PopupEditorWindowIfAnyNotification)
                    UnityEditor.EditorUtility.DisplayDialog("Successfully Converted!", "Your skinned mesh renderer has been successfully converted to the Mesh Filter and Mesh Renderer.", "OK");
            }
#endif

            newgm.AddComponent<MD_MeshProEditor>().MeshBornAsSkinnedMesh = true;

            newgm.transform.position = lastPosition;
            newgm.transform.rotation = lastRotation;
            newgm.transform.localScale = lastScale;

            if (!Application.isPlaying)
                DestroyImmediate(gameObject);
            else
                Destroy(gameObject);
        }

        // Internal mesh-features

        private Mesh intern_modif_sourceMesh;
        private Mesh intern_modif_workingMesh;

        /// <summary>
        /// Internal modifier - mesh smooth HC Filter
        /// </summary>
        public void MPE_SmoothMesh(float intensity = 0.5f)
        {
            if (MeshCachedMeshFilter == null || MeshCachedMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            MPE_ClearPointsEditor();

            if (!MD_Utilities.Specific.CheckVertexCountLimit(MeshCachedMeshFilter.sharedMesh.vertexCount))
                return;

            intern_modif_sourceMesh = new Mesh();
            intern_modif_sourceMesh = MeshCachedMeshFilter.sharedMesh;

            Mesh clone = new Mesh();

            clone.vertices = intern_modif_sourceMesh.vertices;
            clone.normals = intern_modif_sourceMesh.normals;
            clone.tangents = intern_modif_sourceMesh.tangents;
            clone.triangles = intern_modif_sourceMesh.triangles;

            clone.uv = intern_modif_sourceMesh.uv;
            clone.uv2 = intern_modif_sourceMesh.uv2;
            clone.uv2 = intern_modif_sourceMesh.uv2;

            clone.bindposes = intern_modif_sourceMesh.bindposes;
            clone.boneWeights = intern_modif_sourceMesh.boneWeights;
            clone.bounds = intern_modif_sourceMesh.bounds;

            clone.colors = intern_modif_sourceMesh.colors;
            clone.name = intern_modif_sourceMesh.name;

            intern_modif_workingMesh = clone;
            MeshCachedMeshFilter.mesh = intern_modif_workingMesh;

            intern_modif_workingMesh.vertices = MD_Utilities.VertexSmoothingHCFilter.HCFilter(intern_modif_sourceMesh.vertices, intern_modif_workingMesh.triangles, 0.0f, intensity);

            Mesh m = new Mesh();

            m.name = meshInfoMeshName;
            m.vertices = MeshCachedMeshFilter.sharedMesh.vertices;
            m.triangles = MeshCachedMeshFilter.sharedMesh.triangles;
            m.uv = MeshCachedMeshFilter.sharedMesh.uv;
            m.normals = MeshCachedMeshFilter.sharedMesh.normals;

            meshWorkingPoints.Clear();

            m = intern_modif_workingMesh;

            MeshCachedMeshFilter.sharedMesh = m;

            Internal_MPE_ReceiveMeshInfo();
        }

        /// <summary>
        /// Internal modifier - mesh subdivision
        /// </summary>
        public void MPE_SubdivideMesh(int Level)
        {
            if (MeshCachedMeshFilter == null || MeshCachedMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            MPE_ClearPointsEditor();

            if (!MD_Utilities.Specific.CheckVertexCountLimit(MeshCachedMeshFilter.sharedMesh.vertexCount))
                return;

            intern_modif_sourceMesh = new Mesh();
            intern_modif_sourceMesh = MeshCachedMeshFilter.sharedMesh;
            MD_Utilities.MeshSubdivision.Subdivide(intern_modif_sourceMesh, Level);
            MeshCachedMeshFilter.sharedMesh = intern_modif_sourceMesh;

            Mesh m = new Mesh();

            m.name = meshInfoMeshName;
            m.vertices = MeshCachedMeshFilter.sharedMesh.vertices;
            m.triangles = MeshCachedMeshFilter.sharedMesh.triangles;
            m.uv = MeshCachedMeshFilter.sharedMesh.uv;
            m.normals = MeshCachedMeshFilter.sharedMesh.normals;

            meshWorkingPoints.Clear();

            m = intern_modif_sourceMesh;

            MeshCachedMeshFilter.sharedMesh = m;

            Internal_MPE_ReceiveMeshInfo();
        }

        #endregion
    }
}