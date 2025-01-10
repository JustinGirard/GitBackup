using UnityEngine;
using UnityEditor;

using MDPackage;
using MDPackage.Modifiers;
using MDPackage.Utilities;

namespace MDPackage_Editor
{
    [CustomEditor(typeof(MD_MeshProEditor))]
    public sealed class MD_MeshProEditor_Editor : MD_EditorUtilities
    {
        private MD_MeshProEditor m;

        private void OnEnable()
        {
            m = (MD_MeshProEditor)target;

            style = new GUIStyle();
            style.richText = true;

            haveMeshFilter = m.GetComponent<MeshFilter>();
            haveSkinnedMesh = m.GetComponent<SkinnedMeshRenderer>();

            if (m.HasGeneratedVerticePoints)
                meshSelectedModification = EditorModification.Vertices;
        }

        // Gui
        private GUIStyle style;

        public GUIStyle styleTest;
        public Texture2D VerticesIcon;
        public Texture2D ColliderIcon;
        public Texture2D IdentityIcon;
        public Texture2D ModifyIcon;
        public Texture2D SmoothIcon;
        public Texture2D SubdivisionIcon;

        // Editor stuff
        private readonly bool[] Foldout = new bool[3];
        private readonly int[] DivisionLevels = new int[] { 2, 3, 4, 6, 8 };

        private float SmoothMeshIntens = 0.5f;
        private int divisionlevelSelection = 2;
        private bool haveMeshFilter;
        private bool haveSkinnedMesh;

        private enum EditorModification { None, Vertices, Collider, Identity, Mesh };
        private EditorModification meshSelectedModification;

