using UnityEngine;
using UnityEngine.SceneManagement;

public static class InGameSettingsBootstrap
{
    private const string ForestSceneName = "Forest";
    private const string SeungHyunSceneName = "SeungHyun2_Restore";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryCreateForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForScene(scene);
    }

    private static void TryCreateForScene(Scene scene)
    {
        if (!IsTargetScene(scene.name))
            return;

        if (Object.FindObjectOfType<InGameSettingsMenuController>() != null)
            return;

        GameObject controllerObject = new GameObject("RuntimeInGameSettingsMenuController");
        controllerObject.AddComponent<InGameSettingsMenuController>();
    }

    private static bool IsTargetScene(string sceneName)
    {
        return sceneName == ForestSceneName || sceneName == SeungHyunSceneName;
    }
}
