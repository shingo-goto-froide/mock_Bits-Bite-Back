using UnityEngine;

[CreateAssetMenu(fileName = "DungeonConfig", menuName = "BitsBiteBack/DungeonConfig")]
public class DungeonConfigSO : ScriptableObject
{
    [Header("マップサイズ")]
    public int mapWidth = 15;
    public int mapHeight = 15;

    [Header("部屋")]
    public int minRooms = 3;
    public int maxRooms = 5;
    public int minRoomSize = 3;
    public int maxRoomSize = 5;

    [Header("エンティティ配置")]
    public int enemyCount = 5;
    public int treasureCount = 3;
    public int eventCount = 2;

    [Header("イベント")]
    [Range(0f, 1f)] public float healPercent = 0.3f;
    [Range(0f, 1f)] public float eventEnemyChance = 0.3f; // イベントマスで敵が出る確率

    [Header("敵シンボル")]
    [Range(0f, 1f)] public float enemyMoveChance = 0.5f;

    [Header("ボス強化")]
    public float bossHpMultiplier = 2.0f;
    public float bossAtkMultiplier = 1.5f;

    [Header("宝箱")]
    public int treasureMaterialMin = 1;
    public int treasureMaterialMax = 2;

    [Header("敵編成")]
    public EnemyWaveSO[] weakEnemyWaves;
    public EnemyWaveSO[] mediumEnemyWaves;
    public EnemyWaveSO[] strongEnemyWaves;
    public EnemyWaveSO bossWave;
}
