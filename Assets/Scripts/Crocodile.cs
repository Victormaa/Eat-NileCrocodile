using System.Collections;
using UnityEngine;

public class Crocodile : MonoBehaviour
{
    [Header("抓取")]
    public float maxCatchableSpeed = 3.0f;
    public float approachSpeed = 12f;
    public float approachXThreshold = 0.5f;
    public float shrinkDuration = 0.8f;
    public float happyAnimationDuration = 0.5f;
    public float returnDelay = 1f;
    public float returnSpeed = 8f;

    [Header("压缩效果")]
    public float shrinkShakeAmount = 0.12f;
    public float shrinkShakeFrequency = 35f;
    [Tooltip("压缩时播放的粒子，可在 Inspector 拖入，未赋值则跳过")]
    public ParticleSystem shrinkParticles;
    [Tooltip("压缩音效名，需与 Resources/Audios/SFXs 中 clip 名一致")]
    public string shrinkSoundId;
    public float shrinkSoundVolume = 1f;

    private bool isBusy;
    private WildeBeestBehavior currentPrey;
    private Vector3 homePosition;
    private Vector3 homeScale;

    void Awake()
    {
        homePosition = transform.position;
        homeScale = transform.localScale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBusy) return;

        WildeBeestBehavior wildebeest = other.GetComponent<WildeBeestBehavior>();
        if (wildebeest == null)
        {
            wildebeest = other.GetComponentInParent<WildeBeestBehavior>();
        }

        if (wildebeest == null || wildebeest.IsCaught) return;

        if (wildebeest.CurrentSpeed > maxCatchableSpeed)
        {
            wildebeest.TryEscapeJumpFromCrocodile();
            return;
        }

        BeginCatch(wildebeest);
    }

    private void BeginCatch(WildeBeestBehavior wildebeest)
    {
        isBusy = true;
        currentPrey = wildebeest;
        wildebeest.BecomeCaught();
        StartCoroutine(CatchSequence(wildebeest));
    }

    private IEnumerator CatchSequence(WildeBeestBehavior wildebeest)
    {
        if (wildebeest == null)
        {
            ClearBusy();
            yield break;
        }

        // 1+2. 角马震动与鳄鱼靠近同时进行
        yield return ApproachAndScare(wildebeest);
        if (wildebeest == null)
        {
            yield return ReturnHome();
            ClearBusy();
            yield break;
        }

        // 3. 设为鳄鱼子物体
        Transform preyTransform = wildebeest.transform;
        preyTransform.SetParent(transform, true);

        // 4. Y 轴压缩 + 震动 + 粒子
        yield return ShrinkWithShakeAndEffects(preyTransform);

        // 5. 删除角马
        if (preyTransform != null)
        {
            Destroy(preyTransform.gameObject);
        }
        currentPrey = null;

        // 6. 还原鳄鱼外观（Y 轴插值回弹）
        Vector3 restoreStartScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration);
            Vector3 scale = homeScale;
            scale.y = Mathf.Lerp(restoreStartScale.y, homeScale.y, t);
            transform.localScale = scale;
            yield return null;
        }
        transform.localScale = homeScale;

        // 7. 开心动画接口
        yield return PlayHappyAnimation();

        // 8. 稍等再回起点
        yield return new WaitForSeconds(returnDelay);
        yield return ReturnHome();

        ClearBusy();
    }

    private IEnumerator ShrinkWithShakeAndEffects(Transform preyTransform)
    {
        PlayShrinkParticles();
        PlayShrinkSound();

        Vector3 crocStartScale = transform.localScale;
        Vector3 origin = transform.position;
        float elapsed = 0f;

        while (elapsed < shrinkDuration)
        {
            if (preyTransform == null)
            {
                break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration);

            Vector3 scale = crocStartScale;
            scale.y = Mathf.Lerp(crocStartScale.y, 0f, t);
            transform.localScale = scale;

            float damper = 1f - t;
            float ox = Mathf.Sin(elapsed * shrinkShakeFrequency) * shrinkShakeAmount * damper;
            float oy = Mathf.Cos(elapsed * shrinkShakeFrequency * 1.2f) * shrinkShakeAmount * damper;
            transform.position = origin + new Vector3(ox, oy, 0f);

            yield return null;
        }

        transform.position = origin;
    }

    /// <summary>
    /// 压缩粒子接口：在 Inspector 拖入 ParticleSystem 即可，后续也可在此扩展。
    /// </summary>
    private void PlayShrinkParticles()
    {
        if (shrinkParticles == null) return;

        shrinkParticles.transform.position = transform.position;
        shrinkParticles.Play();
    }

    /// <summary>
    /// 压缩音效：通过 AudioController 按 clip 名播放一次。
    /// </summary>
    private void PlayShrinkSound()
    {
        if (string.IsNullOrEmpty(shrinkSoundId)) return;
        if (AudioController.Instance == null) return;

        AudioController.Instance.PlaySound2D(shrinkSoundId, volume: shrinkSoundVolume);
    }

    /// <summary>
    /// 同时播放角马害怕震动与鳄鱼靠近，两者都结束后再继续。
    /// </summary>
    private IEnumerator ApproachAndScare(WildeBeestBehavior wildebeest)
    {
        bool shakeDone = false;
        bool approachDone = false;

        StartCoroutine(RunAndFlag(wildebeest.PlayScaredReaction(), () => shakeDone = true));
        StartCoroutine(RunAndFlag(ApproachPrey(wildebeest), () => approachDone = true));

        while (!shakeDone || !approachDone)
        {
            if (wildebeest == null)
            {
                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator RunAndFlag(IEnumerator routine, System.Action onDone)
    {
        yield return routine;
        onDone?.Invoke();
    }

    private IEnumerator ApproachPrey(WildeBeestBehavior wildebeest)
    {
        // 锁定目标点，避免角马震动导致永远对不齐
        Vector3 lockedTarget = wildebeest != null
            ? wildebeest.transform.position
            : transform.position;

        while (wildebeest != null)
        {
            Vector3 pos = transform.position;
            float dx = Mathf.Abs(pos.x - lockedTarget.x);
            float dy = Mathf.Abs(pos.y - lockedTarget.y);

            if (dx < approachXThreshold && dy < 0.01f)
            {
                pos.x = Mathf.MoveTowards(pos.x, lockedTarget.x, approachSpeed * Time.deltaTime);
                pos.y = lockedTarget.y;
                transform.position = pos;
                yield break;
            }

            Vector3 target = new Vector3(lockedTarget.x, lockedTarget.y, pos.z);
            transform.position = Vector3.MoveTowards(pos, target, approachSpeed * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// 开心动画占位，后续在这里接实际动画。
    /// </summary>
    private IEnumerator PlayHappyAnimation()
    {
        // TODO: 播放鳄鱼开心动画
        yield return new WaitForSeconds(happyAnimationDuration);
    }

    private IEnumerator ReturnHome()
    {
        while (Vector3.Distance(transform.position, homePosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                homePosition,
                returnSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = homePosition;
        transform.localScale = homeScale;
    }

    private void ClearBusy()
    {
        isBusy = false;
        currentPrey = null;
    }
}
