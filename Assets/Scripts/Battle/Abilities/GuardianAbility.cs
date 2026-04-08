public class GuardianAbility : AbilityBase
{
    public GuardianAbility()
    {
        trigger = AbilityTrigger.Passive;
    }

    public override void Execute(BattleManager context)
    {
        // Passive: ターゲット処理はBattleManager.FindTargetで直接処理
        // 魔法ダメージ1.5倍もBattleManager.ApplyDamageで処理
    }
}
