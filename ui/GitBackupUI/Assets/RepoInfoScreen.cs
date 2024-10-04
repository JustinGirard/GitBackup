using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RepoInfoScreen : MonoBehaviour, NavigationManager.ICanInitalize
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnEnable()
    {
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        var root = GetComponent<UIDocument>().rootVisualElement;

        var navigateElement = root.Q<Button>("UNDEFINED"); // TODO initalize null
        navigateElement = root.Q<Button>("Back");
        navigateElement.RegisterCallback<ClickEvent>(evt => navigationManager.NavigateTo("RepoListScreen"));
    }

    public void InitData(Dictionary<string, string> dataframe)
    {
        //throw new System.NotImplementedException();
    }

    public void Refresh()
    {
        //throw new System.NotImplementedException();
    }
}
