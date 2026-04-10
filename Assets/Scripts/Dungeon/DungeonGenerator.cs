using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ダンジョンマップのランダム生成ロジック（純粋C#クラス）
/// </summary>
public class DungeonGenerator
{
    /// <summary>部屋データ</summary>
    public class Room
    {
        public int x, y, width, height;
        public Vector2Int center;

        public Room(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            center = new Vector2Int(x + width / 2, y + height / 2);
        }

        public bool Overlaps(Room other, int padding = 1)
        {
            return x - padding < other.x + other.width &&
                   x + width + padding > other.x &&
                   y - padding < other.y + other.height &&
                   y + height + padding > other.y;
        }
    }

    /// <summary>マップを生成して返す</summary>
    public DungeonMap Generate(DungeonConfigSO config)
    {
        var map = new DungeonMap(config.mapWidth, config.mapHeight);
        var rooms = GenerateRooms(config.mapWidth, config.mapHeight,
            config.minRooms, config.maxRooms, config.minRoomSize, config.maxRoomSize);

        // 部屋をマップに書き込み
        foreach (var room in rooms)
        {
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int y = room.y; y < room.y + room.height; y++)
                {
                    if (x >= 0 && x < map.width && y >= 0 && y < map.height)
                        map.tiles[x, y] = DungeonTileType.Floor;
                }
            }
        }

        ConnectRooms(map, rooms);
        PlaceEntrance(map, rooms);
        PlaceBossRoom(map, rooms);
        PlaceEntities(map, config);

        return map;
    }

    /// <summary>部屋をランダムに配置する</summary>
    public List<Room> GenerateRooms(int mapWidth, int mapHeight, int minRooms, int maxRooms, int minSize, int maxSize)
    {
        var rooms = new List<Room>();
        int targetCount = Random.Range(minRooms, maxRooms + 1);
        int maxAttempts = targetCount * 20;

        for (int attempt = 0; attempt < maxAttempts && rooms.Count < targetCount; attempt++)
        {
            int w = Random.Range(minSize, maxSize + 1);
            int h = Random.Range(minSize, maxSize + 1);
            int x = Random.Range(1, mapWidth - w - 1);
            int y = Random.Range(1, mapHeight - h - 1);

            var newRoom = new Room(x, y, w, h);
            bool overlaps = false;
            foreach (var existing in rooms)
            {
                if (newRoom.Overlaps(existing, 2))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
                rooms.Add(newRoom);
        }

        // 最低2部屋は確保
        if (rooms.Count < 2)
        {
            rooms.Clear();
            rooms.Add(new Room(2, 2, minSize, minSize));
            rooms.Add(new Room(mapWidth - minSize - 2, mapHeight - minSize - 2, minSize, minSize));
        }

        return rooms;
    }

    /// <summary>部屋同士を通路で接続する</summary>
    public void ConnectRooms(DungeonMap map, List<Room> rooms)
    {
        for (int i = 0; i < rooms.Count - 1; i++)
        {
            var a = rooms[i].center;
            var b = rooms[i + 1].center;

            // L字型の通路を掘る
            if (Random.value < 0.5f)
            {
                CarveHorizontal(map, a.x, b.x, a.y);
                CarveVertical(map, a.y, b.y, b.x);
            }
            else
            {
                CarveVertical(map, a.y, b.y, a.x);
                CarveHorizontal(map, a.x, b.x, b.y);
            }
        }
    }

    private void CarveHorizontal(DungeonMap map, int x1, int x2, int y)
    {
        int min = Mathf.Min(x1, x2);
        int max = Mathf.Max(x1, x2);
        for (int x = min; x <= max; x++)
        {
            if (x >= 0 && x < map.width && y >= 0 && y < map.height)
            {
                if (map.tiles[x, y] == DungeonTileType.Wall)
                    map.tiles[x, y] = DungeonTileType.Corridor;
            }
        }
    }

    private void CarveVertical(DungeonMap map, int y1, int y2, int x)
    {
        int min = Mathf.Min(y1, y2);
        int max = Mathf.Max(y1, y2);
        for (int y = min; y <= max; y++)
        {
            if (x >= 0 && x < map.width && y >= 0 && y < map.height)
            {
                if (map.tiles[x, y] == DungeonTileType.Wall)
                    map.tiles[x, y] = DungeonTileType.Corridor;
            }
        }
    }

    /// <summary>入口を最初の部屋に配置</summary>
    public void PlaceEntrance(DungeonMap map, List<Room> rooms)
    {
        if (rooms.Count == 0) return;
        map.entrancePos = rooms[0].center;
        map.tiles[map.entrancePos.x, map.entrancePos.y] = DungeonTileType.Entrance;
    }

    /// <summary>ボス部屋を入口から最も遠い部屋に配置</summary>
    public void PlaceBossRoom(DungeonMap map, List<Room> rooms)
    {
        if (rooms.Count < 2) return;

        float maxDist = 0f;
        Room farthest = rooms[rooms.Count - 1];

        foreach (var room in rooms)
        {
            if (room.center == map.entrancePos) continue;
            float dist = Vector2Int.Distance(map.entrancePos, room.center);
            if (dist > maxDist)
            {
                maxDist = dist;
                farthest = room;
            }
        }

        map.bossPos = farthest.center;
        // ボス部屋のタイルをマーク
        for (int x = farthest.x; x < farthest.x + farthest.width; x++)
        {
            for (int y = farthest.y; y < farthest.y + farthest.height; y++)
            {
                if (x >= 0 && x < map.width && y >= 0 && y < map.height)
                    map.tiles[x, y] = DungeonTileType.BossRoom;
            }
        }

        // ボスエンティティ
        map.entities.Add(new EntityPlacement(DungeonEntityType.Boss, map.bossPos));
    }

    /// <summary>敵・宝箱・イベントを配置する</summary>
    public void PlaceEntities(DungeonMap map, DungeonConfigSO config)
    {
        var walkableTiles = GetWalkableTiles(map);

        // 入口・ボス周辺は除外
        walkableTiles.RemoveAll(t =>
            Vector2Int.Distance(t, map.entrancePos) < 3f ||
            Vector2Int.Distance(t, map.bossPos) < 3f);

        // 距離に基づいてソート（入口からの距離）
        walkableTiles.Sort((a, b) =>
            Vector2Int.Distance(a, map.entrancePos).CompareTo(Vector2Int.Distance(b, map.entrancePos)));

        // 敵シンボル配置（距離に応じてランクを割り当て）
        int enemyPlaced = 0;
        float maxDist = walkableTiles.Count > 0 ?
            Vector2Int.Distance(walkableTiles[walkableTiles.Count - 1], map.entrancePos) : 1f;

        // シャッフルして配置候補を作るが、ランクは距離で決定
        var enemyCandidates = new List<Vector2Int>(walkableTiles);
        ShuffleList(enemyCandidates);

        foreach (var pos in enemyCandidates)
        {
            if (enemyPlaced >= config.enemyCount) break;
            if (map.GetEntityAt(pos.x, pos.y) != null) continue;

            float dist = Vector2Int.Distance(pos, map.entrancePos);
            float ratio = dist / maxDist;
            EnemyRank rank;
            if (ratio < 0.35f) rank = EnemyRank.Weak;
            else if (ratio < 0.7f) rank = EnemyRank.Medium;
            else rank = EnemyRank.Strong;

            map.entities.Add(new EntityPlacement(DungeonEntityType.Enemy, pos, rank));
            enemyPlaced++;
        }

        // 宝箱配置
        var treasureCandidates = new List<Vector2Int>(walkableTiles);
        ShuffleList(treasureCandidates);
        int treasurePlaced = 0;
        foreach (var pos in treasureCandidates)
        {
            if (treasurePlaced >= config.treasureCount) break;
            if (map.GetEntityAt(pos.x, pos.y) != null) continue;
            map.entities.Add(new EntityPlacement(DungeonEntityType.Treasure, pos));
            treasurePlaced++;
        }

        // イベント配置
        var eventCandidates = new List<Vector2Int>(walkableTiles);
        ShuffleList(eventCandidates);
        int eventPlaced = 0;
        foreach (var pos in eventCandidates)
        {
            if (eventPlaced >= config.eventCount) break;
            if (map.GetEntityAt(pos.x, pos.y) != null) continue;
            map.entities.Add(new EntityPlacement(DungeonEntityType.Event, pos));
            eventPlaced++;
        }
    }

    private List<Vector2Int> GetWalkableTiles(DungeonMap map)
    {
        var tiles = new List<Vector2Int>();
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                if (map.IsWalkable(x, y) &&
                    map.tiles[x, y] != DungeonTileType.BossRoom &&
                    map.tiles[x, y] != DungeonTileType.Entrance)
                {
                    tiles.Add(new Vector2Int(x, y));
                }
            }
        }
        return tiles;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
