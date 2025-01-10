using System.Collections.Generic;
using UnityEngine;
using FXV;

[ExecuteInEditMode]
public class VolumeFogManager : MonoBehaviour
{
    [SerializeField] public Color fogColor = Color.white;
    [SerializeField] public bool affectedByLights = false;
    [SerializeField, Range(0.1f, 2.0f)] public float lightScatteringFactor = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] public float lightReflectivity = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] public float lightTransmission = 0.5f;

    [SerializeField] private List<VolumeFog> fogChildren = new List<VolumeFog>();

    private void Awake()
    {
        //RegisterFogChildren();
        ApplyPropertiesToChildren();
    }

    /*private void RegisterFogChildren()
    {
        fogChildren.Clear();
        foreach (Transform child in transform)
        {
            VolumeFog fog = child.GetComponent<VolumeFog>();
            if (fog != null)
            {
                fogChildren.Add(fog);
            }
        }
    }*/

    private void ApplyPropertiesToChildren()
    {
        foreach (VolumeFog fog in fogChildren)
        {
            //Debug
            if (fog != null)
            {
                fog.SetFogColor(fogColor);
                fog.SetAffectedByLights(affectedByLights);
                fog.lightScatteringFactor = lightScatteringFactor;
                fog.lightReflectivity = lightReflectivity;
                fog.lightTransmission = lightTransmission;
                
                // Explicitly update MaterialPropertyBlock to refresh the visuals
                Renderer renderer = fog.GetComponent<Renderer>();
                if (renderer != null)
                {
                    MaterialPropertyBlock props = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(props);
                    
                    props.SetColor("_Color", fogColor);
                    props.SetFloat("_LightScatteringFactor", lightScatteringFactor);
                    props.SetFloat("_LightReflectivity", lightReflectivity);
                    props.SetFloat("_LightTransmission", lightTransmission);
                    
                    renderer.SetPropertyBlock(props);
                }
            }
        }
    }


    public void SetFogColor(Color color)
    {
        fogColor = color;
        ApplyPropertiesToChildren();
    }

    public void SetAffectedByLights(bool affected)
    {
        affectedByLights = affected;
        ApplyPropertiesToChildren();
    }

    public void SetLightScatteringFactor(float factor)
    {
        lightScatteringFactor = Mathf.Clamp(factor, 0.1f, 2.0f);
        ApplyPropertiesToChildren();
    }

    public void SetLightReflectivity(float reflectivity)
    {
        lightReflectivity = Mathf.Clamp01(reflectivity);
        ApplyPropertiesToChildren();
    }

    public void SetLightTransmission(float transmission)
    {
        lightTransmission = Mathf.Clamp01(transmission);
        ApplyPropertiesToChildren();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyPropertiesToChildren();
    }
#endif
}

