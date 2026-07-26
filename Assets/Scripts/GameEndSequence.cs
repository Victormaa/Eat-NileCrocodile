using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class EndFlavorTier
{
    [Tooltip("吃角马数 >= 该值时使用本档（从高到低匹配）")]
    public int minEaten;
    [TextArea(1, 3)]
    public string message;
}

/// <summary>
/// 结算页：天数归零时淡入 EndCanvas，展示捕获/金钱/皮 + 评价，任意键重开。
/// </summary>
public class GameEndSequence : MonoBehaviour
{
    [Header("引用")]
    public GameDayCountdown dayCountdown;
    public CanvasGroup endCanvasGroup;
    public TMP_Text titleText;
    public TMP_Text eatenText;
    public TMP_Text moneyText;
    public TMP_Text furText;
    public TMP_Text flavorText;
    public TMP_Text restartHintText;

    [Header("文案格式")]
    public string titleMessage = "季节结束";
    public string eatenFormat = "捕获角马：{0}";
    public string moneyFormat = "金钱：{0:F0}";
    public string furFormat = "鳄鱼皮：{0:F0}";
    public string restartHintMessage = "按任意键再来一次";

    [Header("评价档位（按 minEaten 从高到低匹配）")]
    public EndFlavorTier[] flavorTiers =
    {
        new EndFlavorTier { minEaten = 66, message = "皮包工厂开起来了，这一季很圆满！" },
        new EndFlavorTier { minEaten = 20, message = "Nile 吃得不错，河岸上热闹了一阵。" },
        new EndFlavorTier { minEaten = 0, message = "这一季有点辛苦，Nile 还要再练练。" },
    };

    [Header("动画")]
    public float dimFadeDuration = 0.35f;
    public float lineFadeDuration = 0.3f;
    public float delayBetweenLines = 0.15f;
    public float countUpDuration = 0.45f;
    public float restartHintDelay = 0.6f;

    [Header("事件")]
    public UnityEvent onEndShown;

    private bool ended;
    private bool canRestart;
    private Coroutine runningRoutine;

    private void Start()
    {
        if (dayCountdown == null)
            dayCountdown = FindObjectOfType<GameDayCountdown>();

        if (dayCountdown != null)
            dayCountdown.OnTimeUp += HandleTimeUp;

        ApplyHiddenState();
    }

    private void OnDestroy()
    {
        if (dayCountdown != null)
            dayCountdown.OnTimeUp -= HandleTimeUp;
    }

    private void Update()
    {
        if (!canRestart)
            return;

        if (Input.anyKeyDown)
            RestartScene();
    }

    private void ApplyHiddenState()
    {
        if (endCanvasGroup != null)
        {
            endCanvasGroup.alpha = 0f;
            endCanvasGroup.interactable = false;
            endCanvasGroup.blocksRaycasts = false;
            endCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void HandleTimeUp()
    {
        if (ended)
            return;

        ended = true;
        if (runningRoutine != null)
            StopCoroutine(runningRoutine);

        runningRoutine = StartCoroutine(ShowEndRoutine());
    }

    /// <summary>测试入口：手动弹出结算。</summary>
    public void ShowEndNow()
    {
        HandleTimeUp();
    }

    private IEnumerator ShowEndRoutine()
    {
        canRestart = false;

        if (dayCountdown != null)
            dayCountdown.PauseCountdown();

        int eaten = 0;
        WildeBeestEatCounter counter = FindObjectOfType<WildeBeestEatCounter>();
        if (counter != null)
            eaten = counter.Count;

        float money = 0f;
        float fur = 0f;
        if (GameManager.Instance != null)
        {
            money = GameManager.Instance.moneyValue;
            fur = GameManager.Instance.CrocoFur;
        }

        if (titleText != null)
            titleText.text = titleMessage;
        if (flavorText != null)
            flavorText.text = ResolveFlavor(eaten);
        if (restartHintText != null)
        {
            restartHintText.text = restartHintMessage;
            SetTextAlpha(restartHintText, 0f);
        }

        SetTextAlpha(titleText, 0f);
        SetTextAlpha(eatenText, 0f);
        SetTextAlpha(moneyText, 0f);
        SetTextAlpha(furText, 0f);
        SetTextAlpha(flavorText, 0f);

        if (eatenText != null)
            eatenText.text = string.Format(eatenFormat, 0);
        if (moneyText != null)
            moneyText.text = string.Format(moneyFormat, 0f);
        if (furText != null)
            furText.text = string.Format(furFormat, 0f);

        if (endCanvasGroup != null)
        {
            endCanvasGroup.gameObject.SetActive(true);
            endCanvasGroup.blocksRaycasts = true;
            endCanvasGroup.interactable = true;
            yield return FadeCanvasGroup(endCanvasGroup, 1f, dimFadeDuration);
        }

        yield return FadeText(titleText, 1f, lineFadeDuration);
        yield return new WaitForSeconds(delayBetweenLines);

        yield return RevealStat(eatenText, eatenFormat, eaten, true);
        yield return new WaitForSeconds(delayBetweenLines);

        yield return RevealStat(moneyText, moneyFormat, money, false);
        yield return new WaitForSeconds(delayBetweenLines);

        yield return RevealStat(furText, furFormat, fur, false);
        yield return new WaitForSeconds(delayBetweenLines);

        yield return FadeText(flavorText, 1f, lineFadeDuration);

        if (restartHintDelay > 0f)
            yield return new WaitForSeconds(restartHintDelay);

        yield return FadeText(restartHintText, 1f, lineFadeDuration);

        canRestart = true;
        onEndShown?.Invoke();
        runningRoutine = null;
    }

    private IEnumerator RevealStat(TMP_Text text, string format, float finalValue, bool asInt)
    {
        if (text == null)
            yield break;

        SetTextAlpha(text, 1f);

        if (countUpDuration <= 0f)
        {
            text.text = asInt
                ? string.Format(format, Mathf.RoundToInt(finalValue))
                : string.Format(format, finalValue);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < countUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / countUpDuration);
            float current = Mathf.Lerp(0f, finalValue, t);
            text.text = asInt
                ? string.Format(format, Mathf.RoundToInt(current))
                : string.Format(format, current);
            yield return null;
        }

        text.text = asInt
            ? string.Format(format, Mathf.RoundToInt(finalValue))
            : string.Format(format, finalValue);
    }

    private string ResolveFlavor(int eaten)
    {
        if (flavorTiers == null || flavorTiers.Length == 0)
            return string.Empty;

        EndFlavorTier best = null;
        for (int i = 0; i < flavorTiers.Length; i++)
        {
            EndFlavorTier tier = flavorTiers[i];
            if (tier == null) continue;
            if (eaten < tier.minEaten) continue;
            if (best == null || tier.minEaten > best.minEaten)
                best = tier;
        }

        return best != null ? best.message : string.Empty;
    }

    private void RestartScene()
    {
        canRestart = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup group, float target, float duration)
    {
        if (group == null)
            yield break;

        float start = group.alpha;
        if (duration <= 0f)
        {
            group.alpha = target;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.LerpUnclamped(start, target, t);
            yield return null;
        }

        group.alpha = target;
    }

    private static IEnumerator FadeText(TMP_Text text, float targetAlpha, float duration)
    {
        if (text == null)
            yield break;

        Color c = text.color;
        float start = c.a;
        if (duration <= 0f)
        {
            c.a = targetAlpha;
            text.color = c;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            c.a = Mathf.LerpUnclamped(start, targetAlpha, t);
            text.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        text.color = c;
    }

    private static void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null) return;
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }
}
