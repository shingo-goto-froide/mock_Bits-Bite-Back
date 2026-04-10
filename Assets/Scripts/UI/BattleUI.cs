using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class BattleUI : MonoBehaviour
{
    private TMP_Text turnText;
    private TMP_Text waveText;
    private Transform playerFormationParent;
    private Transform enemyFormationParent;
    private GameObject battleUnitViewPrefab;
    private TMP_Text logText;
    private ScrollRect logScrollRect;
    private Button speedButton;
    private TMP_Text speedButtonText;
    private GameObject resultPanel;
    private TMP_Text resultText;
    private Button continueButton;
    private GameObject rewardPanel;
    private Transform rewardListContent;
    private Button rewardContinueButton;
    private GameObject rewardItemPrefab;

    private BattleManager battleManager;
    private List<BattleUnitView> playerViews = new List<BattleUnitView>();
    private List<BattleUnitView> enemyViews = new List<BattleUnitView>();
    private Dictionary<BattleUnit, BattleUnitView> unitViewMap = new Dictionary<BattleUnit, BattleUnitView>();
    private float[] speedOptions = { 1f, 0.5f, 0.2f };
    private string[] speedLabels = { "1x", "2x", "5x" };
    private int currentSpeedIndex;
    private string fullLog = "";

    private void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Prefabはリソースから読み込み
        battleUnitViewPrefab = Resources.Load<GameObject>("Prefabs/BattleUnitView");
        rewardItemPrefab = Resources.Load<GameObject>("Prefabs/RewardItem");

        // UI参照を自動検出
        turnText = FindText("TurnText");
        waveText = FindText("WaveText");
        logText = FindText("LogText");
        resultText = FindText("ResultText");

        playerFormationParent = FindDeep(transform, "PlayerContent");
        enemyFormationParent = FindDeep(transform, "EnemyContent");

        // 味方は左詰め、敵は右詰め
        var playerLayout = playerFormationParent?.GetComponent<HorizontalLayoutGroup>();
        if (playerLayout != null) playerLayout.childAlignment = TextAnchor.MiddleLeft;
        var enemyLayout = enemyFormationParent?.GetComponent<HorizontalLayoutGroup>();
        if (enemyLayout != null) enemyLayout.childAlignment = TextAnchor.MiddleRight;

        var logScroll = FindDeep(transform, "LogScrollView");
        if (logScroll != null) logScrollRect = logScroll.GetComponent<ScrollRect>();

        speedButton = FindButton("SpeedButton");
        speedButtonText = speedButton?.GetComponentInChildren<TMP_Text>();

        var resultPanelT = FindDeep(transform, "ResultPanel");
        resultPanel = resultPanelT?.gameObject;
        continueButton = FindButton("ContinueButton");

        var rewardPanelT = FindDeep(transform, "RewardPanel");
        rewardPanel = rewardPanelT?.gameObject;
        var rewardScroll = FindDeep(transform, "RewardScrollView");
        if (rewardScroll != null) rewardListContent = FindDeep(rewardScroll, "Content");
        rewardContinueButton = FindButton("RewardContinueButton");

        // BattleManager
        battleManager = FindFirstObjectByType<BattleManager>();
        if (battleManager == null)
        {
            var go = new GameObject("BattleManager");
            battleManager = go.AddComponent<BattleManager>();
        }
        battleManager.Init(gm.Balance);

        if (resultPanel != null) resultPanel.SetActive(false);
        if (rewardPanel != null) rewardPanel.SetActive(false);

        // イベント登録
        battleManager.OnTurnStartEvent += OnTurnStart;
        battleManager.OnUnitDeathEvent += OnUnitDeath;
        battleManager.OnBattleEndEvent += OnBattleEnd;
        battleManager.OnLogEvent += OnLog;
        battleManager.OnBeforeUnitAction += OnBeforeAction;
        battleManager.OnAfterUnitAction += OnAfterAction;
        battleManager.OnDamageDealtEvent += OnDamageDealt;
        battleManager.OnUnitEffectEvent += OnUnitEffect;
        battleManager.OnBuffDebuffEvent += OnBuffDebuff;

        if (speedButton != null) speedButton.onClick.AddListener(OnSpeedClicked);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (rewardContinueButton != null) rewardContinueButton.onClick.AddListener(OnRewardContinueClicked);

        // バトル開始
        Debug.Log("[BattleUI] バトルセットアップ開始");
        var formation = gm.Formation.GetFormation();
        Debug.Log($"[BattleUI] 隊列: {formation.Count}体, IsDungeonBattle={gm.IsDungeonBattle}");

        if (gm.IsDungeonBattle && gm.DungeonBattleWave != null)
        {
            Debug.Log($"[BattleUI] ダンジョンバトル: Wave={gm.DungeonBattleWave.name}, Boss={gm.IsBossBattle}");
            battleManager.isBossBattle = gm.IsBossBattle;
            battleManager.returnToDungeon = !gm.IsBossBattle;
            battleManager.SetupBattle(formation, gm.DungeonBattleWave, gm.AllMonsterData);
            if (waveText != null)
                waveText.text = gm.IsBossBattle ? "BOSS" : "ダンジョンバトル";
        }
        else
        {
            Debug.Log($"[BattleUI] 通常バトル: CurrentWave={gm.CurrentWave}");
            if (gm.CurrentWave < gm.EnemyWaves.Length)
                battleManager.SetupBattle(formation, gm.EnemyWaves[gm.CurrentWave], gm.AllMonsterData);
            battleManager.isBossBattle = false;
            battleManager.returnToDungeon = false;
            if (waveText != null)
                waveText.text = $"Wave {gm.CurrentWave + 1}";
        }

        Debug.Log($"[BattleUI] SetupBattle完了: 味方{battleManager.playerUnits.Count}, 敵{battleManager.enemyUnits.Count}");
        SetupViews();
        Debug.Log("[BattleUI] SetupViews完了, StartBattle呼び出し");
        battleManager.StartBattle();
        Debug.Log("[BattleUI] StartBattle完了（コルーチン開始）");
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

    private void OnDestroy()
    {
        if (battleManager != null)
        {
            battleManager.OnTurnStartEvent -= OnTurnStart;
            battleManager.OnUnitDeathEvent -= OnUnitDeath;
            battleManager.OnBattleEndEvent -= OnBattleEnd;
            battleManager.OnLogEvent -= OnLog;
            battleManager.OnBeforeUnitAction -= OnBeforeAction;
            battleManager.OnAfterUnitAction -= OnAfterAction;
            battleManager.OnDamageDealtEvent -= OnDamageDealt;
            battleManager.OnUnitEffectEvent -= OnUnitEffect;
            battleManager.OnBuffDebuffEvent -= OnBuffDebuff;
        }
    }

    private void SetupViews()
    {
        if (battleUnitViewPrefab == null)
        {
            Debug.LogError("[BattleUI] BattleUnitView prefab が Resources/Prefabs/ に見つかりません");
            return;
        }

        foreach (var unit in battleManager.playerUnits)
        {
            var go = Instantiate(battleUnitViewPrefab, playerFormationParent);
            var view = go.GetComponent<BattleUnitView>();
            view.Bind(unit);
            playerViews.Add(view);
            unitViewMap[unit] = view;
        }

        // 敵は逆順に配置（前衛が右端=味方側に来る）
        for (int i = battleManager.enemyUnits.Count - 1; i >= 0; i--)
        {
            var unit = battleManager.enemyUnits[i];
            var go = Instantiate(battleUnitViewPrefab, enemyFormationParent);
            var view = go.GetComponent<BattleUnitView>();
            view.Bind(unit);
            enemyViews.Add(view);
            unitViewMap[unit] = view;
        }
    }

    private void Update()
    {
        foreach (var view in playerViews)
        {
            // 復活したユニットを再表示
            if (!view.gameObject.activeSelf && view.Unit != null && view.Unit.isAlive)
                view.gameObject.SetActive(true);
            if (view.gameObject.activeSelf) view.Refresh();
        }
        foreach (var view in enemyViews)
        {
            if (!view.gameObject.activeSelf && view.Unit != null && view.Unit.isAlive)
                view.gameObject.SetActive(true);
            if (view.gameObject.activeSelf) view.Refresh();
        }
    }

    private void OnTurnStart(int turn)
    {
        if (turnText != null)
            turnText.text = $"ターン {turn}";
    }

    private void OnBeforeAction(BattleUnit unit)
    {
        if (unitViewMap.TryGetValue(unit, out var view))
            view.SetActing(true);
    }

    private void OnAfterAction(BattleUnit unit)
    {
        if (unitViewMap.TryGetValue(unit, out var view))
            view.SetActing(false);
    }

    private void OnDamageDealt(BattleUnit attacker, BattleUnit target, int damage, bool killed)
    {
        if (unitViewMap.TryGetValue(attacker, out var fromView) &&
            unitViewMap.TryGetValue(target, out var toView))
        {
            StartCoroutine(ProjectileAndDamage(fromView, toView, damage, killed, attacker.isPlayerSide));
        }
        else if (unitViewMap.TryGetValue(target, out var view))
        {
            view.ShowDamagePopup(damage);
            if (killed)
                view.ShowEffectText("撃破!", new Color(1f, 0.5f, 0f));
        }
    }

    private IEnumerator ProjectileAndDamage(BattleUnitView from, BattleUnitView to, int damage, bool killed, bool isPlayerSide)
    {
        Color baseColor = isPlayerSide ? new Color(0.4f, 0.7f, 1f) : new Color(1f, 0.4f, 0.2f);
        Color coreColor = isPlayerSide ? new Color(0.8f, 0.95f, 1f) : new Color(1f, 0.85f, 0.6f);

        // 光の弾を生成
        var projectile = new GameObject("Projectile");
        projectile.transform.SetParent(transform, false);
        var prt = projectile.AddComponent<RectTransform>();
        prt.sizeDelta = new Vector2(24, 24);

        // 外側のグロー
        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(projectile.transform, false);
        var glowRt = glowGo.AddComponent<RectTransform>();
        glowRt.sizeDelta = new Vector2(40, 40);
        var glowImg = glowGo.AddComponent<Image>();
        glowImg.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.4f);
        glowImg.raycastTarget = false;

        // 中心の明るいコア
        var coreGo = new GameObject("Core");
        coreGo.transform.SetParent(projectile.transform, false);
        var coreRt = coreGo.AddComponent<RectTransform>();
        coreRt.sizeDelta = new Vector2(16, 16);
        var coreImg = coreGo.AddComponent<Image>();
        coreImg.color = coreColor;
        coreImg.raycastTarget = false;

        Vector3 startPos = from.transform.position;
        Vector3 endPos = to.transform.position;

        // 飛行アニメーション
        float flyDuration = 0.2f;
        float elapsed = 0f;
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            float eased = t * t * (3f - 2f * t); // smoothstep
            projectile.transform.position = Vector3.Lerp(startPos, endPos, eased);
            // 飛行中に少し脈動
            float pulse = 1f + 0.2f * Mathf.Sin(t * Mathf.PI * 3f);
            prt.localScale = new Vector3(pulse, pulse, 1f);
            yield return null;
        }

        Destroy(projectile);

        // 着弾エフェクト（はじける）
        StartCoroutine(BurstEffect(endPos, baseColor));
        StartCoroutine(BurstEffect(endPos, coreColor, 0.05f, 0.6f));

        // ダメージ表示
        to.ShowDamagePopup(damage);
        if (killed)
            to.ShowEffectText("撃破!", new Color(1f, 0.5f, 0f));
    }

    private IEnumerator BurstEffect(Vector3 position, Color color, float delay = 0f, float sizeMultiplier = 1f)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        var burst = new GameObject("Burst");
        burst.transform.SetParent(transform, false);
        burst.transform.position = position;
        var brt = burst.AddComponent<RectTransform>();
        brt.sizeDelta = new Vector2(30, 30) * sizeMultiplier;
        var bImg = burst.AddComponent<Image>();
        bImg.color = new Color(color.r, color.g, color.b, 0.8f);
        bImg.raycastTarget = false;

        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + t * 2.5f;
            brt.localScale = new Vector3(scale, scale, 1f);
            bImg.color = new Color(color.r, color.g, color.b, 0.8f * (1f - t));
            yield return null;
        }

        Destroy(burst);
    }

    private void OnUnitEffect(BattleUnit unit, string effect)
    {
        if (unitViewMap.TryGetValue(unit, out var view))
        {
            Color color = Color.white;
            if (effect.Contains("ピヨリ")) color = new Color(1f, 1f, 0.3f);
            else if (effect.Contains("射程外")) color = new Color(0.6f, 0.6f, 0.6f);
            view.ShowEffectText(effect, color);
        }
    }

    private void OnBuffDebuff(BattleUnit unit, bool isBuff)
    {
        if (unitViewMap.TryGetValue(unit, out var view))
        {
            if (isBuff)
                view.Flash(new Color(0.2f, 0.9f, 0.3f), 0.35f); // 緑: バフ・回復
            else
                view.Flash(new Color(0.6f, 0.15f, 0.8f), 0.35f); // 紫: デバフ・毒
        }
    }

    private void OnUnitDeath(BattleUnit unit)
    {
        if (unitViewMap.TryGetValue(unit, out var view))
            StartCoroutine(HideAfterDelay(view, 0.5f));
    }

    private IEnumerator HideAfterDelay(BattleUnitView view, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (view != null && view.Unit != null && !view.Unit.isAlive)
            view.gameObject.SetActive(false);
    }

    private void OnBattleEnd(BattleResult result)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            if (resultText != null)
            {
                if (result == BattleResult.Victory)
                {
                    var gm = GameManager.Instance;
                    if (gm != null && gm.IsDungeonBattle && gm.IsBossBattle)
                        resultText.text = "ボス撃破！ダンジョンクリア！";
                    else
                        resultText.text = "勝利！";
                }
                else
                {
                    resultText.text = "全滅…";
                }
            }

            // ダンジョンバトルの場合、Continueボタンのテキストを変更
            if (continueButton != null)
            {
                var gm = GameManager.Instance;
                var btnText = continueButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null && gm != null && gm.IsDungeonBattle)
                {
                    if (result == BattleResult.Victory)
                        btnText.text = gm.IsBossBattle ? "タイトルへ" : "続ける";
                    else
                        btnText.text = "タイトルへ";
                }
            }
        }
    }

    private void OnLog(string message)
    {
        fullLog += message + "\n";
        if (logText != null)
        {
            logText.text = fullLog;
            Canvas.ForceUpdateCanvases();
            if (logScrollRect != null)
                logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void OnSpeedClicked()
    {
        currentSpeedIndex = (currentSpeedIndex + 1) % speedOptions.Length;
        battleManager.SetTurnSpeed(speedOptions[currentSpeedIndex]);
        if (speedButtonText != null)
            speedButtonText.text = $"速度: {speedLabels[currentSpeedIndex]}";
    }

    private void OnContinueClicked()
    {
        var gm = GameManager.Instance;
        var lastResult = battleManager.CheckBattleEnd();

        if (gm.IsDungeonBattle)
        {
            // ダンジョンバトルの終了処理
            gm.OnDungeonBattleEnd(lastResult ?? BattleResult.Defeat);
            return;
        }

        // 通常Wave戦（v1.0互換）
        if (lastResult == BattleResult.Victory)
        {
            gm.AdvanceWave();
            gm.GiveReward();
            ShowReward();
        }
        else
        {
            gm.GameOver();
            SceneManager.LoadScene("TitleScene");
        }
    }

    private void ShowReward()
    {
        if (resultPanel != null) resultPanel.SetActive(false);

        var gm = GameManager.Instance;
        if (gm.CurrentWave >= gm.EnemyWaves.Length)
        {
            // 全クリア
            if (rewardPanel != null)
            {
                rewardPanel.SetActive(true);
                if (rewardListContent != null)
                {
                    var msgGo = new GameObject("ClearMessage");
                    msgGo.transform.SetParent(rewardListContent, false);
                    msgGo.AddComponent<RectTransform>();
                    msgGo.AddComponent<LayoutElement>().minHeight = 60;
                    var tmp = msgGo.AddComponent<TextMeshProUGUI>();
                    tmp.text = "全ウェーブクリア！おめでとう！";
                    tmp.fontSize = 28; tmp.color = new Color(1f, 0.9f, 0.4f);
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }
            return;
        }

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            DisplayRewardItems();
        }
    }

    private void DisplayRewardItems()
    {
        if (rewardListContent == null) return;

        foreach (Transform child in rewardListContent)
            Destroy(child.gameObject);

        var materialNames = new Dictionary<MaterialType, string> {
            { MaterialType.AnimalSkull, "頭蓋骨(動物)" }, { MaterialType.HumanSkull, "頭蓋骨(人)" },
            { MaterialType.LongWood, "長い木" }, { MaterialType.LongBone, "長い骨" }, { MaterialType.OldSword, "古い剣" }
        };

        var inventory = GameManager.Instance.Inventory.GetAll();
        foreach (var kvp in inventory)
        {
            if (kvp.Value <= 0) continue;
            string matName = materialNames.TryGetValue(kvp.Key, out string n) ? n : kvp.Key.ToString();
            CreateRewardCard(rewardListContent, matName, kvp.Value);
        }
    }

    private void CreateRewardCard(Transform parent, string itemName, int count)
    {
        var go = new GameObject("RewardCard");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.24f, 0.95f);
        go.AddComponent<Outline>().effectColor = new Color(0.35f, 0.35f, 0.55f, 0.6f);
        go.AddComponent<LayoutElement>().minHeight = 80;

        // 左: アイコン（固定サイズ）
        var imgGo = new GameObject("Icon");
        imgGo.transform.SetParent(go.transform, false);
        var imgRt = imgGo.AddComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0, 0.5f);
        imgRt.anchorMax = new Vector2(0, 0.5f);
        imgRt.pivot = new Vector2(0, 0.5f);
        imgRt.anchoredPosition = new Vector2(10, 0);
        imgRt.sizeDelta = new Vector2(60, 60);
        imgGo.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.32f);
        var iconLabel = new GameObject("Label");
        iconLabel.transform.SetParent(imgGo.transform, false);
        var ilRt = iconLabel.AddComponent<RectTransform>();
        ilRt.anchorMin = Vector2.zero; ilRt.anchorMax = Vector2.one;
        ilRt.offsetMin = Vector2.zero; ilRt.offsetMax = Vector2.zero;
        var ilTmp = iconLabel.AddComponent<TextMeshProUGUI>();
        ilTmp.text = "Icon"; ilTmp.fontSize = 12;
        ilTmp.color = new Color(0.45f, 0.45f, 0.55f);
        ilTmp.alignment = TextAlignmentOptions.Center;

        // 右: テキスト（名前 + 個数）
        var info = new GameObject("Info");
        info.transform.SetParent(go.transform, false);
        var infoRt = info.AddComponent<RectTransform>();
        infoRt.anchorMin = Vector2.zero; infoRt.anchorMax = Vector2.one;
        infoRt.offsetMin = new Vector2(80, 6);
        infoRt.offsetMax = new Vector2(-8, -6);
        var vlg = info.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2; vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleLeft;

        // 名前
        var nameGo = new GameObject("Name"); nameGo.transform.SetParent(info.transform, false);
        nameGo.AddComponent<RectTransform>();
        nameGo.AddComponent<LayoutElement>().minHeight = 28;
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text = itemName; nameTmp.fontSize = 22; nameTmp.color = Color.white;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // 個数
        var countGo = new GameObject("Count"); countGo.transform.SetParent(info.transform, false);
        countGo.AddComponent<RectTransform>();
        countGo.AddComponent<LayoutElement>().minHeight = 24;
        var countTmp = countGo.AddComponent<TextMeshProUGUI>();
        countTmp.text = $"×{count}"; countTmp.fontSize = 20;
        countTmp.color = new Color(0.9f, 0.85f, 0.5f);
        countTmp.fontStyle = FontStyles.Bold;
        countTmp.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void OnRewardContinueClicked()
    {
        var gm = GameManager.Instance;
        if (gm.CurrentWave >= gm.EnemyWaves.Length)
        {
            gm.GameClear();
            SceneManager.LoadScene("TitleScene");
        }
        else
        {
            battleManager.ClearBattle();
            gm.ReturnToPrepare();
            SceneManager.LoadScene("PrepareScene");
        }
    }
}
