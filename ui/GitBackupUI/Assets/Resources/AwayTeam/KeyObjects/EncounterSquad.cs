using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EncounterSquad : MonoBehaviour, IPausable
{
    [Header("Settings")]
    // public Vector3 masterPosition = Vector3.zero; // The central "master position"
    public Quaternion masterRotation = Quaternion.identity; // Optional if rotations are needed

    [Header("Resources")]
    public string spaceMapUnitResourcePath = "AwayTeam/KeyObjects/SpaceMapUnit"; // Path to SpaceMapUnit prefab in Resources
    public string veeFormationResourcePath = "AwayTeam/KeyObjects/VeeFormation"; // Path to VeeFormation prefab in Resources

    private List<GameObject> spaceMapUnits = new List<GameObject>(); // Tracks spawned SpaceMapUnit instances
    private GameObject veeFormation; // The dynamically loaded VeeFormation instance

    private bool __is_running = false;
    public void Run()
    {
        foreach(GameObject spaceMapUnit in spaceMapUnits)
        {
            SpaceMapUnitAgent su = spaceMapUnit.GetComponentInChildren<SpaceMapUnitAgent>(); 
            su.Run();
        }

        __is_running = true;
    }
    public void Pause()
    {
        foreach(GameObject spaceMapUnit in spaceMapUnits)
        {
            SpaceMapUnitAgent su = spaceMapUnit.GetComponentInChildren<SpaceMapUnitAgent>();
            su.Pause();
        }
        __is_running = false;
    }
    public List<SpaceMapUnitAgent> GetUnitList()
    {
    
        List<SpaceMapUnitAgent> suses = new List<SpaceMapUnitAgent>();
        foreach(GameObject spaceMapUnit in spaceMapUnits)
        {
            SpaceMapUnitAgent su = spaceMapUnit.GetComponentInChildren<SpaceMapUnitAgent>();
            suses.Add(su);
        }
        return suses;
    }
    public bool IsRunning()
    {
        return __is_running;
    }    

    void Start()
    {
        //Rebuild(); // Automatically rebuild on Start
    }

    /// <summary>
    /// Cleans up and respawns all SpaceMapUnits and the VeeFormation.
    /// </summary>
    public void Rebuild()
    {
        Cleanup();
        Respawn();
    }

    /// <summary>
    /// Cleans up all dynamically spawned units and formation objects.
    /// </summary>
    private void Cleanup()
    {
        // Destroy all existing SpaceMapUnits
        foreach (var unit in spaceMapUnits)
        {
            if (unit != null) Destroy(unit);
        }
        spaceMapUnits.Clear();

        // Destroy existing VeeFormation
        if (veeFormation != null)
        {
            Destroy(veeFormation);
            veeFormation = null;
        }
    }

    /// <summary>
    /// Respawns all SpaceMapUnits and the VeeFormation from Resources.
    /// </summary>
    /// 
    public void UpdatePosition()
    {
        VeeFormation formation =  veeFormation.GetComponent<VeeFormation>();
        
        for (int i = 0; i < spaceMapUnits.Count; i++) // Support up to 5 units in the formation
        {
            Vector3 goalPosition = formation.GetPosition(i) + transform.position;
            SpaceMapUnitAgent positionable = spaceMapUnits[i].GetComponentInChildren<SpaceMapUnitAgent>(); // Assuming SpaceMapUnit handles its position
            if (positionable != null && goalPosition != null)
            {
                positionable.SetGoalPosition(goalPosition,immediate:true);            
            }
            else
            {
                Debug.LogError("SpaceMapUnit does not have the expected positionable interface.");
            }
        }


    }
    public void NotifyDestroy(GameObject removed){
        spaceMapUnits.Remove(removed);
    }
    private void Respawn()
    {
        // Load and instantiate VeeFormation
        // Debug.Log($"Loading my position {this.name}");

        
        Transform units = transform.Find("Units");
        units.name = $"UnitsFor{name}";
        if (units == null)
        {
            Debug.LogError("Failed to load VeeFormation prefab from Resources.");
            return;            
        }
        Transform formations = transform.Find("Formations");
        if (formations == null)
        {
            Debug.LogError("Failed to load VeeFormation prefab from Resources.");
            return;            
        }


        veeFormation = Instantiate(Resources.Load<GameObject>(veeFormationResourcePath));
        if (veeFormation == null)
        {
            Debug.LogError("Failed to load VeeFormation prefab from Resources.");
            return;
        }
        //veeFormation.transform.position = masterPosition;
        //veeFormation.transform.rotation = masterRotation;
        veeFormation.transform.parent = formations;
        veeFormation.transform.position = formations.position;
        veeFormation.transform.rotation = formations.rotation;
        // Dynamically load SpaceMapUnits and position them using VeeFormation
        for (int i = 0; i < 5; i++) // Support up to 5 units in the formation
        {
            //     public string spaceMapUnitResourcePath = "AwayTeam/SpaceMapUnit"; // Path to SpaceMapUnit prefab in Resources
            var spaceMapUnitPrefab = Resources.Load<GameObject>(spaceMapUnitResourcePath);
            if (spaceMapUnitPrefab == null)
            {
                Debug.LogError($"Failed to load SpaceMapUnit prefab from Resources. Check path: {spaceMapUnitResourcePath}");
                continue;
            }

            // Instantiate and configure SpaceMapUnit
            var spaceMapUnit = Instantiate(spaceMapUnitPrefab);
            spaceMapUnits.Add(spaceMapUnit);

            // Parent to EncounterUnit for better hierarchy organization
            spaceMapUnit.transform.parent = units;
            spaceMapUnit.name = name+".unit."+i.ToString();
            // Set position and rotation based on VeeFormation
            Vector3 goalPosition = veeFormation.GetComponent<VeeFormation>().GetPosition(i) + transform.position;
            //Vector3 goalPosition = veeFormation.transform.position;
            //Quaternion goalRotation = masterRotation; // Add rotation logic if needed
            if (goalPosition == null)
            {
                Debug.LogError("Could not extract goal position");
            }
            SpaceMapUnitAgent positionable = spaceMapUnit.GetComponentInChildren<SpaceMapUnitAgent>(); // Assuming SpaceMapUnit handles its position
            if (positionable != null && goalPosition != null)
            {
                positionable.SetGoalPosition(goalPosition,immediate:true);
                //Debug.Log("SetRootLookAt Setting Root Angle now");
                positionable.SetRootLookAt(new Vector3(0,100,0),true);
            }
            else
            {
                Debug.LogError("SpaceMapUnit does not have the expected positionable interface.");
            }
            if (__is_running)
            {
                positionable.Run();
            }
            else
            {
                positionable.Pause();
            }
            //Debug.Break();
        }
    }
}
