using Sirenix.OdinInspector;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public enum MainGamePlayStepType
{
    ShowTip,
    WaitSeconds,
    WaitEatenCount,
    WaitRainSuccess,
    UnlockObjects,
}

[Serializable]
public class MainGamePlayStep
{
    [DrawWithUnity]
    public MainGamePlayStepType stepType = MainGamePlayStepType.ShowTip;

    [TextArea]
    public string tipMessage;
    [Tooltip("ShowTip：淡入后停留多久再继续下一步（文案会一直留在画面上，直到下一句 Tip）")]
    public float tipDuration = 2.5f;

    [Tooltip("WaitSeconds：等待秒数")]
    public float waitSeconds = 1f;

    [Tooltip("WaitEatenCount：累计吃掉数量达到该值")]
    public int requiredEaten = 1;

    [Tooltip("UnlockObjects：要显示的物体")]
    public GameObject[] objectsToShow;
}

/// <summary>
/// 主玩法教学剧本：按步骤显示 Tips、等待条件、解锁 UI。
/// 独立于 RainHerdGoSequence / GameManager。
/// </summary>
public class MainGamePlaySequence : MonoBehaviour
{
    [Header("引用")]
    public TMP_Text tipText;
    public RainHerdGoSequence rainHerdGoSequence;

    [Header("Tips 动画")]
    public float tipFadeInDuration = 0.25f;
    public float tipFadeOutDuration = 0.25f;
    public float tipFloatDistance = 30f;

    [Header("开局隐藏")]
    [Tooltip("开局先 SetActive(false)；之后靠 UnlockObjects 步骤再显示")]
    public GameObject[] initiallyHidden;

    [Header("剧本")]
    public bool playOnStart = true;
    public MainGamePlayStep[] steps;

    private Coroutine runningRoutine;
    private RectTransform tipRect;
    private Vector2 tipBasePos;
    private bool hasTipBasePos;
    private Color tipBaseColor;
    private bool rainSucceeded;
    private bool tipShowing;

    private void Reset()
    {
        steps = CreateDefaultSteps();
    }

    private void OnEnable()
    {
        if (rainHerdGoSequence != null)
        {
            rainHerdGoSequence.PlaySucceeded += HandleRainPlaySucceeded;
        }
    }

    private void OnDisable()
    {
        if (rainHerdGoSequence != null)
        {
            rainHerdGoSequence.PlaySucceeded -= HandleRainPlaySucceeded;
        }

        if (runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
            runningRoutine = null;
        }

        ClearTipImmediate();
    }

    private void Start()
    {
        if (rainHerdGoSequence == null)
        {
            rainHerdGoSequence = FindObjectOfType<RainHerdGoSequence>();
            if (rainHerdGoSequence != null)
            {
                rainHerdGoSequence.PlaySucceeded += HandleRainPlaySucceeded;
            }
        }

        HideInitiallyHidden();
        HideUnlockTargetsAtStart();
        ClearTipImmediate();

        if (playOnStart)
        {
            Play();
        }
    }

    /// <summary>公开入口：从头播放剧本。</summary>
    public void Play()
    {
        if (runningRoutine != null)
        {
            return;
        }

        runningRoutine = StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("MainGamePlaySequence: steps 为空。", this);
            runningRoutine = null;
            yield break;
        }

        for (int i = 0; i < steps.Length; i++)
        {
            MainGamePlayStep step = steps[i];
            if (step == null) continue;

            switch (step.stepType)
            {
                case MainGamePlayStepType.ShowTip:
                    yield return ShowTipRoutine(step.tipMessage, step.tipDuration);
                    break;

                case MainGamePlayStepType.WaitSeconds:
                    if (step.waitSeconds > 0f)
                    {
                        yield return new WaitForSeconds(step.waitSeconds);
                    }
                    break;

                case MainGamePlayStepType.WaitEatenCount:
                    yield return WaitUntilEaten(step.requiredEaten);
                    break;

                case MainGamePlayStepType.WaitRainSuccess:
                    yield return WaitUntilRainSuccess();
                    break;

                case MainGamePlayStepType.UnlockObjects:
                    UnlockObjects(step.objectsToShow);
                    break;
            }
        }

