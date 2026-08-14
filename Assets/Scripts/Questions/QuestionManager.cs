using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Question: MonoBehaviour
{
    public string Text;
    public string[] Choices;   // e.g. ["Paris", "London", "Berlin", "Rome"]
    public int CorrectIndex;   // index into Choices

    public bool IsCorrect(int choiceIndex) => choiceIndex == CorrectIndex;
}

// QuestionManager is now a MonoBehaviour that prefetches questions from the
// database through QuizApiClient. GetRandom() stays SYNCHRONOUS: it pops from
// a buffer of already-downloaded questions and triggers a background refill.
// If the buffer is empty (server down / no internet), it falls back to the
// hardcoded questions so the game never stalls.
//
// SETUP: lives on the same GameObject as QuizApiClient. Only the HOST should
// call GetRandom() (which is already the case - RoundManager is host-authoritative).
public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance { get; private set; }

    [SerializeField] private QuizApiClient api;

    [Header("Question filter")]
    [SerializeField] private string language = "en";
    [SerializeField] private string difficulty = "normal";

    [Header("Prefetch")]
    [Tooltip("How many questions to keep downloaded and ready")]
    [SerializeField] private int bufferSize = 3;

    private readonly Queue<Question> _buffer = new();
    private readonly HashSet<int> _bufferedIds = new();  // avoid duplicates in buffer
    private int _pendingFetches = 0;

    // hardcoded fallback deck (same behaviour as the old QuestionManager)
    private List<Question> _fallback;
    private List<Question> _fallbackRemaining;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _fallback = BuildFallbackQuestions();
        _fallbackRemaining = new List<Question>(_fallback);
    }

    private void Start()
    {
        if (api == null) api = GetComponent<QuizApiClient>();
        TopUpBuffer();
    }

    // -------------------------------------------------------
    // PUBLIC API - same as before
    // -------------------------------------------------------
    public Question GetRandom()
    {
        TopUpBuffer(); // kick off replacement fetches for whatever we take

        if (_buffer.Count > 0)
        {
            Question q = _buffer.Dequeue();
            return q;
        }

        // Server hasn't delivered anything (yet) - use fallback deck
        Debug.LogWarning("[QuestionManager] Buffer empty, using fallback question.");
        return GetFallback();
    }

    // -------------------------------------------------------
    // Prefetching
    // -------------------------------------------------------
    private void TopUpBuffer()
    {
        while (_buffer.Count + _pendingFetches < bufferSize)
        {
            _pendingFetches++;
            StartCoroutine(api.GetQuestion(language, difficulty, OnQuestionReceived));
        }
    }

    private void OnQuestionReceived(QuestionData data)
    {
        _pendingFetches--;

        if (data == null) return;               // fetch failed, error already logged
        if (_bufferedIds.Contains(data.id)) return;  // duplicate of something buffered, skip

        _buffer.Enqueue(new Question
        {
            Text = data.question,
            Choices = data.answers,
            CorrectIndex = data.correct_index
        });
        _bufferedIds.Add(data.id);

        // keep the dedupe set small: only remember the last ~20 ids
        if (_bufferedIds.Count > 20) _bufferedIds.Clear();
    }

    // -------------------------------------------------------
    // Fallback deck (old hardcoded behaviour, kept for offline safety)
    // -------------------------------------------------------
    private Question GetFallback()
    {
        if (_fallbackRemaining.Count == 0)
            _fallbackRemaining = new List<Question>(_fallback);

        int index = Random.Range(0, _fallbackRemaining.Count);
        Question q = _fallbackRemaining[index];
        _fallbackRemaining.RemoveAt(index);
        return q;
    }

    private List<Question> BuildFallbackQuestions() => new List<Question>
    {
        new Question
        {
            Text        = "What is the capital of France?",
            Choices     = new[] { "Paris", "London", "Berlin", "Rome" },
            CorrectIndex = 0
        },
        new Question
        {
            Text        = "How many sides does a hexagon have?",
            Choices     = new[] { "5", "6", "7", "8" },
            CorrectIndex = 1
        },
        new Question
        {
            Text        = "What is 7 × 8?",
            Choices     = new[] { "54", "56", "58", "64" },
            CorrectIndex = 1
        },
        new Question
        {
            Text        = "Which planet is closest to the Sun?",
            Choices     = new[] { "Venus", "Earth", "Mercury", "Mars" },
            CorrectIndex = 2
        },
        new Question
        {
            Text        = "What gas do plants absorb from the air?",
            Choices     = new[] { "Oxygen", "Nitrogen", "Carbon Dioxide", "Hydrogen" },
            CorrectIndex = 2
        },
    };
}