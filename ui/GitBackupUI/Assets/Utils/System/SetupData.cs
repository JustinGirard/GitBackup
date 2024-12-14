using System.Collections.Generic;
using UnityEngine;
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictObjTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using System.IO;
using  System.Threading.Tasks;
using System;

using UnityEngine.Assertions;
using UnityEngine.SocialPlatforms.Impl;
using UnityEditor;
using System.Runtime.ExceptionServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;


public class SetupData : StandardData
{
    ProfileData profileDatasource ; 
    string __latestPythonRoot;
    string __latestPythonWorkDir;
    Dictionary<string,string> __stages = new Dictionary<string,string>{
                    {"Python","download_python_miniconda" },
                    {"IPFS","download_ipfs"},
                    {"Wallet","download_repo"},
                    {"Propagator","download_repo"},
                    {"Venv","setup_virtual_env"}
                    };           
        

    DictStrObj __mapStageToTargetDir = new DictStrObj {
        {"Python","python_install_dir"},
        {"IPFS","ipfs_install_dir"},
        {"setup_venv","venv_path"},
        {"Wallet","decelium_wallet_dir"},
        {"Propagator","propagator_dir"},
        {"Venv","venv_path"},
        };

    public List<string> GetStages(){
        return new List<string>(__stages.Keys);
    }
    
    void Awake()
    {
        GameObject obj = GameObject.Find("Navigator");
        profileDatasource = obj.GetComponent<ProfileData>();
        if (profileDatasource == null)
            throw new System.Exception("No Profile Datasource");
        foreach(string stageId in GetStages())
        {
            AddStage(name:stageId,scriptId:stageId);
            //RunStage(stageId:stageId,action:"verify", onSuccess:null, onFailure:null);            
        }
   }
    void Start()
    {
        ApplicationState.Instance().RegisterChangeCallback("selected_profile",(string key,object profileString) => {
            foreach(string stageId in GetStages())
            {
                RunStage(stageId:stageId,action:"verify", onSuccess:null, onFailure:null);            
            }
        });        
 
    }
    public bool IsPythonReady()
    {
        if (!ContainsKey("Venv"))
            return false;

        if (GetRecordField("Venv","status") == "True")
            return true;
        //Debug.Log($"VENV Status: {GetRecordField("Venv","status")}"); 
        return false;
    }
    public string GetPythonRoot()
    {
        return "/Users/computercomputer/justinops/propagator/propenv/bin/python3";
        return __latestPythonRoot;
    }
    public string GetPythonWorkDir()
    {
        return "/Users/computercomputer/justinops/";
        return __latestPythonWorkDir;
    }


