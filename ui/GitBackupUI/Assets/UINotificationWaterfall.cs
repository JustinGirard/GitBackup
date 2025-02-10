using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class UINotificationWaterfall : MonoBehaviour
{
    private static UINotificationWaterfall __instance;  
    // Singleton accessor
    public static UINotificationWaterfall Instance()
    {
        if (__instance == null)
        {
            Debug.LogError("UINotificationWaterfall instance not found! Make sure an UINotificationWaterfall is in the scene and Awake() is called.");
        }
        return __instance;
    }    
    [Serializable]
    public class NotificationPrefabEntry
    {
        public string notificationId;
        public GameObject prefab;
    }

    [Serializable]
    public class NotificationEvent
    {
        public string messageId;
        public bool isUnique;
        public float timeStart;
        public float duration;
        public GameObject notificationObject;
    }
    public void Start()
    {
        UINotificationWaterfall.Instance().Dispatch("basic", "msg1", "Hello World", 5f, false);
        UINotificationWaterfall.Instance().Dispatch("basic", "msg2", "Goodbye World", 5f, false);
        UINotificationWaterfall.Instance().Dispatch("basic", "msg2", "Number 2", 5f, true);
        UINotificationWaterfall.Instance().Dispatch("basic", "msg3", "Goodbye World", 5f, false);
        UINotificationWaterfall.Instance().Dispatch("basic", "msg2", "Number 2.1", 5f, true);
    }
    [SerializeField] private List<NotificationPrefabEntry> notificationPrefabs;
    private Dictionary<string, GameObject> prefabMap;
    private List<NotificationEvent> activeNotifications = new List<NotificationEvent>();
    //private ObjectPool<GameObject> notificationPool;
    
    private void Awake()
    {
        __instance = this;
        prefabMap = new Dictionary<string, GameObject>();
        foreach (var entry in notificationPrefabs)
        {
            prefabMap[entry.notificationId] = entry.prefab;
        }

        //notificationPool = new ObjectPool<GameObject>(CreateNotification, OnGetNotification, OnReleaseNotification);
        InvokeRepeating(nameof(CleanupExpiredNotifications), 1f, 1f);
    }


    private void UpdateExistingNotification(string prefabId,string messageId, string message, float duration)
    {
        var existingNotification = activeNotifications.Find(n => n.messageId == messageId);
        if (existingNotification != null)
        {
            existingNotification.timeStart = Time.time;
            existingNotification.duration = duration;
            if (existingNotification.notificationObject.TryGetComponent<IUINotify>(out var notify))
            {
                notify.SetText(message);
            }
        }
    }

    public void Dispatch(string prefabId, string messageId, string message, float duration, bool isUnique = false)
    {
        if (!prefabMap.ContainsKey(prefabId))
        {
            Debug.LogWarning($"Notification ID '{prefabId}' not found.");
            return;
        }

        var existingNotification = activeNotifications.Find(n => n.messageId == messageId);
        if (isUnique == true && existingNotification != null)
        {
            UpdateExistingNotification(  prefabId,  messageId,  message, duration);
            return;

        }
        //GameObject notificationObject = notificationPool.Get();
        GameObject notificationObject  = ObjectPool.Instance().Load(prefabMap[prefabId]); 
        //notificationObject.SetActive(true);
        notificationObject.transform.SetParent(transform, false);
        //Debug.Log(notificationObject);
        //Debug.Log(notificationObject.GetComponent<UINotification>());

        if (notificationObject.TryGetComponent<IUINotify>(out var notify))
        {
            notify.SetText(message);
            notify.Show();
        }

        activeNotifications.Add(new NotificationEvent
        {
            messageId = messageId,
            isUnique = isUnique,
            timeStart = Time.time,
            duration = duration,
            notificationObject = notificationObject
        });
    }

    private void CleanupExpiredNotifications()
    {
        float currentTime = Time.time;
        for (int i = activeNotifications.Count - 1; i >= 0; i--)
        {
            var evt = activeNotifications[i];
            if (currentTime - evt.timeStart >= evt.duration)
            {
                evt.notificationObject.SetActive(false);
                //notificationPool.Release(evt.notificationObject);
                activeNotifications.RemoveAt(i);
            }
        }
    }
}
