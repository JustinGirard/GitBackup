using System.Collections.Generic;
using UnityEngine;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
using System;
using System.Threading;
using System.Threading.Tasks;



using System.Diagnostics;

using Debug = UnityEngine.Debug;
using System.Runtime.InteropServices;
/*

   private Func<Task> CreateShellTaskOLD(
        string jobName,
        string[] command,
        DictStrStr arguments,
        bool isNamedArguments,
        string workingDirectory,
        Action<ShellRun.Response> onSuccess,
        Action<ShellRun.Response> onFailure,
        bool debugMode = false)
    {
        Func<Task> asyncTaskFunction = async () =>
        {
            try
            {
                if (debugMode == true) Debug.Log("INNER TEMP Running a command "+command[0]);
                ShellRun.Response response = ShellRun.RunCommand(command, arguments, isNamedArguments: isNamedArguments, workingDirectory);
                if (!string.IsNullOrEmpty(response.Error))
                {
                    if (debugMode == true) Debug.Log("INNER TEMP Running a command FAIL 1");
                    onFailure.Invoke(response);
                }
                if (debugMode == true) Debug.Log("INNER TEMP Running a command SUCCESS");
                onSuccess?.Invoke(response);
            }
            catch (Exception ex)
            {
                if (debugMode == true) Debug.Log("*********Fatal Exception running command:"+ex.Message+" "+ex.StackTrace);
                if (debugMode == true) Debug.Log("TEMP Running a command FAIL 2");
                ShellRun.Response resp = new ShellRun.Response();
                resp.Output = "";
                resp.Error = $"An error occurred during shell execution: {ex.Message}: {ex.StackTrace}";
                onFailure.Invoke(resp);
            }
        };
        return  asyncTaskFunction;
        //Example basic usage Task.Run(() => asyncTaskFunction());
    }
*/
/*
public static Response RunCommand(string[] command,  DictStrStr arguments, bool isNamedArguments = false, string workingDirectory = null)
    {
        var response = new Response();

        try
        {
            using (Process process = new Process())
            {
                string[] commandAndArgs = BuildCommandArguments(command, arguments, isNamedArguments);
                process.StartInfo.FileName = commandAndArgs[0]; 
                process.StartInfo.Arguments = commandAndArgs[1];
                process.StartInfo.RedirectStandardOutput = true;                  
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                if (workingDirectory != null)
                {
                    process.StartInfo.WorkingDirectory = workingDirectory;
                }
                // Print the full command
                process.Start();

                process.WaitForExit();
                // Block until the process exits
                response.Output = process.StandardOutput.ReadToEnd();
                response.Error = process.StandardError.ReadToEnd();

            }
        }
        catch (Exception ex)
        {
            response.Error = $"Exception occurred: {ex.Message}";
        }

        return response;
    }


public class StandardData : MonoBehaviour
{
    private DictTable __records = new DictTable();
    private int __dataRevision = 0;
    private System.Random random = new System.Random(); // Create a Random instance

    public virtual void  Refresh()
    {
    }
    public DictTable  GetRecords()
    {
        BeforeLoadData();     
        return __records;
    }

    public int GetDataRevision()
    {
        return __dataRevision;
    }

    public bool ContainsRecord(string name)
    {
        BeforeLoadData();     
        return __records.ContainsKey(name);
    }

    // Method to assign a new random revision number
    public void UpdateDataRevision(int code)
    {
        // Debug.Log($"++++++UpdateDataRevision Code "+code.ToString());
        __dataRevision = random.Next(1, int.MaxValue); // Assign a new random number, avoiding zero
    }

    public void PrintRecordsToDebugLog()
    {
        Debug.Log("---------My revision"+__dataRevision.ToString());
        foreach (var outerEntry in __records)
        {
            string key = outerEntry.Key;
            DictStrStr innerDict = outerEntry.Value;
            //Debug.Log($"Record Name: {key}");
            foreach (var innerEntry in innerDict)
            {
                Debug.Log($"{innerEntry.Key}: {innerEntry.Value}");
            }
            //Debug.Log("------------------------");
        }
    }


    public virtual List<string> ListRecords()
    {
        BeforeLoadData();     
        return new List<string>(__records.Keys);
    }
    public virtual void BeforeLoadData()
    {


    }
    public virtual void AfterSaveData()
    {

        
    }


    // Simulate getting repository info
    public virtual DictStrStr GetRecord(string name)
    {
        BeforeLoadData();     
        if (__records.ContainsKey(name))
        {
            return __records[name];
        }
        return null; // Repo not found
    }

    public virtual string GetRecordField(string name,string field)
    {
        BeforeLoadData();     
        if (!__records.ContainsKey(name))
        {
            return null;
        }
        if (!__records[name].ContainsKey(field))
        {
            return null;
        }
        return __records[name][field];
    }  

    public virtual bool SetRecordField(string name,string field,string value)
    {
        if (!__records.ContainsKey(name))
        {
            return false;
        }
        if (!__records[name].ContainsKey(field))
        {
            return false;
        }
        __records[name][field] = value;
        UpdateDataRevision(1);   
        AfterSaveData();     
        return true;
    }
    protected bool SetRecord(DictStrStr rec)
    {
        string name = rec["name"];
        __records[name] = rec;
        UpdateDataRevision(2);   
        AfterSaveData();     
        return true; 
    }
    public virtual bool DeleteRecord(string name)
    {
        if (__records.ContainsKey(name))
        {
            __records.Remove(name);

        }
        UpdateDataRevision(3);        
        AfterSaveData();
        return true;
    }
    public void  SetRecords(DictTable records)
    {
        __records = records;
        UpdateDataRevision(4);        
        AfterSaveData();
    }

}
*/



