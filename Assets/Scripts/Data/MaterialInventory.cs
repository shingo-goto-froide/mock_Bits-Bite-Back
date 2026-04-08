using System.Collections.Generic;

[System.Serializable]
public class MaterialInventory
{
    private Dictionary<MaterialType, int> materials = new Dictionary<MaterialType, int>();

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
