using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using Color = UnityEngine.Color;
using System.Linq;

public class MainMenu : MonoBehaviour
{
    
    public static MainMenu Instance { get; private set; }
    
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerRowPrefab;
    [SerializeField] private Button inviteButton;
    
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color unreadyColor = Color.red;

    [SerializeField] private Button leaveButton;
    [SerializeField] private Button startButton;
    
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyPanel;
    
    private string originalSceneName;

    private Lobby currentLobby;
    private readonly List<GameObject> spawnedRows = new List<GameObject>();
    
    private void Start()
    {
        if (Instance != this) return; 

        originalSceneName = SceneManager.GetActiveScene().name;
        
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        inviteButton.onClick.AddListener(OnInviteClicked);
        readyButton.onClick.AddListener(OnReadyClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);
        startButton.onClick.AddListener(OnStartClicked);

        startButton.gameObject.SetActive(false); 

        SteamMatchmaking.OnLobbyMemberJoined += (lobby, friend) => RefreshPlayerList();
        SteamMatchmaking.OnLobbyMemberLeave += (lobby, friend) => RefreshPlayerList();
        SteamMatchmaking.OnLobbyMemberDisconnected += (lobby, friend) => RefreshPlayerList();
    }
    

    public void Quit()
    {
        Application.Quit();
        
    }
    
    public void OnHostButton() => LobbyManager.Instance.HostLobby();

    public async void OnRefreshLobbiesButton()
    {
        var lobbies = await LobbyManager.Instance.GetLobbiesList();
        foreach (var lobby in lobbies)
        {
            Debug.Log($"Lobby: {lobby.GetData("name")} - {lobby.MemberCount}/{lobby.MaxMembers}");
        }
    }
    
    public void SetLobby(Lobby lobby)
    {
        currentLobby = lobby;

        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(true);

        RefreshPlayerList();

        bool isHost = currentLobby.Owner.Id == SteamClient.SteamId;
        startButton.gameObject.SetActive(isHost);
        UpdateInviteButtonState();
    }

    public void RefreshPlayerList()
    {
        foreach (var row in spawnedRows) Destroy(row);
        spawnedRows.Clear();

        foreach (Friend member in currentLobby.Members)
        {
            bool isReady = GetReadyStateForSteamId(member.Id);

            var rowObj = Instantiate(playerRowPrefab, playerListContainer);
            var row = rowObj.GetComponent<LobbyPlayerRow>();
            row.Setup(member, isReady);
            spawnedRows.Add(rowObj);
        }

        UpdateInviteButtonState();
        UpdateStartButtonState();
    }
    
    private void UpdateStartButtonState()
    {
        if (!startButton.gameObject.activeSelf) return;

        var states = FindObjectsByType<PlayerLobbyState>(FindObjectsSortMode.None);
        bool allReady = states.Length > 0 && states.All(s => s.IsReady.Value);

        startButton.interactable = allReady;
    }
    private void OnStartClicked()
    {
        if (currentLobby.Owner.Id != SteamClient.SteamId) return;
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    
    private void UpdateInviteButtonState()
    {
        bool isFull = currentLobby.MemberCount >= currentLobby.MaxMembers;
        inviteButton.interactable = !isFull;
    }
    
    private bool GetReadyStateForSteamId(Steamworks.SteamId steamId)
    {
        foreach (var state in FindObjectsByType<PlayerLobbyState>(FindObjectsSortMode.None))
        {
            if (state.SteamId.Value == steamId.Value)
                return state.IsReady.Value;
        }
        return false;
    }
    private void OnInviteClicked()
    {
        Debug.Log($"OnInviteClicked called. currentLobby.Id = {currentLobby.Id}");

        if (currentLobby.Id == 0)
        {
            Debug.LogWarning("No active lobby to invite to.");
            return;
        }
        SteamFriends.OpenGameInviteOverlay(currentLobby.Id);
    }
    

    private void OnStartGameClicked()
    {
        if (currentLobby.Owner.Id != SteamClient.SteamId) return;
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void OnLeaveClicked()
    {
        if (currentLobby.Id == 0) return;

        var localState = FindLocalPlayerLobbyState();
        if (localState != null && localState.IsReady.Value)
        {
            localState.ToggleReady();
            UpdateReadyButtonText(localState.IsReady.Value);
        }

        currentLobby.Leave();

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        ResetToMainMenu();
    }

    /*private SteamId SteamIdFromClientId(ulong clientId)
    {
        if (LobbyManager.Instance.ClientSteamIds.TryGetValue(clientId, out var steamId))
            return steamId;
        return default;
    }*/
    
    private void OnReadyClicked()
    {

        var localState = FindLocalPlayerLobbyState();
        if (localState == null) return;

        localState.ToggleReady();
        UpdateReadyButtonText(localState.IsReady.Value);
    }

    private PlayerLobbyState FindLocalPlayerLobbyState()
    {
        var allStates = FindObjectsByType<PlayerLobbyState>(FindObjectsSortMode.None);

        foreach (var obj in allStates)
        {
            if (obj.IsOwner) return obj;
        }
        return null;
    }
    
    public void UpdateReadyButtonText(bool isReady)
    {
        readyButtonText.text = isReady ? "Unready" : "Ready";
        readyButton.image.color = isReady ? unreadyColor : readyColor;
    }
    public void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName == "GameScene")
        {
            mainMenuCanvas.SetActive(false);

            var scene = SceneManager.GetSceneByName(originalSceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
    
    public void ResetToMainMenu()
    {
        mainMenuCanvas.SetActive(true); 
        lobbyPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    
    
}
