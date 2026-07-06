using System.Threading.Tasks;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement; 
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }
    private Lobby currentLobby;
    public Dictionary<ulong, SteamId> ClientSteamIds { get; } = new Dictionary<ulong, SteamId>();
    
    public Lobby CurrentLobby => currentLobby;
    
    [SerializeField] private GameObject lobbyPlayerStatePrefab;

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
        SteamMatchmaking.OnLobbyMemberJoined += (lobby, friend) =>
            Debug.Log($"{friend.Name} joined the lobby");
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }
    
    private void Start()
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }
    
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        ulong steamIdRaw = System.BitConverter.ToUInt64(request.Payload, 0);
        ClientSteamIds[request.ClientNetworkId] = new SteamId { Value = steamIdRaw };

        response.Approved = true;
        response.CreatePlayerObject = false; 
    }

    public async void HostLobby()
    {
        if (!SteamClient.IsValid)
        {
            Debug.LogWarning("SteamClient not valid, reinitializing...");
            try
            {
                SteamClient.Init(480); // use your real AppID here
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to reinitialize SteamClient: {e.Message}");
                return;
            }
        }

        await SteamMatchmaking.CreateLobbyAsync(4);
    }

    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK) return;

        lobby.SetFriendsOnly();
        lobby.SetJoinable(true);

        currentLobby = lobby;

        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(SteamClient.SteamId.Value);

        MainMenu.Instance.SetLobby(lobby);
        NetworkManager.Singleton.StartHost();

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += MainMenu.Instance.OnSceneLoadCompleted;
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

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += MainMenu.Instance.OnSceneLoadCompleted;
    }
    
    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        Debug.Log($"OnGameLobbyJoinRequested fired. Lobby Id = {lobby.Id}, currently IsHost = {NetworkManager.Singleton.IsHost}, IsClient = {NetworkManager.Singleton.IsClient}");

        LeaveCurrentSessionIfAny();
        lobby.Join();
    }
    private void LeaveCurrentSessionIfAny()
    {
        Debug.Log($"LeaveCurrentSessionIfAny called. currentLobby.Id = {currentLobby.Id}, IsHost = {NetworkManager.Singleton.IsHost}, IsClient = {NetworkManager.Singleton.IsClient}");

        if (currentLobby.Id != 0)
        {
            currentLobby.Leave();
        }

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        MainMenu.Instance.ResetToMainMenu();
    }

    public async Task<Lobby[]> GetLobbiesList()
    {
        if (!SteamClient.IsValid) SteamClient.Init(480);
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
    
    public void ClearCurrentLobby()
    {
        currentLobby = default;
    }
    
}