public class JobUtils
{
    private static void RunMainThread(Action action)
    {
        ApplicationState.Instance().Enqueue(action);
    }
    /*
    public static Func<Task> CreateShellTask(
        string jobName,
        string[] command,
        DictStrStr arguments,
        bool isNamedArguments,
        string workingDirectory,
        Action<ShellRun.Response> onSuccess,
        Action<ShellRun.Response> onFailure,
        bool debugMode = false)
    {
        Func<Task> asyncTaskFunction = async () =>
        {
            try
            {
                if (debugMode)
                {
                    RunMainThread(() => Debug.Log("INNER TEMP Running a command " + command[0]));
                }

                ShellRun.Response response = ShellRun.RunCommand(command, arguments, isNamedArguments: isNamedArguments, workingDirectory);
                if (debugMode)
                {
                    RunMainThread(() => Debug.Log("INNER TEMP RAN a command " + command[0]));
                }

                if (!string.IsNullOrEmpty(response.Error))
                {
                    if (debugMode)
                    {
                        RunMainThread(() => Debug.Log("INNER TEMP Running a command FAIL 1"));
                    }
                    RunMainThread(() => onFailure.Invoke(response));
                    return;
                }

                if (debugMode)
                {
                    RunMainThread(() => Debug.Log("INNER TEMP Running a command SUCCESS"));
                }
                RunMainThread(() => onSuccess?.Invoke(response));
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    RunMainThread(() => Debug.Log("*********Fatal Exception running command:" + ex.Message + " " + ex.StackTrace));
                }

                ShellRun.Response resp = new ShellRun.Response
                {
                    Output = "",
                    Error = $"An error occurred during shell execution: {ex.Message}: {ex.StackTrace}"
                };
                RunMainThread(() => onFailure.Invoke(resp));
            }
        };
        return asyncTaskFunction;
    }
    */

    public static Func<Task> CreateJobifiedFunc(
        string id,
        string jobName,
        Func<Task<object>> taskFunc,
        Func<bool> isRunning,
        Func<bool> cancel,
        Action<object> onSuccess,
        Action<object> onFailure,
        bool debugMode)

