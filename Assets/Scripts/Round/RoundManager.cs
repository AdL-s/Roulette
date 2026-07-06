using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum GamePhase
{
    Question,
    Waiting
}

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    public NetworkVariable<int> RoundNumber = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<GamePhase> CurrentPhase = new NetworkVariable<GamePhase>(GamePhase.Question, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<ulong> RoundOwnerClientId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString128Bytes> CurrentQuestion = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly string[] placeholderQuestions =
    {
        "What is your favorite color?",
        "What's the last thing you ate?",
        "If you could live anywhere, where would it be?"
    };

    private int nextOwnerIndex = 0;

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartRound();
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }
    
    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected. Current round owner = {RoundOwnerClientId.Value}");

        if (clientId == RoundOwnerClientId.Value)
        {
            Debug.Log("Round owner disconnected, skipping to next round.");
            RoundNumber.Value++;
            StartRound();
        }
    }

    private void StartRound()
    {
        var clientIds = NetworkManager.Singleton.ConnectedClientsIds;
        if (clientIds.Count == 0)
        {
            Debug.LogWarning("No connected clients left, cannot start a new round.");
            return;
        }

        ulong owner = clientIds[nextOwnerIndex % clientIds.Count];
        nextOwnerIndex++;

        RoundOwnerClientId.Value = owner;
        CurrentQuestion.Value = placeholderQuestions[Random.Range(0, placeholderQuestions.Length)];
        CurrentPhase.Value = GamePhase.Question;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitAnswerServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (senderId != RoundOwnerClientId.Value)
        {
            Debug.LogWarning($"Client {senderId} tried to answer but isn't the round owner.");
            return;
        }

        Debug.Log($"Client {senderId} answered round {RoundNumber.Value}.");

        CurrentPhase.Value = GamePhase.Waiting;
        StartCoroutine(NextRoundAfterDelay(2f));
    }

    private IEnumerator NextRoundAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        RoundNumber.Value++;
        StartRound();
    }
}