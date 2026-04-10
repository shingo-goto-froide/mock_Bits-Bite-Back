using UnityEngine;

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

        // エンティティタイプに応じた見た目を設定
        switch (entityType)
        {
            case DungeonEntityType.Enemy:
                spriteRenderer.color = GetEnemyColor(rank);
                gameObject.name = $"Enemy_{rank}_{gridPosition}";
                break;
            case DungeonEntityType.Treasure:
                spriteRenderer.color = new Color(1f, 0.85f, 0.2f);
                gameObject.name = $"Treasure_{gridPosition}";
                break;
            case DungeonEntityType.Event:
                spriteRenderer.color = new Color(0.2f, 0.9f, 0.4f);
                gameObject.name = $"Event_{gridPosition}";
                break;
            case DungeonEntityType.Boss:
                spriteRenderer.color = new Color(0.8f, 0.1f, 0.1f);
                gameObject.name = $"Boss_{gridPosition}";
                break;
        }

        spriteRenderer.sortingOrder = 5;
        UpdateWorldPosition(tileSize);
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