    {
        Func<Task> asyncTaskFunction = async () =>
        {
            // Generate a unique job ID
            string jobId = "";
            if(id == null)
                 jobId = Guid.NewGuid().ToString();
            else
                 jobId = id;

            // Register the job with JobData
            JobData jobData = JobData.Instance();
            jobData.RegisterJob(jobId, jobName);

            // Create JobHandle
            JobHandle jobHandle = new JobHandle
            {
                IsRunning = isRunning,
                Cancel = cancel,
                GetProgress = () =>
                {
                    // Since progress might not be available, return 0 or 100 based on whether the task is running
                    return isRunning() ? 0 : 100;
                },
                GetData = () =>
                {
                    // Provide any additional data if needed
                    return new DictStrStr();
                }
            };

            // Attach the JobHandle to the job
            jobData.AttachJobHandle(jobId, jobHandle);

            try
            {
                if (debugMode)
                {
                    RunMainThread(() => Debug.Log($"Starting task for job '{jobName}' with ID '{jobId}'"));
                }

                // Update the job status to "running"
                jobData.UpdateJobStatus(jobId, "started");
                object result = await taskFunc();
                jobData.UpdateJobStatus(jobId, "finishing");

                if (debugMode)
                {
                    RunMainThread(() => Debug.Log($"Task completed successfully for job '{jobName}'"));
                }

                // Invoke the success callback
                RunMainThread(() => onSuccess?.Invoke(result));
                jobData.UpdateJobStatus(jobId, "finished");
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    RunMainThread(() => Debug.LogError($"Task failed for job '{jobName}': {ex.Message}"));
                }

                // Update the job status to "failed"
                //jobData.UpdateJobStatus(jobId, "failed");

                // Invoke the failure callback
                RunMainThread(() => onFailure?.Invoke(ex));
                jobData.UpdateJobStatus(jobId, "failed");
            }
        };

        return asyncTaskFunction;
    }
    
    /*
    public static Func<Task> CreateJobifiedShellTask(
        string jobName,
        string[] command,
        DictStrStr arguments,
        bool isNamedArguments,
        string workingDirectory,
        Action<ShellRun.Response> onSuccess,
        Action<ShellRun.Response> onFailure,
        bool debugMode = false)
    {
        Func<Task> asyncTaskFunction = async () =>
        {
            // Generate a unique job ID
            string jobId = Guid.NewGuid().ToString();

            // Register the job with JobData
            JobData jobData = JobData.Instance();
            jobData.RegisterJob(jobId, jobName);

            // Variables to hold process and response
            Process process = null;
            ShellRun.Response response = new ShellRun.Response();

            // Create JobHandle
            JobHandle jobHandle = new JobHandle
            {
                IsRunning = () =>
                {
                    if (process == null)
                        return false;
                    try
                    {
                        return !process.HasExited;
                    }
                    catch
                    {
                        return false;
                    }
                },
                Cancel = () =>
                {
                    if (process == null)
                        return false;
                    try
                    {
                        process.Kill();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                },
                GetProgress = () =>
                {
                    // Since we don't have progress information, return 0 or 100 based on whether process is running
                    if (process == null)
                        return 0;
                    return process.HasExited ? 100 : 0;
                },
                GetData = () =>
                {
                    return new DictStrStr
                    {
                        { "stdout", response.Output },
                        { "stderr", response.Error }
                    };
                }
            };

            // Attach the JobHandle to the job
            jobData.AttachJobHandle(jobId, jobHandle);

            try
            {
                if (debugMode)
                {
                    RunMainThread(() => Debug.Log("INNER TEMP Running a command " + command[0]));
                }

                // Start the process using ShellRun.StartProcess
                process = ShellRun.StartProcess(command, arguments, isNamedArguments, workingDirectory);

                // Update the job status to "running"
                jobData.UpdateJobStatus(jobId, "running");

                // Read output and error asynchronously
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                // Wait for the process to exit
                await Task.Run(() => process.WaitForExit());

                // Get the output and error
                response.Output = await outputTask;
                response.Error = await errorTask;

                if (debugMode)
                {
                    RunMainThread(() => Debug.Log("INNER TEMP RAN a command " + command[0]));
                }

                // Update the job status based on process exit code
                if (process.ExitCode == 0)
                {
                    jobData.UpdateJobStatus(jobId, "finished");
                }
                else
                {
                    jobData.UpdateJobStatus(jobId, "failed");
                }

                // Update job data
                jobData.SetRecordField(jobId, "stdout", response.Output);
                jobData.SetRecordField(jobId, "stderr", response.Error);

                if (!string.IsNullOrEmpty(response.Error))
                {
                    if (debugMode)
                    {
                        RunMainThread(() => Debug.Log("INNER TEMP Running a command FAIL 1"));
                    }
                    RunMainThread(() => onFailure?.Invoke(response));
                    return;
                }

                if (debugMode)
                {
                    RunMainThread(() => Debug.Log("INNER TEMP Running a command SUCCESS"));
                }
                RunMainThread(() => onSuccess?.Invoke(response));
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    RunMainThread(() => Debug.Log("*********Fatal Exception running command:" + ex.Message + " " + ex.StackTrace));
                }

                response.Output = "";
                response.Error = $"An error occurred during shell execution: {ex.Message}: {ex.StackTrace}";

                // Update job status and data
                jobData.UpdateJobStatus(jobId, "failed");
                jobData.SetRecordField(jobId, "stderr", response.Error);

                RunMainThread(() => onFailure?.Invoke(response));
            }
            finally
            {
                // Ensure the process is disposed
                if (process != null)
                {
                    process.Dispose();
                }
            }
        };
        return asyncTaskFunction;
    }*/

