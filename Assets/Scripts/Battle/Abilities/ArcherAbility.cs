public class ArcherAbility : AbilityBase
{
    public ArcherAbility()
    {
        trigger = AbilityTrigger.OnAttack;
    }

    public override void Execute(BattleManager context)
    {
        // 5ターン目以降、2連射: BattleManager.ExecuteUnitActionで追加攻撃を処理
    }

    public bool ShouldDoubleAttack(BattleManager context)
    {
        return context.currentTurn >= context.Balance.archerDoubleAttackTurn;
    }
}
