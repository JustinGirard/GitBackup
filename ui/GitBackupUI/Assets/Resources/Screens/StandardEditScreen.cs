using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
public abstract class StandardEditScreen : MonoBehaviour
{
    protected GameObject navigatorObject;
    protected NavigationManager navigationManager;

    protected virtual void Start()
    {
        navigatorObject = GameObject.Find("Navigator");
        navigationManager = navigatorObject.GetComponent<NavigationManager>();
    }

    protected abstract void OnSave(ClickEvent evt);

    protected abstract void OnCancel();

    protected void RegisterButtonCallbacks(UIDocument document, string saveButtonName, string cancelButtonName)
    {
        var root = document.rootVisualElement;
        var saveButton = root.Q<Button>(saveButtonName);
        var cancelButton = root.Q<Button>(cancelButtonName);

        saveButton.RegisterCallback<ClickEvent>(OnSave);
        cancelButton.RegisterCallback<ClickEvent>(evt => OnCancel());
    }

    protected void SetTextboxText(VisualElement root, string labelName, string value)
    {
        var textField = root.Q<TextField>(labelName);
        if (textField != null)
        {
            textField.value = value;
        }
    }

    protected Dictionary<string, string> GetFormData(UIDocument document, List<string> fieldNames)
    {
        var root = document.rootVisualElement;
        var formData = new Dictionary<string, string>();

        foreach (var fieldName in fieldNames)
        {
            var textField = root.Q<TextField>(fieldName);
            if (textField != null)
            {
                formData[fieldName] = textField.text.Trim();
            }
        }

        return formData;
    }
}
