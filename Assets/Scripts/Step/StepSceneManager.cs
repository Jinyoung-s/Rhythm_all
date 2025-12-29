using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StepSceneManager : MonoBehaviour
{
    public string stepsFolderPath = "steps"; // Resources/steps/
    public GameObject stepCardPrefab;
    public Transform stepCardParent;

    void Start()
    {
        CurriculumRepository.Configure(stepFolder: stepsFolderPath);

        string chapterId = StepSceneLoader.SelectedChapterId;
        if (string.IsNullOrEmpty(chapterId))
        {
            chapterId = StepResourceResolver.GetFallbackChapterId();
        }

        LoadStepData(chapterId);
    }

    void LoadStepData(string chapterId)
    {
        if (!CurriculumRepository.TryGetChapter(chapterId, out var chapter))
        {
            Debug.LogError($"[StepSceneManager] Chapter '{chapterId}' not found in CurriculumRepository.");
            return;
        }

        var steps = chapter.Steps;
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning($"[StepSceneManager] Chapter '{chapterId}' has no steps to display.");
            return;
        }

        foreach (StepData step in steps)
        {
            GameObject card = Instantiate(stepCardPrefab, stepCardParent);
            card.GetComponentInChildren<TextMeshProUGUI>().text = step.title;


            Button btn = card.GetComponent<Button>();
            btn.interactable = step.unlocked;

            if (step.unlocked)
            {
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"Start Game: {step.songFile}");
                });
            }
        }
    }
}
