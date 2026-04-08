using UnityEngine;

[System.Serializable]
public class MonsterInstance
{
    public MonsterDataSO baseData;
    public int currentHp;
    public int maxHp;
    public int currentAttack;
    public int currentSpeed;
    public int currentRange;
    public int instanceId;

    private static int nextId;

    public MonsterInstance(MonsterDataSO data)
    {
        baseData = data;
        maxHp = data.baseHp;
        currentHp = maxHp;
        currentAttack = data.baseAttack;
        currentSpeed = data.speed;
        currentRange = data.range;
        instanceId = nextId++;
    }

    public bool TakeDamage(int amount)
    {
        currentHp = Mathf.Max(0, currentHp - amount);
        return currentHp <= 0;
    }

    public void Heal(int amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }

    public bool IsAlive()
    {
        return currentHp > 0;
    }

    public void ResetToBase()
    {
        maxHp = baseData.baseHp;
        currentHp = maxHp;
        currentAttack = baseData.baseAttack;
        currentSpeed = baseData.speed;
        currentRange = baseData.range;
    }
}
