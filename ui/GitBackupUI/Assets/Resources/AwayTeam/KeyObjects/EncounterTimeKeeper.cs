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
        float __timerProgress = 0f;
        float __timerProgressMax = 100f;
        public float GetTimerProgress()
        {
            return __timerProgress;
        }
        public float GetTimerProgressMax()
        {
            return __timerProgressMax;
        }

        Dictionary<string,IATGameMode> __encounterUpdateFunction;

        public void Awake()
        {
            __encounterUpdateFunction = new Dictionary<string,IATGameMode> ();
        }
        
        public void RegisterEncounter(string encounterId, IATGameMode encounter)
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
            //CoroutineRunner.Instance.DebugLog("In Init Loop");
        }

        public void SetTimerProgress(float number)
        {
            __timerProgress = number; 
        }

        public void Run()
        {
            if (_isRunning) 
                return; // or throw an exception if you want strictness
            _isRunning = true;
            //CoroutineRunner.Instance.DebugLog("In Start Loop");
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
            //CoroutineRunner.Instance.DebugLog("In Pause Loop");
            /*
            
            _isRunning = false;
            OnPaused?.Invoke();
            */
        }
        /// Ends the encounter loop entirely (cannot continue).
        public void EndLoop()
        {
            //CoroutineRunner.Instance.DebugLog("In End Loop");

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
            foreach (IATGameMode encounter in __encounterUpdateFunction.Values)
            {
                encounter.DoUpdate(Time.deltaTime);
            }
        }
        

    }
