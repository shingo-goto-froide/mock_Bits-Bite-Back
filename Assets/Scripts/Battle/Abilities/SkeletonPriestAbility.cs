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
            context.AddLog($"{owner.monster.baseData.monsterName}の効果: {ally.monster.baseData.monsterName}のHPを{healAmount}回復");
        }
    }

    public override bool CanExecute(BattleManager context)
    {
        return owner != null && owner.isAlive;
    }
}
