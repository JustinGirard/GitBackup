
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

public class ApplicationState : MonoBehaviour
{
    // Dictionary to store key-value pairs
    private Dictionary<string, object> stateDictionary = new Dictionary<string, object>();

    // Dictionary to store callbacks for specific keys
    private Dictionary<string, Action<object>> changeCallbacks = new Dictionary<string, Action<object>>();

    // Ensure only one instance exists
    private void Awake()
    {
        instance = this;
        // Singleton pattern or any other initialization can be done here
    }

    private static ApplicationState instance;

    public static ApplicationState Instance()
    {
        return instance;
    }

    /// <summary>
    /// Sets a value in the application state.
    /// Notifies registered callbacks if the value has changed.
    /// </summary>
    public void Set(string key, object value)
    {
        bool valueChanged = !stateDictionary.ContainsKey(key) || !Equals(stateDictionary[key], value);
        stateDictionary[key] = value;

        // If the value changed, notify registered callbacks
        if (valueChanged && changeCallbacks.ContainsKey(key))
        {
            changeCallbacks[key]?.Invoke(value);
        }
    }

    /// <summary>
    /// Retrieves a value from the application state.
    /// </summary>
    public object Get(string key)
    {
        stateDictionary.TryGetValue(key, out object value);
        return value;
    }

    /// <summary>
    /// Checks if a key exists in the application state.
    /// </summary>
    public bool ContainsKey(string key)
    {
        return stateDictionary.ContainsKey(key);
    }

    /// <summary>
    /// Removes a key-value pair from the application state.
    /// </summary>
    public void RemoveKey(string key)
    {
        stateDictionary.Remove(key);
        changeCallbacks.Remove(key);
    }

    /// <summary>
    /// Clears all key-value pairs from the application state.
    /// </summary>
    public void ClearState()
    {
        stateDictionary.Clear();
        changeCallbacks.Clear();
    }

    /// <summary>
    /// Registers a callback function to be invoked when a specific key's value changes.
    /// </summary>
    /// <param name="key">The key to watch for changes.</param>
    /// <param name="callback">The callback function to invoke when the key's value changes.</param>
    public void RegisterChangeCallback(string key, Action<object> callback)
    {
        if (changeCallbacks.ContainsKey(key))
        {
            changeCallbacks[key] += callback;
        }
        else
        {
            changeCallbacks[key] = callback;
        }
    }

    /// <summary>
    /// Unregisters a callback function for a specific key.
    /// </summary>
    /// <param name="key">The key whose callback should be removed.</param>
    /// <param name="callback">The callback function to remove.</param>
    public void UnregisterChangeCallback(string key, Action<object> callback)
    {
        if (changeCallbacks.ContainsKey(key))
        {
            changeCallbacks[key] -= callback;

            // Remove the key from the dictionary if no callbacks remain
            if (changeCallbacks[key] == null)
            {
                changeCallbacks.Remove(key);
            }
        }
    }

    /*
    private static readonly Queue<Action> executionQueue = new Queue<Action>();

    public void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                executionQueue.Dequeue()?.Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }*/

    private readonly ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();

    public void Enqueue(Action action)
    {
        _actionQueue.Enqueue(action);
    }

    void Update()
    {
        while (_actionQueue.TryDequeue(out var action))
        {
            action?.Invoke();
        }
    }    

}
