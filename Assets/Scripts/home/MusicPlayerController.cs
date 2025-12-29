using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RhythmEnglish.Economy;
using RhythmEnglish.MusicPlayer;

/// <summary>
/// 뮤직 플레이어 UI 컨트롤러
/// Play Tab의 메인 컨트롤러 - 노래 리스트 + 미니 플레이어 + 전체 화면 플레이어
/// </summary>
public class MusicPlayerController : MonoBehaviour
{
    [Header("UXML Assets")]
    [SerializeField] private VisualTreeAsset nowPlayingViewUxml;

    private UIDocument uiDocument;
    private VisualElement root;

    // === 노래 리스트 UI ===
    private VisualElement songListContainer;
    private ScrollView songListScroll;

    // === 미니 플레이어 UI ===
    private VisualElement miniPlayer;
    private VisualElement miniAlbumArt;
    private Label miniSongTitle;
    private Label miniArtistName;
    private Button miniPlayPauseButton;
    private Button miniNextButton;
    private VisualElement miniPlayerTapArea;

    // === 전체 화면 플레이어 (Now Playing) ===
    private VisualElement nowPlayingOverlay;
    private VisualElement albumArt;
    private Label songTitle;
    private Label artistName;
    private Button favoriteButton;
    private Slider progressSlider;
    private Label currentTimeLabel;
    private Label totalTimeLabel;
    private Button shuffleButton;
    private Button prevButton;
    private Button playPauseButton;
    private Button nextButton;
    private Button repeatButton;
    private Button backButton;

