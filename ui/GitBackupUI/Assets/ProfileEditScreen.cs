
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;


public class ProfileEditScreen : StandardEditScreen, NavigationManager.ICanInitalize
{
    private RepoData repoData;

    protected override void Start()
    {
        base.Start();
        repoData = navigatorObject.GetComponent<ProfileData>();
    }

    protected override void OnCancel()
    {
        navigationManager.NavigateTo("ProfileListScreen");
    }


    protected override void OnSave(ClickEvent evt)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Retrieve form data using base method
        var fieldNames = new List<string> { "ProfileName", "GitUser", "GitKey", "StorageLocation" };
        var formData = GetFormData(GetComponent<UIDocument>(), fieldNames);
        foreach (var field in fieldNames)
        {
            if (string.IsNullOrEmpty(formData[field]))
            {
                navigationManager.NotifyError($"{field} must not be null.");
                return;
            }

        }



        // Add the repo if validation passes
        bool addedSuccessfully = ((ProfileData)repoData).AddProfile(formData["ProfileName"], formData["GitUser"], formData["StorageLocation"], formData["GitKey"]);
        if (!addedSuccessfully)
        {
            navigationManager.NotifyError("Failed to add repository. Please try again.");
            return;
        }

        // Navigate to the RepoListScreen
        navigationManager.NavigateTo("ProfileListScreen");
    }


    /*
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
     
     */
    void OnEnable()
    {
        RegisterButtonCallbacks(GetComponent<UIDocument>(), "Save", "Cancel");
    }

    public void InitData(Dictionary<string, string> dataframe)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;


        //SetTextboxText(root, "ProfileName", dataframe.ContainsKey("name") ? dataframe["name"] : "UNKNOWN_NAME");
        //SetTextboxText(root, "GitUser", dataframe.ContainsKey("username") ? dataframe["username"] : "UNKNOWN_GIT_USER");
        //SetTextboxText(root, "GitKey", dataframe.ContainsKey("access_key") ? dataframe["access_key"] : "UNKNOWN_GIT_KEY");
        //SetTextboxText(root, "StorageLocation", dataframe.ContainsKey("path") ? dataframe["path"] : "UNKNOWN_STORAGE_LOCATION");

        if (dataframe.ContainsKey("name"))
        {
            Debug.Log($"RepoInfo.InitData for {dataframe["name"]} ");
            Debug.Log($"ProfileData LOADING ----------------------");
            DictStrStr record = repoData.GetRecord(dataframe["name"]);
            foreach (var pair in record)
            {
                Debug.Log($"{pair.Key}: {pair.Value}");
            }
            SetTextboxText(root, "ProfileName", record["name"]);
            SetTextboxText(root, "GitUser", record["username"]);
            SetTextboxText(root, "GitKey", record["access_key"]);
            SetTextboxText(root, "StorageLocation", record["path"]);
        }
        else
        {
            SetTextboxText(root, "ProfileName", "New Name");
            SetTextboxText(root, "GitUser", "New Username");
            SetTextboxText(root, "GitKey", "Access Key");
            SetTextboxText(root, "StorageLocation", "OS path");
        }


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
