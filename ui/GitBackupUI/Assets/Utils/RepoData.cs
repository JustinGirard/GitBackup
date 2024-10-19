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

        // Setting the value, which triggers the callback if the value changes
        //appState.Set("playerScore", 100);

        // Unregistering the callback when it is no longer needed
        //appState.UnregisterChangeCallback("playerScore", OnValueChanged);        
    }
    public bool AddRepo(string name, string repoUrl, string branch,string status ="UNKNOWN",string lastUpdated ="NEVER")
    {
        return SetRecord(new DictStrStr
        {
            { "name", name },
            { "url", repoUrl },
            { "branch", branch },
            { "status", status },
            { "last_updated",lastUpdated }
        });
    }
    public void BindToProfileData(ProfileData profileData)
    {
        //profileDatasource = navigatorObject.GetComponent<ProfileData>();
        profileDatasource = profileData;

    }
    public  void ReloadData()
    {
        Debug.Log("RepoData: Doing a full directory read");
        if (profileDatasource == null)
        {
            Debug.Log("RepoData: LoadData halted; profileDatasource is null");
            return;
        }
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        string selectedProfileName = (string)appState.Get("selected_profile");
        if (selectedProfileName == null)
        {
            Debug.Log("RepoData LoadData halted; selectedProfileName is null");
            return;
        }
        DictStrStr rec = profileDatasource.GetRecord(selectedProfileName);
        if (rec == null)
        {
            Debug.Log("Could not find target user");
            return;
        }


        var prnt = new DictTable {{"output",rec}};
        //Debug.Log(ShellRun.BuildJsonFromDictTable(prnt));

        
        // List the Directories
        // Debug.Log("--------------------Running Command:");
        // Debug.Log("--------------------Running Command:");
        // Debug.Log("--------------------Running Command:");
        string[] command = new string[] { "/Users/computercomputer/justinops/propagator/propenv/bin/python3","UtilGit.py","list_local_repos" };
        DictStrStr arguments =new DictStrStr {{"backup_directory",rec["path"]}} ;
        bool isNamedArguments = true; 
        string workingDirectory = "/Users/computercomputer/justinops/propagator/datasource";
        // Debug.Log( ShellRun.BuildCommandArguments(command, arguments, isNamedArguments)[0]);      
        // Debug.Log( ShellRun.BuildCommandArguments(command, arguments, isNamedArguments)[1]);      
        ShellRun.Response r = ShellRun.RunCommand( command, arguments,  isNamedArguments,  workingDirectory );

        // Insert new Dir items
        // Debug.Log("Some output:--------------"+r.Output);
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
                { "url", "unknown" },
                { "branch", "unknown" },
                { "status", "Active" },
                { "last_updated", "Never" } // Default status on creation
            });            
        }

    }
    public override void AfterSaveData()
    {
        
    }    

}
