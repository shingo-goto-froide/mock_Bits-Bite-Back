using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ダンジョン画面のUI管理
/// 15×15マップをSpriteRendererで描画（シンプル方式）
/// </summary>
public class DungeonUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private MinimapUI minimapUI;
    [SerializeField] private Transform partyHpPanel;
    [SerializeField] private TMP_Text materialText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private GameObject bossConfirmPanel;
    [SerializeField] private TMP_Text turnText;

    [Header("ダンジョン描画")]
    [SerializeField] private Camera mainCamera;

    private DungeonManager dungeonManager;
    private Transform tilesParent;
    private GameObject playerSprite;
    private SpriteRenderer[,] fogRenderers;
    private Sprite squareSprite;

    private readonly Color floorColor = new Color(0.45f, 0.4f, 0.32f);
    private readonly Color corridorColor = new Color(0.35f, 0.3f, 0.25f);
    private readonly Color wallColor = new Color(0.12f, 0.12f, 0.15f);
    private readonly Color entranceColor = new Color(0.25f, 0.55f, 0.75f);
    private readonly Color bossRoomColor = new Color(0.5f, 0.15f, 0.15f);
    private readonly Color fogColor = new Color(0.03f, 0.03f, 0.05f, 0.95f);
    private readonly Color playerColor = new Color(0.2f, 0.85f, 0.3f);

    private static readonly Dictionary<MaterialType, string> MaterialNames = new Dictionary<MaterialType, string> {
        { MaterialType.AnimalSkull, "頭蓋骨(動物)" }, { MaterialType.HumanSkull, "頭蓋骨(人)" },
        { MaterialType.LongWood, "長い木" }, { MaterialType.LongBone, "長い骨" }, { MaterialType.OldSword, "古い剣" }
    };

    private Coroutine messageCoroutine;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        squareSprite = CreateSquareSprite();

        SetupBossConfirmPanel();
        if (messagePanel != null) messagePanel.SetActive(false);
        if (bossConfirmPanel != null) bossConfirmPanel.SetActive(false);
    }

    private void Start()
    {
        dungeonManager = DungeonManager.Instance;
        if (dungeonManager == null)
        {
            Debug.LogError("[DungeonUI] DungeonManager が見つかりません");
            return;
        }

        dungeonManager.OnDungeonInitialized += OnDungeonInitialized;
        dungeonManager.OnPlayerMoved += OnPlayerMoved;
        dungeonManager.OnMessage += ShowMessage;
        dungeonManager.OnEntityInteracted += OnEntityInteracted;

        if (dungeonManager.Map != null)
        {
            OnDungeonInitialized();
            OnPlayerMoved(dungeonManager.PlayerPos);
        }
    }

    private void OnDestroy()
    {
        if (dungeonManager != null)
        {
            dungeonManager.OnDungeonInitialized -= OnDungeonInitialized;
            dungeonManager.OnPlayerMoved -= OnPlayerMoved;
            dungeonManager.OnMessage -= ShowMessage;
            dungeonManager.OnEntityInteracted -= OnEntityInteracted;
        }
    }

    private void OnDungeonInitialized()
    {
        BuildMap(dungeonManager.Map);
        if (minimapUI != null)
        {
            minimapUI.Initialize(dungeonManager.Map.width, dungeonManager.Map.height);
            minimapUI.UpdateMinimap(dungeonManager.Map, dungeonManager.PlayerPos);
        }
        UpdatePartyHp();
        UpdateMaterials();
    }

    private void OnPlayerMoved(Vector2Int pos)
    {
        UpdatePlayerPosition(pos);
        UpdateFogAroundPlayer(dungeonManager.Map, pos);
        UpdateEntityVisibility();
        ScrollCameraTo(pos);
        if (minimapUI != null)
            minimapUI.UpdateMinimap(dungeonManager.Map, pos);
        UpdatePartyHp();
        UpdateMaterials();
        UpdateTurnText();
    }

    private void OnEntityInteracted(EntityPlacement entity)
    {
        if (entity.type == DungeonEntityType.Boss)
            ShowBossConfirm();
    }

    // ===== マップ構築（1回だけ） =====

    private void BuildMap(DungeonMap map)
    {
        // 既存を破棄
        if (tilesParent != null)
            Destroy(tilesParent.gameObject);

        var parent = new GameObject("DungeonTiles");
        tilesParent = parent.transform;
        fogRenderers = new SpriteRenderer[map.width, map.height];
        float ts = DungeonManager.TileSize;
        float half = ts * 0.5f;

        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                Vector3 pos = new Vector3(x * ts + half, y * ts + half, 0);

                // タイル（静的、以後変更しない）
                var tileGo = new GameObject($"T{x}_{y}");
                tileGo.transform.SetParent(tilesParent);
                tileGo.transform.position = pos;
                var tileSR = tileGo.AddComponent<SpriteRenderer>();
                tileSR.sprite = squareSprite;
                tileSR.sortingOrder = 0;
                tileSR.color = GetTileColor(map.GetTile(x, y));

                // フォグ（探索で非表示にする）
                var fogGo = new GameObject($"F{x}_{y}");
                fogGo.transform.SetParent(tilesParent);
                fogGo.transform.position = new Vector3(pos.x, pos.y, -0.1f);
                var fogSR = fogGo.AddComponent<SpriteRenderer>();
                fogSR.sprite = squareSprite;
                fogSR.sortingOrder = 10;
                fogSR.color = fogColor;
                fogSR.enabled = !map.IsExplored(x, y);
                fogRenderers[x, y] = fogSR;
            }
        }

        // プレイヤー
        if (playerSprite != null) Destroy(playerSprite);
        playerSprite = new GameObject("Player");
        var psr = playerSprite.AddComponent<SpriteRenderer>();
        psr.sprite = squareSprite;
        psr.color = playerColor;
        psr.sortingOrder = 20;
        playerSprite.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        // 先頭魔物のスプライト
        var gm = GameManager.Instance;
        if (gm != null)
        {
            var formation = gm.Formation.GetFormation();
            if (formation.Count > 0)
            {
                var lead = formation[0];
                var ms = MonsterSpriteLoader.GetSprite(lead.baseData.monsterType);
                if (ms != null)
                {
                    psr.sprite = ms;
                    psr.color = Color.white;
                    float scale = ts / Mathf.Max(ms.bounds.size.x, ms.bounds.size.y) * 0.8f;
                    playerSprite.transform.localScale = new Vector3(
                        MonsterSpriteLoader.IsLeftFacing(lead.baseData.monsterType) ? -scale : scale,
                        scale, 1f);
                }
            }
        }

        Debug.Log($"[DungeonUI] マップ構築完了 {map.width}x{map.height}");
    }

    // ===== フォグ更新（プレイヤー周辺のみ） =====

    private void UpdateFogAroundPlayer(DungeonMap map, Vector2Int playerPos)
    {
        if (fogRenderers == null) return;
        int r = 4; // ExploreRadius + 余裕
        int x0 = Mathf.Max(0, playerPos.x - r);
        int y0 = Mathf.Max(0, playerPos.y - r);
        int x1 = Mathf.Min(map.width - 1, playerPos.x + r);
        int y1 = Mathf.Min(map.height - 1, playerPos.y + r);

        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                if (fogRenderers[x, y] != null && map.IsExplored(x, y))
                    fogRenderers[x, y].enabled = false;
            }
        }
    }

    // ===== 以下、UI更新系 =====

    private void UpdatePlayerPosition(Vector2Int pos)
    {
        if (playerSprite == null) return;
        float ts = DungeonManager.TileSize;
        float half = ts * 0.5f;
        playerSprite.transform.position = new Vector3(pos.x * ts + half, pos.y * ts + half, -0.2f);
    }

    private void UpdateEntityVisibility()
    {
        if (dungeonManager == null) return;
        foreach (var entity in dungeonManager.Entities)
        {
            if (entity == null) continue;
            entity.SetVisible(dungeonManager.Map.IsExplored(entity.gridPosition.x, entity.gridPosition.y));
        }
    }

    private void ScrollCameraTo(Vector2Int pos)
    {
        if (mainCamera == null) return;
        float ts = DungeonManager.TileSize;
        float half = ts * 0.5f;
        mainCamera.transform.position = new Vector3(pos.x * ts + half, pos.y * ts + half, mainCamera.transform.position.z);
    }

    private void UpdatePartyHp()
    {
        if (partyHpPanel == null) return;

        foreach (Transform child in partyHpPanel)
            Destroy(child.gameObject);

        var gm = GameManager.Instance;
        if (gm == null) return;
        var formation = gm.Formation.GetFormation();

        foreach (var monster in formation)
        {
            var go = new GameObject(monster.baseData.monsterName);
            go.transform.SetParent(partyHpPanel, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredWidth = 120;

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 1;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(go.transform, false);
            nameGo.AddComponent<RectTransform>();
            nameGo.AddComponent<LayoutElement>().preferredHeight = 16;
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = monster.baseData.monsterName;
            nameTmp.fontSize = 12;
            nameTmp.color = Color.white;
            nameTmp.alignment = TextAlignmentOptions.Center;

            var barBg = new GameObject("BarBg");
            barBg.transform.SetParent(go.transform, false);
            barBg.AddComponent<RectTransform>();
            barBg.AddComponent<LayoutElement>().preferredHeight = 8;
            barBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);

            var bar = new GameObject("Bar");
            bar.transform.SetParent(barBg.transform, false);
            var barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin = Vector2.zero;
            barRt.anchorMax = new Vector2(Mathf.Clamp01((float)monster.currentHp / monster.maxHp), 1f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;
            var barImg = bar.AddComponent<Image>();
            float hpRatio = (float)monster.currentHp / monster.maxHp;
            barImg.color = hpRatio > 0.5f ? new Color(0.2f, 0.8f, 0.3f) :
                           hpRatio > 0.25f ? new Color(0.9f, 0.7f, 0.1f) :
                           new Color(0.9f, 0.2f, 0.2f);

            var hpGo = new GameObject("Hp");
            hpGo.transform.SetParent(go.transform, false);
            hpGo.AddComponent<RectTransform>();
            hpGo.AddComponent<LayoutElement>().preferredHeight = 14;
            var hpTmp = hpGo.AddComponent<TextMeshProUGUI>();
            hpTmp.text = $"{monster.currentHp}/{monster.maxHp}";
            hpTmp.fontSize = 10;
            hpTmp.color = new Color(0.8f, 0.8f, 0.9f);
            hpTmp.alignment = TextAlignmentOptions.Center;
        }
    }

    private void UpdateMaterials()
    {
        if (materialText == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        var parts = new List<string>();
        foreach (var t in new[] { MaterialType.AnimalSkull, MaterialType.HumanSkull, MaterialType.LongWood, MaterialType.LongBone, MaterialType.OldSword })
        {
            int amount = gm.Inventory.GetAmount(t);
            if (amount > 0)
            {
                string name = MaterialNames.TryGetValue(t, out string n) ? n : t.ToString();
                parts.Add($"{name}:{amount}");
            }
        }
        materialText.text = parts.Count > 0 ? string.Join("  ", parts) : "素材なし";
    }

    private void UpdateTurnText()
    {
        if (turnText != null && dungeonManager != null)
            turnText.text = $"ターン {dungeonManager.TurnCount}";
    }

    public void ShowMessage(string text)
    {
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(ShowMessageCoroutine(text));
    }

    private IEnumerator ShowMessageCoroutine(string text)
    {
        if (messagePanel != null) messagePanel.SetActive(true);
        if (messageText != null) messageText.text = text;
        yield return new WaitForSeconds(2.5f);
        if (messagePanel != null) messagePanel.SetActive(false);
        messageCoroutine = null;
    }

    public void ShowBossConfirm()
    {
        if (bossConfirmPanel != null) bossConfirmPanel.SetActive(true);
    }

    private void SetupBossConfirmPanel()
    {
        if (bossConfirmPanel == null) return;
        var confirm = FindDeep(bossConfirmPanel.transform, "BossConfirmButton");
        if (confirm != null)
        {
            var btn = confirm.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => {
                bossConfirmPanel.SetActive(false);
                DungeonManager.Instance?.ConfirmBossBattle();
            });
        }
        var cancel = FindDeep(bossConfirmPanel.transform, "BossCancelButton");
        if (cancel != null)
        {
            var btn = cancel.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => {
                bossConfirmPanel.SetActive(false);
                DungeonManager.Instance?.CancelBossEncounter();
            });
        }
    }

    private Color GetTileColor(DungeonTileType type)
    {
        return type switch
        {
            DungeonTileType.Floor => floorColor,
            DungeonTileType.Corridor => corridorColor,
            DungeonTileType.Entrance => entranceColor,
            DungeonTileType.BossRoom => bossRoomColor,
            _ => wallColor
        };
    }

    private Sprite CreateSquareSprite()
    {
        var tex = new Texture2D(16, 16);
        var px = new Color[256];
        for (int i = 0; i < 256; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
