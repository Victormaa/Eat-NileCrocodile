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
    [Tooltip("整队相对最终站位再往左平移的额外距离")]
    public float scaredEntryExtraOffset = 2f;
    [Tooltip("到达后是否把每只 X 收束到各自目标站位")]
    public bool snapToTargetsOnArrive = true;

    private readonly List<WildeBeestBehavior> herd = new List<WildeBeestBehavior>();
    private readonly List<float> herdTargetXs = new List<float>();

    public WildeBeestHerdState curState;
    public WildeBeestBehavior headScaredBeest;
    private bool isEnteringScared;



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
    /// 惊慌入场：头马带队从左侧跑到 spawnX，到位后进入 Stop。
    /// </summary>
    public void EnterScared()
    {
        if (isEnteringScared) return;

        if (wildebeestPrefab == null)
        {
            Debug.LogWarning("WildeBeestHerdManager: wildebeestPrefab 未设置。");
            return;
        }

        GameObject leaderPrefab = scaredLeaderPrefab != null ? scaredLeaderPrefab : wildebeestPrefab;

        curState = WildeBeestHerdState.Scared;
        isEnteringScared = true;
        ClearHerd();
        headScaredBeest = null;

        float yMin = Mathf.Min(spawnYMin, spawnYMax);
        float yMax = Mathf.Max(spawnYMin, spawnYMax);
        float entryOffset = herdCount * xSpacing + scaredEntryExtraOffset;

        for (int i = 0; i < herdCount; i++)
        {
            float targetX = spawnX - i * xSpacing + Random.Range(-xJitter, xJitter);
            float y = Random.Range(yMin, yMax);
            float spawnPosX = targetX - entryOffset;
            Vector3 position = new Vector3(spawnPosX, y, spawnZ);

            GameObject prefab = (i == 0) ? leaderPrefab : wildebeestPrefab;
            GameObject instance = Instantiate(prefab, position, Quaternion.identity, transform);

            WildeBeestBehavior behavior = instance.GetComponent<WildeBeestBehavior>();
            if (behavior == null)
            {
                behavior = instance.GetComponentInChildren<WildeBeestBehavior>();
            }

            if (behavior == null)
            {
                Debug.LogWarning("WildeBeestHerdManager: 预制体上找不到 WildeBeestBehavior。");
                Destroy(instance);
                continue;
            }

            behavior.SetCanMove(false);
            herd.Add(behavior);
            herdTargetXs.Add(targetX);

            if (i == 0)
            {
                SetHeadScaredBeest(behavior);
            }
        }

        StartHerdMovement();
    }

    /// <summary>
    /// 全体开跑（Scared 入场过程中调用无效）。
    /// </summary>
    public void StartMovingOn()
    {
        if (isEnteringScared)
        {
            return;
        }

        curState = WildeBeestHerdState.MovingOn;
        StartHerdMovement();
    }

    /// <summary>
    /// 全体停下。
    /// </summary>
    public void StopHerd()
    {
        if (isEnteringScared)
        {
            return;
        }

        curState = WildeBeestHerdState.Stop;
        StopHerdMovement();
    }

    /// <summary>
    /// 静态生成在 spawnX 站位（不入场动画），生成后为 Stop。
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

            WildeBeestBehavior behavior = instance.GetComponent<WildeBeestBehavior>();
            if (behavior == null)
            {
                behavior = instance.GetComponentInChildren<WildeBeestBehavior>();
            }

            if (behavior != null)
            {
                behavior.SetCanMove(false);
                herd.Add(behavior);
                herdTargetXs.Add(x);
            }
            else
            {
                Debug.LogWarning("WildeBeestHerdManager: 生成的预制体上找不到 WildeBeestBehavior。");
            }
        }

        curState = WildeBeestHerdState.Stop;
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
    }

    private void SetHeadScaredBeest(WildeBeestBehavior leaderBehavior)
    {
        headScaredBeest = leaderBehavior;

        WildeBeestScaredLeader leader = leaderBehavior.GetComponent<WildeBeestScaredLeader>();
        if (leader == null)
        {
            leader = leaderBehavior.GetComponentInChildren<WildeBeestScaredLeader>();
        }

        if (leader == null)
        {
            leader = leaderBehavior.gameObject.AddComponent<WildeBeestScaredLeader>();
        }

        leader.Setup(-9.0f, OnScaredLeaderArrived);
    }

    private void OnScaredLeaderArrived()
    {
        if (!isEnteringScared) return;

        StopHerdMovement();

        if (snapToTargetsOnArrive)
        {
            for (int i = 0; i < herd.Count; i++)
            {
                if (herd[i] == null) continue;
                if (i >= herdTargetXs.Count) break;

                Vector3 pos = herd[i].transform.position;
                pos.x = herdTargetXs[i];
                herd[i].transform.position = pos;
            }
        }

        curState = WildeBeestHerdState.Stop;
        isEnteringScared = false;
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
        herdTargetXs.Clear();
    }
}
