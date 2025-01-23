using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StandardUIDocument
{
    public class FloatingPowerIndicator : MonoBehaviour, IShowHide
    {
        [SerializeField]
        private float offsetX = 0;
        [SerializeField]
        private float offsetY = 0;

        [SerializeField]
        private GameObject __attachedGameObject; // Target GameObject
        [SerializeField]
        private Camera __mainCamera; // Perspective camera for the scene

        private RectTransform _panel; // The panel RectTransform
        private RectTransform _canvasRect; // The Canvas RectTransform

        private static FloatingPowerIndicator __instance;

        public static void AttachTo(GameObject obj)
        {
            if (__instance != null)
                __instance.__attachedGameObject = obj;
        }

        public static FloatingPowerIndicator Instance()
        {
            return __instance;
        }

        private void Awake()
        {
            __instance = this;

            // Cache the RectTransform of the panel
            _panel = GetComponent<RectTransform>();
            if (_panel == null)
            {
                Debug.LogError("RectTransform not found on the GameObject.");
            }

            if (__mainCamera == null)
            {
                __mainCamera = Camera.main;
            }

            // Dynamically find the RectTransform of the parent canvas
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                _canvasRect = parentCanvas.GetComponent<RectTransform>();
            }
            else
            {
                Debug.LogError("Parent Canvas not found. Ensure this component is inside a Canvas.");
            }
        }

        public void Show()
        {
            if (_panel != null)
                _panel.gameObject.SetActive(true);
        }

        public void Hide()
        {
            //if (_panel != null)
            //    _panel.gameObject.SetActive(false);
        }

        void Update()
        {
            // Exit early if no target is set
            if (__attachedGameObject == null)
            {
                Hide();
                return;
            }

            // If the target GameObject is inactive or missing, hide and clear the target
            if (!__attachedGameObject.activeInHierarchy)
            {
                Hide();
                __attachedGameObject = null;
                return;
            }

            if (_panel != null && _panel.gameObject.activeSelf && _canvasRect != null)
            {
                Vector3 worldPosition = __attachedGameObject.transform.position;
                Vector3 screenPosition = __mainCamera.WorldToScreenPoint(worldPosition);

                float screenX = screenPosition.x;
                float screenY = screenPosition.y;
                float totalScreenWidth = Screen.width;
                float totalScreenHeight = Screen.height;
                RectTransform canvasRect = _canvasRect; // Assuming _canvasRect is already the Canvas' RectTransform
                float canvasWidth = canvasRect.sizeDelta.x;
                float canvasHeight = canvasRect.sizeDelta.y;
                
                _panel.anchoredPosition = new Vector2(
                    657+ (screenX*canvasWidth/totalScreenWidth) + offsetX,
                    -27 -  (screenY*canvasHeight/totalScreenHeight) - offsetY
                );                

            }
        }
    }
}
