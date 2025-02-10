
using UnityEngine;
public abstract class SpaceEncounterObserver : MonoBehaviour {
    public abstract bool VisualizeEffect(string effect, GameObject onBehlafOf);
    //public abstract void ShowFloatingActivePowers(GameObject obj);
}
interface IPausable
{
    public void Run();
    public void Pause();
    public bool IsRunning();
}