public static Func<Task> CreateJobifiedShellTask(
    string jobName,
    string[] command,
    DictStrStr arguments,
    bool isNamedArguments,
    string workingDirectory,
    Action<object> onSuccess,
    Action<object> onFailure,
    bool debugMode)
{
    ShellRun.Response response = new ShellRun.Response();
    Process process = null;
    Func<Task<object>> shellTaskFunc = async () =>
    {
        try
        {
            // Start the process using ShellRun.StartProcess
            //Debug.Log("--------------------Running Download Command:");
            RunMainThread(() =>
            {
                Debug.Log("Running Command:"+ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[0] +
                        ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]);
            });
            
            process = ShellRun.StartProcess(command, arguments, isNamedArguments, workingDirectory);
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            await Task.Run(() => process.WaitForExit());
            response.Output = await outputTask;
            response.Error = await errorTask;

            if (!string.IsNullOrEmpty(response.Error) || string.IsNullOrEmpty(response.Output))
                throw new System.Exception (response.Error);
            
            return (object)response.Output;

        }
        finally
        {
            process?.Dispose();
        }
    };

    // Define the isRunning and cancel functions for the JobHandle
    Func<bool> isRunning = () =>
    {
        if (process == null)
            return false;
        try
        {
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    };

    Func<bool> cancel = () =>
    {
        if (process == null)
            return false;
        try
        {
            process.Kill();
            return true;
        }
        catch
        {
            return false;
        }
    };


    return CreateJobifiedFunc(
        id: null, // Generate a new ID inside CreateJobifiedFunc
        jobName: jobName,
        taskFunc: shellTaskFunc,
        isRunning: isRunning,
        cancel: cancel,
        onSuccess: onSuccess,
        onFailure: onFailure,
        debugMode:debugMode
    );
}

}


public class JobHandle
{
    public DictStrStr dataframe; // Data associated with the job
    public Func<bool> IsRunning; // Function that returns whether the job is currently running
    public Func<bool> Cancel;    // Function that attempts to cancel the job and returns whether the cancellation was successful
    public Func<int> GetProgress; // Function that returns the progress of the job as an integer
    public Func<DictStrStr> GetData; // Function that returns additional data related to the job
}
/*
public class JobData : StandardData
{
    private static JobData __instance;
    Dictionary<string,JobHandle> __handles;
    void Awake()
    {
        __instance = this;
        JobHandle j = new JobHandle(); // Null Handle
         RegisterJob("007","test", "job");
    }
    public static JobData Instance(){
        return __instance;
    }
    public bool AttachJobHandle(string id,JobHandle j)
    {
        __handles[id] = j;
        SetRecordField(id,"status","started");
        return true;
    }

    public bool RegisterJob(string id, string name, string status)
    {
        if (string.IsNullOrEmpty(id))
            throw new System.Exception("Null is id");
        if (string.IsNullOrEmpty(name))
            throw new System.Exception("Null is name");
        if (string.IsNullOrEmpty(status))
            throw new System.Exception("Null is status");
        return SetRecord(new DictStrStr
        {
            { "id", id },
            { "name", name },
            { "status", "pending" },
            { "stout", "" },
            { "sterr", "" },
            
        },keyfield:"id");
        //return SetJobHandle(id,j);
    }
}
*/


public class JobData : StandardData
{
    private static JobData __instance;
    private Dictionary<string, JobHandle> __handles = new Dictionary<string, JobHandle>();
    private float updateInterval = 5.0f; // Check every 5 seconds
    private float timeSinceLastUpdate = 0.0f;

