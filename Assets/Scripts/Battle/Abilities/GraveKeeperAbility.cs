public class GraveKeeperAbility : AbilityBase
{
    public GraveKeeperAbility()
    {
        trigger = AbilityTrigger.OnDeath;
    }

    public override void Execute(BattleManager context)
    {
        var allies = owner.isPlayerSide ? context.playerUnits : context.enemyUnits;
        int revived = 0;

        foreach (var ally in allies)
        {
            if (!ally.isAlive && ally != owner)
            {
                int reviveHp = ally.monster.maxHp / 2;
                ally.Revive(reviveHp);
                context.AddLog($"{owner.monster.baseData.monsterName}の効果: {ally.monster.baseData.monsterName}がHP{reviveHp}で復活！");
                revived++;
            }
        }

        if (revived == 0)
            context.AddLog($"{owner.monster.baseData.monsterName}の効果: 復活対象なし");
    }

    public override bool CanExecute(BattleManager context)
    {
        return owner != null;
    }
}
