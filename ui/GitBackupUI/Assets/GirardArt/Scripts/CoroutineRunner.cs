using UnityEngine;
public class CoroutineRunner : MonoBehaviour
{
    public static CoroutineRunner Instance { get; private set; }

    private void Awake()
    {
        
        if (Instance == null) Instance = this;

        else Destroy(gameObject);
    }
    public void DebugLog(string debugString){
        Debug.Log(debugString);
    }
}