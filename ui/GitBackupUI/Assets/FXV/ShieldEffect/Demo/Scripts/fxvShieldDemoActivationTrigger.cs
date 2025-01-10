using FXV;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FXV.ShieldDemo
{
    public class fxvShieldDemoActivationTrigger : MonoBehaviour
    {
        [SerializeField]
        Transform shieldsRoot;

        [SerializeField]
        private Animator switchAnimator;

        Shield[] shields = null;

        bool isActive = false;

        void Start()
        {
            shields = FindCorrespondingShields();
        }

        void Update()
        {
            if (isActive && Input.GetKeyDown(KeyCode.F))
            {
                OnButtonPressed();
            }
        }

        public void OnButtonPressed()
        {
            switchAnimator.Play("ButtonPress", -1, 0.0f);

            foreach (Shield s in shields)
            {
                if (s.GetIsShieldActive())
                    s.SetShieldActive(false);
                else
                    s.SetShieldActive(true);
            }
        }

        Shield[] FindCorrespondingShields()
        {
            return shieldsRoot.gameObject.GetComponentsInChildren<Shield>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<CharacterController>() != null)
            {
                fxvDemo demo = FindFirstObjectByType<fxvDemo>();
                demo.SetActionTex("[F] Toggle Shield");

                isActive = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<CharacterController>() != null)
            {
                fxvDemo demo = FindFirstObjectByType<fxvDemo>();
                demo.SetActionTex(null);

                isActive = false;
            }
        }
    }
}