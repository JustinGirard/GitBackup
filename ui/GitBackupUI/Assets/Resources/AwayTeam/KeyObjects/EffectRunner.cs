using System.Collections;
using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
/*
public static class QuickSema
{
    private static readonly ConcurrentDictionary<string, object> _locks = new ConcurrentDictionary<string, object>();

    public static bool TryAcquireLock(string id)
    {
        return _locks.TryAdd(id, new object());
    }

    public static void ReleaseLock(string id)
    {
        _locks.TryRemove(id, out _);
    }

    public static bool IsLocked(string id)
    {
        return _locks.ContainsKey(id);
    }
}*/

public static class Sema
{
    private static readonly ConcurrentDictionary<string, DateTime> _locks = new ConcurrentDictionary<string, DateTime>();
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static bool TryAcquireLock(string id)
    {
        //Debug.Log($"Aquiring {id}");
        var now = DateTime.UtcNow;

        bool success = _locks.AddOrUpdate(
            id,
            key => now, // Add new lock
            (key, timestamp) => (now - timestamp) > DefaultTimeout ? now : timestamp // Replace expired lock
        ) == now; // Return true if the current attempt succeeded
        //Debug.Log($"Aquired? {id}:{success}");
        return success;
    }

    public static void ReleaseLock(string id)
    {
        //Debug.Log($"Removing? {id}");
        _locks.TryRemove(id, out _);
    }
    //Instantiate( Resources.Load<GameObject>(PrefabPath.BoltPath));

    public static bool IsLocked(string id)
    {
        return _locks.TryGetValue(id, out var timestamp) && (DateTime.UtcNow - timestamp) <= DefaultTimeout;
    }
}


public static class EffectHandler
{

    public static IEnumerator ShootMissileAt(GameObject missilePrefab, GameObject explosionPrefab, int number, float duration, float delay, float arcHeight, Vector3 source, Vector3 target)
    {
        for (int i = 0; i < number; i++)
        {
            GameObject missile = UnityEngine.Object.Instantiate(missilePrefab);
            missile.transform.position = source;

            // Random deviation can still be added
            float deviation = 0.4f;
            Vector3 randomizedTarget = target + new Vector3(
                UnityEngine.Random.Range(-deviation, deviation),
                UnityEngine.Random.Range(-deviation, deviation),
                UnityEngine.Random.Range(-deviation, deviation)
            );

            CoroutineRunner.Instance.StartCoroutine(MoveMissileWithArc(missile, explosionPrefab, source, randomizedTarget, duration, arcHeight));
            yield return new WaitForSeconds(0.01f);
        }
         yield return new WaitForSeconds(delay);
    }


    public static IEnumerator CreateShield(GameObject shieldPrefab, Vector3 source)
    {
        GameObject shield = InstantiateObject(shieldPrefab, source);
        if (shield == null)
        {
            Debug.LogError("Failed to create shield prefab.");
            yield break;
        }

        Material shieldMaterial = GetMaterialInstance(shield);
        if (shieldMaterial == null)
        {
            Debug.LogError("Shield prefab does not have a valid Renderer.");
            yield break;
        }

        float fadeInDuration = 0.25f;
        yield return FadeMaterialAlpha(shieldMaterial, 0f, 1f, fadeInDuration);

        float shieldDuration = 2.5f;
        yield return new WaitForSeconds(shieldDuration);

        float fadeOutDuration = 0.25f;
        yield return FadeMaterialAlpha(shieldMaterial, 1f, 0f, fadeOutDuration);

        UnityEngine.Object.Destroy(shield);
    }

    // Sub-Effect 1: Instantiate an object at a given position
    private static GameObject InstantiateObject(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return null;
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        instance.transform.position = position;
        return instance;
    }

    // Sub-Effect 2: Get a material instance for the given GameObject
    private static Material GetMaterialInstance(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return null;

        Material materialInstance = new Material(renderer.sharedMaterial);
        renderer.material = materialInstance;
        return materialInstance;
    }

