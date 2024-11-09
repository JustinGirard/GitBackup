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
using UnityEngine.AI;
using Unity.VisualScripting;
using System.Text;


public class JobUtils
{
    private static void AsyncRunMainThread(Action action)
    {
        ApplicationState.Instance().Enqueue(action);
    }
    private static void AsyncRunMainThread(Action<object> action,object param)
    {
        ApplicationState.Instance().Enqueue(action,param);
    }


    public static Func<Task> CreateJobifiedFunc(
        string id,
        string jobName,
        Func<Task<object>> taskFunc,
        Func<bool> isRunning,
        Func<bool> cancel,
        Func<object> readProgress,
        Action<object> onProgress,
        Action<object> onSuccess,
        Action<object> onFailure,
        bool debugMode,
        string parentId)
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
            jobData.RegisterJob(id:jobId, name:jobName, parentId:parentId);
            // Create JobHandle
            object progressJson = "UNKNOWN";
            JobHandle jobHandle = new JobHandle
            {
                IsRunning = isRunning,
                Cancel = cancel,
                Poll = () =>
                {
                    progressJson = readProgress();
                    Debug.Log("Read "+progressJson);
                    onProgress(progressJson); // TODO Consider injecting progress logic somewhere else

                    return true;
                },
                GetProgress = () =>{
                    return progressJson;
                },
                GetData = () =>
                {
                    return new DictStrStr();
                }
            };
            jobData.AttachJobHandle(jobId, jobHandle);

