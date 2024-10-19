using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

public class EditRepoScreen : StandardEditScreen, NavigationManager.ICanInitalize
{
    private RepoData repoData;

    protected override void Start()
    {
        base.Start();
        repoData = navigatorObject.GetComponent<RepoData>();
    }

    protected override void OnCancel()
    {
        navigationManager.NavigateTo("RepoListScreen");
    }


    protected override void OnSave(ClickEvent evt)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Retrieve form data using base method
        var fieldNames = new List<string> { "Name", "GitURL", "Branch" };
        var formData = GetFormData(GetComponent<UIDocument>(), fieldNames);

        // Perform validation
        if (string.IsNullOrEmpty(formData["GitURL"]) || !formData["GitURL"].StartsWith("https://"))
        {
            navigationManager.NotifyError("Invalid Git URL. Please enter a valid repository URL starting with 'https://'.");
            return;
        }

        if (string.IsNullOrEmpty(formData["Branch"]))
        {
            navigationManager.NotifyError("Branch must not be null.");
        }

        if (string.IsNullOrEmpty(formData["Name"]))
        {
            navigationManager.NotifyError("Name is required.");
        }


        // Add the repo if validation passes
        bool addedSuccessfully = repoData.AddRepo(formData["Name"], formData["GitURL"], formData["Branch"]);
        if (!addedSuccessfully)
        {
            navigationManager.NotifyError("Failed to add repository. Please try again.");
            return;
        }

        // Navigate to the RepoListScreen
        navigationManager.NavigateTo("RepoListScreen");
    }

    public void InitData(Dictionary<string, string> dataframe)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;



        if (dataframe.ContainsKey("name"))
        {
            Debug.Log($"RepoInfo.InitData for {dataframe["name"]} ");
            RecordRepoFull record = new RecordRepoFull(repoData.GetRecord(dataframe["name"]));
            SetTextboxText(root, "Name", record.name);
            SetTextboxText(root, "GitURL", record.url);
            SetTextboxText(root, "Branch", record.branch);
        }
        else
        {
            SetTextboxText(root, "Name", "UNKNOWN_NAME");
            SetTextboxText(root, "GitURL", "https://UNKNOWN_URL.COM");
            SetTextboxText(root, "Branch", "UNKNOWN_BRANCH");
        }
    }

    void OnEnable()
    {
        RegisterButtonCallbacks(GetComponent<UIDocument>(), "Save", "Cancel");
    }


    public void Refresh()
    {
        return;
    }

    public bool SetAction(string actionLabel, System.Action action)
    {
        return false;
    }
}
