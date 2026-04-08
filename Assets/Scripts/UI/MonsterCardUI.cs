using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonsterCardUI : MonoBehaviour
{
    private MonsterInstance monster;
    private System.Action<MonsterInstance> onClicked;

    public void SetData(MonsterInstance monster, bool inFormation, System.Action<MonsterInstance> callback)
    {
        this.monster = monster;
        this.onClicked = callback;

        var texts = GetComponentsInChildren<TMP_Text>();
        // NameText, StatsText, AbilityText の順
        if (texts.Length > 0) texts[0].text = monster.baseData.monsterName;
        if (texts.Length > 1) texts[1].text = $"HP:{monster.currentHp}/{monster.maxHp} ATK:{monster.currentAttack} SPD:{monster.currentSpeed} 射程:{monster.currentRange} 枠:{monster.baseData.slotSize}";
        if (texts.Length > 2) texts[2].text = monster.baseData.abilityDescription;

        var bg = GetComponent<Image>();
        if (bg != null)
            bg.color = inFormation ? new Color(0.6f, 0.8f, 1f) : Color.white;

        var btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(() => onClicked?.Invoke(this.monster));
    }
}
