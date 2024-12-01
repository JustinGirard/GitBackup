using UnityEngine;
using System.Collections.Generic;

public class DynamicButtonRenderer : MonoBehaviour
{
    public static DynamicButtonRenderer Instance; // Singleton instance for easy access

    private List<DockedButton> registeredButtons = new List<DockedButton>(); // List of registered buttons

    private void Awake()
    {
        // Set up the singleton instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple instances of RenderCamera found. Ensure only one RenderCamera exists in the scene.");
            Destroy(this);
        }
    }

    public void RegisterButton(DockedButton button)
    {
        // Add the button to the list if it's not already registered
        if (!registeredButtons.Contains(button))
        {
            registeredButtons.Add(button);
        }
    }

    private void LateUpdate()
    {
        // Render all registered buttons
        foreach (var button in registeredButtons)
        {
            if (button != null && button.renderTexture != null)
            {
                RenderButton(button);
            }
        }
    }

    private void RenderButton(DockedButton button)
    {
        // Position the camera to focus on the button
        // Debug.Log($"Rendring button {button.name}");
        // Debug.Log($"Rendring button {button.renderTexture.name}");
        /*
        transform.position = button.transform.position + button.cameraOffset;

        // Assign the RenderTexture to the camera
        GetComponent<Camera>().targetTexture = button.renderTexture;

        // Render the button
        GetComponent<Camera>().Render();

        // Reset the camera's target texture to avoid interference
        GetComponent<Camera>().targetTexture = null;*/

        // Move the camera to the desired position relative to the button
        transform.position = button.transform.position + button.cameraOffset;

        // Make the camera look at the button
        transform.LookAt(button.transform.position);

        // Assign the RenderTexture to the camera
        GetComponent<Camera>().targetTexture = button.renderTexture;

        // Render the button
        GetComponent<Camera>().Render();

        // Reset the camera's target texture to avoid interference
        GetComponent<Camera>().targetTexture = null;        
    }
}