    // State
    private bool isDraggingSlider = false;
    private bool isNowPlayingVisible = false;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        
        // BottomMenu(sortingOrder=1)보다 아래에 렌더링되도록 설정
        if (uiDocument != null)
        {
            uiDocument.sortingOrder = 0;
        }
    }

    private void OnEnable()
    {
        InitializeUI();
        SubscribeToEvents();
        RefreshSongList();
        UpdateMiniPlayer();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    // ========== 초기화 ==========

    private void InitializeUI()
    {
        root = uiDocument.rootVisualElement;

        // 노래 리스트
        songListContainer = root.Q<VisualElement>("SongListContainer");
        songListScroll = root.Q<ScrollView>("SongListScroll");

        // 미니 플레이어
        miniPlayer = root.Q<VisualElement>("MiniPlayer");
        miniAlbumArt = root.Q<VisualElement>("MiniAlbumArt");
        miniSongTitle = root.Q<Label>("MiniSongTitle");
        miniArtistName = root.Q<Label>("MiniArtistName");
        miniPlayPauseButton = root.Q<Button>("MiniPlayPauseButton");
        miniNextButton = root.Q<Button>("MiniNextButton");
        miniPlayerTapArea = root.Q<VisualElement>("MiniPlayerTapArea");

        // 미니 플레이어 이벤트
        miniPlayPauseButton?.RegisterCallback<ClickEvent>(evt => OnPlayPauseClicked());
        miniNextButton?.RegisterCallback<ClickEvent>(evt => OnNextClicked());
        miniPlayerTapArea?.RegisterCallback<ClickEvent>(evt => ShowNowPlaying());

        // 헤더 버튼
        var searchButton = root.Q<Button>("SearchButton");
        var filterButton = root.Q<Button>("FilterButton");
        searchButton?.RegisterCallback<ClickEvent>(evt => OnSearchClicked());
        filterButton?.RegisterCallback<ClickEvent>(evt => OnFilterClicked());
    }

    private void SubscribeToEvents()
    {
        var player = MusicPlayerManager.Instance;
        player.OnSongChanged += OnSongChanged;
        player.OnPositionChanged += OnPositionChanged;
        player.OnPlayStateChanged += OnPlayStateChanged;
        player.OnShuffleChanged += OnShuffleChanged;
        player.OnRepeatModeChanged += OnRepeatModeChanged;

        // 구매 이벤트
        SongShopManager.Instance.OnPurchaseSuccess += OnPurchaseSuccess;
    }

    private void UnsubscribeFromEvents()
    {
        if (MusicPlayerManager.Instance != null)
        {
            var player = MusicPlayerManager.Instance;
            player.OnSongChanged -= OnSongChanged;
            player.OnPositionChanged -= OnPositionChanged;
            player.OnPlayStateChanged -= OnPlayStateChanged;
            player.OnShuffleChanged -= OnShuffleChanged;
            player.OnRepeatModeChanged -= OnRepeatModeChanged;
        }

        if (SongShopManager.Instance != null)
        {
            SongShopManager.Instance.OnPurchaseSuccess -= OnPurchaseSuccess;
        }
    }

    // ========== 노래 리스트 ==========

    private void RefreshSongList()
    {
        if (songListContainer == null) return;

        songListContainer.Clear();

        // ProgressManager에서 테스트 완료된 Step 목록 가져오기
        var completedSongs = GetCompletedSongs();

        if (completedSongs.Count == 0)
        {
            ShowEmptyState();
            return;
        }

        foreach (var songInfo in completedSongs)
        {
            var card = CreateSongCard(songInfo);
            songListContainer.Add(card);
        }
    }

    /// <summary>
    /// ProgressManager에서 테스트 완료된 Step 정보 가져오기
    /// </summary>
    private List<CompletedSongInfo> GetCompletedSongs()
    {
        var completedSongs = new List<CompletedSongInfo>();
        var pm = ProgressManager.Instance;

        Debug.Log($"[MusicPlayerController] GetCompletedSongs - Courses count: {pm.Courses.Count}");

        // 모든 코스의 진행 상황 확인
        foreach (var courseKvp in pm.Courses)
        {
            string courseId = courseKvp.Key;
            var courseProgress = courseKvp.Value;

            Debug.Log($"[MusicPlayerController] Course: {courseId}, Chapters: {courseProgress.Chapters.Count}");

            foreach (var chapterKvp in courseProgress.Chapters)
            {
                string chapterId = chapterKvp.Key;
                var chapterProgress = chapterKvp.Value;

                Debug.Log($"[MusicPlayerController] Chapter: {chapterId}, Steps: {chapterProgress.Steps.Count}");

                foreach (var stepKvp in chapterProgress.Steps)
                {
                    var stepProgress = stepKvp.Value;

                    Debug.Log($"[MusicPlayerController] Step: {stepProgress.StepId}, TestCompleted: {stepProgress.TestCompleted}");

                    // TestCompleted가 true인 Step만 추가
                    if (stepProgress.TestCompleted)
                    {
                        // CurriculumRepository에서 곡 정보 가져오기
                        if (CurriculumRepository.TryGetChapter(chapterId, out var chapter))
                        {
                            Debug.Log($"[MusicPlayerController] ✅ Adding song: {chapter.Name}");
                            completedSongs.Add(new CompletedSongInfo
                            {
                                ChapterId = chapterId,
                                StepId = stepProgress.StepId,
                                Title = chapter.Name ?? chapterId,
                                Artist = "", // 현재 ChapterData에 Artist 없음
                                ThumbnailPath = "" // 현재 ChapterData에 ThumbnailPath 없음
                            });
                        }
                        else
                        {
                            Debug.LogWarning($"[MusicPlayerController] ⚠️ Chapter not found: {chapterId}");
                        }
                    }
                }
            }
        }

        Debug.Log($"[MusicPlayerController] Total completed songs: {completedSongs.Count}");
        return completedSongs;
    }

    /// <summary>
    /// 완료된 곡 정보 구조체
    /// </summary>
    private class CompletedSongInfo
    {
        public string ChapterId;
        public string StepId;
        public string Title;
        public string Artist;
        public string ThumbnailPath;
    }

    private VisualElement CreateSongCard(CompletedSongInfo songInfo)
    {
        var card = new VisualElement();
        card.AddToClassList("song-card");

        // 썸네일
        var thumbnail = new VisualElement();
        thumbnail.AddToClassList("song-thumbnail");
        if (!string.IsNullOrEmpty(songInfo.ThumbnailPath))
        {
            var tex = Resources.Load<Texture2D>(songInfo.ThumbnailPath);
            if (tex != null)
            {
                thumbnail.style.backgroundImage = new StyleBackground(tex);
            }
        }
        card.Add(thumbnail);

        // 곡 정보
        var info = new VisualElement();
        info.AddToClassList("song-info");

        var title = new Label(songInfo.Title);
        title.AddToClassList("song-title");
        info.Add(title);

        var artist = new Label(songInfo.Artist);
        artist.AddToClassList("song-artist");
        info.Add(artist);

        card.Add(info);

        // 액션 영역 - 재생 버튼 + 체크 아이콘
        var actionArea = new VisualElement();
        actionArea.AddToClassList("song-action-area");

        // 재생 버튼 (아이콘)
        var playBtn = new Button();
        playBtn.AddToClassList("play-song-button");
        
        // 아이콘 이미지 로드 및 설정
        var playIcon = Resources.Load<Texture2D>("Icons/function_icon_player_start");
        if (playIcon != null)
        {
            playBtn.iconImage = playIcon;
        }
        
        playBtn.RegisterCallback<ClickEvent>(evt =>
        {
            PlaySong(songInfo);
            evt.StopPropagation();
        });
        actionArea.Add(playBtn);

        // 구매 완료 체크 아이콘
        var check = new VisualElement();
        check.AddToClassList("owned-check");
        actionArea.Add(check);

        card.Add(actionArea);

        return card;
    }

    private void ShowEmptyState()
    {
        var emptyState = new VisualElement();
        emptyState.AddToClassList("empty-state");

        var icon = new Label("🎵");
        icon.AddToClassList("empty-icon");
        emptyState.Add(icon);

        var title = new Label("아직 완료한 곡이 없습니다");
        title.AddToClassList("empty-title");
        emptyState.Add(title);

        var desc = new Label("Step에서 테스트를 완료하면\n음원을 재생할 수 있습니다!");
        desc.AddToClassList("empty-description");
        emptyState.Add(desc);

        songListContainer.Add(emptyState);
    }

    // ========== 미니 플레이어 ==========

    private void UpdateMiniPlayer()
    {
        if (miniPlayer == null) return;

        var player = MusicPlayerManager.Instance;
        var currentSong = player.CurrentSong;

        if (currentSong == null)
        {
            // 재생 중인 곡 없음 - 미니 플레이어 숨김
            miniPlayer.AddToClassList("hidden");
            return;
        }

        // 재생 중 - 미니 플레이어 표시
        miniPlayer.RemoveFromClassList("hidden");

        // 현재 곡 정보 표시
        if (miniSongTitle != null) miniSongTitle.text = currentSong.title;
        if (miniArtistName != null) miniArtistName.text = currentSong.artist;

        // 앨범 아트
        if (miniAlbumArt != null && !string.IsNullOrEmpty(currentSong.thumbnailPath))
        {
            var tex = Resources.Load<Texture2D>(currentSong.thumbnailPath);
            if (tex != null)
            {
                miniAlbumArt.style.backgroundImage = new StyleBackground(tex);
            }
        }

        // 재생/일시정지 버튼 아이콘 전환
        if (miniPlayPauseButton != null)
        {
            UpdateMiniPlayPauseIcon(player.IsPlaying);
        }
    }

    private void UpdateMiniPlayPauseIcon(bool isPlaying)
    {
        if (miniPlayPauseButton == null) return;
        
        if (isPlaying)
        {
            miniPlayPauseButton.RemoveFromClassList("icon-play-mini");
            miniPlayPauseButton.AddToClassList("icon-pause-mini");
        }
        else
        {
            miniPlayPauseButton.RemoveFromClassList("icon-pause-mini");
            miniPlayPauseButton.AddToClassList("icon-play-mini");
        }
    }

    // ========== 전체 화면 플레이어 ==========

    private void ShowNowPlaying()
    {
        if (nowPlayingViewUxml == null)
        {
            Debug.LogWarning("[MusicPlayerController] NowPlayingView UXML not assigned!");
            return;
        }

        if (nowPlayingOverlay != null)
        {
            nowPlayingOverlay.RemoveFromClassList("hidden");
            isNowPlayingVisible = true;
            return;
        }

        // 생성
        nowPlayingOverlay = nowPlayingViewUxml.CloneTree().Q<VisualElement>("NowPlayingOverlay");
        root.Add(nowPlayingOverlay);

        // UI 요소 바인딩
        backButton = nowPlayingOverlay.Q<Button>("BackButton");
        albumArt = nowPlayingOverlay.Q<VisualElement>("AlbumArt");
        songTitle = nowPlayingOverlay.Q<Label>("SongTitle");
        artistName = nowPlayingOverlay.Q<Label>("ArtistName");
        favoriteButton = nowPlayingOverlay.Q<Button>("FavoriteButton");
        progressSlider = nowPlayingOverlay.Q<Slider>("ProgressSlider");
        currentTimeLabel = nowPlayingOverlay.Q<Label>("CurrentTime");
        totalTimeLabel = nowPlayingOverlay.Q<Label>("TotalTime");
        shuffleButton = nowPlayingOverlay.Q<Button>("ShuffleButton");
        prevButton = nowPlayingOverlay.Q<Button>("PrevButton");
        playPauseButton = nowPlayingOverlay.Q<Button>("PlayPauseButton");
        nextButton = nowPlayingOverlay.Q<Button>("NextButton");
        repeatButton = nowPlayingOverlay.Q<Button>("RepeatButton");

        // 이벤트 바인딩
        backButton?.RegisterCallback<ClickEvent>(evt => HideNowPlaying());
        favoriteButton?.RegisterCallback<ClickEvent>(evt => OnFavoriteClicked());
        shuffleButton?.RegisterCallback<ClickEvent>(evt => OnShuffleClicked());
        prevButton?.RegisterCallback<ClickEvent>(evt => OnPrevClicked());
        playPauseButton?.RegisterCallback<ClickEvent>(evt => OnPlayPauseClicked());
        nextButton?.RegisterCallback<ClickEvent>(evt => OnNextClicked());
        repeatButton?.RegisterCallback<ClickEvent>(evt => OnRepeatClicked());

        // 슬라이더 이벤트
        if (progressSlider != null)
        {
            progressSlider.RegisterCallback<PointerDownEvent>(evt => isDraggingSlider = true);
            progressSlider.RegisterCallback<PointerUpEvent>(evt =>
            {
                isDraggingSlider = false;
                MusicPlayerManager.Instance.Seek(progressSlider.value);
            });
        }

        UpdateNowPlayingUI();
        isNowPlayingVisible = true;
    }

    private void HideNowPlaying()
    {
        nowPlayingOverlay?.AddToClassList("hidden");
        isNowPlayingVisible = false;
    }

    private void UpdateNowPlayingUI()
    {
        var player = MusicPlayerManager.Instance;
        var currentSong = player.CurrentSong;

        if (currentSong == null) return;

        if (songTitle != null) songTitle.text = currentSong.title;
        if (artistName != null) artistName.text = currentSong.artist;

        // 앨범 아트
        if (albumArt != null && !string.IsNullOrEmpty(currentSong.thumbnailPath))
        {
            var tex = Resources.Load<Texture2D>(currentSong.thumbnailPath);
            if (tex != null)
            {
                albumArt.style.backgroundImage = new StyleBackground(tex);
            }
        }

        // 총 재생 시간
        if (totalTimeLabel != null)
        {
            totalTimeLabel.text = currentSong.GetFormattedDuration();
        }

        // 즐겨찾기 상태
        bool isFavorite = player.IsFavorite(currentSong.chapterId);
        UpdateFavoriteButton(isFavorite);

        // 셔플/반복 상태
        UpdateShuffleButton(player.IsShuffleOn);
        UpdateRepeatButton(player.CurrentRepeatMode);
        UpdatePlayPauseButton(player.IsPlaying);
    }

    // ========== 이벤트 핸들러 ==========

    private void OnSongChanged(SongItem song)
    {
        UpdateMiniPlayer();
        if (isNowPlayingVisible)
        {
            UpdateNowPlayingUI();
        }
    }

    private void OnPositionChanged(float position)
    {
        if (!isDraggingSlider && progressSlider != null)
        {
            progressSlider.value = position;
        }

        if (currentTimeLabel != null)
        {
            float currentTime = MusicPlayerManager.Instance.CurrentTime;
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            currentTimeLabel.text = $"{minutes}:{seconds:D2}";
        }
    }

    private void OnPlayStateChanged(bool isPlaying)
    {
        UpdatePlayPauseButton(isPlaying);
        UpdateMiniPlayPauseIcon(isPlaying);
    }

    private void OnShuffleChanged(bool isShuffleOn)
    {
        UpdateShuffleButton(isShuffleOn);
    }

    private void OnRepeatModeChanged(RepeatMode mode)
    {
        UpdateRepeatButton(mode);
    }

    private void OnPurchaseSuccess(SongItem song)
    {
        RefreshSongList();
    }

    // ========== 액션 ==========

    private void PlaySong(CompletedSongInfo songInfo)
    {
        Debug.Log($"[MusicPlayerController] Playing: {songInfo.Title} (Chapter: {songInfo.ChapterId})");

        // Chapter에서 오디오 경로 가져오기
        if (CurriculumRepository.TryGetChapter(songInfo.ChapterId, out var chapter))
        {
            // SongItem 생성하여 MusicPlayerManager에 전달
            var songItem = new SongItem
            {
                chapterId = songInfo.ChapterId,
                title = songInfo.Title,
                artist = songInfo.Artist,
                thumbnailPath = songInfo.ThumbnailPath,
                fullAudioPath = $"Audio/Songs/{songInfo.ChapterId}/full",
                isFree = true,
                isPurchased = true
            };

            // 완료된 곡들로 플레이리스트 구성
            var completedSongs = GetCompletedSongs();
            var playlist = new List<SongItem>();
            int startIndex = 0;

            for (int i = 0; i < completedSongs.Count; i++)
            {
                var info = completedSongs[i];
                if (CurriculumRepository.TryGetChapter(info.ChapterId, out var ch))
                {
                    playlist.Add(new SongItem
                    {
                        chapterId = info.ChapterId,
                        title = info.Title,
                        artist = info.Artist,
                        thumbnailPath = info.ThumbnailPath,
                        fullAudioPath = $"Audio/Songs/{info.ChapterId}/full",
                        isFree = true,
                        isPurchased = true
                    });

                    if (info.ChapterId == songInfo.ChapterId)
                    {
                        startIndex = i;
                    }
                }
            }

            if (playlist.Count > 0)
            {
                MusicPlayerManager.Instance.PlayPlaylist(playlist, startIndex);
            }
        }
        else
        {
            Debug.LogWarning($"[MusicPlayerController] Chapter not found: {songInfo.ChapterId}");
        }
    }

    private void OnPlayPauseClicked()
    {
        var player = MusicPlayerManager.Instance;
        if (player.IsPlaying)
        {
            player.Pause();
        }
        else
        {
            if (player.CurrentSong == null)
            {
                var purchased = SongShopManager.Instance.GetPurchasedSongs();
                if (purchased.Count > 0)
                {
                    player.PlayPlaylist(purchased);
                }
            }
            else
            {
                player.Play();
            }
        }
    }

    private void OnPrevClicked() => MusicPlayerManager.Instance.Previous();
    private void OnNextClicked() => MusicPlayerManager.Instance.Next();
    private void OnShuffleClicked() => MusicPlayerManager.Instance.ToggleShuffle();
    private void OnRepeatClicked() => MusicPlayerManager.Instance.ToggleRepeat();

    private void OnFavoriteClicked()
    {
        var currentSong = MusicPlayerManager.Instance.CurrentSong;
        if (currentSong == null) return;

        bool isFavorite = MusicPlayerManager.Instance.ToggleFavorite(currentSong.chapterId);
        UpdateFavoriteButton(isFavorite);
    }

    private void OnSearchClicked()
    {
        // TODO: 검색 기능 구현
        Debug.Log("[MusicPlayerController] Search clicked");
    }

    private void OnFilterClicked()
    {
        // TODO: 필터 기능 구현
        Debug.Log("[MusicPlayerController] Filter clicked");
    }

    // ========== UI 업데이트 헬퍼 ==========

    private void UpdatePlayPauseButton(bool isPlaying)
    {
        if (playPauseButton != null)
        {
            playPauseButton.text = isPlaying ? "⏸" : "▶";
        }
    }

    private void UpdateShuffleButton(bool isShuffleOn)
    {
        if (shuffleButton != null)
        {
            if (isShuffleOn)
                shuffleButton.AddToClassList("active");
            else
                shuffleButton.RemoveFromClassList("active");
        }
    }

    private void UpdateRepeatButton(RepeatMode mode)
    {
        if (repeatButton != null)
        {
            repeatButton.text = mode == RepeatMode.One ? "🔂" : "🔁";

            if (mode != RepeatMode.Off)
                repeatButton.AddToClassList("active");
            else
                repeatButton.RemoveFromClassList("active");
        }
    }

    private void UpdateFavoriteButton(bool isFavorite)
    {
        if (favoriteButton != null)
        {
            favoriteButton.text = isFavorite ? "♥" : "♡";
            if (isFavorite)
                favoriteButton.AddToClassList("active");
            else
                favoriteButton.RemoveFromClassList("active");
        }
    }
}
