using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

public class CollectionPanel : MonoBehaviour
{
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Transform cardGrid;
    [SerializeField] Button closeButton;

    private readonly List<GameObject> _cardObjects = new();

    void Start()
    {
        closeButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); Close(); })
            .AddTo(this);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        RefreshCards();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void RefreshCards()
    {
        foreach (var go in _cardObjects)
            if (go != null) Destroy(go);
        _cardObjects.Clear();

        var allCards = AppDirector.I.CardDB.GetAll();
        foreach (var cardData in allCards)
        {
            var go = Instantiate(cardPrefab, cardGrid);
            _cardObjects.Add(go);

            var cardInfo = CardManager.Get(cardData.ID);
            int starLevel = cardInfo?.StarLevel ?? 1;
            int quantity = cardInfo?.Quantity ?? 0;
            int totalForUpgrade = 10;

            go.GetComponent<Card>()?.SetupForCollection(cardData, starLevel, quantity, totalForUpgrade);
        }
    }
}