    public string GetTargetDir(string stageId,DictStrObj rec){


        if(!  __mapStageToTargetDir.ContainsKey(stageId))
            return null;
        string recKey = (string)__mapStageToTargetDir[stageId];
        if(!  rec.ContainsKey(recKey))
            return null;

        return (string)rec[recKey];
        
    }
    public void RunStage(string stageId,string action, System.Action<object> onSuccess, System.Action<object> onFailure,bool debugMode = false)
    {
        string currentUser = (string)ApplicationState.Instance().Get("selected_profile");
        if (string.IsNullOrEmpty(currentUser))
        {
            Debug.Log("RunStage failed: No User is present.");
            if (onFailure!= null)
                onFailure("No User Present");
            return;
        }
        DictStrObj rec = profileDatasource.GetRecord(currentUser);
         
        if (! GetStages().Contains(stageId))
        {
            Debug.Log($"RunStage failed: Invalid stage selected {stageId}");
            if (onFailure!= null)
                onFailure($"Invalid stage selected {stageId}");
            return;
        }
        string [] requiredFields = new string [] {
            "name",
            "path",
            "username",
            "encryption_password",
            "access_key",
            ///
            "python_install_dir",
            "ipfs_install_dir",
            "venv_path",
            "git_install_dir",
            "decelium_wallet_url",
            "decelium_wallet_dir",
            "propagator_url",
            "propagator_dir",
            };
        foreach (string reqField in requiredFields )
        {
            if(!rec.ContainsKey(reqField))
            {
                Debug.Log($"Could not load required field {reqField} from user profile");
                if (onFailure!= null)
                    onFailure($"Could not load required field {reqField} from user profile");
                return;
            }
        }

        // TODO string pythonPath = rec["python_install_dir"] +"/v1/bin/python3";
        // Focus on using the user directory as the working dir
        string workingDir = System.IO.Path.Combine(Application.persistentDataPath , "SetupData",(string) rec["name"]);
        // Get ref to micropython for low level commands
        string microPythonPath = System.IO.Path.Combine(Application.streamingAssetsPath, "micropython/bin/micropython");
        // Get ref to minipython for regular commands



        string miniPythonPath =System.IO.Path.Combine(workingDir,(string)rec["python_install_dir"],"v1/bin/python3");
        string venvPythonPath =System.IO.Path.Combine(workingDir,(string)rec["venv_path"],"bin/python3");
        __latestPythonRoot = venvPythonPath;
        __latestPythonWorkDir = workingDir;
        // Get ref the setup script
        string scriptPath = System.IO.Path.Combine(Application.dataPath, "Utils", "System","SetupManager.py");

        if (!System.IO.Directory.Exists(workingDir))
        {
            System.IO.Directory.CreateDirectory(workingDir);
        }        
        string pythonPath ="";
        string stageScriptId = __stages[stageId];
        string targetDir = GetTargetDir(stageId,rec);

        if (stageScriptId == "download_python_miniconda")
            pythonPath = microPythonPath;
        else
            pythonPath = miniPythonPath;

        if (stageScriptId == "download_python_miniconda")
            scriptPath =  System.IO.Path.Combine(Application.dataPath, "Utils", "PythonShim.py");
        if (targetDir == null)
        {
            Debug.Log($"Faild to look up  targetDir for console command {stageScriptId}");
            return;
        }

        DictStrObj args = new DictStrObj {
                {"id",stageScriptId},
                {"mode",action},
                {"target_directory",targetDir}};      
        // TODO Refactor into setting 
        if (stageScriptId == "download_repo" && stageId=="Wallet")
        {
            args["target_repo"] =rec["decelium_wallet_url"];
            args["target_branch"] ="master";
        }
        if (stageScriptId == "download_repo" && stageId=="Propagator")
        {
            args["target_repo"] =rec["propagator_url"];
            args["target_branch"] ="master";
        }
        if (stageScriptId == "setup_virtual_env" )
        {
            args["target_python3_file"] =miniPythonPath;
        }


        Func<Task> tsk = JobUtils.CreateJobifiedShellTask(
            jobName: $"{action}_{stageId}",
            command: new string[] { pythonPath,scriptPath,"stage" },
            arguments: args,
            isNamedArguments:true,
            workingDirectory: workingDir,
            onProgress: (obj) =>{
                //Debug.Log(obj);
                //Debug.LogError($"CORRECTLY Reading progress from {action}_{stageId}");
                
            },
            readProgress:() =>{
                    DictStrObj progressArgs = new DictStrObj(args);
                    progressArgs["mode"]="progress";
                    object progressData =null;
                    Func<Task> tsk = JobUtils.CreateShellTask(
                            command:new string[] { pythonPath,scriptPath,"stage" },
                            arguments:progressArgs,
                            isNamedArguments:true,
                            workingDir,
                            onSuccess = (object obj) =>{
                                progressData = obj;
                            },
                            onFailure= (object obj) =>{
                                progressData = obj;
                            },
                            debugMode);
                    //Task.Run(tsk);
                    Task.Run(tsk).GetAwaiter().GetResult();                    
                    if (progressData == null)
                        throw new System.Exception("Encountered an error running a progress monitor");
                    return progressData;
            },            
            onSuccess: (obj) =>{
                // Debug.Log($"Ran a setup command{stageId}, got resut"+obj.ToString());
                if (action == "execute")
                {
                    RunStage(stageId:stageId, action:"verify", onSuccess:onSuccess,onFailure:onFailure,debugMode:debugMode);
                    return;
                }
                if (action != "verify")
                {
                    Debug.Log("In Run Stage with invalid action");
                    return;
                }

                //Dictionary<string,object> jsonObject = JsonConvert.DeserializeObject<JObject>((string)obj) as Dictionary<string,object> ;
                Dictionary<string,object> results = DJson.Parse((string)obj);
                try
                {
                    bool fresh_status = (bool)results["status"];
                    SetRecordField(stageId,"status",fresh_status.ToString());
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    timestamp = timestamp + $"{stageId} as {results["status"]}";
                    bool triggeredEvent = ApplicationState.Instance().Set("system_settings_updated",timestamp);
                    if (triggeredEvent ==false)
                        Debug.LogError($"Could not trigger an event after status update for {stageId} ");

                }
                catch (System.Exception ex)
                {
                    SetRecordField(stageId,"status","False");
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    timestamp = timestamp + $"{stageId} as {false}";
                    ApplicationState.Instance().Set("system_settings_updated",timestamp);
                    //Debug.LogError("Encountered an Exception while running RunStage "+ex.ToString());
                    throw;
                }

                if (onSuccess!= null)
                    onSuccess(obj);
            },
            
            onFailure: (error) =>
            {
                string theFailMessage = $"Failed to Run Stage:"+((System.Exception)error).ToString();
                theFailMessage = theFailMessage + $"\n For command: [{stageId}, {action}]"; 
                Debug.Log(theFailMessage);
                Debug.Log(pythonPath);
                Debug.Log(scriptPath);
                SetRecordField(stageId,"status","False");

                if (onFailure!= null)
                    onFailure(theFailMessage);
            },
            debugMode:debugMode,
            parentId:"check_repo"
        );
        Task.Run(tsk);

    }



