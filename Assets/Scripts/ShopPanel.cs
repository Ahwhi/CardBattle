using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

public class ShopPanel : MonoBehaviour
{
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Transform cardContainer;
    [SerializeField] Button closePanelButton;
    [SerializeField] Button buyOneButton;
    [SerializeField] Button buyTenButton;
    [SerializeField] TextMeshProUGUI messageText;

    private const int PriceOne = 100;
    private const int PriceTen = 900;
    private TitleSceneDirector _titleDirector;
    private readonly List<GameObject> _spawnedCards = new();

    void Awake()
    {
        _titleDirector = FindAnyObjectByType<TitleSceneDirector>();
    }

    void Start()
    {
        closePanelButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); Close(); })
            .AddTo(this);

        buyOneButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); BuyCards(1, PriceOne); })
            .AddTo(this);

        buyTenButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); BuyCards(10, PriceTen); })
            .AddTo(this);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        ClearSpawnedCards();
        if (messageText != null) messageText.text = "";
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void BuyCards(int count, int totalPrice)
    {
        if (DataManager.Data.Gold < totalPrice)
        {
            if (messageText != null) messageText.text = $"골드가 부족합니다. ({totalPrice}골드 필요)";
            return;
        }

        DataManager.Data.Gold -= totalPrice;
        DataManager.RequestSave();
        _titleDirector?.RefreshGoldUI();

        AppDirector.I.Sound?.PlaySfx(SfxKey.Purchase);

        if (messageText != null) messageText.text = "";
        ClearSpawnedCards();

        var allCards = AppDirector.I.CardDB.GetAll();
        for (int i = 0; i < count; i++)
        {
            int randIndex = UnityEngine.Random.Range(0, allCards.Count);
            var cardData = allCards[randIndex];

            CardManager.Add(cardData.ID);

            var go = Instantiate(cardPrefab, cardContainer);
            go.transform.localScale = Vector3.one * (count > 1 ? 0.5f : 1f);
            _spawnedCards.Add(go);
            var card = go.GetComponent<Card>();
            card?.SetupForShop(cardData);
        }
    }

    private void ClearSpawnedCards()
    {
        foreach (var go in _spawnedCards)
            if (go != null) Destroy(go);
        _spawnedCards.Clear();
    }
}
