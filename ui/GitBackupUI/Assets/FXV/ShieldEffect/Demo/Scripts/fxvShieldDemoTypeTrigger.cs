using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FXV.ShieldDemo
{
    public class fxvShieldDemoTypeTrigger : MonoBehaviour
    {
        [SerializeField]
        private Animator switchAnimator;

        bool isActive = false;

        void Start()
        {

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

            fxvDemo demo = FindFirstObjectByType<fxvDemo>();
            demo.NextShieldType();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<CharacterController>() != null)
            {
                fxvDemo demo = FindFirstObjectByType<fxvDemo>();
                demo.SetActionTex("[F] Change Type");

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

