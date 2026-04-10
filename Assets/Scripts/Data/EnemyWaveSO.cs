using UnityEngine;

[CreateAssetMenu(fileName = "NewWave", menuName = "BitsBiteBack/EnemyWave")]
public class EnemyWaveSO : ScriptableObject
{
    public int waveNumber;
    [Tooltip("ランダム編成を使用する場合はtrue")]
    public bool useRandomFormation;
    [Tooltip("ランダム時の敵の数（1〜5）")]
    [Range(1, 5)] public int randomEnemyCount = 3;
    [Tooltip("ランダム時のレベル補正")]
    public int randomLevel = 1;
    [Tooltip("ランダム時に使用する魔物プール（空なら全魔物から選ぶ）")]
    public MonsterDataSO[] randomPool;
    [Tooltip("固定編成の敵リスト")]
    public EnemyEntry[] enemies;
}

[System.Serializable]
public struct EnemyEntry
{
    public MonsterDataSO monsterData;
    [HideInInspector] public int level; // 未使用（互換性のため残す）
}
