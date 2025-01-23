using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class EncounterSquad : MonoBehaviour, IPausable
{
    [Header("Settings")]
    // public Vector3 masterPosition = Vector3.zero; // The central "master position"
    public Quaternion masterRotation = Quaternion.identity; // Optional if rotations are needed
    [SerializeField]
    private int __numUnits = 2;

    [Header("Resources")]
    public string spaceMapUnitResourcePath = "AwayTeam/KeyObjects/UnitExplorerShip_Physics"; // Path to SpaceMapUnit prefab in Resources
    public string veeFormationResourcePath = "AwayTeam/KeyObjects/VeeFormation"; // Path to VeeFormation prefab in Resources

    private List<GameObject> spaceMapUnits = new List<GameObject>(); // Tracks spawned SpaceMapUnit instances
    private GameObject veeFormation; // The dynamically loaded VeeFormation instance

    private bool __is_running = false;
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
        veeFormation.transform.rotation = formations.rotation;
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

            unit.SetGoalPosition(formationPosition, Vector3.zero);
            unit.SetGoalTarget(formationPosition, formationPosition.forward *2f);
            yield return new WaitForEndOfFrame(); 
            yield return new WaitForFixedUpdate();

        }
        //Debug.Break(); // WHEN DEBUG BREAK RUNS, ALL SHIPS RENDER CORRECTLY
        //yield return new WaitForSeconds(5);
    }
    public void SetGoalPosition(Transform t, Vector3? offset)
    {
        Debug.Log("Set goal position");
        veeFormation.transform.position = t.position;
        veeFormation.transform.rotation = t.rotation;
    
        //foreach(GameObject unit in spaceMapUnits)
        //{
        //    (unit.GetComponent<SimpleShipController>()).SetGoalPosition(t, Vector3.zero);
        //}
    }

    public void SetGoalTarget(Transform t, Vector3? offset)
    {
        foreach(GameObject unit in spaceMapUnits)
        {
            (unit.GetComponent<SimpleShipController>()).SetGoalTarget(t,offset);
        }
    }

}
