using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using static Enums;

public class BattleField : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject cardPrefab;
    [SerializeField] GameObject damagePopupPrefab;

    [Header("Player Slots")]
    [SerializeField] Transform[] playerFieldSlots;
    [SerializeField] Transform[] playerReserveSlots;

    [Header("Enemy Slots")]
    [SerializeField] Transform[] enemyFieldSlots;
    [SerializeField] Transform[] enemyReserveSlots;

    private List<RuntimeCard> _playerField = new();
    private List<RuntimeCard> _playerReserve = new();
    private List<RuntimeCard> _enemyField = new();
    private List<RuntimeCard> _enemyReserve = new();

    private Dictionary<RuntimeCard, Card> _cardViews = new();

    public readonly Subject<RuntimeCard> OnPlayerCardDied = new();
    public readonly Subject<RuntimeCard> OnEnemyCardDied = new();
    public readonly Subject<RuntimeCard> OnPlayerFieldCardClicked = new();
    public readonly Subject<RuntimeCard> OnEnemyFieldCardClicked = new();

    public IReadOnlyList<RuntimeCard> PlayerField => _playerField;
    public IReadOnlyList<RuntimeCard> PlayerReserve => _playerReserve;
    public IReadOnlyList<RuntimeCard> EnemyField => _enemyField;
    public IReadOnlyList<RuntimeCard> EnemyReserve => _enemyReserve;

    public void Initialize(List<RuntimeCard> playerCards, List<RuntimeCard> enemyCards)
    {
        for (int i = 0; i < playerCards.Count; i++)
        {
            if (i < 3) PlaceCard(playerCards[i], true, i, false);
            else _playerReserve.Add(playerCards[i]);
        }

        for (int i = 3; i < Mathf.Min(playerCards.Count, playerReserveSlots.Length + 3); i++)
            SpawnReserveCard(playerCards[i], playerReserveSlots[i - 3]);

        for (int i = 0; i < enemyCards.Count; i++)
        {
            if (i < 3) PlaceCard(enemyCards[i], false, i, false);
            else _enemyReserve.Add(enemyCards[i]);
        }

        for (int i = 3; i < Mathf.Min(enemyCards.Count, enemyReserveSlots.Length + 3); i++)
            SpawnReserveCard(enemyCards[i], enemyReserveSlots[i - 3]);
    }

    private void PlaceCard(RuntimeCard runtimeCard, bool isPlayer, int slotIndex, bool animate)
    {
        var slots = isPlayer ? playerFieldSlots : enemyFieldSlots;
        var field = isPlayer ? _playerField : _enemyField;

        if (slotIndex >= slots.Length) return;

        runtimeCard.FieldIndex = slotIndex;

        var go = Instantiate(cardPrefab, slots[slotIndex]);
        var card = go.GetComponent<Card>();
        card?.SetupForBattle(runtimeCard);

        _cardViews[runtimeCard] = card;

        if (isPlayer) _playerField.Add(runtimeCard);
        else _enemyField.Add(runtimeCard);

        if (card != null)
        {
            var captured = runtimeCard;
            card.OnCardClicked
                .Subscribe(_ =>
                {
                    if (isPlayer) OnPlayerFieldCardClicked.OnNext(captured);
                    else OnEnemyFieldCardClicked.OnNext(captured);
                })
                .AddTo(card);
        }

        runtimeCard.CurrentHP
            .Where(hp => hp <= 0)
            .Take(1)
            .Subscribe(_ => HandleCardDeath(runtimeCard, isPlayer).Forget())
            .AddTo(card);
    }

    private void SpawnReserveCard(RuntimeCard runtimeCard, Transform slot)
    {
        if (slot == null) return;
        var go = Instantiate(cardPrefab, slot);
        var card = go.GetComponent<Card>();
        card?.SetupForBattleReserve(runtimeCard.Data);
        _cardViews[runtimeCard] = card;
    }

    private async UniTask HandleCardDeath(RuntimeCard dead, bool isPlayer)
    {
        AppDirector.I.Sound?.PlaySfx(SfxKey.CardDeath);
        var view = GetCardView(dead);
        if (view != null) await view.PlayDeathAnimation();

        if (isPlayer)
        {
            _playerField.Remove(dead);
            OnPlayerCardDied.OnNext(dead);
        }
        else
        {
            _enemyField.Remove(dead);
            OnEnemyCardDied.OnNext(dead);
        }

        _cardViews.Remove(dead);
        await FillEmptySlots(isPlayer);
    }

    public async UniTask FillEmptySlots(bool isPlayer)
    {
        var field = isPlayer ? _playerField : _enemyField;
        var reserve = isPlayer ? _playerReserve : _enemyReserve;
        var slots = isPlayer ? playerFieldSlots : enemyFieldSlots;
        var reserveSlots = isPlayer ? playerReserveSlots : enemyReserveSlots;

        while (field.Count < 3 && reserve.Count > 0)
        {
            var promoted = reserve[0];
            reserve.RemoveAt(0);

            int slotIndex = FindEmptyFieldSlot(field);
            if (slotIndex < 0 || slotIndex >= slots.Length) break;

            if (_cardViews.TryGetValue(promoted, out var oldView))
            {
                Destroy(oldView.gameObject);
                _cardViews.Remove(promoted);
            }

            await UniTask.Delay(200);
            PlaceCard(promoted, isPlayer, slotIndex, true);

            RearrangeReserveViews(reserve, reserveSlots, isPlayer);
        }
    }

    private int FindEmptyFieldSlot(List<RuntimeCard> field)
    {
        var occupied = new HashSet<int>(field.Select(c => c.FieldIndex));
        for (int i = 0; i < 3; i++)
            if (!occupied.Contains(i)) return i;
        return -1;
    }

    private void RearrangeReserveViews(List<RuntimeCard> reserve, Transform[] reserveSlots, bool isPlayer)
    {
        for (int i = 0; i < reserve.Count && i < reserveSlots.Length; i++)
        {
            if (_cardViews.TryGetValue(reserve[i], out var view) && view != null)
                view.transform.SetParent(reserveSlots[i], false);
        }
    }

    public Card GetCardView(RuntimeCard runtimeCard)
    {
        _cardViews.TryGetValue(runtimeCard, out var card);
        return card;
    }

    public RuntimeCard GetAdjacentEnemy(RuntimeCard target, bool isPlayerAttacking)
    {
        var field = isPlayerAttacking ? _enemyField : _playerField;
        int targetIndex = target.FieldIndex;

        var adjacents = field.Where(c => c != target &&
            Mathf.Abs(c.FieldIndex - targetIndex) == 1 && c.IsAlive).ToList();

        if (adjacents.Count == 0) return null;
        return adjacents[UnityEngine.Random.Range(0, adjacents.Count)];
    }

    public void SpawnDamagePopup(RuntimeCard target, int amount, bool isHeal)
    {
        if (damagePopupPrefab == null) return;
        if (!_cardViews.TryGetValue(target, out var cardView) || cardView == null) return;

        var cardRect = cardView.GetComponent<RectTransform>();
        var go = Instantiate(damagePopupPrefab, cardView.transform.parent);
        var popupRect = go.GetComponent<RectTransform>();
        if (popupRect != null && cardRect != null)
        {
            popupRect.anchoredPosition = cardRect.anchoredPosition + Vector2.up * 40f;
        }
        else
        {
            go.transform.position = cardView.transform.position;
        }
        go.GetComponent<DamagePopup>()?.Setup(amount, isHeal);
    }

    public void SpawnTextPopup(RuntimeCard target, string text, Color color)
    {
        if (damagePopupPrefab == null) return;
        if (!_cardViews.TryGetValue(target, out var cardView) || cardView == null) return;

        var cardRect = cardView.GetComponent<RectTransform>();
        var go = Instantiate(damagePopupPrefab, cardView.transform.parent);
        var popupRect = go.GetComponent<RectTransform>();
        if (popupRect != null && cardRect != null)
            popupRect.anchoredPosition = cardRect.anchoredPosition + Vector2.up * 40f;
        else
            go.transform.position = cardView.transform.position;

        go.GetComponent<DamagePopup>()?.Setup(text, color);
    }

    public bool IsAllPlayerCardsDead() => _playerField.Count == 0 && _playerReserve.Count == 0;
    public bool IsAllEnemyCardsDead() => _enemyField.Count == 0 && _enemyReserve.Count == 0;

    void OnDestroy()
    {
        OnPlayerCardDied.Dispose();
        OnEnemyCardDied.Dispose();
        OnPlayerFieldCardClicked.Dispose();
        OnEnemyFieldCardClicked.Dispose();
    }
}
