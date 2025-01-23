using System;
using UnityEngine;

public class BindableCollisionHandler : MonoBehaviour 
{
    private System.Action<GameObject,Collider,Vector3,Vector3> collisionHandler;

    // Bind a single coroutine handler for both collisions and triggers
    public void BindCollisionHandler(System.Action<GameObject,Collider,Vector3,Vector3> handler)
    {
        collisionHandler = handler;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collisionHandler != null)
        {
            Vector3 collisionPoint;
            Vector3 normal;

            if (collision.contacts.Length > 0)
            {
                // Use the first contact point if available
                collisionPoint = collision.contacts[0].point;
                normal = collision.contacts[0].normal;
            }
            else
            {
                // Fallback to the position of the other collider
                collisionPoint = collision.collider.transform.position;
                normal = Vector3.up;
            }

            // Pass the collision point to the handler
            collisionHandler(this.gameObject, collision.collider, collisionPoint,normal);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collisionHandler != null)
        {
            Vector3 triggerPoint = other.transform.position;
            collisionHandler(this.gameObject, other, triggerPoint,Vector3.up);
        }
    }
}
