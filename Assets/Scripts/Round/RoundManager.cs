using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum GamePhase
{
    Question,
    Reveal, // wrong answer - show correct answer for a few seconds
    Action, // correct answer - owner shoots or uses items
    Waiting, // short gap before the next round
    GameOver // someone reached the winning points (or everyone else left)
}

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    public enum AmmoType
    {
        Blank,
        Live,
        Heal
    }

    [Header("Timing")] [SerializeField] private float questionDuration = 15f;
    [SerializeField] private float revealDuration = 3f;
    [SerializeField] private float actionDuration = 60f;
    [SerializeField] private float shotResultDuration = 1.5f;
    [SerializeField] private float gameOverDuration = 4f;

    [Header("Points")] [SerializeField, Min(0)]
    private int startingPoints = 250;

    [SerializeField, Min(1)] private int winningPoints = 1000;
    [SerializeField, Min(0)] private int pointsForCorrectAnswer = 125;
    [SerializeField, Min(0)] private int pointsLostOnHit = 125;
    [SerializeField, Min(0)] private int pointsGainedOnHeal = 125;

    [Header("Ammo chances (weights, normalized automatically: 35/5/60 = 35%/5%/60%)")] [SerializeField, Min(0f)]
    private float blankChance = 35f;

    [SerializeField, Min(0f)] private float healChance = 5f;
    [SerializeField, Min(0f)] private float liveChance = 60f;

    public bool IsReady { get; private set; }
    public event System.Action OnReady;

    public NetworkVariable<int> RoundNumber =
        new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<GamePhase> CurrentPhase = new(GamePhase.Question, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> RoundOwnerClientId =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> WinnerClientId = new(ulong.MaxValue, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString128Bytes> CurrentQuestion =
        new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> AnswerA = new("", NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> AnswerB = new("", NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> AnswerC = new("", NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> AnswerD = new("", NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> RevealedCorrectIndex =
        new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> RevealedChosenIndex =
        new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<double> QuestionEndTime =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float QuestionDuration => questionDuration;

    private readonly (string question, string[] answers, int correctIndex)[] placeholderQuestions =
    {
        ("What is your favorite color?", new[] { "Red", "Blue", "Green", "Yellow" }, 1),
        ("What's the capital of France?", new[] { "Berlin", "Madrid", "Paris", "Rome" }, 2),
        ("Which planet is closest to the sun?", new[] { "Venus", "Mercury", "Earth", "Mars" }, 1),
    };

    // Server-only state
    private int correctAnswerIndex;
    private int nextOwnerIndex = 0;
    private int currentRoundToken = 0;
    private bool hasShotThisAction = false;
    private bool gameOver = false;

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (LobbyManager.Instance != null) LobbyManager.Instance.SetGameInProgress(true);
            StartCoroutine(WaitForPlayersThenStartRound());
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }
        else
        {
            IsReady = true;
            OnReady?.Invoke();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (LobbyManager.Instance != null) LobbyManager.Instance.SetGameInProgress(false);
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private PlayerPoints GetPoints(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
        return client.PlayerObject != null ? client.PlayerObject.GetComponent<PlayerPoints>() : null;
    }

    private List<ulong> GetPlayerClientIds(ulong exclude = ulong.MaxValue)
    {
        var list = new List<ulong>();
        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (id != exclude && GetPoints(id) != null) list.Add(id);
        }

        return list;
    }

    private bool AllPlayersSpawned()
    {
        var clients = NetworkManager.Singleton.ConnectedClientsList;
        if (clients.Count == 0) return false;
        foreach (var c in clients)
        {
            if (c.PlayerObject == null) return false;
        }

        return true;
    }

    private void ChangePoints(ulong clientId, int amount)
    {
        var points = GetPoints(clientId);
        if (points == null) return;

        points.AddPoints(amount);
        if (points.Points.Value >= winningPoints) EndGame(clientId);
    }


    private IEnumerator WaitForPlayersThenStartRound()
    {
        while (!AllPlayersSpawned()) yield return null;

        foreach (ulong id in GetPlayerClientIds())
            GetPoints(id).SetPoints(startingPoints);

        StartRound();
        IsReady = true;
        OnReady?.Invoke();
    }

    private void EndGame(ulong winnerId)
    {
        if (gameOver) return;
        gameOver = true;

        WinnerClientId.Value = winnerId;
        CurrentPhase.Value = GamePhase.GameOver;
        Debug.Log($"Game over. Winner: client {winnerId}");

        StartCoroutine(ReturnToLobbyAfterDelay());
    }

    private IEnumerator ReturnToLobbyAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDuration);

        foreach (var state in FindObjectsByType<PlayerLobbyState>(FindObjectsSortMode.None))
            state.ResetReadyClientRpc();

        NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (gameOver) return;
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.ShutdownInProgress) return;

        var remaining = GetPlayerClientIds(clientId);

        if (remaining.Count <= 1)
        {
            EndGame(remaining.Count == 1 ? remaining[0] : ulong.MaxValue);
            return;
        }

        if (clientId == RoundOwnerClientId.Value && CurrentPhase.Value != GamePhase.Waiting)
            SkipToNextRound(currentRoundToken);
    }

    private void StartRound()
    {
        if (gameOver) return;

        var players = GetPlayerClientIds();
        if (players.Count == 0) return;

        currentRoundToken++;
        hasShotThisAction = false;

        ulong owner = players[nextOwnerIndex % players.Count];
        nextOwnerIndex++;
        RoundOwnerClientId.Value = owner;

        var q = placeholderQuestions[Random.Range(0, placeholderQuestions.Length)];
        CurrentQuestion.Value = q.question;
        AnswerA.Value = q.answers[0];
        AnswerB.Value = q.answers[1];
        AnswerC.Value = q.answers[2];
        AnswerD.Value = q.answers[3];
        correctAnswerIndex = q.correctIndex;

        RevealedCorrectIndex.Value = -1;
        RevealedChosenIndex.Value = -1;

        QuestionEndTime.Value = NetworkManager.Singleton.ServerTime.Time + questionDuration;
        CurrentPhase.Value = GamePhase.Question;

        StartCoroutine(QuestionTimeoutRoutine(currentRoundToken));
    }

    private void AdvanceRound(int token)
    {
        if (gameOver || token != currentRoundToken) return;
        RoundNumber.Value++;
        StartRound();
    }

    private void SkipToNextRound(int token)
    {
        if (gameOver) return;
        CurrentPhase.Value = GamePhase.Waiting;
        StartCoroutine(WaitingThenNextRound(token));
    }

    private IEnumerator WaitingThenNextRound(int token)
    {
        yield return new WaitForSeconds(1f);
        AdvanceRound(token);
    }

    private IEnumerator QuestionTimeoutRoutine(int token)
    {
        yield return new WaitForSeconds(questionDuration);
        if (token != currentRoundToken || CurrentPhase.Value != GamePhase.Question) yield break;

        Debug.Log("Question timed out, skipping round.");
        SkipToNextRound(token);
    }

    private IEnumerator RevealThenSkipRound(int token)
    {
        yield return new WaitForSeconds(revealDuration);
        if (token != currentRoundToken) yield break;
        SkipToNextRound(token);
    }

    private IEnumerator ActionPhaseTimeout(int token)
    {
        yield return new WaitForSeconds(actionDuration);
        if (token != currentRoundToken || CurrentPhase.Value != GamePhase.Action) yield break;

        Debug.Log("Action phase timed out with no shot, skipping round.");
        AdvanceRound(token);
    }

    private IEnumerator EndActionPhaseAfterShot(int token)
    {
        yield return new WaitForSeconds(shotResultDuration);
        AdvanceRound(token);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitAnswerServerRpc(int answerIndex, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (senderId != RoundOwnerClientId.Value) return;
        if (CurrentPhase.Value != GamePhase.Question) return;

        if (answerIndex == correctAnswerIndex)
        {
            ChangePoints(senderId, pointsForCorrectAnswer);
            if (gameOver) return;

            CurrentPhase.Value = GamePhase.Action;
            StartCoroutine(ActionPhaseTimeout(currentRoundToken));
        }
        else
        {
            RevealedCorrectIndex.Value = correctAnswerIndex;
            RevealedChosenIndex.Value = answerIndex;
            CurrentPhase.Value = GamePhase.Reveal;
            StartCoroutine(RevealThenSkipRound(currentRoundToken));
        }
    }


    private AmmoType RollAmmo()
    {
        float total = blankChance + healChance + liveChance;
        if (total <= 0f) return AmmoType.Blank;

        float roll = Random.Range(0f, total);
        if (roll < blankChance) return AmmoType.Blank;
        if (roll < blankChance + healChance) return AmmoType.Heal;
        return AmmoType.Live;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryAttackServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (senderId != RoundOwnerClientId.Value) return;
        if (CurrentPhase.Value != GamePhase.Action) return;
        if (hasShotThisAction) return;
        if (GetPoints(targetClientId) == null) return;

        hasShotThisAction = true;

        AmmoType ammo = RollAmmo();
        switch (ammo)
        {
            case AmmoType.Live:
                ChangePoints(targetClientId, -pointsLostOnHit);
                break;
            case AmmoType.Heal:
                ChangePoints(targetClientId, pointsGainedOnHeal);
                break;
        }

        NotifyShotClientRpc(senderId, targetClientId, ammo);

        if (gameOver) return;
        StartCoroutine(EndActionPhaseAfterShot(currentRoundToken));
    }

    [ClientRpc]
    private void NotifyShotClientRpc(ulong shooterId, ulong targetId, AmmoType ammo)
    {
        Debug.Log($"[Shot feedback] Shooter {shooterId} -> Target {targetId}, {ammo}");
        // Hook VFX/SFX here later
    }
}