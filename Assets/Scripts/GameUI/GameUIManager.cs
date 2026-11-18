using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button resumeButton;

    public bool IsPaused { get; private set; }

    private RoundManager _round;
    private bool _subscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (Instance != this) return;

        quitButton.onClick.AddListener(OnQuitClicked);
        resumeButton.onClick.AddListener(TogglePause);
        pausePanel.SetActive(false);

        StartCoroutine(WaitForRoundManagerAndSubscribe());
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (_round != null && _subscribed)
        {
            _round.CurrentPhase.OnValueChanged -= OnPhaseChanged;
            _round.RoundOwnerClientId.OnValueChanged -= OnOwnerChanged;
        }

        _subscribed = false;

        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);
        if (resumeButton != null) resumeButton.onClick.RemoveListener(TogglePause);

        Instance = null;
    }

    private IEnumerator WaitForRoundManagerAndSubscribe()
    {
        while (RoundManager.Instance == null)
        {
            yield return null;
        }

        _round = RoundManager.Instance;
        _round.CurrentPhase.OnValueChanged += OnPhaseChanged;
        _round.RoundOwnerClientId.OnValueChanged += OnOwnerChanged;
        _subscribed = true;
    }

    private void OnPhaseChanged(GamePhase oldValue, GamePhase newValue) => CheckForceClosePause();
    private void OnOwnerChanged(ulong oldValue, ulong newValue) => CheckForceClosePause();

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private bool IsLocalPlayerAnsweringNow()
    {
        if (RoundManager.Instance == null) return false;
        if (RoundManager.Instance.CurrentPhase.Value != GamePhase.Question) return false;
        return NetworkManager.Singleton.LocalClientId == RoundManager.Instance.RoundOwnerClientId.Value;
    }

    private void TogglePause()
    {
        if (!IsPaused && IsLocalPlayerAnsweringNow())
        {
            Debug.Log("Can't pause while answering the question.");
            return;
        }

        IsPaused = !IsPaused;
        pausePanel.SetActive(IsPaused);
        NotifyLocalPlayer();
    }

    private void CheckForceClosePause()
    {
        if (IsPaused && IsLocalPlayerAnsweringNow())
        {
            IsPaused = false;
            pausePanel.SetActive(false);
            NotifyLocalPlayer();
        }
    }

    private void NotifyLocalPlayer()
    {
        var localPlayerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayerObj != null)
        {
            var movement = localPlayerObj.GetComponent<PlayerMovement>();
            if (movement != null) movement.RefreshFreezeState();
        }
    }

    private void OnQuitClicked()
    {
        LeaveGameAndReturnToMenu();
    }

    private void LeaveGameAndReturnToMenu()
    {
        var lobby = LobbyManager.Instance.CurrentLobby;
        if (lobby.Id != 0)
        {
            lobby.Leave();
        }

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        LobbyManager.Instance.ClearCurrentLobby();
        MainMenu.Instance.ResetToMainMenuLobby();
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}