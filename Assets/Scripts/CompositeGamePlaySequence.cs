using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 复合步主玩法剧本播放器。剧本内容来自 CompositePlayScript；
/// 场景物体解锁通过 unlockBindings（unlockId → GameObjects）。
/// </summary>
public class CompositeGamePlaySequence : MonoBehaviour
{
    [Header("引用")]
    public TMP_Text tipText;
    public GameplayEventBus eventBus;

    [Header("剧本资产")]
    [Tooltip("拖入 Composite Play Script（中/英可换）")]
    public CompositePlayScript playScript;

    [Header("场景解锁映射")]
    [Tooltip("剧本 steps.unlockIds 对应的场景物体")]
    public SceneUnlockBinding[] unlockBindings;

    [Header("Tips 动画")]
    public float tipFadeInDuration = 0.25f;
    public float tipFadeOutDuration = 0.25f;
    public float tipFloatDistance = 30f;

    [Header("开局隐藏")]
    [Tooltip("开局先 SetActive(false)；之后靠步骤 Unlock 再显示")]
    public GameObject[] initiallyHidden;

    [Header("播放")]
    public bool playOnStart = false;
    [Tooltip("Play 时是否启动天数倒计时")]
    public bool startDayCountdownOnPlay = true;

    private Coroutine runningRoutine;
    private RectTransform tipRect;
    private Vector2 tipBasePos;
    private bool hasTipBasePos;
    private Color tipBaseColor;
    private bool tipShowing;
    private Dictionary<string, GameObject[]> unlockLookup;

    private CompositePlayStep[] Steps =>
        playScript != null ? playScript.steps : null;

    private void OnDisable()
    {
        if (runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
            runningRoutine = null;
        }
        ClearTipImmediate();
    }

    private void Start()
    {
        if (eventBus == null)
            eventBus = FindObjectOfType<GameplayEventBus>();

        BuildUnlockLookup();
        HideInitiallyHidden();
        HideUnlockTargetsAtStart();
        ClearTipImmediate();

        if (playOnStart)
            Play();
    }

    /// <summary>公开入口：从头播放剧本。</summary>
    public void Play()
    {
        if (runningRoutine != null)
            return;

        if (startDayCountdownOnPlay && GameDayCountdown.Instance != null)
            GameDayCountdown.Instance.StartCountdown();

        runningRoutine = StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        CompositePlayStep[] steps = Steps;
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("CompositeGamePlaySequence: playScript / steps 为空。", this);
            runningRoutine = null;
            yield break;
        }

        if (unlockLookup == null)
            BuildUnlockLookup();

        for (int i = 0; i < steps.Length; i++)
        {
            CompositePlayStep step = steps[i];
            if (step == null) continue;

            UnlockByIds(step.unlockIds);

            if (!string.IsNullOrEmpty(step.tipMessage))
                yield return ShowTipRoutine(step.tipMessage, step.tipDuration);

            if (step.waitSeconds > 0f)
            {
                yield return new WaitForSeconds(step.waitSeconds);
            }
            else if (!string.IsNullOrEmpty(step.waitPassEventId))
            {
                yield return WaitPass(step);
            }
        }

