using System;
using System.Collections;
using System.Collections.Generic;
using OpenCover.Framework.Model;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace VisualCommand
{
    public class SurfaceNavigationCommand : MonoBehaviour, IShowHide,ISurfaceNavigationCommand
    {
        [Serializable]
        public class SelectionStyle
        {
           //=public string styleId;
            public GameObject prevNodeStyle;
            public GameObject edgeStyle;
            public GameObject currentNodeStyle;

        }
        public GameObject GameObject()
        {
            return this.gameObject;
        }        
        //GameObject targetSurface;
        //GameObject currentNode;
        //GameObject previousNode;
        public GameObject GetTarget(){
            SelectionStyle st = GetActiveStyle();
            if (st.currentNodeStyle != null)
                return st.currentNodeStyle;
            return st.prevNodeStyle;
        }
        [SerializeField]
        private SpaceCombatScreen __targetScreen;
        //public void SetTargetScreen(SpaceCombatScreen targetScreen){
        //    this.targetScreen = targetScreen;
        //}    


        [SerializeField]
        private SelectionStyle styleSelectFinishedInactive;

        [SerializeField]
        private SelectionStyle styleSelectSurface;
        
        [SerializeField]
        private SelectionStyle styleSelectHeight;

        [SerializeField]
        private SelectionStyle styleSelectFinishedActive;
        
        [SerializeField]
        private bool clickEnabled = false;

        public bool __debugMode = true;
        public class SelectionState {
            public static bool IsValid(string state)
            {
                return all.Contains(state);
            }               
            public static readonly List<string> all = new List<string> { off, inactive,select_surface,select_height,active };

            public const string off = "off";
            public const string inactive = "inactive";
            public const string select_surface = "select_surface";
            public const string select_height = "select_height";
            public const string active = "active";
        }
        private Dictionary<string,SelectionStyle> visualStyle;
        public void StyleSetup()
        {
            visualStyle = new Dictionary<string, SelectionStyle>();
            visualStyle[SelectionState.off] = null;
            visualStyle[SelectionState.inactive] = styleSelectFinishedInactive;
            visualStyle[SelectionState.select_surface] = styleSelectSurface;
            visualStyle[SelectionState.select_height] = styleSelectHeight;
            visualStyle[SelectionState.active] = styleSelectFinishedActive;
        }
        public string commandSelectionState = "off";
        public string GetActiveState(){
            return commandSelectionState;
        }

        public SelectionStyle GetActiveStyle()
        {
            return visualStyle[commandSelectionState];
        }


        public void Show(){
            //Debug.Log("I SHOULD SHOW");
            gameObject.SetActive(true);
            SetVisualState(SelectionState.select_surface);
        }
        public void Hide(){
            gameObject.SetActive(false);
            SetVisualState(SelectionState.off);
        }
        public bool IsShowing()
        {
            //Debug.Log($"gameObject.activeInHierarchy: {gameObject.activeInHierarchy} and GetActiveState():{GetActiveState() }");
            if (gameObject.activeInHierarchy == false)
                return false;
            if (GetActiveState() == SelectionState.off)
                return false;
            //Debug.Log($"IsShowing TRUE");
            return true;
        }
        /*
            public IEnumerator RunSelectHeight(string state, string cmd, Vector3 mousePosition, Vector3 contactPositon,Vector3 contactNormal  )
            {
                if (cmd == GeneralInputManager.Command.primary_up)
                {
                    SetVisualState(SelectionState.active);
                }
                // Rely on "Update()" for height
                AlignCursorWithHeight(state,mousePosition);
                ScaleCursorByDistance(state,contactPositon);
                yield break;
            }        
        */
        /*public void QuickNavTo(Vector3 topPoint,Vector3 bottomPoint)
        {
            SelectionStyle selectedStyle = visualStyle[SelectionState.active];
            __lastHeightPosition = topPoint; 
            __lastSurfacePosition = bottomPoint;
            selectedStyle.prevNodeStyle.transform.position = __lastSurfacePosition;
            selectedStyle.currentNodeStyle.transform.position = __lastHeightPosition;
            Show();
            __targetScreen.HandleNavigationEvent(this);
            SetVisualState(SelectionState.active);
        }*/
        public void ProcessEvent(string cmd, Vector3 mousePosition, Vector3 contactPosition, Vector3 contactNormal )
        {
            if (clickEnabled == false)
                return;
            if (string.IsNullOrEmpty(cmd))
                Debug.LogError("ProcessEvent: Command is null or empty.");

            if (mousePosition == null)
                Debug.LogError("ProcessEvent: Mouse position is null.");

            if (contactPosition == null)
                Debug.LogError("ProcessEvent: Contact position is null.");
            if (contactNormal == null)
                Debug.LogError("ProcessEvent: Contact Normal is null.");
            StartCoroutine(RunStateMachine(commandSelectionState, cmd, mousePosition,contactPosition, contactNormal));
        }
        //class UserSpatialAction{
        //    public string cmd,  
        //    public Vector2 mousePosition,  
        //    public Vector3 contactPositon, 
        //    public Vector3 contactNormal 
        //}
        private IEnumerator RunStateMachine(string state, string cmd, Vector2 mousePosition, Vector3 contactPositon, Vector3 contactNormal )
        {
            if (__debugMode == true && cmd != GeneralInputManager.Command.primary_move && cmd != GeneralInputManager.Command.secondary_move)
                Debug.Log($"----1-RunActions:  {state}:{cmd}, \n mouse: {mousePosition} \n mouse: {contactPositon}");


            if (commandSelectionState== SelectionState.off)
            {
                if (__debugMode == true && cmd != GeneralInputManager.Command.primary_move && cmd != GeneralInputManager.Command.secondary_move)
                    Debug.Log($"----2-RunOff");
                yield return RunSelectSurface( state,  
                                                cmd,  
                                                mousePosition,  
                                                contactPositon,
                                                contactNormal );
            }
            else if (commandSelectionState== SelectionState.select_surface)
            {
                if (__debugMode == true && cmd != GeneralInputManager.Command.primary_move && cmd != GeneralInputManager.Command.secondary_move)
                    Debug.Log($"----2-RunSelectSurface");
                yield return RunSelectSurface( state,  
                                                cmd,  
                                                mousePosition,  
                                                contactPositon,
                                                contactNormal );
            }
            else if (commandSelectionState== SelectionState.select_height)
            {
                if (__debugMode == true && cmd != GeneralInputManager.Command.primary_move && cmd != GeneralInputManager.Command.secondary_move)
                    Debug.Log($"----2-RunSelectHeight");
                yield return RunSelectHeight( state,  
                                                cmd,  
                                                mousePosition,  
                                                contactPositon,
                                                contactNormal );
            }
            else if (commandSelectionState== SelectionState.active)
            {
                if (__debugMode == true && cmd != GeneralInputManager.Command.primary_move && cmd != GeneralInputManager.Command.secondary_move)
                    Debug.Log($"----2-RunActive");
                yield return RunActive( state,  
                                                cmd,  
                                                mousePosition,  
                                                contactPositon,
                                                contactNormal );
            }
            if (commandSelectionState== SelectionState.inactive)
            {
                if (__debugMode == true && cmd != GeneralInputManager.Command.primary_move && cmd != GeneralInputManager.Command.secondary_move)
                    Debug.Log($"----2-RunInactive");
                yield return RunInactive( state,  
                                                cmd,  
                                                mousePosition,  
                                                contactPositon,
                                                contactNormal );
            }
            else
            {
                if (__debugMode == true && cmd != GeneralInputManager.Command.primary_move && cmd != GeneralInputManager.Command.secondary_move)
                    Debug.Log($"----2-RunOff");
                yield return RunOff( state,  
                                                cmd,  
                                                mousePosition,  
                                                contactPositon,
                                                contactNormal );
            }
             //this.transform.position = contactPositon + contactNormal.normalized*0.1f;
            //}
            //finally
            //{
            //    Sema.ReleaseLock($"navsel_{state}_{cmd}");
            //}            
            //yield break;
        }


        public void SetVisualState(string state)
        {
            if (visualStyle == null)
            {
                Debug.LogError($"No Visual Style Presen for state {state}");
                return;
            }
            if (state == null || state.Length == 0)
            {
                Debug.LogError($"State is null");
            }
            if (!visualStyle.ContainsKey(state))
            {
                Debug.LogError($"[{state}] not present in visualStyle");
                return;
            }
            SelectionStyle selectedStyle =visualStyle[state];

            foreach (SelectionStyle otherStyle in visualStyle.Values)
            {
                if (otherStyle == null)
                    continue;
                if (selectedStyle == otherStyle)
                    continue;
                if (otherStyle.prevNodeStyle != null)
                    otherStyle.prevNodeStyle.SetActive(false);
                if (otherStyle.edgeStyle != null)
                    otherStyle.edgeStyle.SetActive(false);
                if (otherStyle.currentNodeStyle != null)
                    otherStyle.currentNodeStyle.SetActive(false);
            }
            commandSelectionState =state; 
            if (state == SelectionState.off)
            {
                return; // No need to activate anything for off state.
            }
            if (selectedStyle == null)
            {
                Debug.LogError($"No selectedStyle Present for state {state}");
                return;
            }            
            if (selectedStyle.prevNodeStyle != null)
                selectedStyle.prevNodeStyle.SetActive(true);
            if (selectedStyle.edgeStyle != null)
                selectedStyle.edgeStyle.SetActive(true);
            if (selectedStyle.currentNodeStyle != null)
                selectedStyle.currentNodeStyle.SetActive(true);
            //if (__debugMode == true)
            //    Debug.Log($"SettingVisualState:  {state} ");                
        }
        
        public void AlignCursorWithSurface(string state, Vector3 contactPositon, Vector3 contactNormal, Vector2 mousePosition){
            Camera mainCamera = GeneralInputManager.Instance().GetCamera();
            SelectionStyle selectedStyle =visualStyle[state];
            if (selectedStyle == null)
            {
                Debug.LogError($"AlignCursorWithSurface:selectedStyle is null for {state}");
                return;
            }
            
            this.transform.position = contactPositon + contactNormal.normalized*0.1f;

            //this.transform.rotation = Quaternion.LookRotation(-mainCamera.transform.forward, contactNormal);
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 projectedForward = Vector3.ProjectOnPlane(-cameraForward, contactNormal.normalized).normalized;
            this.transform.rotation = Quaternion.LookRotation(projectedForward, contactNormal);
            __lastProjectedForward = projectedForward;
            __lastContactNormal = contactNormal;
            __lastSurfacePosition= this.transform.position;
            __lastMousePosition = mousePosition;
            if(selectedStyle.currentNodeStyle != null)
            {
                selectedStyle.currentNodeStyle.transform.rotation = Quaternion.LookRotation(projectedForward, contactNormal);
            }
            if(selectedStyle.prevNodeStyle != null)
            {
                selectedStyle.prevNodeStyle.transform.rotation = Quaternion.LookRotation(projectedForward,contactNormal);
            }
        }

        //public void AlignCursorWithHeight(string state, Vector3 contactPositon, Vector3 contactNormal, Vector2 mousePosition){
        //    // Given __lastMousePosition
        //    // Move selectedStyle.currentNodeStyle position up contactNormal.normalzed at a distance of (1.y + contactNormal.y)
        //    // 
        //}
        public void AlignCursorWithHeight(string state, Vector2 mousePosition) {
            SelectionStyle selectedStyle = visualStyle[state];
            if (selectedStyle == null || selectedStyle.currentNodeStyle == null) {
                Debug.LogError($"AlignCursorWithHeight: selectedStyle or currentNodeStyle is null for {state}");
                return;
            }

            // Calculate the vertical translation based on mouse Y difference
            float yDifference = mousePosition.y - __lastMousePosition.y + 1.0f;
            float scalingFactor = 0.03f; // Adjust scaling factor for sensitivity
            float heightOffset = yDifference * scalingFactor;

            // Update the current node's position
            Vector3 newPosition = __lastSurfacePosition + __lastContactNormal.normalized * heightOffset;
            selectedStyle.currentNodeStyle.transform.position = newPosition;
            __lastHeightPosition = newPosition;
        }

        public void AlignWithSelectedSurfaceAndHeight(string state) {
            SelectionStyle selectedStyle = visualStyle[state];
            if (selectedStyle == null || selectedStyle.currentNodeStyle == null) {
                Debug.LogError($"AlignCursorWithHeight: selectedStyle or currentNodeStyle is null for {state}");
                return;
            }
            selectedStyle.currentNodeStyle.transform.position = __lastHeightPosition; // __lastSurfacePosition
            if (selectedStyle == null || selectedStyle.prevNodeStyle == null) {
                Debug.LogError($"AlignCursorWithHeight: selectedStyle or currentNodeStyle is null for {state}");
                return;
            }
            selectedStyle.prevNodeStyle.transform.position = __lastSurfacePosition;
            this.transform.position = __lastSurfacePosition;
        }


        Vector3 __lastProjectedForward = new Vector3(0,0,-1);
        Vector3 __lastContactNormal = new Vector3(0,-1,0);
        Vector3 __lastSurfacePosition= new Vector3(0,-1,0);
        Vector3 __lastHeightPosition= new Vector3(0,-1,0);
        Vector2 __lastMousePosition= new Vector2(1,1);
        void OnDrawGizmos() {
            // Draw contactNormal (Blue)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(__lastSurfacePosition, __lastSurfacePosition + __lastContactNormal*5f);

            // Draw projectedForward (Green)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(__lastSurfacePosition, __lastSurfacePosition + __lastProjectedForward*5f);
        }


        private Dictionary<int, Vector3> originalScales = new Dictionary<int, Vector3>();        

        public void InitOriginalScales() {
            PopulateOriginalScales(transform);
        }

        private void PopulateOriginalScales(Transform parentTransform) {
            foreach (Transform child in parentTransform) {
                int uniqueId = child.GetInstanceID();
                if (!originalScales.ContainsKey(uniqueId)) {
                    originalScales[uniqueId] = child.localScale;
                }
                PopulateOriginalScales(child); // Recursively visit children
            }
        }

        public void ScaleCursorByDistance(string state, Vector3 targetPosition) {
            Camera mainCamera = GeneralInputManager.Instance().GetCamera();
            SelectionStyle selectedStyle = visualStyle[state];

            if (selectedStyle == null) {
                Debug.LogError($"ScaleCursorByDistance: selectedStyle is null for {state}");
                return;
            }

            // Calculate distance from the camera to the target position
            float distance = Vector3.Distance(mainCamera.transform.position, targetPosition);

            // Define a scaling factor based on the distance
            float scaleMultiplier = 0.05f; // Adjust this for sensitivity
            float baseScale = 1.0f; // Minimum scale
            float newScaleFactor = baseScale + (distance * scaleMultiplier);

            // Apply the new scale to current and previous node styles if they exist
            if (selectedStyle.currentNodeStyle != null) {
                ScaleTransform(selectedStyle.currentNodeStyle.transform, newScaleFactor);
            }

            if (selectedStyle.prevNodeStyle != null) {
                ScaleTransform(selectedStyle.prevNodeStyle.transform, newScaleFactor);
            }
        }

        private void ScaleTransform(Transform transformToScale, float scaleFactor) {
            int uniqueId = transformToScale.gameObject.GetInstanceID();
            if (!originalScales.ContainsKey(uniqueId)) {
                originalScales[uniqueId] = transformToScale.gameObject.transform.localScale;
            }
            if (originalScales.TryGetValue(uniqueId, out Vector3 originalScale)) 
                transformToScale.gameObject.transform.localScale = originalScale * scaleFactor;
            foreach (Transform child in transformToScale) {
                ScaleTransform(child, scaleFactor); // Recursively scale children
            }
        }      

        public IEnumerator RunInactive(string state, string cmd, Vector3 mousePosition, Vector3 contactPositon, Vector3 contactNormal )
        {
            if (__debugMode == true && cmd != GeneralInputManager.Command.primary_move && cmd != GeneralInputManager.Command.secondary_move)
                Debug.Log($"----3-In Inactive:  {state}:{cmd}, \n mouse: {mousePosition} \n mouse: {contactPositon}");

            if (cmd == GeneralInputManager.Command.primary_up)
            {
                if (__debugMode == true && cmd != GeneralInputManager.Command.primary_move && cmd != GeneralInputManager.Command.secondary_move)
                    Debug.Log($"3b - Executing SetVisualState(SelectionState.select_surface)");
                SetVisualState(SelectionState.select_surface);
            }
            yield break;
        }
        public IEnumerator RunSelectSurface(string state, string cmd, Vector3 mousePosition, Vector3 contactPositon,Vector3 contactNormal  )
        {
            Camera mainCamera = GeneralInputManager.Instance().GetCamera();
            SelectionStyle selectedStyle =visualStyle[state];
            AlignCursorWithSurface(state,contactPositon,contactNormal,mousePosition);
            ScaleCursorByDistance(state,contactPositon);
            if (cmd == GeneralInputManager.Command.primary_up)
            {
                SetVisualState(SelectionState.select_height);
            }
            yield break;
        }
        public IEnumerator RunSelectHeight(string state, string cmd, Vector3 mousePosition, Vector3 contactPositon,Vector3 contactNormal  )
        {
            if (cmd == GeneralInputManager.Command.primary_up)
            {
                SetVisualState(SelectionState.active);
            }
            // Rely on "Update()" for height
            AlignCursorWithHeight(state,mousePosition);
            ScaleCursorByDistance(state,contactPositon);
            yield break;
        }
        public IEnumerator RunActive(string state, string cmd, Vector3 mousePosition, Vector3 contactPositon,Vector3 contactNormal  )
        {
            AlignWithSelectedSurfaceAndHeight(state);
            //if (cmd == GeneralInputManager.Command.primary_up)
            //{
            //    SetVisualState(SelectionState.inactive);
            //}
            yield break;
        }
        public IEnumerator RunOff(string state, string cmd, Vector3 mousePosition, Vector3 contactPositon,Vector3 contactNormal  )
        {
            yield break;
        }
        void Awake()
        {
            StyleSetup();

        }

        void Start()
        {
            __targetScreen = SpaceCombatScreen.Instance();
        }

        void Update()
        {
            if (GetActiveState() == SelectionState.select_height)
            {
                var (isPrimaryMove, primaryMovePosition) = GeneralInputManager.Instance().PollCommandStatus(GeneralInputManager.Command.primary_move);
                if (isPrimaryMove)
                {
                    AlignCursorWithHeight(SelectionState.select_height,primaryMovePosition);
                }
                var (isPrimaryUp, primaryUpPosition) = GeneralInputManager.Instance().PollCommandStatus(GeneralInputManager.Command.primary_down);
                if (isPrimaryUp)
                {
                    if (__targetScreen.SetPlayerNavigation(this,AgentNavigationType.NavigateToOnce))
                        SetVisualState(SelectionState.active);
                }

            }
        }

    }
}