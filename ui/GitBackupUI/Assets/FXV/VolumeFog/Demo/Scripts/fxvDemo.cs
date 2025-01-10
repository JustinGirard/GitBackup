using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace FXV.ShieldDemo
{
    public class fxvDemo : MonoBehaviour
    {
        [SerializeField]
        Text shieldToggleText;

        [SerializeField]
        GameObject[] shieldTypes;

        int currentType = 0;
        public void NextType(){}
        void Start()
        {
            shieldToggleText.gameObject.SetActive(false);

            for (int i = 0; i < shieldTypes.Length; i++)
            {
                if (shieldTypes[i].gameObject.activeSelf)
                {
                    currentType = i;
                }
            }
        }

        public void SetActionTex(string text)
        {
            if (text != null)
            {
                shieldToggleText.gameObject.SetActive(true);
                shieldToggleText.text = text;
            }
            else
            {
                shieldToggleText.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                Shield[] shields = GameObject.FindObjectsByType<Shield>(FindObjectsSortMode.None);
                foreach (Shield s in shields)
                {

                    if (s.GetIsShieldActive())
                    {
                        // if (Random.Range(0, 100) < 50)
                        {
                            s.SetShieldActive(false);
                        }
                    }
                    else
                        s.SetShieldActive(true);
                }
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                ShieldPostprocess[] sp = GameObject.FindObjectsByType<ShieldPostprocess>(FindObjectsSortMode.None);

                foreach (ShieldPostprocess s in sp)
                {
                    s.SetGloabalIlluminationEnabled(!s.IsGloballIlluminationEnabled());
                }
            }


            if (Input.GetKeyDown(KeyCode.P))
            {
                ReflectionProbe[] rp = GameObject.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                foreach (ReflectionProbe s in rp)
                {
                    s.gameObject.SetActive(!s.gameObject.activeSelf);
                }
            }
        }

        public void NextShieldType()
        {
            currentType++;

            if(currentType >= shieldTypes.Length)
            {
                currentType = 0;
            }

            for (int i = 0; i <  shieldTypes.Length; i++)
            {
                shieldTypes[i].SetActive(i == currentType);
            }
        }

        public void HideUI()
        {
            foreach(Transform t in transform)
            {
                t.gameObject.SetActive(false);
            }
        }

        public void EnableShields(bool animated = true)
        {
            Shield[] shields = GameObject.FindObjectsByType<Shield>(FindObjectsSortMode.None);
            foreach (Shield s in shields)
                s.SetShieldActive(true, animated);
        }

        public void DisableShiedls(bool animated = true)
        {
            Shield[] shields = GameObject.FindObjectsByType<Shield>(FindObjectsSortMode.None);
            foreach (Shield s in shields)
                s.SetShieldActive(false, animated);
        }
    }

}