        runningRoutine = null;
    }

    private IEnumerator WaitUntilEaten(int required)
    {
        WildeBeestEatCounter counter = WildeBeestEatCounter.Instance;
        if (counter == null)
        {
            Debug.LogWarning("MainGamePlaySequence: 找不到 WildeBeestEatCounter。", this);
            yield break;
        }

        if (counter.Count >= required)
        {
            yield break;
        }

        bool reached = false;
        void OnChanged(int count)
        {
            if (count >= required)
            {
                reached = true;
            }
        }

        counter.OnEatenChanged += OnChanged;
        while (!reached)
        {
            if (counter.Count >= required)
            {
                break;
            }
            yield return null;
        }
        counter.OnEatenChanged -= OnChanged;
    }

    private IEnumerator WaitUntilRainSuccess()
    {
        while (!rainSucceeded)
        {
            yield return null;
        }
    }

    private void HandleRainPlaySucceeded()
    {
        rainSucceeded = true;
    }

    private void HideInitiallyHidden()
    {
        SetObjectsActive(initiallyHidden, false);
    }

    private void HideUnlockTargetsAtStart()
    {
        if (steps == null) return;

        for (int i = 0; i < steps.Length; i++)
        {
            MainGamePlayStep step = steps[i];
            if (step == null || step.stepType != MainGamePlayStepType.UnlockObjects)
            {
                continue;
            }

            SetObjectsActive(step.objectsToShow, false);
        }
    }

    private static void UnlockObjects(GameObject[] objects)
    {
        SetObjectsActive(objects, true);
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(active);
            }
        }
    }

    private IEnumerator ShowTipRoutine(string message, float duration)
    {
        if (tipText == null || string.IsNullOrEmpty(message))
        {
            yield break;
        }

        CacheTipBase();

        // 已有 Tip 时先淡出，再淡入下一句；否则直接淡入并保持在画面上。
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
        {
            yield return new WaitForSeconds(duration);
        }
    }

    private void ClearTipImmediate()
    {
        tipShowing = false;
        ResetTipVisual();
        if (tipText != null)
        {
            tipText.gameObject.SetActive(false);
        }
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
        {
            tipRect = tipText.rectTransform;
        }

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
        {
            tipRect.anchoredPosition = pos;
        }
    }

    private void SetTipAlpha(float alpha)
    {
        if (tipText == null) return;
        Color c = tipBaseColor;
        c.a = alpha;
        tipText.color = c;
    }

    private static MainGamePlayStep[] CreateDefaultSteps()
    {
        return new[]
        {
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "角马不下水，怕鳄鱼，试试召唤云朵好了",
                tipDuration = 2.5f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.WaitRainSuccess,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "远处的乌云会让角马想吃美食而往前跑",
                tipDuration = 2.5f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "让Nile先吃到5只角马吧",
                tipDuration = 2.5f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.WaitSeconds,
                waitSeconds = 5f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "角马跑的好快，鳄鱼吃不着，我们试试干扰一下它们",
                tipDuration = 2.5f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.WaitEatenCount,
                requiredEaten = 5,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.UnlockObjects,
                objectsToShow = Array.Empty<GameObject>(),
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "提升一下抓捕速度吧，这样nile就可以自己抓到角马了",
                tipDuration = 3f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "也许吃到第18只角马，Nile们会很开心",
                tipDuration = 2.5f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.WaitEatenCount,
                requiredEaten = 18,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.UnlockObjects,
                objectsToShow = Array.Empty<GameObject>(),
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "隐藏能力的提高会让捕获更简单",
                tipDuration = 2f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "也许吃到第35只角马吧，留给我们的时间不多了",
                tipDuration = 2.5f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.WaitEatenCount,
                requiredEaten = 35,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.UnlockObjects,
                objectsToShow = Array.Empty<GameObject>(),
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "我们可以建造一家鳄鱼餐厅了，鳄鱼餐厅会把鳄鱼皮留下，鳄鱼皮是好东西呀~",
                tipDuration = 2.5f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.UnlockObjects,
                objectsToShow = Array.Empty<GameObject>(),
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.WaitSeconds,
                waitSeconds = 3f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "我们抓紧升级一下各项属性吧~ 时间不多了，还有x天了~",
                tipDuration = 3f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "我们试试看能不能抓到66只角马吧!",
                tipDuration = 3f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.WaitEatenCount,
                requiredEaten = 66,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "太好了我们好像可以开一个皮包工厂在这里了",
                tipDuration = 3f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.UnlockObjects,
                objectsToShow = Array.Empty<GameObject>(),
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.WaitSeconds,
                waitSeconds = 2f,
            },
            new MainGamePlayStep
            {
                stepType = MainGamePlayStepType.ShowTip,
                tipMessage = "太好了！我们抓紧升级一切听说满级鳄鱼是一个大魔王形态呢！",
                tipDuration = 2f,
            },
        };
    }
}
