using UnityEngine;
using UnityEngine.UI;

public class PowerDemo : MonoBehaviour
{
    public string[] powers = { "destroy", "bullet-small", "bullet-large", "laser" };
    public float sphereLifetime = 5f;
    public float sphereSpeed = 20f;
    public float sphereSize = 0.02f;
    public GameObject weaponMount;
    private int currentPowerIndex = 0;
    private Text powerLabel;

    void Start()
    {
        // Create UI label
        // MD_Preferences pref = Resources.Load<MD_Preferences>(PREF_NAME);
        GameObject canvasObject = new GameObject("PowerCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        GameObject labelObject = new GameObject("PowerLabel");
        labelObject.transform.SetParent(canvasObject.transform);

        powerLabel = labelObject.AddComponent<Text>();
        //powerLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        powerLabel.fontSize = 24;
        powerLabel.alignment = TextAnchor.LowerLeft;
        powerLabel.rectTransform.anchoredPosition = new Vector2(10, 10);
        powerLabel.rectTransform.sizeDelta = new Vector2(300, 50);

        UpdatePowerLabel();
    }

    void Update()
    {
        // Cycle Power (Right Click)
        if (Input.GetMouseButtonDown(1))
        {
            currentPowerIndex = (currentPowerIndex + 1) % powers.Length;
            UpdatePowerLabel();
        }

        // Shoot (Left Click)
        if (Input.GetMouseButtonDown(0))
        {
            ShootSphere();
        }
    }

    void UpdatePowerLabel()
    {
        powerLabel.text = $"Selected power: {powers[currentPowerIndex]}";
    }

    void ShootSphere()
    {
        UnitProjectile bam1Prefab = Resources.Load<UnitProjectile>("Bam1");
        GameObject sphere = GameObject.Instantiate(bam1Prefab.gameObject);

        UnitProjectile bam1Instance = sphere.GetComponent<UnitProjectile>();
        bam1Instance.TriggerMuzzleFlash(weaponMount);

        sphere.transform.position = transform.position + transform.forward * 2f;
        ///sphere.transform.localScale = Vector3.one * sphereSize;
        sphere.name = powers[currentPowerIndex];

        Rigidbody rb = sphere.AddComponent<Rigidbody>();
        rb.useGravity = false;

        Collider collider = sphere.GetComponent<Collider>();
        collider.isTrigger = false;

        // Launch sphere forward
        rb.velocity = transform.forward * sphereSpeed;

        // Destroy after lifetime
        StartCoroutine(DestroySphereAfterTime(sphere, sphereLifetime));
    }

    private System.Collections.IEnumerator DestroySphereAfterTime(GameObject sphere, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(sphere);
    }
}
