using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class IntroStoryLine
{
    [TextArea(1, 4)]
    public string text;
    [Tooltip("完全可见后停留多久")]
    public float holdDuration = 2.5f;
}

/// <summary>
/// 开场导演：主标题 → 任意键开始 → 标题淡出 + 摄像机慢移 + 多分句文案 →
/// 文案全部结束后播音效并快移到 (0,0,-10) → 停顿 → 淡入主游戏 UI → 启动 MainGamePlaySequence。
/// </summary>
public class GameIntroSequence : MonoBehaviour
{
    [Header("引用")]
    public Camera introCamera;
    public CanvasGroup titleGroup;
    public CanvasGroup storyGroup;
    public CanvasGroup mainGameUiGroup;
    public MainGamePlaySequence mainGamePlaySequence;
    public TMP_Text storyText;
    [Tooltip("显示「按任意键开始游戏」的提示文字")]
    public TMP_Text startPromptText;

    [Header("文案")]
    public IntroStoryLine[] storyLines =
    {
        new IntroStoryLine
        {
            text = "很久很久以前，尼罗河畔的鳄鱼们正在等待迁徙的角马……",
            holdDuration = 2.5f,
        },
        new IntroStoryLine
        {
            text = "雨季将至，它们必须抓紧时间捕猎。",
            holdDuration = 2.5f,
        },
        new IntroStoryLine
        {
            text = "而你，就是那条名叫 Nile 的小鳄鱼。",
            holdDuration = 2.5f,
        },
    };
    public float storyLineFadeInDuration = 0.35f;
    public float storyLineFadeOutDuration = 0.35f;
    public string startPromptMessage = "按任意键开始游戏";

    [Header("摄像机")]
    public Vector3 cameraEndPosition = new Vector3(0f, 0f, -10f);
    [Tooltip("慢移总时长（可被文案全部播完打断）")]
    public float cameraSlowDuration = 8f;
    [Tooltip("文案全部结束后的快移时长")]
    public float cameraSnapDuration = 0.12f;

    [Header("移动前音效")]
    [Tooltip("需与 Resources/Audios/SFXs 中 clip 名一致；空则跳过")]
    public string beforeSnapSoundId;
    public float beforeSnapSoundVolume = 1f;
    public UnityEvent onBeforeCameraSnap;

    [Header("淡入淡出")]
    public float titleFadeOutDuration = 1f;
    public float holdAfterSnap = 1f;
    public float mainUiFadeInDuration = 1f;

    [Header("事件")]
    public UnityEvent onIntroFinished;

    private Coroutine runningRoutine;
    private Coroutine cameraSlowRoutine;
    private bool started;

    private void Start()
    {
        if (introCamera == null)
        {
            introCamera = Camera.main;
        }

        if (startPromptText != null && !string.IsNullOrEmpty(startPromptMessage))
        {
            startPromptText.text = startPromptMessage;
        }

        ApplyIntroStartState();
    }

    private void Update()
    {
        if (started || runningRoutine != null)
        {
            return;
        }

        if (Input.anyKeyDown)
        {
            OnStartGame();
        }
    }

    private void ApplyIntroStartState()
    {
        SetGroup(titleGroup, 1f, interactable: true, blocksRaycasts: true);
        SetGroup(storyGroup, 0f, interactable: false, blocksRaycasts: false);
        SetGroup(mainGameUiGroup, 0f, interactable: false, blocksRaycasts: false);

        if (titleGroup != null)
        {
            titleGroup.gameObject.SetActive(true);
        }

        if (storyGroup != null)
        {
            storyGroup.gameObject.SetActive(true);
        }

        if (storyText != null)
        {
            storyText.text = string.Empty;
        }
    }

    /// <summary>任意键触发后进入开场演出。</summary>
    public void OnStartGame()
    {
        if (started || runningRoutine != null)
        {
            return;
        }

        started = true;
        runningRoutine = StartCoroutine(RunIntroAfterStart());
    }

