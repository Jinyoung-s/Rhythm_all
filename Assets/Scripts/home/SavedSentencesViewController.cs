using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Saved Sentences View Controller
/// 사용자가 저장한 문장 목록 표시 및 관리
/// </summary>
public class SavedSentencesViewController : MonoBehaviour
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
        overlay = root.Q<VisualElement>("SavedSentencesOverlay");
        sentenceList = root.Q<ScrollView>("SentenceList");
        emptyState = root.Q<VisualElement>("EmptyState");
        statsLabel = root.Q<Label>("StatsLabel");
        backButton = root.Q<Button>("BackButton");

        Debug.Assert(overlay != null, "SavedSentencesOverlay not found");
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

        var savedSentences = SentenceManager.Instance.GetSavedSentences();

        if (savedSentences.Count == 0)
        {
            // Show empty state
            sentenceList.style.display = DisplayStyle.None;
            emptyState.RemoveFromClassList("empty-state");
            emptyState.AddToClassList("empty-state-visible");
            statsLabel.text = "⭐ 0 saved";
        }
        else
        {
            // Show list
            sentenceList.style.display = DisplayStyle.Flex;
            emptyState.RemoveFromClassList("empty-state-visible");
            emptyState.AddToClassList("empty-state");
            
            statsLabel.text = $"⭐ {savedSentences.Count} saved";

            foreach (var saved in savedSentences)
            {
                var card = CreateSentenceCard(saved);
                sentenceList.Add(card);
            }
        }
    }

    private VisualElement CreateSentenceCard(SavedSentence saved)
    {
        var card = new VisualElement();
        card.AddToClassList("sentence-card");

        // Header (Star + Date)
        var header = new VisualElement();
        header.AddToClassList("sentence-header");

        var unsaveButton = new Button(() => DeleteSentence(saved, card));
        unsaveButton.AddToClassList("unsave-star-button");
        
        var starLabel = new Label("⭐");
        starLabel.AddToClassList("sentence-star");
        unsaveButton.Add(starLabel);

        var dateLabel = new Label(FormatDate(saved.savedDate));
        dateLabel.AddToClassList("sentence-date");

        header.Add(unsaveButton);
        header.Add(dateLabel);

        // English sentence
        var englishLabel = new Label(saved.sentence);
        englishLabel.AddToClassList("sentence-english");

        // Korean translation
        var koreanLabel = new Label(saved.translation);
        koreanLabel.AddToClassList("sentence-korean");

        // Actions
        var actions = new VisualElement();
        actions.AddToClassList("sentence-actions");

        var playButton = CreateActionButton("Play", "play-button", () => PlaySentence(saved));
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

    private string FormatDate(DateTime date)
    {
        var now = DateTime.Now;
        var diff = now - date;

        if (diff.TotalMinutes < 1)
            return "방금 전";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}분 전";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}시간 전";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}일 전";
        
        return date.ToString("yyyy-MM-dd");
    }

    private void PlaySentence(SavedSentence saved)
    {
        Debug.Log($"[SavedSentences] Play: {saved.sentence}");
        VocabularyTTSManager.Instance?.SpeakWord(saved.sentence);
    }

    private void PracticeSentence(SavedSentence saved)
    {
        Debug.Log($"[SavedSentences] Practice: {saved.sentence}");
        // TODO: Navigate to practice mode for this sentence
    }

    private void DeleteSentence(SavedSentence saved, VisualElement card)
    {
        Debug.Log($"[SavedSentences] Delete: {saved.sentence}");
        
        SentenceManager.Instance.RemoveSavedSentence(saved.sentenceId);
        
        // Remove card with animation
        card.style.opacity = 0.5f;
        sentenceList.Remove(card);
        
        // Refresh to update stats and empty state
        RefreshList();
    }
}
