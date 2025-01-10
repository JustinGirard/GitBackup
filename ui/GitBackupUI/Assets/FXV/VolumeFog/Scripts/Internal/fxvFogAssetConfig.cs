using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FXV.Internal
{
    [DefaultExecutionOrder(1000)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public class fxvFogAssetConfig
    {
        internal enum Pipeline
        {
            BuiltIn = 0,
            URP = 1,
            HDRP = 2
        }

        internal static string AssetPath = null;

        internal static Pipeline ActiveRenderPipeline = Pipeline.BuiltIn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void UpdatePipelineConfig()
        {
#if UNITY_EDITOR
            var g = AssetDatabase.FindAssets($"t:Script {nameof(fxvFogAssetConfig)}");
            string scriptPath = AssetDatabase.GUIDToAssetPath(g[0]);

            AssetPath = Path.GetDirectoryName(scriptPath);
            AssetPath = Path.GetDirectoryName(AssetPath);
            AssetPath = Path.GetDirectoryName(AssetPath);
#endif

#if FX_DEBUG_LOGS
            Debug.Log("UpdatePipelineConfig AssetPath " + AssetPath);
#endif

            OnActiveRenderPipelineChanged();

#if UNITY_2021_1_OR_NEWER
            RenderPipelineManager.activeRenderPipelineTypeChanged += OnActiveRenderPipelineChanged;
#endif
        }

        static fxvFogAssetConfig()
        {
            UpdatePipelineConfig();
        }

        private static void OnActiveRenderPipelineChanged()
        {
            var currentRP = GraphicsSettings.currentRenderPipeline;
            if (currentRP == null)
            {
                ActiveRenderPipeline = Pipeline.BuiltIn;

                OnActiveRenderPipelineChanged(ActiveRenderPipeline);

                return;
            }

            var curPipeline = currentRP.GetType().ToString().ToLower();

            if (curPipeline.Contains("universal"))
            {
                ActiveRenderPipeline = Pipeline.URP;
            }
            else if (curPipeline.Contains("high definition") || curPipeline.Contains("highdefinition"))
            {
                ActiveRenderPipeline = Pipeline.HDRP;
            }

            OnActiveRenderPipelineChanged(ActiveRenderPipeline);
        }


        public static void UpdateShadersForActiveRenderPipeline()
        {
            UpdateShaders(ActiveRenderPipeline);
        }

        private static void UpdateShaders(Pipeline newPipeline)
        {
#if UNITY_EDITOR
            string[] shaderGuids = AssetDatabase.FindAssets("t:shader", new[] { AssetPath });
            foreach (string guid in shaderGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                var lines = File.ReadAllLines(assetPath);
                bool changed = false;
                for (int i = 0; i < lines.Length; ++i)
                {
                    if (newPipeline == Pipeline.BuiltIn)
                    {
                        if (lines[i].Contains("#define FXV_VOLUMEFOG_URP"))
                        {
                            lines[i] = lines[i].Replace("#define FXV_VOLUMEFOG_URP", "#define FXV_VOLUMEFOG_BUILTIN");

                            changed = true;
                        }
                    }
                    else if (newPipeline == Pipeline.URP)
                    {
                        if (lines[i].Contains("#define FXV_VOLUMEFOG_BUILTIN"))
                        {
                            lines[i] = lines[i].Replace("#define FXV_VOLUMEFOG_BUILTIN", "#define FXV_VOLUMEFOG_URP");

                            changed = true;
                        }

                    }
                }

                if (changed)
                {
#if FX_DEBUG_LOGS
                    Debug.Log("changed shader to new RP " + assetPath);
#endif

                    File.WriteAllLines(assetPath, lines);
                    Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
                    if (shader)
                    {
                        EditorUtility.SetDirty(shader);
                        AssetDatabase.SaveAssetIfDirty(shader);
                        AssetDatabase.ImportAsset(assetPath);
                    }
                }
            }
#endif
        }

        private static void OnActiveRenderPipelineChanged(Pipeline newPipeline)
        {
#if FX_DEBUG_LOGS
            Debug.Log("OnActiveRenderPipelineChanged newPipeline " + newPipeline);
#endif

#if UNITY_EDITOR

#if UNITY_2022_2_OR_NEWER
            VolumeFog[] fogObjects = GameObject.FindObjectsByType<VolumeFog>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            VolumeFog[] fogObjects = GameObject.FindObjectsOfType<VolumeFog>(true);
    #endif

            foreach (VolumeFog obj in fogObjects)
            {
                VolumeFog.SetupFogMaterial(obj);
            }

            UpdateShaders(newPipeline);

            string[] matGuids = AssetDatabase.FindAssets("t:material", new[] { AssetPath });
            foreach (string guid in matGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

                if (mat == null || mat.shader == null)
                {
#if FX_DEBUG_LOGS
                    Debug.Log("material or shader is null " + assetPath);
#endif
                    
                    continue;
                }

#if FX_DEBUG_LOGS
                Debug.Log("processing material " + assetPath);
#endif
                if (newPipeline == Pipeline.BuiltIn)
                {
                    if (mat.shader.name == "Universal Render Pipeline/Lit" || mat.shader.name == "HDRP/Lit")
                    {
                        mat.shader = Shader.Find("Standard");

                        EditorUtility.SetDirty(mat);
                        AssetDatabase.SaveAssetIfDirty(mat);
                    }
                }
                else if (newPipeline == Pipeline.URP)
                {
                    if (mat.shader.name == "Standard" || mat.shader.name == "HDRP/Lit")
                    {
                        Color mainColor = mat.GetColor("_Color");
                        Texture mainTex = GetMainTexture(mat);
                        Texture metallicGlossTex = GetMetallicGlossTexture(mat);//.GetTexture("_MetallicGlossMap");
                        float glossScale = mat.GetFloat("_GlossMapScale");
                        float gloss = mat.GetFloat("_Glossiness");

                        mat.shader = Shader.Find("Universal Render Pipeline/Lit");

                        mat.SetColor("_BaseColor", mainColor);
                        mat.SetTexture("_BaseMap", mainTex);
                        mat.SetTexture("_MetallicGlossMap", metallicGlossTex);
                        mat.SetFloat("_Smoothness", glossScale);
                        mat.SetFloat("_Glossiness", gloss);

                        EditorUtility.SetDirty(mat);
                        AssetDatabase.SaveAssetIfDirty(mat);
                    }
                }
                else if (newPipeline == Pipeline.HDRP)
                {
                    if (mat.shader.name == "Standard" || mat.shader.name == "Universal Render Pipeline/Lit")
                    {
                        Texture mainTex = mat.mainTexture;
#if FX_DEBUG_LOGS
                        Debug.Log(" mainTex " + mainTex);
#endif
                        if (mainTex == null)
                        {
                            if (mat.HasTexture("_BaseMap"))
                            {
                                mainTex = mat.GetTexture("_BaseMap");
                            }
                            if (mainTex == null)
                            {
                                if (mat.HasTexture("_MainTex"))
                                {
                                    mainTex = mat.GetTexture("_MainTex");
                                }
                            }
                        }

                        mat.shader = Shader.Find("HDRP/Lit");

                        EditorUtility.SetDirty(mat);
                        AssetDatabase.SaveAssetIfDirty(mat);

                        mat.SetTexture("_BaseColorMap", mainTex);

                        EditorUtility.SetDirty(mat);
                        AssetDatabase.SaveAssetIfDirty(mat);
                    }
                }
            }
#endif
        }

#if UNITY_EDITOR
        public static Texture GetMainTexture(Material mat)
        {
            Texture mainTex = mat.mainTexture;

#if FX_DEBUG_LOGS
             Debug.Log(" mainTex " + mainTex);
#endif
            if (mainTex == null)
            {
                if (mat.HasTexture("_BaseMap"))
                {
                    mainTex = mat.GetTexture("_BaseMap");
                }
                if (mainTex == null)
                {
                    if (mat.HasTexture("_MainTex"))
                    {
                        mainTex = mat.GetTexture("_MainTex");
                    }
                }
            }

            return mainTex;
        }

        public static Texture GetMetallicGlossTexture(Material mat)
        {
            Texture retTex = null;

            if (mat.HasTexture("_MetallicGlossMap"))
            {
                retTex = mat.GetTexture("_MetallicGlossMap");
            }

#if FX_DEBUG_LOGS
             Debug.Log(" metallicTex " + mainTex);
#endif
           /* if (retTex == null)
            {
                if (mat.HasTexture("_BaseMap"))
                {
                    retTex = mat.GetTexture("_BaseMap");
                }
                if (retTex == null)
                {
                    if (mat.HasTexture("_MainTex"))
                    {
                        retTex = mat.GetTexture("_MainTex");
                    }
                }
            }*/

            return retTex;
        }
#endif
    }
}
