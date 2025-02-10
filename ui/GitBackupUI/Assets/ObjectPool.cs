using System.Collections.Generic;
using UnityEngine;

public class ObjectPool:MonoBehaviour
{
    private static ObjectPool __instance;  
    // Singleton accessor
    public static ObjectPool Instance()
    {
        if (__instance == null)
        {
            Debug.LogError("ObjectPool instance not found! Make sure an ObjectPool is in the scene and Awake() is called.");
        }
        return __instance;
    }

    // Awake initializes the singleton instance
    private void Awake()
    {
        if (__instance == null)
        {
            __instance = this;
        }
        else if (__instance != this)
        {
            Debug.LogWarning("Duplicate ObjectPool instance found. Destroying this one.");
            Destroy(gameObject);
        }
    }

    // Dictionary to hold lists of pooled objects by prefab name
    private Dictionary<string, List<GameObject>> prefabPool = new Dictionary<string, List<GameObject>>();

    // Loads or retrieves a prefab and returns an instance
    public  GameObject Load(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab cannot be null!");
            return null;
        }

        string prefabName = prefab.name;

        // Ensure a list exists for the prefab
        if (!prefabPool.ContainsKey(prefabName))
        {
            prefabPool[prefabName] = new List<GameObject>();
        }

        // Check for inactive objects in the pool
        //Debug.Log(prefabPool);
        //Debug.Log(prefabName);
        foreach (var pooledObject in prefabPool[prefabName])
        {
            //Debug.Log(pooledObject);
            if (!pooledObject.activeInHierarchy)
            {
                pooledObject.SetActive(true);
                return pooledObject;
            }
        }

        // No inactive objects, instantiate a new one
        GameObject newInstance = Object.Instantiate(prefab);
        if(newInstance.activeInHierarchy == false)
            newInstance.SetActive(true); // If it seems off, double check we may need to turn it on. Just giv'r a go.
        //Debug.Log(newInstance);
        prefabPool[prefabName].Add(newInstance);
        return newInstance;
    }

    // Clears the pool for a specific prefab (optional utility)
    public  void ClearPool(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab cannot be null!");
            return;
        }

        string prefabName = prefab.name;

        if (prefabPool.ContainsKey(prefabName))
        {
            foreach (var obj in prefabPool[prefabName])
            {
                Object.Destroy(obj);
            }
            prefabPool[prefabName].Clear();
        }
    }

    // Clears all pools (optional utility)
    public  void ClearAllPools()
    {
        foreach (var pool in prefabPool.Values)
        {
            foreach (var obj in pool)
            {
                Object.Destroy(obj);
            }
        }
        prefabPool.Clear();
    }
}
