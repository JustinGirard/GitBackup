using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MDPackage
{
    //[CreateAssetMenu(fileName = PREF_NAME, menuName = "ScriptableObjects/MD_Package/" + PREF_NAME, order = 1)]
    /// <summary>
    /// Main preferences scriptable object that holds the MDPackage-related global settings.
    /// Written by Matej Vanco, 2014
    /// </summary>
    public sealed class MD_Preferences : ScriptableObject
    {
        private const string PREF_NAME = "MD_Preferences_Global";

        // Serialized

        [Header("Hover over the fields for tooltips")]
        [SerializeField, Min(8), Tooltip("If the target mesh has more vertices that the allowed count, the component will not be used")]
        private int vertexLimit = 2000;
        [Space]
        [SerializeField, Tooltip("A new mesh reference will be created once the MD Package's component is applied to a gameObject")]
        private bool createNewMeshReferenceAsDefault = true;
        [Space]
        [SerializeField, Tooltip("Allow editor windows to popup if any notification has outputed")]
        private bool popupEditorWindowIfAnyNotification = true;
        [Space]
        [SerializeField, Tooltip("Auto-recalculate mesh normals if required. Otherwise you will have to manually recalculate mesh normals")]
        private bool autoRecalculateNormalsAsDefault = true;
        [SerializeField, Tooltip("Use advanced-mesh normals recalculation as default with custom smoothing angle")]
        private bool smoothAngleNormalsRecalculationAsDefault = false;
        [Space]
        [SerializeField, Tooltip("Auto-recalculate mesh bounds if required. Otherwise you will have to manually recalculate mesh bounds")]
        private bool autoRecalculateBoundsAsDefault = true;

        // Properties

        public int VertexLimit => vertexLimit;
        public bool CreateNewMeshReferenceAsDefault => createNewMeshReferenceAsDefault;
        public bool PopupEditorWindowIfAnyNotification => popupEditorWindowIfAnyNotification;
        public bool AutoRecalculateNormalsAsDefault => autoRecalculateNormalsAsDefault;
        public bool SmoothAngleNormalsRecalculationAsDefault => smoothAngleNormalsRecalculationAsDefault;
        public bool AutoRecalculateBoundsAsDefault => autoRecalculateBoundsAsDefault;

#if UNITY_EDITOR
        [MenuItem("Window/MD_Package/Preferences")]
        private static void SelectPreferencesEditor()
            => SelectPreferencesAsset(true);

        private async void OnRestoreObj()
        {
            EditorUtility.DisplayDialog("Warning", "You have deleted a preferences scriptable object, which is prohibited. There always have to be at least one preferences object. A new one will be created.", "OK");
            string path = AssetDatabase.GetAssetPath(this);

            await System.Threading.Tasks.Task.Delay(128);

            AssetDatabase.CreateAsset(CreateInstance<MD_Preferences>(), path);
            AssetDatabase.Refresh();
        }

        private sealed class OnDestroyProcessor : AssetModificationProcessor
        {
            static System.Type prefType = typeof(MD_Preferences);
            const string extension = ".asset";

            public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions _)
            {
                if (!path.EndsWith(extension))
                    return AssetDeleteResult.DidNotDelete;

                var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType != null && (assetType == prefType || assetType.IsSubclassOf(prefType)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<MD_Preferences>(path);
                    asset.OnRestoreObj();
                }

                return AssetDeleteResult.DidNotDelete;
            }
        }
#endif
        public static MD_Preferences SelectPreferencesAsset(bool selectAssetIfInEditor = false)
        {
            MD_Preferences pref = Resources.Load<MD_Preferences>(PREF_NAME);
            if(pref == null)
            {
                MD_Debug.Debug(null, $"Preferences scriptable object couldn't be found! " +
                    $"Please make sure there is an asset in the 'Resources' folder with name '{PREF_NAME}'", MD_Debug.DebugType.Error);
                return null;
            }
#if UNITY_EDITOR
            if(selectAssetIfInEditor)
                Selection.activeObject = pref;
#endif
            return pref;
        }

    }
}