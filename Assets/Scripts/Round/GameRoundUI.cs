using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

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

    private RoundManager _round;
    private bool _subscribed;

    private void Start()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerClicked(index));
        }

        StartCoroutine(WaitForRoundManagerAndInit());
    }

    private void OnDestroy()
    {
        Unsubscribe();
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

        _round = RoundManager.Instance;

        if (_round.IsReady) HandleRoundManagerReady();
        else _round.OnReady += HandleRoundManagerReady;
    }

    private void HandleRoundManagerReady()
    {
        if (_round == null) return;

        _round.OnReady -= HandleRoundManagerReady;
        Subscribe();
        Refresh();
    }


    private void Subscribe()
    {
        if (_subscribed) return;
        _subscribed = true;

        _round.CurrentQuestion.OnValueChanged += OnText128Changed;
        _round.AnswerA.OnValueChanged += OnText64Changed;
        _round.AnswerB.OnValueChanged += OnText64Changed;
        _round.AnswerC.OnValueChanged += OnText64Changed;
        _round.AnswerD.OnValueChanged += OnText64Changed;
        _round.RoundOwnerClientId.OnValueChanged += OnUlongChanged;
        _round.CurrentPhase.OnValueChanged += OnPhaseChanged;
        _round.RevealedCorrectIndex.OnValueChanged += OnIntChanged;
    }

    private void Unsubscribe()
    {
        if (_round == null)
        {
            _subscribed = false;
            return;
        }

        _round.OnReady -= HandleRoundManagerReady;

        if (_subscribed)
        {
            _round.CurrentQuestion.OnValueChanged -= OnText128Changed;
            _round.AnswerA.OnValueChanged -= OnText64Changed;
            _round.AnswerB.OnValueChanged -= OnText64Changed;
            _round.AnswerC.OnValueChanged -= OnText64Changed;
            _round.AnswerD.OnValueChanged -= OnText64Changed;
            _round.RoundOwnerClientId.OnValueChanged -= OnUlongChanged;
            _round.CurrentPhase.OnValueChanged -= OnPhaseChanged;
            _round.RevealedCorrectIndex.OnValueChanged -= OnIntChanged;
        }

        _subscribed = false;
    }

    private void OnText128Changed(FixedString128Bytes oldValue, FixedString128Bytes newValue) => isDirty = true;
    private void OnText64Changed(FixedString64Bytes oldValue, FixedString64Bytes newValue) => isDirty = true;
    private void OnUlongChanged(ulong oldValue, ulong newValue) => isDirty = true;
    private void OnPhaseChanged(GamePhase oldValue, GamePhase newValue) => isDirty = true;
    private void OnIntChanged(int oldValue, int newValue) => isDirty = true;
}