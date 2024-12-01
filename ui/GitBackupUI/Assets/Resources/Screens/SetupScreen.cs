using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine;
/*
UXML

*/
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using System.Collections.Generic;


public class SetupScreen : StandardListScreen, NavigationManager.ICanInitalize
{

     void Start()
     {
        var navigatorObject = GameObject.Find("Navigator");
        datasource = navigatorObject.GetComponent<SetupData>();        
        listContainerId = "stage_list_items";
        BaseStart();
        if (datasource == null)
        {
            Debug.LogError("OnEnable: No SetupData object found on the GameObject.");
            return;
        }
    }
    // NavigationManager.ICanInitalize
    public void InitData(DictStrObj dataframe){}
    // NavigationManager.ICanInitalize
    public bool SetAction(string actionLabel, System.Action action ){  return false;}
    void Update()
    {
        BaseUpdate();
    }

    void OnEnable()
    {
        RegisterEvents();
        RegisterStandardEvents();
    }

    void RegisterEvents()
    {

    }

    protected override IEnumerable<Dictionary<string, object>> GenerateNavigationActions()
    {
            yield return new Dictionary<string, object> {
            { "buttonName", "Continue" },
            { "destinationScreen", "RepoListScreen" },
            { "passRecord", false }
        };
    }


    protected override VisualElement AddToList(DictStrObj rec)
    {
        // Create a new instance of the RepoListItem template
        var repoListItem = new VisualElement();
        var repoListItemTemplate = Resources.Load<VisualTreeAsset>("Controls/RepoListItem/SetupStageBar");
        repoListItemTemplate.CloneTree(repoListItem);
        repoListItem.style.height = 40; // TODO TOTAL HACK. 
        // Set the repository attributes (repo_name, repo_status, repo_branch)
        repoListItem.Q<Label>("stage_name").text = (string)rec["name"];
        repoListItem.Q<Label>("stage_status").text = (string)rec["status"];
        // TODO Attache EVents
        repoListItem.Q<Button>("Verify").RegisterCallback<ClickEvent>(evt => {
            ((SetupData)datasource).RunStage( 
                                        stageId:(string)rec["name"],
                                        action:"verify", 
                                        onSuccess:(obj) => {Debug.Log("VERIFY SUCCEEDED: "+((string)obj));}, 
                                        onFailure:(err) => {Debug.Log("VERIFY FAILED");}
                                        );
        });
        repoListItem.Q<Button>("Run").RegisterCallback<ClickEvent>(evt => {
            ((SetupData)datasource).RunStage( 
                                        stageId:(string)rec["name"],
                                        action:"execute", 
                                        onSuccess:(obj) => {Debug.Log("SUCCEEDED: "+((string)obj));}, 
                                        onFailure:(err) => {Debug.Log("FAILED");},
                                        debugMode:false
                                        );
        });

        var navigatorObject = GameObject.Find("Navigator"); //TODO ew so wasteful. HACK
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        listItemContainer.Add(repoListItem);
        // repoListContainer.Add(repoListItem);
        // Register click event for highlighting
        return repoListItem;

        // RunStage(string stageId,string action, System.Action<object> onSuccess, System.Action<object> onFailure)
    }


}
