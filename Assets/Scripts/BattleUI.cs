using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

public class BattleUI : MonoBehaviour
{
    [Header("Phase UI")]
    [SerializeField] TextMeshProUGUI turnText;
    [SerializeField] TextMeshProUGUI phaseGuideText;

    [Header("Selected Card Info")]
    [SerializeField] GameObject selectedCardInfoRoot;
    [SerializeField] TextMeshProUGUI selectedCardNameText;
    [SerializeField] TextMeshProUGUI selectedCardSkillText;
    [SerializeField] Image selectedCardTypeIcon;
    [SerializeField] TextMeshProUGUI selectedCardTypeText;

    [Header("Action Buttons")]
    [SerializeField] Button attackButton;
    [SerializeField] Button skillButton;

    [Header("Skill Count")]
    [SerializeField] Transform playerSkillCountRoot;
    [SerializeField] Transform enemySkillCountRoot;
    [SerializeField] GameObject skillCountPrefab;

    [Header("Result Panel")]
    [SerializeField] GameObject resultPanel;
    [SerializeField] TextMeshProUGUI resultText;
    [SerializeField] Button backToTitleButton;

    public Button AttackButton => attackButton;
    public Button SkillButton => skillButton;
    public Button BackToTitleButton => backToTitleButton;

    void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (selectedCardInfoRoot != null) selectedCardInfoRoot.SetActive(false);
        SetActionButtonsActive(false);
    }

    public void SetTurnText(bool isPlayerTurn)
    {
        if (turnText != null)
            turnText.text = isPlayerTurn ? "플레이어 차례" : "적 차례";
    }

    public void SetPhaseGuide(BattlePhase phase, bool isPlayerSkillSealed = false)
    {
        if (phaseGuideText == null) return;

        phaseGuideText.text = phase switch
        {
            BattlePhase.SelectCard => "카드를 선택하세요.",
            BattlePhase.SelectAction => "행동을 선택하세요.",
            BattlePhase.SelectTarget => "대상을 선택하세요.",
            BattlePhase.EnemyTurn => "적이 행동 중입니다...",
            BattlePhase.Executing => "",
            _ => ""
        };
    }

    public void ShowSelectedCardInfo(RuntimeCard card)
    {
        if (selectedCardInfoRoot == null) return;
        selectedCardInfoRoot.SetActive(card != null);

        if (card == null) return;
        if (selectedCardNameText != null) selectedCardNameText.text = card.Data.Name;
        if (selectedCardSkillText != null) selectedCardSkillText.text = card.Data.SkillDesc;
        if (selectedCardTypeIcon != null) selectedCardTypeIcon.sprite = card.Data.TypeIcon;
        if (selectedCardTypeText != null)
        {
            selectedCardTypeText.text = card.Data.Type switch
            {
                CardType.Normal => "일반",
                CardType.Ranged => "원거리",
                CardType.Peerless => "무쌍",
                CardType.Healer => "힐러",
                _ => ""
            };
        }
    }

    public void SetActionButtonsActive(bool active)
    {
        if (attackButton != null) attackButton.gameObject.SetActive(active);
        if (skillButton != null) skillButton.gameObject.SetActive(active);
    }

    public void SetSkillButtonInteractable(bool interactable)
    {
        if (skillButton != null) skillButton.interactable = interactable;
    }

    public void InitializeSkillCount(int count, bool isPlayer)
    {
        var root = isPlayer ? playerSkillCountRoot : enemySkillCountRoot;
        if (root == null || skillCountPrefab == null) return;

        for (int i = 0; i < count; i++)
            Instantiate(skillCountPrefab, root);
    }

    public void ConsumeSkillCount(bool isPlayer)
    {
        var root = isPlayer ? playerSkillCountRoot : enemySkillCountRoot;
        if (root == null || root.childCount <= 0) return;

        var last = root.GetChild(root.childCount - 1);
        Destroy(last.gameObject);
    }

    public void ShowResultPanel(bool victory)
    {
        if (resultPanel == null) return;
        resultPanel.SetActive(true);
        if (resultText != null)
            resultText.text = victory ? "승리" : "패배";
    }
}