    private IEnumerator RunIntroAfterStart()
    {
        if (introCamera == null)
        {
            introCamera = Camera.main;
        }

        Vector3 camStart = introCamera != null
            ? introCamera.transform.position
            : new Vector3(0f, 122f, -10f);

        if (titleGroup != null)
        {
            titleGroup.interactable = false;
            titleGroup.blocksRaycasts = false;
            StartCoroutine(FadeGroup(titleGroup, 0f, titleFadeOutDuration));
        }

        if (introCamera != null && cameraSlowDuration > 0f)
        {
            cameraSlowRoutine = StartCoroutine(
                MoveCamera(camStart, cameraEndPosition, cameraSlowDuration)
            );
        }

        // 多分句全部播完后再快移
        yield return PlayAllStoryLines();

        if (cameraSlowRoutine != null)
        {
            StopCoroutine(cameraSlowRoutine);
            cameraSlowRoutine = null;
        }

        onBeforeCameraSnap?.Invoke();
        PlayOneShot(beforeSnapSoundId, beforeSnapSoundVolume);

        if (introCamera != null)
        {
            yield return MoveCamera(
                introCamera.transform.position,
                cameraEndPosition,
                cameraSnapDuration
            );
        }

        if (holdAfterSnap > 0f)
        {
            yield return new WaitForSeconds(holdAfterSnap);
        }

        if (mainGameUiGroup != null)
        {
            yield return FadeGroup(mainGameUiGroup, 1f, mainUiFadeInDuration);
            mainGameUiGroup.interactable = true;
            mainGameUiGroup.blocksRaycasts = true;
        }

        if (titleGroup != null)
        {
            titleGroup.gameObject.SetActive(false);
        }

        if (storyGroup != null)
        {
            storyGroup.gameObject.SetActive(false);
        }

        if (mainGamePlaySequence != null)
        {
            mainGamePlaySequence.Play();
        }

        onIntroFinished?.Invoke();
        runningRoutine = null;
    }

    private IEnumerator PlayAllStoryLines()
    {
        if (storyGroup == null || storyText == null || storyLines == null || storyLines.Length == 0)
        {
            yield break;
        }

        storyGroup.gameObject.SetActive(true);
        SetGroup(storyGroup, 0f, interactable: false, blocksRaycasts: false);

        for (int i = 0; i < storyLines.Length; i++)
        {
            IntroStoryLine line = storyLines[i];
            if (line == null || string.IsNullOrEmpty(line.text))
            {
                continue;
            }

            storyText.text = line.text;
            yield return FadeGroup(storyGroup, 1f, storyLineFadeInDuration);

            if (line.holdDuration > 0f)
            {
                yield return new WaitForSeconds(line.holdDuration);
            }

            yield return FadeGroup(storyGroup, 0f, storyLineFadeOutDuration);
        }
    }

    private IEnumerator FadeGroup(CanvasGroup group, float targetAlpha, float duration)
    {
        if (group == null)
        {
            yield break;
        }

        float startAlpha = group.alpha;
        if (duration <= 0f)
        {
            group.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, t);
            yield return null;
        }

        group.alpha = targetAlpha;
    }

    private IEnumerator MoveCamera(Vector3 from, Vector3 to, float duration)
    {
        if (introCamera == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            introCamera.transform.position = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float s = t * t * (3f - 2f * t);
            introCamera.transform.position = Vector3.LerpUnclamped(from, to, s);
            yield return null;
        }

        introCamera.transform.position = to;
    }

    private static void SetGroup(
        CanvasGroup group,
        float alpha,
        bool interactable,
        bool blocksRaycasts
    )
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = interactable;
        group.blocksRaycasts = blocksRaycasts;
    }

    private static void PlayOneShot(string soundId, float volume)
    {
        if (string.IsNullOrEmpty(soundId)) return;
        if (AudioController.Instance == null) return;

        AudioController.Instance.PlaySound2D(soundId, volume: volume);
    }
}
