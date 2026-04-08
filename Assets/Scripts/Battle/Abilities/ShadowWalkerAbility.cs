public class ShadowWalkerAbility : AbilityBase
{
    public ShadowWalkerAbility()
    {
        trigger = AbilityTrigger.OnAttack;
    }

    public override void Execute(BattleManager context)
    {
        // ランダムターゲットはBattleManager.FindTargetで処理
    }
}
