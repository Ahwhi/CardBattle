using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Enums;

public class EnemyAI
{
    private Difficulty _difficulty;

    public EnemyAI(Difficulty difficulty)
    {
        _difficulty = difficulty;
    }

    public RuntimeCard SelectAttacker(IReadOnlyList<RuntimeCard> enemyField)
    {
        var available = enemyField.Where(c => c.IsAlive && !c.IsStunned).ToList();
        if (available.Count == 0) return null;

        return _difficulty switch
        {
            Difficulty.Easy => available[Random.Range(0, available.Count)],
            _ => available.OrderByDescending(c => c.StarLevel).First()
        };
    }

    public (BattleAction action, RuntimeCard target, RuntimeCard allyTarget) SelectAction(
        RuntimeCard attacker,
        IReadOnlyList<RuntimeCard> playerField,
        IReadOnlyList<RuntimeCard> enemyField,
        int remainingSkillCount)
    {
        var validTargets = playerField.Where(c => c.IsAlive).ToList();
        bool useSkill = remainingSkillCount > 0 && ShouldUseSkill(attacker);

        if (useSkill)
        {
            var skill = attacker.Data.Skill;

            if (skill == SpecialSkill.Medic)
            {
                var healTarget = enemyField
                    .Where(c => c.IsAlive && c.CurrentHP.Value < c.MaxHP)
                    .OrderBy(c => c.CurrentHP.Value)
                    .FirstOrDefault();

                if (healTarget != null)
                    return (BattleAction.Skill, null, healTarget);
            }
            else if (skill == SpecialSkill.Gunner || skill == SpecialSkill.Nurse)
            {
                return (BattleAction.Skill, null, null);
            }
            else if (validTargets.Count > 0)
            {
                var skillTarget = SelectAttackTarget(validTargets);
                return (BattleAction.Skill, skillTarget, null);
            }
        }

        if (validTargets.Count == 0)
            return (BattleAction.Attack, null, null);

        var attackTarget = SelectAttackTarget(validTargets);
        return (BattleAction.Attack, attackTarget, null);
    }

    private bool ShouldUseSkill(RuntimeCard card)
    {
        if (card.IsSkillSealed) return false;

        return _difficulty switch
        {
            Difficulty.Easy => Random.value > 0.7f,
            Difficulty.Normal => Random.value > 0.4f,
            Difficulty.Hard => true,
            _ => false
        };
    }

    private RuntimeCard SelectAttackTarget(List<RuntimeCard> targets)
    {
        return _difficulty switch
        {
            Difficulty.Easy => targets[Random.Range(0, targets.Count)],
            _ => targets.OrderBy(c => c.CurrentHP.Value).First()
        };
    }
}
