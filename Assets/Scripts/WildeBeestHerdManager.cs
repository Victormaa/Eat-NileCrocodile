using System.Collections.Generic;
using UnityEngine;

public class WildeBeestHerdManager : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject wildebeestPrefab;
    public int herdCount = 10;
    public float spawnX = -13f;
    public float xSpacing = 1.5f;
    public float xJitter = 0.4f;
    public float spawnYMin = -5f;
    public float spawnYMax = 3f;
    public float spawnZ = 0f;
    public bool spawnOnStart = true;

    private readonly List<WildeBeestBehavior> herd = new List<WildeBeestBehavior>();

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnHerd();
        }
    }

    public void SpawnHerd()
    {
        if (wildebeestPrefab == null)
        {
            Debug.LogWarning("WildeBeestHerdManager: wildebeestPrefab 未设置。");
            return;
        }

        ClearHerd();

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
            }
            else
            {
                Debug.LogWarning("WildeBeestHerdManager: 生成的预制体上找不到 WildeBeestBehavior。");
            }
        }
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
    }

}
