using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UnitProjectile : MonoBehaviour
{
    [Header("Projectile Stages")]
    bool isEnabled = true;

    public GameObject muzzleFlashPrefab;
    public GameObject impactPrefab;
    public GameObject projectilePrefab;  // Projectile in-flight prefab


    public float projectileSpeed = 20f;
    public float projectileLifetime = 5f;
    public Vector3 targetPosition;
    private GameObject inFlightProjectile;  // Reference to instantiated projectile in-flight

    private GameObject _muzzle = null;  // Reference to instantiated projectile in-flight

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    [ContextMenu("Trigger Muzzle Flash")]
    public void TriggerMuzzleFlash(GameObject mount)
    {
        if (muzzleFlashPrefab != null)
        {
                if (_muzzle != null)
                {
                    #if UNITY_EDITOR
                        DestroyImmediate(_muzzle);  // Muzzle flash lasts briefly
                    #else
                        Debug.Log("DESTROYING MUZZLE FLASH");
                        Destroy(muzzle);  // Muzzle flash lasts briefly
                    #endif
                }
                _muzzle = Instantiate(muzzleFlashPrefab, transform.position, transform.rotation, transform);
                _muzzle.transform.parent = mount.transform;
                _muzzle.transform.forward = mount.transform.forward;
                _muzzle.transform.position = mount.transform.position;
                ParticleSystem ps = _muzzle.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                }
                else
                {
                    Debug.LogWarning("Muzzle flash prefab does not have a ParticleSystem.");
                }            
        }
        else
        {
            Debug.LogWarning("Muzzle flash prefab is missing.");
        }
    }

    [ContextMenu("Launch Projectile")]
    public void LaunchProjectile(GameObject mount)
    {
        if (rb == null)
        {
            Debug.LogWarning("Rigidbody is missing. Cannot launch.");
            return;
        }

        Vector3 direction = (targetPosition - transform.position).normalized;
        rb.velocity = direction * projectileSpeed;

        TriggerMuzzleFlash(mount);

        if (projectilePrefab != null)
        {
            inFlightProjectile = Instantiate(projectilePrefab, transform.position, transform.rotation);
            inFlightProjectile.transform.parent = transform;
        }
        else
        {
            Debug.LogWarning("Projectile prefab is missing.");
        }

        Invoke(nameof(TriggerImpact), projectileLifetime);
    }

    [ContextMenu("Trigger Impact")]
    public void TriggerImpact()
    {
        if (impactPrefab != null)
        {
            Instantiate(impactPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogWarning("Impact prefab is missing.");
        }
        if (inFlightProjectile != null)
        {
            Destroy(inFlightProjectile);  // Clean up in-flight projectile visual
        }
        Destroy(gameObject);  // Destroy the projectile itself after impact
    }

    public bool IsEnabled()
    {
        return isEnabled;
    }
    public void Disable()
    {
        isEnabled = false;
    }
    
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        FXV.Shield shield = other.GetComponent<FXV.Shield>();
        if (shield != null)
        {
            shield.OnHit(transform.position, transform.forward, 1.0f, 0.5f);
            // (Vector3 hitPos, Vector3 hitNormal, float hitScale, float hitDuration)
            Vector3 hitNormal = (transform.position - other.transform.position).normalized;
            shield.OnHit(hitPos:transform.position, 
                        hitNormal:hitNormal, 
                        hitScale:4.0f, 
                        hitDuration:1.5f);
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        FXV.Shield shield = collision.collider.GetComponent<FXV.Shield>();
        if (shield != null)
        {
            Vector3 hitNormal = (transform.position - collision.transform.position).normalized;
            shield.OnHit(hitPos:transform.position, 
                        hitNormal:hitNormal, 
                        hitScale:4.0f, 
                        hitDuration:1.5f);
            Destroy(gameObject);
        }
    }
}
