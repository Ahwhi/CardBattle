using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using static Enums;

public class BattleDirector : MonoBehaviour
{
    [SerializeField] BattleField battleField;
    [SerializeField] TurnManager turnManager;
    [SerializeField] BattleUI battleUI;

    private List<RuntimeCard> _playerCards = new();
    private List<RuntimeCard> _enemyCards = new();
    private bool _battleEnded;

    void Start()
    {
        AppDirector.I.Sound?.PlayBgm(BgmKey.Battle);
        var difficulty = AppDirector.I.SelectedDifficulty;
        BuildPlayerCards();
        BuildEnemyCards(difficulty);
        battleField.Initialize(_playerCards, _enemyCards);

        var ai = new EnemyAI(difficulty);
        turnManager.Initialize(this, battleField, battleUI, ai);

        SubscribeCardClicks();
        turnManager.StartBattle();
    }

    private void BuildPlayerCards()
    {
        var deck = DeckManager.GetDeck();
        foreach (int cardID in deck)
        {
            var cardData = AppDirector.I.CardDB.Get(cardID);
            var cardInfo = CardManager.Get(cardID);
            int starLevel = cardInfo?.StarLevel ?? 1;
            _playerCards.Add(new RuntimeCard(cardData, starLevel, true));
        }
    }

    private void BuildEnemyCards(Difficulty difficulty)
    {
        var allCards = AppDirector.I.CardDB.GetAll().ToList();
        var healers = allCards.Where(c => c.Type == CardType.Healer).ToList();
        var nonHealers = allCards.Where(c => c.Type != CardType.Healer).ToList();

        var picked = nonHealers.OrderBy(_ => UnityEngine.Random.value).ToList();
        if (healers.Count > 0)
            picked.Insert(0, healers[UnityEngine.Random.Range(0, healers.Count)]);

        var shuffled = picked.Take(6).OrderBy(_ => UnityEngine.Random.value).ToList();

        int starLevel = difficulty switch
        {
            Difficulty.Easy => 1,
            Difficulty.Normal => 2,
            Difficulty.Hard => 3,
            _ => 1
        };

        foreach (var cardData in shuffled)
            _enemyCards.Add(new RuntimeCard(cardData, starLevel, false));
    }

    private void SubscribeCardClicks()
    {
        battleField.OnPlayerFieldCardClicked
            .Subscribe(card => turnManager.HandlePlayerCardClicked(card))
            .AddTo(this);

        battleField.OnPlayerFieldCardClicked
            .Subscribe(card => turnManager.HandleAllyCardClickedForSkill(card))
            .AddTo(this);

        battleField.OnEnemyFieldCardClicked
            .Subscribe(card => turnManager.HandleEnemyCardClicked(card))
            .AddTo(this);
    }

    public bool CheckBattleEnd()
    {
        if (_battleEnded) return true;

        if (battleField.IsAllEnemyCardsDead())
        {
            EndBattle(true);
            return true;
        }

        if (battleField.IsAllPlayerCardsDead())
        {
            EndBattle(false);
            return true;
        }

        return false;
    }

    private void EndBattle(bool playerWon)
    {
        _battleEnded = true;
        AppDirector.I.Sound?.PlaySfx(playerWon ? SfxKey.Victory : SfxKey.Defeat);
        battleUI.ShowResultPanel(playerWon);
    }
}