            try
            {
                if (debugMode)
                {
                    AsyncRunMainThread(() => Debug.Log($"Starting task for job '{jobName}' with ID '{jobId}'"));
                }

                // Update the job status to "running"
                jobData.UpdateJobStatus(jobId, "started");
                object result = await taskFunc();
                jobData.UpdateJobStatus(jobId, "finishing");

                if (debugMode)
                {
                    AsyncRunMainThread(() => Debug.Log($"Task completed successfully for job '{jobName}'"));
                }

                // Invoke the success callback
                AsyncRunMainThread(() => onSuccess?.Invoke(result));
                jobData.UpdateJobStatus(jobId, "finished");
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    AsyncRunMainThread(() => Debug.Log($"Task failed for job '{jobName}': Exception: {ex.Message}"));
                }
                AsyncRunMainThread(() => onFailure?.Invoke(ex));
                jobData.UpdateJobStatus(jobId, "failed");
            }
        };

        return asyncTaskFunction;
    }
    
    private static readonly Dictionary<int, string> KnownErrorMessages = new Dictionary<int, string>
    {
        { 0, "Success: No error occurred." },
        { 1, "General error: Indicates a general error in command execution. Check syntax or permissions." },
        { 2, "Misuse of shell built-ins: Often caused by syntax errors or incorrect usage of built-in commands." },
        { 126, "Command invoked cannot execute: Command found but is not executable. Check permissions or file type." },
        { 127, "Command not found: Command does not exist in the system PATH or directory. Verify command and PATH." },
        { 128, "Invalid argument to exit: Command exited with a non-standard argument." },
        { 130, "Script terminated by Control-C: Process interrupted by SIGINT (Ctrl+C)." },
        { 137, "Process killed: Process terminated by SIGKILL. This can occur if memory limits are reached." },
        { 139, "Segmentation fault: A memory access violation occurred. Check memory usage and file validity." },
        { 143, "Terminated: Process killed by SIGTERM, likely from an external kill request." },
        { 255, "General error (often a command execution issue): Indicates a general failure to execute the command. Commonly due to issues like missing permissions, misconfigured paths, or inaccessible files." }
        // Add other codes as needed for your application environment.
    };
    public static string GetErrorCodeMessage(int exitCode)
    {
        
        return KnownErrorMessages.TryGetValue(exitCode, out var message)
            ? message
            : $"Unknown error code {exitCode}. Check command and environment for potential issues.";
}

    public static Func<Task> CreateJobifiedShellTask(
        string jobName,
        string[] command,
        DictStrStr arguments,
        bool isNamedArguments,
        string workingDirectory,
        Action<object> onSuccess,
        Func<object> readProgress,
        Action<object> onFailure,
        Action<object> onProgress,
        bool debugMode,
        string parentId)
    {
        ShellRun.Response response = new ShellRun.Response();
        Process process = null;
        Func<Task<object>> shellTaskFunc = async () =>
        {
            try
            {
                if (debugMode == true)
                { 
                    AsyncRunMainThread(() =>
                    {
                        Debug.Log("AM Running Command:"+ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[0] +" "+
                                ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]);
                        Debug.Log(workingDirectory);
                    });
                }
                process = ShellRun.StartProcess(command, arguments, isNamedArguments, workingDirectory);
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());
                
                if (debugMode == true)
                {
                    if (!process.HasExited)
                    {
                        AsyncRunMainThread(() =>
                        {
                            Debug.LogError("Very bad: STILL RUNNING!!!!! Command:"+ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[0] +" "+
                                ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]);
                            Debug.LogError("!!! Process is still running.");
                            Debug.LogError("!!! Process is still running.");
                            Debug.LogError("!!! Process is still running.");
                        });
                    }
                    else
                    {
                        AsyncRunMainThread(() =>
                        {
                            Debug.Log("EXITED! Command:"+ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[0] +" "+
                                ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]);
                        });


                    }
                }                
                response.Output = await outputTask;
                response.Error = await errorTask;

                if (debugMode == true)
                { 
                    AsyncRunMainThread((object param) =>
                    {
                        DictStrStr data = (DictStrStr)param;

                        Debug.Log("FINISHED COMMAND Command:"+ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[0] + " " +
                                ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]);
                        //Debug.Log(data["text_value"]);
                        //Debug.Log(data["exit_code"]);
                        //Debug.Log(data["output"]);
                        //Debug.Log(data["error"]);
                    },new DictStrStr {
                        {"text_value","Some Value"},
                        {"exit_code",process.ExitCode.ToString()},
                        {"output",response.Output.ToString()},
                        {"error",response.Error.ToString()}
                    });

                }
                string completeError = "";
                string systemError = "";
                string processError = response.Error;
                if (process.ExitCode != 0)
                    systemError = GetErrorCodeMessage(process.ExitCode);

                completeError = systemError + processError;
                if (!string.IsNullOrEmpty(completeError) || string.IsNullOrEmpty(response.Output))
                    throw new System.Exception (completeError);

                
                return (object)response.Output;
            }
            finally
            {
                process?.Dispose();
            }
        };

        Func<bool> isRunning = () =>
        {
           // Debug.Log($"Checking run status for { ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]} ");
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
            readProgress:readProgress,
            onSuccess: onSuccess,
            onProgress:onProgress,
            onFailure: onFailure,
            debugMode:debugMode,
            parentId:parentId
        );
    }




    public static Func<Task> CreateShellTask(
        string[] command,
        DictStrStr arguments,
        bool isNamedArguments,
        string workingDirectory,
        Action<object> onSuccess,
        Action<object> onFailure,
        bool debugMode)
    {
        Process process = null;

        Func<Task> shellTaskFunc = async () =>
        {
            try
            {
                if (debugMode)
                {
                    AsyncRunMainThread(() =>
                    {
                        Debug.Log("Running Command: " + ShellRun.BuildCommandArguments(command, arguments, isNamedArguments)[0] + " " +
                                ShellRun.BuildCommandArguments(command, arguments, isNamedArguments)[1]);
                        Debug.Log("Working Directory: " + workingDirectory);
                    });
                }

                // Start the process
                process = ShellRun.StartProcess(command, arguments, isNamedArguments, workingDirectory);

                // Read output and error streams asynchronously
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                // Wait for the process to exit
                await Task.Run(() => process.WaitForExit());

                if (debugMode)
                {
                    AsyncRunMainThread(() =>
                    {
                        if (process.HasExited)
                            Debug.Log("Process exited successfully.");
                        else
                            Debug.LogError("Process is still running.");
                    });
                }

                // Get the results from the output and error streams
                string output = await outputTask;
                string error = await errorTask;

                if (debugMode)
                {
                    AsyncRunMainThread(param =>
                    {
                        var data = (DictStrStr)param;
                        Debug.Log("Command Finished: " + data["command"]);
                        Debug.Log("Exit Code: " + data["exit_code"]);
                        Debug.Log("Output: " + data["output"]);
                        Debug.Log("Error: " + data["error"]);
                    }, new DictStrStr
                    {
                        {"command", ShellRun.BuildCommandArguments(command, arguments, isNamedArguments)[1]},
                        {"exit_code", process.ExitCode.ToString()},
                        {"output", output},
                        {"error", error}
                    });
                }

                // Check for errors
                string completeError = "";
                string systemError = "";
                string processError = error;
                if (process.ExitCode != 0)
                    systemError = GetErrorCodeMessage(process.ExitCode);

                completeError = systemError + processError;
                if (!string.IsNullOrEmpty(completeError) || string.IsNullOrEmpty(output))
                    throw new System.Exception(completeError);

                // Invoke the success callback
                onSuccess?.Invoke(output);
            }
            catch (Exception ex)
            {
                // Invoke the failure callback
                onFailure?.Invoke(ex);
            }
            finally
            {
                // Clean up the process resources
                process?.Dispose();
            }
        };

        return shellTaskFunc;
    }




}

