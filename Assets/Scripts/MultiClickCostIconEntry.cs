using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级消耗单条显示：图标 + x数量。
/// </summary>
public class MultiClickCostIconEntry : MonoBehaviour
{
    public Image icon;
    public TMP_Text amountText;

    public void Setup(Sprite sprite, string amountLabel)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (amountText != null)
        {
            amountText.text = amountLabel;
        }
    }
}
