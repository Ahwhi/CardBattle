using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

public class DeckPanel : MonoBehaviour
{
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Transform collectionGrid;
    [SerializeField] Transform deckSlotsRoot;
    [SerializeField] Button closeButton;
    [SerializeField] TextMeshProUGUI deckStatusText;

    private readonly List<Card> _collectionCards = new();
    private DeckSlot[] _deckSlots;

    void Awake()
    {
        InitDeckSlots();
    }

    void Start()
    {
        closeButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); Close(); })
            .AddTo(this);
    }

    private void InitDeckSlots()
    {
        _deckSlots = deckSlotsRoot.GetComponentsInChildren<DeckSlot>(true);
        for (int i = 0; i < _deckSlots.Length; i++)
        {
            var slot = _deckSlots[i];
            slot.Initialize(i);
            slot.OnCardDropped
                .Subscribe(pair => HandleCardDropOnSlot(pair.slot, pair.card))
                .AddTo(this);
        }
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        RefreshCollectionGrid();
        RefreshDeckSlots();
        RefreshDeckStatus();
    }

    private void RefreshCollectionGrid()
    {
        foreach (var card in _collectionCards)
            if (card != null) Destroy(card.gameObject);
        _collectionCards.Clear();

        foreach (var cardData in AppDirector.I.CardDB.GetAll())
        {
            if (!CardManager.Has(cardData.ID)) continue;
            if (DeckManager.IsInDeck(cardData.ID)) continue;
            SpawnCollectionCard(cardData);
        }
    }

    private void SpawnCollectionCard(CardData cardData)
    {
        var go = Instantiate(cardPrefab, collectionGrid);
        go.transform.localScale = Vector3.one * 0.75f;
        var card = go.GetComponent<Card>();
        var info = CardManager.Get(cardData.ID);
        card?.SetupForDeckBuilder(cardData, info?.StarLevel ?? 1);

        card?.OnCardDroppedOnSubject
            .Subscribe(pair => HandleCardDroppedOnCard(pair.target, pair.dropped))
            .AddTo(this);

        _collectionCards.Add(card);
    }

    private void RefreshDeckSlots()
    {
        var deck = DeckManager.GetDeck();
        for (int i = 0; i < _deckSlots.Length; i++)
        {
            ClearSlotCard(_deckSlots[i]);

            if (i < deck.Count)
            {
                var cardData = AppDirector.I.CardDB.Get(deck[i]);
                if (cardData != null) SpawnDeckSlotCard(_deckSlots[i], cardData);
            }
        }
    }

    private void SpawnDeckSlotCard(DeckSlot slot, CardData cardData)
    {
        var go = Instantiate(cardPrefab, slot.transform);
        go.transform.localScale = Vector3.one * 0.75f;
        var card = go.GetComponent<Card>();
        var info = CardManager.Get(cardData.ID);
        card?.SetupForDeckBuilder(cardData, info?.StarLevel ?? 1);

        card?.OnCardDroppedOnSubject
            .Subscribe(pair => HandleCardDroppedOnCard(pair.target, pair.dropped))
            .AddTo(this);

        slot.SetCard(card);
    }

    // DeckSlot.OnDrop - empty slot only (occupied slots are intercepted by Card.OnDrop)
    private void HandleCardDropOnSlot(DeckSlot targetSlot, Card droppedCard)
    {
        var cardData = droppedCard.GetCardData();
        if (cardData == null) return;

        if (FindSlotForCard(droppedCard) != null) return;

        if (DeckManager.IsInDeck(cardData.ID)) return;
        if (targetSlot.IsOccupied) return;

        if (!DeckManager.AddToDeck(cardData.ID)) return;

        AppDirector.I.Sound?.PlaySfx(SfxKey.CardSwap);
        _collectionCards.Remove(droppedCard);
        Destroy(droppedCard.gameObject);

        SpawnDeckSlotCard(targetSlot, cardData);
        RefreshDeckStatus();
    }

    // Card.OnDrop - card dropped on another card
    private void HandleCardDroppedOnCard(Card targetCard, Card droppedCard)
    {
        var droppedData = droppedCard.GetCardData();
        var targetData = targetCard.GetCardData();
        if (droppedData == null || targetData == null) return;

        var droppedSlot = FindSlotForCard(droppedCard);
        var targetSlot = FindSlotForCard(targetCard);

        if (droppedSlot != null && targetSlot != null)
        {
            SwapDeckSlots(droppedSlot, targetSlot);
        }
        else if (droppedSlot == null && targetSlot != null)
        {
            SwapCollectionWithSlot(droppedCard, targetSlot);
        }
        else if (droppedSlot != null && targetSlot == null)
        {
            SwapCollectionWithSlot(targetCard, droppedSlot);
        }
    }

    private void SwapDeckSlots(DeckSlot slotA, DeckSlot slotB)
    {
        var cardA = slotA.OccupyingCard;
        var cardB = slotB.OccupyingCard;
        if (cardA == null || cardB == null) return;

        var dataA = cardA.GetCardData();
        var dataB = cardB.GetCardData();
        if (dataA == null || dataB == null) return;

        DeckManager.SwapInDeck(dataA.ID, dataB.ID);
        AppDirector.I.Sound?.PlaySfx(SfxKey.CardSwap);

        ClearSlotCard(slotA);
        ClearSlotCard(slotB);
        SpawnDeckSlotCard(slotA, dataB);
        SpawnDeckSlotCard(slotB, dataA);
    }

    private void SwapCollectionWithSlot(Card collectionCard, DeckSlot slot)
    {
        var collectionData = collectionCard.GetCardData();
        var slotCard = slot.OccupyingCard;
        if (collectionData == null || slotCard == null) return;

        var slotData = slotCard.GetCardData();
        if (slotData == null) return;

        DeckManager.ReplaceInDeck(slotData.ID, collectionData.ID);
        AppDirector.I.Sound?.PlaySfx(SfxKey.CardSwap);

        _collectionCards.Remove(collectionCard);
        Destroy(collectionCard.gameObject);

        ClearSlotCard(slot);
        SpawnDeckSlotCard(slot, collectionData);
        SpawnCollectionCard(slotData);

        RefreshDeckStatus();
    }

    private DeckSlot FindSlotForCard(Card card)
    {
        foreach (var slot in _deckSlots)
            if (slot.OccupyingCard == card) return slot;
        return null;
    }

    private void ClearSlotCard(DeckSlot slot)
    {
        if (slot.OccupyingCard != null)
        {
            Destroy(slot.OccupyingCard.gameObject);
            slot.ClearCard();
        }
    }

    private void RefreshDeckStatus()
    {
        if (deckStatusText != null)
            deckStatusText.text = $"{DeckManager.GetDeck().Count}/{DeckManager.MaxDeckSize}";
    }
}
