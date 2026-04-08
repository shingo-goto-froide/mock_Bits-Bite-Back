using System;
using System.Collections.Generic;
using UnityEngine;

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

    public GamePhase CurrentPhase { get; private set; }
    public int CurrentWave { get; private set; }
    public MaterialInventory Inventory { get; private set; }
    public List<MonsterInstance> OwnedMonsters { get; private set; }

    public GameBalanceSO Balance => balance;
    public BattleManager Battle => battleManager;
    public CraftingManager Crafting => craftingManager;
    public FormationManager Formation => formationManager;
    public MonsterDataSO[] AllMonsterData => allMonsterData;
    public EnemyWaveSO[] EnemyWaves => enemyWaves;

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
    }

    private void Start()
    {
        if (battleManager != null)
            battleManager.OnBattleEndEvent += OnBattleEnd;
    }

    private void OnDestroy()
    {
        if (battleManager != null)
            battleManager.OnBattleEndEvent -= OnBattleEnd;
        if (Instance == this)
            Instance = null;
    }

    public void StartGame()
    {
        CurrentWave = 0;
        Inventory = new MaterialInventory();
        OwnedMonsters.Clear();

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

    private void OnBattleEnd(BattleResult result)
    {
        if (result == BattleResult.Victory)
        {
            CurrentWave++;
            GiveReward();

            if (CurrentWave >= enemyWaves.Length)
            {
                GameClear();
            }
            else
            {
                TransitionTo(GamePhase.Reward);
            }
        }
        else
        {
            GameOver();
        }
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
        // バトル後のHP状態をMonsterInstanceに反映済み（BattleUnit経由で直接操作）
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

    public void RestartGame()
    {
        formationManager.Clear();
        StartGame();
    }

    public void AddMonster(MonsterInstance monster)
    {
        OwnedMonsters.Add(monster);
    }

    public void RemoveMonster(MonsterInstance monster)
    {
        OwnedMonsters.Remove(monster);
    }
}
