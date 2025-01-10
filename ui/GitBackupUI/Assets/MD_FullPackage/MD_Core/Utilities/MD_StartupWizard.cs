#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;

using MDPackage;

namespace MDPackage_Editor
{
    public sealed class MD_StartupWizard : EditorWindow
    {
        public Texture2D Logo;

        public Texture2D Home;
        public Texture2D Web;
        public Texture2D Doc;
        public Texture2D Discord;

        private GUIStyle style;

        [MenuItem("Window/MD_Package/Startup Window", priority = 40)]
        public static void Init()
        {
            MD_StartupWizard md = (MD_StartupWizard)GetWindow(typeof(MD_StartupWizard));
            md.maxSize = new Vector2(400, 700);
            md.minSize = new Vector2(399, 699);
            md.titleContent = new GUIContent("MD Startup");
            md.Show();
        }

        private void OnGUI()
        {
            style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            style.normal.textColor = Color.white;
            style.wordWrap = false;

            GUILayout.Label(Logo);
            style.fontSize = 13;
            style.wordWrap = true;
            style.alignment = TextAnchor.MiddleCenter;
            GUILayout.BeginVertical("Box");
            GUILayout.Label("Thank you for using the MD Full Collection!\nHave a look at the latest change-log below...", style);
            GUILayout.EndVertical();

            style.alignment = TextAnchor.UpperLeft;
            GUILayout.Space(5);
            style.fontSize = 12;

            GUILayout.BeginVertical("Box");
            GUILayout.Label("<size=14><color=#ffa84a>MD Package version <b>" + MD_Debug.PACKAGE_VERSION + "</b> [" + MD_Debug.LAST_UPDATE_DATE + " <size=11>dd/mm/yyyy</size>]</color></size>\n" +
                "- New modifier added - Angular Bend\n" +
                "- Several fixes to multithreading in SculptingLite modifier\n" +
                "- Package Global Preferences made to scriptable object\n" +
                "- 'Zone Generator' removed from the MeshProEditor (was not necessary as the vertex limit is around 2k)\n" +
                "- 'Alternate Normals' feature renamed to 'Normal Smoothing Angle'\n" +
                "- New example scenes related to tunnel creation at runtime via API\n" +
                "- Major package refactor & public API update\n" +
                "- All MD-Package modifiers and procedural meshes can be now added via AddComponent keyword\n" +
                "- Major update to API documentation\n" +
                "- Major cleanup\n" +
                "- Ready for Unity 2021, 2022 and 2023", style);

            if (GUILayout.Button("Official Roadmap - Trello"))
                Application.OpenURL("https://trello.com/b/MFqllEZE/matej-vanco-unity-extension");
            GUILayout.EndVertical();
            GUILayout.Space(5);
            style.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("No idea where to start? Open general documentation for more!", style);
            GUILayout.Space(5);
            style.alignment = TextAnchor.UpperLeft;
            style = new GUIStyle(GUI.skin.button);
            style.imagePosition = ImagePosition.ImageAbove;

            GUILayout.BeginHorizontal("Box");
            if (GUILayout.Button(new GUIContent("Take Me To Intro", Home), style))
            {
                GenerateScenesToBuild();
                string scene = GetScenePath("MDExample_Introduction");
                if (!string.IsNullOrEmpty(scene))
                    EditorSceneManager.OpenScene(scene);
                else
                    Debug.LogError("Scene is not in the Build Settings! Required path: [" + Application.dataPath + "/MD_FullPackage/MD_Examples/MD_Examples_Scenes/]");
            }
            if (GUILayout.Button(new GUIContent("Official Website", Web), style))
                Application.OpenURL("https://www.matejvanco.com/md-package");

            if (GUILayout.Button(new GUIContent("General Documentation", Doc), style))
                Application.OpenURL("https://docs.google.com/presentation/d/13Utk_hVY304c7QoQPSVG7nHXV5W5RjXzgZIhsKFvUDE/edit");      

            GUILayout.EndHorizontal();

            if (GUILayout.Button(new GUIContent("APi Documentation"), style))
                Application.OpenURL("https://struct9.com/matejvanco-assets/md-package/Introduction");

            style.alignment = TextAnchor.MiddleCenter;
            if (GUILayout.Button(new GUIContent(Discord), style))
                Application.OpenURL("https://discord.gg/Ztr8ghQKqC");
        }

        public static void GenerateScenesToBuild()
        {
            try
            {
                EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];
                List<EditorBuildSettingsScene> sceneAr = new List<EditorBuildSettingsScene>();

                const string MAIN_PATH = "/MD_FullPackage/MD_Examples/MD_Examples_Scenes/";

                int cat = 0;
                while (cat < 6)
                {
                    string[] tempPaths;
                    if (cat == 0)       tempPaths = Directory.GetFiles(Application.dataPath + MAIN_PATH, "*.unity");
                    else if (cat == 1)  tempPaths = Directory.GetFiles(Application.dataPath + MAIN_PATH + "Geometry/", "*.unity");
                    else if (cat == 2)  tempPaths = Directory.GetFiles(Application.dataPath + MAIN_PATH + "MeshEditor/", "*.unity");
                    else if (cat == 3)  tempPaths = Directory.GetFiles(Application.dataPath + MAIN_PATH + "Modifiers/", "*.unity");
                    else if (cat == 4)  tempPaths = Directory.GetFiles(Application.dataPath + MAIN_PATH + "Mobile/", "*.unity");
                    else                tempPaths = Directory.GetFiles(Application.dataPath + MAIN_PATH + "Shaders/", "*.unity");

                    for (int i = 0; i < tempPaths.Length; i++)
                    {
                        sceneAr.Add(new EditorBuildSettingsScene(tempPaths[i].Substring(Application.dataPath.Length - "Assets".Length).Replace('\\', '/'),
                            true));
                    }
                    cat++;
                }
                EditorBuildSettings.scenes = sceneAr.ToArray();
            }
            catch (IOException e)
            {
                Debug.Log("Can't load example scenes! Try to play again. You can find all the example scenes in MD_Examples_Scenes.\nException: " + e.Message);
            }

        }

        private static string GetScenePath(string sceneName)
        {
            try
            {
                if (File.Exists(Application.dataPath + "/MD_FullPackage/MD_Examples/MD_Examples_Scenes/" + sceneName + ".unity"))
                    return Application.dataPath + "/MD_FullPackage/MD_Examples/MD_Examples_Scenes/" + sceneName + ".unity";
                else
                    return "";
            }
            catch (IOException e)
            {
                Debug.Log("Can't load example scenes! Go to /MD_FullPackage/MD_Examples/MD_Examples_Scenes/.\nException: " + e.Message);
            }
            return "";
        }
    }
}
#endif