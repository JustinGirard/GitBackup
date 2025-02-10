using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class EncounterSquad : MonoBehaviour, IPausable
{
    [Header("Settings")]
    // public Vector3 masterPosition = Vector3.zero; // The central "master position"
    public Quaternion masterRotation = Quaternion.identity; // Optional if rotations are needed
    // [SerializeField] public float unitTravelSmoothTime = 0.3f;
    [SerializeField]
    private int __numUnits = 2;

    [Header("Resources")]
    public string spaceMapUnitResourcePath = "AwayTeam/KeyObjects/UnitExplorerShip_Physics"; // Path to SpaceMapUnit prefab in Resources
    public string veeFormationResourcePath = "AwayTeam/KeyObjects/VeeFormation"; // Path to VeeFormation prefab in Resources

    private List<GameObject> spaceMapUnits = new List<GameObject>(); // Tracks spawned SpaceMapUnit instances
    private GameObject veeFormation; // The dynamically loaded VeeFormation instance

    private bool __is_running = false;
    
    public string GetFormation()
    {
          VeeFormation f = veeFormation.GetComponent<VeeFormation>();
          return f.GetFormation();
    }
    public void SetFormation(string formation )
    {
        VeeFormation f = veeFormation.GetComponent<VeeFormation>();
        f.SetFormation(formation);

        int i = 0;
        foreach (GameObject unitGO in spaceMapUnits)
        {
            if (unitGO == null)
                continue;
            SimpleShipController unit = unitGO.GetComponentInChildren<SimpleShipController>(); 
            Transform formationPosition = veeFormation.GetComponent<VeeFormation>().GetPositionTransform(i); 
            unit.SetGoalPosition(formationPosition);
            //unit.SetGoalTarget(formationPosition, formationPosition.forward *2f);
            i = i +1;
        }
        // Debug.Log("Formation updated");
    }

    public void Run()
    {
        foreach(GameObject spaceMapUnit in spaceMapUnits)
        {
            SimpleShipController su = spaceMapUnit.GetComponentInChildren<SimpleShipController>(); 
            //su.Run();
        }

        __is_running = true;
    }
    public void Pause()
    {
        foreach(GameObject spaceMapUnit in spaceMapUnits)
        {
            SimpleShipController su = spaceMapUnit.GetComponentInChildren<SimpleShipController>();
            //su.Pause();
        }
        __is_running = false;
    }
    public List<SimpleShipController> GetUnitList()
    {
    
        List<SimpleShipController> suses = new List<SimpleShipController>();
        List<GameObject> remove = new List<GameObject>();
        foreach(GameObject spaceMapUnit in spaceMapUnits)
        {
            SimpleShipController su = spaceMapUnit.GetComponentInChildren<SimpleShipController>();
            if (su == null)
            {
                Debug.LogWarning("Found empty unit. Doing Autocleaning. This should not be needed.");
                remove.Add(spaceMapUnit);
            }
            else
            {
                suses.Add(su);
            }
        }
        foreach(GameObject invalidSpaceMapUnit in remove)
        {
            Debug.LogWarning($"Cleaning .. {invalidSpaceMapUnit.name}");
            spaceMapUnits.Remove(invalidSpaceMapUnit);
            GameObject.Destroy(invalidSpaceMapUnit);
        }
        return suses;
    }
    public bool IsRunning()
    {
        return __is_running;
    }    

    public void Update(){

        UpdateGoalPosition();        
    }
    /// <summary>
    /// Cleans up and respawns all SpaceMapUnits and the VeeFormation.
    /// </summary>


    /// <summary>
    /// Cleans up all dynamically spawned units and formation objects.
    /// </summary>
    private  System.Collections.IEnumerator Cleanup()
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
        yield return null;
    }

    /// <summary>
    /// Respawns all SpaceMapUnits and the VeeFormation from Resources.
    /// </summary>
    /// 
    public void UpdatePosition()
    {
        return;

    }
    public void NotifyDestroy(GameObject removed){
        spaceMapUnits.Remove(removed);
    }
    public System.Collections.IEnumerator Rebuild()
    {
        yield return Cleanup();
        yield return Respawn();
    }
    private System.Collections.IEnumerator Respawn()
    {
        
        Transform units = transform.Find("Units");
        units.name = $"UnitsFor{name}";
        if (units == null)
        {
            Debug.LogError("Failed to load VeeFormation prefab from Resources.");
            yield break;    
        }
        Transform formations = transform.Find("Formations");
        if (formations == null)
        {
            Debug.LogError("Failed to load VeeFormation prefab from Resources.");
            yield break;            
        }

        veeFormation = Instantiate(Resources.Load<GameObject>(veeFormationResourcePath));
        if (veeFormation == null)
        {
            Debug.LogError("Failed to load VeeFormation prefab from Resources.");
            yield break;
        }

        veeFormation.transform.parent = formations;
        yield return new WaitForEndOfFrame(); 
        //spaceMapUnit.transform.SetParent(units, false);
        yield return new WaitForFixedUpdate(); // Ensures parenting changes are synchronized

        veeFormation.transform.position = formations.position;
        this.goalPosition = formations.position;

        veeFormation.transform.rotation = formations.rotation;
        this.goalRotation = formations.rotation;

        yield return new WaitForEndOfFrame(); 
        yield return new WaitForFixedUpdate(); // Ensures parenting changes are synchronized
        for (int i = 0; i < __numUnits; i++)
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
            yield return new WaitForEndOfFrame();   
            yield return new WaitForFixedUpdate();

            // Parent to EncounterUnit for better hierarchy organization
            spaceMapUnit.transform.parent = units;
            yield return new WaitForEndOfFrame();             
            yield return new WaitForFixedUpdate();

            SimpleShipController unit = spaceMapUnit.GetComponentInChildren<SimpleShipController>(); 
            unit.SetName(name+".unit."+i.ToString());
            Transform formationPosition = veeFormation.GetComponent<VeeFormation>().GetPositionTransform(i); 
            yield return new WaitForEndOfFrame();             
            yield return new WaitForFixedUpdate();
            // 
            unit.transform.position =  formationPosition.position;
            unit.transform.rotation =  formationPosition.rotation;
            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();
            unit.SetGoalPosition(formationPosition);
            unit.SetGoalTarget(formationPosition, formationPosition.forward *2f);
            yield return new WaitForEndOfFrame(); 
            yield return new WaitForFixedUpdate();

        }
        //Debug.Break(); // WHEN DEBUG BREAK RUNS, ALL SHIPS RENDER CORRECTLY
        //yield return new WaitForSeconds(5);
    }
    /*
    public void SetGoalPosition(Transform t, Vector3? offset)
    {
        // Debug.Log("Set goal position");
        veeFormation.transform.position = t.position;
        veeFormation.transform.rotation = t.rotation;
    }*/
    private Vector3 goalPosition;
    private Vector3 goalVelocity = Vector3.zero;
    private float goalSmoothTime = 0.2f;

    private Quaternion goalRotation;
    private float goalRotationSpeed = 5.0f; // Adjustable rotation smoothing speed

    public void SetGoalPosition(Transform t, float customSmoothTime = 0.2f, float customRotationSpeed = 5.0f)
    {
        // Store goal position and rotation
        goalPosition = t.position;
        goalRotation = t.rotation;

        // Store smoothing parameters
        goalSmoothTime = customSmoothTime;
        goalRotationSpeed = customRotationSpeed;
        CreateDebugCube(goalPosition, $"EncounterSquadGoal-{this.gameObject.name}", Color.red);
    }
    private Dictionary<string, GameObject> debugCubes = new Dictionary<string, GameObject>();    
    private void CreateDebugCube(Vector3 position, string id,Color cin)
    {
        // Check if a debug cube already exists for this ID
        if (debugCubes.TryGetValue(id, out GameObject cube))
        {
            // Move the existing cube to the new position
            cube.transform.position = position;
        }
        else
        {
            // Create a new cube
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale =  new Vector3(0.15f,1.5f,0.15f);  // Scale down for better visibility
            
            cube.GetComponent<Renderer>().material.color = cin; // Make it red for easier identification
            cube.transform.position = position;
            cube.name = id;
            if (cube.TryGetComponent<BoxCollider>(out BoxCollider collider))
            {
                Destroy(collider); // Removes it completely
                // collider.enabled = false; // Alternative: Just disables it
            }            
            // Add the new cube to the dictionary
            debugCubes[id] = cube;
        }
    }
    public void UpdateGoalPosition()
    {
        if (veeFormation == null)
        {
            //Debug.LogWarning("UpdateGoalPosition: veeFormation is null. Aborting.");
            return;
        }

        if (veeFormation.transform == null)
        {
            Debug.LogWarning("UpdateGoalPosition: veeFormation.transform is null. Aborting.");
            return;
        }

        if (goalPosition == null)
        {
            Debug.LogWarning("UpdateGoalPosition: goalPosition is null. Aborting.");
            return;
        }

        if (goalVelocity == null)
        {
            Debug.LogWarning("UpdateGoalPosition: goalVelocity is null. Aborting.");
            return;
        }

        if (goalSmoothTime <= 0)
        {
            Debug.LogWarning("UpdateGoalPosition: goalSmoothTime is non-positive. Aborting.");
            return;
        }

        if (goalRotation == null)
        {
            Debug.LogWarning("UpdateGoalPosition: goalRotation is null. Aborting.");
            return;
        }

        if (goalRotationSpeed <= 0)
        {
            Debug.LogWarning("UpdateGoalPosition: goalRotationSpeed is non-positive. Aborting.");
            return;
        }
        //Debug.Log(".");
        // Apply SmoothDamp for position
        veeFormation.transform.position = Vector3.SmoothDamp(
            veeFormation.transform.position, goalPosition, ref goalVelocity, goalSmoothTime);

        // Apply Slerp for smooth rotation
        veeFormation.transform.rotation = Quaternion.Slerp(
            veeFormation.transform.rotation, goalRotation, Time.deltaTime * goalRotationSpeed);
    }



    public void SetGoalTarget(Transform t, Vector3? offset)
    {
        foreach(GameObject unit in spaceMapUnits)
        {
            (unit.GetComponentInChildren<SimpleShipController>()).SetGoalTarget(t,offset);
        }
    }

}
