using UnityEngine;

namespace MDPackage
{
    public interface IDMGenericInputTemplate
    {
        /// <summary>
        /// Use this input hookup for cursor/touch world screen position
        /// </summary>
        public Vector3 InputHook_CursorScreenPosition { get; set; }
        /// <summary>
        /// Use this input hookup for generic button down
        /// </summary>
        public bool InputHook_GenericButtonDown { get; set; }
    }
}