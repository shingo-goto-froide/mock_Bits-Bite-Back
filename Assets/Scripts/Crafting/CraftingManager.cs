using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    [SerializeField] private MonsterDataSO[] allMonsterData;
    [SerializeField] private GameBalanceSO balance;

    public MonsterDataSO[] AllMonsterData => allMonsterData;

    private void Awake()
    {
        if (balance == null)
            balance = Resources.Load<GameBalanceSO>("GameBalance");
        if (allMonsterData == null || allMonsterData.Length == 0)
            allMonsterData = Resources.LoadAll<MonsterDataSO>("Monsters");
    }

    public List<MonsterDataSO> GetCraftableMonsters(MaterialInventory inventory)
    {
        var craftable = new List<MonsterDataSO>();
        foreach (var data in allMonsterData)
        {
            if (data.recipeMaterials != null && inventory.CanAfford(data.recipeMaterials))
                craftable.Add(data);
        }
        return craftable;
    }

    public MonsterInstance Craft(MonsterDataSO data, MaterialInventory inventory, bool useCatalyst = false)
    {
        if (!inventory.CanAfford(data.recipeMaterials))
            return null;

        bool catalystUsed = useCatalyst && inventory.UseCatalyst();
        inventory.Consume(data.recipeMaterials);

        var rank = MonsterInstance.RollRank(catalystUsed);
        var monster = new MonsterInstance(data, rank);
        Debug.Log($"[Crafting] {data.monsterName} 練成！ ランク: {rank} (触媒: {catalystUsed})");
        return monster;
    }

    public void Disassemble(MonsterInstance monster, MaterialInventory inventory)
    {
        var result = GetDisassemblyResult(monster);
        foreach (var kvp in result)
        {
            inventory.Add(kvp.Key, kvp.Value);
        }
    }

    public Dictionary<MaterialType, int> GetDisassemblyResult(MonsterInstance monster)
    {
        var result = new Dictionary<MaterialType, int>();
        if (monster.baseData.recipeMaterials == null) return result;

        foreach (var entry in monster.baseData.recipeMaterials)
        {
            int returned = Mathf.Max(1, Mathf.FloorToInt(entry.amount * balance.disassemblyReturnRate));
            if (!result.ContainsKey(entry.type))
                result[entry.type] = 0;
            result[entry.type] += returned;
        }
        return result;
    }
}
