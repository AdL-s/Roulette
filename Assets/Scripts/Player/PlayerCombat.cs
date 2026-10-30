using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxShootDistance = 50f;
    [SerializeField] private LayerMask shootableLayers = ~0;

    private void Update()
    {
        if (!IsOwner) return;

        var round = RoundManager.Instance;
        var nm = NetworkManager.Singleton;
        if (round == null || nm == null) return;

        bool isMyActionTurn =
            round.CurrentPhase.Value == GamePhase.Action &&
            nm.LocalClientId == round.RoundOwnerClientId.Value;

        if (!isMyActionTurn) return;
        if (ItemSystem.BlockShooting) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryShootAtLookedTarget();
    }

    private void TryShootAtLookedTarget()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxShootDistance, shootableLayers))
        {
            if (!hit.collider.CompareTag("Player")) return;
            var highlight = hit.collider.GetComponentInParent<HighLight_Controller>();

            if (highlight.block.GetFloat("_EmissionIntensity") >= 5.0)
            {
                return;
            }
            else if (highlight.block.GetFloat("_EmissionIntensity") < 5.0)
            {
                highlight.block.SetFloat("_EmissionIntensity",
                    highlight.block.GetFloat("_EmissionIntensity") + 1 * Time.deltaTime);
            }


            var targetNetObj = hit.collider.GetComponentInParent<NetworkObject>();
            if (targetNetObj == null) return;
            if (targetNetObj.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                return; // LMB can't hit self, use RMB for that

            RoundManager.Instance.TryAttackServerRpc(targetNetObj.OwnerClientId);
        }
    }
}