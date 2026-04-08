using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUnitView : MonoBehaviour
{
    private TMP_Text nameText;
    private Slider hpBar;
    private TMP_Text hpText;
    private TMP_Text statusText;
    private Image background;
    private BattleUnit unit;

    public void Bind(BattleUnit unit)
    {
        this.unit = unit;

        // 子オブジェクトから自動検出
        nameText = transform.Find("NameText")?.GetComponent<TMP_Text>();
        hpBar = GetComponentInChildren<Slider>();
        hpText = transform.Find("HpText")?.GetComponent<TMP_Text>();
        statusText = transform.Find("StatusText")?.GetComponent<TMP_Text>();
        background = GetComponent<Image>();

        if (nameText != null)
            nameText.text = unit.monster.baseData.monsterName;

        Refresh();
    }

    public void Refresh()
    {
        if (unit == null) return;

        if (hpBar != null)
        {
            hpBar.maxValue = unit.monster.maxHp;
            hpBar.value = unit.monster.currentHp;
        }

        if (hpText != null)
            hpText.text = $"HP:{unit.monster.currentHp}/{unit.monster.maxHp} ATK:{unit.monster.currentAttack}";

        if (statusText != null)
        {
            string status = "";
            if (unit.HasStatusEffect(StatusEffectType.Poison)) status += "毒 ";
            if (unit.HasStatusEffect(StatusEffectType.Stun)) status += "ピヨリ ";
            if (unit.HasStatusEffect(StatusEffectType.MagicBarrier)) status += "魔法バリア ";
            statusText.text = status;
        }

        if (background != null)
        {
            if (!unit.isAlive)
                background.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            else if (unit.isPlayerSide)
                background.color = new Color(0.5f, 0.7f, 1f);
            else
                background.color = new Color(1f, 0.5f, 0.5f);
        }
    }
}