public class JobHandle
{
    public DictStrStr dataframe; // Data associated with the job
    public Func<bool> IsRunning; // Function that returns whether the job is currently running
    public Func<bool> Cancel;    // Function that attempts to cancel the job and returns whether the cancellation was successful
    public Func<bool> Poll; // A job that can trigger an update to data, progress, and any job internals. Typically used to populate GetData and GetProgress
    public Func<DictStrStr> GetData; // Function that returns additional data related to the job

    public Func<object> GetProgress; // Function that returns additional data related to the job
}


public class JobData : StandardData
{
    private static JobData __instance;
    private Dictionary<string, JobHandle> __handles = new Dictionary<string, JobHandle>();
    private float updateInterval = 6.0f; // Check every 5 seconds
    private float timeSinceLastUpdate = 0.0f;

    private float pollingInterval = 2f; // Check every 5 seconds
    private float timeSinceLastPoll = 0.0f;
    //private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    void Awake()
    {
        __instance = this;
    }
    void Start(){
        
        //StartExampleLongRunningTask("finish",10,100);
        //StartExampleLongRunningTask("cancel",10,2000);
        //StartExampleLongRunningTask("run",10,2000);
        //StartExampleLongRunningTask("run2",15,2000);
        //StartExampleLongRunningTask("crash",1000,10);
        Task.Delay(2000); // Wait for 1 second
        Debug.Log("Cancelling Result");
        bool result = CancelJob("cancel");
        Debug.Log($"Cancel Result {result}");
        
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

    /*
    public IEnumerator DownloadAllCoroutine(List<string> repoNames)
    {
        Debug.Log($"!******Doing Download of  REPOS");
        foreach (var repoName in repoNames)
        {
            if (!ContainsRecord(repoName))
            {
                throw new System.Exception($"!{repoName} is missing from the repos. Some strange error with repos");
            }

            DictStrStr repoRecord = GetRecord(repoName);
            SetRecordField(repoName, "download_status", "downloading");

            // Use a flag to track completion status
            bool isCompleted = false;
            bool isSuccessful = false;

            // Create the download task
            Func<Task> tsk = CreateDownloadRepoTask(
                repoRecord,
                onSuccess: (obj) =>
                {
                    Debug.Log($"Finished Download of {repoName}");
                    isSuccessful = true;
                    isCompleted = true;
                },
                onFailure: (err) =>
                {
                    Debug.Log($"Failed Download of {repoName}");
                    isSuccessful = false;
                    isCompleted = true;
                },
                debugMode: false
            );

            Debug.Log($"Starting download for {repoName}");
            var task = Task.Run(tsk);

            while (!task.IsCompleted)
            {
                yield return null; // Wait for the task to complete
            }
            if (isSuccessful)
            {
                Debug.Log($"Downloaded successfully: {repoName}");
            }
            else
            {
                Debug.Log($"Download failed: {repoName}");
            }
        }
        Debug.Log($"!******FINISH Download of  REPOS");

    }    
    */
    void StartExampleLongRunningTask(string id,int seconds, int exception_seconds)
    {
        bool procDoCancel = false;
        int procCount = 0;
        DateTime lastHeartbeat = DateTime.UtcNow;
        TaskCompletionSource<bool> taskStarted = new TaskCompletionSource<bool>();
        // Define the task function
        float progressInt = 0;
        Func<Task<object>> longRunningTask = async () =>
        {
            taskStarted.SetResult(true);                
            for (int i = 0; i <= seconds; i++)
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
                progressInt = ((float)i/(float)seconds);
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


        Func<object> readProgress = () =>
        {
            return progressInt;
        };


        Action<object> onSuccess = (object obj) => Debug.Log("Long-running task completed successfully.");
        Action<object> onFailure = (object obj) => Debug.LogError($"Long-running task failed: ");
        Action<object> onProgress = (object obj) => Debug.Log($"Long-running task progress: {obj}");

        Func<Task> jobifiedTask = JobUtils.CreateJobifiedFunc(
            id:id,
            jobName: "LongTask",
            taskFunc: longRunningTask,
            isRunning: isRunning,
            cancel: cancel,
            readProgress:readProgress,
            onSuccess: onSuccess,
            onProgress:onProgress,
            onFailure: onFailure,
            debugMode: false,
            parentId:"test"
        );

        Task.Run(jobifiedTask);
        taskStarted.Task.Wait(5000);
    }



    void Update()
    {
        // Increment the timer by the time since the last frame
        timeSinceLastUpdate += Time.deltaTime;
        timeSinceLastPoll  += Time.deltaTime;
        bool doPoll = false;
        bool doIsRunning = false;
        if (timeSinceLastPoll >= pollingInterval)
        {
            doPoll = true;
            timeSinceLastPoll = 0.0f;
        }

        // If enough time has passed, perform the check
        if (timeSinceLastUpdate >= updateInterval)
        {
            doIsRunning = true;
            timeSinceLastUpdate = 0.0f;
        }
        LockReserve();
        try
        {      
            // Check the status of all jobs
            if (doPoll == true || doIsRunning == true)
            {


                UpdateAllJobStatuses(   doPoll:doPoll,
                                        doIsRunning:doIsRunning);
            }
            UpdateDataRevision(1); // Flag that the jobs should update
        }
        finally
        {
            LockRelease(); 
        }

    
    }
    public string StringifyProcessResp(object resp)
    {
        if (resp as System.Exception != null)
        {
            var stringBuilder = new StringBuilder();
            System.Exception exception = (System.Exception)resp;
            while (exception != null)
            {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                //exception = exception.InnerException;
                return "ProcError:"+stringBuilder;
            }

           return  ((System.Exception)resp).ToSummaryString();
        }
        return resp.ToString();
    } 
    // Check and update the status of all jobs
    private void UpdateAllJobStatuses(bool doPoll,bool doIsRunning)
    {
        LockReserve();
        try
        {      
            // TODO - finish moving poll and run update code into jobs themselves, so they mey register their own frequencies. This paves the way for jobs to 
            // run in a lighter manner.
            List<string> recs = this.ListRecords();
            foreach (string id in recs)
            {
                // Check if the job is running
                bool isRunning = IsJobRunning(id);
                if (doPoll==true)
                {
                    if(isRunning==true)
                    {
                        Debug.Log("Running Poll A");
                        PollJob(id); // Tell the job to update
                        object progressData = GetJobProgress(id);
                        UpdateJobField(id,  "progress",$"uX-{StringifyProcessResp(progressData)}");
                    }
                }
                if(doIsRunning==true)
                {
                    //string flagStatus = GetRecordField(id,"status");
                    string flagRunning =  GetRecordField(id,"running");
                    // Detect if the running status is out of sync, and sync it.
                    if (isRunning && flagRunning != "true" )
                    {
                        // Put in inital status right away
                        Debug.Log("Running Poll B");
                        PollJob(id);
                        object progressData = GetJobProgress(id);
                        UpdateJobField(id,  "progress",$"u0-{StringifyProcessResp(progressData)}");
                        UpdateJobRunning(id, "true");
                    }
                    if (!isRunning && flagRunning == "true" )
                    {
                        // Update status, shut down job
                        Debug.Log("Running Poll C");
                        PollJob(id); // Tell the job to update
                        object progressData = GetJobProgress(id);
                        UpdateJobField(id,  "progress",$"un-{StringifyProcessResp(progressData)}");
                        UpdateJobRunning(id, "false");
                    }
                    if (!isRunning && GetRecordField(id,  "progress") == "")
                    {
                        Debug.Log("Running Poll F");
                        PollJob(id); // Tell the job to update
                        object progressData = GetJobProgress(id);
                        UpdateJobField(id,  "progress",$"uf-{StringifyProcessResp(progressData)}");
                        UpdateJobRunning(id, "false");
                    }
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
    public bool RegisterJob(string id, string name, string status = "pending",string parentId=null)
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
                { "parent_id", parentId },
                { "status", status },
                { "running", "unknown" },
                { "progress", "" },
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
    public bool PollJob(string id)
    {
        LockReserve();
        try
        {      

            if (__handles.ContainsKey(id))
            {
                var handle = __handles[id];
                if (handle.Poll == null)
                {   
                    return false;
                }
                return (bool)handle.Poll?.Invoke();
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
    public object GetJobProgress(string id)
    {
        LockReserve();
        try
        {      
            if (__handles.ContainsKey(id))
            {
                var handle = __handles[id];
                // return $"Interception for {id}";
                return (object)handle.GetProgress?.Invoke();
            }
            return 0f;
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

    public override void  AfterSaveData()
    {

        DictTable records = GetRecords();

        SetStatusLabel($"total {records.Keys.Count}");

    }
}

