using UnityEngine;
using UnityEngine.InputSystem; // Ensure you have the Input System package installed
using System;

using System.Collections.Generic;



#if UNITY_EDITOR
using UnityEditor;
#endif



public class LinearProgressBar : StandardDynamicControl, IShowProgress
{
    public Material activeMaterial;
    public Material inactiveMaterial;
    public GameObject __slice; // Prefab for each progress slice
    private Dictionary<int, GameObject> nodes = new Dictionary<int, GameObject>();
    private Dictionary<int, Vector3> locations = new Dictionary<int, Vector3>();
    public int __dataProgress = 0;
    public int __sliceProgress = 0;
    public int __slicesMax = 20;
    public int __dataProgressMax = 100;

    private bool isActive = true; // Tracks the active state
    
        #if UNITY_EDITOR
    [ContextMenu("DemoBuild")]
    private void DemoBuild()
    {
        InitializeZDirectionProgressBar();
        BuildProgressBar();
    }
    #endif  
    /*
    public int directionX = 0;
    public int directionY = 1;
    public int directionZ = 0;
    public int numSlices = 5;
    public float marginX = 0f;
    public float marginY = 0f;
    public float marginZ = 0f;    
  
    private void InitializeZDirectionProgressBar()
    {
         int directionX = 0;
         int directionY = 1;
         int directionZ = 0;
        Vector3 size = __slice.transform.localScale;
        Vector3 localBasePosition = transform.InverseTransformPoint(__slice.transform.position); // Convert to        
        locations.Clear(); // Ensure the locations dictionary is reset
        float xStep = ((float)directionX) * size.x + marginX;
        float yStep = ((float)directionY) * size.y + marginY;
        float zStep = ((float)directionZ) * size.z + marginZ;
        for (int i = 0; i < numSlices; i++)
        {
            locations[i] = localBasePosition + new Vector3(i*xStep, i*yStep, i * zStep); // Position slices along the z-axis
        }
    }
    */

    public int directionX = 0;
    public int directionY = 1;
    public int directionZ = 0;
    public int numSlices = 5;
    public float marginX = 0f;
    public float marginY = 0f;
    public float marginZ = 0f;    
    public enum ArcType { None, Circular, Parabolic }
    public ArcType arcType = ArcType.None;
    public float radius = 1f; // For circular arcs
    public float parabolicA = 0.1f; // For parabolic arcs (a*x^2 + b*x + c)
    public float parabolicB = 0f;
    public float parabolicC = 0f;
    public float arcDirectionX = 1f; // Secondary direction for arc
    public float arcDirectionY = 1f; // Secondary direction for arc
    public float arcDirectionZ = 0; // Secondary direction for arc    

    private void InitializeZDirectionProgressBar()
    {
        Vector3 size = __slice.transform.localScale;
        Vector3 localBasePosition = transform.InverseTransformPoint(__slice.transform.position);
        locations.Clear(); // Ensure the locations dictionary is reset

        float xStep = ((float)directionX) * size.x + marginX;
        float yStep = ((float)directionY) * size.y + marginY;
        float zStep = ((float)directionZ) * size.z + marginZ;

        for (int i = 0; i < numSlices; i++)
        {
            // Base linear position
            Vector3 position = localBasePosition + new Vector3(i * xStep, i * yStep, i * zStep);

            // Apply ArcType adjustments if enabled
            if (arcType != ArcType.None)
            {
                float normalizedStep = (float)i / (numSlices - 1); // Normalized progress [0,1]
                Vector3 arcOffset = Vector3.zero;

                if (arcType == ArcType.Circular)
                {
            
                    //////
                    float angle = Mathf.Lerp(-Mathf.PI / 2, Mathf.PI / 2, normalizedStep); // Semi-circle
                    arcOffset = CalculateArcOffsetCircular(angle);
                }
                else if (arcType == ArcType.Parabolic)
                {
                    float xNormalized = normalizedStep - 0.5f; // Map normalized step to [-0.5, 0.5]
                    float parabolicOffset = parabolicA * xNormalized * xNormalized + parabolicB * xNormalized + parabolicC;
                    arcOffset = CalculateArcOffsetParabolic(parabolicOffset);
                }

                position += arcOffset;
            }

            locations[i] = position;
        }
    }

