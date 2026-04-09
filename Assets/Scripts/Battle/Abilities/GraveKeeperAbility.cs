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
                context.AddLog($"{ally.monster.baseData.monsterName}がHP{reviveHp}で復活！");
                context.NotifyBuff(ally);
                revived++;
            }
        }

        if (revived == 0)
            context.AddLog($"{owner.monster.baseData.monsterName}が死亡した味方を全員復活！（対象なし）");
    }

    public override bool CanExecute(BattleManager context)
    {
        return owner != null;
    }
}
