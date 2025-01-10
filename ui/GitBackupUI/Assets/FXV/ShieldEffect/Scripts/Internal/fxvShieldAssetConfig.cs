using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FXV
{
    [DefaultExecutionOrder(1000)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    internal class fxvShieldAssetConfig
    {
        internal enum Pipeline
        {
            FXV_SHIELD_BUILTIN = 0,
            FXV_SHIELD_URP = 1,
            FXV_SHIELD_HDRP = 2
        }

        internal static string AssetPath = null;

        internal static readonly string FXV_PIPELINE_URP = "FXV_SHIELD_URP";
        internal static readonly string FXV_PIPELINE_HDRP = "FXV_SHIELD_HDRP";

        internal static Pipeline ActiveRenderPipeline = Pipeline.FXV_SHIELD_BUILTIN;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void UpdatePipelineConfig()
        {
#if UNITY_EDITOR
            var g = AssetDatabase.FindAssets($"t:Script {nameof(fxvShieldAssetConfig)}");
            string scriptPath = AssetDatabase.GUIDToAssetPath(g[0]);

            AssetPath = Path.GetDirectoryName(scriptPath);
            AssetPath = Path.GetDirectoryName(AssetPath);
            AssetPath = Path.GetDirectoryName(AssetPath);

#if FX_DEBUG_LOGS
            Debug.Log("UpdatePipelineConfig AssetPath " + AssetPath);
#endif
#endif

            OnActiveRenderPipelineChanged();

#if UNITY_2021_1_OR_NEWER
            RenderPipelineManager.activeRenderPipelineTypeChanged += OnActiveRenderPipelineChanged;
#endif
        }

        static fxvShieldAssetConfig()
        {
            UpdatePipelineConfig();
        }

        private static void OnActiveRenderPipelineChanged()
        {
            Shader.DisableKeyword(FXV_PIPELINE_URP);
            Shader.DisableKeyword(FXV_PIPELINE_HDRP);

            var currentRP = GraphicsSettings.currentRenderPipeline;
            if (currentRP == null)
            {
                ActiveRenderPipeline = Pipeline.FXV_SHIELD_BUILTIN;

                OnActiveRenderPipelineChanged(ActiveRenderPipeline);

                return;
            }

            var curPipeline = currentRP.GetType().ToString().ToLower();

            if (curPipeline.Contains("universal"))
            {
                ActiveRenderPipeline = Pipeline.FXV_SHIELD_URP;
                Shader.EnableKeyword(FXV_PIPELINE_URP);
            }
            else if (curPipeline.Contains("high definition") || curPipeline.Contains("highdefinition"))
            {
                ActiveRenderPipeline = Pipeline.FXV_SHIELD_HDRP;
                Shader.EnableKeyword(FXV_PIPELINE_HDRP);
            }

            OnActiveRenderPipelineChanged(ActiveRenderPipeline);
        }

        private static void OnActiveRenderPipelineChanged(Pipeline newPipeline)
        {
#if UNITY_EDITOR

#if FX_DEBUG_LOGS
            Debug.Log("OnActiveRenderPipelineChanged newPipeline " + newPipeline);
#endif

            ShieldPostprocess.OnPipelineChanged();

            string[] shaderGuids = AssetDatabase.FindAssets("t:shader", new[] { AssetPath });
            foreach (string guid in shaderGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);


                var lines = File.ReadAllLines(assetPath);
                bool changed = false;
                for (int i = 0; i < lines.Length; ++i)
                {
                    if (newPipeline == Pipeline.FXV_SHIELD_BUILTIN)
                    {
                        if (lines[i].Contains("#define FXV_SHIELD_URP"))
                        {
                            lines[i] = lines[i].Replace("#define FXV_SHIELD_URP", "#define FXV_SHIELD_BUILTIN");
                            changed = true;
                        }
                        else if (lines[i].Contains("#define FXV_SHIELD_HDRP"))
                        {
                            lines[i] = lines[i].Replace("#define FXV_SHIELD_HDRP", "#define FXV_SHIELD_BUILTIN");
                            changed = true;
                        }
                    }
                    else if (newPipeline == Pipeline.FXV_SHIELD_URP)
                    {
                        if (lines[i].Contains("#define FXV_SHIELD_BUILTIN"))
                        {
                            lines[i] = lines[i].Replace("#define FXV_SHIELD_BUILTIN", "#define FXV_SHIELD_URP");
                            changed = true;
                        }
                        else if (lines[i].Contains("#define FXV_SHIELD_HDRP"))
                        {
                            lines[i] = lines[i].Replace("#define FXV_SHIELD_HDRP", "#define FXV_SHIELD_URP");
                            changed = true;
                        }
                    }
                    else if (newPipeline == Pipeline.FXV_SHIELD_HDRP)
                    {
                        if (lines[i].Contains("#define FXV_SHIELD_BUILTIN"))
                        {
                            lines[i] = lines[i].Replace("#define FXV_SHIELD_BUILTIN", "#define FXV_SHIELD_HDRP");
                            changed = true;
                        }
                        else if (lines[i].Contains("#define FXV_SHIELD_URP"))
                        {
                            lines[i] = lines[i].Replace("#define FXV_SHIELD_URP", "#define FXV_SHIELD_HDRP");
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
                if (newPipeline == Pipeline.FXV_SHIELD_BUILTIN)
                {
                    if (mat.shader.name == "Universal Render Pipeline/Lit" || mat.shader.name == "HDRP/Lit")
                    {
                        mat.shader = Shader.Find("Standard");

                        EditorUtility.SetDirty(mat);
                        AssetDatabase.SaveAssetIfDirty(mat);
                    }
                }
                else if (newPipeline == Pipeline.FXV_SHIELD_URP)
                {
                    if (mat.shader.name == "Standard" || mat.shader.name == "HDRP/Lit")
                    {
                        Color mainColor = mat.GetColor("_Color");
                        Texture mainTex = GetMainTexture(mat);
                        Texture metallicGlossTex = GetMetallicGlossTexture(mat);
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
                else if (newPipeline == Pipeline.FXV_SHIELD_HDRP)
                {
                    if (mat.shader.name == "Standard" || mat.shader.name == "Universal Render Pipeline/Lit")
                    {
                        Color mainColor = mat.GetColor("_Color");
                        Texture mainTex = GetMainTexture(mat);
                        Texture metallicGlossTex = GetMetallicGlossTexture(mat);
                        float glossScale = mat.GetFloat("_GlossMapScale");
                        float gloss = mat.GetFloat("_Glossiness");

                        mat.shader = Shader.Find("HDRP/Lit");

                        mat.SetColor("_BaseColor", mainColor);
                        mat.SetTexture("_BaseColorMap", mainTex);
                        mat.SetTexture("_MetallicGlossMap", metallicGlossTex);
                        mat.SetFloat("_Smoothness", gloss);
                        mat.SetFloat("_Glossiness", gloss);

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

            return retTex;
        }

        public static Texture GeNormalMapTexture(Material mat)
        {
            Texture retTex = null;

            if (mat.HasTexture("_BumpMap"))
            {
                retTex = mat.GetTexture("_BumpMap");
            }

#if FX_DEBUG_LOGS
             Debug.Log(" normalmap " + mainTex);
#endif

            return retTex;
        }
#endif
    }
}
