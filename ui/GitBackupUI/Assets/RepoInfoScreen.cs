using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

public class RepoInfoScreen : MonoBehaviour, NavigationManager.ICanInitalize
{
    void OnEnable()
    {

        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        var root = GetComponent<UIDocument>().rootVisualElement;

        var navigateElement = root.Q<Button>("UNDEFINED"); // TODO initalize null
        navigateElement = root.Q<Button>("Back");
        navigateElement.RegisterCallback<ClickEvent>(evt => navigationManager.NavigateTo("RepoListScreen"));
    }

    private void SetLabelText(VisualElement root,  string labelName, string value)
    {
        var label = root.Q<Label>(labelName);
        label.text = value;
    }
    public bool SetAction(string actionLabel, System.Action action)
    {
        // Does not support programmable buttons
        return false;
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void InitData(Dictionary<string, string> dataframe)
    {

        var root = GetComponent<UIDocument>().rootVisualElement;
        var navigatorObject = GameObject.Find("Navigator");
        RepoData repoData = navigatorObject.GetComponent<RepoData>();


        Debug.Log($"RepoInfo.InitData for {dataframe["name"]} ");
        RecordRepoFull record = new RecordRepoFull(repoData.GetRecord(dataframe["name"]));
        

        // Populate user-generated section
        SetLabelText(root,  "git-url", record.url );
        SetLabelText(root,  "username","UNKNOWN");
        SetLabelText(root,  "target-branch", record.branch);

        // Populate system-generated section
        SetLabelText(root, "status", record.status);
        SetLabelText(root, "last-hash", "UNKNOWN");
        SetLabelText(root, "last-commit-time", "UNKNOWN") ;
        SetLabelText(root, "last-update-time", "UNKNOWN");
        SetLabelText(root, "conflict-status", "UNKNOWN");
        SetLabelText(root, "size-on-disk", "UNKNOWN");

        /*
        // Handle lists (Active Replicators, Recent Replicators)
        var activeReplicatorsList = root.Q<ListView>("active-replicators");
        if (activeReplicatorsList != null && record.ContainsKey("active_replicators"))
        {
            var replicators = record["active_replicators"].Split(','); // Assuming comma-separated replicators
            activeReplicatorsList.itemsSource = new List<string>(replicators);
        }

        var recentReplicatorsList = root.Q<ListView>("recent-replicators");
        if (recentReplicatorsList != null && record.ContainsKey("recent_replicators"))
        {
            var replicators = record["recent_replicators"].Split(','); // Assuming comma-separated replicators
            recentReplicatorsList.itemsSource = new List<string>(replicators);
        }*/

    }

    public void Refresh()
    {
        //throw new System.NotImplementedException();
    }
}
