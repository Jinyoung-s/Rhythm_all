using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;
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
    [SerializeField] private VisualTreeAsset queueViewUxml;

    private UIDocument uiDocument;
    private VisualElement root;
    private HeaderUI headerUI; // 헤더 참조

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
    private VisualElement nowPlayingContainer; // 템플릿 컨테이너 참조 추가
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
    private Button queueButton;

    // 신규 추가 요소
    private Slider vocalVolumeSlider;
    private VisualElement lyricsTextLine; // 레거시 참조 및 레이아웃 용
    private ScrollView lyricsScroll;

    // === 큐 뷰 UI ===
    private VisualElement queueContainer; // 템플릿 컨테이너 참조 추가
    private VisualElement queueOverlay;
    private Button closeQueueButton;
    private VisualElement currentSongCard;
    private ScrollView queueList;
    private Label queueCount;
    private VisualElement emptyQueueState;

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
    private bool isQueueVisible = false;
    private Dictionary<string, bool> playlistSelection = new Dictionary<string, bool>(); // ChapterId -> IsSelected
    private Coroutine showHeaderCoroutine; // 헤더 표시 코루틴 참조
    private string playlistSelectionPath; // 플레이리스트 선택 저장 경로

    // 플레이리스트 선택 저장용 클래스
    [System.Serializable]
    private class PlaylistSelectionData
    {
        public List<string> selectedSongs = new List<string>();
        public List<string> unselectedSongs = new List<string>();
    }

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        
        // BottomMenu(sortingOrder=1)보다 아래에 렌더링되도록 설정
        if (uiDocument != null)
        {
            uiDocument.sortingOrder = 0;
        }

        // HeaderUI 찾기
        headerUI = FindObjectOfType<HeaderUI>();
        
        // 플레이리스트 선택 저장 경로 설정
        playlistSelectionPath = Path.Combine(Application.persistentDataPath, "playlist_selection.json");
        LoadPlaylistSelection();
    }

    private void OnEnable()
    {
        InitializeUI();
        SubscribeToEvents();
        RefreshSongList();
        UpdateMiniPlayer();
        
        // Play 탭 활성화 시 헤더 표시 (Now Playing이 열려있지 않다면)
        // 약간 딜레이를 주어 ShowNowPlaying()과의 경합 방지
        if (!isNowPlayingVisible)
        {
            // 이전 코루틴이 실행 중이면 중단
            if (showHeaderCoroutine != null)
            {
                StopCoroutine(showHeaderCoroutine);
            }
            showHeaderCoroutine = StartCoroutine(ShowHeaderAfterFrame());
        }
    }

    private System.Collections.IEnumerator ShowHeaderAfterFrame()
    {
        yield return null; // 1프레임 대기
        if (!isNowPlayingVisible)
        {
            headerUI?.Show();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        
        // 탭 전환 시 UIDocument가 비활성화되면서 rootVisualElement가 비워집니다.
        // 다시 활성화될 때 root가 새로 생성(rebuild)되므로, 
        // 기존에 CloneTree로 생성해서 Add했던 오버레이 참조들을 null로 초기화해야 합니다.
        if (nowPlayingOverlay != null)
        {
            if (uiDocument != null) uiDocument.sortingOrder = 0;
        }

        nowPlayingContainer = null;
        nowPlayingOverlay = null;
        queueContainer = null;
        queueOverlay = null;
        
        // 상태 초기화
        if (isNowPlayingVisible)
        {
            headerUI?.Show();
            isNowPlayingVisible = false;
        }
        isQueueVisible = false;
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
        // 클로저 캡처 문제 방지를 위해 로컬 복사본 생성
        var localSongInfo = new CompletedSongInfo
        {
            ChapterId = songInfo.ChapterId,
            StepId = songInfo.StepId,
            Title = songInfo.Title,
            Artist = songInfo.Artist,
            ThumbnailPath = songInfo.ThumbnailPath
        };

        var card = new VisualElement();
        card.AddToClassList("song-card");

        // SongItem 정보 가져오기 (가격 및 구매 상태 확인용)
        var songData = SongShopManager.Instance.GetSongInfo(localSongInfo.ChapterId);
        bool isPurchased = SongShopManager.Instance.IsPurchased(localSongInfo.ChapterId);

        // 썸네일
        var thumbnail = new VisualElement();
        thumbnail.AddToClassList("song-thumbnail");
        if (!string.IsNullOrEmpty(localSongInfo.ThumbnailPath))
        {
            var sprite = Resources.Load<Sprite>(localSongInfo.ThumbnailPath);
            if (sprite != null)
            {
                thumbnail.style.backgroundImage = new StyleBackground(sprite);
            }
        }
        card.Add(thumbnail);

        // 곡 정보
        var info = new VisualElement();
        info.AddToClassList("song-info");

        var title = new Label(localSongInfo.Title);
        title.AddToClassList("song-title");
        info.Add(title);

        var artistNameStr = songData != null ? songData.artist : localSongInfo.Artist;
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
            // ChapterId + StepId 조합으로 고유 키 생성
            string songKey = $"{localSongInfo.ChapterId}_{localSongInfo.StepId}";
            
            if (!playlistSelection.ContainsKey(songKey))
            {
                playlistSelection[songKey] = true;
                SavePlaylistSelection(); // 초기 설정도 저장
                Debug.Log($"[MusicPlayerController] New song added to playlist: {localSongInfo.Title} (Key: {songKey})");
            }
            
            bool isSelected = playlistSelection[songKey];
            if (isSelected) check.AddToClassList("active");

            check.RegisterCallback<ClickEvent>(evt =>
            {
                bool newState = !playlistSelection[songKey];
                playlistSelection[songKey] = newState;
                
                if (newState) check.AddToClassList("active");
                else check.RemoveFromClassList("active");
                
                Debug.Log($"[MusicPlayerController] 🔄 Song {localSongInfo.Title} (Key: {songKey}) selection changed to: {newState}");
                SavePlaylistSelection(); // 변경 사항 저장
                
                // 카드 클릭 방지: 즉시 전파 중단 + 기본 동작 방지
                evt.StopImmediatePropagation();
                evt.StopPropagation();
                evt.PreventDefault();
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
                     Debug.LogWarning($"[MusicPlayerController] Song not in catalog: {localSongInfo.ChapterId}");
                }
                evt.StopPropagation();
            });
        }

        card.Add(actionArea);

        // 카드 전체 클릭 처리 (localSongInfo 사용)
        card.RegisterCallback<ClickEvent>(evt =>
        {
            // 체크 버튼이나 그 자식 요소 클릭은 무시
            var clickedElement = evt.target as VisualElement;
            
            // 클릭된 요소 또는 그 부모 중 하나라도 owned-check 클래스를 가지고 있으면 무시
            var current = clickedElement;
            while (current != null)
            {
                if (current.ClassListContains("owned-check"))
                {
                    Debug.Log($"[MusicPlayerController] Check button (or its child) clicked, ignoring card click");
                    return;
                }
                current = current.parent;
            }
            
            Debug.Log($"[MusicPlayerController] Card clicked: {localSongInfo.Title}");
            if (isPurchased)
            {
                PlaySong(localSongInfo);
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
        Debug.Log("[MusicPlayerController] ShowNowPlaying called");
        
        // 헤더 표시 코루틴 중단 (실행 중이라면)
        if (showHeaderCoroutine != null)
        {
            StopCoroutine(showHeaderCoroutine);
            showHeaderCoroutine = null;
            Debug.Log("[MusicPlayerController] Stopped header coroutine");
        }
        
        if (nowPlayingViewUxml == null)
        {
            Debug.LogWarning("[MusicPlayerController] NowPlayingView UXML not assigned!");
            return;
        }

        if (nowPlayingOverlay != null)
        {
            Debug.Log($"[MusicPlayerController] Showing existing Now Playing overlay - hasHiddenClass: {nowPlayingOverlay.ClassListContains("hidden")}");
            
            // 중요: 컨테이너 자체를 보여주고 pickingMode를 활성화
            nowPlayingContainer.style.display = DisplayStyle.Flex;
            nowPlayingContainer.pickingMode = PickingMode.Position;
            
            nowPlayingOverlay.RemoveFromClassList("hidden");
            Debug.Log("[MusicPlayerController] Overlay display set to Flex");
            
            // sortingOrder 명시적 설정
            if (uiDocument != null) 
            {
                uiDocument.sortingOrder = 200;
                Debug.Log("[MusicPlayerController] Set sortingOrder to 200");
            }
            
            headerUI?.Hide(); // 헤더 숨기기
            Debug.Log("[MusicPlayerController] Header hidden");
            
            UpdateNowPlayingUI(); // 매번 최신 상태로 갱신
            isNowPlayingVisible = true;
            return;
        }

        Debug.Log("[MusicPlayerController] Creating new Now Playing overlay");

        // 생성
        nowPlayingContainer = nowPlayingViewUxml.CloneTree();
        // 템플릿 컨테이너 스타일 설정
        nowPlayingContainer.style.position = Position.Absolute;
        nowPlayingContainer.style.width = new Length(100, LengthUnit.Percent);
        nowPlayingContainer.style.height = new Length(100, LengthUnit.Percent);
        nowPlayingContainer.pickingMode = PickingMode.Position; 

        nowPlayingOverlay = nowPlayingContainer.Q<VisualElement>("NowPlayingOverlay");
        root.Add(nowPlayingContainer);
        if (uiDocument != null) uiDocument.sortingOrder = 200; // 생성 시에도 맨 앞으로
        headerUI?.Hide(); // 헤더 숨기기

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
        queueButton = nowPlayingOverlay.Q<Button>("QueueButton");

        // 신규 요소 바인딩
        vocalVolumeSlider = nowPlayingOverlay.Q<Slider>("VocalVolumeSlider");
        lyricsTextLine = nowPlayingOverlay.Q<Label>("LyricsText"); // UI에서 여전히 이 이름일 것임
        lyricsScroll = nowPlayingOverlay.Q<ScrollView>("LyricsScroll");
        
        Debug.Log($"[MusicPlayerController] lyricsScroll binding result: {(lyricsScroll != null ? "SUCCESS" : "FAILED (NULL)")}");

        // 이벤트 바인딩
        backButton?.RegisterCallback<ClickEvent>(evt => HideNowPlaying());
        shuffleButton?.RegisterCallback<ClickEvent>(evt => OnShuffleClicked());
        prevButton?.RegisterCallback<ClickEvent>(evt => OnPrevClicked());
        playPauseButton?.RegisterCallback<ClickEvent>(evt => OnPlayPauseClicked());
        nextButton?.RegisterCallback<ClickEvent>(evt => OnNextClicked());
        repeatButton?.RegisterCallback<ClickEvent>(evt => OnRepeatClicked());
        queueButton?.RegisterCallback<ClickEvent>(evt => ShowQueue());

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
        if (nowPlayingContainer != null)
        {
            nowPlayingContainer.style.display = DisplayStyle.None;
            nowPlayingContainer.pickingMode = PickingMode.Ignore; // 터치 방지
        }
        
        nowPlayingOverlay?.AddToClassList("hidden");
        if (uiDocument != null) uiDocument.sortingOrder = 0; 
        headerUI?.Show(); 
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
            float duration = (currentSong.duration > 0) ? currentSong.duration : player.Duration;
            int mins = Mathf.FloorToInt(duration / 60);
            int secs = Mathf.FloorToInt(duration % 60);
            totalTimeLabel.text = $"{mins}:{secs:D2}";
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
        if (lyricsScroll == null) 
        {
            Debug.LogError("[MusicPlayerController] ❌ lyricsScroll is NULL! Cannot display lyrics.");
            return;
        }
        Debug.Log($"[MusicPlayerController] UpdateLyrics called for song: {song.title}");

        lyricsScroll.Clear();
        currentLyricsLines.Clear();
        currentLyricIndex = -1;

        if (CurriculumRepository.TryGetChapter(song.chapterId, out var chapter))
        {
            // stepId가 있으면 해당 스텝을 사용, 없으면 첫 번째
            var step = !string.IsNullOrEmpty(song.stepId) 
                ? chapter.Steps.FirstOrDefault(s => s.id == song.stepId)
                : chapter.Steps.FirstOrDefault();
            
            if (step != null)
            {
                // MusicPlayer는 문장 단위 가사가 필요하므로 LoadSingAlongAsset 사용
                TextAsset lyricsAsset = StepResourceResolver.LoadSingAlongAsset(song.chapterId, step);
                    if (lyricsAsset != null)
                    {
                        Debug.Log($"[MusicPlayerController] ✅ Successfully loaded lyrics asset: {lyricsAsset.name}");
                        var rawItems = ParseLyricsJson(lyricsAsset.text);
                        if (rawItems != null && rawItems.Count > 0)
                        {
                            foreach (var item in rawItems)
                            {
                                var line = new LyricsLine 
                                { 
                                    text = item.sentence, 
                                    startTime = item.start, 
                                    endTime = item.end 
                                };
                                AddLyricLineToUI(line);
                            }
                            Debug.Log($"[MusicPlayerController] ✅ Added {rawItems.Count} lines to UI.");
                            
                            // 초기 하이라이트 적용
                            UpdateLyricsHighlight(MusicPlayerManager.Instance.CurrentTime);
                        }
                        else
                        {
                            Debug.LogError("[MusicPlayerController] ❌ Parsed items are null or empty.");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[MusicPlayerController] ❌ Lyrics asset NOT found. Chapter: {song.chapterId}, Step: {step.id}");
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

    // 데이터 파싱: 기존 SingAlongLine 클래스 활용 (정합성 유지)
    private List<SingAlongLine> ParseLyricsJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return new List<SingAlongLine>();
        
        try 
        {
            // Newtonsoft.Json 사용 시 리스트로 직접 파싱
            var list = JsonConvert.DeserializeObject<List<SingAlongLine>>(json);
            if (list == null) 
            {
                Debug.LogWarning("[MusicPlayerController] Parsed list is null");
                return new List<SingAlongLine>();
            }
            
            Debug.Log($"[MusicPlayerController] Parsed {list.Count} items from JSON");
            
            // 데이터 유효성 검사: 문장이 있고, start/end가 유효한 항목만
            var filtered = list.Where(l => 
                !string.IsNullOrEmpty(l.sentence) && 
                l.start >= 0 && 
                l.end >= 0
            ).ToList();
            
            int invalidCount = list.Count - filtered.Count;
            if (invalidCount > 0)
            {
                Debug.LogWarning($"[MusicPlayerController] Filtered out {invalidCount} invalid items (missing sentence or null timing)");
            }
            
            Debug.Log($"[MusicPlayerController] After filtering: {filtered.Count} valid items");
            
            return filtered;
        } 
        catch (System.Exception e) 
        {
            Debug.LogError($"[MusicPlayerController] Lyrics JSON Parse Error: {e.Message}\nJSON Content: {json.Substring(0, Mathf.Min(json.Length, 100))}...");
            return new List<SingAlongLine>();
        }
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
                stepId = songInfo.StepId,
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
                bool isCurrentRequested = (info.ChapterId == songInfo.ChapterId && info.StepId == songInfo.StepId);

                Debug.Log($"[MusicPlayerController] Checking song: {info.Title} (StepId: {info.StepId}) - isSelected: {isSelected}, isCurrentRequested: {isCurrentRequested}");

                if (isSelected || isCurrentRequested)
                {
                    if (CurriculumRepository.TryGetChapter(info.ChapterId, out var ch))
                    {
                        var sData = ch.Steps.FirstOrDefault(s => s.id == info.StepId);
                        var item = new SongItem
                        {
                            chapterId = info.ChapterId,
                            stepId = info.StepId,
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
                            Debug.Log($"[MusicPlayerController] Found requested song at index {startIndex}: {info.Title}");
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

    // ========== 큐 뷰 ==========

    private void ShowQueue()
    {
        if (queueViewUxml == null)
        {
            Debug.LogWarning("[MusicPlayerController] QueueView UXML not assigned!");
            return;
        }

        if (queueContainer != null)
        {
            queueContainer.style.display = DisplayStyle.Flex;
            queueContainer.pickingMode = PickingMode.Position;
            queueOverlay.RemoveFromClassList("hidden");
            UpdateQueueUI();
            isQueueVisible = true;
            return;
        }

        // 생성
        queueContainer = queueViewUxml.CloneTree();
        queueContainer.style.position = Position.Absolute;
        queueContainer.style.width = new Length(100, LengthUnit.Percent);
        queueContainer.style.height = new Length(100, LengthUnit.Percent);
        queueContainer.pickingMode = PickingMode.Position; 

        queueOverlay = queueContainer.Q<VisualElement>("QueueOverlay");
        root.Add(queueContainer);
        if (uiDocument != null) uiDocument.sortingOrder = 200; // 큐 표시 시에도 오더 유지 또는 설정

        // UI 요소 바인딩
        closeQueueButton = queueOverlay.Q<Button>("CloseQueueButton");
        currentSongCard = queueOverlay.Q<VisualElement>("CurrentSongCard");
        queueList = queueOverlay.Q<ScrollView>("QueueList");
        queueCount = queueOverlay.Q<Label>("QueueCount");
        emptyQueueState = queueOverlay.Q<VisualElement>("EmptyQueueState");

        // 이벤트 바인딩
        closeQueueButton?.RegisterCallback<ClickEvent>(evt => HideQueue());

        UpdateQueueUI();
        isQueueVisible = true;
    }

    private void HideQueue()
    {
        if (queueContainer != null)
        {
            queueContainer.style.display = DisplayStyle.None;
            queueContainer.pickingMode = PickingMode.Ignore;
        }
        
        queueOverlay?.AddToClassList("hidden");
        isQueueVisible = false;
    }

    private void UpdateQueueUI()
    {
        if (queueOverlay == null) return;

        var player = MusicPlayerManager.Instance;
        var playlist = player.CurrentPlaylist;
        var currentIndex = player.CurrentIndex;

        if (playlist == null || playlist.Count == 0)
        {
            ShowEmptyQueueState();
            return;
        }

        emptyQueueState?.AddToClassList("hidden");

        // 현재 재생 중인 곡
        if (currentSongCard != null)
        {
            currentSongCard.Clear();
            if (currentIndex >= 0 && currentIndex < playlist.Count)
            {
                var currentSong = playlist[currentIndex];
                var card = CreateQueueSongCard(currentSong, -1, true);
                currentSongCard.Add(card);
            }
        }

        // 다음 곡들
        if (queueList != null)
        {
            queueList.Clear();
            int nextCount = 0;

            for (int i = currentIndex + 1; i < playlist.Count; i++)
            {
                var song = playlist[i];
                var card = CreateQueueSongCard(song, nextCount + 1, false);
                queueList.Add(card);
                nextCount++;
            }

            // 큐 카운트 업데이트
            if (queueCount != null)
            {
                queueCount.text = nextCount == 1 ? "1 song" : $"{nextCount} songs";
            }
        }
    }

    private VisualElement CreateQueueSongCard(SongItem song, int position, bool isCurrent)
    {
        var card = new VisualElement();
        card.AddToClassList("song-card");
        if (isCurrent) card.AddToClassList("current-song");

        // 포지션 번호 (현재 곡은 재생 아이콘)
        if (!isCurrent && position > 0)
        {
            var number = new Label(position.ToString());
            number.AddToClassList("queue-number");
            card.Add(number);
        }

        // 썸네일
        var thumbnail = new VisualElement();
        thumbnail.AddToClassList("song-thumbnail");
        if (!string.IsNullOrEmpty(song.thumbnailPath))
        {
            var sprite = Resources.Load<Sprite>(song.thumbnailPath);
            if (sprite != null)
            {
                thumbnail.style.backgroundImage = new StyleBackground(sprite);
            }
        }
        card.Add(thumbnail);

        // 곡 정보
        var info = new VisualElement();
        info.AddToClassList("song-info");

        var title = new Label(song.title);
        title.AddToClassList("song-title");
        info.Add(title);

        var artist = new Label(song.artist);
        artist.AddToClassList("song-artist");
        info.Add(artist);

        card.Add(info);

        // 재생 시간
        var duration = new Label(song.GetFormattedDuration());
        duration.AddToClassList("song-duration");
        card.Add(duration);

        return card;
    }

    private void ShowEmptyQueueState()
    {
        emptyQueueState?.RemoveFromClassList("hidden");
        if (currentSongCard != null) currentSongCard.Clear();
        if (queueList != null) queueList.Clear();
        if (queueCount != null) queueCount.text = "0 songs";
    }

    // ========== 플레이리스트 선택 저장/로드 ==========
    
    private void SavePlaylistSelection()
    {
        try
        {
            Debug.Log($"[MusicPlayerController] 💾 Saving playlist selection... Total in dict: {playlistSelection.Count}");
            
            var data = new PlaylistSelectionData();
            foreach (var kvp in playlistSelection)
            {
                if (kvp.Value) 
                {
                    data.selectedSongs.Add(kvp.Key);
                    Debug.Log($"[MusicPlayerController]   - {kvp.Key}: selected");
                }
                else // ⭐ false인 것도 저장!
                {
                    data.unselectedSongs.Add(kvp.Key);
                    Debug.Log($"[MusicPlayerController]   - {kvp.Key}: NOT selected (saving as unselected)");
                }
            }
            
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(playlistSelectionPath, json);
            Debug.Log($"[MusicPlayerController] ✅ Playlist selection saved to: {playlistSelectionPath}");
            Debug.Log($"[MusicPlayerController] ✅ Selected: {data.selectedSongs.Count}, Unselected: {data.unselectedSongs.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MusicPlayerController] ❌ Failed to save playlist selection: {e.Message}");
        }
    }
    
    private void LoadPlaylistSelection()
    {
        try
        {
            if (File.Exists(playlistSelectionPath))
            {
                string json = File.ReadAllText(playlistSelectionPath);
                var data = JsonUtility.FromJson<PlaylistSelectionData>(json);
                
                playlistSelection.Clear();
                
                // 선택된 곡 로드 (true)
                int selectedCount = 0;
                int unselectedCount = 0;
                int skippedCount = 0;
                
                foreach (var songId in data.selectedSongs)
                {
                    if (songId.Contains("_step_"))
                    {
                        playlistSelection[songId] = true;
                        selectedCount++;
                    }
                    else
                    {
                        skippedCount++;
                        Debug.LogWarning($"[MusicPlayerController] Skipped old format key: {songId}");
                    }
                }
                
                // ⭐ 선택 해제된 곡 로드 (false)
                foreach (var songId in data.unselectedSongs)
                {
                    if (songId.Contains("_step_"))
                    {
                        playlistSelection[songId] = false;
                        unselectedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                
                Debug.Log($"[MusicPlayerController] Playlist selection loaded: {selectedCount} selected, {unselectedCount} unselected, {skippedCount} old format skipped");
            }
            else
            {
                Debug.Log("[MusicPlayerController] No saved playlist selection found");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MusicPlayerController] Failed to load playlist selection: {e.Message}");
        }
    }
}
