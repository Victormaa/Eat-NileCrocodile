using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
/// <summary>
/// 开场导演：主标题 → 任意键开始 → 标题淡出 + 摄像机慢移 + 文案淡入 →
/// 文案完成后快移到 (0,0,-10) → 停顿 → 淡入主游戏 UI → 启动 MainGamePlaySequence。
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
    [TextArea(2, 6)]
    public string storyMessage = "很久很久以前，尼罗河畔的鳄鱼们正在等待迁徙的角马……";
    public string startPromptMessage = "按任意键开始游戏";

    [Header("摄像机")]
    public Vector3 cameraEndPosition = new Vector3(0f, 0f, -10f);
    [Tooltip("慢移总时长（可被文案淡入结束打断）")]
    public float cameraSlowDuration = 8f;
    [Tooltip("文案淡入结束后的快移时长")]
    public float cameraSnapDuration = 0.12f;

    [Header("淡入淡出")]
    public float titleFadeOutDuration = 1f;
    public float storyFadeInDuration = 3f;
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

        if (storyText != null && !string.IsNullOrEmpty(storyMessage))
        {
            storyText.text = storyMessage;
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

        // 任意键盘 / 鼠标按键
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

        // 标题淡出（不等待）
        if (titleGroup != null)
        {
            titleGroup.interactable = false;
            titleGroup.blocksRaycasts = false;
            StartCoroutine(FadeGroup(titleGroup, 0f, titleFadeOutDuration));
        }

        // 摄像机慢移（可被打断）
        if (introCamera != null && cameraSlowDuration > 0f)
        {
            cameraSlowRoutine = StartCoroutine(
                MoveCamera(camStart, cameraEndPosition, cameraSlowDuration)
            );
        }

        // 等待文案淡入完成
        if (storyGroup != null)
        {
            storyGroup.gameObject.SetActive(true);
            yield return FadeGroup(storyGroup, 1f, storyFadeInDuration);
        }

        // 停慢移，快移到终点
        if (cameraSlowRoutine != null)
        {
            StopCoroutine(cameraSlowRoutine);
            cameraSlowRoutine = null;
        }

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

        // 淡入主游戏 UI
        if (mainGameUiGroup != null)
        {
            yield return FadeGroup(mainGameUiGroup, 1f, mainUiFadeInDuration);
            mainGameUiGroup.interactable = true;
            mainGameUiGroup.blocksRaycasts = true;
        }

        // 收起开场 UI
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
}
