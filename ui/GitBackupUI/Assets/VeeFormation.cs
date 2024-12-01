using System.Collections.Generic;
using UnityEngine;

public class VeeFormation : MonoBehaviour
{
    [Header("Formation Slots")]
    public Transform slot1; // Reference to the first slot (position "1")
    private List<Transform> slots = new List<Transform>(); // Holds all slot references

    private void Awake()
    {
        // Dynamically load slots from children based on their names
        slots.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            // Add slots based on their names (e.g., "1", "2", "3", ...)
            if (int.TryParse(child.name, out int index))
            {
                if (index == 1)
                    slot1 = child;
                slots.Add(child);
            }
        }

        // Sort slots based on their names for consistency
        slots.Sort((a, b) => a.name.CompareTo(b.name));

        // Ensure slot1 is assigned
        if (slot1 == null)
        {
            Debug.LogError("VeeFormation requires a child object named '1' to determine relative positions.");
        }
    }

    /// <summary>
    /// Returns the position of a unit relative to the first slot (item "1").
    /// </summary>
    /// <param name="index">The index of the unit in the formation (0-based).</param>
    /// <returns>A Vector3 representing the position relative to slot1.</returns>
    public Vector3 GetPosition(int index)
    {
        if (index < 0 || index >= slots.Count)
        {
            Debug.LogWarning($"Index {index} is out of range for this formation (max {slots.Count - 1}). Returning Vector3.zero.");
            return Vector3.zero;
        }

        // Ensure slot1 is valid
        if (slot1 == null)
        {
            Debug.LogError("slot1 is not assigned. Ensure the child object named '1' exists in the hierarchy.");
            return Vector3.zero;
        }

        // Calculate relative position
        return (slots[index].position - slot1.position);
    }

    /// <summary>
    /// Returns all positions in the formation relative to the first slot (item "1").
    /// </summary>
    /// <returns>An array of Vector3 positions for the entire formation.</returns>
    public Vector3[] GetAllPositions()
    {
        Vector3[] positions = new Vector3[slots.Count];
        for (int i = 0; i < slots.Count; i++)
        {
            positions[i] = GetPosition(i);
        }
        return positions;
    }

    /// <summary>
    /// Returns the world position of a specific slot.
    /// </summary>
    /// <param name="index">The index of the slot (0-based).</param>
    /// <returns>The world position of the slot.</returns>
    public Vector3 GetWorldPosition(int index)
    {
        if (index < 0 || index >= slots.Count)
        {
            Debug.LogWarning($"Index {index} is out of range for this formation (max {slots.Count - 1}). Returning Vector3.zero.");
            return Vector3.zero;
        }

        return slots[index].position;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Visualize the formation in the editor
        Gizmos.color = Color.cyan;

        if (slot1 != null)
        {
            Vector3 origin = slot1.position;
            foreach (Transform slot in slots)
            {
                Gizmos.DrawSphere(slot.position, 0.2f);
                Gizmos.DrawLine(origin, slot.position); // Draw lines from slot1 to all other slots
            }
        }
    }
#endif
}
