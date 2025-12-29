using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Wrong Sentences View Controller
/// 테스트에서 틀린 문장 목록 표시 및 관리
/// </summary>
public class WrongSentencesViewController : MonoBehaviour
{
    private VisualElement root;
    private VisualElement overlay;
    private ScrollView sentenceList;
    private VisualElement emptyState;
    private Label statsLabel;
    private Button backButton;

    // External UI elements to hide
    private VisualElement bottomMenuRoot;
    private Label reviewTitle;
    private HeaderUI headerUI;

    public void Initialize(VisualElement viewRoot, VisualElement bottomMenu, Label reviewTitleLabel, HeaderUI header)
    {
        root = viewRoot;
        bottomMenuRoot = bottomMenu;
        reviewTitle = reviewTitleLabel;
        headerUI = header;

        BindElements();
        RegisterEvents();
        
        Hide(); // Start hidden
    }

    private void BindElements()
    {
        overlay = root.Q<VisualElement>("WrongSentencesOverlay");
        sentenceList = root.Q<ScrollView>("SentenceList");
        emptyState = root.Q<VisualElement>("EmptyState");
        statsLabel = root.Q<Label>("StatsLabel");
        backButton = root.Q<Button>("BackButton");

        Debug.Assert(overlay != null, "WrongSentencesOverlay not found");
        Debug.Assert(sentenceList != null, "SentenceList not found");
        Debug.Assert(emptyState != null, "EmptyState not found");
    }

    private void RegisterEvents()
    {
        backButton?.RegisterCallback<ClickEvent>(_ => Hide());
    }

    public void Show()
    {
        overlay.style.display = DisplayStyle.Flex;
        
        // Hide external UI
        if (bottomMenuRoot != null) bottomMenuRoot.style.display = DisplayStyle.None;
        if (reviewTitle != null) reviewTitle.style.display = DisplayStyle.None;
        if (headerUI != null) headerUI.Hide();

        RefreshList();
    }

    public void Hide()
    {
        overlay.style.display = DisplayStyle.None;
        
        // Show external UI
        if (bottomMenuRoot != null) bottomMenuRoot.style.display = DisplayStyle.Flex;
        if (reviewTitle != null) reviewTitle.style.display = DisplayStyle.Flex;
        if (headerUI != null) headerUI.Show();
    }

    private void RefreshList()
    {
        sentenceList.Clear();

        var wrongSentences = SentenceManager.Instance.GetWrongSentences();

        if (wrongSentences.Count == 0)
        {
            // Show empty state
            sentenceList.style.display = DisplayStyle.None;
            emptyState.RemoveFromClassList("empty-state");
            emptyState.AddToClassList("empty-state-visible");
            statsLabel.text = "완벽해요! 틀린 문장이 없습니다 🎉";
        }
        else
        {
            // Show list
            sentenceList.style.display = DisplayStyle.Flex;
            emptyState.RemoveFromClassList("empty-state-visible");
            emptyState.AddToClassList("empty-state");
            
            statsLabel.text = $"📊 {wrongSentences.Count} sentences to review";

            foreach (var progress in wrongSentences)
            {
                var card = CreateSentenceCard(progress);
                sentenceList.Add(card);
            }
        }
    }

    private VisualElement CreateSentenceCard(SentenceProgress progress)
    {
        var card = new VisualElement();
        card.AddToClassList("sentence-card");

        // Header (Accuracy + Attempts)
        var header = new VisualElement();
        header.AddToClassList("sentence-header");

        var accuracyLabel = new Label(GetAccuracyIcon(progress.accuracy));
        accuracyLabel.AddToClassList("sentence-accuracy");
        accuracyLabel.AddToClassList(GetAccuracyClass(progress.accuracy));

        var attemptsLabel = new Label($"{progress.successCount}/{progress.attemptCount} correct");
        attemptsLabel.AddToClassList("sentence-attempts");

        header.Add(accuracyLabel);
        header.Add(attemptsLabel);

        // English sentence
        var englishLabel = new Label(progress.sentence);
        englishLabel.AddToClassList("sentence-english");

        // Korean translation
        var koreanLabel = new Label(progress.translation);
        koreanLabel.AddToClassList("sentence-korean");

        // Actions
        var actions = new VisualElement();
        actions.AddToClassList("sentence-actions");

        var playButton = CreateActionButton("Play", "play-button", () => PlaySentence(progress));

        actions.Add(playButton);

        // Assemble card
        card.Add(header);
        card.Add(englishLabel);
        card.Add(koreanLabel);
        card.Add(actions);

        return card;
    }

    private Button CreateActionButton(string text, string additionalClass, System.Action onClick)
    {
        var button = new Button(onClick);
        button.AddToClassList("action-button");
        button.AddToClassList(additionalClass);

        var label = new Label(text);
        label.AddToClassList("action-label");
        button.Add(label);

        return button;
    }

    private string GetAccuracyIcon(float accuracy)
    {
        if (accuracy < 33f) return "❌";
        if (accuracy < 66f) return "⚠️";
        return "✅";
    }

    private string GetAccuracyClass(float accuracy)
    {
        if (accuracy < 33f) return "accuracy-low";
        if (accuracy < 66f) return "accuracy-medium";
        return "accuracy-high";
    }

    private void PlaySentence(SentenceProgress progress)
    {
        Debug.Log($"[WrongSentences] Play: {progress.sentence}");
        VocabularyTTSManager.Instance?.SpeakWord(progress.sentence);
    }

    private void PracticeSentence(SentenceProgress progress)
    {
        Debug.Log($"[WrongSentences] Practice: {progress.sentence}");
        // TODO: Navigate to practice mode for this sentence
    }
}
