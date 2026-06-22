using Cysharp.Threading.Tasks;
using System.Linq;
using UniRx;
using UnityEngine;
using static Enums;

public class TurnManager : MonoBehaviour
{
    private const int MaxSkillCount = 5;

    private BattleDirector _director;
    private BattleField _field;
    private BattleUI _ui;
    private EnemyAI _ai;

    private ReactiveProperty<BattlePhase> _phase = new(BattlePhase.SelectCard);
    private ReactiveProperty<bool> _isPlayerTurn = new(true);

    private RuntimeCard _selectedPlayerCard;
    private BattleAction _selectedAction;

    private int _playerSkillCount = MaxSkillCount;
    private int _enemySkillCount = MaxSkillCount;

    public IReadOnlyReactiveProperty<BattlePhase> Phase => _phase;
    public IReadOnlyReactiveProperty<bool> IsPlayerTurn => _isPlayerTurn;

    public void Initialize(BattleDirector director, BattleField field, BattleUI ui, EnemyAI ai)
    {
        _director = director;
        _field = field;
        _ui = ui;
        _ai = ai;

        _ui.AttackButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); SelectAction(BattleAction.Attack); })
            .AddTo(this);

        _ui.SkillButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); SelectAction(BattleAction.Skill); })
            .AddTo(this);

        _ui.BackToTitleButton?.OnClickAsObservable()
            .Subscribe(_ =>
            {
                AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick);
                UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
            })
            .AddTo(this);

        _ui.InitializeSkillCount(MaxSkillCount, true);
        _ui.InitializeSkillCount(MaxSkillCount, false);
    }

    public void StartBattle()
    {
        _isPlayerTurn.Value = true;
        StartPlayerTurn().Forget();
    }

    private async UniTask StartPlayerTurn()
    {
        _ui.SetTurnText(true);

        await ProcessTurnStartEffects(true);

        if (_director.CheckBattleEnd()) return;

        EnterSelectCardPhase();
    }

    private void EnterSelectCardPhase()
    {
        _phase.Value = BattlePhase.SelectCard;
        _ui.SetPhaseGuide(BattlePhase.SelectCard);
        _selectedPlayerCard = null;
        _ui.ShowSelectedCardInfo(null);
        _ui.SetActionButtonsActive(false);
    }

    private async UniTask ProcessTurnStartEffects(bool isPlayer)
    {
        var field = isPlayer ? _field.PlayerField : _field.EnemyField;
        var alliedField = field.ToList();

        foreach (var healer in alliedField.Where(c => c.IsAlive && c.Data.Type == CardType.Healer).ToList())
        {
            var target = alliedField
                .Where(c => c.IsAlive && c.CurrentHP.Value < c.MaxHP)
                .OrderBy(c => c.CurrentHP.Value)
                .FirstOrDefault();

            if (target != null)
            {
                int healed = target.Heal(healer.StarLevel);
                if (healed > 0)
                {
                    _field.SpawnDamagePopup(target, healed, true);
                    _field.GetCardView(target)?.RefreshBattleUI();
                    AppDirector.I.Sound?.PlaySfx(SfxKey.Heal);
                }
            }
            await UniTask.Delay(300);
        }

        foreach (var card in alliedField.ToList())
        {
            card.TickStatusEffects((c, effectType) =>
            {
                if (effectType == StatusEffectType.Bleed)
                {
                    int dmg = c.TakeDamage(1);
                    _field.SpawnDamagePopup(c, dmg, false);
                }
                _field.GetCardView(c)?.RefreshBattleUI();
            });
        }

        if (alliedField.Any(c => c.HasBleed))
            await UniTask.Delay(400);
    }

    public void HandlePlayerCardClicked(RuntimeCard card)
    {
        if (_phase.Value != BattlePhase.SelectCard && _phase.Value != BattlePhase.SelectAction) return;
        if (!card.IsAlive || card.IsStunned) return;

        AppDirector.I.Sound?.PlaySfx(SfxKey.CardSelect);

        if (_selectedPlayerCard != null)
            _field.GetCardView(_selectedPlayerCard)?.SetSelected(false);

        _selectedPlayerCard = card;
        _field.GetCardView(card)?.SetSelected(true);
        _ui.ShowSelectedCardInfo(card);
        _ui.SetActionButtonsActive(true);

        bool canUseSkill = !card.IsSkillSealed && _playerSkillCount > 0;
        _ui.SetSkillButtonInteractable(canUseSkill);

        _phase.Value = BattlePhase.SelectAction;
        _ui.SetPhaseGuide(BattlePhase.SelectAction);
    }

    public void HandleEnemyCardClicked(RuntimeCard card)
    {
        if (_phase.Value != BattlePhase.SelectTarget) return;
        if (!card.IsAlive) return;

        if (_selectedAction == BattleAction.Skill &&
            _selectedPlayerCard?.Data.Skill == SpecialSkill.Medic) return;

        AppDirector.I.Sound?.PlaySfx(SfxKey.CardSelect);
        ExecutePlayerAction(card).Forget();
    }

    public void HandleAllyCardClickedForSkill(RuntimeCard card)
    {
        if (_phase.Value != BattlePhase.SelectTarget) return;
        if (!card.IsAlive) return;
        if (_selectedAction != BattleAction.Skill) return;
        if (_selectedPlayerCard?.Data.Skill != SpecialSkill.Medic) return;

        AppDirector.I.Sound?.PlaySfx(SfxKey.CardSelect);
        ExecutePlayerSkillOnAlly(card).Forget();
    }

    private void SelectAction(BattleAction action)
    {
        if (_selectedPlayerCard == null) return;
        if (_phase.Value != BattlePhase.SelectAction) return;

        _selectedAction = action;

        var skill = _selectedPlayerCard.Data.Skill;
        if (action == BattleAction.Skill &&
            (skill == SpecialSkill.Gunner || skill == SpecialSkill.Nurse))
        {
            _ui.SetActionButtonsActive(false);
            ExecuteNoTargetSkill(_selectedPlayerCard).Forget();
            return;
        }

        _phase.Value = BattlePhase.SelectTarget;
        _ui.SetPhaseGuide(BattlePhase.SelectTarget);
        _ui.SetActionButtonsActive(false);
    }

    private void ConsumeSkill(bool isPlayer)
    {
        if (isPlayer)
            _playerSkillCount--;
        else
            _enemySkillCount--;

        _ui.ConsumeSkillCount(isPlayer);
    }

    private async UniTask ExecutePlayerAction(RuntimeCard target)
    {
        _phase.Value = BattlePhase.Executing;

        var attacker = _selectedPlayerCard;
        _field.GetCardView(attacker)?.SetSelected(false);

        if (_selectedAction == BattleAction.Attack)
            await ExecuteAttack(attacker, target, true);
        else
        {
            ConsumeSkill(true);
            await ExecuteSkill(attacker, target, null, true);
        }

        _selectedPlayerCard = null;
        if (_director.CheckBattleEnd()) return;

        await UniTask.Delay(400);
        _isPlayerTurn.Value = false;
        StartEnemyTurn().Forget();
    }

    private async UniTask ExecutePlayerSkillOnAlly(RuntimeCard allyTarget)
    {
        _phase.Value = BattlePhase.Executing;

        var attacker = _selectedPlayerCard;
        _field.GetCardView(attacker)?.SetSelected(false);

        ConsumeSkill(true);
        await ExecuteSkill(attacker, null, allyTarget, true);

        _selectedPlayerCard = null;
        if (_director.CheckBattleEnd()) return;

        await UniTask.Delay(400);
        _isPlayerTurn.Value = false;
        StartEnemyTurn().Forget();
    }

    private async UniTask ExecuteNoTargetSkill(RuntimeCard attacker)
    {
        _phase.Value = BattlePhase.Executing;

        _field.GetCardView(attacker)?.SetSelected(false);

        ConsumeSkill(true);
        await ExecuteSkill(attacker, null, null, true);

        _selectedPlayerCard = null;
        if (_director.CheckBattleEnd()) return;

        await UniTask.Delay(400);
        _isPlayerTurn.Value = false;
        StartEnemyTurn().Forget();
    }

    private async UniTask StartEnemyTurn()
    {
        _phase.Value = BattlePhase.EnemyTurn;
        _ui.SetTurnText(false);
        _ui.SetPhaseGuide(BattlePhase.EnemyTurn);

        await ProcessTurnStartEffects(false);
        if (_director.CheckBattleEnd()) return;

        await UniTask.Delay(600);

        var attacker = _ai.SelectAttacker(_field.EnemyField);
        if (attacker != null && _field.PlayerField.Count > 0)
        {
            var (action, target, allyTarget) = _ai.SelectAction(
                attacker, _field.PlayerField, _field.EnemyField, _enemySkillCount);
            await UniTask.Delay(350);

            if (action == BattleAction.Skill)
            {
                ConsumeSkill(false);
                await ExecuteSkill(attacker, target, allyTarget, false);
            }
            else if (target != null)
            {
                await ExecuteAttack(attacker, target, false);
            }

            if (_director.CheckBattleEnd()) return;
            await UniTask.Delay(250);
        }

        _isPlayerTurn.Value = true;
        await UniTask.Delay(400);
        StartPlayerTurn().Forget();
    }

    private async UniTask ExecuteAttack(RuntimeCard attacker, RuntimeCard defender, bool isPlayerAttacking, bool isSkill = false)
    {
        var attackerView = _field.GetCardView(attacker);
        var defenderView = _field.GetCardView(defender);

        bool isMelee = attacker.Data.Type != CardType.Ranged;

        if (isMelee && attackerView != null && defenderView != null)
            await attackerView.PlayMeleeAttack(defenderView.transform);
        else if (attackerView != null)
            await attackerView.PlayRangedAttack(isPlayerAttacking);

        AppDirector.I.Sound?.PlaySfx(SfxKey.Attack);

        int damage = attacker.StarLevel;
        ApplyDamage(attacker, defender, damage, isPlayerAttacking, isSkill);

        AppDirector.I.Sound?.PlaySfx(SfxKey.Hit);
        await (defenderView?.PlayHitEffect() ?? UniTask.CompletedTask);

        if (attacker.Data.Type == CardType.Peerless)
        {
            var adjacent = _field.GetAdjacentEnemy(defender, isPlayerAttacking);
            if (adjacent != null && adjacent.IsAlive)
            {
                await UniTask.Delay(200);
                ApplyDamage(attacker, adjacent, damage, isPlayerAttacking, isSkill);
                AppDirector.I.Sound?.PlaySfx(SfxKey.Hit);
                await (_field.GetCardView(adjacent)?.PlayHitEffect() ?? UniTask.CompletedTask);
            }
        }

        bool noCounter = isSkill && attacker.Data.Skill == SpecialSkill.Warrior;
        if (isMelee && !noCounter && defender.IsAlive)
        {
            await UniTask.Delay(150);
            int counterDmg = attacker.TakeDamage(defender.StarLevel);
            _field.SpawnDamagePopup(attacker, counterDmg, false);
            attackerView?.RefreshBattleUI();
            AppDirector.I.Sound?.PlaySfx(SfxKey.Hit);
            await (attackerView?.PlayHitEffect() ?? UniTask.CompletedTask);
        }

        await _field.FillEmptySlots(!isPlayerAttacking);
        await _field.FillEmptySlots(isPlayerAttacking);
    }

    private void ApplyDamage(RuntimeCard attacker, RuntimeCard defender, int damage, bool isPlayerAttacking, bool isSkill = false)
    {
        if (!defender.IsAlive) return;

        int applied = defender.TakeDamage(damage);
        _field.SpawnDamagePopup(defender, applied, false);
        _field.GetCardView(defender)?.RefreshBattleUI();

        if (!isSkill) return;

        if (attacker.Data.Skill == SpecialSkill.Archer && defender.CurrentHP.Value <= 2 && defender.IsAlive)
        {
            defender.TakeDamage(defender.CurrentHP.Value);
            _field.SpawnTextPopup(defender, "즉사", Color.yellow);
            _field.GetCardView(defender)?.RefreshBattleUI();
        }

        if (attacker.Data.Skill == SpecialSkill.Wrestler && defender.IsAlive)
            defender.AddStatusEffect(StatusEffectType.Stun, 1);

        if (attacker.Data.Skill == SpecialSkill.SWAT && defender.IsAlive)
            defender.AddStatusEffect(StatusEffectType.Bleed, 2);

        if (attacker.Data.Skill == SpecialSkill.Cavalry && defender.IsAlive)
            defender.AddStatusEffect(StatusEffectType.SealSkill, 2);

        _field.GetCardView(defender)?.RefreshBattleUI();
    }

    private async UniTask ExecuteSkill(RuntimeCard attacker, RuntimeCard target, RuntimeCard allyTarget, bool isPlayerAttacking)
    {
        switch (attacker.Data.Skill)
        {
            case SpecialSkill.Gunner:
                await ExecuteGunnerSkill(attacker, isPlayerAttacking);
                break;

            case SpecialSkill.Medic:
                if (allyTarget != null)
                {
                    AppDirector.I.Sound?.PlaySfx(SfxKey.Heal);
                    int healed = allyTarget.Heal(2);
                    _field.SpawnDamagePopup(allyTarget, healed, true);
                    allyTarget.RemoveAllStatusEffects();
                    _field.GetCardView(allyTarget)?.RefreshBattleUI();
                }
                break;

            case SpecialSkill.Nurse:
                AppDirector.I.Sound?.PlaySfx(SfxKey.Heal);
                var nurseAllies = isPlayerAttacking ? _field.PlayerField : _field.EnemyField;
                foreach (var ally in nurseAllies)
                {
                    int healed = ally.Heal(1);
                    _field.SpawnDamagePopup(ally, healed, true);
                    _field.GetCardView(ally)?.RefreshBattleUI();
                }
                await UniTask.Delay(300);
                break;

            default:
                if (target != null)
                    await ExecuteAttack(attacker, target, isPlayerAttacking, isSkill: true);
                break;
        }
    }

    private async UniTask ExecuteGunnerSkill(RuntimeCard attacker, bool isPlayerAttacking)
    {
        var targets = isPlayerAttacking ? _field.EnemyField.ToList() : _field.PlayerField.ToList();
        var attackerView = _field.GetCardView(attacker);

        if (attackerView != null)
            await attackerView.PlayRangedAttack(isPlayerAttacking);

        AppDirector.I.Sound?.PlaySfx(SfxKey.Attack);

        foreach (var target in targets)
        {
            if (!target.IsAlive) continue;
            int dmg = target.TakeDamage(attacker.StarLevel);
            _field.SpawnDamagePopup(target, dmg, false);
            _field.GetCardView(target)?.RefreshBattleUI();
        }

        await UniTask.Delay(200);

        foreach (var target in targets)
        {
            AppDirector.I.Sound?.PlaySfx(SfxKey.Hit);
            await (_field.GetCardView(target)?.PlayHitEffect() ?? UniTask.CompletedTask);
        }

        await _field.FillEmptySlots(!isPlayerAttacking);
    }

    void OnDestroy()
    {
        _phase.Dispose();
        _isPlayerTurn.Dispose();
    }
}
