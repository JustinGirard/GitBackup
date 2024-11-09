using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

public class StandardListScreen : MonoBehaviour
{

    protected StandardData datasource;  // Use the StandardData interface for the RepoData class
    protected List<string> highlightedListItems = new List<string> ();
    public bool highlightSingle = true; 
    protected VisualElement listItemContainer;
    protected string listContainerId = "";
    public string __selectedCoundId = "SelectedCount";        




    // Cr
    protected void BaseStart()
    {
        LoadDatasource();               // Load and display repos in the UI
    }
    protected void BaseUpdate()
    {
        LoadDatasource();
        UpdateStatusBar();
    }



    protected virtual IEnumerable<Dictionary<string, object>> GenerateNavigationActions()
    {
        throw new System.Exception("Unimplemented navigation actions");
        // Return a list of actions. Actions have buttonName,destinationScreen,passRecord
        yield return new Dictionary<string, object> {
            { "buttonName", "Visit" },
            { "destinationScreen", "RepoInfoScreen" },
            { "passRecord", true }
        };
        /*
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
        };*/
    }

    ///
    /// ///
    /// 
    /// 
    /// 
    /// 
    /// 
    /// <summary>
    /// 
    ///  Inline status bar. TODO refactor into new class in a future project.
    /// </summary>
    /// <exception cref="System.Exception"></exception>
    

    private DictStrStr __guiIDToDataID = new DictStrStr() {
        {"ReposLabel","repo_data"},
        {"SetupLabel","setup_data"},
        {"ConnectionLabel","repogithub_data"},
        {"DownloadingLabel","job_data"},
        //{"ProfileLabel","profile_data"}
    };
    
    private VisualElement _visualElement_statusRoot = null;
    private float _statusBar_needsRefresh = 0;
    private Dictionary<string,StandardData> __dataModules = new Dictionary<string, StandardData>();
    private VisualElement GetStatusBar()
    {
        if (_visualElement_statusRoot == null)
        {
            var navigatorObject = GameObject.Find("Navigator");
            var navigationManager = navigatorObject.GetComponent<NavigationManager>();
            var root = GetComponent<UIDocument>().rootVisualElement;

            __dataModules["setup_data"] = navigatorObject.GetComponent<SetupData>();
            __dataModules["profile_data"] = navigatorObject.GetComponent<ProfileData>();
            __dataModules["job_data"] = navigatorObject.GetComponent<JobData>();
            __dataModules["repo_data"] = navigatorObject.GetComponent<RepoData>();
            __dataModules["repogithub_data"] = navigatorObject.GetComponent<RepoGithubData>();

            // Add Status Bar Control
            var statusBarName = "StatusBar";
            if (statusBarName.Length == 0)
                throw new System.Exception("StatusBarName was empty");
            var barElement = root.Q<VisualElement>(statusBarName);
            _visualElement_statusRoot = barElement;
        }
        return _visualElement_statusRoot;
    }

    protected void RegisterStandardEvents()
    {
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        var root = GetComponent<UIDocument>().rootVisualElement;

        foreach (var action in GenerateNavigationActions())
        {
            if (action != null)
            {
                var buttonName = (string)action["buttonName"];
                if (buttonName.Length == 0)
                    throw new System.Exception("buttonName was empty");
                var navigateElement = root.Q<Button>(buttonName);
                if (navigateElement == null)
                    throw new System.Exception("navigateElement was empty");

                var destinationScreen = (string)action["destinationScreen"];
                if (destinationScreen.Length == 0)
                    throw new System.Exception("destinationScreen was empty");
                bool passRecord = (bool)action["passRecord"];


                if (passRecord)
                    navigateElement.RegisterCallback<ClickEvent>(evt => NavigateToWithRecord(evt, destinationScreen));
                else
                    navigateElement.RegisterCallback<ClickEvent>(evt => NavigateToWithBlankRecord(evt, destinationScreen));
            }
        }
        RegisterStatusBarEvents();
    }

