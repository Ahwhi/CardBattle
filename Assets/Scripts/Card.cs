using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Enums;

public class Card : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IDropHandler
{
    [Header("Card Visual")]
    [SerializeField] Image cardBack;
    [SerializeField] Image cardArt;
    [SerializeField] Image typeIcon;

    [Header("Card Info")]
    [SerializeField] GameObject nameRoot;
    [SerializeField] GameObject starsRoot;
    [SerializeField] GameObject starPrefab;
    [SerializeField] TextMeshProUGUI nameText;

    [Header("Battle UI")]
    [SerializeField] GameObject hpRoot;
    [SerializeField] Slider hpSlider;
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] GameObject statusRoot;
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] Image selectionBorder;

    [Header("Collection UI")]
    [SerializeField] TextMeshProUGUI quantityText;

    private CardViewMode _mode;
    private CardData _data;
    private RuntimeCard _runtimeCard;
    private bool _isFaceUp;
    private Canvas _rootCanvas;
    private RectTransform _ghostImage;
    private bool _isDraggable = true;

    public readonly Subject<Card> OnCardClicked = new();
    public readonly Subject<Card> OnDragBeginSubject = new();
    public readonly Subject<Card> OnDragEndSubject = new();
    public readonly Subject<(Card target, Card dropped)> OnCardDroppedOnSubject = new();

    void Awake()
    {
        var c = GetComponentInParent<Canvas>();
        _rootCanvas = c != null ? c.rootCanvas : null;
    }

    public void SetupForCollection(CardData data, int starLevel, int quantity, int maxQuantity)
    {
        _mode = CardViewMode.Collection;
        _data = data;
        ApplyCardData(data, starLevel, true);

        if (quantityText != null)
            quantityText.text = $"{quantity}/{maxQuantity}";

        SetBattleUIActive(false);
        if (selectionBorder != null) selectionBorder.gameObject.SetActive(false);
    }

    public void SetupForShop(CardData data)
    {
        _mode = CardViewMode.Shop;
        _data = data;
        ApplyCardData(data, 1, false);
        if (starsRoot != null) starsRoot.SetActive(false);
        if (quantityText != null) quantityText.gameObject.SetActive(false);
        SetBattleUIActive(false);
        if (selectionBorder != null) selectionBorder.gameObject.SetActive(false);
    }

    public void SetupForDeckBuilder(CardData data, int starLevel)
    {
        _mode = CardViewMode.DeckBuilder;
        _data = data;
        ApplyCardData(data, starLevel, true);
        if (quantityText != null) quantityText.gameObject.SetActive(false);
        SetBattleUIActive(false);
        if (selectionBorder != null) selectionBorder.gameObject.SetActive(false);
    }

    public void SetupForBattleReserve(CardData data)
    {
        _mode = CardViewMode.Battle;
        _data = data;
        ApplyCardData(data, 1, false);
        if (quantityText != null) quantityText.gameObject.SetActive(false);
        SetBattleUIActive(false);
        if (selectionBorder != null) selectionBorder.gameObject.SetActive(false);
    }

    public void SetupForBattle(RuntimeCard runtimeCard)
    {
        _mode = CardViewMode.Battle;
        _runtimeCard = runtimeCard;
        _data = runtimeCard.Data;
        ApplyCardData(runtimeCard.Data, runtimeCard.StarLevel, true);

        if (quantityText != null) quantityText.gameObject.SetActive(false);
        SetBattleUIActive(true);
        if (selectionBorder != null) selectionBorder.gameObject.SetActive(false);

        runtimeCard.CurrentHP
            .Subscribe(_ => RefreshBattleUI())
            .AddTo(this);

        RefreshBattleUI();
    }

    private void ApplyCardData(CardData data, int starLevel, bool faceUp)
    {
        _isFaceUp = faceUp;
        if (cardBack != null) cardBack.gameObject.SetActive(!faceUp);

        // Always assign data to components regardless of faceUp.
        // Visibility is controlled separately so FlipToFront() reveals correct content.
        if (cardArt != null)
        {
            cardArt.gameObject.SetActive(faceUp);
            if (data.sprite != null) cardArt.sprite = data.sprite;
        }

        if (typeIcon != null)
        {
            typeIcon.gameObject.SetActive(faceUp);
            if (data.TypeIcon != null) typeIcon.sprite = data.TypeIcon;
        }

        if (nameRoot != null)
            nameRoot.SetActive(faceUp);

        if (nameText != null)
        {
            nameText.gameObject.SetActive(faceUp);
            nameText.text = data.Name;
        }

        if (starsRoot != null)
        {
            starsRoot.SetActive(faceUp);
            if (faceUp)
            {
                foreach (Transform child in starsRoot.transform)
                    Destroy(child.gameObject);

                for (int i = 0; i < starLevel && starPrefab != null; i++)
                    Instantiate(starPrefab, starsRoot.transform);
            }
        }
    }

    private void SetBattleUIActive(bool active)
    {
        if (hpRoot != null) hpRoot.SetActive(active);
        if (statusRoot != null) statusRoot.SetActive(active);
    }

    public void RefreshBattleUI()
    {
        if (_runtimeCard == null) return;

        if (hpSlider != null)
        {
            hpSlider.maxValue = _runtimeCard.MaxHP;
            hpSlider.value = _runtimeCard.CurrentHP.Value;
        }

        if (hpText != null)
            hpText.text = $"{_runtimeCard.CurrentHP.Value}/{_runtimeCard.MaxHP}";

        RefreshStatusText();
    }

    private void RefreshStatusText()
    {
        if (statusText == null || _runtimeCard == null) return;

        var parts = new List<string>();
        foreach (var effect in _runtimeCard.ActiveEffects)
        {
            string label = effect.Type switch
            {
                StatusEffectType.Stun => "스턴",
                StatusEffectType.Bleed => "출혈",
                StatusEffectType.SealSkill => "스킬 봉인",
                _ => ""
            };
            if (!string.IsNullOrEmpty(label))
                parts.Add($"{label}({effect.RemainingTurns})");
        }

        statusText.text = string.Join(" ", parts);
        statusRoot?.SetActive(parts.Count > 0);
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(selected);

        if (selected)
            transform.DOScale(1.08f, 0.12f).SetEase(Ease.OutBack);
        else
            transform.DOScale(1f, 0.1f);
    }

    public void SetInDeckHighlight(bool inDeck)
    {
        if (cardArt != null)
            cardArt.color = inDeck ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
    }

    public async UniTask FlipToFront()
    {
        if (_isFaceUp) return;
        AppDirector.I.Sound?.PlaySfx(SfxKey.CardFlip);

        float originalScaleX = transform.localScale.x;

        await DOTween.Sequence()
            .Append(transform.DOScaleX(0f, 0.15f))
            .AppendCallback(() =>
            {
                _isFaceUp = true;
                if (cardBack != null) cardBack.gameObject.SetActive(false);
                if (cardArt != null) cardArt.gameObject.SetActive(true);
                if (typeIcon != null) typeIcon.gameObject.SetActive(true);
                if (nameRoot != null) nameRoot.SetActive(true);
                if (nameText != null) nameText.gameObject.SetActive(true);
                if (starsRoot != null) starsRoot.SetActive(false);
            })
            .Append(transform.DOScaleX(originalScaleX, 0.15f))
            .AsyncWaitForCompletion();
    }

    public async UniTask PlayMeleeAttack(Transform targetTransform)
    {
        Vector3 origin = transform.position;
        Vector3 dir = (targetTransform.position - origin).normalized;
        Vector3 approach = targetTransform.position - dir * 80f;

        await DOTween.Sequence()
            .Append(transform.DOMove(approach, 0.22f).SetEase(Ease.OutQuad))
            .Append(transform.DOMove(origin, 0.18f).SetEase(Ease.InQuad))
            .AsyncWaitForCompletion();
    }

    public async UniTask PlayRangedAttack(bool isPlayer)
    {
        Vector3 origin = transform.localPosition;
        float nudge = isPlayer ? 40f : -40f;

        await DOTween.Sequence()
            .Append(transform.DOLocalMoveY(origin.y + nudge, 0.1f).SetEase(Ease.OutQuad))
            .Append(transform.DOLocalMoveY(origin.y, 0.1f).SetEase(Ease.InQuad))
            .AsyncWaitForCompletion();
    }

    public async UniTask PlayHitEffect()
    {
        await transform.DOShakePosition(0.25f, 12f, 20).AsyncWaitForCompletion();
    }

    public async UniTask PlayDeathAnimation()
    {
        var cg = GetComponent<CanvasGroup>();
        var seq = DOTween.Sequence()
            .Append(transform.DOScale(0f, 0.3f).SetEase(Ease.InBack));
        if (cg != null) seq.Join(cg.DOFade(0f, 0.3f));
        await seq.AsyncWaitForCompletion();

        gameObject.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_mode != CardViewMode.DeckBuilder) return;
        var droppedCard = eventData.pointerDrag?.GetComponent<Card>();
        if (droppedCard == null || droppedCard == this) return;
        OnCardDroppedOnSubject.OnNext((this, droppedCard));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isFaceUp)
        {
            if (_mode == CardViewMode.Shop) FlipToFront().Forget();
            return;
        }
        OnCardClicked.OnNext(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_mode != CardViewMode.DeckBuilder || !_isDraggable) return;
        if (_rootCanvas == null) return;

        OnDragBeginSubject.OnNext(this);

        if (_ghostImage != null)
        {
            Destroy(_ghostImage.gameObject);
            _ghostImage = null;
        }

        Vector2 originalSize  = GetComponent<RectTransform>().rect.size;
        Vector3 originalScale = transform.localScale;

        // Clone the entire card so the ghost looks exactly like the original.
        // Do NOT disable MonoBehaviours — doing so also disables Image and TMP
        // which are Graphic components that drive Canvas rendering.
        // blocksRaycasts=false on CanvasGroup is sufficient to prevent interaction.
        var ghostGo = Instantiate(gameObject, _rootCanvas.transform, false);
        ghostGo.name = "DragGhost";

        var cg = ghostGo.GetComponent<CanvasGroup>();
        if (cg == null) cg = ghostGo.AddComponent<CanvasGroup>();
        cg.alpha          = 0.7f;
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        // Override sorting so the ghost always renders above other panels/canvases
        var ghostCanvas = ghostGo.AddComponent<Canvas>();
        ghostCanvas.overrideSorting = true;
        ghostCanvas.sortingOrder    = 999;

        _ghostImage           = ghostGo.GetComponent<RectTransform>();
        _ghostImage.anchorMin = new Vector2(0.5f, 0.5f);
        _ghostImage.anchorMax = new Vector2(0.5f, 0.5f);
        _ghostImage.pivot     = new Vector2(0.5f, 0.5f);
        _ghostImage.sizeDelta = originalSize;
        _ghostImage.localScale = originalScale;
        _ghostImage.SetAsLastSibling();

        UpdateGhostPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_mode != CardViewMode.DeckBuilder || _ghostImage == null) return;
        UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_mode != CardViewMode.DeckBuilder) return;

        if (_ghostImage != null)
        {
            Destroy(_ghostImage.gameObject);
            _ghostImage = null;
        }

        OnDragEndSubject.OnNext(this);
    }

    private void UpdateGhostPosition(PointerEventData eventData)
    {
        if (_ghostImage == null || _rootCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
            out Vector2 localPoint);

        _ghostImage.localPosition = localPoint;
    }

    public void SetDraggable(bool value) => _isDraggable = value;

    public CardData GetCardData() => _data;
    public RuntimeCard GetRuntimeCard() => _runtimeCard;

    void OnDestroy()
    {
        OnCardClicked.Dispose();
        OnDragBeginSubject.Dispose();
        OnDragEndSubject.Dispose();
        OnCardDroppedOnSubject.Dispose();

        if (_ghostImage != null)
            Destroy(_ghostImage.gameObject);
    }
}

