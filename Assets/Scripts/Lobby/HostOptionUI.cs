using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostOptionsUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private Toggle friendsOnlyToggle;
    [SerializeField] private Toggle privateToggle;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private void Start()
    {
        lobbyNameInput.text = "Lobby";
        confirmButton.onClick.AddListener(OnConfirmHost);
        cancelButton.onClick.AddListener(() => panel.SetActive(false));
        panel.SetActive(false);
    }

    public void Open() => panel.SetActive(true);

    private void OnConfirmHost()
    {
        string name = string.IsNullOrWhiteSpace(lobbyNameInput.text) ? "Lobby" : lobbyNameInput.text;
        LobbyManager.Instance.HostLobby(name, friendsOnlyToggle.isOn, privateToggle.isOn);
        panel.SetActive(false);
    }
}