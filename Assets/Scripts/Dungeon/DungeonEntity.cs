using UnityEngine;
using TMPro;

/// <summary>
/// ダンジョン上の敵・宝箱・イベント・ボスのビジュアル表現
/// </summary>
public class DungeonEntity : MonoBehaviour
{
    public DungeonEntityType entityType;
    public EnemyRank rank;
    public Vector2Int gridPosition;

    [HideInInspector] public SpriteRenderer spriteRenderer;
    [HideInInspector] public EntityPlacement placementData;

    public void Initialize(EntityPlacement data, float tileSize)
    {
        placementData = data;
        entityType = data.type;
        rank = data.rank;
        gridPosition = data.position;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sortingOrder = 5;

        switch (entityType)
        {
            case DungeonEntityType.Enemy:
                SetupEnemySprite(tileSize);
                DungeonUI.AddOutline(gameObject, new Color(1f, 0.3f, 0.3f), tileSize);
                break;
            case DungeonEntityType.Boss:
                SetupBossSprite(tileSize);
                DungeonUI.AddOutline(gameObject, new Color(1f, 0.1f, 0.1f), tileSize);
                break;
            case DungeonEntityType.Treasure:
                SetupTextLabel("宝", new Color(1f, 0.85f, 0.2f), tileSize);
                break;
            case DungeonEntityType.Event:
                SetupTextLabel("?", new Color(0.8f, 0.7f, 0.2f), tileSize);
                break;
        }

        UpdateWorldPosition(tileSize);
    }

    /// <summary>敵: Waveの先頭魔物スプライトを表示</summary>
    private void SetupEnemySprite(float tileSize)
    {
        gameObject.name = $"Enemy_{rank}_{gridPosition}";

        var dm = DungeonManager.Instance;
        if (dm != null)
        {
            var wave = dm.GetEnemyWaveForRank(rank);
            if (wave != null && !wave.useRandomFormation && wave.enemies != null && wave.enemies.Length > 0)
            {
                var leaderData = wave.enemies[0].monsterData;
                if (leaderData != null)
                {
                    var sprite = MonsterSpriteLoader.GetSprite(leaderData.monsterType);
                    if (sprite != null)
                    {
                        spriteRenderer.sprite = sprite;
                        spriteRenderer.color = Color.white;
                        float scale = tileSize / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) * 0.8f;
                        transform.localScale = new Vector3(
                            MonsterSpriteLoader.IsLeftFacing(leaderData.monsterType) ? -scale : scale,
                            scale, 1f);
                        return;
                    }
                }
            }
        }

        // フォールバック: 色付き四角
        spriteRenderer.color = GetEnemyColor(rank);
    }

    /// <summary>ボス: ボスWaveの先頭魔物スプライトを表示（大きめ）</summary>
    private void SetupBossSprite(float tileSize)
    {
        gameObject.name = $"Boss_{gridPosition}";

        var dm = DungeonManager.Instance;
        if (dm != null)
        {
            var config = GameManager.Instance?.DungeonConfig;
            var wave = config?.bossWave;
            if (wave != null && !wave.useRandomFormation && wave.enemies != null && wave.enemies.Length > 0)
            {
                var leaderData = wave.enemies[0].monsterData;
                if (leaderData != null)
                {
                    var sprite = MonsterSpriteLoader.GetSprite(leaderData.monsterType);
                    if (sprite != null)
                    {
                        spriteRenderer.sprite = sprite;
                        spriteRenderer.color = Color.white;
                        float scale = tileSize / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) * 1.0f;
                        transform.localScale = new Vector3(
                            MonsterSpriteLoader.IsLeftFacing(leaderData.monsterType) ? -scale : scale,
                            scale, 1f);
                        return;
                    }
                }
            }
        }

        // フォールバック: 赤い四角
        spriteRenderer.color = new Color(0.8f, 0.1f, 0.1f);
    }

    /// <summary>宝箱・イベント: テキストラベルで表示</summary>
    private void SetupTextLabel(string label, Color bgColor, float tileSize)
    {
        gameObject.name = $"{label}_{gridPosition}";

        // 背景色の四角
        spriteRenderer.color = bgColor;

        // テキストを子オブジェクトで追加
        var textGo = new GameObject("Label");
        textGo.transform.SetParent(transform, false);
        textGo.transform.localPosition = new Vector3(0, 0, -0.05f);

        var tmp = textGo.AddComponent<TextMeshPro>();
        tmp.text = label;
        tmp.fontSize = 4;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 6;

        var rt = textGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(tileSize, tileSize);
    }

    public void MoveTo(Vector2Int newPos, float tileSize)
    {
        gridPosition = newPos;
        if (placementData != null)
            placementData.position = newPos;
        UpdateWorldPosition(tileSize);
    }

    public void Remove()
    {
        Destroy(gameObject);
    }

    public void SetVisible(bool visible)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;
        // 子オブジェクト（テキストラベル）も連動
        foreach (Transform child in transform)
            child.gameObject.SetActive(visible);
    }

    private void UpdateWorldPosition(float tileSize)
    {
        float half = tileSize * 0.5f;
        transform.position = new Vector3(gridPosition.x * tileSize + half, gridPosition.y * tileSize + half, -0.1f);
    }

    private Color GetEnemyColor(EnemyRank rank)
    {
        switch (rank)
        {
            case EnemyRank.Weak:   return new Color(0.6f, 0.4f, 0.8f);
            case EnemyRank.Medium: return new Color(0.9f, 0.5f, 0.2f);
            case EnemyRank.Strong: return new Color(0.9f, 0.2f, 0.2f);
            default:               return Color.white;
        }
    }
}
