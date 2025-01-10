using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FXV.ShieldDemo
{
    [ExecuteInEditMode]
    public class fxvReflection : MonoBehaviour
    {
        [SerializeField]
        Camera cameraObj;

        [SerializeField]
        Transform floorPlane;

        void Start()
        {

        }


        void LateUpdate()
        {
            if (cameraObj && floorPlane)
            {
                Vector3 camOffset = cameraObj.transform.position - floorPlane.position;

                transform.position = new Vector3(cameraObj.transform.position.x, floorPlane.position.y - camOffset.y, cameraObj.transform.position.z);

                GetComponent<ReflectionProbe>().RenderProbe();
            }
        }
    }
}
