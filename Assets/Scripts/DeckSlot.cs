using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeckSlot : MonoBehaviour, IDropHandler
{
    public int SlotIndex { get; private set; }
    public Card OccupyingCard { get; private set; }

    public readonly Subject<(DeckSlot slot, Card card)> OnCardDropped = new();

    void Awake()
    {
        // IDropHandler requires a raycast-target Graphic to detect the pointer on drop.
        // Add a transparent Image automatically if none exists on this slot.
        if (GetComponent<UnityEngine.UI.Image>() == null)
        {
            var img = gameObject.AddComponent<UnityEngine.UI.Image>();
            img.color = Color.clear;
        }
    }

    public void Initialize(int index)
    {
        SlotIndex = index;
    }

    public void SetCard(Card card)
    {
        OccupyingCard = card;
    }

    public void ClearCard()
    {
        OccupyingCard = null;
    }

    public bool IsOccupied => OccupyingCard != null;

    public void OnDrop(PointerEventData eventData)
    {
        var card = eventData.pointerDrag?.GetComponent<Card>();
        if (card == null) return;
        OnCardDropped.OnNext((this, card));
    }

    void OnDestroy()
    {
        OnCardDropped.Dispose();
    }
}
