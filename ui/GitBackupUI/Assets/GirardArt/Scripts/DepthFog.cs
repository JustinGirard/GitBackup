using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class DepthFog : MonoBehaviour
{
    [Header("Fog Settings")]
    public bool enableFog = true;
    public Color fogColor = Color.gray;
    [Range(0.0f, 1.0f)] public float fogDensity = 0.1f;
    
    [Tooltip("Linear - Gradual Fog\nExponential - Rapid Growth\nExponentialSquared - Stronger Fog")]
    public FogModeOption fogMode = FogModeOption.Linear;

    public float fogStart = 10f;
    public float fogEnd = 100f;

    void OnValidate()
    {
        ApplyFogSettings();
    }

    void OnEnable()
    {
        ApplyFogSettings();
        SubscribeToEditorUpdate();
    }

    void OnDisable()
    {
        UnsubscribeFromEditorUpdate();
    }

    void ApplyFogSettings()
    {
        RenderSettings.fog = enableFog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;

        // Map serialized enum to actual FogMode
        RenderSettings.fogMode = MapFogMode(fogMode);
    }

    FogMode MapFogMode(FogModeOption option)
    {
        switch (option)
        {
            case FogModeOption.Linear:
                return FogMode.Linear;
            case FogModeOption.Exponential:
                return FogMode.Exponential;
            case FogModeOption.ExponentialSquared:
                return FogMode.ExponentialSquared;
            default:
                return FogMode.Linear;
        }
    }

#if UNITY_EDITOR
    void SubscribeToEditorUpdate()
    {
        EditorApplication.update += ApplyFogSettings;
    }

    void UnsubscribeFromEditorUpdate()
    {
        EditorApplication.update -= ApplyFogSettings;
    }
#endif

    // Serializable enum to expose dropdown in the inspector
    public enum FogModeOption
    {
        Linear,
        Exponential,
        ExponentialSquared
    }
}
