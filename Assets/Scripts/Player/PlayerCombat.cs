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
        if (RoundManager.Instance == null) return;

        bool isMyActionTurn =
            RoundManager.Instance.CurrentPhase.Value == GamePhase.Action &&
            NetworkManager.Singleton.LocalClientId == RoundManager.Instance.RoundOwnerClientId.Value;

        if (!isMyActionTurn) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryShootAtLookedTarget();
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            RoundManager.Instance.TryAttackServerRpc(NetworkManager.Singleton.LocalClientId); // self-attack
        }
    }

    private void TryShootAtLookedTarget()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxShootDistance, shootableLayers))
        {
            if (!hit.collider.CompareTag("Player")) return;

            var targetNetObj = hit.collider.GetComponentInParent<NetworkObject>();
            if (targetNetObj == null) return;
            if (targetNetObj.OwnerClientId == NetworkManager.Singleton.LocalClientId) return; // LMB can't hit self, use RMB for that

            RoundManager.Instance.TryAttackServerRpc(targetNetObj.OwnerClientId);
        }
    }
}