using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum GamePhase
{
    Question,
    Reveal,   // wrong answer - show correct answer highlighted for a few seconds
    Action,   // correct answer - placeholder for the next round phase, implement later
    Waiting   // brief gap after a timeout before the next round
}

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    [SerializeField] private float questionDuration = 15f;
    [SerializeField] private float revealDuration = 3f;
    [SerializeField] private float actionDuration = 60f; 
    public enum AmmoType { Blank, Live, Heal }
    
    private List<AmmoType> ammoQueue = new();
    private int loadCount = 0; // 1 = initial load, 2 = first reload, 3 = second reload, 4+ = heal starts
    
    private int currentActionToken = 0;

    

    public NetworkVariable<int> RoundNumber = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<GamePhase> CurrentPhase = new(GamePhase.Question, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<ulong> RoundOwnerClientId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString128Bytes> CurrentQuestion = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString64Bytes> AnswerA = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString64Bytes> AnswerB = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString64Bytes> AnswerC = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString64Bytes> AnswerD = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public NetworkVariable<int> RevealedCorrectIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> RevealedChosenIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<double> QuestionEndTime = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public float QuestionDuration => questionDuration;

    private readonly (string question, string[] answers, int correctIndex)[] placeholderQuestions =
    {
        ("What is your favorite color?", new[] { "Red", "Blue", "Green", "Yellow" }, 1),
        ("What's the capital of France?", new[] { "Berlin", "Madrid", "Paris", "Rome" }, 2),
        ("Which planet is closest to the sun?", new[] { "Venus", "Mercury", "Earth", "Mars" }, 1),
    };

    private int correctAnswerIndex;
    private int nextOwnerIndex = 0;
    private int currentRoundToken = 0;

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GenerateNewAmmo();
            StartRound();
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (clientId == RoundOwnerClientId.Value)
        {
            SkipToNextRound();
        }
    }

    private void StartRound()
    {
        var aliveIds = GetAliveClientIds();
        if (aliveIds.Count == 0) return;

        currentRoundToken++;
        int myToken = currentRoundToken;

        ulong owner = aliveIds[nextOwnerIndex % aliveIds.Count];
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

        StartCoroutine(QuestionTimeoutRoutine(myToken));
    }

    private IEnumerator QuestionTimeoutRoutine(int myToken)
    {
        yield return new WaitForSeconds(questionDuration);
        if (myToken != currentRoundToken) yield break;
        if (CurrentPhase.Value != GamePhase.Question) yield break;

        Debug.Log("Question timed out, skipping round.");
        SkipToNextRound();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitAnswerServerRpc(int answerIndex, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (senderId != RoundOwnerClientId.Value) return;
        if (CurrentPhase.Value != GamePhase.Question) return;

        if (answerIndex == correctAnswerIndex)
        {
            Debug.Log($"Client {senderId} answered CORRECTLY, entering Action phase.");
            CurrentPhase.Value = GamePhase.Action;

            currentActionToken++;
            int myActionToken = currentActionToken;
            StartCoroutine(ActionPhaseTimeout(myActionToken));
        }
        else
        {
            Debug.Log($"Client {senderId} answered incorrectly.");
            RevealedCorrectIndex.Value = correctAnswerIndex;
            RevealedChosenIndex.Value = answerIndex;
            CurrentPhase.Value = GamePhase.Reveal;
            StartCoroutine(RevealThenSkipRound());
        }
    }
    
    private IEnumerator ActionPhaseTimeout(int myToken)
    {
        yield return new WaitForSeconds(actionDuration);
        if (myToken != currentActionToken) yield break; 
        if (CurrentPhase.Value != GamePhase.Action) yield break;

        Debug.Log("Action phase timed out with no shot, skipping round.");
        AdvanceToNextRoundImmediately();
    }
    private IEnumerator RevealThenSkipRound()
    {
        yield return new WaitForSeconds(revealDuration);
        SkipToNextRound();
    }

    /*private IEnumerator ActionPhaseThenNextRound()
    {
        yield return new WaitForSeconds(actionDuration);
        AdvanceToNextRoundImmediately();
    }*/

    private void SkipToNextRound()
    {
        CurrentPhase.Value = GamePhase.Waiting;
        StartCoroutine(WaitingThenNextRound());
    }

    private IEnumerator WaitingThenNextRound()
    {
        yield return new WaitForSeconds(1f);
        AdvanceToNextRoundImmediately();
    }

    private void AdvanceToNextRoundImmediately()
    {
        RoundNumber.Value++;
        StartRound();
    }
    
    private void GenerateNewAmmo()
    {
        loadCount++;
        ammoQueue.Clear();

        int liveCount = Mathf.Min(5, loadCount);       // 1 on first load, +1 each reload, capped at 5
        int healCount = loadCount >= 3 ? 1 : 0;         // starts on the load after the 3ft reload
        int total = (loadCount >= 3) ? 7 : 6;
        int blankCount = total - liveCount - healCount;

        for (int i = 0; i < liveCount; i++) ammoQueue.Add(AmmoType.Live);
        for (int i = 0; i < healCount; i++) ammoQueue.Add(AmmoType.Heal);
        for (int i = 0; i < blankCount; i++) ammoQueue.Add(AmmoType.Blank);

        // Fisher-Yates shuffle
        for (int i = ammoQueue.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (ammoQueue[i], ammoQueue[j]) = (ammoQueue[j], ammoQueue[i]);
        }

        Debug.Log($"Ammo load #{loadCount}: {liveCount} live, {blankCount} blank, {healCount} heal.");
    }
    
    private AmmoType PopNextAmmo()
    {
        if (ammoQueue.Count == 0) GenerateNewAmmo();

        AmmoType result = ammoQueue[0];
        ammoQueue.RemoveAt(0);

        // If everything left is blank, there's no point continuing this batch - reload for next time.
        if (ammoQueue.Count > 0 && ammoQueue.All(a => a == AmmoType.Blank))
        {
            Debug.Log("Remaining ammo is all blank, reloading.");
            GenerateNewAmmo();
        }

        return result;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void TryAttackServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (senderId != RoundOwnerClientId.Value) return;
        if (CurrentPhase.Value != GamePhase.Action) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var targetClient)) return;
        var targetObj = targetClient.PlayerObject;
        if (targetObj == null) return;

        var targetHealth = targetObj.GetComponent<PlayerHealth>();
        if (targetHealth == null || targetHealth.IsDead.Value)
        {
            Debug.LogWarning("Cannot shoot a dead player.");
            return;
        }

        bool isSelf = senderId == targetClientId;
        AmmoType ammo = PopNextAmmo();

        switch (ammo)
        {
            case AmmoType.Live:
                targetHealth.TakeDamage(1);
                Debug.Log($"LIVE round: Client {senderId} hit {(isSelf ? "themselves" : $"Client {targetClientId}")}. Remaining lives: {targetHealth.Lives.Value}");
                break;
            case AmmoType.Blank:
                Debug.Log($"BLANK round: Client {senderId} shot {(isSelf ? "themselves" : $"Client {targetClientId}")}, no effect.");
                break;
            case AmmoType.Heal:
                targetHealth.Heal(1);
                Debug.Log($"HEAL round: Client {senderId} healed {(isSelf ? "themselves" : $"Client {targetClientId}")}. Lives now: {targetHealth.Lives.Value}");
                break;
        }

        NotifyShotClientRpc(senderId, targetClientId, ammo);

        currentActionToken++;
        StartCoroutine(EndActionPhaseAfterShot());
    }

    private IEnumerator EndActionPhaseAfterShot()
    {
        yield return new WaitForSeconds(1.5f);

        bool gameEnded = CheckForGameEnd();
        if (!gameEnded)
        {
            AdvanceToNextRoundImmediately();
        }
    }
    
    [ClientRpc]
    private void NotifyShotClientRpc(ulong shooterId, ulong targetId, AmmoType ammo)
    {
        Debug.Log($"[Shot feedback] Shooter {shooterId} -> Target {targetId}, {ammo}");
        // Hook VFX/SFX here later
    }
    
    
    private bool IsClientAlive(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return false;
        var health = client.PlayerObject != null ? client.PlayerObject.GetComponent<PlayerHealth>() : null;
        return health != null && !health.IsDead.Value;
    }
    
    private bool CheckForGameEnd()
    {
        var aliveIds = GetAliveClientIds();

        if (aliveIds.Count <= 1)
        {
            Debug.Log($"[GameEnd check] Only {aliveIds.Count} player(s) alive. Game would end here.");

            // Disabled for testing purposes - uncomment when ready to test end-of-game flow:
            // EndGameAndReturnToLobby();
            // return true;
        }

        return false;
    }

    private void EndGameAndReturnToLobby()
    {
        Debug.Log("Game over! Returning all players to the lobby.");
        NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
    
    private List<ulong> GetAliveClientIds()
    {
        return NetworkManager.Singleton.ConnectedClientsIds.Where(IsClientAlive).ToList();
    }
}
