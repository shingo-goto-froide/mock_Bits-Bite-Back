using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FormationSlotUI : MonoBehaviour
{
    private TMP_Text slotText;
    private Image slotImage;
    private int slotIndex;
    private System.Action<int> onClicked;
    private GameObject cardContent;

    private const float ImgSize = 80f;
    private const float ImgMargin = 6f;

    public void Init(int index, System.Action<int> callback)
    {
        slotIndex = index;
        onClicked = callback;

        slotText = GetComponentInChildren<TMP_Text>();
        slotImage = GetComponent<Image>();

        var btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(() => onClicked?.Invoke(slotIndex));

        Clear();
    }

    public void SetMonster(MonsterInstance monster)
    {
        if (monster != null)
        {
            if (slotText != null)
                slotText.gameObject.SetActive(false);

            // 既存カード内容を破棄して再生成
            if (cardContent != null)
                Destroy(cardContent);

            cardContent = new GameObject("CardContent");
            cardContent.transform.SetParent(transform, false);
            var rt = cardContent.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // 画像（左側固定）
            var imgGo = new GameObject("Image");
            imgGo.transform.SetParent(cardContent.transform, false);
            var imgRt = imgGo.AddComponent<RectTransform>();
            imgRt.anchorMin = new Vector2(0, 0.5f);
            imgRt.anchorMax = new Vector2(0, 0.5f);
            imgRt.pivot = new Vector2(0.5f, 0.5f);
            imgRt.anchoredPosition = new Vector2(ImgMargin + ImgSize * 0.5f, 0);
            imgRt.sizeDelta = new Vector2(ImgSize, ImgSize);
            imgGo.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.22f);
            var sprite = MonsterSpriteLoader.GetSprite(monster.baseData.monsterType);
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
                if (MonsterSpriteLoader.IsLeftFacing(monster.baseData.monsterType))
                    spriteGo.transform.localScale = new Vector3(-1, 1, 1);
            }

            // テキスト（右側）
            var info = new GameObject("Info");
            info.transform.SetParent(cardContent.transform, false);
            var infoRt = info.AddComponent<RectTransform>();
            float left = ImgMargin + ImgSize + ImgMargin;
            infoRt.anchorMin = Vector2.zero; infoRt.anchorMax = Vector2.one;
            infoRt.offsetMin = new Vector2(left, 4);
            infoRt.offsetMax = new Vector2(-4, -4);
            var vlg = info.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 1; vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            var d = monster.baseData;
            var rc = monster.GetRankColor();
            AddText(info.transform, "Name", $"{d.monsterName} <color=#{ColorUtility.ToHtmlStringRGB(rc)}>[{monster.GetRankLabel()}]</color>", 16, FontStyles.Bold, Color.white);
            var eff = monster.GetEffectiveStats();
            AddText(info.transform, "Stats",
                $"HP:{monster.currentHp}/{eff.hp} ATK:{eff.atk} {monster.age}年",
                13, FontStyles.Normal, new Color(0.8f, 0.8f, 0.9f));
            AddText(info.transform, "Ability", d.abilityDescription,
                11, FontStyles.Italic, new Color(0.65f, 0.75f, 0.9f));
            if (monster.HasSoulEquipped())
            {
                var soul = monster.equippedSoul;
                var tc = soul.GetTypeColor();
                AddText(info.transform, "Soul",
                    $"魂:{soul.GetName()}[{soul.rank}]",
                    11, FontStyles.Normal, new Color(tc.r, tc.g, tc.b, 1f));
            }

            if (slotImage != null)
                slotImage.color = new Color(0.18f, 0.25f, 0.38f);
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        if (cardContent != null)
        {
            Destroy(cardContent);
            cardContent = null;
        }

        if (slotText != null)
        {
            slotText.gameObject.SetActive(true);
            slotText.text = $"スロット{slotIndex + 1}\n(空き)";
        }
        if (slotImage != null)
            slotImage.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    }

    private void AddText(Transform parent, string name, string text, float size, FontStyles style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<LayoutElement>().minHeight = size + 4;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.richText = true;
    }
}
