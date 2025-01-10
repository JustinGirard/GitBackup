using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

using MDPackage;
using MDPackage.Modifiers;
#endif

namespace MDPackage.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Mesh Bend.
    /// Bend mesh by the specific value to the specific direction.
    /// Written by Matej Vanco, 2015
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAG_PATH_NAME + "Modifiers/Mesh Bend")]
    public sealed class MDM_Bend : MD_ModifierBase
    {
        public enum BendDirection { X, Y, Z }
        public BendDirection bendDirection = BendDirection.X;

        public float bendValue = 0;
        public bool bendMirrored = true;

        #region Event Subscription

        protected override void OnEnable()
        {
            base.OnEnable();
            OnMeshSubdivided += ResetBendParams;
            OnMeshSmoothed += ResetBendParams;
            OnMeshBaked += ResetBendParams;
            OnMeshRestored += ResetBendParams;
            OnNewMeshReferenceCreated += ResetBendParams;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnMeshSubdivided -= ResetBendParams;
            OnMeshSmoothed -= ResetBendParams;
            OnMeshBaked -= ResetBendParams;
            OnMeshRestored -= ResetBendParams;
            OnNewMeshReferenceCreated -= ResetBendParams;
        }

        private void ResetBendParams()
        {
            bendValue = 0.0f;
            MDModifier_ProcessModifier();
        }

        #endregion

        #region Base overrides

        protected override void MDModifier_InitializeBase(MeshReferenceType meshReferenceType = MeshReferenceType.GetFromPreferences, bool forceInitialization = false, bool affectUpdateEveryFrameField = true)
        {
            if (MbIsInitialized && !forceInitialization)
                return;

            base.MDModifier_InitializeBase(meshReferenceType, forceInitialization, affectUpdateEveryFrameField);

            Bend_RegisterCurrentState();
        }

        public override void MDModifier_ProcessModifier()
        {
            if (!MbIsInitialized)
                return;

            for (int i = 0; i < MbBackupMeshData.vertices.Length; i++)
                MbWorkingMeshData.vertices[i] = BendVertex(MbBackupMeshData.vertices[i], bendValue);

            MDMeshBase_UpdateMesh();
            MDMeshBase_RecalculateMesh();
        }

        #endregion

        #region Bend essentials

        /// <summary>
        /// Bend calculation for specific vertex
        /// </summary>
        /// <param name="vert">Vertex vector</param>
        /// <param name="val">Entry bend value</param>
        /// <returns>Returns calculated vertex bend</returns>
        private Vector3 BendVertex(Vector3 vert, float val)
        {
            if (val == 0.0f) 
                return vert;
            if (!bendMirrored && vert.y < 0) 
                return vert;

            float rotExpl;
            float rotS;
            float rotC;

            switch (bendDirection)
            {
                case BendDirection.X:
                    rotExpl = (Mathf.PI / 2) + (val * vert.z);

                    rotS = Mathf.Sin(rotExpl) * ((1 / val) + vert.x);
                    rotC = Mathf.Cos(rotExpl) * ((1 / val) + vert.x);

                    vert.z = -rotC;
                    vert.x = rotS - (1 / val);
                    break;

                case BendDirection.Y:
                    rotExpl = (Mathf.PI / 2) + (val * vert.y);

                    rotS = Mathf.Sin(rotExpl) * ((1 / val) + vert.x);
                    rotC = Mathf.Cos(rotExpl) * ((1 / val) + vert.x);

                    vert.y = -rotC;
                    vert.x = rotS - (1 / val);
                    break;

                case BendDirection.Z:
                    rotExpl = (Mathf.PI / 2) + (val * vert.x);

                    rotS = Mathf.Sin(rotExpl) * ((1 / val) + vert.z);
                    rotC = Mathf.Cos(rotExpl) * ((1 / val) + vert.z);

                    vert.x = -rotC;
                    vert.z = rotS - (1 / val);
                    break;
            }

            return vert;
        }

        /// <summary>
        /// Refresh & register current mesh state. This will override the backup vertices to the current mesh state
        /// </summary>
        public void Bend_RegisterCurrentState()
        {
            MDModifier_InitializeMeshData(false);
            bendValue = 0;
        }

        /// <summary>
        /// Bend object by the UI Slider value
        /// </summary>
        public void Bend_BendObject(Slider entry)
        {
            bendValue = entry.value;
            if (!updateEveryFrame)
                MDModifier_ProcessModifier();
        }

        /// <summary>
        /// Bend object by the float value
        /// </summary>
        /// <param name="entry"></param>
        public void Bend_BendObject(float entry)
        {
            bendValue = entry;
            if (!updateEveryFrame)
                MDModifier_ProcessModifier();
        }

        #endregion
    }
}

#if UNITY_EDITOR
namespace MDPackage_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_Bend))]
    public sealed class MDM_Bend_Editor : MD_ModifierBase_Editor
    {
        private MDM_Bend mb;

        public override void OnEnable()
        {
            base.OnEnable();
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            mb = (MDM_Bend)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("Bend Modifier", true);
            MDE_v();
            MDE_DrawProperty("bendDirection", "Bend Direction");
            MDE_DrawProperty("bendValue", "Bend Value");
            MDE_DrawProperty("bendMirrored", "Mirrored", "If enabled, the bend will process on both sides of the mesh");
            if (MDE_b("Register Mesh")) mb.Bend_RegisterCurrentState();
            MDE_hb("Refresh current mesh & register backup vertices to the edited vertices");
            MDE_ve();
            MDE_s();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif

