using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("設定")]
    [SerializeField] private GameBalanceSO balance;
    [SerializeField] private MonsterDataSO[] allMonsterData;
    [SerializeField] private EnemyWaveSO[] enemyWaves;
    [SerializeField] private MonsterDataSO tutorialMonsterData;

    [Header("マネージャー参照")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private CraftingManager craftingManager;
    [SerializeField] private FormationManager formationManager;

    [Header("ダンジョン設定")]
    [SerializeField] private DungeonConfigSO dungeonConfig;

    public GamePhase CurrentPhase { get; private set; }
    public int CurrentWave { get; private set; }
    public MaterialInventory Inventory { get; private set; }
    public List<MonsterInstance> OwnedMonsters { get; private set; }

    // ダンジョンバトル用の状態
    public EnemyWaveSO DungeonBattleWave { get; private set; }
    public bool IsDungeonBattle { get; private set; }
    public bool IsBossBattle { get; private set; }

    public GameBalanceSO Balance => balance;
    public BattleManager Battle => battleManager;
    public CraftingManager Crafting => craftingManager;
    public FormationManager Formation => formationManager;
    public MonsterDataSO[] AllMonsterData => allMonsterData;
    public EnemyWaveSO[] EnemyWaves => enemyWaves;
    public DungeonConfigSO DungeonConfig => dungeonConfig;

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<Dictionary<MaterialType, int>> OnRewardGiven;
    public event Action OnGameOver;
    public event Action OnGameClear;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Inventory = new MaterialInventory();
        OwnedMonsters = new List<MonsterInstance>();

        LoadReferencesIfMissing();

        if (dungeonConfig == null)
            dungeonConfig = Resources.Load<DungeonConfigSO>("DungeonConfig");

        // 同一GameObjectのマネージャーを取得
        if (battleManager == null) battleManager = GetComponent<BattleManager>();
        if (craftingManager == null) craftingManager = GetComponent<CraftingManager>();
        if (formationManager == null) formationManager = GetComponent<FormationManager>();

        // マネージャーの初期化
        if (formationManager != null && balance != null)
            formationManager.Init(balance);
    }

    private void LoadReferencesIfMissing()
    {
        if (balance == null)
            balance = Resources.Load<GameBalanceSO>("GameBalance");

        if (allMonsterData == null || allMonsterData.Length == 0)
        {
            var loaded = Resources.LoadAll<MonsterDataSO>("Monsters");
            if (loaded.Length > 0)
                allMonsterData = loaded;
        }

        if (enemyWaves == null || enemyWaves.Length == 0)
        {
            var loaded = Resources.LoadAll<EnemyWaveSO>("EnemyWaves");
            if (loaded.Length > 0)
            {
                System.Array.Sort(loaded, (a, b) => a.waveNumber.CompareTo(b.waveNumber));
                enemyWaves = loaded;
            }
        }

        if (tutorialMonsterData == null && allMonsterData != null)
        {
            foreach (var data in allMonsterData)
            {
                if (data.monsterType == MonsterType.Skeleton)
                {
                    tutorialMonsterData = data;
                    break;
                }
            }
        }

        if (balance == null)
            Debug.LogError("[GameManager] GameBalanceSO が見つかりません。Resources/GameBalance に配置してください。");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StartGame()
    {
        CurrentWave = 0;
        Inventory = new MaterialInventory();
        OwnedMonsters.Clear();
        if (formationManager != null)
            formationManager.Clear();
        ClearDungeonBattle();
        DungeonManager.ClearSavedState();

        // 初期素材を配布
        if (balance.initialMaterials != null)
        {
            foreach (var mat in balance.initialMaterials)
                Inventory.Add(mat.type, mat.amount);
        }

        // チュートリアル用魔物を付与
        if (tutorialMonsterData != null)
        {
            var tutorialMonster = new MonsterInstance(tutorialMonsterData);
            OwnedMonsters.Add(tutorialMonster);
        }

        TransitionTo(GamePhase.Prepare);
    }

    public void TransitionTo(GamePhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
        Debug.Log($"[GameManager] フェーズ遷移: {phase}");
    }

    public void StartBattle()
    {
        if (formationManager == null || !formationManager.IsValidFormation())
        {
            Debug.LogWarning("隊列が空です。魔物を配置してください。");
            return;
        }

        if (CurrentWave >= enemyWaves.Length)
        {
            GameClear();
            return;
        }

        var formation = formationManager.GetFormation();
        battleManager.SetupBattle(formation, enemyWaves[CurrentWave], allMonsterData);
        TransitionTo(GamePhase.Battle);
        battleManager.StartBattle();
    }

    public void GiveReward()
    {
        int materialCount = balance.rewardBaseMaterials + CurrentWave * balance.rewardPerWaveBonus;
        var rewards = new Dictionary<MaterialType, int>();
        var materialTypes = (MaterialType[])Enum.GetValues(typeof(MaterialType));

        for (int i = 0; i < materialCount; i++)
        {
            var type = materialTypes[UnityEngine.Random.Range(0, materialTypes.Length)];
            Inventory.Add(type, 1);

            if (!rewards.ContainsKey(type))
                rewards[type] = 0;
            rewards[type]++;
        }

        OnRewardGiven?.Invoke(rewards);
        Debug.Log($"[GameManager] 報酬: 素材{materialCount}個獲得");
    }

    public void ReturnToPrepare()
    {
        // HP0の魔物をHP1で復帰させる
        foreach (var monster in OwnedMonsters)
        {
            if (monster.currentHp <= 0)
                monster.currentHp = 1;
        }
        battleManager.ClearBattle();
        TransitionTo(GamePhase.Prepare);
    }

    public void GameOver()
    {
        Debug.Log("[GameManager] ゲームオーバー");
        battleManager.ClearBattle();
        OnGameOver?.Invoke();
    }

    public void GameClear()
    {
        Debug.Log("[GameManager] ゲームクリア！");
        battleManager.ClearBattle();
        OnGameClear?.Invoke();
    }

    public void AdvanceWave()
    {
        CurrentWave++;
    }

    public void RestartGame()
    {
        DungeonManager.ClearSavedState();
        formationManager.Clear();
        StartGame();
    }

    /// <summary>ダンジョンバトル情報をセット（DungeonManager から呼ばれる）</summary>
    public void SetDungeonBattle(EnemyWaveSO wave, bool isBoss)
    {
        DungeonBattleWave = wave;
        IsDungeonBattle = true;
        IsBossBattle = isBoss;
    }

    /// <summary>ダンジョンバトル情報をクリア</summary>
    public void ClearDungeonBattle()
    {
        DungeonBattleWave = null;
        IsDungeonBattle = false;
        IsBossBattle = false;
    }

    /// <summary>ダンジョンバトル終了時の処理</summary>
    public void OnDungeonBattleEnd(BattleResult result)
    {
        if (result == BattleResult.Victory)
        {
            if (IsBossBattle)
            {
                OnBossDefeated();
            }
            else
            {
                // 通常バトル勝利 → ダンジョンに戻る
                battleManager.ClearBattle();
                ClearDungeonBattle();
                // HP0の魔物をHP1で復帰
                foreach (var monster in OwnedMonsters)
                {
                    if (monster.currentHp <= 0)
                        monster.currentHp = 1;
                }
                SceneManager.LoadScene("DungeonScene");
            }
        }
        else
        {
            // 敗北 → タイトルに戻る
            battleManager.ClearBattle();
            ClearDungeonBattle();
            DungeonManager.ClearSavedState();
            GameOver();
            SceneManager.LoadScene("TitleScene");
        }
    }

    /// <summary>ボス撃破時のクリア処理</summary>
    public void OnBossDefeated()
    {
        Debug.Log("[GameManager] ダンジョンクリア！ボスを撃破！");
        battleManager.ClearBattle();
        ClearDungeonBattle();
        DungeonManager.ClearSavedState();
        OnGameClear?.Invoke();
    }

    public void AddMonster(MonsterInstance monster)
    {
        OwnedMonsters.Add(monster);
    }

    public void RemoveMonster(MonsterInstance monster)
    {
        OwnedMonsters.Remove(monster);
    }

    // === デバッグ用 ===

    public void DebugMaxMaterials()
    {
        foreach (MaterialType type in System.Enum.GetValues(typeof(MaterialType)))
            Inventory.Add(type, 99 - Inventory.GetAmount(type));
    }

    public void DebugAddAllMonsters()
    {
        foreach (var data in allMonsterData)
            OwnedMonsters.Add(new MonsterInstance(data));
    }

    public void DebugHealAll()
    {
        foreach (var m in OwnedMonsters)
        {
            m.currentHp = m.maxHp;
        }
    }

    public void DebugClearMonsters()
    {
        OwnedMonsters.Clear();
        formationManager.Clear();
    }

    public void DebugSetWave(int wave)
    {
        CurrentWave = Mathf.Clamp(wave, 0, enemyWaves.Length - 1);
    }
}
