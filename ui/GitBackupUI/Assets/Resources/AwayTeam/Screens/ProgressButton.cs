using System;
using System.ComponentModel.Design;
using UnityEngine;

public class ProgressButton : StandardDynamicControl, IShowProgress
{
    private DockedButton dockedButton;
    private LinearProgressBar progressBar;
    public string commandId;

    protected override void Start()
    {
        base.Start();

        // Find child components
        dockedButton = GetComponentInChildren<DockedButton>();
        progressBar = GetComponentInChildren<LinearProgressBar>();

        if (dockedButton == null || progressBar == null)
        {
            Debug.LogError("ProgressButton requires both a DockedButton and ProgressBar as child objects.");
        }
    }

    public override string GetCommandId()
    {
        // Use DockedButton's commandId as the identifier
        return commandId;
    }

    public override void SetState(string state)
    {
        // Mirror state to both DockedButton and ProgressBar
        dockedButton.SetState(state);
        progressBar.SetState(state);
    }

    // Expose progress-specific functionality
    public bool SetProgress(int progress, string id="")
    {
        return progressBar.SetProgress(progress,id);
    }
    public int GetProgress(string id="")
    {
        return progressBar.GetProgress(id);
    }    
    public int GetProgressMax(string id="")
    {
        return progressBar.GetProgressMax(id);
    }    
    public void SetProgressMax(int max, string id="")
    {
        progressBar.SetProgressMax(max,id);
    }        

    // Additional functionality for input handling (inherits from StandardDynamicControl)

    public override void MouseDown(int buttonId, bool within)
    {
        base.MouseDown(buttonId, within);
        dockedButton?.MouseDown(buttonId, within);
        progressBar?.MouseDown(buttonId, within);
    }

    public override void MouseUp(int buttonId, bool within)
    {
        base.MouseUp(buttonId, within);
        // Handle DockedButton-specific behavior, if needed
        dockedButton?.MouseUp(buttonId, within);
        progressBar?.MouseUp(buttonId, within);
    }

    public override void RegisterInteractionHandler(System.Action<string, string, bool> handler)
    {
        base.RegisterInteractionHandler(handler);

        // Register handlers for both child components
        dockedButton?.RegisterInteractionHandler(handler);
        progressBar?.RegisterInteractionHandler(handler); // Requires ProgressBar extension for interaction handling, if applicable
    }
}
