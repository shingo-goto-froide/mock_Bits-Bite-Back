public enum MonsterType
{
    Skeleton,
    Guardian,
    Archer,
    Zombie,
    ShadowWalker,
    SkeletonPriest,
    Orc,
    ShapeShifter,
    Martyr,
    Wraith,
    BarrierMage,
    GraveKeeper,
    Revenger
}

public enum MaterialType
{
    AnimalSkull,
    HumanSkull,
    LongWood,
    LongBone,
    OldSword
}

public enum StatusEffectType
{
    Poison,
    Stun,
    MagicBarrier
}

public enum GamePhase
{
    Title,
    Prepare,
    Dungeon,
    Battle,
    Reward
}

public enum DungeonTileType
{
    Wall,
    Floor,
    Corridor,
    Entrance,
    BossRoom
}

public enum DungeonEntityType
{
    Enemy,
    Treasure,
    Event,
    Boss
}

public enum EnemyRank
{
    Weak,
    Medium,
    Strong
}

public enum AbilityTrigger
{
    OnBattleStart,
    OnTurnStart,
    OnAction,
    OnAttack,
    OnDeath,
    OnBattleEnd,
    Passive
}

public enum AttackType
{
    Physical,
    Magic
}

public enum BattleResult
{
    Victory,
    Defeat
}

public enum MonsterRank
{
    D,  // ×0.8
    C,  // ×1.0（基本値）
    B,  // ×1.15
    A,  // ×1.3
    S   // ×1.5
}
