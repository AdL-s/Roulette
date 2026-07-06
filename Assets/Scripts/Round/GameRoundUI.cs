using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameRoundUI : MonoBehaviour
{
    [SerializeField] private GameObject questionPanel; // the rectangle
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button answerButton;

    private void Start()
    {
        answerButton.onClick.AddListener(OnAnswerClicked);

        RoundManager.Instance.CurrentQuestion.OnValueChanged += (_, _) => Refresh();
        RoundManager.Instance.RoundOwnerClientId.OnValueChanged += (_, _) => Refresh();
        RoundManager.Instance.CurrentPhase.OnValueChanged += (_, _) => Refresh();

        Refresh();
    }

    private void Refresh()
    {
        bool isQuestionPhase = RoundManager.Instance.CurrentPhase.Value == GamePhase.Question;
        questionPanel.SetActive(isQuestionPhase);

        if (!isQuestionPhase) return; // nothing else to update while hidden

        questionText.text = RoundManager.Instance.CurrentQuestion.Value.ToString();

        bool isOwner = NetworkManager.Singleton.LocalClientId == RoundManager.Instance.RoundOwnerClientId.Value;
        answerButton.gameObject.SetActive(isOwner);
    }

    private void OnAnswerClicked()
    {
        RoundManager.Instance.SubmitAnswerServerRpc();
    }
}