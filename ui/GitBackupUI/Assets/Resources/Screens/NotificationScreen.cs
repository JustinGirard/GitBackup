using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictOnjTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;


public class NotificationScreen : MonoBehaviour, NavigationManager.ICanInitalize
{
    private Dictionary<string, System.Action> actions = new Dictionary<string, System.Action>();

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

    }

    public void InitData(DictStrObj record)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var ele = root.Q<Label>("Message");
        var btnOk = root.Q<Button>("OK");
        var btnCancel = root.Q<Button>("Cancel");

        if (record.ContainsKey("message"))
        {
            //Debug.Log("Got Message");
            //Debug.Log(record["message"]);
            ele.text = (string)record["message"];
        }
        else
           Debug.Log("No Message");
        //Debug.Log("FINISHING");
        /*
        btnOk.RegisterCallback<ClickEvent>(null);
        btnCancel.RegisterCallback<ClickEvent>(null);
        */
        // Replace the OK button (The only way to purge events, apparently)
        VisualElement okParent = btnOk.parent;
        var newBtnOk = new Button();
        newBtnOk.name = btnOk.name;
        newBtnOk.text = btnOk.text;
        okParent.Insert(okParent.IndexOf(btnOk), newBtnOk); // Insert new button in the same place
        okParent.Remove(btnOk); // Remove the old button
        //SetAction("OK", action)
        // Replace the Cancel button (The only way to purge events, apparently)
        VisualElement cancelParent = btnCancel.parent;
        var newBtnCancel = new Button();
        newBtnCancel.name = btnCancel.name;
        newBtnCancel.text = btnCancel.text;
        cancelParent.Insert(cancelParent.IndexOf(btnCancel), newBtnCancel); // Insert new button in the same place
        cancelParent.Remove(btnCancel); // Remove the old button
        //Debug.Log("FINISHED");

    }
    public void DoOK(ClickEvent evt, System.Action action) {
        Debug.Log("Running OK");
        action.Invoke();
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.style.display = DisplayStyle.None;
    }
    public void DoCancel(ClickEvent evt, System.Action action)
    {
        Debug.Log("Running Cancel");
        action.Invoke();
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.style.display = DisplayStyle.None;
    }

    public bool SetAction(string actionLabel, System.Action action)
    {
        if (actionLabel != "OK" && actionLabel != "Cancel")
        { 
            throw new System.Exception($"Can not set this '{actionLabel}' action!");
        }
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (actionLabel == "OK")
        {
            var btnOk = root.Q<Button>("OK");
            btnOk.RegisterCallback<ClickEvent>(evt => DoOK(evt,action));
        }
        if (actionLabel == "Cancel")
        {
            var btnCancel = root.Q<Button>("Cancel");
            btnCancel.RegisterCallback<ClickEvent>(evt => DoCancel(evt, action));
        }

        return true;
    }
}
