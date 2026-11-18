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

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryShootAtLookedTarget();
    }

    private void TryShootAtLookedTarget()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxShootDistance, shootableLayers)) return;

        var target = hit.collider.GetComponentInParent<NetworkObject>();
        if (target == null || !target.IsPlayerObject) return;
        if (target.OwnerClientId == NetworkManager.Singleton.LocalClientId) return;

        RoundManager.Instance.TryAttackServerRpc(target.OwnerClientId);
    }
}