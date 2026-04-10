using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生成されたマップデータを保持する純粋C#クラス
/// </summary>
[System.Serializable]
public class DungeonMap
{
    public int width;
    public int height;
    public DungeonTileType[,] tiles;
    public List<EntityPlacement> entities;
    public Vector2Int entrancePos;
    public Vector2Int bossPos;
    public bool[,] explored;

    public DungeonMap(int width, int height)
    {
        this.width = width;
        this.height = height;
        tiles = new DungeonTileType[width, height];
        entities = new List<EntityPlacement>();
        explored = new bool[width, height];

        // 全タイルを壁で初期化
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                tiles[x, y] = DungeonTileType.Wall;
    }

    public DungeonTileType GetTile(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return DungeonTileType.Wall;
        return tiles[x, y];
    }

    public bool IsWalkable(int x, int y)
    {
        var tile = GetTile(x, y);
        return tile != DungeonTileType.Wall;
    }

    public void SetExplored(int x, int y, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    explored[nx, ny] = true;
            }
        }
    }

    public bool IsExplored(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;
        return explored[x, y];
    }

    public EntityPlacement GetEntityAt(int x, int y)
    {
        foreach (var entity in entities)
        {
            if (entity.position.x == x && entity.position.y == y)
                return entity;
        }
        return null;
    }

    public void RemoveEntity(EntityPlacement entity)
    {
        entities.Remove(entity);
    }
}

[System.Serializable]
public class EntityPlacement
{
    public DungeonEntityType type;
    public EnemyRank rank;
    public Vector2Int position;

    public EntityPlacement(DungeonEntityType type, Vector2Int position, EnemyRank rank = EnemyRank.Weak)
    {
        this.type = type;
        this.position = position;
        this.rank = rank;
    }
}
