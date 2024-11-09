using System.Collections.Generic;
using UnityEngine;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;
// TODO kill these stupid data classes. They dont really help.
class RecordRepoReference
{
    public string name;
    public RecordRepoReference(DictStrStr record)
    {
        name = record["name"];
    }

    public DictStrStr ToDictRecord()
    {
        DictStrStr d = new DictStrStr { { "name", name } };
        return d;
    }

}
class RecordRepoFull
{
    public string name;
    public string url;
    public string branch;
    public string status;
    public string last_updated;
    public string username;

    public RecordRepoFull(DictStrStr record)
    {
        name = record["name"];
        url = record["url"];
        branch = record["branch"];
        status = record["status"];
        last_updated = record["last_updated"];
        //username = record["username"];
    }

    public DictStrStr ToDictRecord()
    {
        DictStrStr d = new DictStrStr {
            { "name", name },
            { "url", url },
            { "branch", branch },
            { "status", status },
            { "last_updated", last_updated },
        };
        return d;
    }

}


public class RepoData : StandardData
{
    ProfileData profileDatasource;  // Use the IRepoData interface for the RepoData class
    void Awake()
    {
        void OnValueChanged(object newValue)
        {
            ReloadData();
        }
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        appState.RegisterChangeCallback("selected_profile", OnValueChanged);
        appState.RegisterChangeCallback("system_settings_updated", OnValueChanged);
        SetStatusLabel($"no attached user");            

        // Setting the value, which triggers the callback if the value changes
        //appState.Set("playerScore", 100);

        // Unregistering the callback when it is no longer needed
        //appState.UnregisterChangeCallback("playerScore", OnValueChanged);        
    }
    public bool AddRepo(string name, string repoUrl, string branch,string status ="UNKNOWN",string lastUpdated ="NEVER")
    {
        throw new System.Exception("Also dead code?!?!");
        bool success = SetRecord(new DictStrStr
        {
            { "name", name },
            { "url", repoUrl },
            { "branch", branch },
            { "status", status },
            { "last_updated",lastUpdated }
        });
        SetStatusLabel($"repo_count {GetRecords().Count}");
        return success;

    }
    // TODO get rid of this binding method, in favour of all Data classes working independetly
    public void BindToProfileData(ProfileData profileData)
    {
        //profileDatasource = navigatorObject.GetComponent<ProfileData>();
        profileDatasource = profileData;

    }
    public  void ReloadData()
    {
        if (profileDatasource == null)
        {
            Debug.Log("RepoData: LoadData halted; profileDatasource is null");
            return;
        }
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        SetupData setDat = navigatorObject.GetComponent<SetupData>();

        if (setDat.IsPythonReady() == false)
        {
            Debug.Log("ReloadData Cant run, because venv is not configured yet.");
            return;
        }

        string selectedProfileNames = (string)appState.Get("selected_profile");
        string[] profileNames = selectedProfileNames.Split(',');
        foreach ( string selectedProfileName in profileNames)
        {
            if (selectedProfileNames == null)
            {
                Debug.Log("RepoData LoadData halted; selectedProfileName is null");
                return;
            }
        }

    }
    private void ProcessUser(string selectedProfileName)
    {

        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        SetupData setDat = navigatorObject.GetComponent<SetupData>();
        string venvPythonPath = setDat.GetPythonRoot();
        DictStrStr rec = profileDatasource.GetRecord(selectedProfileName);
        if (rec == null)
        {
            Debug.Log("Could not find target user");
            return;
        }
        // propenv/bin/python3

        var prnt = new DictTable {{"output",rec}};
        string[] command = new string[] { venvPythonPath,"propagator/datasource/UtilGit.py","list_local_repos" };
        DictStrStr arguments =new DictStrStr {{"backup_directory",rec["path"]}} ;
        bool isNamedArguments = true; 
        ShellRun.Response r = ShellRun.RunCommand( command, arguments,  isNamedArguments,  setDat.GetPythonWorkDir() );
        List<object> jsonObj = (List<object> )JsonParser.ParseJsonObjects(r.Output);
        string jsonString = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
        if (jsonObj.Count == 0)
            return;
        jsonString = JsonConvert.SerializeObject(jsonObj[0], Formatting.Indented);
        // Debug.Log(jsonString);
        foreach (Dictionary<string,object> folder in jsonObj)
        {
            // Debug.Log((string)folder["directory_name"]);
            string name = (string)folder["directory_name"];
            SetRecord(new DictStrStr
            {
                { "name", name },
                { "profile_name", selectedProfileName },
                { "url", "unknown" },
                { "branch", "unknown" },
                { "status", "Active" },
                { "last_updated", "Never" } // Default status on creation
            });            
            SetStatusLabel($"repo_count {GetRecords().Count}");            
        }
    }

    public override void AfterSaveData()
    {
        
    }    

}
