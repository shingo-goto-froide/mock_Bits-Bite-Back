public class OrcAbility : AbilityBase
{
    public OrcAbility()
    {
        trigger = AbilityTrigger.OnAttack;
    }

    public override void Execute(BattleManager context)
    {
        // ピヨリ付与はBattleManager.ExecuteUnitActionで攻撃後に処理
    }
}
