using UnityEngine;

public static class StepSceneLoader
{
    public static string SelectedChapterId { get; private set; }

    public static void LoadStepScene(string chapterId)
    {
        SelectedChapterId = chapterId;
        SceneNavigator.Load("StepScene");
    }
}
