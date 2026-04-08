using UnityEngine;

public class MartyrAbility : AbilityBase
{
    public MartyrAbility()
    {
        trigger = AbilityTrigger.OnAction;
    }

    public override void Execute(BattleManager context)
    {
        var allies = owner.isPlayerSide ? context.playerUnits : context.enemyUnits;

        foreach (var ally in allies)
        {
            if (!ally.isAlive || ally == owner) continue;
            if (ally.monster.currentHp >= ally.monster.maxHp) continue;

            int missingHp = ally.monster.maxHp - ally.monster.currentHp;
            int healAmount = Mathf.Min(missingHp, owner.monster.currentHp);
            if (healAmount <= 0) continue;

            ally.monster.Heal(healAmount);
            owner.monster.TakeDamage(healAmount);
            context.AddLog($"{owner.monster.baseData.monsterName}がHPを{healAmount}{ally.monster.baseData.monsterName}に分配");

            if (!owner.monster.IsAlive())
            {
                owner.isAlive = false;
                break;
            }
        }
    }
}