    //private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    void Awake()
    {
        __instance = this;
    }
    void Start(){
        /*
        StartExampleLongRunningTask("finish",10,100);
        StartExampleLongRunningTask("cancel",1000,2000);
        StartExampleLongRunningTask("run",1000,2000);
        StartExampleLongRunningTask("crash",1000,10);
        Task.Delay(2000); // Wait for 1 second
        Debug.Log("Cancelling Result");
        bool result = CancelJob("cancel");
        Debug.Log($"Cancel Result {result}");
        */
    }
    private static readonly Mutex _mutex = new Mutex();
    public static JobData Instance()
    {
        return __instance;
    }
    public void LockReserve()
    {
        _mutex.WaitOne();
    }
    public void LockRelease()
    {
        _mutex.ReleaseMutex();
    }

    void StartExampleLongRunningTask(string id,int seconds, int exception_seconds)
    {
        bool procDoCancel = false;
        int procCount = 0;
        DateTime lastHeartbeat = DateTime.UtcNow;
        TaskCompletionSource<bool> taskStarted = new TaskCompletionSource<bool>();
        // Define the task function
        Func<Task<object>> longRunningTask = async () =>
        {
            taskStarted.SetResult(true);                
            for (int i = 0; i < seconds; i++)
            {
                procCount = i;
                int internali = i;
                string internalid = id;
                ApplicationState.Instance().Enqueue(() => Debug.Log($"{internalid} is at {internali.ToString()}"));

                // Update the heartbeat timestamp
                lastHeartbeat = DateTime.UtcNow;

                if (procDoCancel)
                    break;
                if (i > exception_seconds)
                    throw new System.Exception("I failed badly");
                await Task.Delay(1000); // Wait for 1 second
                
            }
            return "id_finished";
        };

        Func<bool> isRunning = () =>
        {
            TimeSpan timeSinceLastHeartbeat = DateTime.UtcNow - lastHeartbeat;
            return timeSinceLastHeartbeat.TotalSeconds < 2;
        };

        Func<bool> cancel = () =>
        {
            procDoCancel = true;
            return true;
        };

        Action<object> onSuccess = (object obj) => Debug.Log("Long-running task completed successfully.");
        Action<object> onFailure = (object obj) => Debug.LogError($"Long-running task failed: ");

        Func<Task> jobifiedTask = JobUtils.CreateJobifiedFunc(
            id:id,
            jobName: "LongTask",
            taskFunc: longRunningTask,
            isRunning: isRunning,
            cancel: cancel,
            onSuccess: onSuccess,
            onFailure: onFailure,
            debugMode: true
        );

        Task.Run(jobifiedTask);
        taskStarted.Task.Wait(5000);
    }



    void Update()
    {
        // Increment the timer by the time since the last frame
        timeSinceLastUpdate += Time.deltaTime;

        // If enough time has passed, perform the check
        if (timeSinceLastUpdate >= updateInterval)
        {
            // Reset the timer
            timeSinceLastUpdate = 0.0f;

            LockReserve();
            try
            {      
                // Check the status of all jobs
                UpdateAllJobStatuses();
                UpdateDataRevision(1); // Flag that the jobs should update
            }
            finally
            {
                LockRelease(); 
            }

        }
    }

    // Check and update the status of all jobs
    private void UpdateAllJobStatuses()
    {
        LockReserve();
        try
        {      
            List<string> recs = this.ListRecords();
            foreach (string id in recs)
            {
                // Check if the job is running
                bool isRunning = IsJobRunning(id);
                string status = GetRecordField(id,"status");
                // Update the job status accordingly
                if (isRunning )
                {
                    UpdateJobRunning(id, "true");
                }
                else
                {
                    UpdateJobRunning(id, "false");
                }
            }
        }
        finally
        {
            LockRelease();
        }

    }    

    // Attach an existing JobHandle to a job ID
    public bool AttachJobHandle(string id, JobHandle handle)
    {
        LockReserve();
        try
        {      
            if (!__handles.ContainsKey(id))
            {
                __handles[id] = handle;
                SetRecordField(id, "status", "started");
                return true;
            }
            return false;
        }
        finally
        {
            LockRelease();
        }

    }

