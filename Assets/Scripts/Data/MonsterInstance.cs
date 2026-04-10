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
    public MonsterRank rank;
    public int age; // 年数（ダンジョン内の歩数で加算）

    private static int nextId;

    /// <summary>ランクなし（基本値、チュートリアル用）</summary>
    public MonsterInstance(MonsterDataSO data)
    {
        baseData = data;
        rank = MonsterRank.C;
        ApplyBaseStats();
        instanceId = nextId++;
    }

    /// <summary>ランク指定で生成（練成用）</summary>
    public MonsterInstance(MonsterDataSO data, MonsterRank rank)
    {
        baseData = data;
        this.rank = rank;
        ApplyBaseStats();
        ApplyRankMultiplier();
        instanceId = nextId++;
    }

    private void ApplyBaseStats()
    {
        maxHp = baseData.baseHp;
        currentHp = maxHp;
        currentAttack = baseData.baseAttack;
        currentSpeed = baseData.speed;
        currentRange = baseData.range;
    }

    private void ApplyRankMultiplier()
    {
        float mult = GetRankMultiplier(rank);
        if (Mathf.Approximately(mult, 1f)) return;

        maxHp = Mathf.Max(1, Mathf.RoundToInt(baseData.baseHp * mult));
        currentHp = maxHp;
        currentAttack = Mathf.Max(1, Mathf.RoundToInt(baseData.baseAttack * mult));
    }

    public static float GetRankMultiplier(MonsterRank rank)
    {
        switch (rank)
        {
            case MonsterRank.D: return 0.8f;
            case MonsterRank.C: return 1.0f;
            case MonsterRank.B: return 1.15f;
            case MonsterRank.A: return 1.3f;
            case MonsterRank.S: return 1.5f;
            default: return 1.0f;
        }
    }

    /// <summary>触媒なしのランク抽選</summary>
    public static MonsterRank RollRank(bool useCatalyst)
    {
        float r = Random.value * 100f;
        if (useCatalyst)
        {
            // S:15%, A:25%, B:35%, C:20%, D:5%
            if (r < 15f) return MonsterRank.S;
            if (r < 40f) return MonsterRank.A;
            if (r < 75f) return MonsterRank.B;
            if (r < 95f) return MonsterRank.C;
            return MonsterRank.D;
        }
        else
        {
            // S:2%, A:8%, B:20%, C:40%, D:30%
            if (r < 2f)  return MonsterRank.S;
            if (r < 10f) return MonsterRank.A;
            if (r < 30f) return MonsterRank.B;
            if (r < 70f) return MonsterRank.C;
            return MonsterRank.D;
        }
    }

    public string GetRankLabel()
    {
        return rank.ToString();
    }

    public Color GetRankColor()
    {
        return GetRankColor(rank);
    }

    public static Color GetRankColor(MonsterRank r)
    {
        switch (r)
        {
            case MonsterRank.S: return new Color(1f, 0.85f, 0.2f);   // 金
            case MonsterRank.A: return new Color(0.9f, 0.3f, 0.9f);  // 紫
            case MonsterRank.B: return new Color(0.3f, 0.6f, 1f);    // 青
            case MonsterRank.C: return new Color(0.5f, 0.8f, 0.5f);  // 緑
            case MonsterRank.D: return new Color(0.6f, 0.6f, 0.6f);  // グレー
            default: return Color.white;
        }
    }

    /// <summary>分解時に獲得できる魂のランクを年数から判定</summary>
    public MonsterRank GetSoulRank()
    {
        // 年数が多いほど高ランクの魂
        if (age >= 200) return MonsterRank.S;
        if (age >= 100) return MonsterRank.A;
        if (age >= 50)  return MonsterRank.B;
        if (age >= 20)  return MonsterRank.C;
        return MonsterRank.D;
    }

    public static string GetSoulRankLabel(MonsterRank rank)
    {
        return $"{rank}ランクの魂";
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
        ApplyBaseStats();
        ApplyRankMultiplier();
    }
}
