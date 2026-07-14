using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerBrowserUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Transform listContainer;
    [SerializeField] private GameObject serverRowPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button closeButton;

    [Header("Join by code")]
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private Button joinByCodeButton;
    [SerializeField] private TMP_Text errorText;

    private readonly List<GameObject> spawnedRows = new();

    private void Start()
    {
        refreshButton.onClick.AddListener(() => _ = RefreshList());
        closeButton.onClick.AddListener(() => panel.SetActive(false));
        searchInput.onValueChanged.AddListener(text => { _ = RefreshList(); });
        joinByCodeButton.onClick.AddListener(OnJoinByCodeClicked);

        codeInput.characterLimit = LobbyCodeUtility.CodeLength;
        panel.SetActive(false);
        errorText.gameObject.SetActive(false);
    }

    public void Open()
    {
        panel.SetActive(true);
        _ = RefreshList();
    }

    private async Task RefreshList()
    {
        foreach (var row in spawnedRows) Destroy(row);
        spawnedRows.Clear();

        var lobbies = await LobbyManager.Instance.SearchLobbies(searchInput.text);
        foreach (var lobby in lobbies)
        {
            var rowObj = Instantiate(serverRowPrefab, listContainer);
            var row = rowObj.GetComponent<ServerBrowserRow>();
            row.Setup(lobby, () => OnJoinClicked(lobby));
            spawnedRows.Add(rowObj);
        }
    }

    private void OnJoinClicked(Lobby lobby)
    {
        LobbyManager.Instance.JoinLobbyDirect(lobby);
        panel.SetActive(false);
    }

    private void OnJoinByCodeClicked()
    {
        LobbyManager.Instance.JoinByCode(codeInput.text);
        panel.SetActive(false);
    }

    public void ShowError(string message)
    {
        errorText.text = message;
        errorText.gameObject.SetActive(true);
    }
}