using Unity.Netcode;

[System.Flags]
public enum PlayerStateFlags
{
    None = 0,
    SkipTurn = 1 << 0, // the player's next turn is skipped
    ShortQuestionTime = 1 << 1, // the player gets less time to answer
    DoubleDamage = 1 << 2, // the player's live shot takes double points
}

public class PlayerStates : NetworkBehaviour
{
    public NetworkVariable<PlayerStateFlags> Flags = new(
        PlayerStateFlags.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool Has(PlayerStateFlags state) => (Flags.Value & state) != 0;

    public void Add(PlayerStateFlags state)
    {
        if (!IsServer) return;
        Flags.Value |= state;
    }

    public void Clear()
    {
        if (!IsServer) return;
        Flags.Value = PlayerStateFlags.None;
    }

    public static PlayerStates Get(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
        return client.PlayerObject != null ? client.PlayerObject.GetComponent<PlayerStates>() : null;
    }
}