    private Vector3 CalculateArcOffsetCircular(float angle)
    {
        // Determine primary and secondary directions based on the arc plane
        if (arcDirectionX != 0 && arcDirectionY != 0)
        {
            return new Vector3(
                radius * Mathf.Cos(angle)*arcDirectionX, // X-axis (primary)
                radius * Mathf.Sin(angle)*arcDirectionY, // Y-axis (secondary)
                0                          // Z-axis (constrained)
            );
        }
        else if (arcDirectionX != 0 && arcDirectionZ != 0)
        {
            return new Vector3(
                radius * Mathf.Cos(angle)*arcDirectionX, // X-axis (primary)
                0,                         // Y-axis (constrained)
                radius * Mathf.Sin(angle)*arcDirectionZ  // Z-axis (secondary)
            );
        }
        else if (arcDirectionY != 0 && arcDirectionZ != 0)
        {
            return new Vector3(
                0,                         // X-axis (constrained)
                radius * Mathf.Cos(angle)*arcDirectionY, // Y-axis (primary)
                radius * Mathf.Sin(angle)*arcDirectionZ  // Z-axis (secondary)
            );
        }

        // Default fallback if no valid plane is defined
        return Vector3.zero;
    }


    private Vector3 CalculateArcOffsetParabolic(float parabolicOffset)
    {
        // Compute the offsets in the specified plane
        return new Vector3(
            arcDirectionX != 0 ? parabolicOffset : 0,
            arcDirectionY != 0 ? parabolicOffset : 0,
            arcDirectionZ != 0 ? parabolicOffset : 0
        );
    }



    protected override void Start()
    {
        base.Start();
        InitializeZDirectionProgressBar();
        BuildProgressBar();
        SetProgress(10);
    }

    private void BuildProgressBar()
    {
        List<Transform> childrenToDestroy = new List<Transform>();

        // Collect children to destroy
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Clone"))
            {
                childrenToDestroy.Add(child);
            }
        }

        // Destroy collected children
        foreach (Transform child in childrenToDestroy)
        {
            #if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
            #else
                Destroy(child.gameObject);
            #endif
        }
        nodes.Clear();

        foreach (var locationEntry in locations)
        {
            int index = locationEntry.Key;
            Vector3 position = locationEntry.Value;

            if (!nodes.ContainsKey(index))
            {
                GameObject slice = Instantiate(__slice, transform);
                slice.transform.localPosition = position;
                slice.SetActive(true);
                nodes[index] = slice;
            }
        }

        // Initialize all nodes to inactive
        UpdateSlices();
    }

    public bool SetProgress(int progress,string id = "")
    {
        if (progress < 0) 
            progress = 0;
        if (progress > __dataProgressMax) 
            progress = __dataProgressMax;
        __dataProgress = progress;
                
        __sliceProgress = (int)(((float)__dataProgress/(float)__dataProgressMax)*(float)__slicesMax);
        
        UpdateSlices();
        return true;
    }
    public int GetProgress(string id = "")
    {
        return __dataProgress;
    }    
    public int GetProgressMax(string id = "")
    {
        return __dataProgressMax;
    }  
    public void SetProgressMax(int max,string id = "")
    {
         __dataProgressMax=max;
    }      

    public override void SetState(string state)
    {
        if (state == "active")
        {
            SetActive(true);
        }
        else if (state == "inactive")
        {
            SetActive(false);
        }
        else
        {
            Debug.LogWarning($"Unhandled state: {state}");
        }
    }

    private void SetActive(bool status)
    {
        isActive = status;
        UpdateSlices();
    }

    private void UpdateSlices()
    {
        foreach (var nodeEntry in nodes)
        {
            int index = nodeEntry.Key;
            GameObject slice = nodeEntry.Value;

            Renderer sliceRenderer = slice.GetComponent<Renderer>();
            if (sliceRenderer != null)
            {
                if (isActive)
                {
                    sliceRenderer.material = index < __sliceProgress ? activeMaterial : inactiveMaterial;
                }
                else
                {
                    sliceRenderer.material = inactiveMaterial;
                }
            }
        }
    }

    public override string GetCommandId()
    {
        return "ProgressBar"; // Replace with unique identifier logic if needed
    }
}


