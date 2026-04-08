public class BarrierMageAbility : AbilityBase
{
    public BarrierMageAbility()
    {
        trigger = AbilityTrigger.OnBattleStart;
    }

    public override void Execute(BattleManager context)
    {
        var allies = owner.isPlayerSide ? context.playerUnits : context.enemyUnits;
        BattleUnit frontAlly = null;

        foreach (var ally in allies)
        {
            if (!ally.isAlive) continue;
            if (frontAlly == null || ally.formationIndex < frontAlly.formationIndex)
                frontAlly = ally;
        }

        if (frontAlly != null)
        {
            frontAlly.AddStatusEffect(new StatusEffect(StatusEffectType.MagicBarrier, -1));
            context.AddLog($"{owner.monster.baseData.monsterName}の効果: {frontAlly.monster.baseData.monsterName}に魔法バリアを付与");
        }
    }
}
