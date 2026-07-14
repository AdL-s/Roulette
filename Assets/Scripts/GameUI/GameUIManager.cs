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
        quitButton.onClick.AddListener(OnQuitClicked);
        resumeButton.onClick.AddListener(TogglePause);
        pausePanel.SetActive(false);

        StartCoroutine(WaitForRoundManagerAndSubscribe());
    }

    private IEnumerator WaitForRoundManagerAndSubscribe()
    {
        while (RoundManager.Instance == null)
        {
            yield return null;
        }

        RoundManager.Instance.CurrentPhase.OnValueChanged += (_, _) => CheckForceClosePause();
        RoundManager.Instance.RoundOwnerClientId.OnValueChanged += (_, _) => CheckForceClosePause();
    }

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
        // Block opening the pause menu while you're the one answering
        if (!IsPaused && IsLocalPlayerAnsweringNow())
        {
            Debug.Log("Can't pause while answering the question.");
            return;
        }

        IsPaused = !IsPaused;
        pausePanel.SetActive(IsPaused);
        NotifyLocalPlayer();
    }

    // If a paused player becomes the round owner during Question phase, force-close the pause menu
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