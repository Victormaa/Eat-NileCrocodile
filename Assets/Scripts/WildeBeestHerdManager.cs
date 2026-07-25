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

    private readonly List<WildeBeestBehavior> herd = new List<WildeBeestBehavior>();
    private readonly List<WildeBeestBehavior> scaredFront = new List<WildeBeestBehavior>();

    public WildeBeestHerdState curState;
    public WildeBeestBehavior headScaredBeest;

    private bool isEnteringScared;
    private int scaredArrivedCount;
    private int nextScaredGroupIndex;

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
    /// 惊慌入场：前排 6 只跑向场景 Target，出发后身后追加跟随群；到齐后 Stop。
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

        GameObject leaderPrefab = scaredLeaderPrefab != null ? scaredLeaderPrefab : wildebeestPrefab;

        curState = WildeBeestHerdState.Scared;
        isEnteringScared = true;
        scaredArrivedCount = 0;
        ClearHerd();
        headScaredBeest = null;
        scaredFront.Clear();

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

            // 入场由 MoveToTarget 驱动，关闭自主跑动
            behavior.SetCanMove(false);
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
        scaredFront.Clear();

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
        StartHerdMovement();
    }

    public void StopHerd()
    {
        if (isEnteringScared) return;

        curState = WildeBeestHerdState.Stop;
        StopHerdMovement();
    }

    public void StartHerdMovement()
    {
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
        for (int i = 0; i < herd.Count; i++)
        {
            if (herd[i] != null)
            {
                herd[i].SetCanMove(false);
            }
        }

        for (int i = 0; i < scaredFront.Count; i++)
        {
            if (scaredFront[i] == null) continue;
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

        // 六只都到齐：全体停下（含跟随）
        StopHerdMovement();
        curState = WildeBeestHerdState.Stop;
        isEnteringScared = false;
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

        // 优先按 Target1..TargetN 名字排序
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
    }
}
