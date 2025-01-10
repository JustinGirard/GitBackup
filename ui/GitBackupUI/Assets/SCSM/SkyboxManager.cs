using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class SkyboxManager : MonoBehaviour
{
    [Header("Skybox Configuration")]
    public List<SkyboxEntry> skyboxes = new List<SkyboxEntry>();
    public string selectedSkybox;

    private Material currentSkybox;

    [System.Serializable]
    public struct SkyboxEntry
    {
        public string key;
        public Material skyboxMaterial;
    }

    void OnValidate()
    {
        ApplySkybox();
    }

    void Awake()
    {
        ApplySkybox();
    }

    void OnEnable()
    {
        ApplySkybox();
        SubscribeToEditorUpdate();
    }

    void OnDisable()
    {
        UnsubscribeFromEditorUpdate();
    }

    void ApplySkybox()
    {
        Material skybox = GetSkyboxByKey(selectedSkybox);
        
        if (skybox != null && skybox != currentSkybox)
        {
            RenderSettings.skybox = skybox;
            currentSkybox = skybox;
            DynamicGI.UpdateEnvironment();  // Update lighting for skybox changes
        }
    }

    Material GetSkyboxByKey(string key)
    {
        foreach (var entry in skyboxes)
        {
            if (entry.key == key)
            {
                return entry.skyboxMaterial;
            }
        }
        return null;
    }

#if UNITY_EDITOR
    void SubscribeToEditorUpdate()
    {
        EditorApplication.update += ApplySkybox;
    }

    void UnsubscribeFromEditorUpdate()
    {
        EditorApplication.update -= ApplySkybox;
    }
#endif
}
