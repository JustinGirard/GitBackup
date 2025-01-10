using UnityEngine;

namespace MDPackage
{
    /// <summary>
    /// Custom debug class for MD Package
    /// </summary>
    public static class MD_Debug
    {
        public const string ORGANISATION = "Matej Vanco";
        public const string PACKAG_PATH_NAME = "/MD Package/";
        public const short PACKAGE_VERSION = 18;
        /// <summary>
        /// dd/mm/yyyy
        /// </summary>
        public const string LAST_UPDATE_DATE = "10/06/2024";

        public enum DebugType { Error, Warning, Info };
        public static void Debug(MonoBehaviour sender, string message, DebugType debugType = DebugType.Info)
        {
            string senderName = !sender ? "(Unknown sender)" : sender.GetType().Name;
            string senderObjName = !sender ? "(Unknown sender)" : sender.gameObject.name;
            switch (debugType)
            {
                case DebugType.Info: UnityEngine.Debug.Log(senderName + " [" + senderObjName + "]: " + message); break;
                case DebugType.Warning: UnityEngine.Debug.LogWarning(senderName + " [" + senderObjName + "]: " + message); break;
                case DebugType.Error: UnityEngine.Debug.LogError(senderName + " [" + senderObjName + "]: " + message); break;
            }
        }
    }
}