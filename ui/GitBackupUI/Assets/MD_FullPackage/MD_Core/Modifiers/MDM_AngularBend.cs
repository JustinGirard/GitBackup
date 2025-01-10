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
    /// MDM(Mesh Deformation Modifier): Angular Mesh Bend.
    /// Angular Bend modifier by the specific value to the specific direction.
    /// Written by Matej Vanco, 2024.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAG_PATH_NAME + "Modifiers/Angular Mesh Bend")]
    public sealed class MDM_AngularBend : MD_ModifierBase
    {
        public float bendValue = 0;
        [Range(-180, 180)] public float bendAngle = 0;

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
            bendAngle = 0.0f;
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
                MbWorkingMeshData.vertices[i] = BendVertex(MbBackupMeshData.vertices[i], bendValue, bendAngle);

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
        /// <param name="angle">Entry angle value</param>
        /// <returns>Returns calculated vertex bend</returns>
        private Vector3 BendVertex(Vector3 vert, float val, float angle)
        {
            return Quaternion.AngleAxis(
                Mathf.InverseLerp(MbMeshFilter.sharedMesh.bounds.min.y, MbMeshFilter.sharedMesh.bounds.max.y, vert.y) * val, 
                Quaternion.Euler(0, angle, 0) * Vector3.forward) * vert;
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
        /// Bend current object with UI slider value
        /// </summary>
        public void Bend_BendObject(Slider value)
        {
            bendAngle = value.value;
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
    [CustomEditor(typeof(MDM_AngularBend))]
    public sealed class MDM_AngularBend_Editor : MD_ModifierBase_Editor
    {
        private MDM_AngularBend mb;

        public override void OnEnable()
        {
            base.OnEnable();
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            mb = (MDM_AngularBend)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("Vertical Bend Modifier", true);
            MDE_v();
            MDE_DrawProperty("bendAngle", "Bend Angle");
            MDE_DrawProperty("bendValue", "Bend Value");
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

