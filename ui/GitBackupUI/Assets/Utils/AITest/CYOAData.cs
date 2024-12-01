using System.Collections.Generic;
using UnityEngine;
using DictObjTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using DictStrObj =  System.Collections.Generic.Dictionary<string, object>;

using System.IO;
using System.Threading;
using Task = System.Threading.Tasks.Task;

using System.Collections.Concurrent;
using UnityEngine.TerrainUtils;
using System;
using Unity.VisualScripting.Dependencies.NCalc;
public class GraphManagerService
{
    private static SetupData __setupData;
    private static ApplicationState __appState;
    private static string __venvPythonPath;
    private static string __workingDir;
    private static string cmd_path = "/Users/computercomputer/justinops/art/apps/cyoa/graph_manager.py";


    private static readonly Dictionary<string, List<string>> __commandMap = new Dictionary<string, List<string>>()
    {
        { "create_graph", new List<string> { "base_directory", "graph_id", "name" } },
        { "add", new List<string> { "base_directory", "data" } },
        { "remove", new List<string> { "base_directory", "self_id" } },
        { "get", new List<string> { "base_directory", "self_id" } },
        { "add_group_type", new List<string> { "base_directory", "group_type_id", "name" } },
        { "add_node_type", new List<string> { "base_directory", "node_type_id", "name" } }
    };

    public GraphManagerService(GameObject appContainer)
    {
        // Connect to settings
        //Debug.Log("Trying to load graph element");
        //var appContainer = GameObject.Find("Navigator");
        if (__appState == null)
            __appState = appContainer.GetComponent<ApplicationState>();
        if (__appState == null)
            throw new System.Exception("Should have ab App State avail");
        
        if (__setupData == null)
            __setupData = appContainer.GetComponent<SetupData>();
        if (__setupData == null)
            throw new System.Exception("Should have SetupData enabled");
        
        if (__venvPythonPath == null)
            __venvPythonPath = __setupData.GetPythonRoot();
        if (__venvPythonPath == null)
            throw new System.Exception("Should have GetPythonRoot() result");

        if (__workingDir == null)
        {
            // TODO use setup data for each app. SetupData should have a headless mode.
            //__workingDir = __setupData.GetPythonWorkDir();
            __workingDir = "/Users/computercomputer/justinops/art/apps/cyoa";
        }
        if (__workingDir == null)
            throw new System.Exception("Should have GetPythonWorkDir() result");

        //  python3 graph_manager.py add base_directory="./test_graph" data='{"node_id": "n1",  "data": "some/path","attached_groups": []}'
    }

    public Func<Task> RunCommand(string cmd,DictStrObj arguments, System.Action<object> onSuccess, System.Action<object> onFailure, bool debugMode = false )
    {
        System.Func<Task>  tsk = JobUtils.CreateShellTask(
            command: new string[] { __venvPythonPath,cmd_path,cmd },
            arguments: arguments,
            isNamedArguments:true,
            workingDirectory: __workingDir,
            onSuccess:(object rawJson) => {
                onSuccess(rawJson);
            },
            onFailure:(object error) => {
                onFailure(error);
            },
            debugMode:debugMode);
        //Task.Run(tsk); // ASYNC
        //Task.Run(tsk).GetAwaiter().GetResult(); // SYNC    
        return tsk;
    }

