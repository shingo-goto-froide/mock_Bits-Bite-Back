using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スプライトシートから各魔物のスプライトを切り出して提供する
/// レイアウト: 7列×2行、MonsterType enum順（左上から右へ）
/// </summary>
public static class MonsterSpriteLoader
{
    private const int Columns = 7;
    private const int Rows = 2;
    private static Dictionary<MonsterType, Sprite> cache;

    public static Sprite GetSprite(MonsterType type)
    {
        if (cache == null) LoadAll();
        cache.TryGetValue(type, out var sprite);
        return sprite;
    }

    /// <summary>
    /// スプライトシート上で左向き（←）かどうか
    /// 1-5（Skeleton〜ShadowWalker）は右向き、6-13（SkeletonPriest〜Revenger）は左向き
    /// </summary>
    public static bool IsLeftFacing(MonsterType type)
    {
        return (int)type >= (int)MonsterType.SkeletonPriest;
    }

    /// <summary>
    /// スプライトシートの切り出し位置のX方向オフセット（テクスチャピクセル単位）
    /// 正=右にずらす（隣の左端が映り込む場合）、負=左にずらす（隣の右端が映り込む場合）
    /// </summary>
    private static float GetCropOffset(MonsterType type)
    {
        switch (type)
        {
            case MonsterType.Skeleton:       return -40;
            case MonsterType.ShadowWalker:   return 40;
            case MonsterType.SkeletonPriest: return 40;
            case MonsterType.Orc:            return 80;
            case MonsterType.Martyr:         return 40;
            case MonsterType.BarrierMage:    return 40;
            case MonsterType.GraveKeeper:    return 55;
            case MonsterType.Revenger:       return 40;
            default:                         return 0;
        }
    }

    private static void LoadAll()
    {
        cache = new Dictionary<MonsterType, Sprite>();

        var tex = Resources.Load<Texture2D>("MonsterSpriteSheet");
        if (tex == null)
        {
            Debug.LogError("[MonsterSpriteLoader] MonsterSpriteSheet not found in Resources/");
            return;
        }

        // テクスチャの実サイズからセルサイズを自動計算
        float cellW = (float)tex.width / Columns;
        float cellH = (float)tex.height / Rows;

        for (int i = 0; i <= (int)MonsterType.Revenger; i++)
        {
            int col = i % Columns;
            int row = i / Columns;
            var monsterType = (MonsterType)i;

            // Unity座標系: 左下が原点。スプライトシートは左上が先頭なのでY反転
            float x = col * cellW + GetCropOffset(monsterType);
            float y = tex.height - (row + 1) * cellH;

            // テクスチャ境界を超えないようクランプ
            x = Mathf.Max(0f, x);
            y = Mathf.Max(0f, y);
            float w = Mathf.Min(cellW, tex.width - x);
            float h = Mathf.Min(cellH, tex.height - y);

            var rect = new Rect(x, y, w, h);
            var pivot = new Vector2(0.5f, 0.5f);
            var sprite = Sprite.Create(tex, rect, pivot, 100);
            sprite.name = monsterType.ToString();

            cache[monsterType] = sprite;
        }
    }
}