    private void UpdateStatusLabel()
    {
        int uncategorizedCount = 0;
        int falseCount = 0;
        int trueCount = 0;
        int unknownCount = 0;
        int totalCount = GetStages().Count;
        string missingLabel = "";
        foreach (string stageId in GetStages())
        {
            string fieldStatus = (string)GetRecordField(stageId,"status");
            if (fieldStatus == "UNKNOWN") unknownCount = unknownCount + 1;
            else if (fieldStatus == "True") trueCount = trueCount + 1;
            else if (fieldStatus == "False") falseCount = falseCount + 1;
            else if (fieldStatus == "UNKNOWN") unknownCount = unknownCount + 1;
            else 
            {   
                uncategorizedCount = uncategorizedCount+1;
                missingLabel = fieldStatus;
            }
        }
        string new_status = "";
        if (missingLabel != "")
            new_status= $"MISSING: {missingLabel} ";
        else if (unknownCount > 0 && trueCount > 0 )
            new_status= $"{unknownCount}/{totalCount} unknown, {trueCount}/{totalCount} installed  ";
        else if (falseCount > 0)
            new_status= $"{falseCount} uninstalled ";
        else if (unknownCount > 0)
            new_status= $"{unknownCount} unknown ";
        else if (trueCount > 0)
            new_status= $"{trueCount}/{totalCount} installed";
        else 
            new_status= "failed to update";
        SetStatusLabel(new_status);
    }
    public bool AddStage(string name, 
                          string scriptId)
    {
        if (string.IsNullOrEmpty(name))
            throw new System.Exception("Null is Name");
        if (string.IsNullOrEmpty(scriptId))
            throw new System.Exception("Null is scriptId");
        SetRecord(new DictStrObj
        {
            { "name", name },
            { "script_id", scriptId },
            { "status", "UNKNOWN" },
        });
        return true;
    }
    
    public override void AfterSaveData()
    {
        UpdateStatusLabel();
    }
    public override void BeforeLoadData()
    {

    }    
}

