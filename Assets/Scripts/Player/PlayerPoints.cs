using Unity.Netcode;
using UnityEngine;

// Replaces PlayerHealth. Goes on the PLAYER PREFAB.
// Values (starting / winning / gain / loss) are configured in RoundManager.
public class PlayerPoints : NetworkBehaviour
{
    public NetworkVariable<int> Points = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Server only. Never goes below 0.
    public void SetPoints(int value)
    {
        if (!IsServer) return;
        Points.Value = Mathf.Max(0, value);
    }

    // Server only. Positive = gain, negative = loss.
    public void AddPoints(int amount)
    {
        if (!IsServer) return;
        Points.Value = Mathf.Max(0, Points.Value + amount);
    }
}