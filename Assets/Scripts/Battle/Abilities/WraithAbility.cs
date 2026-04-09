public class WraithAbility : AbilityBase
{
    public WraithAbility()
    {
        trigger = AbilityTrigger.OnDeath;
    }

    public override void Execute(BattleManager context)
    {
        var allies = owner.isPlayerSide ? context.playerUnits : context.enemyUnits;
        BattleUnit lastAlly = null;

        foreach (var ally in allies)
        {
            if (ally.isAlive && ally != owner)
            {
                if (lastAlly == null || ally.formationIndex > lastAlly.formationIndex)
                    lastAlly = ally;
            }
        }

        if (lastAlly != null)
        {
            lastAlly.monster.maxHp += 10;
            lastAlly.monster.currentHp += 10;
            lastAlly.monster.currentAttack += 3;
            context.AddLog($"{owner.monster.baseData.monsterName}の魂が{lastAlly.monster.baseData.monsterName}を強化（HP+10, ATK+3）");
            context.NotifyBuff(lastAlly);
        }
    }

    public override bool CanExecute(BattleManager context)
    {
        return owner != null;
    }
}
