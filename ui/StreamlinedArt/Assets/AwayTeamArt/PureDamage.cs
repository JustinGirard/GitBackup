/*
using UnityEngine;
using RayFire;
using UnityEngine;
using RayFire;
using System.Collections.Generic;

public class PureDamage : MonoBehaviour 
{
    public float delay = 1.0f; // Delay before applying damage
    public float damageValue = 50f; // Damage to apply
    public float explosionRadius = 1.0f; // Radius of the explosion
    public List<GameObject> spheres; // Sphere used to determine damage point
    private float timer = 0.0f;
    private int fractureLevel = 0;
    private bool hasDemolished = false;
    public bool allColliders = false;
    public int fragmentNumber = 10;
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= delay && !hasDemolished && spheres.Count >0)
        {
            hasDemolished = true;
            ApplyDamage();
        }
    }

    private void ApplyDamage()
    {
        if (spheres == null)
        {
            Debug.LogWarning("No sphere assigned for damage calculation.");
            return;
        }
        // Prepare args
        List<GameObject> listOfImpacts = new List<GameObject>(spheres);;
        Collider sphereCollider = spheres[0].GetComponent<SphereCollider>();
        float sphereRadius = sphereCollider.bounds.extents.x;

        // Do Damage
        GameObject damagedReincarnation = MeshEffect.DoSphereDamage(gameObject,spheres[0],fragmentNumber,sphereRadius,fractureLevel,allColliders );        
        listOfImpacts.Remove(listOfImpacts[0]);
        if (damagedReincarnation == null)
        {
            Debug.LogError("No New Mesh Returned");
            return;
        }        
        // Attach Pure Damage Object
        PureDamage furtherPhysicalTrauma = damagedReincarnation.GetComponent<PureDamage>() ;
        if (furtherPhysicalTrauma == null)
        {
            Debug.Log("Adding new PureDamage");            
            furtherPhysicalTrauma = damagedReincarnation.AddComponent<PureDamage>();
            furtherPhysicalTrauma.delay = delay;
            furtherPhysicalTrauma.damageValue = damageValue;      // Pass the same damage value
            furtherPhysicalTrauma.explosionRadius = sphereRadius; // Use sphere radius as explosion radius
            furtherPhysicalTrauma.spheres = listOfImpacts;  
            furtherPhysicalTrauma.timer = 0.0f;
            furtherPhysicalTrauma.hasDemolished = false;
            furtherPhysicalTrauma.fractureLevel = 1; // You have been fractured once already
        }
        else
        {
            Debug.Log("Using Current PureDamage");            
            furtherPhysicalTrauma.spheres = listOfImpacts;  
            furtherPhysicalTrauma.timer = 0.0f;
            furtherPhysicalTrauma.hasDemolished = false;
            furtherPhysicalTrauma.fractureLevel = 0; // You have been fractured once already
        }

    }
}
*/

using UnityEngine;
using RayFire;
using System.Collections.Generic;

public class PureDamage : MonoBehaviour 
{
    public float delay = 1.0f;           // Delay before applying damage
    public float damageValue = 50f;      // Damage to apply
    public float explosionRadius = 1.0f; // Radius of the explosion
    public bool allColliders = false;
    public int fragmentNumber = 10;
    private int fractureLevel = 0;
    public float explosionForce = 200f;

    /*
    public void HandleCollisionOrTrigger(GameObject gameObject, GameObject other, Collider collider, Collision collision)
    {
        UnitProjectile up = other.gameObject.GetComponent<UnitProjectile>();
        if (up != null  && up.IsEnabled())
        {
            Debug.Log($"Child Smash Detected with {other.gameObject.name}");
            up.Disable();
            ApplyDamage(collider);
            Destroy(other.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Laser-like projectiles (passing through)
        UnitProjectile up = other.gameObject.GetComponent<UnitProjectile>();
        if (up != null  && up.IsEnabled())
        {
            Debug.Log($"Trigger Detected with {other.gameObject.name}");
            up.Disable();
            ApplyDamage(other);
            Destroy(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Bouncing physical projectiles
        UnitProjectile up = collision.gameObject.GetComponent<UnitProjectile>();
        if (up != null && up.IsEnabled())
        {
            Debug.Log($"Collision Detected with {collision.gameObject.name}");
            up.Disable();
            ApplyDamage(collision.collider);
            Destroy(collision.gameObject);
        }
    }*/
        public bool isColliding = false;
        public void HandleProjectileInteraction( Collider collider , string interactionType = "Interaction")
        {
            if (isColliding == false)
            {
                isColliding = true;
                try
                {
                    UnitProjectile up = collider.gameObject.GetComponent<UnitProjectile>();
                    if (up != null && up.IsEnabled())
                    {
                        // Debug.Log($"{interactionType} Detected with {collider.name}");
                        up.Disable();
                        ApplyDamageToMe(collider);
                        Destroy(collider.gameObject);
                    }
                }
                finally
                {
                   isColliding = false; 
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleProjectileInteraction( other, "Trigger");
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleProjectileInteraction(collision.collider, "Collision");
        }

        public void HandleCollisionOrTrigger(GameObject gameObject, GameObject other, Collider collider, Collision collision)
        {
            HandleProjectileInteraction(collider, "Child Smash");
        }


    private void ApplyDamageToMe(Collider sphereCollider)
    {
        if (sphereCollider == null)
        {
            Debug.LogWarning("No sphere assigned for damage calculation.");
            return;
        }

        float sphereRadius = sphereCollider.bounds.extents.x;
        Vector3 collisionPoint = sphereCollider.ClosestPoint(transform.position);

        // Apply damage using MeshEffect
        GameObject damagedReincarnation = MeshEffect.DoSphereDamage(
            gameObject, 
            sphereCollider.gameObject, 
            fragmentNumber, 
            //sphereRadius, 
            allColliders,
            explosionForce,
            layersMax : 1
        );

        if (damagedReincarnation == null)
        {
            Debug.LogError("No New Mesh Returned from Damage");
            return;
        }

        // Transfer Damage Logic to Reincarnated Object
        PureDamage furtherPhysicalTrauma = damagedReincarnation.GetComponent<PureDamage>();
        if (furtherPhysicalTrauma == null)
        {
            Debug.Log("Adding new PureDamage to reincarnated object.");
            furtherPhysicalTrauma = damagedReincarnation.AddComponent<PureDamage>();
            furtherPhysicalTrauma.delay = delay;
            furtherPhysicalTrauma.damageValue = damageValue;
            furtherPhysicalTrauma.explosionRadius = sphereRadius;
            furtherPhysicalTrauma.fragmentNumber = fragmentNumber;
        }

    }
}
