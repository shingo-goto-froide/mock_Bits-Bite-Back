public class ShapeShifterAbility : AbilityBase
{
    private int originalAttack;
    private int originalHp;
    private int originalMaxHp;
    private int originalSpeed;
    private int originalRange;
    private AbilityBase originalAbility;
    private bool isCopying;

    public ShapeShifterAbility()
    {
        trigger = AbilityTrigger.OnAction;
    }

    public override void Execute(BattleManager context)
    {
        var allies = owner.isPlayerSide ? context.playerUnits : context.enemyUnits;
        BattleUnit backAlly = null;

        foreach (var ally in allies)
        {
            if (ally.formationIndex == owner.formationIndex + 1 && ally.isAlive)
            {
                backAlly = ally;
                break;
            }
        }

        if (backAlly == null) return;

        // 元のステータスを保存
        originalAttack = owner.monster.currentAttack;
        originalHp = owner.monster.currentHp;
        originalMaxHp = owner.monster.maxHp;
        originalSpeed = owner.monster.currentSpeed;
        originalRange = owner.monster.currentRange;
        originalAbility = owner.ability;

        // コピー
        owner.monster.currentAttack = backAlly.monster.currentAttack;
        owner.monster.maxHp = backAlly.monster.maxHp;
        owner.monster.currentSpeed = backAlly.monster.currentSpeed;
        owner.monster.currentRange = backAlly.monster.currentRange;
        owner.ability = backAlly.ability;
        isCopying = true;

        context.AddLog($"{owner.monster.baseData.monsterName}が{backAlly.monster.baseData.monsterName}に変身！");
    }

    public void RevertStats()
    {
        if (!isCopying) return;
        owner.monster.currentAttack = originalAttack;
        owner.monster.currentHp = originalHp;
        owner.monster.maxHp = originalMaxHp;
        owner.monster.currentSpeed = originalSpeed;
        owner.monster.currentRange = originalRange;
        owner.ability = this;
        isCopying = false;
    }

    public bool IsCopying => isCopying;
}