        runningRoutine = null;
    }

    private IEnumerator WaitPass(CompositePlayStep step)
    {
        string id = step.waitPassEventId;

        if (id == GameplayEventBus.EatenReached)
        {
            yield return WaitUntilEaten(step.waitPassRequiredEaten);
            yield break;
        }

        if (eventBus == null)
            eventBus = GameplayEventBus.Instance;

        if (eventBus == null)
        {
            Debug.LogWarning("CompositeGamePlaySequence: GameplayEventBus 不存在，WaitPass 跳过。", this);
            yield break;
        }

        yield return eventBus.WaitUntil(id);
    }

    private IEnumerator WaitUntilEaten(int required)
    {
        WildeBeestEatCounter counter = WildeBeestEatCounter.Instance;
        if (counter == null)
        {
            Debug.LogWarning("CompositeGamePlaySequence: 找不到 WildeBeestEatCounter。", this);
            yield break;
        }

        if (counter.Count >= required)
            yield break;

        bool reached = false;
        void OnChanged(int count)
        {
            if (count >= required)
                reached = true;
        }

        counter.OnEatenChanged += OnChanged;
        while (!reached)
        {
            if (counter.Count >= required)
                break;
            yield return null;
        }
        counter.OnEatenChanged -= OnChanged;
    }

    private void BuildUnlockLookup()
    {
        unlockLookup = new Dictionary<string, GameObject[]>();
        if (unlockBindings == null) return;

        for (int i = 0; i < unlockBindings.Length; i++)
        {
            SceneUnlockBinding binding = unlockBindings[i];
            if (binding == null || string.IsNullOrEmpty(binding.unlockId))
                continue;

            unlockLookup[binding.unlockId] = binding.objects;
        }
    }

    private void UnlockByIds(string[] ids)
    {
        if (ids == null || ids.Length == 0) return;
        if (unlockLookup == null)
            BuildUnlockLookup();

        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i];
            if (string.IsNullOrEmpty(id)) continue;

            if (unlockLookup != null && unlockLookup.TryGetValue(id, out GameObject[] objects))
            {
                SetObjectsActive(objects, true);
            }
            else
            {
                Debug.LogWarning($"CompositeGamePlaySequence: 未找到 unlockId「{id}」的绑定。", this);
            }
        }
    }

    private void HideInitiallyHidden()
    {
        SetObjectsActive(initiallyHidden, false);
    }

    private void HideUnlockTargetsAtStart()
    {
        // 隐藏绑定表里所有会出现在剧本中的物体（开局未解锁态）
        if (unlockBindings == null) return;

        for (int i = 0; i < unlockBindings.Length; i++)
        {
            SceneUnlockBinding binding = unlockBindings[i];
            if (binding == null) continue;
            SetObjectsActive(binding.objects, false);
        }
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }

    private IEnumerator ShowTipRoutine(string message, float duration)
    {
        if (tipText == null || string.IsNullOrEmpty(message))
            yield break;

        CacheTipBase();

        if (tipShowing)
        {
            Vector2 exitPos = tipBasePos + Vector2.down * tipFloatDistance;
            yield return AnimateTip(
                tipBasePos,
                exitPos,
                tipBaseColor.a,
                0f,
                tipFadeOutDuration
            );
        }

        tipText.text = message;
        tipText.gameObject.SetActive(true);

        Vector2 startPos = tipBasePos + Vector2.down * tipFloatDistance;
        yield return AnimateTip(
            startPos,
            tipBasePos,
            0f,
            tipBaseColor.a,
            tipFadeInDuration
        );

        tipShowing = true;

        if (duration > 0f)
            yield return new WaitForSeconds(duration);
    }

    private void ClearTipImmediate()
    {
        tipShowing = false;
        ResetTipVisual();
        if (tipText != null)
            tipText.gameObject.SetActive(false);
    }

    private IEnumerator AnimateTip(
        Vector2 fromPos,
        Vector2 toPos,
        float fromAlpha,
        float toAlpha,
        float duration
    )
    {
        if (duration <= 0f)
        {
            SetTipPos(toPos);
            SetTipAlpha(toAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetTipPos(Vector2.LerpUnclamped(fromPos, toPos, t));
            SetTipAlpha(Mathf.LerpUnclamped(fromAlpha, toAlpha, t));
            yield return null;
        }

        SetTipPos(toPos);
        SetTipAlpha(toAlpha);
    }

    private void CacheTipBase()
    {
        if (tipText == null) return;

        if (tipRect == null)
            tipRect = tipText.rectTransform;

        if (!hasTipBasePos)
        {
            tipBasePos = tipRect.anchoredPosition;
            tipBaseColor = tipText.color;
            hasTipBasePos = true;
        }
    }

    private void ResetTipVisual()
    {
        if (tipText == null) return;
        CacheTipBase();
        SetTipPos(tipBasePos);
        SetTipAlpha(0f);
    }

    private void SetTipPos(Vector2 pos)
    {
        if (tipRect != null)
            tipRect.anchoredPosition = pos;
    }

    private void SetTipAlpha(float alpha)
    {
        if (tipText == null) return;
        Color c = tipBaseColor;
        c.a = alpha;
        tipText.color = c;
    }
}
