using System.Collections.Generic;
using Unity.Netcode;

public class LobbyManagerServer : NetworkBehaviour
{
    public static LobbyManagerServer Instance { get; private set; }

    private readonly List<PlayerLobbyState> players = new List<PlayerLobbyState>();

    private void Awake() => Instance = this;

    public void RegisterPlayer(PlayerLobbyState player)
    {
        players.Add(player);
        CheckAllReady();
    }

    public void UnregisterPlayer(PlayerLobbyState player)
    {
        players.Remove(player);
        CheckAllReady();
    }

    // Call this whenever a ready state changes too (hook into IsReady.OnValueChanged server-side, or poll)
    public void CheckAllReady()
    {
        if (!IsServer) return;
        if (players.Count < 2) return;

        bool allReady = true;
        foreach (var p in players)
        {
            if (!p.IsReady.Value) { allReady = false; break; }
        }

        if (allReady)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}