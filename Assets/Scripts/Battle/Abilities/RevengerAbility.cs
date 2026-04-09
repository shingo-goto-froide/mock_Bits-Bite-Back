public class RevengerAbility : AbilityBase
{
    private int buffedDeathCount;

    public RevengerAbility()
    {
        trigger = AbilityTrigger.Passive;
    }

    public override void Execute(BattleManager context)
    {
        // Passive: 味方死亡時にBattleManagerから呼ばれる
    }

    public void OnAllyDeath(BattleManager context)
    {
        if (owner == null || !owner.isAlive) return;

        buffedDeathCount++;
        owner.monster.maxHp += 4;
        owner.monster.currentHp += 4;
        owner.monster.currentAttack += 2;
        context.AddLog($"{owner.monster.baseData.monsterName}が強化！HP+4 ATK+2（累計{buffedDeathCount}回）");
        context.NotifyBuff(owner);
    }
}