    public Func<Task> Query(
        string command,
        DictStrObj arguments,
        System.Action<object> onSuccess,
        System.Action<object> onFailure,
        bool debugMode = false)
    {
        if (!__commandMap.ContainsKey(command))
            throw new ArgumentException($"Unknown command: {command}");

        // Validate arguments
        var requiredArgs = __commandMap[command];
        foreach (var requiredArg in requiredArgs)
        {
            if (!arguments.ContainsKey(requiredArg))
            {
                throw new ArgumentException($"Missing required argument: {requiredArg} for command: {command}");
            }
        }
        return RunCommand(command, arguments, onSuccess, onFailure, debugMode);
    }    



}
public class CYOAData : StandardData
{
    private string filePath;
    private DictObjTable navMap;
    private string currentLocation = "UNKNOWN";
    void Awake()
    {
        // filePath = Application.persistentDataPath + "/profiles.json";
        //var data = FileUtils.SafeReadJsonFile(filePath);
        //if (data != null )
        //    SetRecords((DictTable)data);
        // 1. Load Location Template
        navMap = new DictObjTable();
    }
    void Start()
    {
        InitLocations();
    }
    public (DictStrObj,DictStrObj) GenerateLocation()
    {
        DictStrObj location;
        DictStrObj navigation;

        location = new DictStrObj
        {
            { "LocationID", "Relay" },
            { "AreaTitle", "Charleston Relay" },
            { "Description", "A lone communications relay, framed in a gaseos nebula." },
            { "AreaImage", "DefaultAreaImage" },
            { "Actions", ">Open the panel." }
        };
        navigation = new DictStrObj{
            {"Pass though the nebula","ZalarSystem"},
            {"Return to open space","OmegaStation"}
        };        

        return (location, navigation);
    }
    public void InitLocations()
    {
        var navigatorObject = GameObject.Find("Navigator");

        GraphManagerService graphManager = new GraphManagerService(navigatorObject);
 
        /*var createGraphTask = graphManager.Query(
            command: "create_graph",
            arguments: new DictStrStr
            {
                { "base_directory", "/path/to/graph" },
                { "graph_id", "g1" },
                { "name", "Example Graph" }
            },
            onSuccess: (result) => Debug.Log($"Graph created: {result}"),
            onFailure: (error) => Debug.LogError($"Error creating graph: {error}")
        );
        Task.Run(createGraphTask).GetAwaiter().GetResult(); // SYNC    
        */
        string loadedGraph="";
        Func<Task> getTask = graphManager.Query(
            command: "get",
            arguments: new DictStrObj
            {
                { "base_directory", "./spacegraph/graph" },
                { "self_id", "root" }
            },
            onSuccess: (object result) =>
            {
                loadedGraph = (string)result;
                Debug.Log($"Graph Manager Query Success: {loadedGraph}");
            },
            onFailure: (object error) =>
            {
                throw new Exception($"Query failed: {error}");
            },
            debugMode:false
        );
        Task.Run(getTask).GetAwaiter().GetResult(); // SYNC    
        DictStrObj rawNode = DJson.Parse(loadedGraph);
        DictStrObj locationAsObj =   (DictStrObj)((( DictStrObj)rawNode["data"])["location"]);
        DictStrObj locationStrStr = locationAsObj;

        currentLocation = (string)locationStrStr["LocationID"];

         //Task.Run(createGraphTask);
         /*
        SetRecord(new Dictionary<string, string>
        {
            { "LocationID", "MysteriousForest" },
            { "AreaTitle", "Mysterious Forest" },
            { "Description", "You find yourself in a dense forest..." },
            { "AreaImage", "DefaultAreaImage" },
            { "Actions", ">Open the panel." }
        },keyfield:"LocationID");
        navMap["MysteriousForest"] = new DictStrStr{
            {"Go north","NorthernForest"},
            {"Climb the ladder","Treehouse"}
            };
        */
        // Record for NorthernForest
        SetRecord(locationStrStr,keyfield:"LocationID");

        navMap[currentLocation] = new DictStrObj{
            {"Go south","MysteriousForest"}
            };
        SetCurrentLocation(currentLocation);
    }
    public DictStrObj GetCurrentLocation(){
        return GetRecord(currentLocation);
    }
    public DictStrObj GetCurrentNavigationChoices(){

        if (navMap.ContainsKey(currentLocation) == false)
            return null;
        return navMap[currentLocation];
    }

    public bool SetCurrentLocation(string newLocal)
    {
        if ( GetRecord(newLocal) == null )
        {
            Debug.Log($"Cant navigate to {newLocal}");
            return false;
        }
        currentLocation = newLocal;
        UpdateDataRevision(5);
        return true;
    }
    public override void AfterSaveData()
    {
        //FileUtils.SafeWriteJsonFile(filePath,GetRecords());

    }
    public override void BeforeLoadData()
    {

    }    
}

