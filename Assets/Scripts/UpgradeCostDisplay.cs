using TMPro;
using UnityEngine;

/// <summary>
/// 可复用的升级消耗显示：按 costs 生成「图标 x 数量」；满级显示文案。
/// 任意升级按钮挂上后调用 Refresh(costs, isMaxLevel)。
/// </summary>
public class UpgradeCostDisplay : MonoBehaviour
{
    [Header("升级消耗显示（图标 x 数量）")]
    [Tooltip("横向排布父节点，建议挂 Horizontal Layout Group")]
    public Transform upgradeCostRoot;
    [Tooltip("单条预制体：含 Image + TMP")]
    public MultiClickCostIconEntry costIconPrefab;
    public Sprite moneyIcon;
    public Sprite crocodileFatIcon;
    public Sprite crocoFurIcon;
    [Tooltip("满级时显示；未赋值则跳过")]
    public TMP_Text maxLevelCostText;
    [Tooltip("例如 x{0} → x3")]
    public string amountFormat = "x{0}";
    [Tooltip("满级文案")]
    public string maxLevelLabel = "已满级";

    /// <summary>按当前消耗列表刷新显示。</summary>
    public void Refresh(MultiClickCostEntry[] costs, bool isMaxLevel)
    {
        ClearCostEntries();

        if (maxLevelCostText != null)
        {
            maxLevelCostText.gameObject.SetActive(isMaxLevel);
            if (isMaxLevel)
            {
                maxLevelCostText.text = maxLevelLabel;
            }
        }

        if (upgradeCostRoot != null)
        {
            upgradeCostRoot.gameObject.SetActive(!isMaxLevel);
        }

        if (isMaxLevel || upgradeCostRoot == null || costIconPrefab == null)
        {
            return;
        }

        if (costs == null)
        {
            return;
        }

        for (int i = 0; i < costs.Length; i++)
        {
            MultiClickCostEntry cost = costs[i];
            if (cost == null) continue;

            MultiClickCostIconEntry entry = Instantiate(costIconPrefab, upgradeCostRoot);
            entry.Setup(GetIcon(cost.costType), string.Format(amountFormat, cost.amount));
        }
    }

    private void ClearCostEntries()
    {
        if (upgradeCostRoot == null) return;

        for (int i = upgradeCostRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(upgradeCostRoot.GetChild(i).gameObject);
        }
    }

    private Sprite GetIcon(UpgradeCostType costType)
    {
        switch (costType)
        {
            case UpgradeCostType.Money:
                return moneyIcon;
            case UpgradeCostType.CrocodileFat:
                return crocodileFatIcon;
            case UpgradeCostType.CrocoFur:
                return crocoFurIcon;
            default:
                return null;
        }
    }
}
