public class SkeletonAbility : AbilityBase
{
    public SkeletonAbility()
    {
        trigger = AbilityTrigger.OnBattleStart;
    }

    public override void Execute(BattleManager context)
    {
        var allies = owner.isPlayerSide ? context.playerUnits : context.enemyUnits;
        foreach (var ally in allies)
        {
            if (ally.formationIndex == owner.formationIndex - 1 && ally.isAlive)
            {
                ally.monster.currentAttack += 1;
                context.AddLog($"{owner.monster.baseData.monsterName}が{ally.monster.baseData.monsterName}の攻撃力を+1した");
                break;
            }
        }
    }
}
