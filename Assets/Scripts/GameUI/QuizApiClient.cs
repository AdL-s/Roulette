// QuizApiClient.cs
// Talks to the PHP backend. Put this on a GameObject (e.g. your RoundManager
// object), set Base Url in the Inspector, then call:
//
//   StartCoroutine(api.GetQuestion("en", "normal", q => { ... }));
//   StartCoroutine(api.CheckAnswer(q.id, pickedAnswerId, (ok, rightId) => { ... }));
//   StartCoroutine(api.GetImage(q.image_url, tex => { ... }));   // only if q.HasImage

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class QuizApiClient : MonoBehaviour
{
    [Tooltip("Folder on your server that contains the php files, no trailing slash. e.g. https://yourserver.com/quiz")]
    [SerializeField] private string baseUrl = "https://yourserver.com/quiz";

    // ---------------- GET QUESTION ----------------
    // onDone receives the QuestionData, or null if something failed.
    public IEnumerator GetQuestion(string lang, string diff, System.Action<QuestionData> onDone)
    {
        string url = $"{baseUrl}/get_question.php?lang={lang}&diff={diff}";

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[QuizApi] GetQuestion failed: {req.error}");
            onDone?.Invoke(null);
            yield break;
        }

        string json = req.downloadHandler.text;

        // the PHP sends {"error": "..."} when something is wrong
        if (json.Contains("\"error\""))
        {
            Debug.LogError($"[QuizApi] Server error: {json}");
            onDone?.Invoke(null);
            yield break;
        }

        QuestionData data = JsonUtility.FromJson<QuestionData>(json);
        onDone?.Invoke(data);
    }

    

    // ---------------- GET IMAGE (for picture questions) ----------------
    public IEnumerator GetImage(string url, System.Action<Texture2D> onDone)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[QuizApi] GetImage failed: {req.error}");
            onDone?.Invoke(null);
            yield break;
        }

        onDone?.Invoke(DownloadHandlerTexture.GetContent(req));
    }
}
