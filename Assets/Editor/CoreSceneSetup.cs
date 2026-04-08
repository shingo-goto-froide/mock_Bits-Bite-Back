#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CoreSceneSetup : EditorWindow
{
    [MenuItem("Tools/Bits Bite Back/コアシーン構築")]
    public static void SetupAll()
    {
        if (!EditorUtility.DisplayDialog("コアシーン構築",
            "ScriptableObject・Prefab・シーンを一括生成します。\n既存のアセットは上書きされます。\n続行しますか？",
            "実行", "キャンセル"))
            return;

        CreateDirectories();
        var monsterDataMap = CreateMonsterDataAssets();
        CreateGameBalanceAsset();
        CreateEnemyWaveAssets(monsterDataMap);
        CreateMonsterCardPrefab();
        CreateBattleUnitViewPrefab();
        CreateCraftItemPrefab();
        CreateRewardItemPrefab();
        CreateTitleScene();
        CreatePrepareScene();
        CreateBattleScene();
        SetupBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完了", "コアシーン構築が完了しました！\nTitleSceneを開いてPlayしてください。", "OK");
    }

    static void CreateDirectories()
    {
        string[] dirs = {
            "Assets/Resources",
            "Assets/Resources/Monsters",
            "Assets/Resources/EnemyWaves",
            "Assets/Resources/Prefabs",
            "Assets/Prefabs",
            "Assets/Prefabs/UI",
            "Assets/Scenes"
        };
        foreach (var dir in dirs)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string parent = System.IO.Path.GetDirectoryName(dir).Replace("\\", "/");
                string folder = System.IO.Path.GetFileName(dir);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }

    // ==============================
    // ScriptableObject生成
    // ==============================

    static Dictionary<MonsterType, MonsterDataSO> CreateMonsterDataAssets()
    {
        var map = new Dictionary<MonsterType, MonsterDataSO>();
        var defs = GetMonsterDefinitions();

        foreach (var def in defs)
        {
            // Resources/Monsters/ に配置（Resources.LoadAll で取得可能にする）
            string path = $"Assets/Resources/Monsters/{def.type}.asset";
            var so = AssetDatabase.LoadAssetAtPath<MonsterDataSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<MonsterDataSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            so.monsterType = def.type;
            so.monsterName = def.name;
            so.baseHp = def.hp;
            so.baseAttack = def.atk;
            so.speed = def.spd;
            so.range = def.range;
            so.slotSize = def.slots;
            so.attackType = def.attackType;
            so.isPenetrate = def.penetrate;
            so.abilityDescription = def.desc;
            so.recipeMaterials = def.recipe;
            EditorUtility.SetDirty(so);
            map[def.type] = so;
        }

        AssetDatabase.SaveAssets();
        return map;
    }

    static GameBalanceSO CreateGameBalanceAsset()
    {
        string path = "Assets/Resources/GameBalance.asset";
        var so = AssetDatabase.LoadAssetAtPath<GameBalanceSO>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<GameBalanceSO>();
            AssetDatabase.CreateAsset(so, path);
        }
        so.maxFormationSlots = 5;
        so.totalWaveCount = 7;
        so.poisonDamage = 3;
        so.stunChance = 0.3f;
        so.magicBarrierReduction = 0.5f;
        so.guardianMagicMultiplier = 1.5f;
        so.archerDoubleAttackTurn = 5;
        so.skeletonPriestHealRate = 0.1f;
        so.disassemblyReturnRate = 0.5f;
        so.rewardBaseMaterials = 3;
        so.rewardPerWaveBonus = 1;
        so.initialMaterials = new InitialMaterial[]
        {
            new InitialMaterial { type = MaterialType.AnimalSkull, amount = 3 },
            new InitialMaterial { type = MaterialType.HumanSkull, amount = 2 },
            new InitialMaterial { type = MaterialType.LongWood, amount = 3 },
            new InitialMaterial { type = MaterialType.LongBone, amount = 4 },
            new InitialMaterial { type = MaterialType.OldSword, amount = 2 },
        };
        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        return so;
    }

    static void CreateEnemyWaveAssets(Dictionary<MonsterType, MonsterDataSO> monsterMap)
    {
        var waves = new (int num, (MonsterType type, int level)[] enemies)[]
        {
            (1, new[] { (MonsterType.Skeleton, 1), (MonsterType.Skeleton, 1) }),
            (2, new[] { (MonsterType.Zombie, 1), (MonsterType.Skeleton, 1), (MonsterType.Skeleton, 1) }),
            (3, new[] { (MonsterType.Guardian, 1), (MonsterType.Archer, 1), (MonsterType.Archer, 1) }),
            (4, new[] { (MonsterType.Guardian, 1), (MonsterType.Zombie, 1), (MonsterType.SkeletonPriest, 1) }),
            (5, new[] { (MonsterType.Guardian, 2), (MonsterType.Skeleton, 1), (MonsterType.BarrierMage, 1) }),
            (6, new[] { (MonsterType.Orc, 2), (MonsterType.SkeletonPriest, 2), (MonsterType.BarrierMage, 2) }),
            (7, new[] { (MonsterType.Wraith, 2), (MonsterType.Wraith, 2), (MonsterType.GraveKeeper, 2), (MonsterType.Revenger, 2) }),
        };

        foreach (var wave in waves)
        {
            string path = $"Assets/Resources/EnemyWaves/Wave{wave.num}.asset";
            var so = AssetDatabase.LoadAssetAtPath<EnemyWaveSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<EnemyWaveSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            so.waveNumber = wave.num;
            var entries = new List<EnemyEntry>();
            foreach (var e in wave.enemies)
            {
                entries.Add(new EnemyEntry { monsterData = monsterMap[e.type], level = e.level });
            }
            so.enemies = entries.ToArray();
            EditorUtility.SetDirty(so);
        }
        AssetDatabase.SaveAssets();
    }

    // ==============================
    // Prefab生成
    // ==============================

    static GameObject CreateMonsterCardPrefab()
    {
        string path = "Assets/Resources/Prefabs/MonsterCard.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var root = new GameObject("MonsterCard");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 80);

        var bg = root.AddComponent<Image>();
        bg.color = Color.white;

        var button = root.AddComponent<Button>();
        var card = root.AddComponent<MonsterCardUI>();

        var layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 4, 4);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var nameGo = CreateTextChild(root.transform, "NameText", "魔物名", 16, TextAnchor.MiddleLeft);
        var statsGo = CreateTextChild(root.transform, "StatsText", "HP:0 ATK:0", 12, TextAnchor.MiddleLeft);
        var abilityGo = CreateTextChild(root.transform, "AbilityText", "特殊効果", 11, TextAnchor.MiddleLeft);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject CreateBattleUnitViewPrefab()
    {
        string path = "Assets/Resources/Prefabs/BattleUnitView.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var root = new GameObject("BattleUnitView");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(140, 120);

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.5f, 0.7f, 1f);
        root.AddComponent<BattleUnitView>();

        var layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 2;

        CreateTextChild(root.transform, "NameText", "名前", 13, TextAnchor.MiddleCenter);

        // HPバー
        var hpBarGo = new GameObject("HpBar");
        hpBarGo.transform.SetParent(root.transform, false);
        var hpBarRt = hpBarGo.AddComponent<RectTransform>();
        hpBarRt.sizeDelta = new Vector2(0, 16);
        var hpBarLayout = hpBarGo.AddComponent<LayoutElement>();
        hpBarLayout.preferredHeight = 16;

        var slider = hpBarGo.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        // Background
        var bgSlider = new GameObject("Background");
        bgSlider.transform.SetParent(hpBarGo.transform, false);
        var bgSliderRt = bgSlider.AddComponent<RectTransform>();
        bgSliderRt.anchorMin = Vector2.zero;
        bgSliderRt.anchorMax = Vector2.one;
        bgSliderRt.sizeDelta = Vector2.zero;
        var bgSliderImg = bgSlider.AddComponent<Image>();
        bgSliderImg.color = new Color(0.2f, 0.2f, 0.2f);

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(hpBarGo.transform, false);
        var fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.sizeDelta = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.8f, 0.2f);

        slider.fillRect = fillRt;

        CreateTextChild(root.transform, "HpText", "HP: 0/0", 11, TextAnchor.MiddleCenter);
        CreateTextChild(root.transform, "StatusText", "", 10, TextAnchor.MiddleCenter);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject CreateCraftItemPrefab()
    {
        string path = "Assets/Resources/Prefabs/CraftItem.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var root = new GameObject("CraftItem");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40);
        root.AddComponent<Image>().color = new Color(0.9f, 0.9f, 0.85f);
        root.AddComponent<Button>();
        var layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 40;

        CreateTextChild(root.transform, "Text", "アイテム", 13, TextAnchor.MiddleCenter);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject CreateRewardItemPrefab()
    {
        string path = "Assets/Resources/Prefabs/RewardItem.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var root = new GameObject("RewardItem");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 30);
        var layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 30;

        CreateTextChild(root.transform, "Text", "報酬", 14, TextAnchor.MiddleCenter);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ==============================
    // シーン生成
    // ==============================

    static void CreateTitleScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
        cam.orthographic = true;
        camGo.AddComponent<AudioListener>();
        camGo.tag = "MainCamera";

        // EventSystem
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();

        // Canvas
        var canvasGo = CreateCanvas("TitleCanvas");

        // Title Text
        var titleText = CreateUIText(canvasGo.transform, "TitleText", "Bits Bite Back", 48, TextAnchor.MiddleCenter);
        var titleRt = titleText.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.2f, 0.55f);
        titleRt.anchorMax = new Vector2(0.8f, 0.8f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        titleText.GetComponent<Text>().color = Color.white;

        // Start Button
        var startBtn = CreateButton(canvasGo.transform, "StartButton", "ゲーム開始");
        var btnRt = startBtn.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.35f, 0.25f);
        btnRt.anchorMax = new Vector2(0.65f, 0.35f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;

        // TitleUI（参照はランタイムで自動検出）
        canvasGo.AddComponent<TitleUI>();

        // GameManager（SO参照はResources.Loadでフォールバック、コンポーネントはGetComponentで取得）
        var gmGo = new GameObject("GameManager");
        gmGo.AddComponent<GameManager>();
        gmGo.AddComponent<FormationManager>();
        gmGo.AddComponent<CraftingManager>();
        gmGo.AddComponent<BattleManager>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TitleScene.unity");
    }

    static void CreatePrepareScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
        cam.orthographic = true;
        camGo.AddComponent<AudioListener>();
        camGo.tag = "MainCamera";

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();

        var canvasGo = CreateCanvas("PrepareCanvas");

        // Header Panel
        var headerPanel = CreatePanel(canvasGo.transform, "HeaderPanel",
            new Vector2(0f, 0.88f), new Vector2(1f, 1f), new Color(0.2f, 0.2f, 0.3f, 0.9f));
        var headerLayout = headerPanel.AddComponent<HorizontalLayoutGroup>();
        headerLayout.padding = new RectOffset(20, 20, 5, 5);
        headerLayout.spacing = 20;
        headerLayout.childForceExpandWidth = false;

        var waveText = CreateUIText(headerPanel.transform, "WaveText", "Wave 1/7", 20, TextAnchor.MiddleLeft);
        waveText.AddComponent<LayoutElement>().preferredWidth = 120;

        // Material Texts
        var matPanel = new GameObject("MaterialPanel");
        matPanel.transform.SetParent(headerPanel.transform, false);
        matPanel.AddComponent<RectTransform>();
        var matLayout = matPanel.AddComponent<HorizontalLayoutGroup>();
        matLayout.spacing = 15;
        matLayout.childForceExpandWidth = false;

        string[] matNames = { "頭蓋骨(動物)", "頭蓋骨(人)", "長い木", "長い骨", "古い剣" };
        var matTextObjects = new List<GameObject>();
        foreach (var name in matNames)
        {
            var mt = CreateUIText(matPanel.transform, $"Mat_{name}", $"{name}: 0", 14, TextAnchor.MiddleLeft);
            mt.AddComponent<LayoutElement>().preferredWidth = 120;
            matTextObjects.Add(mt);
        }

        // Monster List Panel (left side)
        var monsterListPanel = CreatePanel(canvasGo.transform, "MonsterListPanel",
            new Vector2(0f, 0.15f), new Vector2(0.35f, 0.87f), new Color(0.18f, 0.18f, 0.25f, 0.9f));

        var mlTitle = CreateUIText(monsterListPanel.transform, "MonsterListTitle", "所持魔物", 18, TextAnchor.MiddleCenter);
        var mlTitleRt = mlTitle.GetComponent<RectTransform>();
        mlTitleRt.anchorMin = new Vector2(0, 0.94f);
        mlTitleRt.anchorMax = new Vector2(1, 1f);
        mlTitleRt.offsetMin = Vector2.zero;
        mlTitleRt.offsetMax = Vector2.zero;

        // ScrollView for monster list
        var mlScrollView = CreateScrollView(monsterListPanel.transform, "MonsterScrollView",
            new Vector2(0, 0), new Vector2(1, 0.93f));
        var mlContent = mlScrollView.transform.Find("Viewport/Content").gameObject;

        // Formation Panel (center)
        var formationPanel = CreatePanel(canvasGo.transform, "FormationPanel",
            new Vector2(0.36f, 0.35f), new Vector2(0.64f, 0.87f), new Color(0.2f, 0.25f, 0.2f, 0.9f));

        var fTitle = CreateUIText(formationPanel.transform, "FormationTitle", "隊列 (前衛←→後衛)", 16, TextAnchor.MiddleCenter);
        var fTitleRt = fTitle.GetComponent<RectTransform>();
        fTitleRt.anchorMin = new Vector2(0, 0.9f);
        fTitleRt.anchorMax = new Vector2(1, 1f);
        fTitleRt.offsetMin = Vector2.zero;
        fTitleRt.offsetMax = Vector2.zero;

        var slotsParent = new GameObject("SlotsParent");
        slotsParent.transform.SetParent(formationPanel.transform, false);
        var slotsRt = slotsParent.AddComponent<RectTransform>();
        slotsRt.anchorMin = new Vector2(0.05f, 0.05f);
        slotsRt.anchorMax = new Vector2(0.95f, 0.88f);
        slotsRt.offsetMin = Vector2.zero;
        slotsRt.offsetMax = Vector2.zero;
        var slotsLayout = slotsParent.AddComponent<VerticalLayoutGroup>();
        slotsLayout.spacing = 8;
        slotsLayout.childForceExpandHeight = true;

        var slotObjects = new List<GameObject>();
        for (int i = 0; i < 5; i++)
        {
            var slotGo = new GameObject($"FormationSlot_{i}");
            slotGo.transform.SetParent(slotsParent.transform, false);
            var slotRt = slotGo.AddComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(0, 50);
            slotGo.AddComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            slotGo.AddComponent<Button>();
            slotGo.AddComponent<FormationSlotUI>();

            var slotText = CreateUIText(slotGo.transform, "SlotText", $"スロット{i + 1}\n(空き)", 13, TextAnchor.MiddleCenter);
            var stRt = slotText.GetComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero;
            stRt.anchorMax = Vector2.one;
            stRt.offsetMin = Vector2.zero;
            stRt.offsetMax = Vector2.zero;

            slotObjects.Add(slotGo);
        }

        // Craft Panel (right side)
        var craftPanelGo = CreatePanel(canvasGo.transform, "CraftPanel",
            new Vector2(0.65f, 0.15f), new Vector2(1f, 0.87f), new Color(0.25f, 0.2f, 0.18f, 0.9f));

        var cTitle = CreateUIText(craftPanelGo.transform, "CraftTitle", "練成", 18, TextAnchor.MiddleCenter);
        var cTitleRt = cTitle.GetComponent<RectTransform>();
        cTitleRt.anchorMin = new Vector2(0, 0.94f);
        cTitleRt.anchorMax = new Vector2(1, 1f);
        cTitleRt.offsetMin = Vector2.zero;
        cTitleRt.offsetMax = Vector2.zero;

        var craftScrollView = CreateScrollView(craftPanelGo.transform, "CraftScrollView",
            new Vector2(0, 0), new Vector2(1, 0.93f));
        var craftContent = craftScrollView.transform.Find("Viewport/Content").gameObject;

        // Button Panel (bottom)
        var buttonPanel = CreatePanel(canvasGo.transform, "ButtonPanel",
            new Vector2(0f, 0f), new Vector2(1f, 0.14f), new Color(0.15f, 0.15f, 0.2f, 0.95f));
        var bpLayout = buttonPanel.AddComponent<HorizontalLayoutGroup>();
        bpLayout.padding = new RectOffset(20, 20, 10, 10);
        bpLayout.spacing = 20;
        bpLayout.childAlignment = TextAnchor.MiddleCenter;
        bpLayout.childForceExpandWidth = true;

        var craftTabBtn = CreateButton(buttonPanel.transform, "CraftTabButton", "練成");
        var disassembleTabBtn = CreateButton(buttonPanel.transform, "DisassembleTabButton", "分解");
        var battleStartBtn = CreateButton(buttonPanel.transform, "BattleStartButton", "出撃！");
        battleStartBtn.GetComponent<Image>().color = new Color(0.8f, 0.3f, 0.3f);
        battleStartBtn.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;

        // Message Panel
        var msgPanel = CreatePanel(canvasGo.transform, "MessagePanel",
            new Vector2(0.36f, 0.15f), new Vector2(0.64f, 0.34f), new Color(0.15f, 0.15f, 0.2f, 0.8f));
        var msgText = CreateUIText(msgPanel.transform, "MessageText", "魔物を選択して隊列に配置しましょう", 13, TextAnchor.MiddleCenter);
        var msgRt = msgText.GetComponent<RectTransform>();
        msgRt.anchorMin = Vector2.zero;
        msgRt.anchorMax = Vector2.one;
        msgRt.offsetMin = new Vector2(8, 4);
        msgRt.offsetMax = new Vector2(-8, -4);

        // PrepareUI（参照はランタイムで自動検出）
        canvasGo.AddComponent<PrepareUI>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/PrepareScene.unity");
    }

    static void CreateBattleScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        cam.orthographic = true;
        camGo.AddComponent<AudioListener>();
        camGo.tag = "MainCamera";

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();

        var canvasGo = CreateCanvas("BattleCanvas");

        // Header
        var header = CreatePanel(canvasGo.transform, "BattleHeader",
            new Vector2(0, 0.92f), new Vector2(1, 1f), new Color(0.2f, 0.2f, 0.3f, 0.9f));
        var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.padding = new RectOffset(20, 20, 5, 5);
        headerLayout.spacing = 30;

        var turnText = CreateUIText(header.transform, "TurnText", "ターン 0", 22, TextAnchor.MiddleLeft);
        turnText.AddComponent<LayoutElement>().preferredWidth = 160;
        var bWaveText = CreateUIText(header.transform, "WaveText", "Wave 1", 22, TextAnchor.MiddleLeft);
        bWaveText.AddComponent<LayoutElement>().preferredWidth = 120;

        // Player Formation
        var playerPanel = CreatePanel(canvasGo.transform, "PlayerFormation",
            new Vector2(0.02f, 0.55f), new Vector2(0.98f, 0.91f), new Color(0.15f, 0.2f, 0.3f, 0.7f));
        var playerTitle = CreateUIText(playerPanel.transform, "PlayerTitle", "味方", 14, TextAnchor.UpperCenter);
        var ptRt = playerTitle.GetComponent<RectTransform>();
        ptRt.anchorMin = new Vector2(0, 0.88f);
        ptRt.anchorMax = new Vector2(1, 1f);
        ptRt.offsetMin = Vector2.zero;
        ptRt.offsetMax = Vector2.zero;

        var playerContent = new GameObject("PlayerContent");
        playerContent.transform.SetParent(playerPanel.transform, false);
        var pcRt = playerContent.AddComponent<RectTransform>();
        pcRt.anchorMin = new Vector2(0, 0);
        pcRt.anchorMax = new Vector2(1, 0.87f);
        pcRt.offsetMin = new Vector2(10, 5);
        pcRt.offsetMax = new Vector2(-10, -5);
        var pcLayout = playerContent.AddComponent<HorizontalLayoutGroup>();
        pcLayout.spacing = 8;
        pcLayout.childAlignment = TextAnchor.MiddleCenter;
        pcLayout.childForceExpandWidth = true;

        // Enemy Formation
        var enemyPanel = CreatePanel(canvasGo.transform, "EnemyFormation",
            new Vector2(0.02f, 0.22f), new Vector2(0.98f, 0.54f), new Color(0.3f, 0.15f, 0.15f, 0.7f));
        var enemyTitle = CreateUIText(enemyPanel.transform, "EnemyTitle", "敵", 14, TextAnchor.UpperCenter);
        var etRt = enemyTitle.GetComponent<RectTransform>();
        etRt.anchorMin = new Vector2(0, 0.88f);
        etRt.anchorMax = new Vector2(1, 1f);
        etRt.offsetMin = Vector2.zero;
        etRt.offsetMax = Vector2.zero;

        var enemyContent = new GameObject("EnemyContent");
        enemyContent.transform.SetParent(enemyPanel.transform, false);
        var ecRt = enemyContent.AddComponent<RectTransform>();
        ecRt.anchorMin = new Vector2(0, 0);
        ecRt.anchorMax = new Vector2(1, 0.87f);
        ecRt.offsetMin = new Vector2(10, 5);
        ecRt.offsetMax = new Vector2(-10, -5);
        var ecLayout = enemyContent.AddComponent<HorizontalLayoutGroup>();
        ecLayout.spacing = 8;
        ecLayout.childAlignment = TextAnchor.MiddleCenter;
        ecLayout.childForceExpandWidth = true;

        // Battle Log
        var logPanel = CreatePanel(canvasGo.transform, "BattleLogPanel",
            new Vector2(0.02f, 0.02f), new Vector2(0.75f, 0.21f), new Color(0.1f, 0.1f, 0.1f, 0.9f));
        var logScrollView = CreateScrollView(logPanel.transform, "LogScrollView",
            new Vector2(0, 0), new Vector2(1, 1));
        var logContent = logScrollView.transform.Find("Viewport/Content").gameObject;
        var logTextGo = CreateUIText(logContent.transform, "LogText", "", 11, TextAnchor.UpperLeft);
        var logTextRt = logTextGo.GetComponent<RectTransform>();
        logTextRt.anchorMin = Vector2.zero;
        logTextRt.anchorMax = new Vector2(1, 1);
        logTextRt.offsetMin = new Vector2(5, 0);
        logTextRt.offsetMax = new Vector2(-5, 0);
        var logTextComp = logTextGo.GetComponent<Text>();
        logTextComp.color = new Color(0.8f, 1f, 0.8f);
        logTextGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Speed Button
        var speedBtn = CreateButton(canvasGo.transform, "SpeedButton", "速度: 1x");
        var speedBtnRt = speedBtn.GetComponent<RectTransform>();
        speedBtnRt.anchorMin = new Vector2(0.78f, 0.02f);
        speedBtnRt.anchorMax = new Vector2(0.98f, 0.08f);
        speedBtnRt.offsetMin = Vector2.zero;
        speedBtnRt.offsetMax = Vector2.zero;

        // Result Panel
        var resultPanel = CreatePanel(canvasGo.transform, "ResultPanel",
            new Vector2(0.25f, 0.35f), new Vector2(0.75f, 0.65f), new Color(0.1f, 0.1f, 0.2f, 0.95f));
        resultPanel.SetActive(false);

        var resultText = CreateUIText(resultPanel.transform, "ResultText", "結果", 36, TextAnchor.MiddleCenter);
        var rrRt = resultText.GetComponent<RectTransform>();
        rrRt.anchorMin = new Vector2(0, 0.5f);
        rrRt.anchorMax = new Vector2(1, 0.9f);
        rrRt.offsetMin = Vector2.zero;
        rrRt.offsetMax = Vector2.zero;
        resultText.GetComponent<Text>().color = Color.white;

        var continueBtn = CreateButton(resultPanel.transform, "ContinueButton", "続ける");
        var cbRt = continueBtn.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(0.25f, 0.1f);
        cbRt.anchorMax = new Vector2(0.75f, 0.4f);
        cbRt.offsetMin = Vector2.zero;
        cbRt.offsetMax = Vector2.zero;

        // Reward Panel
        var rewardPanel = CreatePanel(canvasGo.transform, "RewardPanel",
            new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), new Color(0.1f, 0.15f, 0.1f, 0.95f));
        rewardPanel.SetActive(false);

        var rewardTitle = CreateUIText(rewardPanel.transform, "RewardTitle", "報酬", 28, TextAnchor.MiddleCenter);
        var rwTitleRt = rewardTitle.GetComponent<RectTransform>();
        rwTitleRt.anchorMin = new Vector2(0, 0.85f);
        rwTitleRt.anchorMax = new Vector2(1, 0.98f);
        rwTitleRt.offsetMin = Vector2.zero;
        rwTitleRt.offsetMax = Vector2.zero;
        rewardTitle.GetComponent<Text>().color = Color.white;

        var rewardScrollView = CreateScrollView(rewardPanel.transform, "RewardScrollView",
            new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.83f));
        var rewardContent = rewardScrollView.transform.Find("Viewport/Content").gameObject;

        var rewardContinueBtn = CreateButton(rewardPanel.transform, "RewardContinueButton", "次へ進む");
        var rcbRt = rewardContinueBtn.GetComponent<RectTransform>();
        rcbRt.anchorMin = new Vector2(0.3f, 0.03f);
        rcbRt.anchorMax = new Vector2(0.7f, 0.15f);
        rcbRt.offsetMin = Vector2.zero;
        rcbRt.offsetMax = Vector2.zero;

        // BattleUI（参照はランタイムで自動検出）
        canvasGo.AddComponent<BattleUI>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/BattleScene.unity");
    }

    static void SetupBuildSettings()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/TitleScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/PrepareScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/BattleScene.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
    }

    // ==============================
    // ヘルパー
    // ==============================

    static GameObject CreateCanvas(string name)
    {
        var canvasGo = new GameObject(name);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        return canvasGo;
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static GameObject CreateUIText(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = Color.white;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return go;
    }

    static GameObject CreateTextChild(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
    {
        return CreateUIText(parent, name, text, fontSize, alignment);
    }

    static GameObject CreateButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.4f);
        go.AddComponent<Button>();

        var textGo = CreateUIText(go.transform, "Text", label, 16, TextAnchor.MiddleCenter);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        return go;
    }

    static GameObject CreateScrollView(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var scrollGo = new GameObject(name);
        scrollGo.transform.SetParent(parent, false);
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin = anchorMin;
        scrollRt.anchorMax = anchorMax;
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;
        scrollGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // Viewport
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewport.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(0, 0);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = vpRt;
        scrollRect.content = contentRt;

        return scrollGo;
    }

    // ==============================
    // 魔物データ定義
    // ==============================

    struct MonsterDef
    {
        public MonsterType type;
        public string name;
        public int hp, atk, spd, range, slots;
        public AttackType attackType;
        public bool penetrate;
        public string desc;
        public RecipeEntry[] recipe;
    }

    static MonsterDef[] GetMonsterDefinitions()
    {
        return new MonsterDef[]
        {
            new MonsterDef {
                type = MonsterType.Skeleton, name = "スケルトン", hp = 20, atk = 1, spd = 3, range = 1, slots = 1,
                attackType = AttackType.Physical, penetrate = false,
                desc = "バトル開始時に1つ前の味方の攻撃力+1",
                recipe = new[] { new RecipeEntry { type = MaterialType.LongBone, amount = 2 }, new RecipeEntry { type = MaterialType.AnimalSkull, amount = 1 } }
            },
            new MonsterDef {
                type = MonsterType.Guardian, name = "ガーディアン", hp = 100, atk = 2, spd = 1, range = 1, slots = 2,
                attackType = AttackType.Physical, penetrate = false,
                desc = "攻撃をすべて引き受けるが魔法攻撃は1.5倍受ける",
                recipe = new[] { new RecipeEntry { type = MaterialType.OldSword, amount = 3 }, new RecipeEntry { type = MaterialType.LongBone, amount = 2 }, new RecipeEntry { type = MaterialType.HumanSkull, amount = 1 } }
            },
            new MonsterDef {
                type = MonsterType.Archer, name = "アーチャー", hp = 20, atk = 2, spd = 4, range = 3, slots = 1,
                attackType = AttackType.Physical, penetrate = false,
                desc = "5ターン目以降、攻撃が2連射になる",
                recipe = new[] { new RecipeEntry { type = MaterialType.LongWood, amount = 2 }, new RecipeEntry { type = MaterialType.LongBone, amount = 1 } }
            },
            new MonsterDef {
                type = MonsterType.Zombie, name = "ゾンビ", hp = 35, atk = 1, spd = 2, range = 1, slots = 1,
                attackType = AttackType.Physical, penetrate = false,
                desc = "前後の味方に毒攻撃（ターン開始時HP-3）を付与",
                recipe = new[] { new RecipeEntry { type = MaterialType.HumanSkull, amount = 2 }, new RecipeEntry { type = MaterialType.LongBone, amount = 1 } }
            },
            new MonsterDef {
                type = MonsterType.ShadowWalker, name = "シャドウウォーカー", hp = 20, atk = 1, spd = 5, range = 0, slots = 1,
                attackType = AttackType.Physical, penetrate = false,
                desc = "攻撃時、敵隊列のランダムな位置のユニットを攻撃",
                recipe = new[] { new RecipeEntry { type = MaterialType.AnimalSkull, amount = 2 }, new RecipeEntry { type = MaterialType.LongWood, amount = 1 } }
            },
            new MonsterDef {
                type = MonsterType.SkeletonPriest, name = "骸骨僧侶", hp = 30, atk = 1, spd = 2, range = 1, slots = 1,
                attackType = AttackType.Magic, penetrate = false,
                desc = "バトル終了時に味方全体のHPを10%回復",
                recipe = new[] { new RecipeEntry { type = MaterialType.LongBone, amount = 2 }, new RecipeEntry { type = MaterialType.HumanSkull, amount = 1 } }
            },
            new MonsterDef {
                type = MonsterType.Orc, name = "オーク", hp = 70, atk = 5, spd = 1, range = 1, slots = 2,
                attackType = AttackType.Physical, penetrate = false,
                desc = "編成枠を2枠使う。攻撃時、相手にピヨリを付与",
                recipe = new[] { new RecipeEntry { type = MaterialType.AnimalSkull, amount = 3 }, new RecipeEntry { type = MaterialType.LongWood, amount = 2 }, new RecipeEntry { type = MaterialType.OldSword, amount = 1 } }
            },
            new MonsterDef {
                type = MonsterType.ShapeShifter, name = "シェイプシフター", hp = 25, atk = 1, spd = 4, range = 1, slots = 1,
                attackType = AttackType.Physical, penetrate = false,
                desc = "行動時に1つ後ろの魔物のステータスと特殊効果をコピー",
                recipe = new[] { new RecipeEntry { type = MaterialType.AnimalSkull, amount = 1 }, new RecipeEntry { type = MaterialType.HumanSkull, amount = 1 }, new RecipeEntry { type = MaterialType.LongBone, amount = 1 } }
            },
            new MonsterDef {
                type = MonsterType.Martyr, name = "殉教者", hp = 50, atk = 0, spd = 2, range = 0, slots = 1,
                attackType = AttackType.Physical, penetrate = false,
                desc = "行動時に自分のHPをダメージを受けた仲間に与える",
                recipe = new[] { new RecipeEntry { type = MaterialType.HumanSkull, amount = 2 }, new RecipeEntry { type = MaterialType.OldSword, amount = 1 } }
            },
            new MonsterDef {
                type = MonsterType.Wraith, name = "レイス", hp = 15, atk = 3, spd = 4, range = 1, slots = 1,
                attackType = AttackType.Magic, penetrate = false,
                desc = "死亡時に最後尾の魔物をHP+10、攻撃力+3",
                recipe = new[] { new RecipeEntry { type = MaterialType.HumanSkull, amount = 2 }, new RecipeEntry { type = MaterialType.LongBone, amount = 1 } }
            },
            new MonsterDef {
                type = MonsterType.BarrierMage, name = "結界師", hp = 25, atk = 2, spd = 3, range = 1, slots = 1,
                attackType = AttackType.Magic, penetrate = false,
                desc = "バトル開始時、前衛にダメージ半減の魔法バリア",
                recipe = new[] { new RecipeEntry { type = MaterialType.LongWood, amount = 2 }, new RecipeEntry { type = MaterialType.HumanSkull, amount = 1 } }
            },
            new MonsterDef {
                type = MonsterType.GraveKeeper, name = "墓守", hp = 35, atk = 3, spd = 2, range = 1, slots = 1,
                attackType = AttackType.Physical, penetrate = false,
                desc = "死亡時にこの階層で死んだ他の味方を全て復活",
                recipe = new[] { new RecipeEntry { type = MaterialType.LongBone, amount = 2 }, new RecipeEntry { type = MaterialType.OldSword, amount = 1 }, new RecipeEntry { type = MaterialType.AnimalSkull, amount = 1 } }
            },
            new MonsterDef {
                type = MonsterType.Revenger, name = "リベンジャー", hp = 25, atk = 5, spd = 3, range = 3, slots = 1,
                attackType = AttackType.Physical, penetrate = true,
                desc = "味方の死亡1体につきHP+4、攻撃力+2。貫通攻撃",
                recipe = new[] { new RecipeEntry { type = MaterialType.OldSword, amount = 2 }, new RecipeEntry { type = MaterialType.HumanSkull, amount = 1 }, new RecipeEntry { type = MaterialType.LongBone, amount = 1 } }
            },
        };
    }
}
#endif
