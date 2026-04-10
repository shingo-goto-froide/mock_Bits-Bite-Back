using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PrepareUI : MonoBehaviour
{
    private enum PrepareTab { Formation, Craft, Disassemble, Soul }

    // 共通ヘッダー
    private TMP_Text waveText;
    private TMP_Text[] materialTexts;

    // タブパネル
    private GameObject formationTabContent;
    private GameObject craftTabContent;
    private GameObject disassembleTabContent;
    private GameObject soulTabContent;

    // タブボタン
    private Button formationTabButton;
    private Button craftTabButton;
    private Button disassembleTabButton;
    private Button soulTabButton;
    private Image formationTabImage;
    private Image craftTabImage;
    private Image disassembleTabImage;
    private Image soulTabImage;

    // 魂タブ
    private Transform soulListContent;

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

        // 所持魂タブを動的生成（分解タブボタンの隣に追加）
        BuildSoulTab();

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
        prt.sizeDelta = new Vector2(320, 280);
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

        // 行3: 触媒MAX, 全魂入手
        var row3 = CreateDebugRow(debugPanel.transform);
        CreateDebugButton(row3.transform, "触媒MAX", () => { gm.DebugMaxCatalysts(); RefreshAll(); });
        CreateDebugButton(row3.transform, "全魂入手", () => { gm.DebugAddAllSouls(); RefreshAll(); });

        // 行3.5: 全要素MAX
        var row35 = CreateDebugRow(debugPanel.transform);
        CreateDebugButton(row35.transform, "全要素MAX", () => { gm.DebugMaxAll(); RefreshAll(); });
        CreateDebugButton(row35.transform, "---", null);

        // 行4: 階層操作
        var row4 = CreateDebugRow(debugPanel.transform);
        CreateDebugButton(row4.transform, "階層 -", () => { gm.DebugSetFloor(gm.CurrentFloor - 1); RefreshDebugFloorText(); RefreshHeader(); });
        CreateDebugButton(row4.transform, "階層 +", () => { gm.DebugSetFloor(gm.CurrentFloor + 1); RefreshDebugFloorText(); RefreshHeader(); });

        // 階層表示
        var floorGo = new GameObject("FloorInfo");
        floorGo.transform.SetParent(debugPanel.transform, false);
        floorGo.AddComponent<RectTransform>();
        floorGo.AddComponent<LayoutElement>().preferredHeight = 28;
        debugFloorText = floorGo.AddComponent<TextMeshProUGUI>();
        debugFloorText.fontSize = 16; debugFloorText.color = Color.white;
        debugFloorText.alignment = TextAlignmentOptions.Center;
        RefreshDebugFloorText();

        debugPanel.SetActive(false);
        btnComp.onClick.AddListener(() => debugPanel.SetActive(!debugPanel.activeSelf));
    }

    private TMP_Text debugFloorText;

    private void RefreshDebugFloorText()
    {
        if (debugFloorText != null)
            debugFloorText.text = $"現在: 階層 {gm.CurrentFloor}";
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
        if (action != null) btn.onClick.AddListener(action);
        else btn.interactable = false;

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
        if (soulTabContent != null) soulTabContent.SetActive(tab == PrepareTab.Soul);

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
            case PrepareTab.Soul:
                RefreshSoulList();
                ShowMessage("所持している魂の一覧");
                break;
        }
    }

    private void UpdateTabButtonColors()
    {
        if (formationTabImage != null) formationTabImage.color = currentTab == PrepareTab.Formation ? TabActiveColor : TabInactiveColor;
        if (craftTabImage != null) craftTabImage.color = currentTab == PrepareTab.Craft ? TabActiveColor : TabInactiveColor;
        if (disassembleTabImage != null) disassembleTabImage.color = currentTab == PrepareTab.Disassemble ? TabActiveColor : TabInactiveColor;
        if (soulTabImage != null) soulTabImage.color = currentTab == PrepareTab.Soul ? TabActiveColor : TabInactiveColor;
    }

    public void RefreshAll()
    {
        SwitchTab(currentTab);
    }

    // === ヘッダー ===

    private enum FooterTab { Material, Catalyst }
    private FooterTab currentFooterTab;
    private GameObject catalystPanel;
    private Button footerMatTabBtn;
    private Button footerCatTabBtn;
    private Image footerMatTabImg;
    private Image footerCatTabImg;

    private void RefreshHeader()
    {
        if (waveText != null)
            waveText.text = $"階層 {gm.CurrentFloor}";

        BuildFooterTabs();
        RefreshFooterContent();
    }

    private void BuildFooterTabs()
    {
        if (footerMatTabBtn != null) return; // 既に構築済み

        // HeaderPanel（所持素材バー）を探す
        var headerPanel = FindDeep(transform, "HeaderPanel");
        if (headerPanel == null) return;
        var footerPanel = headerPanel.parent;

        // MaterialLabel を非表示
        var matLabel = FindDeep(transform, "MaterialLabel");
        if (matLabel != null) matLabel.gameObject.SetActive(false);

        // タブボタンを HeaderPanel の上に直接配置（RectTransformで絶対位置指定）
        var headerRt = headerPanel.GetComponent<RectTransform>();

        footerMatTabBtn = CreateFooterTabButtonAbsolute(footerPanel, "素材", 0, headerRt, out footerMatTabImg);
        footerCatTabBtn = CreateFooterTabButtonAbsolute(footerPanel, "触媒", 245, headerRt, out footerCatTabImg);
        footerMatTabBtn.onClick.AddListener(() => { currentFooterTab = FooterTab.Material; RefreshFooterContent(); });
        footerCatTabBtn.onClick.AddListener(() => { currentFooterTab = FooterTab.Catalyst; RefreshFooterContent(); });

        // 触媒パネルを MaterialPanel と同じ階層に生成（初期非表示）
        var matPanel = FindDeep(transform, "MaterialPanel");
        catalystPanel = new GameObject("CatalystPanel");
        catalystPanel.transform.SetParent(matPanel != null ? matPanel.parent : headerPanel, false);
        catalystPanel.AddComponent<RectTransform>();
        // MaterialPanel と同じアンカー・位置にする
        var cpRt = catalystPanel.GetComponent<RectTransform>();
        if (matPanel != null)
        {
            var matRt = matPanel.GetComponent<RectTransform>();
            cpRt.anchorMin = matRt.anchorMin;
            cpRt.anchorMax = matRt.anchorMax;
            cpRt.offsetMin = matRt.offsetMin;
            cpRt.offsetMax = matRt.offsetMax;
            cpRt.pivot = matRt.pivot;
            cpRt.anchoredPosition = matRt.anchoredPosition;
        }
        var cpHlg = catalystPanel.AddComponent<HorizontalLayoutGroup>();
        cpHlg.spacing = 15;
        cpHlg.padding = new RectOffset(10, 10, 0, 0);
        cpHlg.childControlWidth = false; cpHlg.childControlHeight = true;
        cpHlg.childForceExpandWidth = false; cpHlg.childForceExpandHeight = true;
        cpHlg.childAlignment = TextAnchor.MiddleLeft;
        catalystPanel.SetActive(false);

        // 素材パネルも左詰めに
        if (matPanel != null)
        {
            var matHlg = matPanel.GetComponent<HorizontalLayoutGroup>();
            if (matHlg != null)
            {
                matHlg.childAlignment = TextAnchor.MiddleLeft;
                matHlg.childForceExpandWidth = false;
            }
        }
    }

    private Button CreateFooterTabButtonAbsolute(Transform parent, string label, float xOffset, RectTransform headerRt, out Image img)
    {
        var go = new GameObject($"FooterTab_{label}");
        go.transform.SetParent(headerRt.parent, false); // HeaderPanelの親に配置
        var rt = go.AddComponent<RectTransform>();
        // HeaderPanel の左上の上に配置
        rt.anchorMin = new Vector2(headerRt.anchorMin.x, headerRt.anchorMax.y);
        rt.anchorMax = new Vector2(headerRt.anchorMin.x, headerRt.anchorMax.y);
        rt.pivot = new Vector2(0, 0); // 左下ピボット
        rt.anchoredPosition = new Vector2(xOffset + 5, 2);
        rt.sizeDelta = new Vector2(240, 60);

        img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tRt = textGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 24; tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private Button CreateFooterTabButton(Transform parent, string label, float width, out Image img)
    {
        var go = new GameObject($"FooterTab_{label}");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth = width;
        img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tRt = textGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 14; tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private void RefreshFooterContent()
    {
        var matPanel = FindDeep(transform, "MaterialPanel");

        // タブ色
        Color active = new Color(0.3f, 0.35f, 0.5f);
        Color inactive = new Color(0.15f, 0.18f, 0.25f);
        if (footerMatTabImg != null) footerMatTabImg.color = currentFooterTab == FooterTab.Material ? active : inactive;
        if (footerCatTabImg != null) footerCatTabImg.color = currentFooterTab == FooterTab.Catalyst ? active : inactive;
        // 全パネル非表示
        if (matPanel != null) matPanel.gameObject.SetActive(false);
        if (catalystPanel != null) catalystPanel.SetActive(false);

        switch (currentFooterTab)
        {
            case FooterTab.Material:
                if (matPanel != null) matPanel.gameObject.SetActive(true);
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
                break;

            case FooterTab.Catalyst:
                if (catalystPanel != null)
                {
                    catalystPanel.SetActive(true);
                    foreach (Transform child in catalystPanel.transform)
                        Destroy(child.gameObject);
                    AddCatalystItem(catalystPanel.transform, "古い剣", gm.Inventory.CatalystCount);
                }
                break;

        }
    }

    private void AddCatalystItem(Transform parent, string name, int count)
    {
        // 素材と完全に同じ構造: LayoutElement(210x) + 子Icon(Image 0.25,0.25,0.35) + 子Text(TMP 20pt 白)
        var go = new GameObject($"CatalystText_{name}");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 210;
        le.minWidth = 200;

        // Icon（素材と同一: 28×28, anchor中央左, pos(4,0), pivot(0,0.5)）
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(4, 0);
        iconRt.sizeDelta = new Vector2(28, 28);
        iconGo.AddComponent<CanvasRenderer>();
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = new Color(0.25f, 0.25f, 0.35f);

        // Text（素材と同一: anchor(0,0)-(1,1), offsetMin(38,0), fontSize=20, 白, overflow）
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tRt = textGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(38, 0);
        tRt.offsetMax = Vector2.zero;
        textGo.AddComponent<CanvasRenderer>();
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = $"{name}: {count}";
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TMPro.TextOverflowModes.Overflow;
    }

    // === 編成タブ ===

    private void RefreshMonsterList()
    {
        if (monsterListContent == null) return;
        foreach (Transform child in monsterListContent)
            Destroy(child.gameObject);

        var formation = gm.Formation.GetFormation();

        // コスト順でソート
        var sortedMonsters = new System.Collections.Generic.List<MonsterInstance>(gm.OwnedMonsters);
        sortedMonsters.Sort((a, b) =>
        {
            int costA = GetRecipeCost(a.baseData);
            int costB = GetRecipeCost(b.baseData);
            if (costA != costB) return costA.CompareTo(costB);
            if (a.baseData.slotSize != b.baseData.slotSize) return a.baseData.slotSize.CompareTo(b.baseData.slotSize);
            return a.baseData.monsterType.CompareTo(b.baseData.monsterType);
        });

        foreach (var monster in sortedMonsters)
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

            var rankLabel = monster.GetRankLabel();
            var rankColor = monster.GetRankColor();
            AddTmpLE(info.transform, "NameText", $"{d.monsterName} <color=#{ColorUtility.ToHtmlStringRGB(rankColor)}>[{rankLabel}]</color>", 24, FontStyles.Bold, Color.white, 28, TextAlignmentOptions.Center);

            // 装備込みのステータス表示
            var eff = monster.GetEffectiveStats();
            AddTmpLE(info.transform, "StatsText",
                $"HP:{monster.currentHp}/{eff.hp}  ATK:{eff.atk}  SPD:{eff.spd}  射程:{eff.range}  枠:{d.slotSize}  {monster.age}年",
                18, FontStyles.Normal, new Color(0.8f, 0.8f, 0.9f), 22, TextAlignmentOptions.Center);
            AddTmpLE(info.transform, "AbilityText", d.abilityDescription,
                16, FontStyles.Italic, new Color(0.65f, 0.75f, 0.9f), 20, TextAlignmentOptions.Center);

            // 装備中の魂を表示
            if (monster.HasSoulEquipped())
            {
                var soul = monster.equippedSoul;
                var tc = soul.GetTypeColor();
                var src = MonsterInstance.GetRankColor(soul.rank);
                AddTmpLE(info.transform, "SoulText",
                    $"魂: <color=#{ColorUtility.ToHtmlStringRGB(tc)}>{soul.GetName()}</color> <color=#{ColorUtility.ToHtmlStringRGB(src)}>[{soul.rank}]</color> ({soul.GetEffectDescription()})",
                    15, FontStyles.Normal, new Color(0.85f, 0.75f, 1f), 20, TextAlignmentOptions.Center);
            }

            if (inFormation)
            {
                AddTmpLE(info.transform, "StatusText", "[ 編成中 ]",
                    16, FontStyles.Bold, new Color(0.4f, 0.7f, 1f), 22, TextAlignmentOptions.Center);
            }

            // 魂装備ボタン（カード下部にアンカー配置、VLG外）
            {
                float btnH = 28f;
                float btnW = 120f;
                float btnY = 14f;
                // info領域の中央（画像の右端〜カード右端の中間）を基準に
                float infoLeft = ImgMargin + ImgSize + ImgMargin;
                bool hasAvailable = GetAvailableSouls().Count > 0;
                bool hasEquipped = monster.HasSoulEquipped();
                float totalW = 0;
                if (hasAvailable) totalW += btnW;
                if (hasEquipped) { if (totalW > 0) totalW += 6; totalW += btnW; }
                // anchor(0.5,0)基準なので、info中央へのオフセットを計算
                float infoCenterOffset = infoLeft * 0.5f;
                float startX = infoCenterOffset - totalW * 0.5f;

                if (hasAvailable)
                {
                    var capturedForEquip = monster;
                    PlaceAnchorButton(go.transform, "魂装備", new Color(0.4f, 0.3f, 0.55f, 0.9f),
                        startX, btnY, btnW, btnH, () => ShowSoulSelectDialog(capturedForEquip));
                    startX += btnW + 6;
                }
                if (hasEquipped)
                {
                    var capturedForUnequip = monster;
                    PlaceAnchorButton(go.transform, "魂を外す", new Color(0.45f, 0.35f, 0.35f, 0.9f),
                        startX, btnY, btnW, btnH, () => { capturedForUnequip.UnequipSoul(); RefreshMonsterList(); RefreshFormation(); RefreshHeader(); });
                }
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

        // コスト順（安い→高い）でソート。同コストなら枠数が少ない順
        var sorted = new System.Collections.Generic.List<MonsterDataSO>(allData);
        sorted.Sort((a, b) =>
        {
            int costA = GetRecipeCost(a);
            int costB = GetRecipeCost(b);
            if (costA != costB) return costA.CompareTo(costB);
            if (a.slotSize != b.slotSize) return a.slotSize.CompareTo(b.slotSize);
            return a.monsterType.CompareTo(b.monsterType);
        });

        foreach (var data in sorted)
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

    private static int GetRecipeCost(MonsterDataSO data)
    {
        if (data == null || data.recipeMaterials == null) return 0;
        int cost = 0;
        foreach (var entry in data.recipeMaterials)
            cost += entry.amount;
        return cost;
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

        // コスト順でソート
        var sortedMonsters = new System.Collections.Generic.List<MonsterInstance>(gm.OwnedMonsters);
        sortedMonsters.Sort((a, b) =>
        {
            int costA = GetRecipeCost(a.baseData);
            int costB = GetRecipeCost(b.baseData);
            if (costA != costB) return costA.CompareTo(costB);
            if (a.baseData.slotSize != b.baseData.slotSize) return a.baseData.slotSize.CompareTo(b.baseData.slotSize);
            return a.baseData.monsterType.CompareTo(b.baseData.monsterType);
        });

        foreach (var monster in sortedMonsters)
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
            var rankLabel = monster.GetRankLabel();
            var rankColor = monster.GetRankColor();
            var eff = monster.GetEffectiveStats();
            var go = CreateDisassembleRow(disassembleListContent,
                d.monsterType,
                $"{d.monsterName} <color=#{ColorUtility.ToHtmlStringRGB(rankColor)}>[{rankLabel}]</color>",
                $"HP:{eff.hp}  ATK:{eff.atk}  SPD:{eff.spd}  射程:{eff.range}  枠:{d.slotSize}  {monster.age}年",
                d.abilityDescription,
                resultStr.TrimEnd('\n'));
            var capturedMonster = monster;
            go.GetComponent<Button>()?.onClick.AddListener(() => OnDisassembleSelected(capturedMonster));
        }

        if (gm.OwnedMonsters.Count <= 1)
        {
            foreach (Transform child in disassembleListContent)
                Destroy(child.gameObject);
            CreateEmptyRow(disassembleListContent, "魔物が1体のみのため分解できません");
        }
        else if (disassembleListContent.childCount == 0)
            CreateEmptyRow(disassembleListContent, "分解できる魔物がありません");
    }

    // === 所持魂タブ ===

    private void BuildSoulTab()
    {
        // DisassembleTabContent と同じ親にコンテンツを動的生成
        var parent = disassembleTabContent != null ? disassembleTabContent.transform.parent : transform;

        // タブコンテンツ
        soulTabContent = new GameObject("SoulTabContent");
        soulTabContent.transform.SetParent(parent, false);
        var rt = soulTabContent.AddComponent<RectTransform>();
        // DisassembleTabContent と同じサイズにする
        if (disassembleTabContent != null)
        {
            var srcRt = disassembleTabContent.GetComponent<RectTransform>();
            rt.anchorMin = srcRt.anchorMin;
            rt.anchorMax = srcRt.anchorMax;
            rt.offsetMin = srcRt.offsetMin;
            rt.offsetMax = srcRt.offsetMax;
            rt.pivot = srcRt.pivot;
        }
        else
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        // ScrollView
        var svGo = new GameObject("SoulScrollView");
        svGo.transform.SetParent(soulTabContent.transform, false);
        var svRt = svGo.AddComponent<RectTransform>();
        svRt.anchorMin = Vector2.zero; svRt.anchorMax = Vector2.one;
        svRt.offsetMin = Vector2.zero; svRt.offsetMax = Vector2.zero;
        var sv = svGo.AddComponent<ScrollRect>();
        svGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f); // マスク用
        svGo.AddComponent<Mask>().showMaskGraphic = false;

        // Viewport
        var vpGo = new GameObject("Viewport");
        vpGo.transform.SetParent(svGo.transform, false);
        var vpRt = vpGo.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
        vpGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        vpGo.AddComponent<Mask>().showMaskGraphic = false;

        // Content (GridLayoutGroup)
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(vpGo.transform, false);
        var cRt = contentGo.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0, 1); cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot = new Vector2(0.5f, 1);
        cRt.offsetMin = new Vector2(0, 0); cRt.offsetMax = new Vector2(0, 0);
        var glg = contentGo.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(500, 100);
        glg.spacing = new Vector2(12, 8);
        glg.padding = new RectOffset(10, 10, 10, 10);
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.constraint = GridLayoutGroup.Constraint.Flexible;
        glg.childAlignment = TextAnchor.UpperCenter;
        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sv.content = cRt;
        sv.viewport = vpRt;
        sv.horizontal = false;
        sv.vertical = true;

        soulListContent = contentGo.transform;
        soulTabContent.SetActive(false);

        // タブボタンを分解タブボタンの隣に動的追加
        if (disassembleTabButton != null)
        {
            var tabParent = disassembleTabButton.transform.parent;
            var srcBtnRt = disassembleTabButton.GetComponent<RectTransform>();

            var btnGo = new GameObject("SoulTabButton");
            btnGo.transform.SetParent(tabParent, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            // 分解ボタンの右隣に配置
            btnRt.anchorMin = srcBtnRt.anchorMin;
            btnRt.anchorMax = srcBtnRt.anchorMax;
            btnRt.sizeDelta = srcBtnRt.sizeDelta;
            btnRt.pivot = srcBtnRt.pivot;
            btnRt.anchoredPosition = srcBtnRt.anchoredPosition + new Vector2(srcBtnRt.sizeDelta.x + 8, 0);

            soulTabImage = btnGo.AddComponent<Image>();
            soulTabImage.color = TabInactiveColor;
            soulTabButton = btnGo.AddComponent<Button>();
            soulTabButton.onClick.AddListener(() => SwitchTab(PrepareTab.Soul));

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            var tRt = txtGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "所持魂";
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }

    private void RefreshSoulList()
    {
        if (soulListContent == null) return;
        foreach (Transform child in soulListContent)
            Destroy(child.gameObject);

        var allSouls = gm.Inventory.GetAllSouls();
        if (allSouls.Count == 0)
        {
            CreateEmptyRow(soulListContent, "魂を所持していません");
            return;
        }

        // ランク高い順 → 種類順でソート
        var sorted = new System.Collections.Generic.List<SoulData>(allSouls);
        sorted.Sort((a, b) =>
        {
            int rc = b.rank.CompareTo(a.rank);
            if (rc != 0) return rc;
            return a.type.CompareTo(b.type);
        });

        foreach (var soul in sorted)
            CreateSoulCard(soulListContent, soul);
    }

    private void CreateSoulCard(Transform parent, SoulData soul)
    {
        var go = new GameObject($"SoulCard_{soul.type}_{soul.rank}");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        var rankColor = MonsterInstance.GetRankColor(soul.rank);
        var typeColor = soul.GetTypeColor();
        go.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.22f, 0.95f);
        go.AddComponent<Outline>().effectColor = new Color(typeColor.r * 0.5f, typeColor.g * 0.5f, typeColor.b * 0.5f, 0.8f);

        // 左: 魂アイコン（種類色のオーブ）
        var iconGo = new GameObject("SoulIcon");
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(50, 0);
        iconRt.sizeDelta = new Vector2(70, 70);
        iconGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.18f);

        // オーブ（種類の色）
        var orbGo = new GameObject("Orb");
        orbGo.transform.SetParent(iconGo.transform, false);
        var orbRt = orbGo.AddComponent<RectTransform>();
        orbRt.anchorMin = new Vector2(0.15f, 0.15f); orbRt.anchorMax = new Vector2(0.85f, 0.85f);
        orbRt.offsetMin = Vector2.zero; orbRt.offsetMax = Vector2.zero;
        orbGo.AddComponent<Image>().color = typeColor;

        // 右: テキスト情報
        var info = new GameObject("Info");
        info.transform.SetParent(go.transform, false);
        var infoRt = info.AddComponent<RectTransform>();
        infoRt.anchorMin = Vector2.zero; infoRt.anchorMax = Vector2.one;
        infoRt.offsetMin = new Vector2(100, 6);
        infoRt.offsetMax = new Vector2(-8, -6);
        var vlg = info.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2; vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleLeft;

        // 魂名 + ランク
        string rankHex = ColorUtility.ToHtmlStringRGB(rankColor);
        AddTmpLE(info.transform, "NameText",
            $"{soul.GetName()} <color=#{rankHex}>[{soul.rank}]</color>",
            22, FontStyles.Bold, Color.white, 28, TextAlignmentOptions.Left);

        // 効果
        string typeHex = ColorUtility.ToHtmlStringRGB(typeColor);
        AddTmpLE(info.transform, "EffectText",
            $"<color=#{typeHex}>{soul.GetEffectDescription()}</color>",
            17, FontStyles.Normal, new Color(0.8f, 0.85f, 0.9f), 22, TextAlignmentOptions.Left);
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
        ShowCraftDialog(data);
    }

    private void DoCraft(MonsterDataSO data, bool useCatalyst)
    {
        var monster = gm.Crafting.Craft(data, gm.Inventory, useCatalyst);
        if (monster != null)
        {
            gm.AddMonster(monster);
            ShowCraftResultDialog(monster, useCatalyst);
        }
    }

    private void ShowCraftResultDialog(MonsterInstance monster, bool usedCatalyst)
    {
        var canvas = FindDeep(transform.root, "PrepareCanvas") ?? transform;
        var craftResultOverlay = new GameObject("CraftResultOverlay");
        craftResultOverlay.transform.SetParent(canvas, false);
        var oRt = craftResultOverlay.AddComponent<RectTransform>();
        oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one;
        oRt.offsetMin = Vector2.zero; oRt.offsetMax = Vector2.zero;
        craftResultOverlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        var panel = new GameObject("Panel");
        panel.transform.SetParent(craftResultOverlay.transform, false);
        var pRt = panel.AddComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.18f, 0.18f);
        pRt.anchorMax = new Vector2(0.82f, 0.82f);
        pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5; vlg.padding = new RectOffset(25, 25, 15, 65);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        // 「練成完了！」
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        titleGo.AddComponent<RectTransform>();
        titleGo.AddComponent<LayoutElement>().preferredHeight = 50;
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "練成完了！";
        titleTmp.fontSize = 48; titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;

        // 横並び行（左:画像、右:テキスト情報）
        var row = new GameObject("ContentRow");
        row.transform.SetParent(panel.transform, false);
        row.AddComponent<RectTransform>();
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 160;
        rowLe.minHeight = 160;
        var rowHlg = row.AddComponent<HorizontalLayoutGroup>();
        rowHlg.spacing = 20;
        rowHlg.padding = new RectOffset(20, 20, 10, 10);
        rowHlg.childControlWidth = true; rowHlg.childControlHeight = true;
        rowHlg.childForceExpandWidth = true; rowHlg.childForceExpandHeight = true;
        rowHlg.childAlignment = TextAnchor.MiddleCenter;

        // 左: 魔物画像
        var imgContainer = new GameObject("ImageContainer");
        imgContainer.transform.SetParent(row.transform, false);
        imgContainer.AddComponent<RectTransform>();
        var imgLe = imgContainer.AddComponent<LayoutElement>();
        imgLe.preferredWidth = 110;
        imgLe.flexibleWidth = 0;
        imgContainer.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f);

        var sprite = MonsterSpriteLoader.GetSprite(monster.baseData.monsterType);
        if (sprite != null)
        {
            var sprGo = new GameObject("Sprite");
            sprGo.transform.SetParent(imgContainer.transform, false);
            var srt = sprGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.05f); srt.anchorMax = new Vector2(0.95f, 0.95f);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            var sImg = sprGo.AddComponent<Image>();
            sImg.sprite = sprite;
            sImg.preserveAspect = true;
            sImg.raycastTarget = false;
        }

        // 右: テキスト情報
        var infoContainer = new GameObject("InfoContainer");
        infoContainer.transform.SetParent(row.transform, false);
        infoContainer.AddComponent<RectTransform>();
        infoContainer.AddComponent<LayoutElement>().flexibleWidth = 1;
        var infoVlg = infoContainer.AddComponent<VerticalLayoutGroup>();
        infoVlg.spacing = 8;
        infoVlg.padding = new RectOffset(10, 10, 10, 10);
        infoVlg.childControlWidth = true; infoVlg.childControlHeight = false;
        infoVlg.childForceExpandWidth = true; infoVlg.childForceExpandHeight = false;
        infoVlg.childAlignment = TextAnchor.UpperLeft;

        // ランク
        var rankGo = new GameObject("Rank");
        rankGo.transform.SetParent(infoContainer.transform, false);
        rankGo.AddComponent<RectTransform>();
        rankGo.AddComponent<LayoutElement>().preferredHeight = 65;
        var rankTmp = rankGo.AddComponent<TextMeshProUGUI>();
        rankTmp.text = $"ランク {monster.GetRankLabel()}";
        rankTmp.fontSize = 38; rankTmp.fontStyle = FontStyles.Bold;
        rankTmp.color = monster.GetRankColor();
        rankTmp.alignment = TextAlignmentOptions.Left;

        // 魔物名
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(infoContainer.transform, false);
        nameGo.AddComponent<RectTransform>();
        nameGo.AddComponent<LayoutElement>().preferredHeight = 40;
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text = monster.baseData.monsterName;
        nameTmp.fontSize = 32; nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = Color.white;
        nameTmp.alignment = TextAlignmentOptions.Left;

        // ステータス
        float mult = MonsterInstance.GetRankMultiplier(monster.rank);
        var statsGo = new GameObject("Stats");
        statsGo.transform.SetParent(infoContainer.transform, false);
        statsGo.AddComponent<RectTransform>();
        statsGo.AddComponent<LayoutElement>().preferredHeight = 30;
        var statsTmp = statsGo.AddComponent<TextMeshProUGUI>();
        statsTmp.text = $"HP:{monster.maxHp}  ATK:{monster.currentAttack}  （×{mult:F2}）";
        statsTmp.fontSize = 22;
        statsTmp.color = new Color(0.8f, 0.8f, 0.9f);
        statsTmp.alignment = TextAlignmentOptions.Left;

        // 能力説明
        var abilGo = new GameObject("Ability");
        abilGo.transform.SetParent(infoContainer.transform, false);
        abilGo.AddComponent<RectTransform>();
        abilGo.AddComponent<LayoutElement>().preferredHeight = 50;
        var abilTmp = abilGo.AddComponent<TextMeshProUGUI>();
        abilTmp.text = monster.baseData.abilityDescription;
        abilTmp.fontSize = 18;
        abilTmp.color = new Color(0.65f, 0.75f, 0.9f);
        abilTmp.alignment = TextAlignmentOptions.Left;
        abilTmp.enableWordWrapping = true;

        if (usedCatalyst)
        {
            var catGo = new GameObject("Catalyst");
            catGo.transform.SetParent(infoContainer.transform, false);
            catGo.AddComponent<RectTransform>();
            catGo.AddComponent<LayoutElement>().preferredHeight = 25;
            var catTmp = catGo.AddComponent<TextMeshProUGUI>();
            catTmp.text = "触媒使用";
            catTmp.fontSize = 18;
            catTmp.color = new Color(1f, 0.85f, 0.2f);
            catTmp.alignment = TextAlignmentOptions.Left;
        }

        // OKボタン（overlay上にパネル下部基準で配置）
        var okGo = new GameObject("OKBtn");
        okGo.transform.SetParent(craftResultOverlay.transform, false);
        var okRt = okGo.AddComponent<RectTransform>();
        okRt.anchorMin = new Vector2(0.5f, 0.18f); okRt.anchorMax = new Vector2(0.5f, 0.18f);
        okRt.pivot = new Vector2(0.5f, 0);
        okRt.anchoredPosition = new Vector2(0, 15);
        okRt.sizeDelta = new Vector2(180, 45);
        okGo.AddComponent<Image>().color = new Color(0.3f, 0.45f, 0.6f);
        okGo.AddComponent<Button>().onClick.AddListener(() => {
            Destroy(craftResultOverlay);
            RefreshHeader();
            RefreshCraftList();
            RefreshMonsterList();
        });
        AddAnchorBtnText(okGo.transform, "OK");
    }

    private GameObject craftOverlay;

    private void ShowCraftDialog(MonsterDataSO data)
    {
        if (craftOverlay != null) Destroy(craftOverlay);

        var canvas = FindDeep(transform.root, "PrepareCanvas") ?? transform;
        craftOverlay = new GameObject("CraftOverlay");
        craftOverlay.transform.SetParent(canvas, false);
        var oRt = craftOverlay.AddComponent<RectTransform>();
        oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one;
        oRt.offsetMin = Vector2.zero; oRt.offsetMax = Vector2.zero;
        craftOverlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        var panel = new GameObject("Panel");
        panel.transform.SetParent(craftOverlay.transform, false);
        var pRt = panel.AddComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.22f, 0.2f);
        pRt.anchorMax = new Vector2(0.78f, 0.8f);
        pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.15f, 0.17f, 0.25f, 0.95f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8; vlg.padding = new RectOffset(25, 25, 30, 25);
        vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        // タイトル
        var tGo = new GameObject("Title");
        tGo.transform.SetParent(panel.transform, false);
        tGo.AddComponent<RectTransform>();
        tGo.AddComponent<LayoutElement>().preferredHeight = 45;
        var tTmp = tGo.AddComponent<TextMeshProUGUI>();
        tTmp.text = $"{data.monsterName} を練成しますか？";
        tTmp.fontSize = 32; tTmp.fontStyle = FontStyles.Bold;
        tTmp.color = Color.white;
        tTmp.alignment = TextAlignmentOptions.Center;

        // ランク説明
        var dGo = new GameObject("Desc");
        dGo.transform.SetParent(panel.transform, false);
        dGo.AddComponent<RectTransform>();
        dGo.AddComponent<LayoutElement>().preferredHeight = 45;
        var dTmp = dGo.AddComponent<TextMeshProUGUI>();
        dTmp.text = "練成するとランクがランダムで決まります\n<color=#FFE080>触媒を使うとS・A・Bランクが出やすくなります</color>";
        dTmp.fontSize = 22; dTmp.color = new Color(0.8f, 0.8f, 0.9f);
        dTmp.alignment = TextAlignmentOptions.Center;
        dTmp.richText = true;

        // スペーサー（説明とボタンの間）
        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(panel.transform, false);
        spacer.AddComponent<RectTransform>();
        spacer.AddComponent<LayoutElement>().preferredHeight = 15;

        // ボタン列（縦並び）
        var row = panel.transform; // パネル自体のVLGに直接追加

        // 練成ボタン
        CreateSimpleButton(row, "練成", new Color(0.3f, 0.55f, 0.3f), () => {
            Destroy(craftOverlay);
            DoCraft(data, false);
        });

        // 触媒ボタン（常に表示、所持なしはグレーアウト）
        bool hasCatalyst = gm.Inventory.CatalystCount > 0;
        string catText = hasCatalyst
            ? $"触媒で練成（残{gm.Inventory.CatalystCount}個）"
            : "触媒で練成（所持なし）";
        var catBtn = CreateSimpleButton(row, catText,
            hasCatalyst ? new Color(0.75f, 0.5f, 0.15f) : new Color(0.25f, 0.25f, 0.3f), () => {
            if (!hasCatalyst) return;
            Destroy(craftOverlay);
            DoCraft(data, true);
        });
        if (!hasCatalyst)
        {
            var catTmp = catBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (catTmp != null) catTmp.color = new Color(0.5f, 0.5f, 0.55f);
        }

        // キャンセルボタン
        CreateSimpleButton(row, "キャンセル", new Color(0.35f, 0.35f, 0.4f), () => {
            Destroy(craftOverlay);
        });
    }

    private GameObject CreateSimpleButton(Transform parent, string text, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 45;
        le.minHeight = 45;
        go.AddComponent<Image>().color = color;
        go.AddComponent<Button>().onClick.AddListener(onClick);
        var tGo = new GameObject("Text");
        tGo.transform.SetParent(go.transform, false);
        var tRt = tGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(5, 5); tRt.offsetMax = new Vector2(-5, -5);
        var tmp = tGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 24; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.richText = true;
        return go;
    }

    private void OnDisassembleSelected(MonsterInstance monster)
    {
        ShowDisassembleConfirmDialog(monster);
    }

    private void ShowDisassembleConfirmDialog(MonsterInstance monster)
    {
        var canvas = FindDeep(transform.root, "PrepareCanvas") ?? transform;
        var overlay = new GameObject("DisassembleConfirmOverlay");
        overlay.transform.SetParent(canvas, false);
        var oRt = overlay.AddComponent<RectTransform>();
        oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one;
        oRt.offsetMin = Vector2.zero; oRt.offsetMax = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        var panel = new GameObject("Panel");
        panel.transform.SetParent(overlay.transform, false);
        var pRt = panel.AddComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.15f, 0.2f);
        pRt.anchorMax = new Vector2(0.85f, 0.8f);
        pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8; vlg.padding = new RectOffset(25, 25, 20, 15);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        // タイトル
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        titleGo.AddComponent<RectTransform>();
        titleGo.AddComponent<LayoutElement>().preferredHeight = 50;
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "分解しますか？";
        titleTmp.fontSize = 32; titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;

        // 横並び行（左:画像、右:情報）
        var row = new GameObject("ContentRow");
        row.transform.SetParent(panel.transform, false);
        row.AddComponent<RectTransform>();
        row.AddComponent<LayoutElement>().preferredHeight = 250;
        var rowHlg = row.AddComponent<HorizontalLayoutGroup>();
        rowHlg.spacing = 25;
        rowHlg.padding = new RectOffset(30, 30, 10, 10);
        rowHlg.childControlWidth = true; rowHlg.childControlHeight = true;
        rowHlg.childForceExpandWidth = true; rowHlg.childForceExpandHeight = true;
        rowHlg.childAlignment = TextAnchor.MiddleCenter;

        // 左: 魔物画像
        var imgContainer = new GameObject("ImageContainer");
        imgContainer.transform.SetParent(row.transform, false);
        imgContainer.AddComponent<RectTransform>();
        var imgLe = imgContainer.AddComponent<LayoutElement>();
        imgLe.preferredWidth = 200;
        imgLe.flexibleWidth = 0;
        imgContainer.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f);

        var sprite = MonsterSpriteLoader.GetSprite(monster.baseData.monsterType);
        if (sprite != null)
        {
            var sprGo = new GameObject("Sprite");
            sprGo.transform.SetParent(imgContainer.transform, false);
            var srt = sprGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.05f); srt.anchorMax = new Vector2(0.95f, 0.95f);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            var sImg = sprGo.AddComponent<Image>();
            sImg.sprite = sprite;
            sImg.preserveAspect = true;
            sImg.raycastTarget = false;
        }

        // 右: テキスト情報
        var infoContainer = new GameObject("InfoContainer");
        infoContainer.transform.SetParent(row.transform, false);
        infoContainer.AddComponent<RectTransform>();
        infoContainer.AddComponent<LayoutElement>().flexibleWidth = 1;
        var infoVlg = infoContainer.AddComponent<VerticalLayoutGroup>();
        infoVlg.spacing = 6;
        infoVlg.padding = new RectOffset(10, 10, 10, 10);
        infoVlg.childControlWidth = true; infoVlg.childControlHeight = false;
        infoVlg.childForceExpandWidth = true; infoVlg.childForceExpandHeight = false;
        infoVlg.childAlignment = TextAnchor.UpperLeft;

        // 魔物名+ランク
        var d = monster.baseData;
        var rc = monster.GetRankColor();
        AddDialogText(infoContainer.transform, $"{d.monsterName} <color=#{ColorUtility.ToHtmlStringRGB(rc)}>[{monster.GetRankLabel()}]</color>", 26, FontStyles.Bold, Color.white, 35);

        // ステータス
        AddDialogText(infoContainer.transform, $"HP:{monster.currentHp}/{monster.maxHp}  ATK:{monster.currentAttack}", 18, FontStyles.Normal, new Color(0.8f, 0.8f, 0.9f), 25);

        // 返却素材タイトル
        AddDialogText(infoContainer.transform, "返却される素材：", 18, FontStyles.Normal, new Color(0.7f, 0.8f, 0.6f), 25);

        // 返却素材リスト
        var result = gm.Crafting.GetDisassemblyResult(monster);
        foreach (var kvp in result)
        {
            string matName = MaterialNames.TryGetValue(kvp.Key, out string mn) ? mn : kvp.Key.ToString();
            AddDialogText(infoContainer.transform, $"  {matName} ×{kvp.Value}", 18, FontStyles.Normal, new Color(0.85f, 0.85f, 0.9f), 22);
        }

        // 獲得魂
        var soulRank = gm.Crafting.GetSoulRankPreview(monster);
        var soulColor = MonsterInstance.GetRankColor(soulRank);
        AddDialogText(infoContainer.transform, $"獲得魂: <color=#{ColorUtility.ToHtmlStringRGB(soulColor)}>{soulRank}ランク</color>（{monster.age}年）", 18, FontStyles.Bold, new Color(0.9f, 0.8f, 1f), 25);

        // ボタン行
        var btnRow = new GameObject("BtnRow");
        btnRow.transform.SetParent(panel.transform, false);
        btnRow.AddComponent<RectTransform>();
        btnRow.AddComponent<LayoutElement>().preferredHeight = 45;
        var btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHlg.spacing = 20;
        btnHlg.childControlWidth = true; btnHlg.childControlHeight = true;
        btnHlg.childForceExpandWidth = true; btnHlg.childForceExpandHeight = true;

        CreateSimpleButton(btnRow.transform, "分解する", new Color(0.7f, 0.3f, 0.3f), () => {
            Destroy(overlay);
            // 装備中の魂を解放
            if (monster.HasSoulEquipped())
                monster.UnequipSoul();
            var resultMats = gm.Crafting.GetDisassemblyResult(monster);
            gm.Crafting.Disassemble(monster, gm.Inventory);
            gm.RemoveMonster(monster);
            ShowDisassembleResultDialog(monster, resultMats);
        });

        CreateSimpleButton(btnRow.transform, "キャンセル", new Color(0.35f, 0.35f, 0.4f), () => {
            Destroy(overlay);
        });
    }

    // === 魂付与フロー ===

    private void PlaceAnchorButton(Transform parent, string text, Color color, float x, float y, float w, float h, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("AncBtn");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        go.AddComponent<Image>().color = color;
        go.AddComponent<Button>().onClick.AddListener(onClick);
        var tGo = new GameObject("Text");
        tGo.transform.SetParent(go.transform, false);
        var tRt = tGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var tmp = tGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 16; tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
    }

    private void AddSmallButton(Transform parent, string text, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("SmBtn");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 100; le.preferredHeight = 18; le.minHeight = 18; le.flexibleHeight = 0;
        go.AddComponent<Image>().color = color;
        go.AddComponent<Button>().onClick.AddListener(onClick);
        var tGo = new GameObject("Text");
        tGo.transform.SetParent(go.transform, false);
        var tRt = tGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var tmp = tGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 11; tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
    }

    /// <summary>他の魔物に装備されていない魂のリストを返す</summary>
    private System.Collections.Generic.List<SoulData> GetAvailableSouls()
    {
        var allSouls = gm.Inventory.GetAllSouls();
        var equipped = new System.Collections.Generic.HashSet<int>();
        foreach (var m in gm.OwnedMonsters)
            if (m.equippedSoul != null) equipped.Add(m.equippedSoul.id);
        var available = new System.Collections.Generic.List<SoulData>();
        foreach (var s in allSouls)
            if (!equipped.Contains(s.id)) available.Add(s);
        return available;
    }

    private void ShowSoulSelectDialog(MonsterInstance monster)
    {
        var canvas = FindDeep(transform.root, "PrepareCanvas") ?? transform;
        var overlay = new GameObject("SoulSelectOverlay");
        overlay.transform.SetParent(canvas, false);
        var oRt = overlay.AddComponent<RectTransform>();
        oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one;
        oRt.offsetMin = Vector2.zero; oRt.offsetMax = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        var panel = new GameObject("Panel");
        panel.transform.SetParent(overlay.transform, false);
        var pRt = panel.AddComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.1f, 0.1f);
        pRt.anchorMax = new Vector2(0.9f, 0.9f);
        pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8; vlg.padding = new RectOffset(20, 20, 15, 15);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        // タイトル
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        titleGo.AddComponent<RectTransform>();
        titleGo.AddComponent<LayoutElement>().preferredHeight = 40;
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        var rc = monster.GetRankColor();
        titleTmp.text = $"{monster.baseData.monsterName} <color=#{ColorUtility.ToHtmlStringRGB(rc)}>[{monster.GetRankLabel()}]</color> に装備する魂を選択";
        titleTmp.fontSize = 24; titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white; titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.richText = true;

        // スクロール可能な魂リスト
        var svGo = new GameObject("ScrollView");
        svGo.transform.SetParent(panel.transform, false);
        svGo.AddComponent<RectTransform>();
        svGo.AddComponent<LayoutElement>().flexibleHeight = 1;
        var sv = svGo.AddComponent<ScrollRect>();
        svGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        svGo.AddComponent<Mask>().showMaskGraphic = false;

        var vpGo = new GameObject("Viewport");
        vpGo.transform.SetParent(svGo.transform, false);
        var vpRt = vpGo.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
        vpGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        vpGo.AddComponent<Mask>().showMaskGraphic = false;

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(vpGo.transform, false);
        var cRt = contentGo.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0, 1); cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot = new Vector2(0.5f, 1);
        var glg = contentGo.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(340, 65);
        glg.spacing = new Vector2(8, 6);
        glg.padding = new RectOffset(10, 10, 5, 5);
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.constraint = GridLayoutGroup.Constraint.Flexible;
        glg.childAlignment = TextAnchor.UpperCenter;
        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sv.content = cRt;
        sv.viewport = vpRt;
        sv.horizontal = false; sv.vertical = true;

        // 装備可能な魂のみ並べる
        var sorted = new System.Collections.Generic.List<SoulData>(GetAvailableSouls());
        sorted.Sort((a, b) =>
        {
            int rc2 = b.rank.CompareTo(a.rank);
            if (rc2 != 0) return rc2;
            return a.type.CompareTo(b.type);
        });

        foreach (var soul in sorted)
        {
            var card = new GameObject($"Soul_{soul.type}_{soul.rank}");
            card.transform.SetParent(contentGo.transform, false);
            card.AddComponent<RectTransform>();
            var typeColor = soul.GetTypeColor();
            card.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.24f, 0.95f);
            card.AddComponent<Outline>().effectColor = new Color(typeColor.r * 0.4f, typeColor.g * 0.4f, typeColor.b * 0.4f, 0.8f);

            // アイコン
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(card.transform, false);
            var iRt = iconGo.AddComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0, 0.5f); iRt.anchorMax = new Vector2(0, 0.5f);
            iRt.pivot = new Vector2(0.5f, 0.5f);
            iRt.anchoredPosition = new Vector2(35, 0); iRt.sizeDelta = new Vector2(50, 50);
            iconGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.18f);
            var orb = new GameObject("Orb");
            orb.transform.SetParent(iconGo.transform, false);
            var oRt2 = orb.AddComponent<RectTransform>();
            oRt2.anchorMin = new Vector2(0.15f, 0.15f); oRt2.anchorMax = new Vector2(0.85f, 0.85f);
            oRt2.offsetMin = Vector2.zero; oRt2.offsetMax = Vector2.zero;
            orb.AddComponent<Image>().color = typeColor;

            // テキスト
            var infoGo = new GameObject("Info");
            infoGo.transform.SetParent(card.transform, false);
            var infRt = infoGo.AddComponent<RectTransform>();
            infRt.anchorMin = Vector2.zero; infRt.anchorMax = Vector2.one;
            infRt.offsetMin = new Vector2(70, 4); infRt.offsetMax = new Vector2(-8, -4);
            var ivlg = infoGo.AddComponent<VerticalLayoutGroup>();
            ivlg.spacing = 1; ivlg.childControlWidth = true; ivlg.childControlHeight = true;
            ivlg.childForceExpandWidth = true; ivlg.childForceExpandHeight = false;
            ivlg.childAlignment = TextAnchor.MiddleLeft;

            var rankColor = MonsterInstance.GetRankColor(soul.rank);
            AddTmpLE(infoGo.transform, "Name",
                $"{soul.GetName()} <color=#{ColorUtility.ToHtmlStringRGB(rankColor)}>[{soul.rank}]</color>",
                20, FontStyles.Bold, Color.white, 24, TextAlignmentOptions.Left);
            string tHex = ColorUtility.ToHtmlStringRGB(typeColor);
            AddTmpLE(infoGo.transform, "Effect",
                $"<color=#{tHex}>{soul.GetEffectDescription()}</color>",
                15, FontStyles.Normal, new Color(0.8f, 0.85f, 0.9f), 20, TextAlignmentOptions.Left);

            var capturedSoul = soul;
            var btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                Destroy(overlay);
                ShowSoulConfirmDialog(monster, capturedSoul);
            });
        }

        // キャンセルボタン
        CreateSimpleButton(panel.transform, "キャンセル", new Color(0.35f, 0.35f, 0.4f), () => Destroy(overlay));
    }

    private void ShowSoulConfirmDialog(MonsterInstance monster, SoulData soul)
    {
        var canvas = FindDeep(transform.root, "PrepareCanvas") ?? transform;
        var overlay = new GameObject("SoulConfirmOverlay");
        overlay.transform.SetParent(canvas, false);
        var oRt = overlay.AddComponent<RectTransform>();
        oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one;
        oRt.offsetMin = Vector2.zero; oRt.offsetMax = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        var panel = new GameObject("Panel");
        panel.transform.SetParent(overlay.transform, false);
        var pRt = panel.AddComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.18f, 0.25f);
        pRt.anchorMax = new Vector2(0.82f, 0.75f);
        pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8; vlg.padding = new RectOffset(25, 25, 20, 70);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        // タイトル
        AddCenterText(panel.transform, "魂を装備しますか？", 30, FontStyles.Bold, Color.white, 38);

        // 魂情報
        var typeColor = soul.GetTypeColor();
        var rankColor = MonsterInstance.GetRankColor(soul.rank);
        string soulName = $"{soul.GetName()} <color=#{ColorUtility.ToHtmlStringRGB(rankColor)}>[{soul.rank}]</color>";
        AddCenterText(panel.transform, soulName, 24, FontStyles.Bold, Color.white, 30);
        AddCenterText(panel.transform, $"<color=#{ColorUtility.ToHtmlStringRGB(typeColor)}>{soul.GetEffectDescription()}</color>",
            20, FontStyles.Normal, new Color(0.8f, 0.85f, 0.9f), 26);

        // 区切り線
        var sep = new GameObject("Sep");
        sep.transform.SetParent(panel.transform, false);
        sep.AddComponent<RectTransform>();
        sep.AddComponent<LayoutElement>().preferredHeight = 2;
        sep.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.4f);

        // ステータス変化プレビュー
        var preview = monster.PreviewSoul(soul);
        AddCenterText(panel.transform, $"{monster.baseData.monsterName} のステータス変化", 20, FontStyles.Bold, new Color(0.9f, 0.85f, 0.6f), 28);

        string hpStr = FormatStatChange("HP", monster.maxHp, preview.hp);
        string atkStr = FormatStatChange("ATK", monster.currentAttack, preview.atk);
        string spdStr = FormatStatChange("SPD", monster.currentSpeed, preview.spd);
        string rngStr = FormatStatChange("射程", monster.currentRange, preview.range);
        AddCenterText(panel.transform, $"{hpStr}    {atkStr}", 20, FontStyles.Normal, Color.white, 26);
        AddCenterText(panel.transform, $"{spdStr}    {rngStr}", 20, FontStyles.Normal, Color.white, 26);

        // ボタン（overlay上にパネル下部基準で配置）
        float dbW = 250f, dbH = 55f, dbGap = 30f;
        var equipBtnGo = new GameObject("EquipBtn");
        equipBtnGo.transform.SetParent(overlay.transform, false);
        var eqRt = equipBtnGo.AddComponent<RectTransform>();
        eqRt.anchorMin = new Vector2(0.5f, 0.25f); eqRt.anchorMax = new Vector2(0.5f, 0.25f);
        eqRt.pivot = new Vector2(1, 0);
        eqRt.anchoredPosition = new Vector2(-dbGap * 0.5f, 30);
        eqRt.sizeDelta = new Vector2(dbW, dbH);
        equipBtnGo.AddComponent<Image>().color = new Color(0.4f, 0.3f, 0.6f);
        equipBtnGo.AddComponent<Button>().onClick.AddListener(() =>
        {
            monster.UnequipSoul();
            monster.EquipSoul(soul);
            Destroy(overlay);
            RefreshMonsterList();
            RefreshFormation();
            RefreshHeader();
            ShowMessage($"{monster.baseData.monsterName} に {soul.GetName()} を装備しました！");
        });
        AddAnchorBtnText(equipBtnGo.transform, "装備する");

        var cancelBtnGo = new GameObject("CancelBtn");
        cancelBtnGo.transform.SetParent(overlay.transform, false);
        var ccRt = cancelBtnGo.AddComponent<RectTransform>();
        ccRt.anchorMin = new Vector2(0.5f, 0.25f); ccRt.anchorMax = new Vector2(0.5f, 0.25f);
        ccRt.pivot = new Vector2(0, 0);
        ccRt.anchoredPosition = new Vector2(dbGap * 0.5f, 30);
        ccRt.sizeDelta = new Vector2(dbW, dbH);
        cancelBtnGo.AddComponent<Image>().color = new Color(0.35f, 0.35f, 0.4f);
        cancelBtnGo.AddComponent<Button>().onClick.AddListener(() => Destroy(overlay));
        AddAnchorBtnText(cancelBtnGo.transform, "キャンセル");
    }

    private void AddAnchorBtnText(Transform parent, string text)
    {
        var tGo = new GameObject("Text");
        tGo.transform.SetParent(parent, false);
        var tRt = tGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var tmp = tGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 28; tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateDialogButton(Transform parent, string text, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("DlgBtn");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 45; le.minHeight = 45;
        go.AddComponent<Image>().color = color;
        go.AddComponent<Button>().onClick.AddListener(onClick);
        var tGo = new GameObject("Text");
        tGo.transform.SetParent(go.transform, false);
        var tRt = tGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var tmp = tGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 28; tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private string FormatStatChange(string label, int before, int after)
    {
        if (before == after) return $"{label}: {before}";
        int diff = after - before;
        string sign = diff > 0 ? "+" : "";
        return $"{label}: {before} → <color=#66FF88>{after}</color> (<color=#66FF88>{sign}{diff}</color>)";
    }

    private void AddCenterText(Transform parent, string text, float fontSize, FontStyles style, Color color, float height)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<LayoutElement>().preferredHeight = height;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.richText = true;
    }

    private void AddDialogText(Transform parent, string text, float fontSize, FontStyles style, Color color, float height)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<LayoutElement>().preferredHeight = height;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.richText = true;
    }

    private void ShowDisassembleResultDialog(MonsterInstance monster, System.Collections.Generic.Dictionary<MaterialType, int> result)
    {
        var canvas = FindDeep(transform.root, "PrepareCanvas") ?? transform;
        var overlay = new GameObject("DisassembleResultOverlay");
        overlay.transform.SetParent(canvas, false);
        var oRt = overlay.AddComponent<RectTransform>();
        oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one;
        oRt.offsetMin = Vector2.zero; oRt.offsetMax = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        var panel = new GameObject("Panel");
        panel.transform.SetParent(overlay.transform, false);
        var pRt = panel.AddComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.15f, 0.25f);
        pRt.anchorMax = new Vector2(0.85f, 0.75f);
        pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8; vlg.padding = new RectOffset(25, 25, 20, 15);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        // タイトル
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        titleGo.AddComponent<RectTransform>();
        titleGo.AddComponent<LayoutElement>().preferredHeight = 50;
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "分解完了！";
        titleTmp.fontSize = 48; titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;

        // 横並び行（左:画像、右:情報）
        var row = new GameObject("ContentRow");
        row.transform.SetParent(panel.transform, false);
        row.AddComponent<RectTransform>();
        row.AddComponent<LayoutElement>().preferredHeight = 200;
        var rowHlg = row.AddComponent<HorizontalLayoutGroup>();
        rowHlg.spacing = 25;
        rowHlg.padding = new RectOffset(30, 30, 10, 10);
        rowHlg.childControlWidth = true; rowHlg.childControlHeight = true;
        rowHlg.childForceExpandWidth = true; rowHlg.childForceExpandHeight = true;
        rowHlg.childAlignment = TextAnchor.MiddleCenter;

        // 左: 魔物画像
        var imgContainer = new GameObject("ImageContainer");
        imgContainer.transform.SetParent(row.transform, false);
        imgContainer.AddComponent<RectTransform>();
        var imgLe = imgContainer.AddComponent<LayoutElement>();
        imgLe.preferredWidth = 180;
        imgLe.flexibleWidth = 0;
        imgContainer.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f);

        var sprite = MonsterSpriteLoader.GetSprite(monster.baseData.monsterType);
        if (sprite != null)
        {
            var sprGo = new GameObject("Sprite");
            sprGo.transform.SetParent(imgContainer.transform, false);
            var srt = sprGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.05f); srt.anchorMax = new Vector2(0.95f, 0.95f);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            var sImg = sprGo.AddComponent<Image>();
            sImg.sprite = sprite;
            sImg.preserveAspect = true;
            sImg.raycastTarget = false;
        }

        // 右: テキスト情報
        var infoContainer = new GameObject("InfoContainer");
        infoContainer.transform.SetParent(row.transform, false);
        infoContainer.AddComponent<RectTransform>();
        infoContainer.AddComponent<LayoutElement>().flexibleWidth = 1;
        var infoVlg = infoContainer.AddComponent<VerticalLayoutGroup>();
        infoVlg.spacing = 6;
        infoVlg.padding = new RectOffset(10, 10, 10, 10);
        infoVlg.childControlWidth = true; infoVlg.childControlHeight = false;
        infoVlg.childForceExpandWidth = true; infoVlg.childForceExpandHeight = false;
        infoVlg.childAlignment = TextAnchor.UpperLeft;

        // 魔物名+ランク
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(infoContainer.transform, false);
        nameGo.AddComponent<RectTransform>();
        nameGo.AddComponent<LayoutElement>().preferredHeight = 35;
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        var rc = monster.GetRankColor();
        nameTmp.text = $"{monster.baseData.monsterName} <color=#{ColorUtility.ToHtmlStringRGB(rc)}>[{monster.GetRankLabel()}]</color>";
        nameTmp.fontSize = 26; nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = Color.white;
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.richText = true;

        // 獲得素材タイトル
        var matTitleGo = new GameObject("MatTitle");
        matTitleGo.transform.SetParent(infoContainer.transform, false);
        matTitleGo.AddComponent<RectTransform>();
        matTitleGo.AddComponent<LayoutElement>().preferredHeight = 25;
        var matTitleTmp = matTitleGo.AddComponent<TextMeshProUGUI>();
        matTitleTmp.text = "獲得素材：";
        matTitleTmp.fontSize = 18;
        matTitleTmp.color = new Color(0.7f, 0.8f, 0.6f);
        matTitleTmp.alignment = TextAlignmentOptions.Left;

        // 獲得素材リスト
        foreach (var kvp in result)
        {
            string matName = MaterialNames.TryGetValue(kvp.Key, out string mn) ? mn : kvp.Key.ToString();
            var matGo = new GameObject($"Mat_{kvp.Key}");
            matGo.transform.SetParent(infoContainer.transform, false);
            matGo.AddComponent<RectTransform>();
            matGo.AddComponent<LayoutElement>().preferredHeight = 22;
            var matTmp = matGo.AddComponent<TextMeshProUGUI>();
            matTmp.text = $"  {matName} ×{kvp.Value}";
            matTmp.fontSize = 18;
            matTmp.color = new Color(0.85f, 0.85f, 0.9f);
            matTmp.alignment = TextAlignmentOptions.Left;
        }

        // 獲得魂
        var soulRank = monster.GetSoulRank();
        var soulColor = MonsterInstance.GetRankColor(soulRank);
        var soulGo = new GameObject("Soul");
        soulGo.transform.SetParent(infoContainer.transform, false);
        soulGo.AddComponent<RectTransform>();
        soulGo.AddComponent<LayoutElement>().preferredHeight = 25;
        var soulTmp = soulGo.AddComponent<TextMeshProUGUI>();
        soulTmp.text = $"獲得魂: <color=#{ColorUtility.ToHtmlStringRGB(soulColor)}>{soulRank}ランク</color>";
        soulTmp.fontSize = 18;
        soulTmp.fontStyle = FontStyles.Bold;
        soulTmp.color = new Color(0.9f, 0.8f, 1f);
        soulTmp.alignment = TextAlignmentOptions.Left;
        soulTmp.richText = true;

        // OKボタン
        var okBtn = CreateSimpleButton(panel.transform, "OK", new Color(0.3f, 0.45f, 0.6f), () => {
            Destroy(overlay);
            RefreshHeader();
            RefreshDisassembleList();
            RefreshMonsterList();
        });
        var okLe = okBtn.GetComponent<LayoutElement>();
        if (okLe != null)
        {
            okLe.preferredHeight = 40;
            okLe.minHeight = 40;
        }
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
