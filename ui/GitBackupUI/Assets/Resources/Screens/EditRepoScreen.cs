/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EditRepoScreen : MonoBehaviour, NavigationManager.ICanInitalize
{
    private GameObject navigatorObject;
    private NavigationManager navigationManager;
    private RepoData repoData;
    // Start is called before the first frame update
    void Start()
    {
        navigatorObject = GameObject.Find("Navigator");
        navigationManager = navigatorObject.GetComponent<NavigationManager>();
        repoData = navigatorObject.GetComponent<RepoData>();
        // DictStrStr

    }

    // Update is called once per frame
    void Update()
    {

    }

    // Method to perform the save operation
    void DoSave(ClickEvent evt)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Retrieve the values from the input fields
        var nameField = root.Q<TextField>("Name");
        var gitUrlField = root.Q<TextField>("GitURL");
        var usernameField = root.Q<TextField>("Username");
        var accessKeyField = root.Q<TextField>("AccessKey");
        var branchField = root.Q<TextField>("Branch");

        // Sanitize inputs
        string gitUrl = gitUrlField.text.Trim();
        string username = usernameField.text.Trim();
        string name = nameField.text.Trim();
        string accessKey = accessKeyField.text.Trim();
        string branch = branchField.text.Trim();

        // Basic validation (you can extend this based on your requirements)
        if (string.IsNullOrEmpty(gitUrl) || !gitUrl.StartsWith("https://"))
        {
            // Invalid Git URL
            navigationManager.NotifyError("Invalid Git URL. Please enter a valid repository URL starting with 'https://'.");
            return;
        }

        if (string.IsNullOrEmpty(branch))
        {
            navigationManager.NotifyError("Branch must not be null.");
        }

        if (string.IsNullOrEmpty(name))
        {
            navigationManager.NotifyError("Name is required.");
        }

        if (string.IsNullOrEmpty(accessKey))
        {
            navigationManager.NotifyError("Access key is required.");
            return;
        }

        // If everything is valid, add the repo to the repo list (this part depends on how you manage repos)
        //var repoData = new RepoData();
        Debug.Log("Should soon add a repo");
        bool addedSuccessfully = repoData.AddRepo(name, gitUrl, branch);
        Debug.Log("Should have added a repo");

        if (!addedSuccessfully)
        {
            navigationManager.NotifyError("Failed to add repository. Please try again.");
            return;
        }

        // If successful, navigate back to the RepoListScreen
        navigationManager.NavigateTo("RepoListScreen");
    }
    public bool SetAction(string actionLabel, System.Action action)
    {
        // Does not support programmable buttons
        return false;
    }


    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var cancelButton = root.Q<Button>("Cancel");
        var saveButton = root.Q<Button>("Save");
        saveButton.RegisterCallback<ClickEvent>(evt => DoSave(evt));
        cancelButton.RegisterCallback<ClickEvent>(evt => navigationManager.NavigateTo("RepoListScreen"));
    }

    private void SetTextboxText(VisualElement root, string labelName, string value)
    {
        var label = root.Q<TextField>(labelName);
        label.value = value;
    }

    public void InitData(Dictionary<string, string> dataframe)
    {

        var root = GetComponent<UIDocument>().rootVisualElement;
        var navigatorObject = GameObject.Find("Navigator");
        RepoData repoData = navigatorObject.GetComponent<RepoData>();

        if (dataframe.ContainsKey("name"))
        {
            Debug.Log($"RepoInfo.InitData for {dataframe["name"]} ");
            RecordRepoFull record = new RecordRepoFull(repoData.GetRecord(dataframe["name"]));

            SetTextboxText(root, "Name", record.name);
            SetTextboxText(root, "GitURL", record.url);
            SetTextboxText(root, "Branch", record.branch);
            SetTextboxText(root, "Username", "UNKNOWN");
            SetTextboxText(root, "AccessKey", "UNKNOWN");
        }
        else
        {
            SetTextboxText(root, "Name", "UNKNOWN_NAME");
            SetTextboxText(root, "GitURL", "https://UNKNOWN_URL.COM");
            SetTextboxText(root, "Branch", "UNKNOWN_BRANCH");
            SetTextboxText(root, "Username", "UNKNOWN_USERNAME");
            SetTextboxText(root, "AccessKey", "UNKNOWN_ACCESS_KEY");

        }
    }

    public void Refresh()
    {
        //throw new System.NotImplementedException();
    }
}
*/

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

public abstract class StandardEditScreen : MonoBehaviour
{
    protected GameObject navigatorObject;
    protected NavigationManager navigationManager;

    protected virtual void Start()
    {
        navigatorObject = GameObject.Find("Navigator");
        navigationManager = navigatorObject.GetComponent<NavigationManager>();
    }

    protected abstract void OnSave(ClickEvent evt);

    protected abstract void OnCancel();

    protected void RegisterButtonCallbacks(UIDocument document, string saveButtonName, string cancelButtonName)
    {
        var root = document.rootVisualElement;
        var saveButton = root.Q<Button>(saveButtonName);
        var cancelButton = root.Q<Button>(cancelButtonName);

        saveButton.RegisterCallback<ClickEvent>(OnSave);
        cancelButton.RegisterCallback<ClickEvent>(evt => OnCancel());
    }

    protected void SetTextboxText(VisualElement root, string labelName, string value)
    {
        var textField = root.Q<TextField>(labelName);
        if (textField != null)
        {
            textField.value = value;
        }
    }

    protected Dictionary<string, string> GetFormData(UIDocument document, List<string> fieldNames)
    {
        var root = document.rootVisualElement;
        var formData = new Dictionary<string, string>();

        foreach (var fieldName in fieldNames)
        {
            var textField = root.Q<TextField>(fieldName);
            if (textField != null)
            {
                formData[fieldName] = textField.text.Trim();
            }
        }

        return formData;
    }
}

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
