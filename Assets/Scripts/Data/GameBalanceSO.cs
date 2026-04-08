using UnityEngine;

[CreateAssetMenu(fileName = "GameBalance", menuName = "BitsBiteBack/GameBalance")]
public class GameBalanceSO : ScriptableObject
{
    [Header("隊列")]
    public int maxFormationSlots = 5;

    [Header("ウェーブ")]
    public int totalWaveCount = 7;

    [Header("状態異常")]
    public int poisonDamage = 3;
    [Range(0f, 1f)] public float stunChance = 0.3f;
    [Range(0f, 1f)] public float magicBarrierReduction = 0.5f;

    [Header("特殊効果")]
    public float guardianMagicMultiplier = 1.5f;
    public int archerDoubleAttackTurn = 5;
    [Range(0f, 1f)] public float skeletonPriestHealRate = 0.1f;

    [Header("練成・分解")]
    [Range(0f, 1f)] public float disassemblyReturnRate = 0.5f;

    [Header("報酬")]
    public int rewardBaseMaterials = 3;
    public int rewardPerWaveBonus = 1;

    [Header("初期支給")]
    public InitialMaterial[] initialMaterials;
}

[System.Serializable]
public struct InitialMaterial
{
    public MaterialType type;
    public int amount;
}
