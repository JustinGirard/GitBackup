using UnityEngine;
using RayFire;
using System.Collections.Generic;
using System.Net.Http.Headers;

interface IFragmentObserver
{
    public void InitFragment(RayfireRigid rayFrag, Collider iteractionProjectils);
    public bool ShouldSplitFragment(RayfireRigid rayFrag, Collider iteractionProjectils);
    public void OnHitFragment(RayfireRigid rayFrag, Collider iteractionProjectils);
    public bool ShouldDestroyFragment(RayfireRigid rayFrag, Collider iteractionProjectils);
    public void OnDestroyFragment(RayfireRigid rayFrag, Collider iteractionProjectils);

}

public class PureDamage : MonoBehaviour ,IFragmentObserver
{
    public float delay = 1.0f;           // Delay before applying damage
    public float damageValue = 10f;      // Damage to apply
    public float explosionRadius = 1.0f; // Radius of the explosion
    public bool allColliders = false;
    public int fragmentNumber = 10;
    private int fractureLevel = 0;
    public float explosionForce = 200f;
    public int layersMax = 1;
    public Material destructionMaterial;

    public Material GetMaterialFor(RayfireRigid rayFrag)
    {
        return destructionMaterial; 
    }
 
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

        GameObject damagedReincarnation = MeshEffect.DoSphereDamage(
            gameObject, 
            sphereCollider.gameObject, 
            fragmentNumber, 
            allColliders,
            explosionForce,
            layersMax : layersMax,
            fragmentObserver:this.GetComponent<IFragmentObserver>()
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
            furtherPhysicalTrauma.explosionRadius =  sphereCollider.bounds.extents.x;
            furtherPhysicalTrauma.fragmentNumber = fragmentNumber;
            furtherPhysicalTrauma.layersMax = layersMax;
        }

    }

    public void InitFragment(RayfireRigid rayFrag, Collider interactionProjectile)
    {
        Debug.Log($"INIT FOR {rayFrag.name}");
        DamagedChunk chunk = rayFrag.GetComponent<DamagedChunk>();
        if (chunk == null)
        {
            chunk = rayFrag.gameObject.AddComponent<DamagedChunk>();
            chunk.InitFragment(this, rayFrag, interactionProjectile);
        }
    }

    public bool ShouldSplitFragment(RayfireRigid rayFrag, Collider interactionProjectile)
    {
        DamagedChunk chunk = rayFrag.GetComponent<DamagedChunk>();
        if (chunk == null)
        {
            Debug.LogError($"No DamagedChunk found on {rayFrag.gameObject.name}");
            return false;
        }
        return chunk.ShouldSplitFragment(rayFrag, interactionProjectile);
    }

    public void OnHitFragment(RayfireRigid rayFrag, Collider interactionProjectile)
    {
        DamagedChunk chunk = rayFrag.GetComponent<DamagedChunk>();
        if (chunk != null)
        {
            chunk.OnHitFragment(rayFrag, interactionProjectile, damageValue);
        }
        else
        {
            Debug.LogError($"No DamagedChunk found on {rayFrag.gameObject.name}");
        }
    }

    public bool ShouldDestroyFragment(RayfireRigid rayFrag, Collider interactionProjectile)
    {
        DamagedChunk chunk = rayFrag.GetComponent<DamagedChunk>();
        if (chunk == null)
        {
            Debug.LogError($"No DamagedChunk found on {rayFrag.gameObject.name}");
            return false;
        }
        return chunk.ShouldDestroyFragment(rayFrag, interactionProjectile, layersMax);
    }

    public void OnDestroyFragment(RayfireRigid rayFrag, Collider interactionProjectile)
    {
        DamagedChunk chunk = rayFrag.GetComponent<DamagedChunk>();
        if (chunk != null)
        {
            chunk.OnDestroyFragment(rayFrag, interactionProjectile);
        }
        else
        {
            Debug.LogError($"No DamagedChunk found on {rayFrag.gameObject.name}");
        }
    }

    public class DamagedChunk : MonoBehaviour
    {
        public float health;
        public float maxHealth = 100f;
        public int layerMax = 3;
        private Material destructionMaterial;
        public virtual void InitFragment(PureDamage chunkCreator, RayfireRigid rayFrag, Collider interactionProjectile)
        {
            destructionMaterial = chunkCreator.GetMaterialFor(rayFrag);
            health = 100f;
            __UpdateColor(rayFrag);         
        }

        public virtual void ApplyDamage(float damage)
        {
            health -= damage;
        }

        public virtual bool IsDestroyed()
        {
            return health <= 0;
        }

        public virtual bool ShouldSplitFragment(RayfireRigid rayFrag, Collider interactionProjectile)
        {
            return IsDestroyed();
            //return false;
        }

        public virtual void OnHitFragment(RayfireRigid rayFrag, Collider interactionProjectile, float damageValue)
        {
            ApplyDamage(damageValue);
            __UpdateColor(rayFrag);                 
 
        }
        private void __UpdateColor(RayfireRigid rayFrag)
        {
            Renderer renderer = rayFrag.GetComponent<Renderer>();
            if (renderer != null)
            {
                UnitFragment uf = rayFrag.gameObject.GetComponent<UnitFragment>();
                float layerDepth = (float)uf.GetLayer();
                float otherLayersHealth = (layerMax-(layerDepth+1)) * maxHealth; 
                float thisLayerHealth = Mathf.Clamp01(health / maxHealth)*maxHealth;
                float colorMaxHealth = maxHealth * layerMax;
                float colorHealth = thisLayerHealth +otherLayersHealth;
                Debug.Log($"{layerDepth}/{layerMax}: ({thisLayerHealth}+{otherLayersHealth})->{colorHealth}f/{colorMaxHealth}f");
                float color = Mathf.Clamp01(colorHealth / colorMaxHealth);
                renderer.material.color = new Color(1f, color, color);
                //Debug.Log($"fragment HIT at layer{uf.GetLayer()}: {color}f/1f");
                StartCoroutine(OscillateColor(rayFrag, renderer, 0.1f));  // Pass amplitude                
            }     

        }
        bool runningOss = false;
        private System.Collections.IEnumerator  OscillateColor(RayfireRigid rayFrag, Renderer renderer, float amplitude)
        {
            float elapsedTime = 0f;
            float duration = 0.2f;
            if (runningOss == true)
                yield break;
            runningOss = true;
            try
            {
                while (elapsedTime < duration)
                {
                    UnitFragment uf = rayFrag.gameObject.GetComponent<UnitFragment>();
                    float layerDepth = (float)uf.GetLayer();
                    float otherLayersHealth = (layerMax - (layerDepth + 1)) * maxHealth;
                    float thisLayerHealth = Mathf.Clamp01(health / maxHealth) * maxHealth;
                    float colorMaxHealth = maxHealth * layerMax;
                    float colorHealth = thisLayerHealth + otherLayersHealth;
                    float color = Mathf.Clamp01(colorHealth / colorMaxHealth);
                    
                    float offset = Mathf.Sin(elapsedTime * Mathf.PI * 10f) * amplitude;
                    Color oscillatedColor = new Color(
                        Mathf.Clamp01(1f - offset),
                        Mathf.Clamp01(color - offset),
                        Mathf.Clamp01(color - offset)
                    );
                    renderer.material.color = oscillatedColor;
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                // Set final color after oscillation
                UnitFragment ufFinal = rayFrag.gameObject.GetComponent<UnitFragment>();
                float layerDepthFinal = (float)ufFinal.GetLayer();
                float otherLayersHealthFinal = (layerMax - (layerDepthFinal + 1)) * maxHealth;
                float thisLayerHealthFinal = Mathf.Clamp01(health / maxHealth) * maxHealth;
                float colorMaxHealthFinal = maxHealth * layerMax;
                float colorHealthFinal = thisLayerHealthFinal + otherLayersHealthFinal;
                float finalColor = Mathf.Clamp01(colorHealthFinal / colorMaxHealthFinal);
                renderer.material.color = new Color(1f, finalColor, finalColor);
            }
            finally
            {
                runningOss = false;
            }
        }

        public virtual bool ShouldDestroyFragment(RayfireRigid rayFrag, Collider interactionProjectile, int layersMax)
        {
            UnitFragment unitFrag = rayFrag.GetComponent<UnitFragment>();
            if(unitFrag.GetLayer() == layersMax && health <= 0)
            {
                return true;
            }
            return false;
        }

        public virtual void OnDestroyFragment(RayfireRigid rayFrag, Collider interactionProjectile)
        {
            float explosionForce = 1500f;
            float fragmentFraction = 5;
            bool doRecurse = false;
            List<RayfireRigid>  frags = MeshEffect.DoExplode(rayFrag.gameObject,explosionForce,fragmentFraction,doRecurse);
            /// destructionMaterial
            StartCoroutine(DisableCollidersAfterDelay(frags, 1f));
            foreach (RayfireRigid frag in frags)
            {
                Renderer renderer = frag.GetComponent<Renderer>();
                if (renderer != null && destructionMaterial != null)
                {
                    renderer.sharedMaterial = destructionMaterial;
                }
            }            
        }

        private static System.Collections.IEnumerator DisableCollidersAfterDelay(List<RayfireRigid> fragments, float delay)
        {
            yield return new WaitForSeconds(delay);
            foreach (RayfireRigid fragment in fragments)
            {
                Collider col = fragment.GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }        
        
    }

}


