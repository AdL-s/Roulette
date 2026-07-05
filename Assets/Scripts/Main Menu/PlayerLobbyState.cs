using Unity.Netcode;

public class PlayerLobbyState : NetworkBehaviour
{
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        IsReady.OnValueChanged += (oldVal, newVal) => MainMenu.Instance.RefreshPlayerList();
    }

    public void ToggleReady()
    {
        if (!IsOwner) return;

        IsReady.Value = !IsReady.Value;
    }
}