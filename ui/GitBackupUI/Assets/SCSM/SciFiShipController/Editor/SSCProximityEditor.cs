﻿using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    [CustomEditor(typeof(SSCProximity))]
    public class SSCProximityEditor : Editor
    {
        #region Static Strings

        #endregion

        #region Custom Editor private variables
        //private SSCProximity sscProximity;
        private bool isStylesInitialised = false;
        private bool isSceneModified = false;
        // Formatting and style variables
        private string labelText;
        private GUIStyle labelFieldRichText;
        private GUIStyle headingFieldRichText;
        private GUIStyle helpBoxRichText;
        private GUIStyle buttonCompact;
        private GUIStyle foldoutStyleNoLabel;
        //private Color separatorColor = new Color();
        private float defaultEditorLabelWidth = 0f;
        private float defaultEditorFieldWidth = 0f;
        #endregion

        #region GUIContent - Headers
        private readonly static GUIContent headerContent = new GUIContent("This module enables you call methods when something enters or exits the trigger area");

        #endregion

        #region GUIContent - General
        private readonly static GUIContent initialiseOnStartContent = new GUIContent("Initialise on Start", "If enabled, Initialise() will be called as soon as Start() runs. This should be disabled if you want to control when the SCC Proximity is enabled through code.");
        private readonly static GUIContent tagsContent = new GUIContent("Unity Tags", "Array of Unity Tags for objects that affect this collider area. If none are provided, all objects can affect this area. NOTE: All tags MUST exist.");
        private readonly static GUIContent onEnterMethodsContent = new GUIContent("On Enter Methods", "Methods that get called when a collider enters the trigger area");
        private readonly static GUIContent onExitMethodsContent = new GUIContent("On Exit Methods", "Methods that get called when a collider exits the trigger area");

        #endregion

        #region Serialized Properties - General
        private SerializedProperty initialiseOnStartProp;
        private SerializedProperty tagsProp;
        private SerializedProperty onEnterMethodsProp;
        private SerializedProperty onExitMethodsProp;

        #endregion

        #region Events

        public void OnEnable()
        {
            //sscProximity = (SSCProximity)target;

            defaultEditorLabelWidth = 175f;
            defaultEditorFieldWidth = EditorGUIUtility.fieldWidth;

            //separatorColor = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f, 2f) : Color.grey;

            #region Find Properties - General
            initialiseOnStartProp = serializedObject.FindProperty("initialiseOnStart");
            tagsProp = serializedObject.FindProperty("tags");
            onEnterMethodsProp = serializedObject.FindProperty("onEnterMethods");
            onExitMethodsProp = serializedObject.FindProperty("onExitMethods");

            #endregion

            #region Find Buttons

            #endregion

        }

        #endregion

        #region Public Static Methods

        // Add a menu item so that a SSCProximity can be created via the GameObject > 3D Object menu
        [MenuItem("GameObject/3D Object/Sci-Fi Ship Controller/SSC Proximity (Sphere)")]
        public static SSCProximity CreateProximitySphere()
        {
            SSCProximity sscProximity = null;

            // Create a new gameobject
            GameObject proximityObj = new GameObject("SSCProximity (Sphere)");
            if (proximityObj != null)
            {
                SphereCollider proximityCollider = proximityObj.AddComponent<SphereCollider>();

                if (proximityCollider != null)
                {
                    proximityCollider.isTrigger = true;

                    sscProximity = proximityObj.AddComponent<SSCProximity>();

                    #if UNITY_EDITOR
                    if (sscProximity == null)
                    {
                        Debug.LogWarning("ERROR: SSCProximity.CreateProximitySphere could not add SSCProximity component to " + proximityObj.name);
                    }
                    #endif
                }
                #if UNITY_EDITOR
                else
                {
                    Debug.LogWarning("ERROR: SSCProximity.CreateProximitySphere could not add a sphere collider to " + proximityObj.name);
                }
                #endif
            }

            return sscProximity;
        }

        // Add a menu item so that a SSCProximity can be created via the GameObject > 3D Object menu
        [MenuItem("GameObject/3D Object/Sci-Fi Ship Controller/SSC Proximity (Box)")]
        public static SSCProximity CreateProximityBox()
        {
            SSCProximity sscProximity = null;

            // Create a new gameobject
            GameObject proximityObj = new GameObject("SSCProximity (Box)");
            if (proximityObj != null)
            {
                BoxCollider proximityCollider = proximityObj.AddComponent<BoxCollider>();

                if (proximityCollider != null)
                {
                    proximityCollider.isTrigger = true;

                    sscProximity = proximityObj.AddComponent<SSCProximity>();

                    #if UNITY_EDITOR
                    if (sscProximity == null)
                    {
                        Debug.LogWarning("ERROR: SSCProximity.CreateProximityBox could not add SSCProximity component to " + proximityObj.name);
                    }
                    #endif
                }
                #if UNITY_EDITOR
                else
                {
                    Debug.LogWarning("ERROR: SSCProximity.CreateProximityBox could not add a box collider to " + proximityObj.name);
                }
                #endif
            }

            return sscProximity;
        }

        #endregion

        #region OnInspectorGUI
        // This function overides what is normally seen in the inspector window
        // This allows stuff like buttons to be drawn there
        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();

            #region Initialise
            EditorGUIUtility.labelWidth = defaultEditorLabelWidth;
            EditorGUIUtility.fieldWidth = defaultEditorFieldWidth;
            isSceneModified = false;
            #endregion

            #region Configure Buttons and Styles

            // Set up rich text GUIStyles
            if (!isStylesInitialised)
            {
                helpBoxRichText = new GUIStyle("HelpBox");
                helpBoxRichText.richText = true;

                labelFieldRichText = new GUIStyle("Label");
                labelFieldRichText.richText = true;

                headingFieldRichText = new GUIStyle(UnityEditor.EditorStyles.miniLabel);
                headingFieldRichText.richText = true;
                headingFieldRichText.normal.textColor = helpBoxRichText.normal.textColor;

                // Overide default styles
                EditorStyles.foldout.fontStyle = FontStyle.Bold;

                // When using a no-label foldout, don't forget to set the global
                // EditorGUIUtility.fieldWidth to a small value like 15, then back
                // to the original afterward.
                foldoutStyleNoLabel = new GUIStyle(EditorStyles.foldout);
                foldoutStyleNoLabel.fixedWidth = 0.01f;

                buttonCompact = new GUIStyle("Button");
                buttonCompact.fontSize = 10;

                isStylesInitialised = true;
            }

            #endregion

            // Read in all the properties
            serializedObject.Update();

            #region Header Info and Buttons
            EditorGUILayout.LabelField(headerContent, helpBoxRichText);
            #endregion

            #region General Settings
            GUILayout.BeginVertical("HelpBox");
            EditorGUILayout.PropertyField(initialiseOnStartProp, initialiseOnStartContent);

            SSCEditorHelper.DrawArray(tagsProp, tagsContent, defaultEditorLabelWidth, "Tag");

            GUILayoutUtility.GetRect(1f, 2f);

            EditorGUILayout.PropertyField(onEnterMethodsProp, onEnterMethodsContent);
            EditorGUILayout.PropertyField(onExitMethodsProp, onExitMethodsContent);

            GUILayout.EndVertical();
            #endregion

            // Apply property changes
            serializedObject.ApplyModifiedProperties();

            #region Mark Scene Dirty if required

            if (isSceneModified && !Application.isPlaying)
            {
                isSceneModified = false;
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            #endregion
        }

        #endregion
    }
}