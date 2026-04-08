using System.Collections.Generic;

public class BattleAction
{
    public BattleUnit actor;
    public List<BattleUnit> targets = new List<BattleUnit>();
    public int damage;
    public bool abilityTriggered;
    public string logMessage = "";
}
