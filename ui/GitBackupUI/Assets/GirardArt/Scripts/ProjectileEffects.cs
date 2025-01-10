using System;
using System.Collections;
using UnityEngine;

public static class ProjectileLauncher
{
    public static IEnumerator LaunchProjectile(
        Func<Vector3, IEnumerator> muzzleFlashEffect,
        Func<Vector3, IEnumerator> shootProjectileEffect,
        Func<Vector3, IEnumerator> impactEffect,
        Vector3 source,
        Vector3 target)
    {
        if (muzzleFlashEffect != null)
            yield return muzzleFlashEffect(source);

        if (shootProjectileEffect != null)
            yield return shootProjectileEffect(target);

        if (impactEffect != null)
            yield return impactEffect(target);
    }


    // Sub-Effect Implementations

    private static IEnumerator MuzzleFlash(GameObject prefab, Vector3 position, float duration)
    {
        if (prefab == null) yield break;
        GameObject flash = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
        yield return new WaitForSeconds(duration);
        UnityEngine.Object.Destroy(flash);
    }

    private static IEnumerator ShootProjectile(GameObject prefab, Vector3 target, float speed)
    {
        if (prefab == null) yield break;
        GameObject projectile = UnityEngine.Object.Instantiate(prefab);
        Vector3 startPosition = projectile.transform.position;
        float distance = Vector3.Distance(startPosition, target);
        float duration = distance / speed;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            projectile.transform.position = Vector3.Lerp(startPosition, target, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        UnityEngine.Object.Destroy(projectile);
    }

    private static IEnumerator Impact(GameObject prefab, Vector3 position)
    {
        if (prefab == null) yield break;
        GameObject impact = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
        yield return new WaitForSeconds(0.5f);
        UnityEngine.Object.Destroy(impact);
    }
    /*
    public static IEnumerator ShootBlaster(GameObject muzzleFlashPrefab, Vector3 position)
    {
        
        return CoroutineRunner.Instance.StartCoroutine(ProjectileLauncher.LaunchProjectile(
            (pos) => MuzzleFlash(muzzleFlashPrefab, pos, 0.15f),
            (pos) => ShootProjectile(projectilePrefab, pos, 20f),
            (pos) => Impact(impactPrefab, pos),
            gunBarrelPosition,
            targetPosition
        ));
    }*/


}