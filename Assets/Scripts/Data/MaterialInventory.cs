using System.Collections.Generic;

[System.Serializable]
public class MaterialInventory
{
    private Dictionary<MaterialType, int> materials = new Dictionary<MaterialType, int>();
    public int CatalystCount { get; private set; }
    private Dictionary<MonsterRank, int> souls = new Dictionary<MonsterRank, int>();

    public void AddCatalyst(int amount = 1) { CatalystCount += amount; }
    public bool UseCatalyst() { if (CatalystCount <= 0) return false; CatalystCount--; return true; }

    // === 魂管理 ===
    public void AddSoul(MonsterRank rank, int amount = 1)
    {
        if (!souls.ContainsKey(rank)) souls[rank] = 0;
        souls[rank] += amount;
    }

    public int GetSoulCount(MonsterRank rank)
    {
        return souls.TryGetValue(rank, out int count) ? count : 0;
    }

    public int GetTotalSoulCount()
    {
        int total = 0;
        foreach (var kvp in souls) total += kvp.Value;
        return total;
    }

    public bool UseSoul(MonsterRank rank)
    {
        if (GetSoulCount(rank) <= 0) return false;
        souls[rank]--;
        return true;
    }

    public int GetAmount(MaterialType type)
    {
        return materials.TryGetValue(type, out int amount) ? amount : 0;
    }

    public void Add(MaterialType type, int amount)
    {
        if (!materials.ContainsKey(type))
            materials[type] = 0;
        materials[type] += amount;
    }

    public bool Remove(MaterialType type, int amount)
    {
        if (GetAmount(type) < amount)
            return false;
        materials[type] -= amount;
        return true;
    }

    public bool CanAfford(RecipeEntry[] recipe)
    {
        foreach (var entry in recipe)
        {
            if (GetAmount(entry.type) < entry.amount)
                return false;
        }
        return true;
    }

    public void Consume(RecipeEntry[] recipe)
    {
        foreach (var entry in recipe)
        {
            Remove(entry.type, entry.amount);
        }
    }

    public Dictionary<MaterialType, int> GetAll()
    {
        return new Dictionary<MaterialType, int>(materials);
    }
}
