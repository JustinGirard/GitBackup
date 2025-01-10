using UnityEngine;

namespace MDPackage.ExampleContent
{
    /// <summary>
    /// This is an example 'Input Wrapper' for components containing 'GenericInputTemplate' interface. Uses LEGACY UNITY INPUT!
    /// Please write adapt your own input system based on the used APi here.
    /// </summary>
    public sealed class MD_Examples_GenericInputWrapper : MonoBehaviour
    {
        public MonoBehaviour targetGenericInputTemplate;
        [Space]
        public bool usingMobile = false;
        public KeyCode mainButton = KeyCode.Mouse0;

        private IDMGenericInputTemplate targetGeneric;

        private void Start()
        {
            if(targetGenericInputTemplate != null)
                targetGeneric = targetGenericInputTemplate.GetComponent<IDMGenericInputTemplate>();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            targetGenericInputTemplate = GetComponent<MonoBehaviour>();
        }
#endif

        public void ChangeGenericTarget(IDMGenericInputTemplate target)
            => targetGeneric = target;

        private void Update()
        {
            if (targetGeneric == null)
                return;

            if (usingMobile)
            {
                targetGeneric.InputHook_CursorScreenPosition = Input.touchCount > 0 ? Input.GetTouch(0).position : targetGeneric.InputHook_CursorScreenPosition;
                targetGeneric.InputHook_GenericButtonDown = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
            }
            else
            {
                targetGeneric.InputHook_CursorScreenPosition = Input.mousePosition;
                targetGeneric.InputHook_GenericButtonDown = Input.GetKey(mainButton);
            }
        }
    }
}