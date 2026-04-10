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