        public override void OnInspectorGUI()
        {
            if (m == null)
            {
                DrawDefaultInspector();
                return;
            }

            if (!haveMeshFilter)
            {
                if (haveSkinnedMesh)
                {
                    if (MDE_b("Convert to Mesh Filter"))
                    {
                        if (MDE_dd("Are you sure?", "Are you sure to convert the skinned mesh renderer to the mesh filter? There is no way back (Undo won't record this process)", "Yes", "No"))
                            m.MPE_ConvertFromSkinnedToFilter();
                    }
                    MDE_hb("Skinned Mesh Renderer is a component that controls your mesh with bones. Press 'Convert To Mesh Filter' to start editing it's mesh source.", MessageType.Info);
                }
                else
                    MDE_hb("No mesh identity. In order to access the mesh editor, the object must contain Mesh Filter or Skinned Mesh Renderer component.", MessageType.Error);
                
                return;
            }

            MDE_s(20);

            #region Upper Categories

            MDE_h(false);
            if (MDE_b(new GUIContent("Vertices", VerticesIcon, "Point/Vertex Modification")))
            {
                if (meshSelectedModification == EditorModification.Vertices)
                {
                    meshSelectedModification = EditorModification.None;
                    m.MPE_ClearPointsEditor();
                }
                else
                {
                    meshSelectedModification = EditorModification.Vertices;
                    m.MPE_CreatePointsEditor();
                }
            }
            if (MDE_b(new GUIContent("Collider", ColliderIcon, "Collider Modification")))
            {
                m.MPE_ClearPointsEditor();
                if (meshSelectedModification == EditorModification.Collider)
                    meshSelectedModification = EditorModification.None;
                else
                    meshSelectedModification = EditorModification.Collider;
            }
            if (MDE_b(new GUIContent("Identity", IdentityIcon, "Identity Modification")))
            {
                m.MPE_ClearPointsEditor();
                if (meshSelectedModification == EditorModification.Identity)
                    meshSelectedModification = EditorModification.None;
                else
                    meshSelectedModification = EditorModification.Identity;
            }
            if (MDE_b(new GUIContent("Mesh", ModifyIcon, "Mesh Modification")))
            {
                m.MPE_ClearPointsEditor();
                if (meshSelectedModification == EditorModification.Mesh)
                    meshSelectedModification = EditorModification.None;
                else
                    meshSelectedModification = EditorModification.Mesh;
            }
            MDE_he();

            #endregion

            #region Category_Vertices

            if (meshSelectedModification == EditorModification.Vertices)
            {
                ColorUtility.TryParseHtmlString("#f2d3d3", out Color c);
                GUI.color = c;
                MDE_l("| Vertices Modification", true);
                MDE_v();
				MDE_h();
                MDE_DrawProperty("vertModifPointSizeMultiplier", "Point Size Multiplier", "Adjust generated points size. Press 'Vertices' button above to refresh. Keep the value '1' for default size (without any effect)");
                MDE_he();
                MDE_h();
                MDE_DrawProperty("meshAnimationMode", "Animation Mode", "If enabled, the system will not refresh the generated points");
                MDE_he();
                MDE_v();
                MDE_DrawProperty("vertModifUseCustomPattern", "Use Custom Point Pattern", "If enabled, you will be able to assign a custom point/vertex object pattern reference");
                if (m.HasCustomPointPatternReference)
                {
                    MDE_plus();
                    MDE_DrawProperty("vertModifCustomPatternReference", "Point Object Pattern");
                    MDE_hb("Refresh vertice editor to display a new point pattern by clicking the 'Vertices Modification' button");
                    MDE_minus();
                }
                MDE_DrawProperty("vertModifUseCustomPatternColor", "Use Custom Color");
                if (m.HasCustomPointPatternColor)
                {
                    MDE_plus();
                    MDE_DrawProperty("vertModifCustomPatternColor", "Custom Point Color");
                    MDE_hb("Refresh vertice editor to display a new point color by clicking the 'Vertices Modification' button");
                    MDE_minus();
                }
                MDE_ve();
                MDE_s(5);

                if (MDE_b("Open Vertex Tool Window"))
                    MD_VertexTool.Init();

                ColorUtility.TryParseHtmlString("#f2d3d3", out c);
                GUI.color = c;

                MDE_s(5);

                if (m.meshAnimationMode)
                {
                    MDE_v();
                    MDE_l("Animation Mode | Vertices Manager");
                    MDE_h();
                    if (MDE_b("Show Vertices"))
                        m.MPE_ShowHideGeneratedPoints(true);
                    if (MDE_b("Hide Vertices"))
                        m.MPE_ShowHideGeneratedPoints(false);
                    MDE_he();
                    MDE_s(5);
                    MDE_h();
                    if (MDE_b("Ignore Raycast"))
                        m.MPE_IgnoreRaycastForGeneratedPoints(true);
                    if (MDE_b("Default Layer"))
                        m.MPE_IgnoreRaycastForGeneratedPoints(false);
                    MDE_he();
                    MDE_ve();
                }
                MDE_ve();
            }

            #endregion

            #region Category_Collider

            if (meshSelectedModification == EditorModification.Collider)
            {
                ColorUtility.TryParseHtmlString("#7beb99", out Color c);
                GUI.color = c;
                MDE_l("| Collider Modification", true);
                if (!m.GetComponent<MD_MeshColliderRefresher>())
                {
                    MDE_v();

                    if (MDE_b("Add Mesh Collider Refresher"))
                        Undo.AddComponent<MD_MeshColliderRefresher>(m.gameObject);

                    MDE_ve();
                }
                else
                    MDE_hb("The selected object already contains Mesh Collider Refresher component", MessageType.Info);
                if(m.TryGetComponent(out MeshCollider mc) && MDE_b("Refresh Mesh Collider"))
                    mc.sharedMesh = m.MeshCachedMeshFilter.sharedMesh;
            }

            #endregion

            #region Category_Identity

            if (meshSelectedModification == EditorModification.Identity)
            {
                ColorUtility.TryParseHtmlString("#baefff", out Color c);
                GUI.color = c;
                MDE_l("| Identity Modification", true);

                MDE_v();

                if (MDE_b(new GUIContent("Create New Mesh Reference", "Create a brand new object with new mesh reference. This will create a new mesh reference and all your components & behaviours on this gameObject will be removed")))
                {
                    if (!EditorUtility.DisplayDialog("Are you sure?", "Are you sure to create a new mesh reference? This will create a brand new object with new mesh reference and all your components and behaviours on this gameObject will be lost.", "Yes", "No"))
                        return;
                    m.MPE_CreateNewReference();
                    return;
                }
                if (m.transform.childCount > 0 && m.transform.GetChild(0).GetComponent<MeshFilter>())
                {
                    if (MDE_b(new GUIContent("Combine All SubMeshes", "Combine all the meshes attached to the current object")))
                    {
                        m.MPE_CombineMeshAndCreateNewInstance();
                        return;
                    }
                }
                if (MDE_b("Save Mesh To Assets"))
                    MD_Utilities.Specific.SaveMeshToAssets_EditorOnly(m.MeshCachedMeshFilter);

                MDE_s(5);
                if (!m.meshUpdateEveryFrame)
                {
                    if (MDE_b("Update Mesh"))
                        m.MPE_UpdateMesh();
                }
                MDE_s(5);

                MDE_DrawProperty("meshCreateNewReferenceAfterCopy", "Create New Reference After Copy-Paste", "If enabled, the new mesh reference will be created with brand new mesh data on copy-paste action");
                MDE_DrawProperty("meshUpdateEveryFrame", "Update Every Frame", "If enabled, the mesh will be updated every frame and you will be able to deform the mesh at runtime");
                MDE_DrawProperty("meshSmoothAngleNormalsRecalc", "Normals Smooth Angle", "If disabled, the mesh normals will be recalculated through the default Unity's Recalculate Normals method. Otherwise you can customize the normals smoothing angle");
                if(m.meshSmoothAngleNormalsRecalc)
                {
                    MDE_plus();
                    MDE_DrawProperty("meshNormalsSmoothAngle", "Smoothing Angle");
                    MDE_minus();
                }
                MDE_ve();
            }

            #endregion

            #region Category_Mesh

            if (meshSelectedModification == EditorModification.Mesh)
            {
                ColorUtility.TryParseHtmlString("#dee7ff", out Color c);
                GUI.color = c;
                MDE_l("| Mesh Modification", true);

                MDE_v();

                MDE_l("Internal Mesh Features");
                MDE_plus();
                MDE_v();
                Foldout[0] = EditorGUILayout.Foldout(Foldout[0], new GUIContent("Mesh Smooth", SmoothIcon, "Smooth mesh by the smooth level"), true, EditorStyles.foldout);
                if (Foldout[0])
                {
                    MDE_plus();
                    SmoothMeshIntens = EditorGUILayout.Slider("Smooth Level", SmoothMeshIntens, 0.5f, 0.05f);
                    MDE_h(false);
                    MDE_s(EditorGUI.indentLevel * 10);
                    if (MDE_b(new GUIContent("Smooth Mesh", SmoothIcon)))
                        m.MPE_SmoothMesh(SmoothMeshIntens);
                    MDE_he();
                    MDE_hb("Undo won't record this process");
                    MDE_minus();
                }
                MDE_ve();
                MDE_v();
                Foldout[1] = EditorGUILayout.Foldout(Foldout[1], new GUIContent("Mesh Subdivide", SubdivisionIcon, "Subdivide mesh by the subdivision level"), true, EditorStyles.foldout);
                if (Foldout[1])
                {
                    MDE_plus();
                    divisionlevelSelection = EditorGUILayout.IntSlider("Subdivision Level", divisionlevelSelection, 2, DivisionLevels[DivisionLevels.Length - 1]);
                    MDE_h(false);
                    MDE_s(EditorGUI.indentLevel * 10);
                    if (MDE_b(new GUIContent("Subdivide Mesh", SubdivisionIcon)))
                        m.MPE_SubdivideMesh(divisionlevelSelection);
                    MDE_he();
                    MDE_hb("Undo won't record this process");
                    MDE_minus();
                }
                MDE_ve();
                MDE_minus();
                serializedObject.Update();
                MDE_s();

                MDE_l("Mesh Modifiers");
                MDE_plus();
                MDE_v();
                Foldout[2] = EditorGUILayout.Foldout(Foldout[2], "Modifiers", true, EditorStyles.foldout);
                if (Foldout[2])
                {
                    MDE_plus();

                    ColorUtility.TryParseHtmlString("#e3badb", out c);
                    GUI.color = c;
                    MDE_l("Logical Deformers");
                    if (MDE_b(new GUIContent("Mesh Morpher")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_Morpher>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh Effector")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeshEffector>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh FFD")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_FFD>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh Cut")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeshCut>(gm);
                        return;
                    }

                    ColorUtility.TryParseHtmlString("#dedba0", out c);
                    GUI.color = c;
                    MDE_l("World Interactive");
                    if (MDE_b(new GUIContent("Interactive Surface [CPU]")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_InteractiveSurface>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Surface Tracking [GPU]")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_SurfaceTracking>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh Damage")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeshDamage>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh Fit")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeshFit>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Melt Controller")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeltController>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh Slime")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeshSlime>(gm);
                        return;
                    }

                    ColorUtility.TryParseHtmlString("#aae0b2", out c);
                    GUI.color = c;
                    MDE_l("Basics");
                    if (MDE_b(new GUIContent("Twist")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_Twist>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Angular Bend")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_AngularBend>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Bend")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_Bend>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh Noise")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeshNoise>(gm);
                        return;
                    }

                    ColorUtility.TryParseHtmlString("#aad2e0", out c);
                    GUI.color = c;
                    MDE_l("Sculpting");
                    if (MDE_b(new GUIContent("Sculpting Lite")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_SculptingLite>(gm);
                        return;
                    }

                    ColorUtility.TryParseHtmlString("#ebebeb", out c);
                    GUI.color = c;
                    MDE_l("Additional Events");
                    if (MDE_b(new GUIContent("Raycast Event")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_RaycastEvent>(gm);
                        return;
                    }

                    ColorUtility.TryParseHtmlString("#dee7ff", out c);
                    GUI.color = c;
                    MDE_minus();
                }
                MDE_ve();
                MDE_minus();
                MDE_ve();
            }

            #endregion

            #region Bottom Category

            MDE_s(20);
            GUI.color = Color.white;
            MDE_l("Mesh Information");
            MDE_v();

            MDE_h(false);
            MDE_DrawProperty("meshInfoMeshName", "Mesh Name", "Change mesh name and Refresh Identity.");
            MDE_he();

            MDE_h(false);
            GUI.enabled = false;
            MDE_l("Vertices:");
            GUILayout.TextField(m.MeshCachedMeshFilter.sharedMesh.vertexCount.ToString());
            MDE_l("Triangles:");
            GUILayout.TextField((m.MeshCachedMeshFilter.sharedMesh.triangles.Length / 3).ToString());
            MDE_l("Normals:");
            GUILayout.TextField(m.MeshCachedMeshFilter.sharedMesh.normals.Length.ToString());
            MDE_l("UVs:");
            GUILayout.TextField(m.MeshCachedMeshFilter.sharedMesh.uv.Length.ToString());
            GUI.enabled = true;
            MDE_he();

            if (MDE_b("Restore Initial Mesh"))
                m.MPE_RestoreMeshToOriginal();
            MDE_ve();

            #endregion

            MDE_s();

            serializedObject.Update();
        }
    }
}