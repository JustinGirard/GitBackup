using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script by TetraArts
/// </summary>

namespace TatoonEffects
{
    public class DissolveScript : MonoBehaviour
    {
        #region Variables

        private SkinnedMeshRenderer[] skin;
        private MeshRenderer[] mesh;

        [Tooltip("Particles to launch when dissolving")]
        [SerializeField]
        private ParticleSystem particles;

        //private Material material;
        private float time;

        [Tooltip("Starting height")]
        [SerializeField]
        private float startDissolveValue;

        [Tooltip("Ending height")]
        [SerializeField]
        private float endDissolveValue;

        [Tooltip("Delay for playing entire animation")]
        [SerializeField]
        private float delay;

        private bool action;

        //[SerializeField]
        //private bool debug;

        #endregion

        // Start is called before the first frame update
        void Start()
        {
            skin = GetComponentsInChildren<SkinnedMeshRenderer>();
            mesh = GetComponentsInChildren<MeshRenderer>();
            //material = skin.material;
            foreach (SkinnedMeshRenderer skinned in skin)
            {
                skinned.material.SetFloat("_Dissolve", startDissolveValue);
            }

            foreach (MeshRenderer meshRend in mesh)
            {
                meshRend.material.SetFloat("_Dissolve", startDissolveValue);
            }

            particles = GetComponentInChildren<ParticleSystem>();


        }

        // Update is called once per frame
        void Update()
        {
            time += Time.deltaTime;
            /*if (debug)
            {
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    LaunchDissolve();
                    //Debug.Log("Dissolve");
                }
            }*/

            if (action)
            {
                foreach (SkinnedMeshRenderer skined in skin)
                {
                    skined.material.SetFloat("_Dissolve", Mathf.Lerp(startDissolveValue, endDissolveValue, time / delay));
                }

                foreach (MeshRenderer meshRend in mesh)
                {
                    meshRend.material.SetFloat("_Dissolve", Mathf.Lerp(startDissolveValue, endDissolveValue, time / delay));
                }

                if (time >= delay)
                    action = false;
            }
        }

        /// <summary>
        /// Use to launch dissolve animation
        /// </summary>
        public void LaunchDissolve()
        {
            action = true;
            time = 0;
            if (action == true && particles != null)
            {
                particles.Play();
            }
        }

        public void ResetDissolve()
        {
            foreach (SkinnedMeshRenderer skinned in skin)
            {
                skinned.material.SetFloat("_Dissolve", startDissolveValue);
            }

            foreach (MeshRenderer meshRend in mesh)
            {
                meshRend.material.SetFloat("_Dissolve", startDissolveValue);
            }
        }
    }
}