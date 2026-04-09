using UnityEngine;

public class SkeletonPriestAbility : AbilityBase
{
    public SkeletonPriestAbility()
    {
        trigger = AbilityTrigger.OnBattleEnd;
    }

    public override void Execute(BattleManager context)
    {
        var allies = owner.isPlayerSide ? context.playerUnits : context.enemyUnits;
        float healRate = context.Balance.skeletonPriestHealRate;

        foreach (var ally in allies)
        {
            if (!ally.isAlive) continue;
            int healAmount = Mathf.Max(1, Mathf.FloorToInt(ally.monster.maxHp * healRate));
            ally.monster.Heal(healAmount);
            context.AddLog($"味方全体をHP{healAmount}回復（{ally.monster.baseData.monsterName}）");
            context.NotifyBuff(ally);
        }
    }

    public override bool CanExecute(BattleManager context)
    {
        return owner != null && owner.isAlive;
    }
}
