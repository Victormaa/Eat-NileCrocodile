using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Sirenix.OdinInspector;

public enum UpgradeTarget
{
    Stealth,
    CatchSpeed,
    CatchCoolDown,
}

[Serializable]
public class StatUpgradeStep
{
    [Tooltip("本级消耗；可填多项 = 同时扣多种资源")]
    public MultiClickCostEntry[] costs;
    [Tooltip("本级升级后，目标属性变化量（Stealth 取整；CatchSpeed 增加速度；CatchCoolDown 减少冷却秒数）")]
    public float valueIncrease = 1f;
}

/// <summary>
/// 升级按钮反馈：按 upgradeSteps 配置每级消耗与加值；不够资源 / 满级 / 成功 三类文案与音效。
/// Button OnClick 绑 TryUpgrade()。
/// </summary>
public class StatUpgradeButton : MonoBehaviour
{
    [Header("升级配置")]
    [EnumToggleButtons]
    public UpgradeTarget upgradeTarget = UpgradeTarget.Stealth;
    [Tooltip("表有几项就能升几级；每级可配多种消耗与增加数值")]
    public StatUpgradeStep[] upgradeSteps;

    [Header("提示 Text")]
    public TMP_Text hintText;
    [Tooltip("完全可见后停留多久")]
    public float hintDuration = 2f;
    public float hintFadeInDuration = 0.25f;
    public float hintFadeOutDuration = 0.25f;
    [Tooltip("上浮 / 下浮的像素距离")]
    public float hintFloatDistance = 30f;

    [Header("文案")]
    [TextArea]
    public string notEnoughMessage;
    [TextArea]
    public string maxLevelMessage = "已满级";
    [TextArea]
    public string successMessage = "升级成功";

    [Header("音效 - 资源不足")]
    [Tooltip("需与 Resources/Audios/SFXs 中 clip 名一致；空则跳过")]
    public string notEnoughSoundId;
    public float notEnoughSoundVolume = 1f;

    [Header("音效 - 已满级")]
    public string maxLevelSoundId;
    public float maxLevelSoundVolume = 1f;

    [Header("音效 - 升级成功")]
    public string successSoundId;
    public float successSoundVolume = 1f;

    private Coroutine hintRoutine;
    private RectTransform hintRect;
    private Vector2 hintBasePos;
    private bool hasHintBasePos;
    private Color hintBaseColor;
    private string pendingHintMessage;

    void Awake()
    {
        if (string.IsNullOrEmpty(notEnoughMessage))
        {
            notEnoughMessage = "资源不足";
        }
    }

