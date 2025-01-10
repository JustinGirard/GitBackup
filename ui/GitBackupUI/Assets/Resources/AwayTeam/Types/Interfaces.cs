
using UnityEngine;
public abstract class SpaceEncounterObserver : MonoBehaviour {
    public abstract bool VisualizeEffect(string effect, GameObject onBehlafOf);
}
interface IPausable
{
    public void Run();
    public void Pause();
    public bool IsRunning();
}