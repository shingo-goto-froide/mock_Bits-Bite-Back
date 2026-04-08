public class ZombieAbility : AbilityBase
{
    public ZombieAbility()
    {
        trigger = AbilityTrigger.OnBattleStart;
    }

    public override void Execute(BattleManager context)
    {
        var allies = owner.isPlayerSide ? context.playerUnits : context.enemyUnits;
        foreach (var ally in allies)
        {
            if (!ally.isAlive || ally == owner) continue;
            int diff = ally.formationIndex - owner.formationIndex;
            if (diff == 1 || diff == -1)
            {
                ally.hasPoisonAttack = true;
                context.AddLog($"{owner.monster.baseData.monsterName}の効果: {ally.monster.baseData.monsterName}に毒攻撃を付与");
            }
        }
        // ゾンビ自身も毒攻撃を持つ
        owner.hasPoisonAttack = true;
    }
}
