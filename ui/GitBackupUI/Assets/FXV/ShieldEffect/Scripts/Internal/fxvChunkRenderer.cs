using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FXV
{
    [ExecuteInEditMode]
    public class fxvChunkRenderer : MonoBehaviour
    {
        internal fxvRenderObject.RenderChunk renderChunk = null;
        internal fxvRenderObject renderObject = null;
     
        private void OnDestroy()
        {
            renderChunk = null;
            renderObject = null;
        }

        private void OnBecameVisible()
        {
            if (renderChunk != null)
            {
                renderChunk.visibleRenderers++;

                if (!renderChunk.isInRenderList)
                {
                    renderObject.OnAddToRenderList(renderChunk);
                }
            }
        }

        private void OnBecameInvisible()
        {
            if (renderChunk != null)
            {
                renderChunk.visibleRenderers--;

                if (renderChunk.visibleRenderers == 0)
                {
                    renderObject.OnRemoveFromRenderList(renderChunk);
                }
            }
        }
    }
}