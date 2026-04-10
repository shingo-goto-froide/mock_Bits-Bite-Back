using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ダンジョン探索のメインロジック
/// </summary>
public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }

    [Header("設定")]
    [SerializeField] private DungeonConfigSO config;

    public DungeonMap Map { get; private set; }
    public Vector2Int PlayerPos { get; private set; }
    public List<DungeonEntity> Entities { get; private set; } = new List<DungeonEntity>();
    public bool IsPlayerTurn { get; set; } = true;
    public int TurnCount { get; private set; }

    public const float TileSize = 1f;
    private const int ExploreRadius = 3;

    // ダンジョン状態保存用（バトル遷移時に使用）
    private static DungeonSaveData savedState;

    public event Action<Vector2Int> OnPlayerMoved;
    public event Action<string> OnMessage;
    public event Action OnDungeonInitialized;
    public event Action<EntityPlacement> OnEntityInteracted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (config == null)
            config = Resources.Load<DungeonConfigSO>("DungeonConfig");

        // SOアセットに古い値が入っている場合の安全策
        if (config != null && config.mapWidth > 20)
        {
            config.mapWidth = 15;
            config.mapHeight = 15;
            config.minRooms = 3;
            config.maxRooms = 5;
            config.minRoomSize = 3;
            config.maxRoomSize = 5;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        // バトルから戻ってきた場合は状態を復元
        if (savedState != null)
        {
            RestoreDungeonState();
        }
        else
        {
            InitializeDungeon();
        }
    }

    private void Update()
    {
        if (!IsPlayerTurn) return;

        // 方向キー入力（InputSystem）
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector2Int dir = Vector2Int.zero;
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
            dir = Vector2Int.up;
        else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
            dir = Vector2Int.down;
        else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
            dir = Vector2Int.left;
        else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
            dir = Vector2Int.right;

        if (dir != Vector2Int.zero)
            MovePlayer(dir);
    }

    /// <summary>マップ生成 → エンティティ配置 → プレイヤー配置 → UI初期化</summary>
    public void InitializeDungeon()
    {
        AutoPopulateWaves();

        var generator = new DungeonGenerator();
        Map = generator.Generate(config);
        PlayerPos = Map.entrancePos;
        TurnCount = 0;
        IsPlayerTurn = true;

        // フォグ初期化：プレイヤー周辺を探索済みに
        Map.SetExplored(PlayerPos.x, PlayerPos.y, ExploreRadius);

        // エンティティのGameObjectを生成
        SpawnEntities();

        OnDungeonInitialized?.Invoke();
        OnPlayerMoved?.Invoke(PlayerPos);

        Debug.Log($"[DungeonManager] ダンジョン初期化完了: {Map.width}x{Map.height}, エンティティ{Map.entities.Count}個");
    }

    private void SpawnEntities()
    {
        foreach (var entity in Entities)
        {
            if (entity != null) Destroy(entity.gameObject);
        }
        Entities.Clear();

        foreach (var placement in Map.entities)
        {
            var go = new GameObject($"Entity_{placement.type}");
            go.transform.SetParent(transform);
            // SpriteRendererを先に追加（Initializeで使用）
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            var entity = go.AddComponent<DungeonEntity>();
            entity.Initialize(placement, TileSize);

            // フォグ内なら非表示
            entity.SetVisible(Map.IsExplored(placement.position.x, placement.position.y));

            Entities.Add(entity);
        }
    }

    /// <summary>プレイヤーを1マス移動</summary>
    public void MovePlayer(Vector2Int direction)
    {
        if (!IsPlayerTurn) return;
        if (isTransitioning) return;

        Vector2Int newPos = PlayerPos + direction;
        if (!Map.IsWalkable(newPos.x, newPos.y)) return;

        PlayerPos = newPos;
        TurnCount++;

        Map.SetExplored(PlayerPos.x, PlayerPos.y, ExploreRadius);
        UpdateEntityVisibility();

        OnPlayerMoved?.Invoke(PlayerPos);
        ProcessPlayerMove();
        // 敵は静的配置（移動しない）。プレイヤーが踏んだらエンカウント
    }

    /// <summary>移動先のエンティティを処理</summary>
    private void ProcessPlayerMove()
    {
        var entityPlacement = Map.GetEntityAt(PlayerPos.x, PlayerPos.y);
        if (entityPlacement == null) return;

        OnEntityInteracted?.Invoke(entityPlacement);

        switch (entityPlacement.type)
        {
            case DungeonEntityType.Enemy:
                OnEnemyEncounter(entityPlacement);
                break;
            case DungeonEntityType.Treasure:
                OnTreasureFound(entityPlacement);
                break;
            case DungeonEntityType.Event:
                OnEventTriggered(entityPlacement);
                break;
            case DungeonEntityType.Boss:
                OnBossEncounter();
                break;
        }
    }

    /// <summary>敵シンボルに接触 → バトルへ遷移</summary>
    private void OnEnemyEncounter(EntityPlacement enemy)
    {
        if (isTransitioning) return;

        IsPlayerTurn = false;
        isTransitioning = true;

        var wave = GetEnemyWaveForRank(enemy.rank);
        if (wave == null)
        {
            Debug.LogWarning($"[DungeonManager] EnemyRank {enemy.rank} の Wave が未設定");
            IsPlayerTurn = true;
            isTransitioning = false;
            return;
        }

        // 遭遇した敵を記録して除去
        Map.RemoveEntity(enemy);
        RemoveEntityGameObject(enemy);

        Debug.Log($"[DungeonManager] エンカウント！ Rank={enemy.rank}, Wave={wave.name}");
        OnMessage?.Invoke($"エンカウント！ Rank={enemy.rank} Wave={wave.name}");

        // ★デバッグ: シーン遷移せずに戻る
        IsPlayerTurn = true;
        isTransitioning = false;
        return;

        /* --- 本来の処理（デバッグ後に有効化）---
        SaveDungeonState();

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SetDungeonBattle(wave, false);
            SceneManager.LoadSceneAsync("BattleScene");
        }
        */
    }

    /// <summary>宝箱を発見 → 素材をランダム付与</summary>
    private void OnTreasureFound(EntityPlacement treasure)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        int count = UnityEngine.Random.Range(config.treasureMaterialMin, config.treasureMaterialMax + 1);
        var materialTypes = (MaterialType[])Enum.GetValues(typeof(MaterialType));
        var gained = new Dictionary<MaterialType, int>();

        for (int i = 0; i < count; i++)
        {
            var type = materialTypes[UnityEngine.Random.Range(0, materialTypes.Length)];
            gm.Inventory.Add(type, 1);
            if (!gained.ContainsKey(type)) gained[type] = 0;
            gained[type]++;
        }

        // メッセージ作成
        var parts = new List<string>();
        foreach (var kvp in gained)
            parts.Add($"{GetMaterialName(kvp.Key)}x{kvp.Value}");
        string msg = $"宝箱を発見！ {string.Join(", ", parts)} を入手！";

        Map.RemoveEntity(treasure);
        RemoveEntityGameObject(treasure);
        OnMessage?.Invoke(msg);

        Debug.Log($"[DungeonManager] {msg}");
    }

    /// <summary>イベントマス → 回復イベント</summary>
    private void OnEventTriggered(EntityPlacement eventEntity)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        var formation = gm.Formation.GetFormation();
        int totalHealed = 0;

        foreach (var monster in formation)
        {
            if (monster.currentHp <= 0) continue;
            int healAmount = Mathf.CeilToInt(monster.maxHp * config.healPercent);
            int before = monster.currentHp;
            monster.Heal(healAmount);
            totalHealed += monster.currentHp - before;
        }

        Map.RemoveEntity(eventEntity);
        RemoveEntityGameObject(eventEntity);

        string msg = $"回復の泉を発見！ 味方全体のHPが回復した！（合計{totalHealed}回復）";
        OnMessage?.Invoke(msg);

        Debug.Log($"[DungeonManager] {msg}");
    }

    /// <summary>ボス部屋に入る → 確認後バトルへ遷移</summary>
    private void OnBossEncounter()
    {
        IsPlayerTurn = false;
        OnMessage?.Invoke("ボス部屋に到達！ 強大な敵の気配がする…");

        // DungeonUI がボス確認ダイアログを表示し、
        // 確認時に ConfirmBossBattle() を呼ぶ
    }

    /// <summary>ボス戦を開始（UIからの確認コールバック）</summary>
    public void ConfirmBossBattle()
    {
        if (config.bossWave == null)
        {
            Debug.LogWarning("[DungeonManager] bossWave が未設定");
            IsPlayerTurn = true;
            return;
        }

        // ボスエンティティを除去
        var bossEntity = Map.GetEntityAt(Map.bossPos.x, Map.bossPos.y);
        if (bossEntity != null)
        {
            Map.RemoveEntity(bossEntity);
            RemoveEntityGameObject(bossEntity);
        }

        SaveDungeonState();

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SetDungeonBattle(config.bossWave, true);
            SceneManager.LoadSceneAsync("BattleScene");
        }
    }

    /// <summary>ボス戦をキャンセル（UIからのキャンセルコールバック）</summary>
    public void CancelBossEncounter()
    {
        IsPlayerTurn = true;
    }

    private bool isTransitioning;

    /// <summary>全敵シンボルを確率で1マス移動</summary>
    private void MoveEnemies()
    {
        if (isTransitioning) return;

        // 敵はランダムに1マス移動するだけ。エンカウントはプレイヤー側（ProcessPlayerMove）でのみ判定
        var snapshot = new List<DungeonEntity>(Entities);

        foreach (var entity in snapshot)
        {
            if (entity == null) continue;
            if (entity.entityType != DungeonEntityType.Enemy) continue;
            if (isTransitioning) return;
            if (UnityEngine.Random.value > config.enemyMoveChance) continue;

            var dir = GetRandomDirection();
            Vector2Int newPos = entity.gridPosition + dir;

            // プレイヤー位置・他エンティティ位置・壁には移動しない
            if (newPos == PlayerPos) continue;
            if (!Map.IsWalkable(newPos.x, newPos.y)) continue;
            if (HasEntityAt(newPos, entity)) continue;

            entity.MoveTo(newPos, TileSize);
            entity.SetVisible(Map.IsExplored(newPos.x, newPos.y));
        }
    }

    private static readonly Vector2Int[] Directions = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private Vector2Int GetRandomDirection()
    {
        return Directions[UnityEngine.Random.Range(0, Directions.Length)];
    }

    /// <summary>指定位置に自分以外のエンティティがあるか</summary>
    private bool HasEntityAt(Vector2Int pos, DungeonEntity self)
    {
        foreach (var e in Entities)
        {
            if (e == null || e == self) continue;
            if (e.gridPosition == pos) return true;
        }
        return false;
    }

    /// <summary>ランクに応じた敵編成を返す</summary>
    public EnemyWaveSO GetEnemyWaveForRank(EnemyRank rank)
    {
        EnemyWaveSO[] waves = null;
        switch (rank)
        {
            case EnemyRank.Weak:   waves = config.weakEnemyWaves; break;
            case EnemyRank.Medium: waves = config.mediumEnemyWaves; break;
            case EnemyRank.Strong: waves = config.strongEnemyWaves; break;
        }

        if (waves == null || waves.Length == 0) return null;
        return waves[UnityEngine.Random.Range(0, waves.Length)];
    }

    /// <summary>バトル遷移前にダンジョン状態を保存</summary>
    public void SaveDungeonState()
    {
        savedState = new DungeonSaveData
        {
            map = Map,
            playerPos = PlayerPos,
            turnCount = TurnCount,
            entityPlacements = new List<EntityPlacement>(Map.entities)
        };
    }

    /// <summary>バトル後にダンジョン状態を復元</summary>
    public void RestoreDungeonState()
    {
        if (savedState == null) return;

        Map = savedState.map;
        PlayerPos = savedState.playerPos;
        TurnCount = savedState.turnCount;
        IsPlayerTurn = true;

        SpawnEntities();

        savedState = null;

        OnDungeonInitialized?.Invoke();
        OnPlayerMoved?.Invoke(PlayerPos);

        Debug.Log("[DungeonManager] ダンジョン状態を復元しました");
    }

    /// <summary>ダンジョンセーブデータをクリア（タイトルに戻る時等）</summary>
    public static void ClearSavedState()
    {
        savedState = null;
    }

    public static bool HasSavedState()
    {
        return savedState != null;
    }

    private void UpdateEntityVisibility()
    {
        foreach (var entity in Entities)
        {
            if (entity == null) continue;
            entity.SetVisible(Map.IsExplored(entity.gridPosition.x, entity.gridPosition.y));
        }
    }

    private void RemoveEntityGameObject(EntityPlacement placement)
    {
        for (int i = Entities.Count - 1; i >= 0; i--)
        {
            if (Entities[i] != null && Entities[i].placementData == placement)
            {
                Entities[i].Remove();
                Entities.RemoveAt(i);
                break;
            }
        }
    }

    private Sprite CreateSquareSprite()
    {
        var tex = new Texture2D(16, 16);
        var pixels = new Color[16 * 16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }

    /// <summary>DungeonConfigSOの敵Wave配列が空の場合、既存EnemyWaveSOから自動割り当て</summary>
    private void AutoPopulateWaves()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.EnemyWaves == null || gm.EnemyWaves.Length == 0) return;

        var waves = gm.EnemyWaves;

        // 弱敵: Wave 1-2（序盤）
        if (config.weakEnemyWaves == null || config.weakEnemyWaves.Length == 0)
        {
            var weak = new System.Collections.Generic.List<EnemyWaveSO>();
            foreach (var w in waves) { if (w.waveNumber <= 2) weak.Add(w); }
            if (weak.Count > 0) config.weakEnemyWaves = weak.ToArray();
            Debug.Log($"[DungeonManager] weakEnemyWaves を自動設定: {weak.Count}件");
        }

        // 中敵: Wave 3-5（中盤）
        if (config.mediumEnemyWaves == null || config.mediumEnemyWaves.Length == 0)
        {
            var medium = new System.Collections.Generic.List<EnemyWaveSO>();
            foreach (var w in waves) { if (w.waveNumber >= 3 && w.waveNumber <= 5) medium.Add(w); }
            if (medium.Count > 0) config.mediumEnemyWaves = medium.ToArray();
            Debug.Log($"[DungeonManager] mediumEnemyWaves を自動設定: {medium.Count}件");
        }

        // 強敵: Wave 6（終盤）
        if (config.strongEnemyWaves == null || config.strongEnemyWaves.Length == 0)
        {
            var strong = new System.Collections.Generic.List<EnemyWaveSO>();
            foreach (var w in waves) { if (w.waveNumber == 6) strong.Add(w); }
            if (strong.Count > 0) config.strongEnemyWaves = strong.ToArray();
            Debug.Log($"[DungeonManager] strongEnemyWaves を自動設定: {strong.Count}件");
        }

        // ボス: Wave 7（最終）
        if (config.bossWave == null)
        {
            foreach (var w in waves) { if (w.waveNumber == 7) { config.bossWave = w; break; } }
            // 見つからなければ最後のWave
            if (config.bossWave == null && waves.Length > 0)
                config.bossWave = waves[waves.Length - 1];
            Debug.Log($"[DungeonManager] bossWave を自動設定: {config.bossWave?.name}");
        }
    }

    private string GetMaterialName(MaterialType type)
    {
        switch (type)
        {
            case MaterialType.AnimalSkull: return "頭蓋骨(動物)";
            case MaterialType.HumanSkull:  return "頭蓋骨(人)";
            case MaterialType.LongWood:    return "長い木";
            case MaterialType.LongBone:    return "長い骨";
            case MaterialType.OldSword:    return "古い剣";
            default: return type.ToString();
        }
    }

    /// <summary>ダンジョン状態の保存データ</summary>
    private class DungeonSaveData
    {
        public DungeonMap map;
        public Vector2Int playerPos;
        public int turnCount;
        public List<EntityPlacement> entityPlacements;
    }
}
