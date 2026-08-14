// QuizData.cs
// Mirrors the JSON from get_question.php. Field names must match JSON keys.

[System.Serializable]
public class QuestionData
{
    public int id;
    public string question;
    public string image_url;
    public string[] answers;      // 4 shuffled answer texts
    public int correct_index;     // index into answers (host-only knowledge)

    public bool HasImage => !string.IsNullOrEmpty(image_url);
    
    
    
}
[System.Serializable]
public class CheckResult
{
    public bool correct;
    public int correct_answer_id;
}