    protected void RegisterStatusBarEvents()
    {
        /*
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Add Status Bar Control
        var statusBarName = "StatusBar";
        if (statusBarName.Length == 0)
            throw new System.Exception("StatusBarName was empty");
        var barElement = root.Q<VisualElement>(statusBarName);*/
        


        var barElement = GetStatusBar();
        if (barElement != null)
        {
            var navigateElement = barElement.Q<Button>("JobsButton");
            if (navigateElement == null)
                throw new System.Exception("Could not find Jobs button");
            navigateElement.RegisterCallback<ClickEvent>(evt => NavigateTo("JobListScreen",hideAll:false));
            barElement.Q<Button>("DownloadsButton").RegisterCallback<ClickEvent>(evt => NavigateTo("JobListScreen",hideAll:false));
        }
    }
    public string GetLoadingSuffix()
    {

        float secondsSinceLastMinute = Time.time % 60f;
        float twoSecondTimer = (secondsSinceLastMinute % 1.5f)/1.5f;
        var suffix = "";
        if ( twoSecondTimer  < 1.0f ) suffix = "...";
        if ( twoSecondTimer  < 0.7f ) suffix = "..";
        if ( twoSecondTimer  < 0.5f ) suffix = ".";
        if ( twoSecondTimer  < 0.2f ) suffix = "";
        return suffix;
    }
    protected string GetLabel(string guiLabelId)
    {
         if (__guiIDToDataID.ContainsKey(guiLabelId) == false)
            throw new System.Exception($"Missing GUI label {guiLabelId}");
        return __guiIDToDataID[guiLabelId];
    }    
    protected StandardData GetGUIStatus(string guiLabelId)
    {
       string dataModelId =  GetLabel(guiLabelId);
        return __dataModules[dataModelId];
    }
    protected void UpdateStatusBar()
    {
        if (_statusBar_needsRefresh <= 1)
            _statusBar_needsRefresh = Time.time;

        if ( Time.time > _statusBar_needsRefresh)
            return;
        VisualElement  barElement = GetStatusBar();
        if (barElement == null)
            return; // No status bar
        string suffix = GetLoadingSuffix(); 

        List<string> statusLabels = new List<string>(__guiIDToDataID.Keys);
        
        foreach (string labelId in statusLabels)
        {
            StandardData standardData = GetGUIStatus( labelId);
            barElement.Q<Label>(labelId).text = $"{GetLabel(labelId)}: {standardData.GetStatusLabel()} {suffix}";
        }

        _statusBar_needsRefresh = Time.time + 10000f;
    }

    protected void NavigateToWithRecord(ClickEvent evt, string targetScreen)
    {
        if (targetScreen.Length == 0)
            throw new System.Exception("No Target Screen selected");

        // TODO Generalize NavigateToWithRecord and NavigateToWithBlankRecord
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();

        DictStrStr argument = new DictStrStr { { "name", highlightedListItems[0] } };
        navigationManager.NavigateToWithRecord(targetScreen, argument);
    }

    protected void NavigateToWithBlankRecord(ClickEvent evt, string targetScreen)
    {
        if (targetScreen.Length == 0)
            throw new System.Exception("No Target Screen selected");

        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();

        DictStrStr argument = new DictStrStr { };
        navigationManager.NavigateToWithRecord(targetScreen, argument);
    }


    public void NavigateTo(string targetScreen, bool hideAll = true)
    {
        if (targetScreen.Length == 0)
            throw new System.Exception("No Target Screen selected");

        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        navigationManager.NavigateTo(targetScreen, hideAll);
    }

  
    private int __dataRevision = 0;

    protected virtual List<DictStrStr>  PreProcessList(List<DictStrStr> records){
        return records;
    }
    // Method to load repositories and display them in the list
    public void LoadDatasource()
    {
        if (datasource == null)
        {
            Debug.LogError($"{this.name}: Missing Required Datasource");
            return;
        }
        int dataRevision = datasource.GetDataRevision();
        if (__dataRevision == dataRevision)
        {
            return;
        }
        __dataRevision = dataRevision;
        // Clear the list of repository items
        listItemContainer = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>(listContainerId);
        listItemContainer.Clear();


        List<DictStrStr> repoList = datasource.ListFullRecords();
        repoList = PreProcessList(repoList);

        foreach (var repoData in repoList)
        {
            string repoName = repoData["name"];
            VisualElement repoListItem = AddToList(repoData);
            foreach (string highlightedListItem in highlightedListItems)
            {
                if (highlightedListItem == repoName)
                {
                    OnRepoItemClick(repoListItem, repoName);
                }
            }
        }
    }

    // Method to add a repository entry to the list using the RepoListItem template
    protected virtual VisualElement AddToList(DictStrStr rec)
    {
        throw new  System.Exception("Not implemented. See an example implementation");
        return new VisualElement();
    }

    // Method to handle the click event and highlight the selected repo
    protected void OnRepoItemClick(VisualElement clickedRepoListItem, string repoName)
    {
        // De-select old ones
        if (highlightSingle == true)
        {
            foreach (var repoListItem in listItemContainer.Children())
            {
                repoListItem.RemoveFromClassList("highlighted_ve");
            }
        }


        // If single: Purge list
        if (highlightSingle == true)
        {
            highlightedListItems = new List<string>();
        }

        // Always: Toggle current selection
        if (clickedRepoListItem.ClassListContains("highlighted_ve"))
        {
            if (highlightedListItems.Contains(repoName))
                highlightedListItems.Remove(repoName);
            if (!highlightedListItems.Contains(repoName))
                clickedRepoListItem.RemoveFromClassList("highlighted_ve");
        }
        else
        {
            if (!highlightedListItems.Contains(repoName))
                highlightedListItems.Add(repoName);
            if (highlightedListItems.Contains(repoName))
                clickedRepoListItem.AddToClassList("highlighted_ve");
        }

        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        var root = GetComponent<UIDocument>().rootVisualElement;
        Label selectedCountElement;
        selectedCountElement = root.Q<Label>(this.__selectedCoundId);
        if (selectedCountElement != null)
            selectedCountElement.text = $"{highlightedListItems.Count.ToString()} Selected";        
        
    }

    public string GetHighlightedItem()
    {
        if (highlightSingle == false)
            throw new System.Exception("This is a milti-select. There is no single item to draw");
        return highlightedListItems[0];

    }
    public List<string> GetHighlightedItems()
    {
        return highlightedListItems;
    }
}
