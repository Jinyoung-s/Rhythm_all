using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNavigator
{
    private static string lastSceneName;

    /// <summary>
    /// 원하는 씬으로 이동하면서 현재 씬을 자동 기록.
    /// </summary>
    public static void Load(string sceneName)
    {
        lastSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 바로 직전 씬으로 이동.
    /// </summary>
    public static void Back()
    {
        if (!string.IsNullOrEmpty(lastSceneName))
        {
            SceneManager.LoadScene(lastSceneName);
        }
        else
        {
            Debug.LogWarning("No previous scene recorded.");
        }
    }
}
