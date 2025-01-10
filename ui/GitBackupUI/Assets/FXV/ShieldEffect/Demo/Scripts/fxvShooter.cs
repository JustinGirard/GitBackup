using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FXV.ShieldDemo
{
    public class fxvShooter : MonoBehaviour
    {
        public GameObject[] guns;
        public GameObject bulletPrefab;
        public GameObject bigBulletPrefab;

        public AudioClip[] shootClips;

        public float manualShootDelay = 0.1f;

        public bool autoShoot = false;
        public bool onlyWhenShieldActive = true;
        public float autoShootRate = 1.0f;
        public float autoShootRandomness = 0.5f;
        public float autoShootDispersion = 15.0f;

        private int currentGun = 0;

        private Vector3 baseShootDir;
        private float timeToNextShoot = 0.5f;

        FXV.Shield shield = null;

        AudioSource shootSound;

        void Start()
        {
            baseShootDir = transform.forward;

            shootSound = GetComponent<AudioSource>();

            if (onlyWhenShieldActive)
            {
                if (transform.parent)
                {
                    shield = transform.parent.GetComponentInChildren<FXV.Shield>();
                    if (shield == null && transform.parent.parent)
                    {
                        shield = transform.parent.parent.GetComponentInChildren<FXV.Shield>();
                    }
                }
            }
        }

        public void SetAutoShooting(bool val)
        {
            autoShoot = val;
        }

        void Update()
        {
            if (autoShoot)
            {
                timeToNextShoot -= Time.deltaTime;
                if (timeToNextShoot <= 0.0f)
                {
                    if (!shield || shield.GetIsShieldActive())
                    {
                        baseShootDir = transform.forward;

                        int retryCount = 50;
                        Vector3 dir = Quaternion.Euler(Random.Range(-autoShootDispersion, autoShootDispersion), Random.Range(-autoShootDispersion, autoShootDispersion), Random.Range(-autoShootDispersion, autoShootDispersion)) * baseShootDir;
                        RaycastHit rhi;

                        while (retryCount > 0 && (!Physics.Raycast(gameObject.transform.position, dir, out rhi, 10000.0f, ~0, QueryTriggerInteraction.Ignore) || !rhi.collider.gameObject.GetComponentInParent<Shield>()))
                        {
                            dir = Quaternion.Euler(Random.Range(-autoShootDispersion, autoShootDispersion), Random.Range(-autoShootDispersion, autoShootDispersion), Random.Range(-autoShootDispersion, autoShootDispersion)) * baseShootDir;
                            retryCount--;
                        }
                        
                        gameObject.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                        Shoot(dir, Random.Range(0, 100) > 75 ? bigBulletPrefab : bulletPrefab);
                    }

                    timeToNextShoot = 1.0f / (autoShootRate + Random.Range(-autoShootRandomness, autoShootRandomness));
                }
            }
            else
            {
                timeToNextShoot -= Time.deltaTime;
                if (timeToNextShoot <= 0.0f)
                {
                    timeToNextShoot = 0.0f;

                    Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2, 0.0f));
                    RaycastHit rhi;

                    if (Physics.Raycast(ray, out rhi, 10000.0f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        Vector3 dirShip = rhi.point - transform.position;
                        dirShip.Normalize();
                        gameObject.transform.rotation = Quaternion.LookRotation(dirShip, Vector3.up);

                        if (!EventSystem.current.IsPointerOverGameObject())
                        {
                            if (Input.GetMouseButton(0))
                            {
                                Vector3 dirBullet = rhi.point - guns[currentGun].transform.position;
                                dirBullet.Normalize();

                                Shoot(dirBullet, Input.GetKey(KeyCode.LeftControl) ? bigBulletPrefab : bulletPrefab);

                                timeToNextShoot = manualShootDelay;
                            }
                        }
                    }
                }
            }
        }

        void Shoot(Vector3 dir, GameObject prefab)
        {
            GameObject bullet = GameObject.Instantiate(prefab, guns[currentGun].transform.position, guns[currentGun].transform.rotation);

            bullet.GetComponent<fxvBullet>().Shoot(dir);

            if (shootSound && shootClips != null && shootClips.Length > 0)
            {
                shootSound.PlayOneShot(shootClips[Random.Range(0, shootClips.Length)]);
            }
            currentGun++;
            if (currentGun >= guns.Length)
            {
                currentGun = 0;
            }
        }
    }

}