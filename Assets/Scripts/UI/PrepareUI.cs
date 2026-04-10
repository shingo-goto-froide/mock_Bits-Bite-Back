using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PrepareUI : MonoBehaviour
{
    private enum PrepareTab { Formation, Craft, Disassemble }

    // 共通ヘッダー
    private TMP_Text waveText;
    private TMP_Text[] materialTexts;

    // タブパネル
    private GameObject formationTabContent;
    private GameObject craftTabContent;
    private GameObject disassembleTabContent;

    // タブボタン
    private Button formationTabButton;
    private Button craftTabButton;
    private Button disassembleTabButton;
    private Image formationTabImage;
    private Image craftTabImage;
    private Image disassembleTabImage;

    // 編成タブ
    private Transform monsterListContent;
    private GameObject monsterCardPrefab;
    private FormationSlotUI[] formationSlots;

    // 練成タブ
    private Transform craftListContent;
    private GameObject craftItemPrefab;

    // 分解タブ
    private Transform disassembleListContent;

    // 共通フッター
    private Button battleStartButton;
    private TMP_Text messageText;

    private GameManager gm;
    private MonsterInstance selectedMonster;
    private PrepareTab currentTab = PrepareTab.Formation;

    private static readonly Color TabActiveColor = new Color(0.35f, 0.45f, 0.65f);
    private static readonly Color TabInactiveColor = new Color(0.2f, 0.2f, 0.3f);

    private static readonly Dictionary<MaterialType, string> MaterialNames = new Dictionary<MaterialType, string> {
        { MaterialType.AnimalSkull, "頭蓋骨(動物)" }, { MaterialType.HumanSkull, "頭蓋骨(人)" },
        { MaterialType.LongWood, "長い木" }, { MaterialType.LongBone, "長い骨" }, { MaterialType.OldSword, "古い剣" }
    };

    private void Start()
    {
        gm = GameManager.Instance;
        if (gm == null) return;

        // Prefab
        monsterCardPrefab = Resources.Load<GameObject>("Prefabs/MonsterCard");
        craftItemPrefab = Resources.Load<GameObject>("Prefabs/CraftItem");

        // 共通ヘッダー
        waveText = FindText("WaveText");
        messageText = FindText("MessageText");

        var matTexts = new List<TMP_Text>();
        var matPanel = FindDeep(transform, "MaterialPanel");
        if (matPanel != null)
        {
            foreach (Transform child in matPanel)
            {
                var textChild = child.Find("Text");
                if (textChild != null)
                    matTexts.Add(textChild.GetComponent<TMP_Text>());
                else
                    matTexts.Add(child.GetComponent<TMP_Text>());
            }
        }
        materialTexts = matTexts.ToArray();

        // タブパネル
        formationTabContent = FindDeep(transform, "FormationTabContent")?.gameObject;
        craftTabContent = FindDeep(transform, "CraftTabContent")?.gameObject;
        disassembleTabContent = FindDeep(transform, "DisassembleTabContent")?.gameObject;

        // タブボタン
        formationTabButton = FindButton("FormationTabButton");
        craftTabButton = FindButton("CraftTabButton");
        disassembleTabButton = FindButton("DisassembleTabButton");
        formationTabImage = formationTabButton?.GetComponent<Image>();
        craftTabImage = craftTabButton?.GetComponent<Image>();
        disassembleTabImage = disassembleTabButton?.GetComponent<Image>();

        // 編成タブ内の参照
        var monsterSV = FindDeep(transform, "MonsterScrollView");
        if (monsterSV != null)
            monsterListContent = FindDeep(monsterSV, "Content");

        var slotList = new List<FormationSlotUI>();
        for (int i = 0; i < 5; i++)
        {
            var slotT = FindDeep(transform, $"FormationSlot_{i}");
            if (slotT != null)
            {
                var slot = slotT.GetComponent<FormationSlotUI>();
                if (slot != null) slotList.Add(slot);
            }
        }
        formationSlots = slotList.ToArray();

        // 練成タブ内の参照
        var craftSV = FindDeep(transform, "CraftScrollView");
        if (craftSV != null)
            craftListContent = FindDeep(craftSV, "Content");

        // 分解タブ内の参照
        var disassembleSV = FindDeep(transform, "DisassembleScrollView");
        if (disassembleSV != null)
            disassembleListContent = FindDeep(disassembleSV, "Content");

        // ボタンイベント
        battleStartButton = FindButton("BattleStartButton");
        if (battleStartButton != null) battleStartButton.onClick.AddListener(OnBattleStartClicked);
        if (formationTabButton != null) formationTabButton.onClick.AddListener(() => SwitchTab(PrepareTab.Formation));
        if (craftTabButton != null) craftTabButton.onClick.AddListener(() => SwitchTab(PrepareTab.Craft));
        if (disassembleTabButton != null) disassembleTabButton.onClick.AddListener(() => SwitchTab(PrepareTab.Disassemble));

        for (int i = 0; i < formationSlots.Length; i++)
        {
            int index = i;
            formationSlots[i].Init(index, OnFormationSlotClicked);
        }

        SwitchTab(PrepareTab.Formation);
        BuildDebugPanel();
    }

    // === デバッグパネル ===

    private GameObject debugPanel;

    private void BuildDebugPanel()
    {
        var canvas = FindDeep(transform.root, "PrepareCanvas") ?? transform;

        // DEBUGトグルボタン（右上）
        var btnGo = new GameObject("DebugToggle");
        btnGo.transform.SetParent(canvas, false);
        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1, 1);
        btnRt.anchorMax = new Vector2(1, 1);
        btnRt.pivot = new Vector2(1, 1);
        btnRt.anchoredPosition = new Vector2(-10, -10);
        btnRt.sizeDelta = new Vector2(100, 36);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        var btnComp = btnGo.AddComponent<Button>();
        var btnTxt = new GameObject("Text");
        btnTxt.transform.SetParent(btnGo.transform, false);
        var btrt = btnTxt.AddComponent<RectTransform>();
        btrt.anchorMin = Vector2.zero; btrt.anchorMax = Vector2.one;
        btrt.offsetMin = Vector2.zero; btrt.offsetMax = Vector2.zero;
        var btmp = btnTxt.AddComponent<TextMeshProUGUI>();
        btmp.text = "DEBUG"; btmp.fontSize = 18; btmp.fontStyle = FontStyles.Bold;
        btmp.color = Color.white; btmp.alignment = TextAlignmentOptions.Center;

        // パネル
        debugPanel = new GameObject("DebugPanel");
        debugPanel.transform.SetParent(canvas, false);
        var prt = debugPanel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(1, 1);
        prt.anchorMax = new Vector2(1, 1);
        prt.pivot = new Vector2(1, 1);
        prt.anchoredPosition = new Vector2(-10, -52);
        prt.sizeDelta = new Vector2(320, 200);
        debugPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        debugPanel.AddComponent<Outline>().effectColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);

        var vlg = debugPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // 行1: 素材MAX, 全魔物入手
        var row1 = CreateDebugRow(debugPanel.transform);
        CreateDebugButton(row1.transform, "素材MAX", () => { gm.DebugMaxMaterials(); RefreshAll(); });
        CreateDebugButton(row1.transform, "全魔物入手", () => { gm.DebugAddAllMonsters(); RefreshAll(); });

        // 行2: 全回復, 魔物クリア
        var row2 = CreateDebugRow(debugPanel.transform);
        CreateDebugButton(row2.transform, "全回復", () => { gm.DebugHealAll(); RefreshAll(); });
        CreateDebugButton(row2.transform, "魔物クリア", () => { gm.DebugClearMonsters(); RefreshAll(); });

        // 行3: Wave操作
        var row3 = CreateDebugRow(debugPanel.transform);
        CreateDebugButton(row3.transform, "Wave ▼", () => { gm.DebugSetWave(gm.CurrentWave - 1); RefreshDebugWaveText(); });
        CreateDebugButton(row3.transform, "Wave ▲", () => { gm.DebugSetWave(gm.CurrentWave + 1); RefreshDebugWaveText(); });

        // Wave表示
        var waveGo = new GameObject("WaveInfo");
        waveGo.transform.SetParent(debugPanel.transform, false);
        waveGo.AddComponent<RectTransform>();
        waveGo.AddComponent<LayoutElement>().preferredHeight = 28;
        debugWaveText = waveGo.AddComponent<TextMeshProUGUI>();
        debugWaveText.fontSize = 16; debugWaveText.color = Color.white;
        debugWaveText.alignment = TextAlignmentOptions.Center;
        RefreshDebugWaveText();

        debugPanel.SetActive(false);
        btnComp.onClick.AddListener(() => debugPanel.SetActive(!debugPanel.activeSelf));
    }

    private TMP_Text debugWaveText;

    private void RefreshDebugWaveText()
    {
        if (debugWaveText != null)
            debugWaveText.text = $"現在: Wave {gm.CurrentWave + 1} / {gm.EnemyWaves.Length}";
        // WaveBarも更新
        var waveText = FindDeep(transform, "WaveText")?.GetComponent<TMP_Text>();
        if (waveText != null)
            waveText.text = $"Wave {gm.CurrentWave + 1}";
    }

    private GameObject CreateDebugRow(Transform parent)
    {
        var go = new GameObject("Row");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<LayoutElement>().preferredHeight = 40;
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
        return go;
    }

    private void CreateDebugButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.35f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(action);

        var txt = new GameObject("Text");
        txt.transform.SetParent(go.transform, false);
        var trt = txt.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 16; tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
    }

    // === タブ切替 ===

    private void SwitchTab(PrepareTab tab)
    {
        currentTab = tab;
        selectedMonster = null;

        if (formationTabContent != null) formationTabContent.SetActive(tab == PrepareTab.Formation);
        if (craftTabContent != null) craftTabContent.SetActive(tab == PrepareTab.Craft);
        if (disassembleTabContent != null) disassembleTabContent.SetActive(tab == PrepareTab.Disassemble);

        UpdateTabButtonColors();
        RefreshHeader();

        switch (tab)
        {
            case PrepareTab.Formation:
                RefreshMonsterList();
                RefreshFormation();
                ShowMessage("魔物を選択して隊列スロットをクリックして配置");
                break;
            case PrepareTab.Craft:
                RefreshCraftList();
                ShowMessage("練成したい魔物を選択してください");
                break;
            case PrepareTab.Disassemble:
                RefreshDisassembleList();
                ShowMessage("分解したい魔物を選択してください");
                break;
        }
    }

    private void UpdateTabButtonColors()
    {
        if (formationTabImage != null) formationTabImage.color = currentTab == PrepareTab.Formation ? TabActiveColor : TabInactiveColor;
        if (craftTabImage != null) craftTabImage.color = currentTab == PrepareTab.Craft ? TabActiveColor : TabInactiveColor;
        if (disassembleTabImage != null) disassembleTabImage.color = currentTab == PrepareTab.Disassemble ? TabActiveColor : TabInactiveColor;
    }

    public void RefreshAll()
    {
        SwitchTab(currentTab);
    }

    // === ヘッダー ===

    private void RefreshHeader()
    {
        if (waveText != null)
            waveText.text = $"Wave {gm.CurrentWave + 1} / {gm.EnemyWaves.Length}";

        var materialTypes = new[] {
            MaterialType.AnimalSkull, MaterialType.HumanSkull,
            MaterialType.LongWood, MaterialType.LongBone, MaterialType.OldSword
        };
        var names = new[] { "頭蓋骨(動物)", "頭蓋骨(人)", "長い木", "長い骨", "古い剣" };

        for (int i = 0; i < materialTexts.Length && i < materialTypes.Length; i++)
        {
            int amount = gm.Inventory.GetAmount(materialTypes[i]);
            materialTexts[i].text = $"{names[i]}: {amount}";
        }
    }

    // === 編成タブ ===

    private void RefreshMonsterList()
    {
        if (monsterListContent == null) return;
        foreach (Transform child in monsterListContent)
            Destroy(child.gameObject);

        var formation = gm.Formation.GetFormation();

        foreach (var monster in gm.OwnedMonsters)
        {
            bool inFormation = false;
            foreach (var f in formation)
            {
                if (f.instanceId == monster.instanceId) { inFormation = true; break; }
            }

            var d = monster.baseData;
            var go = CreateCardBase(monsterListContent, "MonsterCard");
            // 編成中は背景色を変える
            if (inFormation)
                go.GetComponent<Image>().color = new Color(0.15f, 0.22f, 0.35f, 0.95f);
            // 選択中は枠を黄色に
            if (selectedMonster != null && selectedMonster.instanceId == monster.instanceId)
            {
                var ol = go.GetComponent<Outline>();
                if (ol != null)
                {
                    ol.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
                    ol.effectDistance = new Vector2(3, 3);
                }
            }

            PlaceImage(go.transform, d.monsterType);
            var info = PlaceInfo(go.transform);

            AddTmpLE(info.transform, "NameText", d.monsterName, 24, FontStyles.Bold, Color.white, 28, TextAlignmentOptions.Center);
            AddTmpLE(info.transform, "StatsText",
                $"HP:{monster.currentHp}/{monster.maxHp}  ATK:{monster.currentAttack}  SPD:{d.speed}  射程:{d.range}  枠:{d.slotSize}",
                18, FontStyles.Normal, new Color(0.8f, 0.8f, 0.9f), 22, TextAlignmentOptions.Center);
            AddTmpLE(info.transform, "AbilityText", d.abilityDescription,
                16, FontStyles.Italic, new Color(0.65f, 0.75f, 0.9f), 20, TextAlignmentOptions.Center);

            if (inFormation)
            {
                AddTmpLE(info.transform, "StatusText", "[ 編成中 ]",
                    16, FontStyles.Bold, new Color(0.4f, 0.7f, 1f), 22, TextAlignmentOptions.Center);
            }

            var capturedMonster = monster;
            go.GetComponent<Button>()?.onClick.AddListener(() => OnMonsterCardClicked(capturedMonster));
        }
    }

    private void RefreshFormation()
    {
        for (int i = 0; i < formationSlots.Length; i++)
        {
            var monster = gm.Formation.GetSlot(i);
            formationSlots[i].SetMonster(monster);
        }
    }

    // === 練成タブ ===

    private void RefreshCraftList()
    {
        if (craftListContent == null) return;
        foreach (Transform child in craftListContent)
            Destroy(child.gameObject);

        var allData = gm.Crafting.AllMonsterData;
        if (allData == null) return;

        foreach (var data in allData)
        {
            if (data.recipeMaterials == null) continue;
            bool canAfford = gm.Inventory.CanAfford(data.recipeMaterials);
            string recipe = FormatRecipe(data.recipeMaterials);
            var go = CreateCraftRow(craftListContent, data.monsterType, data.monsterName,
                $"HP:{data.baseHp}  ATK:{data.baseAttack}  SPD:{data.speed}  射程:{data.range}  枠:{data.slotSize}",
                data.abilityDescription, recipe);

            if (canAfford)
            {
                var capturedData = data;
                go.GetComponent<Button>()?.onClick.AddListener(() => OnCraftSelected(capturedData));
            }
            else
            {
                // グレーアウト
                var img = go.GetComponent<Image>();
                if (img != null) img.color = new Color(0.12f, 0.12f, 0.16f, 0.7f);
                // ボタン無効化
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
                // 子テキスト・画像を暗くする
                foreach (var tmp in go.GetComponentsInChildren<TMPro.TMP_Text>())
                    tmp.color = new Color(tmp.color.r * 0.5f, tmp.color.g * 0.5f, tmp.color.b * 0.5f, 0.6f);
                foreach (var childImg in go.GetComponentsInChildren<Image>())
                {
                    if (childImg == img) continue;
                    childImg.color = new Color(childImg.color.r * 0.5f, childImg.color.g * 0.5f, childImg.color.b * 0.5f, childImg.color.a * 0.6f);
                }
            }
        }
    }

    private string FormatRecipe(RecipeEntry[] recipe)
    {
        if (recipe == null) return "";
        var parts = new List<string>();
        foreach (var entry in recipe)
        {
            string name = MaterialNames.TryGetValue(entry.type, out string mn) ? mn : entry.type.ToString();
            parts.Add($"{name}×{entry.amount}");
        }
        return string.Join("  ", parts);
    }

    // === 分解タブ ===

    private void RefreshDisassembleList()
    {
        if (disassembleListContent == null) return;
        foreach (Transform child in disassembleListContent)
            Destroy(child.gameObject);

        var formation = gm.Formation.GetFormation();
        foreach (var monster in gm.OwnedMonsters)
        {
            bool inFormation = false;
            foreach (var f in formation)
            {
                if (f.instanceId == monster.instanceId) { inFormation = true; break; }
            }
            if (inFormation) continue;

            var result = gm.Crafting.GetDisassemblyResult(monster);
            string resultStr = "";
            foreach (var kvp in result)
            {
                string n = MaterialNames.TryGetValue(kvp.Key, out string mn) ? mn : kvp.Key.ToString();
                resultStr += $"{n}×{kvp.Value}\n";
            }

            var d = monster.baseData;
            var go = CreateDisassembleRow(disassembleListContent,
                d.monsterType, d.monsterName,
                $"HP:{d.baseHp}  ATK:{d.baseAttack}  SPD:{d.speed}  射程:{d.range}  枠:{d.slotSize}",
                d.abilityDescription,
                resultStr.TrimEnd('\n'));
            var capturedMonster = monster;
            go.GetComponent<Button>()?.onClick.AddListener(() => OnDisassembleSelected(capturedMonster));
        }

        if (disassembleListContent.childCount == 0)
            CreateEmptyRow(disassembleListContent, "分解できる魔物がありません");
    }

    // === 行UI生成ヘルパー ===
    // HLG不使用。画像=アンカー左寄せ固定、テキスト=右側アンカー配置。
    // GridLayoutGroupのcellSizeで外枠が統一されるため、内部はアンカーで安定。

    private const float ImgSize = 120f;
    private const float ImgMargin = 10f;

    private void PlaceImage(Transform parent, MonsterType monsterType = MonsterType.Skeleton)
    {
        var imgGo = new GameObject("ImagePlaceholder");
        imgGo.transform.SetParent(parent, false);
        var rt = imgGo.AddComponent<RectTransform>();
        // 左端に固定サイズで配置
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(ImgMargin + ImgSize * 0.5f, 0);
        rt.sizeDelta = new Vector2(ImgSize, ImgSize);
        imgGo.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.22f);

        var sprite = MonsterSpriteLoader.GetSprite(monsterType);
        if (sprite != null)
        {
            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(imgGo.transform, false);
            var srt = spriteGo.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            var sImg = spriteGo.AddComponent<Image>();
            sImg.sprite = sprite;
            sImg.preserveAspect = true;
            sImg.color = Color.white;
            sImg.raycastTarget = false;
            if (MonsterSpriteLoader.IsLeftFacing(monsterType))
                spriteGo.transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    private GameObject PlaceInfo(Transform parent)
    {
        var info = new GameObject("Info");
        info.transform.SetParent(parent, false);
        var rt = info.AddComponent<RectTransform>();
        // 画像の右側からRight端まで
        float left = ImgMargin + ImgSize + ImgMargin;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, 6);
        rt.offsetMax = new Vector2(-8, -6);
        var vlg = info.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 1; vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;
        return info;
    }

    private GameObject CreateCardBase(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.24f, 0.95f);
        go.AddComponent<Outline>().effectColor = new Color(0.35f, 0.35f, 0.55f, 0.6f);
        go.AddComponent<Button>();
        return go;
    }

    private GameObject CreateCraftRow(Transform parent, MonsterType monsterType, string monsterName, string stats, string ability, string recipe)
    {
        var go = CreateCardBase(parent, "CraftRow");
        PlaceImage(go.transform, monsterType);
        var info = PlaceInfo(go.transform);

        AddTmpLE(info.transform, "NameText", monsterName, 24, FontStyles.Bold, Color.white, 28, TextAlignmentOptions.Center);
        AddTmpLE(info.transform, "StatsText", stats, 18, FontStyles.Normal, new Color(0.8f, 0.8f, 0.9f), 22, TextAlignmentOptions.Center);
        AddTmpLE(info.transform, "AbilityText", ability, 16, FontStyles.Italic, new Color(0.65f, 0.75f, 0.9f), 20, TextAlignmentOptions.Center);

        var matGo = new GameObject("RecipeBg");
        matGo.transform.SetParent(info.transform, false);
        matGo.AddComponent<RectTransform>();
        matGo.AddComponent<Image>().color = new Color(0.12f, 0.2f, 0.3f, 0.8f);
        var matVLG = matGo.AddComponent<VerticalLayoutGroup>();
        matVLG.padding = new RectOffset(6, 6, 1, 1); matVLG.spacing = 0;
        matVLG.childControlWidth = true; matVLG.childControlHeight = true;
        matVLG.childForceExpandWidth = true;
        matGo.AddComponent<LayoutElement>().minHeight = 20;
        AddTmpChild(matGo.transform, "RecipeLabel", "<b>必要素材</b>", 16, TextAlignmentOptions.Center, new Color(0.9f, 0.85f, 0.5f));
        AddTmpChild(matGo.transform, "RecipeList", recipe, 16, TextAlignmentOptions.Center, new Color(0.95f, 0.9f, 0.7f));

        return go;
    }

    private GameObject CreateDisassembleRow(Transform parent, MonsterType monsterType, string monsterName, string stats, string ability, string materials)
    {
        var go = CreateCardBase(parent, "DisassembleRow");
        PlaceImage(go.transform, monsterType);
        var info = PlaceInfo(go.transform);

        AddTmpLE(info.transform, "NameText", monsterName, 24, FontStyles.Bold, Color.white, 28, TextAlignmentOptions.Center);
        AddTmpLE(info.transform, "StatsText", stats, 18, FontStyles.Normal, new Color(0.8f, 0.8f, 0.9f), 22, TextAlignmentOptions.Center);
        AddTmpLE(info.transform, "AbilityText", ability, 16, FontStyles.Italic, new Color(0.65f, 0.75f, 0.9f), 20, TextAlignmentOptions.Center);

        var matGo = new GameObject("MaterialBg");
        matGo.transform.SetParent(info.transform, false);
        matGo.AddComponent<RectTransform>();
        matGo.AddComponent<Image>().color = new Color(0.2f, 0.15f, 0.12f, 0.8f);
        var matVLG = matGo.AddComponent<VerticalLayoutGroup>();
        matVLG.padding = new RectOffset(6, 6, 1, 1); matVLG.spacing = 0;
        matVLG.childControlWidth = true; matVLG.childControlHeight = true;
        matVLG.childForceExpandWidth = true;
        matGo.AddComponent<LayoutElement>().minHeight = 20;
        AddTmpChild(matGo.transform, "MatLabel", "<b>返却素材</b>", 16, TextAlignmentOptions.Center, new Color(0.9f, 0.7f, 0.5f));
        AddTmpChild(matGo.transform, "MatList", materials, 16, TextAlignmentOptions.Center, new Color(0.95f, 0.9f, 0.7f));

        return go;
    }

    private void CreateEmptyRow(Transform parent, string message)
    {
        var go = new GameObject("EmptyRow");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.22f, 0.5f);
        AddTmpChild(go.transform, "Text", message, 20, TextAlignmentOptions.Center, Color.white);
    }

    private void AddTmpLE(Transform parent, string name, string text, float fontSize, FontStyles style, Color color, float minH, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<LayoutElement>().minHeight = minH;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
        tmp.fontStyle = style; tmp.richText = true;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.alignment = align;
    }

    private void AddTmpChild(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
        tmp.alignment = align; tmp.richText = true;
        tmp.textWrappingMode = TextWrappingModes.Normal;
    }

    // === イベントハンドラー ===

    private void OnCraftSelected(MonsterDataSO data)
    {
        var monster = gm.Crafting.Craft(data, gm.Inventory);
        if (monster != null)
        {
            gm.AddMonster(monster);
            ShowMessage($"{data.monsterName}を練成しました！");
            RefreshHeader();
            RefreshCraftList();
        }
    }

    private void OnDisassembleSelected(MonsterInstance monster)
    {
        gm.Crafting.Disassemble(monster, gm.Inventory);
        gm.RemoveMonster(monster);
        ShowMessage($"{monster.baseData.monsterName}を分解しました");
        RefreshHeader();
        RefreshDisassembleList();
    }

    private void OnMonsterCardClicked(MonsterInstance monster)
    {
        selectedMonster = monster;
        ShowMessage($"{monster.baseData.monsterName}を選択中。隊列スロットをクリックして配置");
        RefreshMonsterList();
    }

    private void OnFormationSlotClicked(int slotIndex)
    {
        var current = gm.Formation.GetSlot(slotIndex);

        if (current != null)
        {
            gm.Formation.RemoveMonster(slotIndex);
            ShowMessage($"{current.baseData.monsterName}を隊列から外しました");
            selectedMonster = null;
            RefreshMonsterList();
            RefreshFormation();
            return;
        }

        if (selectedMonster != null)
        {
            for (int i = 0; i < gm.Formation.SlotCount; i++)
            {
                var slot = gm.Formation.GetSlot(i);
                if (slot != null && slot.instanceId == selectedMonster.instanceId)
                {
                    gm.Formation.RemoveMonster(i);
                    break;
                }
            }

            if (gm.Formation.PlaceMonster(selectedMonster, slotIndex))
            {
                ShowMessage($"{selectedMonster.baseData.monsterName}をスロット{slotIndex + 1}に配置");
                selectedMonster = null;
            }
            else
            {
                ShowMessage("配置できません（枠不足または重複）");
            }
            RefreshMonsterList();
            RefreshFormation();
        }
        else
        {
            ShowMessage("魔物を選択してからスロットをクリックしてください");
        }
    }

    private void OnBattleStartClicked()
    {
        if (!gm.Formation.IsValidFormation())
        {
            ShowMessage("隊列に1体以上配置してください！");
            return;
        }
        SceneManager.LoadScene("DungeonScene");
    }

    // === ユーティリティ ===

    private void ShowMessage(string msg)
    {
        if (messageText != null)
            messageText.text = msg;
    }

    private TMP_Text FindText(string name)
    {
        var t = FindDeep(transform, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private Button FindButton(string name)
    {
        var t = FindDeep(transform, name);
        return t != null ? t.GetComponent<Button>() : null;
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
