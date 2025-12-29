using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RhythmEnglish.Economy;

/// <summary>
/// 노래 상점 UI 컨트롤러
/// </summary>
public class SongShopController
{
    private VisualElement root;
    private Action onCloseCallback;

    // UI Elements
    private Button closeButton;
    private Label pointsLabel;
    private Button tabAll;
    private Button tabBeginner;
    private Button tabElementary;
    private Button tabIntermediate;
    private ScrollView songList;

    // State
    private string currentFilter = "All";
    private AudioSource previewSource;

    public SongShopController(VisualElement rootElement, Action onClose)
    {
        root = rootElement;
        onCloseCallback = onClose;

        InitializeUI();
        RefreshPointsDisplay();
        LoadSongs();

        // 구매 이벤트 구독
        SongShopManager.Instance.OnPurchaseSuccess += OnPurchaseSuccess;
        PointManager.Instance.OnPointsChanged += OnPointsChanged;
    }

    public void Dispose()
    {
        SongShopManager.Instance.OnPurchaseSuccess -= OnPurchaseSuccess;
        PointManager.Instance.OnPointsChanged -= OnPointsChanged;
    }

    private void InitializeUI()
    {
        // UI 요소 바인딩
        closeButton = root.Q<Button>("CloseButton");
        pointsLabel = root.Q<Label>("PointsLabel");
        tabAll = root.Q<Button>("TabAll");
        tabBeginner = root.Q<Button>("TabBeginner");
        tabElementary = root.Q<Button>("TabElementary");
        tabIntermediate = root.Q<Button>("TabIntermediate");
        songList = root.Q<ScrollView>("SongList");

        // 이벤트 바인딩
        closeButton?.RegisterCallback<ClickEvent>(evt => Close());

        tabAll?.RegisterCallback<ClickEvent>(evt => SetFilter("All"));
        tabBeginner?.RegisterCallback<ClickEvent>(evt => SetFilter("Beginner"));
        tabElementary?.RegisterCallback<ClickEvent>(evt => SetFilter("Elementary"));
        tabIntermediate?.RegisterCallback<ClickEvent>(evt => SetFilter("Intermediate"));
    }

    private void RefreshPointsDisplay()
    {
        if (pointsLabel != null)
        {
            int points = PointManager.Instance.GetAvailableNotes();
            pointsLabel.text = points.ToString("N0");
        }
    }

    private void SetFilter(string filter)
    {
        currentFilter = filter;

        // 탭 활성화 상태 업데이트
        UpdateTabStyles();
        LoadSongs();
    }

    private void UpdateTabStyles()
    {
        tabAll?.RemoveFromClassList("active");
        tabBeginner?.RemoveFromClassList("active");
        tabElementary?.RemoveFromClassList("active");
        tabIntermediate?.RemoveFromClassList("active");

        switch (currentFilter)
        {
            case "All":
                tabAll?.AddToClassList("active");
                break;
            case "Beginner":
                tabBeginner?.AddToClassList("active");
                break;
            case "Elementary":
                tabElementary?.AddToClassList("active");
                break;
            case "Intermediate":
                tabIntermediate?.AddToClassList("active");
                break;
        }
    }

    private void LoadSongs()
    {
        if (songList == null) return;

        songList.Clear();

        List<SongItem> songs;
        if (currentFilter == "All")
        {
            songs = SongShopManager.Instance.GetAllSongs();
        }
        else
        {
            songs = SongShopManager.Instance.GetSongsByDifficulty(currentFilter);
        }

        foreach (var song in songs)
        {
            var card = CreateSongCard(song);
            songList.Add(card);
        }

        // 빈 상태 처리
        if (songs.Count == 0)
        {
            var emptyState = new VisualElement();
            emptyState.AddToClassList("empty-state");

            var emptyIcon = new Label("🎵");
            emptyIcon.AddToClassList("empty-icon");
            emptyState.Add(emptyIcon);

            var emptyText = new Label("노래가 없습니다");
            emptyText.AddToClassList("empty-text");
            emptyState.Add(emptyText);

            songList.Add(emptyState);
        }
    }

