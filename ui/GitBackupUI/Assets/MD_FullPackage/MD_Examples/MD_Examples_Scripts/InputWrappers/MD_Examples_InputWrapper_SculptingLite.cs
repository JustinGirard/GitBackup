using UnityEngine;

using MDPackage.Modifiers;

namespace MDPackage.ExampleContent
{
    /// <summary>
    /// This is an example 'Input Wrapper' for MDM_SculptingLite modifier component. Uses LEGACY UNITY INPUT!
    /// Please write adapt your own input system based on the used APi here.
    /// </summary>
    public sealed class MD_Examples_InputWrapper_SculptingLite : MonoBehaviour
    {
        public MDM_SculptingLite targetSculptingLite;
        [Space]
        public KeyCode sculptingRaiseInput = KeyCode.Mouse0;
        public KeyCode sculptingLowerInput = KeyCode.Mouse1;
        public KeyCode sculptingRevertInput = KeyCode.Mouse2;
        public KeyCode sculptingNoiseInput = KeyCode.LeftControl;
        public KeyCode sculptingSmoothInput = KeyCode.LeftAlt;
        public KeyCode sculptingStylizeInput = KeyCode.Z;

#if UNITY_EDITOR
        private void Reset()
        {
            targetSculptingLite = GetComponent<MDM_SculptingLite>();
        }
#endif

        private void Update()
        {
            if (targetSculptingLite == null)
                return;

            if (targetSculptingLite.sculptingMobileSupport)
            {
                targetSculptingLite.InputHook_CursorScreenPosition = Input.touchCount > 0 ? Input.GetTouch(0).position : targetSculptingLite.InputHook_CursorScreenPosition;
                targetSculptingLite.InputHook_GenericNonPCButton = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
            }
            else if (targetSculptingLite.sculptingUseInput)
            {
                targetSculptingLite.InputHook_CursorScreenPosition = Input.mousePosition;
                targetSculptingLite.InputHook_Raise = Input.GetKey(sculptingRaiseInput);
                targetSculptingLite.InputHook_Lower = Input.GetKey(sculptingLowerInput);
                targetSculptingLite.InputHook_Revert = Input.GetKey(sculptingRevertInput);
                targetSculptingLite.InputHook_Noise = Input.GetKey(sculptingNoiseInput);
                targetSculptingLite.InputHook_Smooth = Input.GetKey(sculptingSmoothInput);
                targetSculptingLite.InputHook_Stylize = Input.GetKey(sculptingStylizeInput);
            }
        }
    }
}