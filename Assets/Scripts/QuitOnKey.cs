using UnityEngine;

/// <summary>
/// 按指定键退出游戏（打包后 Quit；编辑器里停止 Play）。
/// </summary>
public class QuitOnKey : MonoBehaviour
{
    public KeyCode quitKey = KeyCode.Escape;

    private void Update()
    {
        if (!Input.GetKeyDown(quitKey))
            return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
