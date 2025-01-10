using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FXV.ShieldDemo
{
    public class fxvDemoAnimationEvents : MonoBehaviour
    {
        void Start()
        {

        }

        void Update()
        {

        }

        public void DisableAllShieldsNoAnim()
        {
            fxvDemo demo = FindAnyObjectByType<fxvDemo>();
            if (demo)
            {
                demo.DisableShiedls(false);
            }

            fxvShooter[] shooters = GameObject.FindObjectsByType<fxvShooter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < shooters.Length; i++)
            {
                shooters[i].enabled = false;
            }
        }

        public void EnableAllShieldsNoAnim()
        {
            fxvDemo demo = FindAnyObjectByType<fxvDemo>();
            if (demo)
            {
                demo.EnableShields(false);
            }

            fxvShooter[] shooters = GameObject.FindObjectsByType<fxvShooter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < shooters.Length; i++)
            {
                shooters[i].enabled = true;
            }
        }

        public void DisableAllShields()
        {
            fxvDemo demo = FindAnyObjectByType<fxvDemo>();
            if (demo)
            {
                demo.DisableShiedls();
            }

            fxvShooter[] shooters = GameObject.FindObjectsByType<fxvShooter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < shooters.Length; i++)
            {
                shooters[i].enabled = false;
            }
        }

        public void EnableAllShields()
        {
            fxvDemo demo = FindAnyObjectByType<fxvDemo>();
            if (demo)
            {
                demo.EnableShields();
            }

            fxvShooter[] shooters = GameObject.FindObjectsByType<fxvShooter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < shooters.Length; i++)
            {
                shooters[i].enabled = true;
            }
        }

        public void StopShooters()
        {
            fxvShooter[] shooters = GameObject.FindObjectsByType<fxvShooter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < shooters.Length; i++)
            {
                shooters[i].enabled = true;
            }
        }

        public void StartCameraShooting()
        {
            fxvShooter shooter = GetComponentInChildren<fxvShooter>();
            shooter.SetAutoShooting(true);
        }

        public void StopCameraShooting()
        {
            fxvShooter shooter = GetComponentInChildren<fxvShooter>();
            shooter.SetAutoShooting(false);
        }

        public void ShowNextShieldType()
        {
            fxvDemo demo = FindAnyObjectByType<fxvDemo>();
            if (demo)
            {
                demo.NextShieldType();
            }
        }

        public void HideUI()
        {
            fxvDemo demo = FindAnyObjectByType<fxvDemo>();
            if (demo)
            {
                demo.HideUI();
            }
        }
    }
}