using Unity.Netcode;
using UnityEngine;


public class PlayerPoints : NetworkBehaviour
{
    public NetworkVariable<int> Points = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void SetPoints(int value)
    {
        if (!IsServer) return;
        Points.Value = Mathf.Max(0, value);
    }

    public void AddPoints(int amount)
    {
        if (!IsServer) return;
        Points.Value = Mathf.Max(0, Points.Value + amount);
    }

    public static PlayerPoints Get(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
        return client.PlayerObject != null ? client.PlayerObject.GetComponent<PlayerPoints>() : null;
    }
}