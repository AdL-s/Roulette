using System;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerBrowserRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text memberCountText;
    [SerializeField] private Button joinButton;

    public void Setup(Lobby lobby, Action onJoin)
    {
        string name = lobby.GetData("name");
        nameText.text = string.IsNullOrEmpty(name) ? "Lobby" : name;
        memberCountText.text = $"{lobby.MemberCount}/{lobby.MaxMembers}";

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => onJoin());
    }
}