using System;
using System.Collections.Generic;

/// <summary>
/// A lightweight scheduler to run named tasks at intervals.
/// This is a bare minimum utility inspired by the IntervalRunner 
/// used inside SpaceEncounterManager.
/// </summary>
namespace Encounter
{
    public class IntervalRunner
    {
        private Dictionary<string, float> _timeTrackers = new Dictionary<string, float>();

        /// <summary>
        /// Attempts to run a callback if enough time (interval) has elapsed for the given key.
        /// </summary>
        /// <param name="key">Unique ID for this repeating operation.</param>
        /// <param name="interval">Seconds that must pass before callback triggers.</param>
        /// <param name="deltaTime">Time elapsed since last frame.</param>
        /// <param name="callback">Action to invoke if interval has passed.</param>
        public void RunIfTime(string key, float interval, float deltaTime, Action callback)
        {
            if (!_timeTrackers.ContainsKey(key))
                _timeTrackers[key] = 0f;

            _timeTrackers[key] += deltaTime;

            if (_timeTrackers[key] >= interval)
            {
                _timeTrackers[key] = 0f;
                callback?.Invoke();
            }
        }

        /// <summary>
        /// Clears all stored time trackers, effectively resetting them.
        /// </summary>
        public void ClearAllTimers()
        {
            _timeTrackers.Clear();
        }
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
    public class EncounterTimeLoop
    {
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
        }

        /// <summary>
        /// Resets internal timers and signals that the loop is about to run.
        /// </summary>
        public void StartLoop()
        {
            if (_isRunning) 
                return; // or throw an exception if you want strictness
            
            _intervalRunner.ClearAllTimers();
            _timerProgress = 0f;
            _isRunning = true;
            OnUnpaused?.Invoke();
        }

        /// <summary>
        /// Pauses the loop (no more timing checks).
        /// </summary>
        public void PauseLoop()
        {
            if (!_isRunning) return;
            
            _isRunning = false;
            OnPaused?.Invoke();
        }

        /// <summary>
        /// Ends the encounter loop entirely (cannot continue).
        /// </summary>
        public void EndLoop()
        {
            // You may want to set a separate "ended" flag if you 
            // intend to allow "Resume" after End. 
            // Typically, End is final.
            
            _isRunning = false;
            _intervalRunner.ClearAllTimers();
            _timerProgress = 0f;

            OnEncounterOver?.Invoke();
        }

        /// <summary>
        /// Main update that should be called each frame from outside (e.g. in a MonoBehaviour).
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last frame.</param>
        public void UpdateLoop(float deltaTime)
        {
            // If not running, do nothing.
            if (!_isRunning) return;

            // 1) Example: Check if the encounter should end every second.
            _intervalRunner.RunIfTime("endEncounterCheck", 1f, deltaTime, () =>
            {
                // You can do any "should the encounter end?" checks here.
                // If it should end, call EndLoop().
                // 
                // e.g.:
                // if (SomeConditionForEnd) { EndLoop(); }
            });

            // 2) Periodically update the "timer" or "progress bar" every 0.25s
            //    so we can reflect the time passing for the current epoch.
            _intervalRunner.RunIfTime("showActionProgress", 0.25f, deltaTime, () =>
            {
                // The original logic: __timerProgress += (0.25f / epochLength) * __timerProgressMax;
                float increment = (0.25f / _epochLength) * _timerProgressMax;
                _timerProgress = Math.Min(_timerProgress + increment, _timerProgressMax);

                OnTimerTick?.Invoke();
            });

            // 3) Trigger an "action interval" every 5.0s (default _epochLength).
            _intervalRunner.RunIfTime("doActions", _epochLength, deltaTime, () =>
            {
                if (!_actionsRunning)
                {
                    _actionsRunning = true;
                    StartActionInterval();
                }
            });

            // 4) After a small delay (0.5s) from starting the action interval, 
            //    check if we can end the action interval.
            _intervalRunner.RunIfTime("doActionsClear", _postActionClearDelay, deltaTime, () =>
            {
                if (_actionsRunning)
                {
                    // In a real system, you might check if Agents are still busy.
                    // For V1, we just assume they're done after 0.5s.
                    EndActionInterval();
                }
            });
        }

        // ---------------------------------------------------------
        // Internal (Action Interval) 
        // ---------------------------------------------------------

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
    }
}