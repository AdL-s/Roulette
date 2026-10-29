using Steamworks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Goes on the PLAYER PREFAB (next to PlayerPoints).
// Shows the name and the points as two separate texts above the player.
// The name is resolved on the server and synced, because ClientSteamIds only exists on the server.
[RequireComponent(typeof(PlayerPoints))]
public class PlayerNameTag : NetworkBehaviour
{
    private const int MaxNameLength = 24;

    [Tooltip("Parent object of both texts, placed above the player's head. It is rotated toward the camera.")]
    [SerializeField]
    private Transform tagRoot;

    [Tooltip("TextMeshPro (3D, not UI) child of tagRoot for the name")] [SerializeField]
    private TMP_Text nameText;

    [Tooltip("TextMeshPro (3D, not UI) child of tagRoot for the points")] [SerializeField]
    private TMP_Text pointsText;

    [Tooltip("The local player's own camera sits at their head, so their own tag is hidden")] [SerializeField]
    private bool hideOwnTag = true;

    public NetworkVariable<FixedString128Bytes> PlayerName = new(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private PlayerPoints _points;
    private Camera _cam;

    public override void OnNetworkSpawn()
    {
        _points = GetComponent<PlayerPoints>();

        if (IsServer) PlayerName.Value = ResolveName();

        PlayerName.OnValueChanged += OnNameChanged;
        _points.Points.OnValueChanged += OnPointsChanged;

        tagRoot.gameObject.SetActive(!(hideOwnTag && IsOwner));
        RefreshName();
        RefreshPoints();
    }

    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnNameChanged;
        if (_points != null) _points.Points.OnValueChanged -= OnPointsChanged;
    }

    private void OnNameChanged(FixedString128Bytes oldValue, FixedString128Bytes newValue) => RefreshName();
    private void OnPointsChanged(int oldValue, int newValue) => RefreshPoints();

    private void RefreshName() => nameText.text = PlayerName.Value.ToString();
    private void RefreshPoints() => pointsText.text = _points.Points.Value.ToString();

    // Server only
    private string ResolveName()
    {
        string result = null;

        if (OwnerClientId == NetworkManager.ServerClientId)
        {
            if (SteamClient.IsValid) result = SteamClient.Name;
        }
        else if (LobbyManager.Instance != null &&
                 LobbyManager.Instance.ClientSteamIds.TryGetValue(OwnerClientId, out SteamId steamId))
        {
            result = new Friend(steamId).Name;
        }

        if (string.IsNullOrWhiteSpace(result)) result = $"Player {OwnerClientId}";
        if (result.Length > MaxNameLength) result = result.Substring(0, MaxNameLength);
        return result;
    }

    private void LateUpdate()
    {
        if (!tagRoot.gameObject.activeSelf) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        tagRoot.rotation = Quaternion.LookRotation(tagRoot.position - _cam.transform.position);
    }
}