    // Register a new job in the JobData registry
    public bool RegisterJob(string id, string name, string status = "pending")
    {
        LockReserve();
        try
        {      
            if (string.IsNullOrEmpty(id))
                throw new System.Exception("Job ID cannot be null");
            if (string.IsNullOrEmpty(name))
                throw new System.Exception("Job name cannot be null");

            // Create a job record and add it to __records
            var jobRecord = new DictStrStr
            {
                { "id", id },
                { "name", name },
                { "status", status },
                { "running", "false" },
                { "stdout", "" },
                { "stderr", "" }
            };

            SetRecord(jobRecord,keyfield:"id");
            return true;
        }
        finally
        {
            LockRelease();
        }

    }

    // Update the status of a job
    public bool UpdateJobStatus(string id, string status)
    {
        return UpdateJobField(id,"status",status);
    }
    public bool UpdateJobRunning(string id, string status)
    {
        return UpdateJobField(id,"running",status);
    }
    public bool UpdateJobField(string id, string field, string status)
    {
        LockReserve();
        try
        {      
            if (!ContainsRecord(id))
                return false;
            return SetRecordField(id, field, status);
        }
        finally
        {
            LockRelease();
        }

    }    

    // Cancel a job and update its status
    public bool CancelJob(string id)
    {
        LockReserve();
        try
        {      
            if (__handles.ContainsKey(id))
            {
                var handle = __handles[id];
                if ((bool)handle.Cancel?.Invoke() == true)
                {
                    UpdateJobStatus(id, "cancelling");
                    return true;
                }
                return false;
            }
            return false;
        }
        finally
        {
            LockRelease();
        }

    }

    // Check if a job is currently running
    public bool IsJobRunning(string id)
    {
        LockReserve();
        try
        {      
        
            if (__handles.ContainsKey(id))
            {
                var handle = __handles[id];
                if (handle.IsRunning == null)
                    return false;
                return (bool)handle.IsRunning?.Invoke();
            }
            return false;
        }
        finally
        {
            LockRelease();
        }

    }

    // Get the status of a job
    public string GetJobStatus(string id)
    {
        LockReserve();
        try
        {      
            return GetRecordField(id, "status");
        }
        finally
        {
            LockRelease();
        }
    }

    // Get the progress of a job, if applicable
    public int GetJobProgress(string id)
    {
        LockReserve();
        try
        {      
            if (__handles.ContainsKey(id))
            {
                var handle = __handles[id];
                return (int)handle.GetProgress?.Invoke();
            }
            return 0;
        }
        finally
        {
            LockRelease();
        }

    }

    // Get the data associated with a job
    public DictStrStr GetJobData(string id)
    {
        LockReserve();
        try
        {      
            if (__handles.ContainsKey(id))
            {
                var handle = __handles[id];
                return (DictStrStr)handle.GetData?.Invoke();
            }
            return null;
        }
        finally
        {
            LockRelease();
        }

    }

    // List all jobs with a given status
    public List<string> ListJobsByStatus(string status)
    {
        LockReserve();
        try
        {      
            var matchingJobs = new List<string>();
            foreach (var job in ListRecords())
            {
                if (GetRecordField(job, "status") == status)
                {
                    matchingJobs.Add(job);
                }
            }
            return matchingJobs;
        }
        finally
        {
            LockRelease();
        }

    }

    // Get all registered jobs
    public List<string> GetAllJobIds()
    {
        LockReserve();
        try
        {      
            return ListRecords();
        }
        finally
        {
            LockRelease();
        }

    }

    // Remove a job from the registry
    public bool RemoveJob(string id)
    {
        LockReserve();
        try
        {      
            if (DeleteRecord(id))
            {
                if (__handles.ContainsKey(id))
                {
                    __handles.Remove(id);
                }
                return true;
            }
            return false;
        }
        finally
        {
            LockRelease();
        }

    }

    // Print job details for debugging
    public void PrintJobDetails(string id)
    {
        LockReserve();
        try
        {      
            var jobRecord = GetRecord(id);
            if (jobRecord != null)
            {
                Debug.Log($"Job ID: {jobRecord["id"]}, Name: {jobRecord["name"]}, Status: {jobRecord["status"]}");
                Debug.Log($"Stdout: {jobRecord["stdout"]}, Stderr: {jobRecord["stderr"]}");
            }
            else
            {
                Debug.Log("Job not found.");
            }
        }
        finally
        {
            LockRelease();
        }

    }
}

