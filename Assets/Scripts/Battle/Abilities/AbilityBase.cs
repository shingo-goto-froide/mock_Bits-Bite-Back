public abstract class AbilityBase
{
    public AbilityTrigger trigger;
    public BattleUnit owner;

    public void Initialize(BattleUnit owner)
    {
        this.owner = owner;
    }

    public abstract void Execute(BattleManager context);

    public virtual bool CanExecute(BattleManager context)
    {
        return owner != null && owner.isAlive;
    }
}
