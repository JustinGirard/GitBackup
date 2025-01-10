using UnityEngine;

using MDPackage.Geometry;

namespace MDPackage.ExampleContent
{
    /// <summary>
    /// Example script for tunnel-creation at runtime via APi.
    /// </summary>
    public sealed class MD_Examples_TunnelCreatorRuntime : MonoBehaviour
    {
        [SerializeField] private MDG_TunnelCreator targetTunnelCreator;

        private const float ADD_SPACING = 2f;
        private const float ADD_SPACING_HALF = ADD_SPACING / 2f;

        public void AddNodeStraight()
        {
            var lastNode = targetTunnelCreator.TunnelCurrentNodes[^1];
            targetTunnelCreator.Tunnel_AddNode(lastNode.position + lastNode.forward * ADD_SPACING);
            targetTunnelCreator.Tunnel_RefreshNodes();
        }

        public void AddNodeRight()
        {
            var lastNode = targetTunnelCreator.TunnelCurrentNodes[^1];
            targetTunnelCreator.Tunnel_AddNode(lastNode.position + (lastNode.forward * ADD_SPACING_HALF) + (lastNode.right * ADD_SPACING_HALF));
            targetTunnelCreator.Tunnel_RefreshNodes();
        }

        public void AddNodeLeft()
        {
            var lastNode = targetTunnelCreator.TunnelCurrentNodes[^1];
            targetTunnelCreator.Tunnel_AddNode(lastNode.position + (lastNode.forward * ADD_SPACING_HALF) + (-lastNode.right * ADD_SPACING_HALF));
            targetTunnelCreator.Tunnel_RefreshNodes();
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position,
                targetTunnelCreator.TunnelCurrentNodes[^1].position + (Vector3.up * 8) + (Vector3.back * 6),
                Time.deltaTime * 6f);
        }
    }
}