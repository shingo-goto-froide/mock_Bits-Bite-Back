using System.Collections.Generic;

[System.Serializable]
public class MaterialInventory
{
    private Dictionary<MaterialType, int> materials = new Dictionary<MaterialType, int>();
    public int CatalystCount { get; private set; }
    private List<SoulData> souls = new List<SoulData>();

    public void AddCatalyst(int amount = 1) { CatalystCount += amount; }
    public bool UseCatalyst() { if (CatalystCount <= 0) return false; CatalystCount--; return true; }

    // === 魂管理 ===
    public void AddSoul(SoulData soul) { souls.Add(soul); }

    public void AddSoul(MonsterRank rank)
    {
        souls.Add(SoulData.CreateRandom(rank));
    }

    public List<SoulData> GetAllSouls() { return souls; }

    public int GetSoulCount(MonsterRank rank)
    {
        int count = 0;
        foreach (var s in souls) if (s.rank == rank) count++;
        return count;
    }

    public int GetTotalSoulCount() { return souls.Count; }

    public bool RemoveSoul(int soulId)
    {
        for (int i = 0; i < souls.Count; i++)
        {
            if (souls[i].id == soulId) { souls.RemoveAt(i); return true; }
        }
        return false;
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
