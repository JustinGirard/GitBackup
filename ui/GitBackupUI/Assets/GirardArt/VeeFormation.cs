using System.Collections.Generic;
using UnityEngine;

public class VeeFormation : MonoBehaviour
{
    [Header("Formation Slots")]
    private List<Transform> slots = new List<Transform>(); // Holds all slot references
    private string selectedFormation = "formation_diamond";
    public string GetFormation(){
        return selectedFormation;
    }
    public void SetFormation(string formation){
        selectedFormation = formation;
        ResetFormation();
    }
    private void Awake()
    {
        ResetFormation();
    }
    private void ResetFormation()
    {
        slots.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.Find(selectedFormation).GetChild(i);

            // Add slots based on their names (e.g., "1", "2", "3", ...)
            if (int.TryParse(child.name, out int index))
            {
                slots.Add(child);
            }
        }

        // Sort slots based on their names for consistency
        slots.Sort((a, b) => a.name.CompareTo(b.name));
    }
    public Transform GetPositionTransform(int i)
    {
        return slots[i];
    }


}
