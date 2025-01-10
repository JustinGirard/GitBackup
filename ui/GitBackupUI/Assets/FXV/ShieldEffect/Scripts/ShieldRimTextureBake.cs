using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FXV
{
    public class ShieldRimTextureBake : MonoBehaviour
    {
        public struct BakeRimParams
        {
            public int layerMask;

            public int textureWidth;
            public int textureHeight;
            public float power;
            public float radiusR;
            public float radiusG;
            public float radiusB;
            public int numSteps;

            public bool denoiseEnabled;

            public Vector3 bakeDirection;
            public Vector3 bakeTangent;
            public float bakeDistance;
        }

        [fxvHeader("Baking Options")]
        [SerializeField]
        int textureWidth = 512;

        [SerializeField]
        int textureHeight = 512;

        [SerializeField]
        float radiusR = 0.1f;

        [SerializeField]
        float radiusG = 0.25f;

        [SerializeField]
        float radiusB = 0.5f;

        [SerializeField, ShieldDraggablePoint]
        Vector3 bakeOrigin = -Vector3.forward;

        void Start()
        {

        }

        void Update()
        {

        }

        public BakeRimParams GetBakeParams()
        {
            Vector3 bakeDirection = -transform.TransformDirection(bakeOrigin);
            float bakeDistance = transform.TransformVector(bakeOrigin).magnitude;

            return new BakeRimParams { layerMask = ~0, textureWidth = textureWidth, textureHeight = textureHeight, radiusR = radiusR, radiusG = radiusG, radiusB = radiusB, bakeDirection = bakeDirection.normalized, bakeTangent = Vector3.Cross(bakeDirection.normalized, Vector3.up).normalized, bakeDistance = bakeDistance };
        }
    }
}
