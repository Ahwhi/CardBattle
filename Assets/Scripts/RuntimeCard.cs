using System;
using System.Collections.Generic;
using UniRx;
using static Enums;

[Serializable]
public class StatusEffectInstance
{
    public StatusEffectType Type;
    public int RemainingTurns;

    public StatusEffectInstance(StatusEffectType type, int turns)
    {
        Type = type;
        RemainingTurns = turns;
    }
}

public class RuntimeCard
{
    public CardData Data { get; private set; }
    public int StarLevel { get; private set; }
    public int MaxHP { get; private set; }
    public ReactiveProperty<int> CurrentHP { get; private set; }
    public bool IsPlayer { get; private set; }
    public int FieldIndex { get; set; }

    public bool IsAlive => CurrentHP.Value > 0;

    public bool IsStunned => _effects.Exists(e =>
        e.Type == StatusEffectType.Stun && e.RemainingTurns > 0);

    public bool IsSkillSealed => _effects.Exists(e =>
        e.Type == StatusEffectType.SealSkill && e.RemainingTurns > 0);

    public bool HasBleed => _effects.Exists(e =>
        e.Type == StatusEffectType.Bleed && e.RemainingTurns > 0);

    public IReadOnlyList<StatusEffectInstance> ActiveEffects => _effects;

    private List<StatusEffectInstance> _effects = new();

    public RuntimeCard(CardData data, int starLevel, bool isPlayer)
    {
        Data = data;
        StarLevel = Math.Max(1, starLevel);
        MaxHP = data.BaseHP;
        CurrentHP = new ReactiveProperty<int>(MaxHP);
        IsPlayer = isPlayer;
        FieldIndex = -1;
    }

    public int TakeDamage(int amount)
    {
        int actual = Math.Max(0, amount);
        CurrentHP.Value = Math.Max(0, CurrentHP.Value - actual);
        return actual;
    }

    public int Heal(int amount)
    {
        int actual = Math.Max(0, amount);
        CurrentHP.Value = Math.Min(MaxHP, CurrentHP.Value + actual);
        return actual;
    }

    public void AddStatusEffect(StatusEffectType type, int turns)
    {
        var existing = _effects.Find(e => e.Type == type);
        if (existing != null)
            existing.RemainingTurns = Math.Max(existing.RemainingTurns, turns);
        else
            _effects.Add(new StatusEffectInstance(type, turns));
    }

    public void RemoveAllStatusEffects()
    {
        _effects.Clear();
    }

    public void TickStatusEffects(Action<RuntimeCard, StatusEffectType> onEffectTick)
    {
        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            var effect = _effects[i];
            onEffectTick?.Invoke(this, effect.Type);
            effect.RemainingTurns--;
            if (effect.RemainingTurns <= 0)
                _effects.RemoveAt(i);
        }
    }
}
