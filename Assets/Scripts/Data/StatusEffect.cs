[System.Serializable]
public class StatusEffect
{
    public StatusEffectType type;
    public int remainingTurns;

    public StatusEffect(StatusEffectType type, int turns = -1)
    {
        this.type = type;
        this.remainingTurns = turns;
    }

    public bool Tick()
    {
        if (remainingTurns < 0)
            return false;
        remainingTurns--;
        return remainingTurns <= 0;
    }
}
