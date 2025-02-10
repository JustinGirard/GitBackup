using TMPro;
using UnityEngine;

public interface IUINotify
{
    void Show();
    void Hide();
    void SetText(string text);
    string GetText();
}

public class UINotification : MonoBehaviour, IUINotify
{
    private TextMeshProUGUI textComponent;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        textComponent = GetComponentInChildren<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    public void SetText(string text)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
        }
    }

    public string GetText()
    {
        return textComponent != null ? textComponent.text : string.Empty;
    }
}
