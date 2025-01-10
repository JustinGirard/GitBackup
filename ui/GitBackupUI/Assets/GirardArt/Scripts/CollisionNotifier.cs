using UnityEngine;
using UnityEngine.UIElements;

public class UnitFragment : MonoBehaviour
{
    int fragmentLayer = 0; // Fragmentation proceeds in layers.
    public void SetLayer(int val)
    {
        fragmentLayer = val;
    }
    public int GetLayer()
    {
        return fragmentLayer;
    }
    private void OnCollisionEnter(Collision collision)
    {
        ForwardEventToParent(collision.gameObject, collision.collider, collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        ForwardEventToParent(other.gameObject, other, null);
    }

    private void ForwardEventToParent(GameObject other, Collider collider, Collision collision)
    {
        if (transform.parent != null)
        {
            var handler = transform.GetComponentInParent<PureDamage>();
            if (handler != null)
            {
                handler.HandleCollisionOrTrigger(gameObject, other, collider, collision);
            }
            else
                Debug.LogError($" {name} No PureDamage found above me. Cant trigger event.");
        }
    }
}
