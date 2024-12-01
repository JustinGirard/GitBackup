using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SpaceCombatScreen : MonoBehaviour
{
    // Reference to the ResourceData object (attach in the Inspector or find dynamically)
    public ATResourceData resourceData;

    // UXML root and mapping of UXML#ID to resource names
    private VisualElement root;
    private Dictionary<string, string> uxmlToResourceMapping;
    void Awake()
    {
        uxmlToResourceMapping = new Dictionary<string, string>
        {
            { "status-value-1-1", "Health" },
            { "status-value-1-2", "Shields" },
            { "status-value-2-1", "Energy" },
            { "status-value-2-2", "Ammunition" }
        };

    }
    // Start is called before the first frame update
    void Start()
    {
        // Log activation
        resourceData.Deposit("Health", 100f);
        resourceData.Deposit("Shields", 50f);
        resourceData.Deposit("Energy", 75f);
        resourceData.Deposit("Ammunition", 200f);

        // Ensure ResourceData is assigned
        if (resourceData == null)
        {
            Debug.LogError("ResourceData is not assigned to SpaceCombatScreen.");
            return;
        }

        // Get the UXML root element (assuming a UIDocument component is attached to the same GameObject)
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("No UIDocument component found on SpaceCombatScreen GameObject.");
            return;
        }
        root = uiDocument.rootVisualElement;

        // Initialize the mapping of UXML#ID to resource names dynamically


        // Populate fields from the resource data
        PopulateFields();
    }

    // Update is called once per frame
    void Update()
    {
        // Dynamically update the status fields during the encounter
        UpdateFields();
    }

    /// <summary>
    /// Initializes the mapping of UXML#ID to resource names.
    /// </summary>


    /// <summary>
    /// Populates the fields in the UI based on the resource data.
    /// </summary>
    private void PopulateFields()
    {
        foreach (var mapping in uxmlToResourceMapping)
        {
            // Get the resource name and corresponding UXML IDs
            string uxmlId = mapping.Key;
            string resourceName = mapping.Value;

            // Find the Label in the UXML by its ID
            var label = root.Q<Label>(uxmlId);
            if (label != null)
            {
                // Get the resource amount from ResourceData
                object resourceAmount = resourceData.GetResourceAmount(resourceName);

                // Update the label's text
                label.text = $"{resourceName}: {resourceAmount}";
            }
            else
            {
                Debug.LogWarning($"UXML Label with ID {uxmlId} not found.");
            }

            // Find the associated image element and add a name label as a placeholder
            string imageId = uxmlId.Replace("status-value", "status-icon"); // Assuming a consistent naming pattern
            var imageElement = root.Q<VisualElement>(imageId);
            if (imageElement != null)
            {
                // Create a new label to act as a placeholder
                var placeholderLabel = new Label(resourceName)
                {
                    name = $"{imageId}-label",
                    style = { unityTextAlign = TextAnchor.MiddleCenter }
                };

                // Add the label to the image element
                imageElement.Clear(); // Clear existing children to avoid duplication
                imageElement.Add(placeholderLabel);
            }
            else
            {
                Debug.LogWarning($"UXML Image element with ID {imageId} not found.");
            }
        }

        Debug.Log("Fields populated with initial resource data.");
    }

    /// <summary>
    /// Updates the fields dynamically during the encounter.
    /// </summary>
    private void UpdateFields()
    {
        foreach (var mapping in uxmlToResourceMapping)
        {
            // Get the resource name and corresponding UXML ID
            string uxmlId = mapping.Key;
            string resourceName = mapping.Value;
            if (root == null)
            {
                Debug.LogError("No Root Element");
                return;
            }

            // Update the label for the resource value
            var label = root.Q<Label>(uxmlId);
            if (label != null)
            {
                // Get the resource amount from ResourceData
                object resourceAmount = resourceData.GetResourceAmount(resourceName);

                // Update the label's text
                label.text = $"{resourceName}: {resourceAmount}";
            }

            // Update the placeholder label on the image (if needed)
            string imageId = uxmlId.Replace("status-value", "status-icon");
            var imageElement = root.Q<VisualElement>(imageId);
            if (imageElement != null)
            {
                var placeholderLabel = imageElement.Q<Label>($"{imageId}-label");
                if (placeholderLabel != null)
                {
                    placeholderLabel.text = resourceName; // Keep the name updated, if necessary
                }
            }
        }
    }
}