    // Sub-Effect 3: Fade a material's alpha over time
    private static IEnumerator FadeMaterialAlpha(Material material, float startAlpha, float endAlpha, float duration)
    {
        if (material == null) yield break;

        Color color = material.color;
        color.a = startAlpha;
        material.color = color;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            color.a = Mathf.Lerp(startAlpha, endAlpha, t / duration);
            material.color = color;
            yield return null;
        }

        // Ensure final alpha
        color.a = endAlpha;
        material.color = color;
    }


    private static IEnumerator MoveMissileWithArc(GameObject missile, GameObject explosionPrefab, Vector3 source, Vector3 target, float duration, float arcHeight)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Calculate linear interpolation between source and target
            Vector3 linearPosition = Vector3.Lerp(source, target, t);

            // Add parabolic arc (up and down)
            float heightOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            linearPosition.y += heightOffset;

            missile.transform.position = linearPosition;

            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure the missile reaches the target position and trigger explosion
        missile.transform.position = target;
        UnityEngine.Object.Destroy(missile);
        /*
        // Explosion logic
        GameObject explodeeoooeee = UnityEngine.Object.Instantiate(explosionPrefab);
        explodeeoooeee.transform.position = target;

        Vector3 smalleee = new Vector3(0.1f, 0.1f, 0.1f) * 0.5f;
        Vector3 bigeee = new Vector3(1, 1, 1) * 1f;
        elapsedTime = 0f;
        float durationExplode = 0.7f;

        Transform sphereChild = explodeeoooeee.transform.Find("Sphere");
        Material explodeMaterial = sphereChild.GetComponent<Renderer>().material;
        Color initialColor = explodeMaterial.color;

        while (elapsedTime < durationExplode)
        {
            explodeeoooeee.transform.localScale = Vector3.Lerp(smalleee, bigeee, elapsedTime / durationExplode);
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / durationExplode);
            explodeMaterial.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        UnityEngine.Object.Destroy(explodeeoooeee);*/
        CoroutineRunner.Instance.StartCoroutine(SingleExplosion( explosionPrefab,  target,  0.5f,  1f));

    }
    public static IEnumerator SingleExplosion(GameObject explosionPrefab, Vector3 target, float sizeSmall, float sizeLarge)
    {
        // Explosion logic
        GameObject explodeeoooeee = UnityEngine.Object.Instantiate(explosionPrefab);
        explodeeoooeee.transform.position = target;

        Vector3 smalleee = new Vector3(0.1f, 0.1f, 0.1f) * sizeSmall;
        Vector3 bigeee = new Vector3(1, 1, 1) * sizeLarge;
        float elapsedTime = 0f;
        float durationExplode = 0.7f;

        Transform sphereChild = explodeeoooeee.transform.Find("Sphere");
        Material explodeMaterial = sphereChild.GetComponent<Renderer>().material;
        Color initialColor = explodeMaterial.color;

        while (elapsedTime < durationExplode)
        {
            explodeeoooeee.transform.localScale = Vector3.Lerp(smalleee, bigeee, elapsedTime / durationExplode);
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / durationExplode);
            explodeMaterial.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        UnityEngine.Object.Destroy(explodeeoooeee);
    }

    public static IEnumerator ShootBlasterAt(GameObject boltPrefab,GameObject explosionPrefab , int number, float duration, float delay, float maxDistance, Vector3 source, Vector3 target,float deviation)
    {
        for (int i = 0; i < number; i++)
        {
            GameObject bolt = UnityEngine.Object.Instantiate(boltPrefab);
            bolt.transform.position = source;

            // Apply random deviation to the target
            // float dist =  1f + UnityEngine.Random.Range(0, deviation*2);
            Vector3 randomizedTarget = target + new Vector3(
                UnityEngine.Random.Range(-deviation, deviation),
                UnityEngine.Random.Range(-deviation, deviation),
                UnityEngine.Random.Range(-deviation, deviation)
            );
            ////
            ///
            float forwardOffset = UnityEngine.Random.Range(0, deviation * 10);
            Vector3 travelDirection = (target - source).normalized;
            randomizedTarget += travelDirection * forwardOffset;
            // Start Lerp Coroutine for the bolt (concurrent)
            CoroutineRunner.Instance.StartCoroutine(MoveBoltWithLerp(bolt,explosionPrefab, source, randomizedTarget, duration));

            yield return new WaitForSeconds(0.01f);
            //break;
            // Wait before spawning the next bolt
        }
        yield return new WaitForSeconds(delay);

        yield return null; // Allow other processes to resume
    }

    private static IEnumerator MoveBoltWithLerp(GameObject bolt,GameObject explosionPrefab, Vector3 source, Vector3 target, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            bolt.transform.position = Vector3.Lerp(source, target, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null; // Wait for next frame
        }

        // Ensure the bolt reaches the target position and clean up
        //bolt.transform.position = target;
        UnityEngine.Object.Destroy(bolt);
        GameObject explodeeoooeee = UnityEngine.Object.Instantiate(explosionPrefab);
        explodeeoooeee.transform.position = bolt.transform.position ;
        
        //explodeeoooeee.transform.localScale = (new Vector3(1,1,1))*0.001f;
        Vector3 smalleee = new Vector3(0.1f,0.1f,0.1f)*0.5f;
        Vector3 bigeee = new Vector3(1,1,1)*0.5f;;
        elapsedTime = 0f;
        float durationExplode = 0.2f;
        Transform sphereChild = explodeeoooeee.transform.Find("Sphere");        
        Material explodeMaterial = sphereChild.GetComponent<Renderer>().material;        
        Color initialColor = explodeMaterial.color;        
        while (elapsedTime < durationExplode)
        {
            explodeeoooeee.transform.localScale = Vector3.Lerp(smalleee, bigeee, elapsedTime / durationExplode);
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / durationExplode);
            explodeMaterial.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);

            elapsedTime += Time.deltaTime;
            yield return null; // Wait for next frame
        }
        UnityEngine.Object.Destroy(explodeeoooeee);
        


        //bolt.transform.position = source;

    }



    public static IEnumerator Pulse(GameObject target, float duration, float velocity,float size,Vector3? finalScale)
    {
        Vector3 initialScale = target.transform.localScale;
        Vector3 concreteFinalScale = finalScale ?? target.transform.localScale; 
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float scaleMultiplier = 1 - Mathf.Sin(elapsedTime * velocity)*size;
            target.transform.localScale = initialScale *scaleMultiplier;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        target.transform.localScale = concreteFinalScale;
    }

    public static IEnumerator ShrinkTo(GameObject target, Vector3 targetScale, float duration)
    {
        Vector3 scaleDelta = (targetScale - target.transform.localScale) / duration;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            Vector3 frameDelta = scaleDelta * Time.deltaTime;
            target.transform.localScale += frameDelta;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    public static IEnumerator GrowTo(GameObject target, Vector3 targetScale, float duration)
    {
        return ShrinkTo(target, targetScale, duration); // Same logic, just descriptive
    }

    public static IEnumerator ElasticMove(GameObject target, Vector3 movementVector, float duration, float elasticity)
    {
        Vector3 accumulatedDelta = Vector3.zero;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float springFactor = Mathf.Sin(t * Mathf.PI * elasticity) * (1 - t); // Elastic oscillation
            Vector3 frameDelta = Vector3.Lerp(Vector3.zero, movementVector, Time.deltaTime / duration) + (movementVector * springFactor);
            target.transform.position += frameDelta;

            accumulatedDelta += frameDelta; // Adjust for stacked changes
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    public static IEnumerator Translate(GameObject target, Vector3 translationVector, float duration)
    {
        Vector3 translationDelta = translationVector / duration;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            Vector3 frameDelta = translationDelta * Time.deltaTime;
            target.transform.position += frameDelta;

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}



//
//
//



