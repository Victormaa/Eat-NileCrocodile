using UnityEngine;

/// <summary>
/// 复合步剧本资产：中/英可各做一份，场景里只拖引用。
/// Create → ClickerGame → Composite Play Script
/// </summary>
[CreateAssetMenu(
    fileName = "PlayScript",
    menuName = "ClickerGame/Composite Play Script",
    order = 0)]
public class CompositePlayScript : ScriptableObject
{
    [Tooltip("按顺序执行的复合步")]
    public CompositePlayStep[] steps;
}
