using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using UnityEngine.UIElements;

public class NavigationButton : MonoBehaviour
{
    private Button _button;
    private UIDocument _self;

    void Awake() {

        _self = GetComponent<UIDocument>();
        _button = _self.rootVisualElement.Q<Button>("NavigationButton") as Button;

        //_button.clicked += OnButtonClick;
        _button.RegisterCallback<ClickEvent>(OnButtonClick);
    }

    void OnButtonClick(ClickEvent evt)
    {
        
        Debug.Log("Button clicked at position: ");
    }
    /*
    void Start()
    {
        
    }
    void Update()
    {
        
    }*/

}


