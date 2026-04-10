using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 右上のミニマップ表示
/// </summary>
public class MinimapUI : MonoBehaviour
{
    [SerializeField] private RawImage minimapImage;

    private Texture2D texture;
    private int mapWidth;
    private int mapHeight;

    private readonly Color playerColor = Color.green;
    private readonly Color exploredFloorColor = new Color(0.6f, 0.55f, 0.45f);
    private readonly Color exploredCorridorColor = new Color(0.5f, 0.45f, 0.35f);
    private readonly Color wallColor = new Color(0.2f, 0.2f, 0.25f);
    private readonly Color unexploredColor = new Color(0.08f, 0.08f, 0.1f);
    private readonly Color bossColor = new Color(0.8f, 0.15f, 0.15f);
    private readonly Color entranceColor = new Color(0.2f, 0.7f, 1f);
    private readonly Color enemyColor = new Color(0.9f, 0.3f, 0.3f);
    private readonly Color treasureColor = new Color(1f, 0.85f, 0.2f);
    private readonly Color eventColor = new Color(0.2f, 0.9f, 0.4f);

    public void Initialize(int width, int height)
    {
        mapWidth = width;
        mapHeight = height;
        texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        if (minimapImage == null)
            minimapImage = GetComponent<RawImage>();
        if (minimapImage != null)
            minimapImage.texture = texture;
    }

    public void UpdateMinimap(DungeonMap map, Vector2Int playerPos)
    {
        if (texture == null || map == null) return;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Color color;
                if (!map.IsExplored(x, y))
                {
                    color = unexploredColor;
                }
                else
                {
                    var tile = map.GetTile(x, y);
                    switch (tile)
                    {
                        case DungeonTileType.Wall:
                            color = wallColor;
                            break;
                        case DungeonTileType.Floor:
                            color = exploredFloorColor;
                            break;
                        case DungeonTileType.Corridor:
                            color = exploredCorridorColor;
                            break;
                        case DungeonTileType.Entrance:
                            color = entranceColor;
                            break;
                        case DungeonTileType.BossRoom:
                            color = bossColor;
                            break;
                        default:
                            color = unexploredColor;
                            break;
                    }

                    // 探索済みエンティティ表示
                    var entity = map.GetEntityAt(x, y);
                    if (entity != null)
                    {
                        switch (entity.type)
                        {
                            case DungeonEntityType.Enemy:   color = enemyColor; break;
                            case DungeonEntityType.Treasure: color = treasureColor; break;
                            case DungeonEntityType.Event:   color = eventColor; break;
                            case DungeonEntityType.Boss:    color = bossColor; break;
                        }
                    }
                }

                texture.SetPixel(x, y, color);
            }
        }

        // プレイヤー位置（上書き）
        if (playerPos.x >= 0 && playerPos.x < mapWidth && playerPos.y >= 0 && playerPos.y < mapHeight)
            texture.SetPixel(playerPos.x, playerPos.y, playerColor);

        texture.Apply();
    }
}
