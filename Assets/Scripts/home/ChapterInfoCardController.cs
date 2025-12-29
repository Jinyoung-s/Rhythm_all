using UnityEngine;
using UnityEngine.UIElements;

public class ChapterInfoCardController : MonoBehaviour
{
     public UIDocument uiDocument; 

    private Label titleLabel, subtitleLabel, rewardLabel;
    private Label sentencesLabel, tracksLabel, timeLabel;
    private ProgressBar progressBar;
    private Button previewButton, startButton;

   

    void Start()
    {

    }


    public void Initialize(VisualElement root)
    {
        titleLabel = root.Q<Label>("title");
        subtitleLabel = root.Q<Label>("subtitle");
        rewardLabel = root.Q<Label>("reward");

        sentencesLabel = root.Q<Label>("sentences");
        tracksLabel = root.Q<Label>("tracks");
        timeLabel = root.Q<Label>("time");

        progressBar = root.Q<ProgressBar>("progress-bar");
        previewButton = root.Q<Button>("preview-btn");
        startButton = root.Q<Button>("start-btn");

        previewButton.clicked += OnPreviewClicked;
        startButton.clicked += OnStartClicked;
    }

    public void UpdateInfo(ChapterStatusData data)
    {
        titleLabel.text = data.title;
        subtitleLabel.text = $"🔥 {data.dailyStreak} Day Streak";
        sentencesLabel.text = $"{data.sentenceCount} Key Sentences";
        tracksLabel.text = $"{data.trackCount} Rhythm Tracks";
        timeLabel.text = $"Approx. {data.estimatedMinutes} minutes";
        rewardLabel.text = $"Next Reward: {data.nextReward}";
        progressBar.value = data.progress * 100f;
    }

    private void OnPreviewClicked()
    {
        Debug.Log("▶ Preview Sentences clicked!");
    }

    private void OnStartClicked()
    {
        Debug.Log("🚀 Start Now clicked!");
    }
}


[System.Serializable]
public class ChapterStatusData
{
    public string title;
    public int sentenceCount;
    public int trackCount;
    public int estimatedMinutes;
    public int dailyStreak;
    public string nextReward;
    public float progress;
}
