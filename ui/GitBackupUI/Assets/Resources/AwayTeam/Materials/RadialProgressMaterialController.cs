using UnityEngine;
/*
public class RadialProgressMaterialController : MonoBehaviour
{
    private DockedButton dockedButton; // Reference to the DockedButton component
    private float __materialProgress; // Progress value between 0 and 1
    private float __materialAnimationDuration = 3f; // Time (in seconds) for one full loop
    private float __materialStartTime;

    void Awake()
    {
        // Get the DockedButton component
        dockedButton = GetComponent<DockedButton>();
        if (dockedButton == null)
        {
            Debug.LogError("RadialProgressMaterialController: No DockedButton component found.");
            return;
        }

        // Ensure active and inactive materials are instances
        dockedButton.activeMaterial = Instantiate(dockedButton.activeMaterial);
        dockedButton.inactiveMaterial = Instantiate(dockedButton.inactiveMaterial);

        // Record the starting time
        __materialStartTime = Time.time;
    }

    void Update()
    {
        if (dockedButton == null)
            return;

        // Calculate time elapsed since start
        float elapsedTime = Time.time - __materialStartTime;

        // Normalize progress to a value between 0 and 1 based on the elapsed time
        __materialProgress = (elapsedTime % __materialAnimationDuration) / __materialAnimationDuration;

        // Update the progress on the active material (if button is active)
        if (dockedButton._isActivated && dockedButton.activeMaterial != null)
        {
            dockedButton.activeMaterial.SetFloat("_Progress", __materialProgress);
        }

        // Optionally, update the inactive material's progress if needed
        if (!dockedButton._isActivated && dockedButton.inactiveMaterial != null)
        {
            dockedButton.inactiveMaterial.SetFloat("_Progress", __materialProgress);
        }
    }
}
*/