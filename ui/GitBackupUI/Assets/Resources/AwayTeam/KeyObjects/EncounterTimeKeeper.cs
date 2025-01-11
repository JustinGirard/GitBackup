using System;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;

/// <summary>
/// A lightweight scheduler to run named tasks at intervals.
/// This is a bare minimum utility inspired by the IntervalRunner 
/// used inside SpaceEncounterManager.
/// </summary>
/// 

    public interface IGameEncounter
    {
        void Begin();
        void End();
        void Run();
        void Pause();
        void DoUpdate(float deltaTime);
    }


    /// <summary>
    /// Encapsulates a time-stepped "Encounter Loop" that can be used 
    /// by any type of encounter (combat, puzzle, etc.).
    /// 
    /// USAGE HINTS:
    ///  - Instantiate EncounterTimeLoop in your manager/encounter.
    ///  - Each frame, call UpdateLoop(deltaTime).
    ///  - Subscribe to events (OnTimerTick, OnActionIntervalStart, etc.) 
    ///    to handle your custom logic.
    /// </summary>
    public class EncounterTimeKeeper:MonoBehaviour, IPausable
    {
        /*
        // ---------------------------------------------------------
        // Events/Callbacks that the encounter can subscribe to:
        // ---------------------------------------------------------
        
        /// <summary>
        /// Fires when the loop is "unpaused" or started.
        /// </summary>
        public event Action OnUnpaused;
        
        /// <summary>
        /// Fires when the loop is paused.
        /// </summary>
        public event Action OnPaused;
        
        /// <summary>
        /// Fires when the encounter ends (for *any* reason).
        /// </summary>
        public event Action OnEncounterOver;
        
        /// <summary>
        /// Fires periodically (e.g., every 0.25s) to update progress bars, 
        /// countdown timers, etc.
        /// </summary>
        public event Action OnTimerTick;
        
        /// <summary>
        /// Fires at the start of each action interval (e.g., every 5s).
        /// Encounters can use this moment to queue up actions, do targeting, etc.
        /// </summary>
        public event Action OnActionIntervalStart;

        /// <summary>
        /// Fires when an action interval is considered finished (after a brief delay, etc.).
        /// Typically a point to reset UI or prepare for the next round.
        /// </summary>
        public event Action OnActionIntervalEnd;

        // ---------------------------------------------------------
        // Internal loop state
        // ---------------------------------------------------------
        
        private IntervalRunner _intervalRunner = new IntervalRunner();
        
        private bool _isRunning = false;
        private bool _actionsRunning = false;

        // For a simple “progress bar” or “timer.” 
        private float _timerProgress = 0f;
        private float _timerProgressMax = 100f;

        // The time between “action intervals” (like a turn in a turn-based game).
        private float _epochLength = 5.0f;

        // A small delay used after starting the actions, to detect when to end them.
        // (In the original code, it was 0.5f)
        private float _postActionClearDelay = 0.5f;


        // ---------------------------------------------------------
        // Public API
        // ---------------------------------------------------------

        /// <summary>
        /// True if the loop is currently running (unpaused).
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Returns the current progress from 0..TimerProgressMax.
        /// </summary>
        public float TimerProgress => _timerProgress;

        /// <summary>
        /// The maximum progress value used for the “timer bar.”
        /// </summary>
        public float TimerProgressMax => _timerProgressMax;

        /// <summary>
        /// How many seconds between each "action interval."
        /// </summary>
        public float EpochLength
        {
            get => _epochLength;
            set => _epochLength = value;
        }*/
        Dictionary<string,IGameEncounter> __encounterUpdateFunction;

        public void Awake()
        {
            __encounterUpdateFunction = new Dictionary<string,IGameEncounter> ();
        }
        
        public void RegisterEncounter(string encounterId, IGameEncounter encounter)
        {
            __encounterUpdateFunction[encounterId]= encounter;
        }


        bool __isReady = false;
        public bool AmReady()
        {
            return __isReady;
        } 
        public void SetReadyToRun(bool rdy)
        {
            if (IsRunning())
                throw new System.Exception("Cant set the level if am running level");
            __isReady = rdy;
        }

        private bool _isRunning = false;

        public bool IsRunning(){
            return _isRunning;
        }
        public void InitLoop()
        {
            _isRunning = false;
            CoroutineRunner.Instance.DebugLog("In Init Loop");
        }

        public void Run()
        {
            if (_isRunning) 
                return; // or throw an exception if you want strictness
            _isRunning = true;
            CoroutineRunner.Instance.DebugLog("In Start Loop");
            /*
            
            _intervalRunner.ClearAllTimers();
            _timerProgress = 0f;
            _isRunning = true;
            OnUnpaused?.Invoke();
            */
        }
        /// Pauses the loop (no more timing checks).
        public void Pause()
        {
            if (!_isRunning) return;
            _isRunning = false;
            CoroutineRunner.Instance.DebugLog("In Pause Loop");
            /*
            
            _isRunning = false;
            OnPaused?.Invoke();
            */
        }
        /// Ends the encounter loop entirely (cannot continue).
        public void EndLoop()
        {
            CoroutineRunner.Instance.DebugLog("In End Loop");

            // You may want to set a separate "ended" flag if you 
            // intend to allow "Resume" after End. 
            // Typically, End is final.
            /*
            _isRunning = false;
            _intervalRunner.ClearAllTimers();
            _timerProgress = 0f;

            OnEncounterOver?.Invoke();
            */
        }
        /// Main update that should be called each frame from outside (e.g. in a MonoBehaviour).
        public void Update(){
            if (!AmReady())
            {
                return;
            }
            if (!IsRunning())
            {
                return;
            }
            foreach (IGameEncounter encounter in __encounterUpdateFunction.Values)
            {
                encounter.DoUpdate(Time.deltaTime);
            }
        }
        

        // ---------------------------------------------------------
        // Internal (Action Interval) 
        // ---------------------------------------------------------
    /*
        private void StartActionInterval()
        {
            // Reset progress bar to 0 for the next epoch, if desired
            _timerProgress = 0f;

            // Let subscribers know we started a fresh "turn" or "action interval."
            OnActionIntervalStart?.Invoke();
            // Subscribing code can now queue attacks, run coroutines, etc.
        }

        private void EndActionInterval()
        {
            // Clear any "actions" or "states" for the new round
            _actionsRunning = false;
            // Possibly set _timerProgress = 0 if you want each "turn" to re-init, etc.

            OnActionIntervalEnd?.Invoke();
        }
    */
    }
