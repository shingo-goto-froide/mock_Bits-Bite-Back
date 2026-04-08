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
            waveText.text = $"Wave {gm.CurrentWave + 1} / {gm.Balance.totalWaveCount}";

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

            PlaceImage(go.transform);
            var info = PlaceInfo(go.transform);

            AddTmpLE(info.transform, "NameText", d.monsterName, 24, FontStyles.Bold, Color.white, 28, TextAlignmentOptions.Center);
            AddTmpLE(info.transform, "StatsText",
                $"HP:{d.baseHp}  ATK:{d.baseAttack}  SPD:{d.speed}  射程:{d.range}  枠:{d.slotSize}",
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

        var craftable = gm.Crafting.GetCraftableMonsters(gm.Inventory);
        foreach (var data in craftable)
        {
            string recipe = FormatRecipe(data.recipeMaterials);
            var go = CreateCraftRow(craftListContent, data.monsterName,
                $"HP:{data.baseHp}  ATK:{data.baseAttack}  SPD:{data.speed}  射程:{data.range}  枠:{data.slotSize}",
                data.abilityDescription, recipe);
            var capturedData = data;
            go.GetComponent<Button>()?.onClick.AddListener(() => OnCraftSelected(capturedData));
        }

        if (craftable.Count == 0)
            CreateEmptyRow(craftListContent, "素材が足りません");
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
                d.monsterName,
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

    private void PlaceImage(Transform parent)
    {
        var imgGo = new GameObject("ImagePlaceholder");
        imgGo.transform.SetParent(parent, false);
        var rt = imgGo.AddComponent<RectTransform>();
        // 左端に固定サイズで配置
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(ImgMargin, 0);
        rt.sizeDelta = new Vector2(ImgSize, ImgSize);
        imgGo.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.32f);

        var txt = new GameObject("Label"); txt.transform.SetParent(imgGo.transform, false);
        var trt = txt.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = "Image"; tmp.fontSize = 16; tmp.color = new Color(0.45f, 0.45f, 0.55f);
        tmp.alignment = TextAlignmentOptions.Center;
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

    private GameObject CreateCraftRow(Transform parent, string monsterName, string stats, string ability, string recipe)
    {
        var go = CreateCardBase(parent, "CraftRow");
        PlaceImage(go.transform);
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

    private GameObject CreateDisassembleRow(Transform parent, string monsterName, string stats, string ability, string materials)
    {
        var go = CreateCardBase(parent, "DisassembleRow");
        PlaceImage(go.transform);
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
        SceneManager.LoadScene("BattleScene");
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
