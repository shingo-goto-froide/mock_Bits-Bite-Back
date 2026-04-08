using UnityEngine;

[CreateAssetMenu(fileName = "NewWave", menuName = "BitsBiteBack/EnemyWave")]
public class EnemyWaveSO : ScriptableObject
{
    public int waveNumber;
    public EnemyEntry[] enemies;
}

[System.Serializable]
public struct EnemyEntry
{
    public MonsterDataSO monsterData;
    public int level;
}
