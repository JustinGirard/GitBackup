/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Include example event handling for
// - TextField named TextField
// - Slider name Slider
// - DropdownField named DropdownField (please dynamically add some items, and event handling)
// - ListView named ListView (please dynamically add some example labels)
// - ListView named ListView (please dynamically add some example labels)



public class MainMenuScript : MonoBehaviour
{
    private UIDocument _uidoc;
    private Button _butt;

    // Start is called before the first frame update
    void Awake()
    {
        _uidoc = GetComponent<UIDocument>();
        _butt = _uidoc.rootVisualElement.Q("LoadReposButton") as Button;

        _butt.RegisterCallback<ClickEvent>(LoadReposEvent);
    }
    private void OnDisable()
    {
        _butt.UnregisterCallback<ClickEvent>(LoadReposEvent);
    }

    private void LoadReposEvent(ClickEvent click) {
        Debug.Log("I have pressed the button");
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
*/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuScript : MonoBehaviour
{
    private UIDocument _uidoc;
    private Button _butt;
    private Button _backupScreenButton;

    private TextField _textField;
    private Slider _slider;
    private DropdownField _dropdownField;
    private ListView _listView;

    private UIDocument _backupScreen;

    // Start is called before the first frame update
    void Awake()
    {
        // Retrieve the root visual element from the UIDocument
        _uidoc = GetComponent<UIDocument>();
        _backupScreen = GameObject.Find("UIBackupScreen").GetComponent<UIDocument>();

        // Example Button Handling
        _butt = _uidoc.rootVisualElement.Q<Button>("LoadReposButton");
        //if (_butt != null)
        //{
            _butt.RegisterCallback<ClickEvent>(LoadReposEvent);
        //}

        // TextField Handling
        _textField = _uidoc.rootVisualElement.Q<TextField>("TextField");
        //if (_textField != null)
        //{
            _textField.RegisterCallback<ChangeEvent<string>>(TextFieldChangedEvent);
        //}

        // Slider Handling
        _slider = _uidoc.rootVisualElement.Q<Slider>("Slider");
        //if (_slider != null)
        //{
            _slider.RegisterCallback<ChangeEvent<float>>(SliderChangedEvent);
        //}

        // DropdownField Handling
        _dropdownField = _uidoc.rootVisualElement.Q<DropdownField>("DropdownField");
        //if (_dropdownField != null)
        //{
            List<string> dropdownItems = new List<string> { "Option 1", "Option 2", "Option 3" };
            _dropdownField.choices = dropdownItems;
            _dropdownField.RegisterCallback<ChangeEvent<string>>(DropdownFieldChangedEvent);
        //}

        // ListView Handling
        _listView = _uidoc.rootVisualElement.Q<ListView>("ListView");
        if (_listView != null)
        {
            InitializeListView();
        }



        VisualElement root = _uidoc.rootVisualElement;

        // Iterate over each child element in the root
        foreach (var element in root.Children())
        {
            // Print the type and name of the element (name could be null)
            Debug.Log($"Element Type: {element.GetType()}, Name: {element.name}");
        }

        _uidoc = GetComponent<UIDocument>();
        ListAllElements(_uidoc.rootVisualElement);


        //_backupScreenButton = GameObject.Find("BackupScreenButton").GetComponent<Button>();
        _backupScreenButton = _uidoc.rootVisualElement.Q<Button>("BackupScreenButton");
        // var clientCards = root.Query<Button>().Class("ClientCard").ToList();
        _backupScreenButton.RegisterCallback<ClickEvent>(NavBackupScreen);
        _backupScreen.rootVisualElement.style.display = DisplayStyle.None;
    }


    /// <summary>
    ///
    /// 
    /// </summary>
    /// <param name="evt"></param>

    void ListAllElements(VisualElement root, int depth = 0)
    {
        // Print the current element's type and name, with indentation based on depth
        Debug.Log(new string(' ', depth * 2) + $"Element Type: {root.GetType()}, Name: {root.name}");

        // Recursively call this method for each child element
        foreach (var child in root.Children())
        {
            ListAllElements(child, depth + 1);  // Increase the depth level for indentation
        }
    }

    // Method to switch from Main Menu to Game Screen


private void NavBackupScreen(ClickEvent evt)
    {
        Debug.Log("Switched to Game Screen");
        //// Hide the Main Menu document
        _uidoc.rootVisualElement.style.display = DisplayStyle.None;

        // Show the Game Screen document
        _backupScreen.rootVisualElement.style.display = DisplayStyle.Flex;
        
        Debug.Log("Switched to Game Screen");
    }

    /// <summary>
    /// BackupScreenButton
    /// </summary>
    /// <param name="click"></param>

    // Example event handler for the button
    private void LoadReposEvent(ClickEvent click)
    {
        Debug.Log("I have pressed the button");
    }

    // Example event handler for the TextField
    private void TextFieldChangedEvent(ChangeEvent<string> evt)
    {
        Debug.Log("TextField value changed: " + evt.newValue);
    }

    // Example event handler for the Slider
    private void SliderChangedEvent(ChangeEvent<float> evt)
    {
        Debug.Log("Slider value changed: " + evt.newValue);
    }

    // Example event handler for the DropdownField
    private void DropdownFieldChangedEvent(ChangeEvent<string> evt)
    {
        Debug.Log("DropdownField selection changed: " + evt.newValue);
    }

    // Initialize and populate the ListView
    private void InitializeListView()
    {
        List<string> listViewItems = new List<string> { "Item 1", "Item 2", "Item 3", "Item 4" };

        Func<VisualElement> makeItem = () => new Label();
        Action<VisualElement, int> bindItem = (e, i) =>
        {
            (e as Label).text = listViewItems[i];
        };

        _listView.itemsSource = listViewItems;
        _listView.makeItem = makeItem;
        _listView.bindItem = bindItem;
        _listView.fixedItemHeight = 45;
        _listView.selectionType = SelectionType.Single;

        // Register event for selection change
        _listView.onSelectionChange += obj =>
        {
            Debug.Log("ListView item selected: " + obj);
        };
    }

    // Unregister callbacks when the script is disabled
    private void OnDisable()
    {
        if (_butt != null) _butt.UnregisterCallback<ClickEvent>(LoadReposEvent);
        if (_textField != null) _textField.UnregisterCallback<ChangeEvent<string>>(TextFieldChangedEvent);
        if (_slider != null) _slider.UnregisterCallback<ChangeEvent<float>>(SliderChangedEvent);
        if (_dropdownField != null) _dropdownField.UnregisterCallback<ChangeEvent<string>>(DropdownFieldChangedEvent);
        if (_listView != null) _listView.onSelectionChange -= obj => Debug.Log("ListView item selected: " + obj);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
