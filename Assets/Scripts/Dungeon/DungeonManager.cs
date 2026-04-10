using System;
using System.Collections;
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
    public event Action OnStatusChanged;  // HP・素材等が変化した時

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

        // マップサイズ強制設定
        if (config != null)
        {
            int floor = GameManager.Instance != null ? GameManager.Instance.CurrentFloor : 1;
            config.mapWidth = 25;
            config.mapHeight = 25;
            config.minRooms = Mathf.Min(3 + floor, 8);
            config.maxRooms = Mathf.Min(5 + floor, 10);
            config.minRoomSize = 3;
            config.maxRoomSize = 6;
            config.enemyCount = Mathf.Min(2 + floor, 12);    // 階層1=3体、階層2=4体...
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

        // 隊列の魔物全員の年数+1
        var gm = GameManager.Instance;
        if (gm != null)
        {
            var formation = gm.Formation.GetFormation();
            foreach (var monster in formation)
                monster.age++;
        }

        Map.SetExplored(PlayerPos.x, PlayerPos.y, ExploreRadius);
        UpdateEntityVisibility();

        OnPlayerMoved?.Invoke(PlayerPos);
        ProcessPlayerMove();

        if (IsPlayerTurn && !isTransitioning)
            MoveEnemies();
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

        OnMessage?.Invoke("敵と遭遇！バトル開始！");
        SaveDungeonState();

        Debug.Log($"[DungeonManager] エンカウント！ Rank={enemy.rank}, Wave={wave.name}");

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SetDungeonBattle(wave, false);
            StartCoroutine(TransitionToBattle());
        }
    }

    /// <summary>タイルを分割破棄してからBattleSceneへ遷移</summary>
    private IEnumerator TransitionToBattle()
    {
        // DungeonUI のタイル親を探して事前破棄（シーン破棄時の負荷軽減）
        var tilesParent = GameObject.Find("DungeonTiles");
        if (tilesParent != null)
        {
            int count = tilesParent.transform.childCount;
            int batchSize = 100;
            for (int i = count - 1; i >= 0; i -= batchSize)
            {
                int start = Mathf.Max(0, i - batchSize + 1);
                for (int j = i; j >= start; j--)
                {
                    Destroy(tilesParent.transform.GetChild(j).gameObject);
                }
                yield return null; // 1フレーム待機
            }
            Destroy(tilesParent);
            yield return null;
        }

        // エンティティも事前破棄
        foreach (var entity in Entities)
        {
            if (entity != null) Destroy(entity.gameObject);
        }
        Entities.Clear();
        yield return null;

        Debug.Log("[DungeonManager] タイル事前破棄完了、BattleScene ロード");
        SceneManager.LoadScene("BattleScene");
    }

    /// <summary>宝箱を発見 → 素材をランダム付与</summary>
    private void OnTreasureFound(EntityPlacement treasure)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // 階層に応じて宝箱の中身が増える
        int floor = GameManager.Instance != null ? GameManager.Instance.CurrentFloor : 1;
        int min = config.treasureMaterialMin + (floor - 1);
        int max = config.treasureMaterialMax + (floor - 1);
        int count = UnityEngine.Random.Range(min, max + 1);
        var materialTypes = (MaterialType[])Enum.GetValues(typeof(MaterialType));
        var gained = new Dictionary<MaterialType, int>();

        for (int i = 0; i < count; i++)
        {
            var type = materialTypes[UnityEngine.Random.Range(0, materialTypes.Length)];
            gm.Inventory.Add(type, 1);
            if (!gained.ContainsKey(type)) gained[type] = 0;
            gained[type]++;
        }

        // 触媒のレアドロップ（15%）
        bool gotCatalyst = UnityEngine.Random.value < 0.15f;
        if (gotCatalyst)
            gm.Inventory.AddCatalyst();

        // メッセージ作成
        var parts = new List<string>();
        foreach (var kvp in gained)
            parts.Add($"{GetMaterialName(kvp.Key)}x{kvp.Value}");
        if (gotCatalyst)
            parts.Add("触媒x1");
        string msg = $"宝箱を発見！ {string.Join(", ", parts)} を入手！";

        Map.RemoveEntity(treasure);
        RemoveEntityGameObject(treasure);
        OnMessage?.Invoke(msg);
        OnStatusChanged?.Invoke();

        Debug.Log($"[DungeonManager] {msg}");
    }

    /// <summary>イベントマス → 回復 or 敵出現</summary>
    private void OnEventTriggered(EntityPlacement eventEntity)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        Map.RemoveEntity(eventEntity);
        RemoveEntityGameObject(eventEntity);

        // 確率で敵出現
        if (UnityEngine.Random.value < config.eventEnemyChance)
        {
            OnEventEnemyEncounter(eventEntity);
            return;
        }

        // 回復イベント
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

        string msg = $"回復の泉を発見！ 味方全体のHPが回復した！（合計{totalHealed}回復）";
        OnMessage?.Invoke(msg);
        OnStatusChanged?.Invoke();
        Debug.Log($"[DungeonManager] {msg}");
    }

    /// <summary>イベントマスからの敵出現</summary>
    private void OnEventEnemyEncounter(EntityPlacement eventEntity)
    {
        if (isTransitioning) return;

        IsPlayerTurn = false;
        isTransitioning = true;

        // Weak〜Medium のランダム
        var rank = UnityEngine.Random.value < 0.6f ? EnemyRank.Weak : EnemyRank.Medium;
        var wave = GetEnemyWaveForRank(rank);
        if (wave == null)
        {
            OnMessage?.Invoke("何かの気配がしたが、何もいなかった...");
            IsPlayerTurn = true;
            isTransitioning = false;
            return;
        }

        OnMessage?.Invoke("不意打ち！ 敵が現れた！");
        SaveDungeonState();

        Debug.Log($"[DungeonManager] イベント敵遭遇！ Rank={rank}, Wave={wave.name}");

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SetDungeonBattle(wave, false);
            StartCoroutine(TransitionToBattle());
        }
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
            StartCoroutine(TransitionToBattle());
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

            // 壁・他エンティティには移動しない
            if (!Map.IsWalkable(newPos.x, newPos.y)) continue;
            if (HasEntityAt(newPos, entity)) continue;

            entity.MoveTo(newPos, TileSize);
            entity.SetVisible(Map.IsExplored(newPos.x, newPos.y));

            // プレイヤーに到達 → エンカウント
            if (newPos == PlayerPos)
            {
                OnEnemyEncounter(entity.placementData);
                return;
            }
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

    /// <summary>階層に応じて敵Wave配列を設定（明示テーブル）</summary>
    private void AutoPopulateWaves()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.EnemyWaves == null || gm.EnemyWaves.Length == 0) return;

        var waveMap = new Dictionary<int, EnemyWaveSO>();
        foreach (var w in gm.EnemyWaves)
            waveMap[w.waveNumber] = w;

        int floor = gm.CurrentFloor;

        // Wave難易度テーブル（番号で指定）
        // W1:スケ×2, W8:スケ×3, W9:シャドウ+スケ×2, W2:ゾンビ+スケ×2,
        // W10:スケ×2+弓, W11:ゾンビ+スケ+弓, W12:弓×2+僧侶,
        // W3:ガーディアン+弓×2, W4:ガーディアン+ゾンビ+僧侶+スケ,
        // W5:結界師+ガーディアン+α, W6:オーク+僧侶+結界師, W7:シナジー編成

        int[] weakNums, medNums, strNums;
        int bossNum;

        switch (floor)
        {
            // === 階層1-4: スケルトン級 ===
            case 1:
                weakNums = new[]{ 1 };
                medNums  = new[]{ 1 };
                strNums  = new[]{ 1 };
                bossNum  = 1;               // スケ×2（チュートリアル）
                break;
            case 2:
                weakNums = new[]{ 1 };
                medNums  = new[]{ 1 };
                strNums  = new[]{ 1 };
                bossNum  = 8;               // スケ×3
                break;
            case 3:
                weakNums = new[]{ 1 };
                medNums  = new[]{ 1, 8 };
                strNums  = new[]{ 8 };
                bossNum  = 8;               // スケ×3
                break;
            case 4:
                weakNums = new[]{ 1, 8 };
                medNums  = new[]{ 8 };
                strNums  = new[]{ 8 };
                bossNum  = 9;               // シャドウ+スケ×2
                break;

            // === 階層5-7: 毒・射程が登場 ===
            case 5:
                weakNums = new[]{ 1, 8 };
                medNums  = new[]{ 8, 9 };
                strNums  = new[]{ 9 };
                bossNum  = 2;               // ゾンビ+スケ×2
                break;
            case 6:
                weakNums = new[]{ 8, 9 };
                medNums  = new[]{ 9, 2 };
                strNums  = new[]{ 2 };
                bossNum  = 10;              // スケ×2+アーチャー
                break;
            case 7:
                weakNums = new[]{ 9, 2 };
                medNums  = new[]{ 2, 10 };
                strNums  = new[]{ 10 };
                bossNum  = 11;              // ゾンビ+スケ+アーチャー
                break;

            // === 階層8-10: 回復・タンク登場 ===
            case 8:
                weakNums = new[]{ 2, 10 };
                medNums  = new[]{ 10, 11 };
                strNums  = new[]{ 11 };
                bossNum  = 12;              // アーチャー×2+僧侶
                break;
            case 9:
                weakNums = new[]{ 10, 11 };
                medNums  = new[]{ 11, 12 };
                strNums  = new[]{ 12 };
                bossNum  = 3;               // ガーディアン+アーチャー×2
                break;
            case 10:
                weakNums = new[]{ 11, 12 };
                medNums  = new[]{ 12, 3 };
                strNums  = new[]{ 3 };
                bossNum  = 4;               // ガーディアン+ゾンビ+僧侶+スケ
                break;

            // === 階層11+: フル編成 ===
            default:
                weakNums = new[]{ 12, 3 };
                medNums  = new[]{ 3, 4 };
                strNums  = new[]{ Mathf.Min(4 + (floor - 11), 7) };
                bossNum  = Mathf.Min(5 + (floor - 11), 7);
                break;
        }

        config.weakEnemyWaves   = Resolve(waveMap, weakNums);
        config.mediumEnemyWaves = Resolve(waveMap, medNums);
        config.strongEnemyWaves = Resolve(waveMap, strNums);

        config.bossWave = waveMap.TryGetValue(bossNum, out var bw) ? bw : config.strongEnemyWaves[0];

        Debug.Log($"[DungeonManager] 階層{floor}: " +
            $"Weak=[{Join(config.weakEnemyWaves)}], Med=[{Join(config.mediumEnemyWaves)}], " +
            $"Str=[{Join(config.strongEnemyWaves)}], Boss={config.bossWave?.name}");
    }

    private static EnemyWaveSO[] Resolve(Dictionary<int, EnemyWaveSO> map, int[] nums)
    {
        var list = new List<EnemyWaveSO>();
        foreach (var n in nums)
            if (map.TryGetValue(n, out var w)) list.Add(w);
        return list.Count > 0 ? list.ToArray() : null;
    }

    private static string Join(EnemyWaveSO[] waves)
    {
        if (waves == null) return "null";
        var names = new List<string>();
        foreach (var w in waves) names.Add(w.name);
        return string.Join(",", names);
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