    private VisualElement CreateSongCard(SongItem song)
    {
        var card = new VisualElement();
        card.AddToClassList("song-card");
        if (song.isPurchased) card.AddToClassList("purchased");

        // 썸네일
        var thumbnail = new VisualElement();
        thumbnail.AddToClassList("song-thumbnail");
        if (!string.IsNullOrEmpty(song.thumbnailPath))
        {
            var tex = Resources.Load<Texture2D>(song.thumbnailPath);
            if (tex != null)
            {
                thumbnail.style.backgroundImage = new StyleBackground(tex);
            }
        }
        card.Add(thumbnail);

        // 상세 정보
        var details = new VisualElement();
        details.AddToClassList("song-details");

        var title = new Label(song.title);
        title.AddToClassList("song-card-title");
        details.Add(title);

        var artist = new Label(song.artist);
        artist.AddToClassList("song-card-artist");
        details.Add(artist);

        var meta = new VisualElement();
        meta.AddToClassList("song-card-meta");

        var diffBadge = new Label(song.difficulty);
        diffBadge.AddToClassList("difficulty-badge");
        meta.Add(diffBadge);

        var duration = new Label(song.GetFormattedDuration());
        duration.AddToClassList("song-duration");
        meta.Add(duration);

        details.Add(meta);
        card.Add(details);

        // 구매 섹션
        var purchaseSection = new VisualElement();
        purchaseSection.AddToClassList("purchase-section");

        if (song.isPurchased)
        {
            var ownedBtn = new Button();
            ownedBtn.AddToClassList("buy-button");
            ownedBtn.AddToClassList("purchased");
            ownedBtn.text = "보유중";
            ownedBtn.SetEnabled(false);
            purchaseSection.Add(ownedBtn);
        }
        else if (song.isFree)
        {
            var freeBtn = new Button();
            freeBtn.AddToClassList("free-badge");
            freeBtn.text = "무료";
            freeBtn.RegisterCallback<ClickEvent>(evt => PlaySong(song));
            purchaseSection.Add(freeBtn);
        }
        else
        {
            // 가격 표시
            var priceTag = new VisualElement();
            priceTag.AddToClassList("price-tag");

            var priceIcon = new Label("♪");
            priceIcon.AddToClassList("price-icon");
            priceTag.Add(priceIcon);

            var priceValue = new Label(song.price.ToString());
            priceValue.AddToClassList("price-value");
            priceTag.Add(priceValue);

            purchaseSection.Add(priceTag);

            // 구매 버튼
            var buyBtn = new Button();
            buyBtn.AddToClassList("buy-button");

            bool canAfford = PointManager.Instance.CanAfford(song.price);
            buyBtn.text = canAfford ? "구매" : "부족";
            buyBtn.SetEnabled(canAfford);

            buyBtn.RegisterCallback<ClickEvent>(evt => TryPurchase(song, buyBtn));
            purchaseSection.Add(buyBtn);
        }

        card.Add(purchaseSection);

        return card;
    }

    private void TryPurchase(SongItem song, Button buyButton)
    {
        if (SongShopManager.Instance.TryPurchaseSong(song.chapterId, out string error))
        {
            // 성공 - UI 업데이트는 이벤트 핸들러에서 처리
            Debug.Log($"[SongShopController] Successfully purchased: {song.title}");
        }
        else
        {
            Debug.LogWarning($"[SongShopController] Purchase failed: {error}");
            // 실패 알림 표시 (PopupManager 사용 가능)
            if (PopupManager.Instance != null)
            {
                PopupManager.Instance.ShowPopup("구매 실패", error, "확인", null);
            }
        }
    }

    private void PlaySong(SongItem song)
    {
        MusicPlayerManager.Instance.Play(song);
        Close();
    }

    private void OnPurchaseSuccess(SongItem song)
    {
        RefreshPointsDisplay();
        LoadSongs();
    }

    private void OnPointsChanged(int newAmount)
    {
        RefreshPointsDisplay();
        LoadSongs(); // 구매 가능 상태 업데이트
    }

    private void Close()
    {
        Dispose();
        onCloseCallback?.Invoke();
    }
}
