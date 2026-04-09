using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUnitView : MonoBehaviour
{
    private TMP_Text nameText;
    private Image hpBarFill;
    private TMP_Text paramText;
    private TMP_Text abilityText;
    private TMP_Text statusText;
    private Image background;
    private BattleUnit unit;

    private bool isActing;
    private float flashEndTime;
    private Color flashColor;

    public BattleUnit Unit => unit;

    public void Bind(BattleUnit unit)
    {
        this.unit = unit;

        // 既存の子オブジェクトをクリア
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // ルート設定
        background = GetComponent<Image>();
        if (background == null) background = gameObject.AddComponent<Image>();

        var le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 160;
        le.preferredHeight = 300;

        var vlg = GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 3;
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        // 魔物画像エリア（プレースホルダー）
        BuildImageArea();

        // 名前（中央寄せ）
        nameText = BuildLabel("NameText", unit.monster.baseData.monsterName, 15, FontStyles.Bold, Color.white, 22);
        nameText.alignment = TextAlignmentOptions.Center;

        // HPバー
        BuildHpBar();

        // パラメータ（中央寄せ）
        paramText = BuildLabel("ParamText", "", 13, FontStyles.Bold, new Color(0.95f, 0.95f, 0.95f), 20);
        paramText.alignment = TextAlignmentOptions.Center;

        // 能力説明
        abilityText = BuildLabel("AbilityText", unit.monster.baseData.abilityDescription, 12, FontStyles.Normal, new Color(1f, 0.95f, 0.7f), 42);
        abilityText.enableWordWrapping = true;
        abilityText.overflowMode = TextOverflowModes.Ellipsis;

        // 状態異常
        statusText = BuildLabel("StatusText", "", 11, FontStyles.Bold, new Color(1f, 0.7f, 0.7f), 18);

        Refresh();
    }

    private static readonly Color ImgBgColor = new Color(0.12f, 0.14f, 0.22f);

    private void BuildImageArea()
    {
        var go = new GameObject("MonsterImage");
        go.transform.SetParent(transform, false);
        go.AddComponent<RectTransform>();
        var imgLe = go.AddComponent<LayoutElement>();
        imgLe.preferredHeight = 150;
        imgLe.flexibleHeight = 0;
        // 背景色
        go.AddComponent<Image>().color = ImgBgColor;

        // スプライトシートから画像を取得
        var sprite = MonsterSpriteLoader.GetSprite(unit.monster.baseData.monsterType);
        if (sprite != null)
        {
            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(go.transform, false);
            var srt = spriteGo.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            var sImg = spriteGo.AddComponent<Image>();
            sImg.sprite = sprite;
            sImg.preserveAspect = true;
            sImg.color = Color.white;
            sImg.raycastTarget = false;

            bool isLeft = MonsterSpriteLoader.IsLeftFacing(unit.monster.baseData.monsterType);
            bool needFlip;
            if (unit.isPlayerSide)
                needFlip = !isLeft;
            else
                needFlip = isLeft;
            if (needFlip)
                spriteGo.transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    private void BuildHpBar()
    {
        var go = new GameObject("HpBarBg");
        go.transform.SetParent(transform, false);
        go.AddComponent<RectTransform>();
        var hpLe = go.AddComponent<LayoutElement>();
        hpLe.preferredHeight = 10;
        hpLe.minHeight = 10;
        hpLe.flexibleHeight = 0;
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f);

        var fillGo = new GameObject("HpBarFill");
        fillGo.transform.SetParent(go.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        hpBarFill = fillGo.AddComponent<Image>();
        hpBarFill.color = new Color(0.3f, 0.8f, 0.3f);
    }

    private TMP_Text BuildLabel(string goName, string text, float fontSize, FontStyles style, Color color, float height)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<LayoutElement>().preferredHeight = height;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Truncate;
        return tmp;
    }

    public void Refresh()
    {
        if (unit == null) return;

        // HPバー
        if (hpBarFill != null)
        {
            float ratio = Mathf.Clamp01((float)unit.monster.currentHp / unit.monster.maxHp);
            hpBarFill.rectTransform.anchorMax = new Vector2(ratio, 1);
            // HP低下で色を変える
            if (ratio > 0.5f) hpBarFill.color = new Color(0.3f, 0.8f, 0.3f);
            else if (ratio > 0.2f) hpBarFill.color = new Color(0.9f, 0.7f, 0.2f);
            else hpBarFill.color = new Color(0.9f, 0.2f, 0.2f);
        }

        // パラメータ
        if (paramText != null)
        {
            var m = unit.monster;
            paramText.text = $"HP:{m.currentHp}/{m.maxHp} ATK:{m.currentAttack} 射程:{m.currentRange}";
        }

        // 状態異常
        if (statusText != null)
        {
            string status = "";
            if (unit.HasStatusEffect(StatusEffectType.Poison)) status += "毒 ";
            if (unit.HasStatusEffect(StatusEffectType.Stun)) status += "ピヨリ ";
            if (unit.HasStatusEffect(StatusEffectType.MagicBarrier)) status += "バリア ";
            statusText.text = status;
        }

        // 背景色
        if (background != null)
        {
            if (Time.time < flashEndTime)
            {
                background.color = flashColor;
            }
            else if (isActing)
            {
                background.color = new Color(1f, 0.85f, 0f);
            }
            else if (!unit.isAlive)
            {
                background.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
            else if (unit.isPlayerSide)
            {
                background.color = new Color(0.5f, 0.7f, 1f);
            }
            else
            {
                background.color = new Color(1f, 0.5f, 0.5f);
            }
        }
    }

    public void SetActing(bool acting)
    {
        isActing = acting;
    }

    public void Flash(Color color, float duration = 0.2f)
    {
        flashColor = color;
        flashEndTime = Time.time + duration;
    }

    public void ShowDamagePopup(int damage)
    {
        Flash(new Color(1f, 0.2f, 0.2f), 0.3f);
        StartCoroutine(PopupCoroutine($"-{damage}", new Color(1f, 0.2f, 0.2f), 48));
    }

    public void ShowEffectText(string text, Color color)
    {
        StartCoroutine(PopupCoroutine(text, color, 42));
    }

    private IEnumerator PopupCoroutine(string text, Color color, float fontSize)
    {
        var go = new GameObject("Popup");
        go.transform.SetParent(transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, 10);
        rt.sizeDelta = new Vector2(260, 80);
        var le = go.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.outlineWidth = 0.4f;
        tmp.outlineColor = new Color32(0, 0, 0, 220);

        float elapsed = 0;
        float duration = 1.5f;
        Vector2 startPos = rt.anchoredPosition;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rt.anchoredPosition = startPos + new Vector2(0, t * 40);
            float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) / 0.5f;
            tmp.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        Destroy(go);
    }
}
