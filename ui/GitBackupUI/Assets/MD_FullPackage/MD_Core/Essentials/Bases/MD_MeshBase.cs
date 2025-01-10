using UnityEngine;

using MDPackage.Utilities;

#if UNITY_EDITOR
using UnityEditor;

using MDPackage;
#endif

namespace MDPackage
{
    /// <summary>
    /// MD(Mesh Deformation): Mesh base class in Mesh Deformation Package.
    /// Base mesh class for objects with mesh-related behaviour. Implement this base class to any script that will work with Unity meshes and Mesh Deformation Package.
    /// Nested inheritation continues to the MD_ModifierBase (modifiers) and MD_GeometryBase (geometry and primitives).
    /// Written by Matej Vanco, 2022
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public abstract class MD_MeshBase : MonoBehaviour
    {
        // Public Fields & Props

        public MeshFilter MbMeshFilter { get => _mbMeshFilter; protected set => _mbMeshFilter = value; }
        [SerializeField] private MeshFilter _mbMeshFilter;

        public bool updateEveryFrame = true;
        public bool recalculateNormals = true;
        public bool useNormalSmoothingAngle = false;
        public float normalSmoothingAngle = 90.0f;
        public bool recalculateBounds = true;

        // Public Methods

        /// <summary>
        /// Process complete mesh update - process calculation, pass data to the mesh and recalculate surface
        /// </summary>
        public void MDMeshBase_ProcessCompleteMeshUpdate()
        {
            MDMeshBase_ProcessCalculations();
            MDMeshBase_UpdateMesh();
            MDMeshBase_RecalculateMesh();
        }

        // Protected Methods

        /// <summary>
        /// Check if everything is alright and the MeshFilter is ok
        /// </summary>
        /// <returns>Returns true if all the required parameters have been set</returns>
        protected bool MDMeshBase_CheckForMeshFilter(bool checkMeshFilterMesh = false)
        {
            if (!MbMeshFilter)
            {
                MD_Debug.Debug(this, "The gameObject '" + gameObject.name + "' does not contain MeshFilter component", MD_Debug.DebugType.Error);
                return false;
            }
            if (checkMeshFilterMesh && !MbMeshFilter.sharedMesh)
            {
                MD_Debug.Debug(this, "The gameObject '" + gameObject.name + "' does not contain mesh source inside the MeshFilter component", MD_Debug.DebugType.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Async method for safe self-destroy
        /// </summary>
        protected async void MDMeshBase_DestroySelf()
        {
            await System.Threading.Tasks.Task.Delay(50);
            if (Application.isPlaying)
                Destroy(this);
            else
                DestroyImmediate(this);
        }

        // Inheritance

        /// <summary>
        /// Initialize and cache all the required fields - required to call if inherited from this class
        /// </summary>
        /// <param name="affectUpdateEveryFrameField">Affect 'Update every frame' field? This field may get disabled if vertex count is exceeded</param>
        /// <param name="checkMeshFilterMesh">Check if the mesh filter has a mesh source?</param>
        public virtual void MDMeshBase_InitializeBase(bool affectUpdateEveryFrameField = true, bool checkMeshFilterMesh = false)
        {
            MbMeshFilter = GetComponent<MeshFilter>();

            if (!MDMeshBase_CheckForMeshFilter(checkMeshFilterMesh))
                return;

            var preferences = MD_Preferences.SelectPreferencesAsset();

            if (affectUpdateEveryFrameField && MbMeshFilter.sharedMesh)
                updateEveryFrame = preferences.VertexLimit > MbMeshFilter.sharedMesh.vertexCount;

            recalculateBounds = preferences.AutoRecalculateBoundsAsDefault;
            recalculateNormals = preferences.AutoRecalculateNormalsAsDefault;
            useNormalSmoothingAngle = preferences.SmoothAngleNormalsRecalculationAsDefault;
        }

        /// <summary>
        /// Implement this method for updating the main mesh with the new mesh-data (vertices, triangles etc) - See other classes that inherit from this class
        /// </summary>
        public abstract void MDMeshBase_UpdateMesh();

        /// <summary>
        /// Implement this method for processing certain mesh calculations - See other classes that inherit from this class
        /// </summary>
        public abstract void MDMeshBase_ProcessCalculations();

        /// <summary>
        /// Recalculate mesh bounds & normals
        /// </summary>
        public virtual void MDMeshBase_RecalculateMesh(bool forceNormals = false, bool forceBounds = false)
        {
            if (recalculateNormals || forceNormals)
            {
                if (!useNormalSmoothingAngle)
                    MbMeshFilter.sharedMesh.RecalculateNormals();
                else
                    MD_Utilities.RecalculateNormalsSmoothingAngle.RecalculateNormals(MbMeshFilter.sharedMesh, normalSmoothingAngle);
            }

            if (recalculateBounds || forceBounds)
                MbMeshFilter.sharedMesh.RecalculateBounds();
        }
    }
}

#if UNITY_EDITOR
namespace MDPackage_Editor
{
    /// <summary>
    /// Base editor for the MeshBase instances - all the class instances that inherit from the MD_MeshBase MUST have the Unity-editor implemented!
    /// </summary>
    [CustomEditor(typeof(MD_MeshBase), true)]
    public abstract class MD_MeshBase_Editor : MD_EditorUtilities
    {
        protected MD_MeshBase mMeshBase;
        protected bool showUpdateEveryFrame = true;
        private bool meshBaseFoldout = false;

        public abstract void OnEnable();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (!mMeshBase)
            {
                DrawDefaultInspector();
                return;
            }

            MDE_s();
            MDE_v();
            meshBaseFoldout = MDE_f(meshBaseFoldout, "Mesh Base Settings");
            if (meshBaseFoldout)
            {
                MDE_v();
                if (showUpdateEveryFrame)
                    MDE_DrawProperty("updateEveryFrame", "Update Every Frame", "Update current mesh and modifier every frame (default Update)");
                MDE_DrawProperty("recalculateNormals", "Recalculate Normals", "Recalculate normals automatically");
                MDE_plus();
                MDE_DrawProperty("useNormalSmoothingAngle", "Use Normal Smoothing Angle", "Allows for adjustment of normals smoothing angle, takes more performance (fits for seam-based meshes)");
                if (mMeshBase.useNormalSmoothingAngle)
                    MDE_DrawProperty("normalSmoothingAngle", "Normal Smoothing Angle");
                MDE_minus();
                MDE_DrawProperty("recalculateBounds", "Recalculate Bounds", "Recalculate bounds automatically");

                if ((!mMeshBase.updateEveryFrame && showUpdateEveryFrame) || !mMeshBase.recalculateBounds || !mMeshBase.recalculateNormals)
                {
                    MDE_s(5);
                    MDE_l("Manual Mesh Controls", true);
                    MDE_h();
                    if (!mMeshBase.updateEveryFrame && showUpdateEveryFrame)
                    {
                        if (MDE_b("Update Mesh"))
                        {
                            mMeshBase.MDMeshBase_ProcessCalculations();
                            mMeshBase.MDMeshBase_UpdateMesh();
                            mMeshBase.MDMeshBase_RecalculateMesh();
                        }
                    }
                    if (!mMeshBase.recalculateNormals)
                    {
                        if (MDE_b("Recalculate Normals"))
                            mMeshBase.MDMeshBase_RecalculateMesh(forceNormals: true);
                    }
                    if (!mMeshBase.recalculateBounds)
                    {
                        if (MDE_b("Recalculate Bounds"))
                            mMeshBase.MDMeshBase_RecalculateMesh(forceBounds: true);
                    }
                    MDE_he();
                }
                MDE_ve();
                if (MDE_b("Save Mesh To Assets", "Save current mesh to the assets folder as prefab"))
                {
                    MD_Utilities.Specific.SaveMeshToAssets_EditorOnly(mMeshBase.MbMeshFilter);
                    return;
                }
            }
            MDE_ve();
        }
    }
}
#endif