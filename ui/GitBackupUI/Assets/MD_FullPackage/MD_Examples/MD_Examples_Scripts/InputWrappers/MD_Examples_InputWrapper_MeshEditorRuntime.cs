using UnityEngine;

namespace MDPackage.ExampleContent
{
    /// <summary>
    /// This is an example 'Input Wrapper' for MD_MeshEditorRuntime component. Uses LEGACY UNITY INPUT!
    /// Please write adapt your own input system based on the used APi here.
    /// </summary>
    public sealed class MD_Examples_InputWrapper_MeshEditorRuntime : MonoBehaviour
    {
        public MD_MeshEditorRuntime targetMeshEditorRuntime;
        [Space]
        public KeyCode inputVertexSelection = KeyCode.Mouse0;
        public KeyCode inputVertexAdd = KeyCode.LeftShift;
        public KeyCode inputVertexRemove = KeyCode.LeftAlt;

#if UNITY_EDITOR
        private void Reset()
        {
            targetMeshEditorRuntime = GetComponent<MD_MeshEditorRuntime>();
        }
#endif

        private void Update()
        {
            if (targetMeshEditorRuntime == null)
                return;

            if (targetMeshEditorRuntime.isAxisEditor)
                targetMeshEditorRuntime.InputHook_CursorScreenPosition = Input.mousePosition;
            else
            {
                if (targetMeshEditorRuntime.nonAxis_isMobileFocused && Input.touchCount > 0)
                    targetMeshEditorRuntime.InputHook_CursorScreenPosition = Input.GetTouch(0).position;
                else
                    targetMeshEditorRuntime.InputHook_CursorScreenPosition = Input.mousePosition;
            }

            if (targetMeshEditorRuntime.nonAxis_isMobileFocused && Input.touchCount > 0)
                targetMeshEditorRuntime.InputHook_GenericButtonDown = Input.GetTouch(0).phase == TouchPhase.Began;
            else
                targetMeshEditorRuntime.InputHook_GenericButtonDown = Input.GetKey(inputVertexSelection);

            targetMeshEditorRuntime.InputHook_AxisEditorAddPointsToSelection = Input.GetKey(inputVertexAdd);
            targetMeshEditorRuntime.InputHook_AxisEditorRemovePointsFromSelection = Input.GetKey(inputVertexRemove);
            targetMeshEditorRuntime.InputHook_AxisEditorMousePositionDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }
    }
}