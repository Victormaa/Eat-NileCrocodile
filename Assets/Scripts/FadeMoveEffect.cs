using System.Collections;
using UnityEngine;

/// <summary>
/// 子物体 A 从起点淡入并恒速移向 B，接近 B 时淡出；期间循环音效与粒子。
/// 每次调用 PlayFromStart() 都会从起始点重新出发。
/// </summary>
public class FadeMoveEffect : MonoBehaviour
{
    [Header("移动")]
    [Tooltip("要淡入并移动的子物体 A")]
    public Transform childA;
    [Tooltip("目标位置：子物体 B")]
    public Transform childB;
    public float moveSpeed = 3f;
    public float arriveThreshold = 0.01f;

    [Header("淡入 / 淡出")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    [Tooltip("距离 B 小于该值时开始淡出")]
    public float fadeOutStartDistance = 1f;
    [Tooltip("优先使用；为空则自动在 childA 上找 SpriteRenderer")]
    public SpriteRenderer fadeRenderer;

    [Header("循环音效接口")]
    [Tooltip("需与 Resources/Audios/SFXs 中 clip 名一致")]
    public string loopSoundId;
    public float loopSoundVolume = 1f;

    [Header("粒子接口")]
    public ParticleSystem moveParticles;

    private AudioSource activeLoopSource;
    private Vector3 startPosition;
    private bool hasStartPosition;
    private Coroutine runningRoutine;
    private SpriteRenderer cachedRenderer;

    void Awake()
    {
        CacheStartPosition();
        CacheRenderer();
    }

    private void Start()
    {
        SetAlpha(0f);
    }

    /// <summary>
    /// 公开接口：每次调用都让 A 从起始点重新出发移动一次。
    /// </summary>
    public void PlayFromStart()
    {
        if (runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
            runningRoutine = null;
        }

        StopLoopSound();
        StopMoveParticles();
        runningRoutine = StartCoroutine(PlayFadeMove());
    }

    /// <summary>兼容旧调用名。</summary>
    public void PlayRainProp()
    {
        PlayFromStart();
    }

    /// <summary>
    /// A 从起点淡入、恒速移向 B，接近 B 时淡出；同时循环音效与粒子。
    /// </summary>
    public IEnumerator PlayFadeMove()
    {
        if (childA == null || childB == null)
        {
            Debug.LogWarning("FadeMoveEffect: childA 或 childB 未赋值。");
            yield break;
        }

        if (!hasStartPosition)
        {
            CacheStartPosition();
        }

        CacheRenderer();

        childA.position = startPosition;
        childA.gameObject.SetActive(true);
        SetAlpha(0f);

        PlayLoopSound();
        PlayMoveParticles();

        float fadeInElapsed = 0f;
        bool fadingOut = false;
        float fadeOutElapsed = 0f;
        bool arrived = false;
        bool fadedOut = cachedRenderer == null;

        while (!arrived || !fadedOut)
        {
            if (!arrived)
            {
                childA.position = Vector3.MoveTowards(
                    childA.position,
                    childB.position,
                    moveSpeed * Time.deltaTime
                );

                float dist = Vector3.Distance(childA.position, childB.position);
                if (dist <= arriveThreshold)
                {
                    childA.position = childB.position;
                    arrived = true;
                }

                if (!fadingOut && dist <= fadeOutStartDistance)
                {
                    fadingOut = true;
                }
            }

            if (cachedRenderer != null)
            {
                if (fadingOut)
                {
                    fadeOutElapsed += Time.deltaTime;
                    float t = fadeOutDuration > 0f
                        ? Mathf.Clamp01(fadeOutElapsed / fadeOutDuration)
                        : 1f;
                    SetAlpha(1f - t);
                    if (t >= 1f)
                    {
                        fadedOut = true;
                    }
                }
                else
                {
                    fadeInElapsed += Time.deltaTime;
                    float t = fadeInDuration > 0f
                        ? Mathf.Clamp01(fadeInElapsed / fadeInDuration)
                        : 1f;
                    SetAlpha(t);
                }
            }
            else
            {
                fadedOut = arrived;
            }

            yield return null;
        }

        SetAlpha(0f);
        StopLoopSound();
        StopMoveParticles();
        runningRoutine = null;
    }

    private void CacheStartPosition()
    {
        if (childA == null) return;
        startPosition = childA.position;
        hasStartPosition = true;
    }

    private void CacheRenderer()
    {
        if (fadeRenderer != null)
        {
            cachedRenderer = fadeRenderer;
            return;
        }

        if (childA == null) return;

        cachedRenderer = childA.GetComponent<SpriteRenderer>();
        if (cachedRenderer == null)
        {
            cachedRenderer = childA.GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void SetAlpha(float alpha)
    {
        if (cachedRenderer == null) return;
        Color c = cachedRenderer.color;
        c.a = alpha;
        cachedRenderer.color = c;
    }

    private void PlayLoopSound()
    {
        if (string.IsNullOrEmpty(loopSoundId)) return;
        if (AudioController.Instance == null) return;

        activeLoopSource = AudioController.Instance.PlaySound2D(
            loopSoundId,
            volume: loopSoundVolume,
            looping: true
        );
    }

    private void StopLoopSound()
    {
        if (activeLoopSource != null)
        {
            activeLoopSource.Stop();
            Destroy(activeLoopSource.gameObject);
            activeLoopSource = null;
        }
    }

    private void PlayMoveParticles()
    {
        if (moveParticles == null) return;

        moveParticles.transform.position = childA != null ? childA.position : transform.position;
        moveParticles.Play();
    }

    private void StopMoveParticles()
    {
        if (moveParticles == null) return;
        moveParticles.Stop();
    }
}
