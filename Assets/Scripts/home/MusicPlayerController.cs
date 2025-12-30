using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private Slider progressSlider;
    private Label currentTimeLabel;
    private Label totalTimeLabel;
    private Button shuffleButton;
    private Button prevButton;
    private Button playPauseButton;
    private Button nextButton;
    private Button repeatButton;
    private Button backButton;

    // 신규 추가 요소
    private Slider vocalVolumeSlider;
    private VisualElement lyricsTextLine; // 레거시 참조 및 레이아웃 용
    private ScrollView lyricsScroll;

    // 가사 하이라이팅을 위한 데이터
    private class LyricsLine 
    { 
        public string text; 
        public float startTime; 
        public float endTime; 
        public Label label; 
    }
    private List<LyricsLine> currentLyricsLines = new List<LyricsLine>();
    private int currentLyricIndex = -1;

    // State
    private bool isDraggingSlider = false;
    private bool isNowPlayingVisible = false;
    private Dictionary<string, bool> playlistSelection = new Dictionary<string, bool>(); // ChapterId -> IsSelected

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

        // 카탈로그 데이터 최신화 (새로 추가된 곡 등이 있을 수 있음)
        SongShopManager.Instance.RefreshCatalog();

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
                            // 해당 스텝의 타이틀 찾기
                            var stepData = chapter.Steps.FirstOrDefault(s => s.id == stepProgress.StepId);
                            string displayTitle = (stepData != null && !string.IsNullOrEmpty(stepData.title)) 
                                ? stepData.title 
                                : (chapter.Name ?? chapterId);

                            string thumbnailPath = $"Covers/{chapterId}/{stepProgress.StepId}";

                            Debug.Log($"[MusicPlayerController] ✅ Adding song: {displayTitle}, Thumbnail: {thumbnailPath}");
                            completedSongs.Add(new CompletedSongInfo
                            {
                                ChapterId = chapterId,
                                StepId = stepProgress.StepId,
                                Title = displayTitle,
                                Artist = chapter.Name ?? "Rhythm English", // fallback
                                ThumbnailPath = thumbnailPath
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

        // SongItem 정보 가져오기 (가격 및 구매 상태 확인용)
        var songData = SongShopManager.Instance.GetSongInfo(songInfo.ChapterId);
        bool isPurchased = SongShopManager.Instance.IsPurchased(songInfo.ChapterId);

        // 썸네일
        var thumbnail = new VisualElement();
        thumbnail.AddToClassList("song-thumbnail");
        if (!string.IsNullOrEmpty(songInfo.ThumbnailPath))
        {
            var sprite = Resources.Load<Sprite>(songInfo.ThumbnailPath);
            if (sprite != null)
            {
                thumbnail.style.backgroundImage = new StyleBackground(sprite);
            }
        }
        card.Add(thumbnail);

        // 곡 정보
        var info = new VisualElement();
        info.AddToClassList("song-info");

        var title = new Label(songInfo.Title);
        title.AddToClassList("song-title");
        info.Add(title);

        var artistNameStr = songData != null ? songData.artist : songInfo.Artist;
        var artist = new Label(artistNameStr);
        artist.AddToClassList("song-artist");
        info.Add(artist);

        card.Add(info);

        // 액션 영역
        var actionArea = new VisualElement();
        actionArea.AddToClassList("song-action-area");

        if (isPurchased)
        {
            // 구매 완료: 체크 아이콘 (플레이리스트 포함 여부 토글)
            var check = new VisualElement();
            check.AddToClassList("owned-check");
            
            // 초기 상태 설정 (구매된 곡은 디폴트가 선택 상태)
            if (!playlistSelection.ContainsKey(songInfo.ChapterId))
            {
                playlistSelection[songInfo.ChapterId] = true;
            }
            
            bool isSelected = playlistSelection[songInfo.ChapterId];
            if (isSelected) check.AddToClassList("active");

            check.RegisterCallback<ClickEvent>(evt =>
            {
                bool newState = !playlistSelection[songInfo.ChapterId];
                playlistSelection[songInfo.ChapterId] = newState;
                
                if (newState) check.AddToClassList("active");
                else check.RemoveFromClassList("active");
                
                Debug.Log($"[MusicPlayerController] Song {songInfo.Title} playlist selection: {newState}");
                evt.StopPropagation(); // 카드 클릭(재생) 방지
            });

            actionArea.Add(check);
        }
        else
        {
            // 카탈로그에 없어도 완료된 곡이라면 구매 버튼을 표시하거나 
            // 기본값으로 처리 (여기서는 기본 구매 버튼 표시)
            var buyBtn = new Button();
            buyBtn.AddToClassList("buy-button");
            int price = songData?.price ?? 500;
            buyBtn.text = $"♪{price}\nBuy";
            actionArea.Add(buyBtn);

            buyBtn.RegisterCallback<ClickEvent>(evt =>
            {
                if (songData != null)
                {
                    RequestPurchase(songData);
                }
                else
                {
                    // 카탈로그에 없는 경우에 대한 구매 로직 (기본 처리)
                     Debug.LogWarning($"[MusicPlayerController] Song not in catalog: {songInfo.ChapterId}");
                }
                evt.StopPropagation();
            });
        }

        card.Add(actionArea);

        // 카드 전체 클릭 처리
        card.RegisterCallback<ClickEvent>(evt =>
        {
            if (isPurchased)
            {
                PlaySong(songInfo);
                ShowNowPlaying();
            }
            else if (songData != null)
            {
                RequestPurchase(songData);
            }
        });

        return card;
    }

    private void RequestPurchase(SongItem song)
    {
        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.ShowPopup(
                "곡 구매", 
                $"'{song.title}' 곡을 {song.price} Notes로 구매하시겠습니까?",
                "구매", 
                () => TryPurchase(song),
                "취소",
                null
            );
        }
        else
        {
            // PopupManager가 없으면 기존처럼 즉시 구매 시도
            TryPurchase(song);
        }
    }

    private void TryPurchase(SongItem song)
    {
        if (SongShopManager.Instance.TryPurchaseSong(song.chapterId, out string error))
        {
            Debug.Log($"[MusicPlayerController] Successfully purchased: {song.title}");
            RefreshSongList(); // 구매 후 목록 갱신
            
            // 구매 성공 알림 (선택 사항)
            PopupManager.Instance?.ShowPopup("구매 완료", $"'{song.title}' 곡을 구매했습니다.", "확인");
        }
        else
        {
            Debug.LogWarning($"[MusicPlayerController] Purchase failed: {error}");
            PopupManager.Instance?.ShowPopup("구매 실패", error, "확인");
        }
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
            var sprite = Resources.Load<Sprite>(currentSong.thumbnailPath);
            if (sprite != null)
            {
                miniAlbumArt.style.backgroundImage = new StyleBackground(sprite);
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
            if (uiDocument != null) uiDocument.sortingOrder = 10; // 플레이 화면 오픈 시 맨 앞으로
            UpdateNowPlayingUI(); // 매번 최신 상태로 갱신
            isNowPlayingVisible = true;
            return;
        }

        // 생성
        var template = nowPlayingViewUxml.CloneTree();
        // 템플릿 컨테이너 스타일 설정: 전체 화면을 차지하면서 클릭은 통과시킴 (하위 요소만 클릭 가능하게)
        template.style.position = Position.Absolute;
        template.style.width = new Length(100, LengthUnit.Percent);
        template.style.height = new Length(100, LengthUnit.Percent);
        template.pickingMode = PickingMode.Ignore;

        nowPlayingOverlay = template.Q<VisualElement>("NowPlayingOverlay");
        root.Add(template);
        if (uiDocument != null) uiDocument.sortingOrder = 10; // 생성 시에도 맨 앞으로

        // UI 요소 바인딩
        backButton = nowPlayingOverlay.Q<Button>("BackButton");
        albumArt = nowPlayingOverlay.Q<VisualElement>("AlbumArt");
        songTitle = nowPlayingOverlay.Q<Label>("SongTitle");
        artistName = nowPlayingOverlay.Q<Label>("ArtistName");
        progressSlider = nowPlayingOverlay.Q<Slider>("ProgressSlider");
        currentTimeLabel = nowPlayingOverlay.Q<Label>("CurrentTime");
        totalTimeLabel = nowPlayingOverlay.Q<Label>("TotalTime");
        shuffleButton = nowPlayingOverlay.Q<Button>("ShuffleButton");
        prevButton = nowPlayingOverlay.Q<Button>("PrevButton");
        playPauseButton = nowPlayingOverlay.Q<Button>("PlayPauseButton");
        nextButton = nowPlayingOverlay.Q<Button>("NextButton");
        repeatButton = nowPlayingOverlay.Q<Button>("RepeatButton");

        // 신규 요소 바인딩
        vocalVolumeSlider = nowPlayingOverlay.Q<Slider>("VocalVolumeSlider");
        lyricsTextLine = nowPlayingOverlay.Q<Label>("LyricsText"); // UI에서 여전히 이 이름일 것임
        lyricsScroll = nowPlayingOverlay.Q<ScrollView>("LyricsScroll");

        // 이벤트 바인딩
        backButton?.RegisterCallback<ClickEvent>(evt => HideNowPlaying());
        shuffleButton?.RegisterCallback<ClickEvent>(evt => OnShuffleClicked());
        prevButton?.RegisterCallback<ClickEvent>(evt => OnPrevClicked());
        playPauseButton?.RegisterCallback<ClickEvent>(evt => OnPlayPauseClicked());
        nextButton?.RegisterCallback<ClickEvent>(evt => OnNextClicked());
        repeatButton?.RegisterCallback<ClickEvent>(evt => OnRepeatClicked());

        // 진행바 설정 및 이벤트
        if (progressSlider != null)
        {
            progressSlider.lowValue = 0f;
            progressSlider.highValue = 1f;

            // 드래그 시작 감지
            progressSlider.RegisterCallback<PointerDownEvent>(evt => {
                isDraggingSlider = true;
                Debug.Log($"[LyricsDebug] Slider Drag Start - Value: {progressSlider.value}");
            }, TrickleDown.TrickleDown);

            // 드래그 종료 감지 (포인터 캡처 해제 시)
            progressSlider.RegisterCallback<PointerCaptureOutEvent>(evt => {
                if (isDraggingSlider) {
                    float targetPos = progressSlider.value;
                    Debug.Log($"[LyricsDebug] Slider Drag End (Capture Released) - Target: {targetPos}");
                    
                    MusicPlayerManager.Instance.Seek(targetPos);
                    UpdateLyricsHighlight(targetPos * MusicPlayerManager.Instance.Duration);
                    
                    isDraggingSlider = false;
                }
            });

            // 드래그 중 시간 업데이트
            progressSlider.RegisterValueChangedCallback(evt => {
                if (isDraggingSlider) {
                    float duration = MusicPlayerManager.Instance.Duration;
                    float dragTime = evt.newValue * duration;
                    int minutes = Mathf.FloorToInt(dragTime / 60);
                    int seconds = Mathf.FloorToInt(dragTime % 60);
                    if (currentTimeLabel != null) currentTimeLabel.text = $"{minutes}:{seconds:D2}";
                }
            });
        }

        // 보컬 슬라이더 이벤트
        if (vocalVolumeSlider != null)
        {
            vocalVolumeSlider.lowValue = 0f;
            vocalVolumeSlider.highValue = 1f;
            vocalVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                MusicPlayerManager.Instance.VocalVolume = evt.newValue;
            });
        }

        UpdateNowPlayingUI();
        isNowPlayingVisible = true;
    }

    private void HideNowPlaying()
    {
        nowPlayingOverlay?.AddToClassList("hidden");
        if (uiDocument != null) uiDocument.sortingOrder = 0; // 뒤로 가기 시 원래 순서로
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
            var sprite = Resources.Load<Sprite>(currentSong.thumbnailPath);
            if (sprite != null)
            {
                albumArt.style.backgroundImage = new StyleBackground(sprite);
            }
        }

        // 총 재생 시간
        if (totalTimeLabel != null)
        {
            totalTimeLabel.text = currentSong.GetFormattedDuration();
        }

        // 가사 로드 및 표시
        UpdateLyrics(currentSong);

        // 보컬 볼륨 슬라이더 값 동기화
        if (vocalVolumeSlider != null)
        {
            vocalVolumeSlider.value = player.VocalVolume;
        }

        // 셔플/반복 상태
        UpdateShuffleButton(player.IsShuffleOn);
        UpdateRepeatButton(player.CurrentRepeatMode);
        UpdatePlayPauseButton(player.IsPlaying);
    }

    private void UpdateLyrics(SongItem song)
    {
        if (lyricsScroll == null) return;

        lyricsScroll.Clear();
        currentLyricsLines.Clear();
        currentLyricIndex = -1;

        if (CurriculumRepository.TryGetChapter(song.chapterId, out var chapter))
        {
            var step = chapter.Steps.FirstOrDefault();
            if (step != null)
            {
                TextAsset lyricsAsset = StepResourceResolver.LoadLyricsAsset(song.chapterId, step);
                if (lyricsAsset != null)
                {
                    var rawItems = ParseLyricsJson(lyricsAsset.text);
                    if (rawItems != null && rawItems.Count > 0)
                    {
                        LyricsLine currentLine = new LyricsLine { startTime = rawItems[0].start };
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        float lastEndTime = 0;

                        for (int i = 0; i < rawItems.Count; i++)
                        {
                            var item = rawItems[i];
                            bool isLongGap = i > 0 && (item.start - lastEndTime > 1.2f);

                            if (isLongGap && sb.Length > 0)
                            {
                                currentLine.text = sb.ToString().Trim();
                                currentLine.endTime = lastEndTime;
                                AddLyricLineToUI(currentLine);
                                currentLine = new LyricsLine { startTime = item.start };
                                sb.Clear();
                            }

                            sb.Append(item.word).Append(" ");
                            lastEndTime = item.end;
                        }

                        if (sb.Length > 0)
                        {
                            currentLine.text = sb.ToString().Trim();
                            currentLine.endTime = lastEndTime;
                            AddLyricLineToUI(currentLine);
                        }
                    }
                }
            }
        }
    }

    private void AddLyricLineToUI(LyricsLine line)
    {
        var label = new Label(line.text);
        label.AddToClassList("lyrics-text-line");
        lyricsScroll.Add(label);
        line.label = label;
        currentLyricsLines.Add(line);
    }

    private void UpdateLyricsHighlight(float currentTime)
    {
        if (currentLyricsLines == null || currentLyricsLines.Count == 0) return;

        int foundIndex = -1;
        for (int i = 0; i < currentLyricsLines.Count; i++)
        {
            if (currentTime >= currentLyricsLines[i].startTime)
                foundIndex = i;
            if (currentTime < currentLyricsLines[i].startTime)
                break;
        }

        if (foundIndex != -1 && foundIndex != currentLyricIndex)
        {
            if (currentLyricIndex != -1)
                currentLyricsLines[currentLyricIndex].label.RemoveFromClassList("active");

            currentLyricIndex = foundIndex;
            var activeLabel = currentLyricsLines[currentLyricIndex].label;
            activeLabel.AddToClassList("active");
            lyricsScroll.ScrollTo(activeLabel);
        }
    }

    // 데이터 파싱 내부 클래스 (레거시 유지하되 내부 호출용)
    private class LyricItem { public string word; public float start; public float end; }
    private List<LyricItem> ParseLyricsJson(string json)
    {
        var list = new List<LyricItem>();
        try {
            var regex = new System.Text.RegularExpressions.Regex("\"word\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"start\"\\s*:\\s*([0-9.]+)\\s*,\\s*\"end\"\\s*:\\s*([0-9.]+)");
            var matches = regex.Matches(json);
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                list.Add(new LyricItem {
                    word = m.Groups[1].Value,
                    start = float.Parse(m.Groups[2].Value),
                    end = float.Parse(m.Groups[3].Value)
                });
            }
        } catch { }
        return list;
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
            progressSlider.SetValueWithoutNotify(position);
        }

        float currTime = MusicPlayerManager.Instance.CurrentTime;
        
        // 가사 하이라이트 업데이트 호출
        UpdateLyricsHighlight(currTime);

        if (currentTimeLabel != null)
        {
            int minutes = Mathf.FloorToInt(currTime / 60);
            int seconds = Mathf.FloorToInt(currTime % 60);
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
            var stepData = chapter.Steps.FirstOrDefault(s => s.id == songInfo.StepId);
            
            // SongItem 생성하여 MusicPlayerManager에 전달
            var songItem = new SongItem
            {
                chapterId = songInfo.ChapterId,
                title = songInfo.Title,
                artist = songInfo.Artist,
                thumbnailPath = songInfo.ThumbnailPath,
                fullAudioPath = $"mp3/{songInfo.ChapterId}/full", // 레거시 혹은 기본
                vocalAudioPath = stepData != null && !string.IsNullOrEmpty(stepData.vocalFile) 
                    ? $"mp3/{songInfo.ChapterId}/{Path.GetFileNameWithoutExtension(stepData.vocalFile)}" 
                    : null,
                instrumentalAudioPath = stepData != null && !string.IsNullOrEmpty(stepData.instrumentalFile) 
                    ? $"mp3/{songInfo.ChapterId}/{Path.GetFileNameWithoutExtension(stepData.instrumentalFile)}" 
                    : null,
                isFree = true,
                isPurchased = true
            };

            // 완료된 곡들 중 선택된 곡들로만 플레이리스트 구성
            var completedSongs = GetCompletedSongs();
            var playlist = new List<SongItem>();
            int startIndex = -1;

            foreach (var info in completedSongs)
            {
                // 선택된 곡이거나 현재 클릭한 곡인 경우에만 플레이리스트에 포함
                bool isSelected = playlistSelection.ContainsKey(info.ChapterId) && playlistSelection[info.ChapterId];
                bool isCurrentRequested = (info.ChapterId == songInfo.ChapterId);

                if (isSelected || isCurrentRequested)
                {
                    if (CurriculumRepository.TryGetChapter(info.ChapterId, out var ch))
                    {
                        var sData = ch.Steps.FirstOrDefault(s => s.id == info.StepId);
                        var item = new SongItem
                        {
                            chapterId = info.ChapterId,
                            title = info.Title,
                            artist = info.Artist,
                            thumbnailPath = info.ThumbnailPath,
                            fullAudioPath = $"mp3/{info.ChapterId}/full",
                            vocalAudioPath = sData != null && !string.IsNullOrEmpty(sData.vocalFile) 
                                ? $"mp3/{info.ChapterId}/{Path.GetFileNameWithoutExtension(sData.vocalFile)}" 
                                : null,
                            instrumentalAudioPath = sData != null && !string.IsNullOrEmpty(sData.instrumentalFile) 
                                ? $"mp3/{info.ChapterId}/{Path.GetFileNameWithoutExtension(sData.instrumentalFile)}" 
                                : null,
                            isFree = true,
                            isPurchased = true
                        };
                        playlist.Add(item);

                        // 방금 클릭한 노래의 인덱스 기억
                        if (isCurrentRequested)
                        {
                            startIndex = playlist.Count - 1;
                        }
                    }
                }
            }

            if (playlist.Count > 0)
            {
                // startIndex를 찾지 못했다면(현재 곡이 선택 안 된 상태에서 강제 재생 시 등) 0번부터
                if (startIndex == -1) startIndex = 0;
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
            if (isPlaying)
            {
                playPauseButton.RemoveFromClassList("icon-play");
                playPauseButton.AddToClassList("icon-pause");
            }
            else
            {
                playPauseButton.RemoveFromClassList("icon-pause");
                playPauseButton.AddToClassList("icon-play");
            }
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
            // repeatButton.text = mode == RepeatMode.One ? "🔂" : "🔁"; // 아이콘으로 대체됨

            if (mode != RepeatMode.Off)
                repeatButton.AddToClassList("active");
            else
                repeatButton.RemoveFromClassList("active");
        }
    }

    private void UpdateFavoriteButton(bool isFavorite)
    {
        /*
        if (favoriteButton != null)
        {
            favoriteButton.text = isFavorite ? "♥" : "♡";
            if (isFavorite)
                favoriteButton.AddToClassList("active");
            else
                favoriteButton.RemoveFromClassList("active");
        }
        */
    }
}
