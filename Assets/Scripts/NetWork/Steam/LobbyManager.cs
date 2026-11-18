using System.Threading.Tasks;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class LobbyManager : MonoBehaviour
{
    public const int MaxPlayers = 4;

    public static LobbyManager Instance { get; private set; }
    private Lobby currentLobby;
    public Dictionary<ulong, SteamId> ClientSteamIds { get; } = new Dictionary<ulong, SteamId>();

    public Lobby CurrentLobby => currentLobby;

    public bool IsGameInProgress { get; private set; }

    private string pendingLobbyName;
    private bool pendingFriendsOnly;
    private bool pendingPrivate;

    [SerializeField] private GameObject lobbyPlayerStatePrefab;

    private bool _netCallbacksSubscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    private void Start()
    {
        if (Instance != this) return;

        var nm = NetworkManager.Singleton;
        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = ApprovalCheck;
        nm.OnClientConnectedCallback += HandleClientConnected;
        nm.OnClientDisconnectCallback += HandleClientDisconnected;
        _netCallbacksSubscribed = true;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;

        var nm = NetworkManager.Singleton;
        if (nm != null && _netCallbacksSubscribed)
        {
            nm.OnClientConnectedCallback -= HandleClientConnected;
            nm.OnClientDisconnectCallback -= HandleClientDisconnected;
            if (nm.ConnectionApprovalCallback == ApprovalCheck) nm.ConnectionApprovalCallback = null;
        }

        _netCallbacksSubscribed = false;

        UnsubscribeSceneEvents();
        Instance = null;
    }

    private void SubscribeSceneEvents()
    {
        var sceneManager = NetworkManager.Singleton.SceneManager;
        if (sceneManager == null || MainMenu.Instance == null) return;

        sceneManager.OnLoadEventCompleted -= MainMenu.Instance.OnSceneLoadCompleted;
        sceneManager.OnLoadEventCompleted += MainMenu.Instance.OnSceneLoadCompleted;
    }

    private void UnsubscribeSceneEvents()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.SceneManager == null || MainMenu.Instance == null) return;

        nm.SceneManager.OnLoadEventCompleted -= MainMenu.Instance.OnSceneLoadCompleted;
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        Debug.Log($"{friend.Name} joined the lobby");
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.CreatePlayerObject = false;

        if (request.Payload == null || request.Payload.Length < sizeof(ulong))
        {
            Reject(response, "Invalid connection data.");
            return;
        }

        if (IsGameInProgress)
        {
            Reject(response, "Game already in progress.");
            return;
        }

        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MaxPlayers)
        {
            Reject(response, "Lobby is full.");
            return;
        }

        ulong steamIdRaw = System.BitConverter.ToUInt64(request.Payload, 0);
        ClientSteamIds[request.ClientNetworkId] = new SteamId { Value = steamIdRaw };
        response.Approved = true;
    }

    private static void Reject(NetworkManager.ConnectionApprovalResponse response, string reason)
    {
        response.Approved = false;
        response.Reason = reason;
    }

    public void SetGameInProgress(bool inProgress)
    {
        IsGameInProgress = inProgress;

        if (!SteamClient.IsValid || currentLobby.Id == 0) return;
        if (currentLobby.Owner.Id != SteamClient.SteamId) return;
        currentLobby.SetJoinable(!inProgress);
    }

    public async void HostLobby(string lobbyName, bool friendsOnly, bool isPrivate)
    {
        IsGameInProgress = false;

        pendingLobbyName = string.IsNullOrWhiteSpace(lobbyName) ? "Lobby" : lobbyName;
        pendingFriendsOnly = friendsOnly;
        pendingPrivate = isPrivate;

        SteamManager.Instance.EnsureInitialized();

        await SteamMatchmaking.CreateLobbyAsync(MaxPlayers);
    }

    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK) return;

        if (pendingPrivate) lobby.SetPrivate();
        else if (pendingFriendsOnly) lobby.SetFriendsOnly();
        else lobby.SetPublic();

        lobby.SetJoinable(true);
        lobby.SetData("name", pendingLobbyName);

        currentLobby = lobby;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(SteamClient.SteamId.Value);

        MainMenu.Instance.SetLobby(lobby);
        NetworkManager.Singleton.StartHost();

        SubscribeSceneEvents();
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        Debug.Log($"OnLobbyEntered fired. Lobby Id = {lobby.Id}, IsHost = {NetworkManager.Singleton.IsHost}");

        currentLobby = lobby;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(SteamClient.SteamId.Value);

        MainMenu.Instance.SetLobby(lobby);

        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("OnLobbyEntered: IsHost is true, returning early without StartClient.");
            return;
        }

        var facepunchTransport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();
        facepunchTransport.targetSteamId = lobby.Owner.Id;
        NetworkManager.Singleton.StartClient();
        Debug.Log("OnLobbyEntered: StartClient() called.");

        SubscribeSceneEvents();
    }

    public void JoinLobbyDirect(Lobby lobby)
    {
        LeaveCurrentSessionIfAny();
        lobby.Join();
    }

    public async void JoinByCode(string code)
    {
        if (!LobbyCodeUtility.TryDecode(code, out ulong lobbyIdValue))
        {
            MainMenu.Instance.ShowJoinError("Invalid code format.");
            return;
        }

        LeaveCurrentSessionIfAny();

        var result = await SteamMatchmaking.JoinLobbyAsync(lobbyIdValue);
        if (result == null)
        {
            Debug.LogWarning("JoinLobbyAsync returned null - lobby may not exist or is unreachable.");
            MainMenu.Instance.ShowJoinError("Could not join lobby.");
        }
    }

    public void LeaveLobby()
    {
        LeaveCurrentSessionIfAny();
    }

    public async Task<List<Lobby>> SearchLobbies(string nameFilter = null)
    {
        var lobbies = await SteamMatchmaking.LobbyList.RequestAsync();
        if (lobbies == null) return new List<Lobby>();

        var list = lobbies.ToList();
        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            list = list.Where(l =>
                (l.GetData("name") ?? "").IndexOf(nameFilter, System.StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();
        }

        return list;
    }

    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        Debug.Log(
            $"OnGameLobbyJoinRequested fired. Lobby Id = {lobby.Id}, currently IsHost = {NetworkManager.Singleton.IsHost}, IsClient = {NetworkManager.Singleton.IsClient}");

        if (NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning(
                "Ignoring OnGameLobbyJoinRequested while already hosting - this is a stray/duplicate Steam event, not a real join action.");
            return;
        }

        LeaveCurrentSessionIfAny();
        lobby.Join();
    }

    private void LeaveCurrentSessionIfAny()
    {
        Debug.Log(
            $"LeaveCurrentSessionIfAny called. currentLobby.Id = {currentLobby.Id}, IsHost = {NetworkManager.Singleton.IsHost}, IsClient = {NetworkManager.Singleton.IsClient}");

        if (currentLobby.Id != 0)
        {
            currentLobby.Leave();
        }

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            UnsubscribeSceneEvents();
            NetworkManager.Singleton.Shutdown();

            SteamManager.Instance.EnsureInitialized();
        }

        ClearCurrentLobby();
        MainMenu.Instance.ResetToMainMenuLobby();
    }

    public async Task<Lobby[]> GetLobbiesList()
    {
        SteamManager.Instance.EnsureInitialized();
        return await SteamMatchmaking.LobbyList.RequestAsync();
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        var instance = Instantiate(lobbyPlayerStatePrefab);
        var netObj = instance.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId);

        if (ClientSteamIds.TryGetValue(clientId, out var steamId))
        {
            instance.GetComponent<PlayerLobbyState>().SteamId.Value = steamId.Value;
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            ClientSteamIds.Remove(clientId);
            return;
        }

        // Client side: this client lost its connection (host left, rejected, kicked)
        Debug.Log($"Disconnected from host: {NetworkManager.Singleton.DisconnectReason}");
        LeaveCurrentSessionIfAny();

        if (SceneManager.GetActiveScene().name == "GameScene")
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    public void ClearCurrentLobby()
    {
        currentLobby = default;
        IsGameInProgress = false;
    }

    public void SetLobbyPrivate(bool isPrivate)
    {
        if (currentLobby.Id == 0) return;
        if (currentLobby.Owner.Id != SteamClient.SteamId) return; // safety: only host can change this

        if (isPrivate) currentLobby.SetPrivate();
        else currentLobby.SetPublic();

        Debug.Log($"Lobby visibility set to {(isPrivate ? "Private" : "Public")}.");
    }
}