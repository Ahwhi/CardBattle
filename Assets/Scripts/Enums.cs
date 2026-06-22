public class Enums
{
    public enum CardType
    {
        Normal,
        Ranged,
        Peerless,
        Healer
    }

    public enum SpecialSkill
    {
        Warrior,
        Wrestler,
        Archer,
        Gunner,
        SWAT,
        Cavalry,
        Medic,
        Nurse
    }

    public enum StatusEffectType
    {
        Stun,
        Bleed,
        SealSkill
    }

    public enum BattlePhase
    {
        TurnStart,
        SelectCard,
        SelectAction,
        SelectTarget,
        Executing,
        EnemyTurn,
        GameOver
    }

    public enum BattleAction
    {
        Attack,
        Skill
    }

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public enum CardViewMode
    {
        Collection,
        Shop,
        DeckBuilder,
        Battle
    }

    public enum SfxKey
    {
        ButtonClick,
        CardSelect,
        CardFlip,
        CardSwap,
        Attack,
        Hit,
        Heal,
        CardDeath,
        Victory,
        Defeat,
        Purchase
    }

    public enum BgmKey
    {
        Title,
        Battle
    }
}
