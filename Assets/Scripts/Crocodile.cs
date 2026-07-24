using System.Collections;
using UnityEngine;

public class Crocodile : MonoBehaviour
{
    [Header("抓取")]
    public float maxCatchableSpeed = 3.0f;
    public float shrinkDuration = 0.8f;

    private bool isBusy;
    private WildeBeestBehavior currentPrey;

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
        StartCoroutine(ShrinkAndDestroyPrey(wildebeest));
    }

    private IEnumerator ShrinkAndDestroyPrey(WildeBeestBehavior wildebeest)
    {
        if (wildebeest == null)
        {
            ClearBusy();
            yield break;
        }

        Transform preyTransform = wildebeest.transform;
        Vector3 startScale = preyTransform.localScale;
        float elapsed = 0f;

        while (elapsed < shrinkDuration)
        {
            if (preyTransform == null)
            {
                ClearBusy();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration);
            Vector3 scale = startScale;
            scale.y = Mathf.Lerp(startScale.y, 0f, t);
            preyTransform.localScale = scale;
            yield return null;
        }

        if (preyTransform != null)
        {
            Destroy(preyTransform.gameObject);
        }

        ClearBusy();
    }

    private void ClearBusy()
    {
        isBusy = false;
        currentPrey = null;
    }
}
