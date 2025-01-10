using UnityEngine;
using UnityEngine.UI;

using MDPackage.Modifiers;

namespace MDPackage.ExampleContent
{
    /// <summary>
    /// Sample script for universal mesh editor - add/remove and edit mesh at runtime.
    /// </summary>
    public sealed class MD_Examples_UniversalMeshEditorController : MonoBehaviour
    {
        [SerializeField] private MeshFilter targetMesh;
        [Space]
        [SerializeField] private GameObject meshEditorGroup;
        [SerializeField] private GameObject sculptingGroup;
        [SerializeField] private GameObject ffdGroup;
        [SerializeField] private GameObject aBendGroup;
        [Space]
        [SerializeField] private MD_Examples_InputWrapper_MeshEditorRuntime inputWrapMeshEditorRuntime;
        [SerializeField] private MD_Examples_InputWrapper_SculptingLite inputWrapSculptingLite;

        private MD_MeshEditorRuntime runtimeEditor;
        private MD_MeshProEditor meshEditor;

        private MDM_SculptingLite sculptingLite;

        private MDM_FFD ffdModifier;
        private MDM_AngularBend angularBend;

        public void CloseEditor()
        {
            if (meshEditor) Destroy(meshEditor);
            if (runtimeEditor) Destroy(runtimeEditor);
            if (ffdModifier) Destroy(ffdModifier);
            if (angularBend) Destroy(angularBend);
            if (sculptingLite) Destroy(sculptingLite);

            meshEditorGroup.SetActive(false);
            sculptingGroup.SetActive(false);
            ffdGroup.SetActive(false);
            aBendGroup.SetActive(false);
        }

        public void OpenMeshEditor()
        {
            if (meshEditor)
                return;

            if (sculptingLite) Destroy(sculptingLite);
            if (ffdModifier) Destroy(ffdModifier);
            if (angularBend) Destroy(angularBend);

            meshEditorGroup.SetActive(true);
            sculptingGroup.SetActive(false);
            ffdGroup.SetActive(false);
            aBendGroup.SetActive(false);

            meshEditor = targetMesh.gameObject.AddComponent<MD_MeshProEditor>();
            meshEditor.onMeshProEditorInitialized = () =>
            {
                meshEditor.MPE_CreatePointsEditor();
                runtimeEditor = gameObject.AddComponent<MD_MeshEditorRuntime>();
                runtimeEditor.isAxisEditor = false;
                runtimeEditor.nonAxis_vertexControlMode = MD_MeshEditorRuntime.VertexControlMode.GrabDropVertex;
                inputWrapMeshEditorRuntime.targetMeshEditorRuntime = runtimeEditor;
            };
        }

        public void OpenSculptingEditor()
        {
            if (sculptingLite)
                return;

            meshEditorGroup.SetActive(false);
            sculptingGroup.SetActive(true);
            ffdGroup.SetActive(false);
            aBendGroup.SetActive(false);

            if (meshEditor) Destroy(meshEditor);
            if (runtimeEditor) Destroy(runtimeEditor);
            if (ffdModifier) Destroy(ffdModifier);
            if (angularBend) Destroy(angularBend);

            sculptingLite = targetMesh.gameObject.AddComponent<MDM_SculptingLite>();
            sculptingLite.sculptingBrushIntensity = 0.2f;
            sculptingLite.sculptingRecalculateNormalsOnRelease = false;
            inputWrapSculptingLite.targetSculptingLite = sculptingLite;
        }

        public void OpenFFDModifier()
        {
            if (ffdModifier)
                return;

            meshEditorGroup.SetActive(false);
            sculptingGroup.SetActive(false);
            ffdGroup.SetActive(true);
            aBendGroup.SetActive(false);

            if (meshEditor) Destroy(meshEditor);
            if (runtimeEditor) Destroy(runtimeEditor);
            if (sculptingLite) Destroy(sculptingLite);
            if (angularBend) Destroy(angularBend);

            ffdModifier = targetMesh.gameObject.AddComponent<MDM_FFD>();
            ffdModifier.OnModifierInitialized = () =>
            {
                ffdModifier.FFD_RefreshFFDGrid();
            };
        }

        public void OpenAngularBend()
        {
            if (angularBend)
                return;

            meshEditorGroup.SetActive(false);
            sculptingGroup.SetActive(false);
            ffdGroup.SetActive(false);
            aBendGroup.SetActive(true);

            if (meshEditor) Destroy(meshEditor);
            if (runtimeEditor) Destroy(runtimeEditor);
            if (sculptingLite) Destroy(sculptingLite);
            if (ffdModifier) Destroy(ffdModifier);

            angularBend = targetMesh.gameObject.AddComponent<MDM_AngularBend>();
        }

        public void ChangeAngularBendValue(Slider slider)
        {
            if(angularBend)
                angularBend.bendValue = slider.value;
        }

        public void ChangeAngularBendAngle(Slider slider)
        {
            if (angularBend)
                angularBend.bendAngle = slider.value;
        }

        private void Update()
        {
            if (ffdModifier == null || !ffdModifier.MbIsInitialized)
                return;
            if(Input.GetKey(KeyCode.Space))
            {
                foreach(var node in ffdModifier.FFD_GetNodes())
                    node.transform.position += ((node.transform.position - targetMesh.transform.position).normalized * 0.5f) * Time.deltaTime;
            }
        }
    }
}