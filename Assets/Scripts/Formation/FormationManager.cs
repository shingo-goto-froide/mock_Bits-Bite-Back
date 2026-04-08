using System.Collections.Generic;
using UnityEngine;

public class FormationManager : MonoBehaviour
{
    [SerializeField] private GameBalanceSO balance;

    private MonsterInstance[] slots;

    private void Awake()
    {
        slots = new MonsterInstance[balance != null ? balance.maxFormationSlots : 5];
    }

    public void Init(GameBalanceSO balanceSO)
    {
        balance = balanceSO;
        slots = new MonsterInstance[balance.maxFormationSlots];
    }

    public bool PlaceMonster(MonsterInstance monster, int slotIndex)
    {
        int size = monster.baseData.slotSize;

        if (slotIndex < 0 || slotIndex + size > slots.Length)
            return false;

        for (int i = slotIndex; i < slotIndex + size; i++)
        {
            if (slots[i] != null)
                return false;
        }

        for (int i = slotIndex; i < slotIndex + size; i++)
        {
            slots[i] = monster;
        }
        return true;
    }

    public MonsterInstance RemoveMonster(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex] == null)
            return null;

        var monster = slots[slotIndex];
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == monster)
                slots[i] = null;
        }
        return monster;
    }

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Length || indexB < 0 || indexB >= slots.Length)
            return;

        var temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;
    }

    public List<MonsterInstance> GetFormation()
    {
        var formation = new List<MonsterInstance>();
        var added = new HashSet<int>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && !added.Contains(slots[i].instanceId))
            {
                formation.Add(slots[i]);
                added.Add(slots[i].instanceId);
            }
        }
        return formation;
    }

    public MonsterInstance GetSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    public int SlotCount => slots != null ? slots.Length : 0;

    public int GetUsedSlotCount()
    {
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                count++;
        }
        return count;
    }

    public int GetRemainingSlots()
    {
        return slots.Length - GetUsedSlotCount();
    }

    public bool IsValidFormation()
    {
        return GetFormation().Count > 0;
    }

    public void Clear()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i] = null;
    }
}
