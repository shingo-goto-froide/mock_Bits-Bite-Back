using UnityEngine;

[CreateAssetMenu(fileName = "NewMonster", menuName = "BitsBiteBack/MonsterData")]
public class MonsterDataSO : ScriptableObject
{
    public MonsterType monsterType;
    public string monsterName;
    public int baseHp;
    public int baseAttack;
    public int speed;
    public int range;
    public int slotSize = 1;
    public AttackType attackType;
    public bool isPenetrate;
    [TextArea] public string abilityDescription;
    public RecipeEntry[] recipeMaterials;
}

[System.Serializable]
public struct RecipeEntry
{
    public MaterialType type;
    public int amount;
}
