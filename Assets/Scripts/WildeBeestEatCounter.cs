using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 独立计数：累计成功吃掉的角马数量。不进 GameManager。
/// </summary>
public class WildeBeestEatCounter : MonoBehaviour
{
    private static WildeBeestEatCounter instance;

    public static WildeBeestEatCounter Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<WildeBeestEatCounter>();
                if (instance == null)
                    Debug.LogError("场景里没有 WildeBeestEatCounter 实例。");
            }
            return instance;
        }
    }

    [SerializeField]
    private int count;

    [Header("显示（可选）")]
    public TMP_Text countText;
    [Tooltip("例如 Eaten: {0}")]
    public string countFormat = "{0}";

    /// <summary>吃掉数量变化时回调，参数为当前累计数量。</summary>
    public event Action<int> OnEatenChanged;

    public int Count => count;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        RefreshCountUI();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>成功吃掉一只角马时调用。</summary>
    public void RegisterEat()
    {
        count++;
        RefreshCountUI();
        OnEatenChanged?.Invoke(count);
    }

    public void RefreshCountUI()
    {
        if (countText == null) return;
        countText.text = string.Format(countFormat, count);
    }
}
