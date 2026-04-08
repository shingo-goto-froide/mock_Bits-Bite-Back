public static class AbilityFactory
{
    public static AbilityBase Create(MonsterType type)
    {
        return type switch
        {
            MonsterType.Skeleton => new SkeletonAbility(),
            MonsterType.Guardian => new GuardianAbility(),
            MonsterType.Archer => new ArcherAbility(),
            MonsterType.Zombie => new ZombieAbility(),
            MonsterType.ShadowWalker => new ShadowWalkerAbility(),
            MonsterType.SkeletonPriest => new SkeletonPriestAbility(),
            MonsterType.Orc => new OrcAbility(),
            MonsterType.ShapeShifter => new ShapeShifterAbility(),
            MonsterType.Martyr => new MartyrAbility(),
            MonsterType.Wraith => new WraithAbility(),
            MonsterType.BarrierMage => new BarrierMageAbility(),
            MonsterType.GraveKeeper => new GraveKeeperAbility(),
            MonsterType.Revenger => new RevengerAbility(),
            _ => null
        };
    }
}
