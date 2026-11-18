using Unity.Netcode;

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
        IsReady.OnValueChanged += OnReadyChanged;
        SteamId.OnValueChanged += OnSteamIdChanged;
    }

    public override void OnNetworkDespawn()
    {
        IsReady.OnValueChanged -= OnReadyChanged;
        SteamId.OnValueChanged -= OnSteamIdChanged;
    }

    private void OnReadyChanged(bool oldVal, bool newVal)
    {
        if (MainMenu.Instance == null) return;

        MainMenu.Instance.RefreshPlayerList();
        if (IsOwner) MainMenu.Instance.UpdateReadyButtonText(newVal);
    }

    private void OnSteamIdChanged(ulong oldVal, ulong newVal)
    {
        if (MainMenu.Instance != null) MainMenu.Instance.RefreshPlayerList();
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