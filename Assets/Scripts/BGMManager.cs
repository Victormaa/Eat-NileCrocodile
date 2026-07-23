using UnityEngine;
using System.Collections;

public class BMGManager : MonoBehaviour
{
    [Header("BGM 列表（两首）")]
    [SerializeField] private string[] bgmList = new string[] { "BGM1", "BGM2" };

    [Header("环境音设置")]
    [SerializeField] private string ambientName = "BGM_Vice";   // 环境音文件名
    [SerializeField] private float ambientVolume = 0.15f;      // 环境音音量（建议0.1~0.3）
    [SerializeField] private float mainBGMVolume = 0.8f;      // 环境音音量（建议0.1~0.3）

    private int currentBgmIndex = 0;

    void Start()
    {
        // 1. 播放环境音（使用索引1，一直循环）
        AudioController.Instance.SetLoopAndPlay(ambientName, 1, looping: true, cancelFades: false);
        AudioController.Instance.SetLoopVolumeImmediate(ambientVolume, 1);

        // 2. 启动 BGM 循环协程
        StartCoroutine(PlayBGMSequence());
    }

    IEnumerator PlayBGMSequence()
    {
        // --- 先播放第一首 BGM（直接播放，无淡入） ---
        string firstBgm = bgmList[0];
        AudioController.Instance.SetLoopAndPlay(firstBgm, 0, looping: false, cancelFades: false);
        AudioController.Instance.SetLoopVolumeImmediate(mainBGMVolume, 0);
        yield return WaitForBgmEnd(firstBgm);   // 等待播放完毕

        // --- 循环切换后面两首 ---
        while (true)
        {
            // 计算下一首的索引
            int nextIndex = (currentBgmIndex + 1) % bgmList.Length;
            string nextBgm = bgmList[nextIndex];

            // 淡出当前 BGM（索引0）
            AudioController.Instance.FadeOutLoop(1.5f, 0);   // 1.5秒淡出
            yield return new WaitForSeconds(1.5f);           // 等待淡出完成

            // 完全停止当前 BGM（确保清空）
            AudioController.Instance.StopLoop(0);

            // 设置新 BGM，音量先设为 0，然后淡入
            AudioController.Instance.SetLoopAndPlay(nextBgm, 0, looping: false, cancelFades: false);
            AudioController.Instance.SetLoopVolumeImmediate(0f, 0);   // 立即静音
            AudioController.Instance.FadeInLoop(1.5f, mainBGMVolume, 0);        // 1.5秒淡入到最大音量

            // 等待淡入完成（可省略，但建议等待，避免后续逻辑干扰）
            yield return new WaitForSeconds(1.5f);

            // 更新当前索引
            currentBgmIndex = nextIndex;

            // 等待这首 BGM 播放完毕
            yield return WaitForBgmEnd(nextBgm);
        }
    }

    // 辅助方法：等待指定 BGM 播放完毕（根据音频长度）
    private IEnumerator WaitForBgmEnd(string bgmName)
    {
        AudioClip clip = AudioController.Instance.GetLoopClip(bgmName);
        if (clip != null)
        {
            yield return new WaitForSeconds(clip.length);
        }
        else
        {
            // 如果找不到音频，等待 5 秒后继续（避免死循环）
            Debug.LogWarning($"BGM '{bgmName}' not found in Resources/Audios/Loops");
            yield return new WaitForSeconds(5f);
        }
    }
}