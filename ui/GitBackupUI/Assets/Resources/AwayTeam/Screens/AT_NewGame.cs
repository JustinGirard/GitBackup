using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;
public class GameScreenController : MonoBehaviour,IShowHide
{
    public enum MenuScreenType
    {
        MainMenu,
        PausedScreen,
        GameOverScreen,
        YouWinScreen,
    }

    [Header("Menu Screen Settings")]
    public MenuScreenType menuScreenType;

    public IATGameMode __encounterManager;
    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement buttonsContainer;
    private Dictionary<string,UnityEngine.UIElements.Button> buttonDict;

    private Label subjectLabel;
    private int count=0;
    void Awake()
    {
        // Get the UIDocument and root VisualElement
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Find the container for buttons
        buttonsContainer = root.Q<VisualElement>("buttons-container");
        buttonDict = new Dictionary<string, UnityEngine.UIElements.Button>();
        subjectLabel = root.Q<Label>("subject");

        // Clear existing buttons

    }

    void OnEnable()
    {

    }

    public System.Collections.IEnumerator WaitForScreenReady(System.Action callback)
    {
        yield return new WaitForEndOfFrame();
        callback?.Invoke();
    }

    [SerializeField]
    private bool __debugAutoPlayNewGame = false;
    public void AutoTest(){
        if (__debugAutoPlayNewGame && menuScreenType == MenuScreenType.MainMenu)
        {
            __debugAutoPlayNewGame = false; // Prevent repeated triggering
            Debug.Log("Debug: Auto-playing new game...");
            UnityEngine.UIElements.Button playButton = buttonDict["Play"];

            if (playButton != null)
            {
                //Debug.Log("Launching Play Mode.");
                __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();
                __encounterManager.Initalize();
                __encounterManager.Begin();
                NavigateTo("AT_SpaceCombatEncounterScreen");                
                //playButton.SendEvent(new ClickEvent()); // Programmatically trigger the click
                //Debug.Log("Debug: Invoked 'Play' button for auto-play.");
            }
        }

    }
    public void Show()
    {
        uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        CreateButtonsForScreen(menuScreenType);
        SetScreenState(menuScreenType);
        //AutoTest();
        StartCoroutine(WaitForScreenReady(() => AutoTest()));
    }
    public void Hide()
    {
        uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void CreateButtonsForScreen(MenuScreenType type)
    {
        __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();

        count++;
        buttonsContainer.Clear();
        buttonDict.Clear();
        switch (type)
        {
            case MenuScreenType.MainMenu:
                AddButton("Play", () =>
                {
                    //WaitForScreenReady(() => {
                        Debug.Log("Launching Play Mode.");
                        __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();
                        __encounterManager.Initalize();
                        __encounterManager.Begin();
                        NavigateTo("AT_SpaceCombatEncounterScreen");
                    //});
                });
                break;
            case MenuScreenType.YouWinScreen:
                int level = __encounterManager.GetLevel();
                level = level + 1;
                AddButton($"{count.ToString()}: Start Level Round {level}", () =>
                {
                    __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();
                    __encounterManager.Initalize();
                    __encounterManager.SetLevel(level);
                    __encounterManager.Begin();
                    NavigateTo("AT_SpaceCombatEncounterScreen");
                });
                AddButton("Quit to Menu.", () =>
                {
                    Debug.Log("Quit To Menu.");

                    __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();
                     __encounterManager.End();
                    NavigateTo("AT_NewGame");
                });
                break;
            case MenuScreenType.PausedScreen:
                AddButton("Resume.", () =>
                {
                    Debug.Log("Resume");
                    __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();
                    NavigateTo("AT_SpaceCombatEncounterScreen");
                    __encounterManager.Run();
                });
                AddButton("Quit to Menu", () =>
                {
                    Debug.Log("Quit.");
                    __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();
                     __encounterManager.End();
                    NavigateTo("AT_NewGame");
                });
                AddButton("Quit Application", () =>
                {
                    Application.Quit();
                });
                break;

            case MenuScreenType.GameOverScreen:
                AddButton("Restart", () =>
                {
                    __encounterManager.Initalize();
                    __encounterManager.SetLevel(1);                    
                    __encounterManager.Begin();
                    NavigateTo("AT_SpaceCombatEncounterScreen");
                });
                break;

            default:
                Debug.LogWarning("Unsupported menu screen type.");
                break;
        }
    }

    private void AddButton(string text, System.Action onClickAction)
    {
        // Create and configure the button
        UnityEngine.UIElements.Button button = new UnityEngine.UIElements.Button
        {
            text = text
        };

        // Attach the event handler
        button.clicked += onClickAction;

        // Add the button to the container
        buttonsContainer.Add(button);
        buttonDict[text] = button;
    }

    private void NavigateTo(string screenName)
    {
        GameObject navigatorObject = GameObject.Find("AwayTeam");
        NavigationManager navigationManager = navigatorObject.GetComponent<NavigationManager>();
        navigationManager.NavigateTo(screenName, true);
    }

    /// <summary>
    /// Sets the screen state text.
    /// </summary>
    /// <param name="type">The menu screen type.</param>
    public void SetScreenState(MenuScreenType type)
    {
        subjectLabel.text = type switch
        {
            MenuScreenType.MainMenu => "Main Menu",
            MenuScreenType.PausedScreen => "Game Paused",
            MenuScreenType.GameOverScreen => "Game Over Screen",
            MenuScreenType.YouWinScreen => "Round Won",
            _ => "Unknown Menu Screen"
        };
    }
}
