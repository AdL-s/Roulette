using Unity.Netcode;
using Steamworks;

public class PlayerLobbyState : NetworkBehaviour
{
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public NetworkVariable<ulong> SteamId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        IsReady.OnValueChanged += (oldVal, newVal) =>
        {
            if (MainMenu.Instance != null) MainMenu.Instance.RefreshPlayerList();
            if (IsOwner && MainMenu.Instance != null) MainMenu.Instance.UpdateReadyButtonText(newVal);
        };

        SteamId.OnValueChanged += (oldVal, newVal) =>
        {
            if (MainMenu.Instance != null) MainMenu.Instance.RefreshPlayerList();
        };
    }
    
    [ClientRpc]
    public void ResetReadyClientRpc()
    {
        if (IsOwner) IsReady.Value = false;
    }

    public void ToggleReady()
    {
        if (!IsOwner) return;
        IsReady.Value = !IsReady.Value;
    }
}