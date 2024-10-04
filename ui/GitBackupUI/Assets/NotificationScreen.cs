using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;


public class NotificationScreen : MonoBehaviour, NavigationManager.ICanInitalize
{
    // Start is called before the first frame update
    void Start()
    {
        //            var rootVisualElement = doc.GetComponent<UIDocument>()?.rootVisualElement;
        //rootVisualElement.style.display = DisplayStyle.None;
    }
    public void Refresh()
    {

    }
    // Update is called once per frame
    void Update()
    {
        // Message
        // Progress
        // Cancel
        // OK
    }

    public void InitData(DictStrStr record)
    {
        if (record.ContainsKey("message"))
        {
            Debug.Log("Got Message");
            Debug.Log(record["message"]);
            var root = GetComponent<UIDocument>().rootVisualElement;
            var ele = root.Q<Label>("Message");
            ele.text = record["message"];
        }
        Debug.Log("No Message");

    }
}
