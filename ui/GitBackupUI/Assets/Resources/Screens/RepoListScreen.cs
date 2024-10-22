using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UIElements;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

public class RepoListScreen : StandardListScreen, NavigationManager.ICanInitalize
{

    // Should we look at the "local" harddrive or "github"
    public string __repoSource = "local";    

    public string[] __repoSources = new string[]{"local","github"};    
    void Start()
    {
        var navigatorObject = GameObject.Find("Navigator");
        if(__repoSource == "local")
        {
            // TODO - Generalize BindToProfileData into base class
            datasource = navigatorObject.GetComponent<RepoData>();
            listContainerId = "repo_list_items";
            ProfileData profileDatasource = navigatorObject.GetComponent<ProfileData>();
            ((RepoData) datasource).BindToProfileData(profileDatasource);
        }
        if(__repoSource == "github")
        {
            datasource = navigatorObject.GetComponent<RepoGithubData>();
            listContainerId = "unity-content-container";
            ProfileData profileDatasource = navigatorObject.GetComponent<ProfileData>();
            ((RepoGithubData) datasource).BindToProfileData(profileDatasource);
        }

        BaseStart();
    }
    void Update(){
        BaseUpdate();        
    }

    protected override List<DictStrStr>  PreProcessList(List<DictStrStr> sourceRecords){

        if(__repoSource == "local")
        {
            return sourceRecords;
        }
        else if(__repoSource == "github")
        {
            var navigatorObject = GameObject.Find("Navigator");
            RepoData repoData = navigatorObject.GetComponent<RepoData>();
             List<DictStrStr> alreadyDownloadedRecords  = repoData.ListFullRecords();

            List<DictStrStr> neededRecords = new List<DictStrStr>();
            HashSet<string> downloadedNames = new HashSet<string>();
            foreach (var record in alreadyDownloadedRecords)
            {
                if (record.ContainsKey("name"))
                {
                    downloadedNames.Add(record["name"]);
                }
            }

            foreach (var record in sourceRecords)
            {
                if (record.ContainsKey("name") && !downloadedNames.Contains(record["name"]))
                {
                    neededRecords.Add(record);
                }
            }

            return neededRecords;
         }    
         return null;    
    }


    protected override VisualElement AddToList(DictStrStr rec)
    {
        // Create a new instance of the RepoListItem template
        var repoListItem = new VisualElement();
        if(__repoSource == "local")
        {
            var repoListItemTemplate = Resources.Load<VisualTreeAsset>("Controls/RepoListItem/RepoListItem");
            repoListItemTemplate.CloneTree(repoListItem);
            repoListItem.style.height = 40; // TODO TOTAL HACK. 
            repoListItem.Q<Label>("repo_name").text = rec["name"];
            repoListItem.Q<Label>("repo_status").text = rec["status"];
            repoListItem.Q<Label>("repo_branch").text = rec["branch"];
        }
        if(__repoSource == "github")
        {
            var repoListItemTemplate = Resources.Load<VisualTreeAsset>("Controls/RepoListItem/RepoListItemSelect");
            repoListItemTemplate.CloneTree(repoListItem);
            repoListItem.style.height = 40; // TODO TOTAL HACK. 
            repoListItem.Q<Label>("repo_name").text = rec["name"];
            repoListItem.Q<Label>("repo_branch").text = rec["branch"];
        }

        // Add the new item to the repo list container
        listItemContainer.Add(repoListItem);

        var navigatorObject = GameObject.Find("Navigator"); //TODO ew so wasteful. HACK
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();

        //repoListContainer.Add(repoListItem);
        // Register click event for highlighting
        repoListItem.RegisterCallback<ClickEvent>(evt => OnRepoItemClick(repoListItem, rec["name"]));
        return repoListItem;
    }


    protected override IEnumerable<Dictionary<string, object>> GenerateNavigationActions()
    {
        if (__repoSource == "local")
        {
                yield return new Dictionary<string, object> {
                { "buttonName", "Add" },
                { "destinationScreen", "EditRepoScreen" },
                { "passRecord", false }
            };
                yield return new Dictionary<string, object> {
                { "buttonName", "Edit" },
                { "destinationScreen", "EditRepoScreen" },
                { "passRecord", true }
            };
                yield return new Dictionary<string, object> {
                { "buttonName", "View" },
                { "destinationScreen", "RepoInfoScreen" },
                { "passRecord", true }
            };
                yield return new Dictionary<string, object> {
                { "buttonName", "Visit" },
                { "destinationScreen", "RepoInfoScreen" },
                { "passRecord", true }
            };
        }
        if (__repoSource == "github")
        {
                yield return new Dictionary<string, object> {
                { "buttonName", "Cancel" },
                { "destinationScreen", "RepoListScreen" },
                { "passRecord", false }
            };
        }        
    }

    void OnEnable()
    {
        RegisterStandardEvents();
        RegisterActionButtons();
    }
    void RegisterActionButtons() {
        /// Assign NavigationManage
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        var root = GetComponent<UIDocument>().rootVisualElement;
        Button navigateElement;

        if (__repoSource == "local")
        {
            // Register Delete
            navigateElement = root.Q<Button>("Delete");
            navigateElement.RegisterCallback<ClickEvent>(DeleteConfirm);

            // Register GH List
            navigateElement = root.Q<Button>("GHList");
            navigateElement.RegisterCallback<ClickEvent>(GithubListPopup);
        }

        if (__repoSource == "github")
        {
            // Register Delete
            navigateElement = root.Q<Button>("Download");
            navigateElement.RegisterCallback<ClickEvent>(DownloadConfirm);
        }

    }

    void DeleteConfirm(ClickEvent evt)
    {
        void DoDelete()
        {
            Debug.Log($"Doing Delete {GetHighlightedItem()}");
            datasource.DeleteRecord(GetHighlightedItem());
            //LoadDatasource();
        }

        void DoCancel()
        {
            Debug.Log("Doing Cancel");
        }
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        navigationManager.NotifyConfirm("Are you sure you want to delete?", DoDelete, DoCancel);
    }

    void DownloadConfirm(ClickEvent evt)
    {
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        string downloadString = "";
        List<string> selectedRepos = GetHighlightedItems();
        foreach(string item in selectedRepos)
        {
            //Debug.Log($"building with {item}");
            downloadString +=  $",{item}";
        }

        async void DoDownload()
        {
            Debug.Log($"!******Doing Download of  {downloadString}");
            ((RepoGithubData)datasource).DownloadAll(selectedRepos);            
            //Debug.Log($"Finished Download of  {downloadString}");
            navigationManager.NavigateTo("RepoListScreen");
        }

        void DoCancel()
        {
            Debug.Log("Doing Cancel");
        }
        navigationManager.NotifyConfirm($"Are you sure you want to download? {downloadString}", DoDownload, DoCancel);
    }


    void GithubListPopup(ClickEvent evt)
    {
        // Show Github Popup and define buttons
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        bool hideAll = false;
        navigationManager.NavigateTo("RepoListPopup",hideAll); 
    }

    //  NavigationManager.ICanInitalize
    public bool SetAction(string actionLabel, System.Action action)
    {
        return false;
    }


    public void InitData(Dictionary<string, string> dataframe)
    {
        //throw new System.NotImplementedException();
    }

    public void Refresh()
    {
        LoadDatasource();
        //throw new System.NotImplementedException();
    }

}


