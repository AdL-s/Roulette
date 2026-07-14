using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class GameRoundUI : MonoBehaviour
{
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button[] answerButtons;
    [SerializeField] private TMP_Text[] answerButtonTexts;
    [SerializeField] private Image[] answerButtonBackgrounds;
    [SerializeField] private Image timerFillBar;

    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;
    
    private bool isDirty = false;


    private void Start()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerClicked(index));
        }

        StartCoroutine(WaitForRoundManagerAndInit());
    }
    
    private void Update()
    {
        if (RoundManager.Instance == null) return;

        if (isDirty)
        {
            isDirty = false;
            Refresh();
        }

        if (RoundManager.Instance.CurrentPhase.Value != GamePhase.Question) return;

        double remaining = RoundManager.Instance.QuestionEndTime.Value - NetworkManager.Singleton.ServerTime.Time;
        timerFillBar.fillAmount = Mathf.Clamp01((float)(remaining / RoundManager.Instance.QuestionDuration));
    }


    private void MarkDirty(object a = null, object b = null) => isDirty = true;

    private void Refresh()
    {
        var phase = RoundManager.Instance.CurrentPhase.Value;

        bool showPanel = phase == GamePhase.Question || phase == GamePhase.Reveal;
        questionPanel.SetActive(showPanel);
        if (!showPanel) return;

        questionText.text = RoundManager.Instance.CurrentQuestion.Value.ToString();
        answerButtonTexts[0].text = RoundManager.Instance.AnswerA.Value.ToString();
        answerButtonTexts[1].text = RoundManager.Instance.AnswerB.Value.ToString();
        answerButtonTexts[2].text = RoundManager.Instance.AnswerC.Value.ToString();
        answerButtonTexts[3].text = RoundManager.Instance.AnswerD.Value.ToString();

        bool isOwner = NetworkManager.Singleton.LocalClientId == RoundManager.Instance.RoundOwnerClientId.Value;

        if (phase == GamePhase.Question)
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                answerButtons[i].gameObject.SetActive(isOwner);
                answerButtons[i].interactable = true;
                answerButtonBackgrounds[i].color = defaultColor;
            }
            timerFillBar.fillAmount = 1f;
        }
        else if (phase == GamePhase.Reveal)
        {
            int correctIndex = RoundManager.Instance.RevealedCorrectIndex.Value;
            int chosenIndex = RoundManager.Instance.RevealedChosenIndex.Value;

            for (int i = 0; i < answerButtons.Length; i++)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].interactable = false;

                if (i == correctIndex) answerButtonBackgrounds[i].color = correctColor;
                else if (i == chosenIndex) answerButtonBackgrounds[i].color = wrongColor;
                else answerButtonBackgrounds[i].color = defaultColor;
            }
        }
    }

    private void OnAnswerClicked(int index)
    {
        RoundManager.Instance.SubmitAnswerServerRpc(index);
        foreach (var btn in answerButtons) btn.interactable = false;
    }
    
    private IEnumerator WaitForRoundManagerAndInit()
    {
        while (RoundManager.Instance == null)
        {
            yield return null;
        }

        if (RoundManager.Instance.IsReady)
        {
            HandleRoundManagerReady();
        }
        else
        {
            RoundManager.Instance.OnReady += HandleRoundManagerReady;
        }
    }

    private void HandleRoundManagerReady()
    {
        RoundManager.Instance.CurrentQuestion.OnValueChanged += (_, _) => MarkDirty();
        RoundManager.Instance.AnswerA.OnValueChanged += (_, _) => MarkDirty();
        RoundManager.Instance.AnswerB.OnValueChanged += (_, _) => MarkDirty();
        RoundManager.Instance.AnswerC.OnValueChanged += (_, _) => MarkDirty();
        RoundManager.Instance.AnswerD.OnValueChanged += (_, _) => MarkDirty();
        RoundManager.Instance.RoundOwnerClientId.OnValueChanged += (_, _) => MarkDirty();
        RoundManager.Instance.CurrentPhase.OnValueChanged += (_, _) => MarkDirty();
        RoundManager.Instance.RevealedCorrectIndex.OnValueChanged += (_, _) => MarkDirty();
        
        

        Refresh();
    }
    
    
}