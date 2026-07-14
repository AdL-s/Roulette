using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using Color = UnityEngine.Color;
using System.Linq;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    [Header("Lobby player list")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerRowPrefab;

    [Header("Lobby buttons")]
    [SerializeField] private Button inviteButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color unreadyColor = Color.red;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text lobbyCodeText;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("Join panel")]
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private Button openJoinPanelButton;
    
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Transform serverListContainer;
    [SerializeField] private GameObject serverRowPrefab;
    
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button closeJoinPanelButton;
    
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private Button joinByCodeButton;
    [SerializeField] private TMP_Text joinErrorText;
    
    [Header("Toggle")]
    [SerializeField] private Toggle privateToggle;
    
    [Header("Lobby id buttons / toggles")]
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private Toggle showCodeToggle;

    private string originalSceneName;
    private Lobby currentLobby;
    private readonly List<GameObject> spawnedRows = new List<GameObject>();
    private readonly List<GameObject> spawnedServerRows = new List<GameObject>();
    
    private string currentLobbyCode = "";
    private bool isCodeVisible = false;

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

        openJoinPanelButton.onClick.AddListener(OnOpenJoinPanel);
        refreshButton.onClick.AddListener(() => _ = RefreshServerList());
        closeJoinPanelButton.onClick.AddListener(ResetToMainSearch);
        searchInput.onValueChanged.AddListener(text => { _ = RefreshServerList(); });
        joinByCodeButton.onClick.AddListener(OnJoinByCodeClicked);
        codeInput.characterLimit = LobbyCodeUtility.CodeLength;
        joinPanel.SetActive(false);
        joinErrorText.gameObject.SetActive(false);
        
        privateToggle.onValueChanged.AddListener(OnPrivateToggleChanged);
        privateToggle.gameObject.SetActive(false);
        
        copyCodeButton.onClick.AddListener(OnCopyCodeClicked);
        showCodeToggle.onValueChanged.AddListener(OnShowCodeToggleChanged);
        showCodeToggle.SetIsOnWithoutNotify(false);
        
        SteamMatchmaking.OnLobbyMemberJoined += (lobby, friend) => RefreshPlayerList();
        SteamMatchmaking.OnLobbyMemberLeave += (lobby, friend) => RefreshPlayerList();
        SteamMatchmaking.OnLobbyMemberDisconnected += (lobby, friend) => RefreshPlayerList();
    }

    public void Quit() => Application.Quit();

    public void OnHostButton() => LobbyManager.Instance.HostLobby("Lobby", false, false);
    // Swap the line above for hostOptionsUI.Open() once you wire up the Host options popup
    

    private void OnOpenJoinPanel()
    {
        mainMenuPanel.SetActive(false);
        joinPanel.SetActive(true);
        joinErrorText.gameObject.SetActive(false);
        _ = RefreshServerList();
    }

    private async Task RefreshServerList()
    {
        foreach (var row in spawnedServerRows) Destroy(row);
        spawnedServerRows.Clear();

        var lobbies = await LobbyManager.Instance.SearchLobbies(searchInput.text);
        foreach (var lobby in lobbies)
        {
            var rowObj = Instantiate(serverRowPrefab, serverListContainer);
            var row = rowObj.GetComponent<ServerBrowserRow>();
            row.Setup(lobby, () => OnServerRowJoinClicked(lobby));
            spawnedServerRows.Add(rowObj);
        }
    }

    private void OnServerRowJoinClicked(Lobby lobby)
    {
        LobbyManager.Instance.JoinLobbyDirect(lobby);
        joinPanel.SetActive(false);
    }

    private void OnJoinByCodeClicked()
    {
        if (!LobbyCodeUtility.TryDecode(codeInput.text, out ulong lobbyIdValue))
        {
            ShowJoinError("Invalid code format.");
            return;
        }

        LobbyManager.Instance.JoinByCode(codeInput.text);
        joinPanel.SetActive(false);
    }

    public void ShowJoinError(string message)
    {
        joinErrorText.text = message;
        joinErrorText.gameObject.SetActive(true);
    }
    

    public void SetLobby(Lobby lobby)
    {
        currentLobby = lobby;

        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(true);

        currentLobbyCode = LobbyCodeUtility.Encode(lobby.Id.Value);
        isCodeVisible = false;

        if (showCodeToggle != null)
        {
            showCodeToggle.SetIsOnWithoutNotify(false);
        }

        RefreshCodeDisplay(); // shows masked stars immediately

        RefreshPlayerList();

        bool isHost = currentLobby.Owner.Id == SteamClient.SteamId;
        startButton.gameObject.SetActive(isHost);

        privateToggle.gameObject.SetActive(isHost);
        if (isHost)
        {
            privateToggle.SetIsOnWithoutNotify(false);
        }

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

    private bool GetReadyStateForSteamId(SteamId steamId)
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
        if (currentLobby.Id == 0)
        {
            Debug.LogWarning("No active lobby to invite to.");
            return;
        }
        SteamFriends.OpenGameInviteOverlay(currentLobby.Id);
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

        ResetToMainMenuLobby();
    }

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
        else if (sceneName == "MainMenu")
        {
            mainMenuCanvas.SetActive(true);
            lobbyPanel.SetActive(true);
            mainMenuPanel.SetActive(false);
            RefreshPlayerList();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ResetToMainMenuLobby()
    {
        mainMenuCanvas.SetActive(true);
        lobbyPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void ResetToMainSearch()
    {
        mainMenuCanvas.SetActive(true);
        joinPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    
    private void OnPrivateToggleChanged(bool isPrivate)
    {
        LobbyManager.Instance.SetLobbyPrivate(isPrivate);
    }
    
    private void OnShowCodeToggleChanged(bool show)
    {
        isCodeVisible = show;
        RefreshCodeDisplay();
    }

    private void RefreshCodeDisplay()
    {
        if (lobbyCodeText == null || string.IsNullOrEmpty(currentLobbyCode)) return;

        string display = isCodeVisible
            ? currentLobbyCode
            : new string('*', currentLobbyCode.Length);

        lobbyCodeText.text = $"Code: {display}";
    }

    private void OnCopyCodeClicked()
    {
        if (string.IsNullOrEmpty(currentLobbyCode)) return;
        GUIUtility.systemCopyBuffer = currentLobbyCode;
        Debug.Log($"Copied lobby code: {currentLobbyCode}");
    }
    
    
}