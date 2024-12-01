using System.Collections.Generic;
using UnityEngine;

public class EncounterUnitController : MonoBehaviour
{
    [Header("Settings")]
    // public Vector3 masterPosition = Vector3.zero; // The central "master position"
    public Quaternion masterRotation = Quaternion.identity; // Optional if rotations are needed

    [Header("Resources")]
    public string spaceMapUnitResourcePath = "AwayTeam/SpaceMapUnit"; // Path to SpaceMapUnit prefab in Resources
    public string veeFormationResourcePath = "AwayTeam/VeeFormation"; // Path to VeeFormation prefab in Resources

    private List<GameObject> spaceMapUnits = new List<GameObject>(); // Tracks spawned SpaceMapUnit instances
    private GameObject veeFormation; // The dynamically loaded VeeFormation instance

    void Start()
    {
        Rebuild(); // Automatically rebuild on Start
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
    private void Respawn()
    {
        // Load and instantiate VeeFormation
        Transform units = transform.Find("Units");
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

            // Set position and rotation based on VeeFormation
            Vector3 goalPosition = veeFormation.GetComponent<VeeFormation>().GetPosition(i) + transform.position;
            //Quaternion goalRotation = masterRotation; // Add rotation logic if needed
            if (goalPosition == null)
            {
                Debug.LogWarning("Could not extract goal position");
            }
            SpaceMapUnitAgent positionable = spaceMapUnit.GetComponent<SpaceMapUnitAgent>(); // Assuming SpaceMapUnit handles its position
            if (positionable != null && goalPosition != null)
            {
                positionable.SetGoalPosition(goalPosition,immediate:true);
            }
            else
            {
                Debug.LogWarning("SpaceMapUnit does not have the expected positionable interface.");
            }
        }
    }
}
