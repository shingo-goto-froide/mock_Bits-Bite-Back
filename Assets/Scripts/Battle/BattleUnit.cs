using System.Collections.Generic;
using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    public MonsterInstance monster;
    public AbilityBase ability;
    public List<StatusEffect> statusEffects = new List<StatusEffect>();
    public int formationIndex;
    public bool isPlayerSide;
    public bool isAlive = true;
    public bool hasPoisonAttack;

    public void Initialize(MonsterInstance monster, int index, bool isPlayer)
    {
        this.monster = monster;
        this.formationIndex = index;
        this.isPlayerSide = isPlayer;
        this.isAlive = monster.IsAlive();
        this.hasPoisonAttack = false;
        ability = AbilityFactory.Create(monster.baseData.monsterType);
        ability?.Initialize(this);
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        return statusEffects.Exists(e => e.type == type);
    }

    public void AddStatusEffect(StatusEffect effect)
    {
        if (!HasStatusEffect(effect.type))
            statusEffects.Add(effect);
    }

    public void RemoveStatusEffect(StatusEffectType type)
    {
        statusEffects.RemoveAll(e => e.type == type);
    }

    public int ProcessTurnStartEffects(GameBalanceSO balance)
    {
        int poisonDmg = 0;
        if (HasStatusEffect(StatusEffectType.Poison))
        {
            poisonDmg = balance.poisonDamage;
            monster.TakeDamage(poisonDmg);
            if (!monster.IsAlive())
                isAlive = false;
        }

        var expired = new List<StatusEffect>();
        foreach (var effect in statusEffects)
        {
            if (effect.Tick())
                expired.Add(effect);
        }
        foreach (var effect in expired)
            statusEffects.Remove(effect);

        return poisonDmg;
    }

    public void Die()
    {
        isAlive = false;
        monster.currentHp = 0;
    }

    public void Revive(int hp)
    {
        isAlive = true;
        monster.currentHp = hp;
        statusEffects.Clear();
    }
}
