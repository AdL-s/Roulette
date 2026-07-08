using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxLives = 3;

    public NetworkVariable<int> Lives = new(
        3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsDead = new(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Lives.Value = maxLives;
            IsDead.Value = false;
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer) return;
        if (IsDead.Value) return;

        Lives.Value = Mathf.Max(0, Lives.Value - amount);
        if (Lives.Value == 0)
        {
            IsDead.Value = true;
            Debug.Log($"Client {OwnerClientId} has died.");
        }
    }

    public void Heal(int amount)
    {
        if (!IsServer) return;
        if (IsDead.Value) return;

        Lives.Value = Mathf.Min(maxLives, Lives.Value + amount);
    }
}