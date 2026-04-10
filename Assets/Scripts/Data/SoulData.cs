using UnityEngine;

public enum SoulType
{
    Vigor,      // HP強化
    Fury,       // ATK強化
    Swiftness,  // SPD強化
    Fortitude,  // HP+ATK複合
    Precision   // 射程強化
}

[System.Serializable]
public class SoulData
{
    public SoulType type;
    public MonsterRank rank;
    public int id;

    private static int nextId;

    public SoulData(SoulType type, MonsterRank rank)
    {
        this.type = type;
        this.rank = rank;
        this.id = nextId++;
    }

    public string GetName()
    {
        switch (type)
        {
            case SoulType.Vigor:     return "活力の魂";
            case SoulType.Fury:      return "猛攻の魂";
            case SoulType.Swiftness: return "迅速の魂";
            case SoulType.Fortitude: return "剛健の魂";
            case SoulType.Precision: return "精密の魂";
            default: return "魂";
        }
    }

    public string GetEffectDescription()
    {
        int value = GetEffectValue();
        switch (type)
        {
            case SoulType.Vigor:     return $"HP +{value}";
            case SoulType.Fury:      return $"ATK +{value}";
            case SoulType.Swiftness: return $"SPD +{value}";
            case SoulType.Fortitude: return $"HP +{value / 2}  ATK +{value / 2}";
            case SoulType.Precision: return $"射程 +{(rank >= MonsterRank.B ? 2 : 1)}";
            default: return "";
        }
    }

    public int GetEffectValue()
    {
        switch (rank)
        {
            case MonsterRank.S: return 20;
            case MonsterRank.A: return 15;
            case MonsterRank.B: return 10;
            case MonsterRank.C: return 6;
            case MonsterRank.D: return 3;
            default: return 0;
        }
    }

    public Color GetTypeColor()
    {
        switch (type)
        {
            case SoulType.Vigor:     return new Color(0.3f, 0.85f, 0.4f);  // 緑
            case SoulType.Fury:      return new Color(0.9f, 0.35f, 0.3f);  // 赤
            case SoulType.Swiftness: return new Color(0.3f, 0.75f, 0.95f); // 水色
            case SoulType.Fortitude: return new Color(0.9f, 0.7f, 0.2f);   // 黄
            case SoulType.Precision: return new Color(0.8f, 0.5f, 0.9f);   // 紫
            default: return Color.white;
        }
    }

    /// <summary>ランクに応じたランダムな魂を生成</summary>
    public static SoulData CreateRandom(MonsterRank rank)
    {
        var types = System.Enum.GetValues(typeof(SoulType));
        var type = (SoulType)types.GetValue(Random.Range(0, types.Length));
        return new SoulData(type, rank);
    }
}
