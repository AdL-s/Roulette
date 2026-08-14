// QuizExample.cs
// Minimal example of the full flow. Not meant to ship - copy the pattern
// into your RoundManager / GameUIManager.
/*
using UnityEngine;

public class QuizExample : MonoBehaviour
{
    [SerializeField] private QuizApiClient api;

    private QuestionData current;

    void Start()
    {
        LoadNextQuestion();
    }

    public void LoadNextQuestion()
    {
        StartCoroutine(api.GetQuestion("en", "normal", q =>
        {
            if (q == null) return; // fetch failed, error already logged

            current = q;

            // ---- YOUR UI CODE ----
            // q.question          -> question text label
            // q.answers[i].text   -> answer button labels (0..3)
            // q.answers[i].id     -> remember per button, send it on click
            Debug.Log($"Q: {q.question}");
            for (int i = 0; i < q.answers.Length; i++)
                Debug.Log($"  [{i}] {q.answers[i].text} (id {q.answers[i].id})");

            if (q.HasImage)
                StartCoroutine(api.GetImage(q.image_url, tex =>
                {
                    if (tex == null) return;
                    // assign to a RawImage: myRawImage.texture = tex;
                }));
        }));
    }

    // Hook this to your answer buttons: OnAnswerPicked(q.answers[i].id)
    public void OnAnswerPicked(int answerId)
    {
        StartCoroutine(api.CheckAnswer(current.id, answerId, (wasCorrect, correctId) =>
        {
            // ---- YOUR UI CODE ----
            // wasCorrect -> play right/wrong feedback, deal damage, etc.
            // correctId  -> find the button whose answer id == correctId
            //               and highlight it green
            Debug.Log(wasCorrect ? "CORRECT!" : $"Wrong. Right answer id: {correctId}");
        }));
    }
}
*/