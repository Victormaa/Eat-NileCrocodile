using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WildeBeestHerdManager : MonoBehaviour
{
    public enum WildeBeestHerdState
    {
        Scared,
        MovingOn,
        Stop,
    }

    [Header("生成设置")]
    public GameObject wildebeestPrefab;
    public GameObject scaredLeaderPrefab;
    public int herdCount = 10;
    public float spawnX = -13f;
    public float xSpacing = 1.5f;
    public float xJitter = 0.4f;
    public float spawnYMin = -5f;
    public float spawnYMax = 3f;
    public float spawnZ = 0f;
    public bool spawnOnStart = true;

    [Header("Scared 入场")]
    public Transform scaredTargetGroup1;
    public Transform scaredTargetGroup2;
    private int scaredFrontCount = 6;
    [Tooltip("在 Target 左侧多远生成前排角马")]
    public float scaredSpawnOffsetX = 12f;
    public float scaredMoveSpeed = 6f;
    [Tooltip("true=交替两组阵型；false=每次随机")]
    public bool alternateScaredGroups = true;
    [Tooltip("六只到齐后，头马等待多久再显示 Scared 表情")]
    public float scaredEmoteDelay = 1f;
    [Tooltip("右边离场角马超过该 X 后销毁（不绕回）")]
    public float exitDespawnX = 12f;

    [Header("MovingOn → Scared")]
    [Tooltip("MovingOn 开始后，间隔多久自动 EnterScared")]
    public float movingOnToScaredDelay = 8f;
    public bool autoEnterScaredAfterMovingOn = true;

    private readonly List<WildeBeestBehavior> herd = new List<WildeBeestBehavior>();
    private readonly List<WildeBeestBehavior> scaredFront = new List<WildeBeestBehavior>();
    private readonly List<WildeBeestBehavior> exitingHerd = new List<WildeBeestBehavior>();

    public WildeBeestHerdState curState;
    public WildeBeestBehavior headScaredBeest;

    /// <summary>
    /// 六只 Scared 前排已到齐站稳，可以开始雨 → GO → 出发序列。
    /// </summary>
    public bool IsScaredLineReady =>
        !isEnteringScared
        && curState == WildeBeestHerdState.Stop
        && headScaredBeest != null;

    private bool isEnteringScared;
    private int scaredArrivedCount;
    private int nextScaredGroupIndex;
    private Coroutine scaredEmoteRoutine;
    private Coroutine movingOnToScaredRoutine;

    private void Awake()
    {
        curState = WildeBeestHerdState.Scared;
    }

    void Start()
    {
        if (spawnOnStart)
        {
            EnterScared();
        }
    }

    /// <summary>
    /// 惊慌入场：保留在场角马并左右分流；生成前排 6 只跑向 Target；到齐后只停 Scared。
    /// </summary>
    public void EnterScared()
    {
        if (isEnteringScared) return;

        if (wildebeestPrefab == null)
        {
            Debug.LogWarning("WildeBeestHerdManager: wildebeestPrefab 未设置。");
            return;
        }

        Transform targetGroup = PickScaredTargetGroup();
        if (targetGroup == null)
        {
            Debug.LogWarning("WildeBeestHerdManager: scaredTargetGroup 未设置。");
            return;
        }

        List<Transform> targets = CollectTargets(targetGroup);
        if (targets.Count < scaredFrontCount)
        {
            Debug.LogWarning(
                $"WildeBeestHerdManager: 目标组 {targetGroup.name} 子物体不足 {scaredFrontCount} 个。"
            );
            return;
        }

        CancelAutoEnterScared();
        SetHerdClickStunEnabled(false);

        if (scaredEmoteRoutine != null)
        {
            StopCoroutine(scaredEmoteRoutine);
            scaredEmoteRoutine = null;
        }

        ClearPreviousScaredFront();
        PruneHerdLists();

        // 快照当前在场角马（不含即将生成的 Scared）
        var preexisting = new List<WildeBeestBehavior>(herd);

        GameObject leaderPrefab = scaredLeaderPrefab != null ? scaredLeaderPrefab : wildebeestPrefab;

        curState = WildeBeestHerdState.Scared;
        isEnteringScared = true;
        scaredArrivedCount = 0;
        headScaredBeest = null;

        float scaredLineX = GetMinTargetX(targets);

        for (int i = 0; i < scaredFrontCount; i++)
        {
            Transform target = targets[i];
            Vector3 spawnPos = target.position + Vector3.left * scaredSpawnOffsetX;

            GameObject prefab = (i == 0) ? leaderPrefab : wildebeestPrefab;
            GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity, transform);

            WildeBeestBehavior behavior = GetBehavior(instance);
            if (behavior == null)
            {
                Debug.LogWarning("WildeBeestHerdManager: 预制体上找不到 WildeBeestBehavior。");
                Destroy(instance);
                continue;
            }

            behavior.SetCanMove(false);
            behavior.SetClickStunEnabled(false);
            herd.Add(behavior);
            scaredFront.Add(behavior);

            WildeBeestMoveToTarget mover = instance.GetComponent<WildeBeestMoveToTarget>();
            if (mover == null)
            {
                mover = instance.AddComponent<WildeBeestMoveToTarget>();
            }

            mover.moveSpeed = scaredMoveSpeed;
            int captureIndex = i;
            mover.Setup(target, () => OnScaredFrontArrived(captureIndex));
            mover.StartMoving();

            if (i == 0)
            {
                headScaredBeest = behavior;
                if (behavior.GetComponent<WildeBeestScaredLeader>() == null)
                {
                    behavior.gameObject.AddComponent<WildeBeestScaredLeader>();
                }
            }
        }

        SplitPreexistingHerd(preexisting, scaredLineX);
    }

    /// <summary>
    /// 在前排身后追加普通角马并立刻向右跑（不清空已有角马）。
    /// </summary>
    public void SpawnFollowerHerd()
    {
        if (wildebeestPrefab == null) return;

        float baseX = spawnX;
        if (scaredFront.Count > 0)
        {
            float minX = float.MaxValue;
            for (int i = 0; i < scaredFront.Count; i++)
            {
                if (scaredFront[i] == null) continue;
                minX = Mathf.Min(minX, scaredFront[i].transform.position.x);
            }

            if (minX < float.MaxValue)
            {
                baseX = minX - xSpacing;
            }
        }

        float yMin = Mathf.Min(spawnYMin, spawnYMax);
        float yMax = Mathf.Max(spawnYMin, spawnYMax);

        for (int i = 0; i < herdCount; i++)
        {
            float x = baseX - i * xSpacing + Random.Range(-xJitter, xJitter);
            float y = Random.Range(yMin, yMax);
            Vector3 position = new Vector3(x, y, spawnZ);

            GameObject instance = Instantiate(wildebeestPrefab, position, Quaternion.identity, transform);
            WildeBeestBehavior behavior = GetBehavior(instance);
            if (behavior == null)
            {
                Debug.LogWarning("WildeBeestHerdManager: 跟随预制体上找不到 WildeBeestBehavior。");
                Destroy(instance);
                continue;
            }

            herd.Add(behavior);
            behavior.SetCanMove(true);
        }
    }

    /// <summary>
    /// 静态生成在 spawnX 站位（会清空现有群），生成后为 Stop。
    /// </summary>
    public void SpawnHerd()
    {
        if (wildebeestPrefab == null)
        {
            Debug.LogWarning("WildeBeestHerdManager: wildebeestPrefab 未设置。");
            return;
        }

        isEnteringScared = false;
        ClearHerd();
        headScaredBeest = null;

        float yMin = Mathf.Min(spawnYMin, spawnYMax);
        float yMax = Mathf.Max(spawnYMin, spawnYMax);

        for (int i = 0; i < herdCount; i++)
        {
            float x = spawnX - i * xSpacing + Random.Range(-xJitter, xJitter);
            float y = Random.Range(yMin, yMax);
            Vector3 position = new Vector3(x, y, spawnZ);
            GameObject instance = Instantiate(wildebeestPrefab, position, Quaternion.identity, transform);

            WildeBeestBehavior behavior = GetBehavior(instance);
            if (behavior != null)
            {
                behavior.SetCanMove(false);
                herd.Add(behavior);
            }
            else
            {
                Debug.LogWarning("WildeBeestHerdManager: 生成的预制体上找不到 WildeBeestBehavior。");
            }
        }

        curState = WildeBeestHerdState.Stop;
    }

    public void StartMovingOn()
    {
        if (isEnteringScared) return;

        curState = WildeBeestHerdState.MovingOn;
        SetHerdClickStunEnabled(true);
        StartHerdMovement();
        ScheduleAutoEnterScared();
    }

    public void StopHerd()
    {
        if (isEnteringScared) return;

        curState = WildeBeestHerdState.Stop;
        SetHerdClickStunEnabled(false);
        StopHerdMovement();
        CancelAutoEnterScared();
    }

    private void SetHerdClickStunEnabled(bool enabled)
    {
        PruneHerdLists();
        for (int i = 0; i < herd.Count; i++)
        {
            if (herd[i] != null)
            {
                herd[i].SetClickStunEnabled(enabled);
            }
        }
    }

    private void ScheduleAutoEnterScared()
    {
        CancelAutoEnterScared();
        if (!autoEnterScaredAfterMovingOn) return;
        movingOnToScaredRoutine = StartCoroutine(AutoEnterScaredAfterDelay());
    }

    private void CancelAutoEnterScared()
    {
        if (movingOnToScaredRoutine == null) return;
        StopCoroutine(movingOnToScaredRoutine);
        movingOnToScaredRoutine = null;
    }

    private IEnumerator AutoEnterScaredAfterDelay()
    {
        yield return new WaitForSeconds(movingOnToScaredDelay);
        movingOnToScaredRoutine = null;

        if (curState != WildeBeestHerdState.MovingOn) yield break;
        if (isEnteringScared) yield break;

        EnterScared();
    }

    public void StartHerdMovement()
    {
        PruneHerdLists();
        for (int i = 0; i < herd.Count; i++)
        {
            if (herd[i] != null)
            {
                herd[i].SetCanMove(true);
            }
        }
    }

    public void StopHerdMovement()
    {
        PruneHerdLists();
        for (int i = 0; i < herd.Count; i++)
        {
            if (herd[i] == null) continue;
            // 正在离场的右边角马继续跑出屏幕
            if (herd[i].DespawnOnExit) continue;
            herd[i].SetCanMove(false);
        }

        StopScaredFrontMovement();
    }

    private void SplitPreexistingHerd(List<WildeBeestBehavior> preexisting, float scaredLineX)
    {
        for (int i = 0; i < preexisting.Count; i++)
        {
            WildeBeestBehavior beast = preexisting[i];
            if (beast == null) continue;

            // 刚生成的 Scared 不在 preexisting 里；双保险跳过
            if (scaredFront.Contains(beast)) continue;

            if (beast.transform.position.x < scaredLineX)
            {
                herd.Remove(beast);
                Destroy(beast.gameObject);
            }
            else
            {
                beast.SetDespawnOnExit(true, exitDespawnX);
                beast.SetCanMove(true);
                if (!exitingHerd.Contains(beast))
                {
                    exitingHerd.Add(beast);
                }
            }
        }

        PruneHerdLists();
    }

    private void ClearPreviousScaredFront()
    {
        for (int i = 0; i < scaredFront.Count; i++)
        {
            WildeBeestBehavior beast = scaredFront[i];
            if (beast == null) continue;
            herd.Remove(beast);
            Destroy(beast.gameObject);
        }

        scaredFront.Clear();
        headScaredBeest = null;
    }

    private void StopScaredFrontMovement()
    {
        for (int i = 0; i < scaredFront.Count; i++)
        {
            if (scaredFront[i] == null) continue;
            scaredFront[i].SetClickStunEnabled(false);
            scaredFront[i].SetCanMove(false);

            WildeBeestMoveToTarget mover = scaredFront[i].GetComponent<WildeBeestMoveToTarget>();
            if (mover != null)
            {
                mover.StopMoving();
            }
        }
    }

    private void OnScaredFrontArrived(int index)
    {
        if (!isEnteringScared) return;

        scaredArrivedCount++;
        if (scaredArrivedCount < scaredFront.Count)
        {
            return;
        }

        // 六只都到齐：只停 Scared 前排，离场右边角马继续跑
        StopScaredFrontMovement();
        curState = WildeBeestHerdState.Stop;
        isEnteringScared = false;
        OnAllScaredBeestsArrived();
    }

    private void OnAllScaredBeestsArrived()
    {
        if (scaredEmoteRoutine != null)
        {
            StopCoroutine(scaredEmoteRoutine);
        }
        scaredEmoteRoutine = StartCoroutine(PlayLeaderScaredEmoteAfterDelay());
    }

    private IEnumerator PlayLeaderScaredEmoteAfterDelay()
    {
        yield return new WaitForSeconds(scaredEmoteDelay);

        if (headScaredBeest == null) yield break;

        WildeBeestScaredLeader leader = headScaredBeest.GetComponent<WildeBeestScaredLeader>();
        if (leader == null)
        {
            leader = headScaredBeest.GetComponentInChildren<WildeBeestScaredLeader>();
        }

        if (leader != null)
        {
            leader.ShowEmote("Scared");
        }

        scaredEmoteRoutine = null;
    }

    private static float GetMinTargetX(List<Transform> targets)
    {
        float minX = float.MaxValue;
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            minX = Mathf.Min(minX, targets[i].position.x);
        }

        return minX < float.MaxValue ? minX : 0f;
    }

    private Transform PickScaredTargetGroup()
    {
        bool has1 = scaredTargetGroup1 != null;
        bool has2 = scaredTargetGroup2 != null;

        if (!has1 && !has2) return null;
        if (has1 && !has2) return scaredTargetGroup1;
        if (!has1 && has2) return scaredTargetGroup2;

        if (alternateScaredGroups)
        {
            Transform picked = (nextScaredGroupIndex % 2 == 0) ? scaredTargetGroup1 : scaredTargetGroup2;
            nextScaredGroupIndex++;
            return picked;
        }

        return Random.value < 0.5f ? scaredTargetGroup1 : scaredTargetGroup2;
    }

    private static List<Transform> CollectTargets(Transform group)
    {
        var list = new List<Transform>();
        if (group == null) return list;

        var named = new List<Transform>();
        for (int i = 0; i < group.childCount; i++)
        {
            named.Add(group.GetChild(i));
        }

        named.Sort((a, b) =>
        {
            int na = ExtractTargetIndex(a.name);
            int nb = ExtractTargetIndex(b.name);
            if (na >= 0 && nb >= 0) return na.CompareTo(nb);
            if (na >= 0) return -1;
            if (nb >= 0) return 1;
            return a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
        });

        list.AddRange(named);
        return list;
    }

    private static int ExtractTargetIndex(string name)
    {
        if (string.IsNullOrEmpty(name)) return -1;
        int digitsStart = -1;
        for (int i = name.Length - 1; i >= 0; i--)
        {
            if (char.IsDigit(name[i])) digitsStart = i;
            else break;
        }

        if (digitsStart < 0) return -1;
        if (int.TryParse(name.Substring(digitsStart), out int index))
        {
            return index;
        }

        return -1;
    }

    private static WildeBeestBehavior GetBehavior(GameObject instance)
    {
        WildeBeestBehavior behavior = instance.GetComponent<WildeBeestBehavior>();
        if (behavior == null)
        {
            behavior = instance.GetComponentInChildren<WildeBeestBehavior>();
        }

        return behavior;
    }

    private void PruneHerdLists()
    {
        herd.RemoveAll(b => b == null);
        scaredFront.RemoveAll(b => b == null);
        exitingHerd.RemoveAll(b => b == null);
    }

    private void ClearHerd()
    {
        for (int i = 0; i < herd.Count; i++)
        {
            if (herd[i] != null)
            {
                Destroy(herd[i].gameObject);
            }
        }
        herd.Clear();
        scaredFront.Clear();
        exitingHerd.Clear();
    }
}