    /// <summary>按钮入口：尝试升级并播放对应反馈。</summary>
    public void TryUpgrade()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("StatUpgradeButton: GameManager 不存在。", this);
            return;
        }

        UpgradeResult result = AttemptUpgrade();

        switch (result)
        {
            case UpgradeResult.Success:
                ShowFeedback(successMessage, successSoundId, successSoundVolume);
                break;
            case UpgradeResult.MaxLevel:
                ShowFeedback(maxLevelMessage, maxLevelSoundId, maxLevelSoundVolume);
                break;
            default:
                ShowFeedback(notEnoughMessage, notEnoughSoundId, notEnoughSoundVolume);
                break;
        }
    }

    private UpgradeResult AttemptUpgrade()
    {
        if (IsMaxUpgrade())
        {
            return UpgradeResult.MaxLevel;
        }

        StatUpgradeStep step = upgradeSteps[GetCurrentLevelIndex()];
        MultiClickCostEntry[] costs = step != null ? step.costs : null;
        float valueIncrease = step != null ? step.valueIncrease : 0f;

        return upgradeTarget switch
        {
            UpgradeTarget.Stealth => GameManager.Instance.TryUpgradeStealth(costs, valueIncrease),
            UpgradeTarget.CatchSpeed => GameManager.Instance.TryUpgradeCatchSpeed(costs, valueIncrease),
            UpgradeTarget.CatchCoolDown => GameManager.Instance.TryUpgradeCatchCoolDown(costs, valueIncrease),
            _ => UpgradeResult.NotEnoughResource,
        };
    }

    private bool IsMaxUpgrade()
    {
        if (upgradeSteps == null || upgradeSteps.Length == 0)
        {
            return true;
        }

        return GetCurrentLevelIndex() >= upgradeSteps.Length;
    }

    private int GetCurrentLevelIndex()
    {
        if (GameManager.Instance == null) return 0;

        return upgradeTarget switch
        {
            UpgradeTarget.Stealth => GameManager.Instance.StealthUpgradeCount,
            UpgradeTarget.CatchSpeed => GameManager.Instance.CatchSpeedUpgradeCount,
            UpgradeTarget.CatchCoolDown => GameManager.Instance.CatchCoolDownUpgradeCount,
            _ => 0,
        };
    }

    private void ShowFeedback(string message, string soundId, float volume)
    {
        PlayOneShot(soundId, volume);

        if (hintText == null) return;

        pendingHintMessage = message;

        if (hintRoutine != null)
        {
            StopCoroutine(hintRoutine);
            hintRoutine = null;
            ResetHintVisual();
        }

        hintRoutine = StartCoroutine(HintRoutine());
    }

    private IEnumerator HintRoutine()
    {
        CacheHintBase();

        hintText.text = pendingHintMessage;
        hintText.gameObject.SetActive(true);

        Vector2 startPos = hintBasePos + Vector2.down * hintFloatDistance;
        Vector2 endPos = hintBasePos;

        yield return AnimateHint(
            startPos,
            endPos,
            0f,
            hintBaseColor.a,
            hintFadeInDuration
        );

        if (hintDuration > 0f)
        {
            yield return new WaitForSeconds(hintDuration);
        }

        Vector2 exitPos = hintBasePos + Vector2.down * hintFloatDistance;
        yield return AnimateHint(
            endPos,
            exitPos,
            hintBaseColor.a,
            0f,
            hintFadeOutDuration
        );

        hintText.gameObject.SetActive(false);
        ResetHintVisual();
        hintRoutine = null;
    }

    private IEnumerator AnimateHint(
        Vector2 fromPos,
        Vector2 toPos,
        float fromAlpha,
        float toAlpha,
        float duration
    )
    {
        if (duration <= 0f)
        {
            SetHintPos(toPos);
            SetHintAlpha(toAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetHintPos(Vector2.LerpUnclamped(fromPos, toPos, t));
            SetHintAlpha(Mathf.LerpUnclamped(fromAlpha, toAlpha, t));
            yield return null;
        }

        SetHintPos(toPos);
        SetHintAlpha(toAlpha);
    }

    private void CacheHintBase()
    {
        if (hintRect == null)
        {
            hintRect = hintText.rectTransform;
        }

        if (!hasHintBasePos)
        {
            hintBasePos = hintRect.anchoredPosition;
            hintBaseColor = hintText.color;
            hasHintBasePos = true;
        }
    }

    private void ResetHintVisual()
    {
        if (hintText == null) return;
        CacheHintBase();
        SetHintPos(hintBasePos);
        SetHintAlpha(0f);
    }

    private void SetHintPos(Vector2 pos)
    {
        if (hintRect != null)
        {
            hintRect.anchoredPosition = pos;
        }
    }

    private void SetHintAlpha(float alpha)
    {
        Color c = hintBaseColor;
        c.a = alpha;
        hintText.color = c;
    }

    private static void PlayOneShot(string soundId, float volume)
    {
        if (string.IsNullOrEmpty(soundId)) return;
        if (AudioController.Instance == null) return;

        AudioController.Instance.PlaySound2D(soundId, volume: volume);
    }

    void OnDisable()
    {
        if (hintRoutine != null)
        {
            StopCoroutine(hintRoutine);
            hintRoutine = null;
            ResetHintVisual();
            if (hintText != null)
            {
                hintText.gameObject.SetActive(false);
            }
        }
    }
}
