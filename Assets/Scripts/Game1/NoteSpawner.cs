using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class NoteSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject notePrefab;
    public Transform noteParent;
    public AudioSource musicSource;
    public NoteLoader noteLoader;
    public RectTransform targetPointRect;
    public RectTransform noteStartPointRect;

    [Header("Timing")]
    public float noteTravelTime = 3f;   // 노트가 이동하는 데 걸리는 시간(초)
    public float syncOffset = 1.5f;     // 악보 기준 오프셋(곡마다 조정)
    [Tooltip("DSP 예약 리드타임(버퍼 채우기 여유)")]
    public double scheduleLeadSec = 0.2;

    [Header("Spawn Layout")]
    public float noteHorizontalPadding = 10f; // 버튼 간 최소 거리

    [Header("Freeze / Auto Pause")]
    public static bool Freeze = false;
    public static float FreezeTime = 0f;      // 동결 시점(곡 진행 시간)
    public bool freezeAtFirstPerfect = false; // 첫 퍼펙트 시각에서 자동 정지
    private bool _frozenOnce = false;
    private float _firstTargetTime = -1f;

    [Header("A/V Sync Calibration")]
    [Tooltip("사용자 보정값(옵션 메뉴로 ± 조정 권장)")]
    public float userCalibSec = 0f;

    // 내부 상태
    public static float CurrentSongTime { get; private set; } = 0f;  // 화면/판정용 곡 진행 시간(보정 반영)
    public static double OutputLatencySec;                           // 출력 버퍼 지연 추정값

    private List<NoteData> notes;
    private int nextNoteIndex = 0;

    private float noteStartY;
    private float noteTargetY;

    private double dspSongStart = double.NaN; // 곡 시작 DSP 절대시간(PlayScheduled로 예약)
    private bool dspInit = false;

    // 바로 직전 노트의 x좌표를 저장(겹침 방지용)
    private float prevNoteX = float.NaN;

    [Header("UI / Managers")]
    [SerializeField] private TopBarController topBarController;
    [SerializeField] private CountdownManager countdownManager;
    [SerializeField] private AudioManager audioManager;

    public UnityEngine.UI.Slider progressSlider;

    private bool resultShown = false;

    [SerializeField] private UIDocument resultPopupDocument;
    [SerializeField] private JudgementManager judgementManager;


    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }



    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        Debug.Log($"[NoteSpawner] timeScale reset → 1 on load: {scene.name}");
    }


    void Awake()
    {
        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0; 
        Time.timeScale = 1f;
    }


    void Start()
    {
        StartPlayback();
    }
    
    public void StartPlayback()
    {
        userCalibSec = GameSettings.GetDSPUserCalib();

        if (musicSource == null)
        {
            Debug.LogError("[NoteSpawner] musicSource is null");
            return;
        }

        // 1️⃣ 노트 목록, 위치 초기화
        notes = (noteLoader != null) ? noteLoader.notes : new List<NoteData>();
        noteStartY = noteStartPointRect.anchoredPosition.y;
        noteTargetY = targetPointRect.anchoredPosition.y;

        // 2️⃣ 첫 퍼펙트 시각 (freezeAtFirstPerfect 옵션용)
        if (freezeAtFirstPerfect && notes != null && notes.Count > 0)
            _firstTargetTime = notes[0].startTime + syncOffset;

        // 3️⃣ 출력 버퍼 지연 계산
        int buffer, num;
        AudioSettings.GetDSPBufferSize(out buffer, out num);
        OutputLatencySec = (double)(buffer * num) / AudioSettings.outputSampleRate;
        Debug.Log($"[Audio] OutputLatency ~ {OutputLatencySec * 1000f:F1} ms");

        // 4️⃣ DSP 예약 재생
        double scheduled = AudioSettings.dspTime + scheduleLeadSec;
        dspSongStart = scheduled;
        dspInit = true;

        if (musicSource.isPlaying) musicSource.Stop();
        musicSource.PlayScheduled(scheduled);

        // 5️⃣ 상태 리셋
        nextNoteIndex = 0;
        Freeze = false;
        _frozenOnce = false;

        Debug.Log($"[NoteSpawner] Playback scheduled at dsp={scheduled:F3}");
    }   

    void Update()
    {
        // 3) 현재 곡 진행 시간 갱신 (Freeze면 고정)
        if (Freeze)
        {
            CurrentSongTime = FreezeTime;
            return;
        }

        if (dspInit)
        {
            // 화면/판정 기준 시간 = (지금 DSP) - (시작 DSP) - (출력지연) - (유저보정)
            double now = AudioSettings.dspTime;
            double t = now - dspSongStart - OutputLatencySec - (double)userCalibSec;
            if (t < 0) t = 0; // 예약 리드 시간동안 0 유지
            CurrentSongTime = (float)t;


            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[Spawner-Time] nowDSP={now:F3}, dspSongStart={dspSongStart:F3}, " +
                        $"userCalibSec={userCalibSec:F3}, CurrentSongTime={CurrentSongTime:F3}");
            }


        }

        // 4) 첫 퍼펙트 시각에서 자동 정지(옵션)
        if (!_frozenOnce && freezeAtFirstPerfect && musicSource != null && musicSource.isPlaying && _firstTargetTime >= 0f)
        {
            if (CurrentSongTime >= _firstTargetTime)
            {
                FreezeTime = CurrentSongTime;
                Freeze = true;
                _frozenOnce = true;

                musicSource.Pause();
                Time.timeScale = 0f;

                Debug.Log($"[DEBUG] Freeze at FIRST PERFECT (t={FreezeTime:F3}s). Call Unfreeze() to resume.");
                return;
            }
        }

        // 5) 스폰
        if (!dspInit || musicSource == null) return;

        // isPlaying은 PlayScheduled 후 실제 시작 시점까지 false일 수 있으므로 CurrentSongTime으로도 판별 가능
        if (nextNoteIndex < notes.Count)
        {
            float songTime = CurrentSongTime;
            float spawnTime = notes[nextNoteIndex].startTime + syncOffset + userCalibSec - noteTravelTime;

            // 앞쪽 몇 개 노트만 자세히 로그 찍기
            if (nextNoteIndex < 5)
            {
                Debug.Log($"[Spawner-SpawnCheck] idx={nextNoteIndex}, songTime={songTime:F3}, " +
                        $"spawnTime={spawnTime:F3}, noteStart={notes[nextNoteIndex].startTime:F3}, " +
                        $"syncOffset={syncOffset:F3}, userCalibSec={userCalibSec:F3}");
            }


            if (songTime >= spawnTime)
            {
                SpawnNote(notes[nextNoteIndex]);
                nextNoteIndex++;
            }
        }

        // 🔥 진행도 업데이트
        UpdateProgressUI();

        // 🔥 게임 종료 조건: 모든 노트 처리 + 음악 종료
        if (!resultShown && nextNoteIndex >= notes.Count && !musicSource.isPlaying)
        {
            resultShown = true;
            ShowResultPopup();
        }
    }

    public void Unfreeze()
    {
        if (!Freeze) return;

        Freeze = false;
        Time.timeScale = 1f;

        // FreezeTime 시각에서 정확히 이어가도록 시작 DSP 정렬
        dspSongStart = AudioSettings.dspTime - FreezeTime - OutputLatencySec - (double)userCalibSec;
        dspInit = true;

        if (musicSource != null)
            musicSource.UnPause();

        Debug.Log($"[DEBUG] Unfreeze -> resume at t={FreezeTime:F3}s, new dspStart={dspSongStart:F3}");
    }

    public void SpawnNote(NoteData noteData)
    {
        if (notePrefab == null || noteParent == null)
        {
            Debug.LogError("[NoteSpawner] Missing notePrefab or noteParent");
            return;
        }

        RectTransform panelRect = noteParent.GetComponent<RectTransform>();
        float panelWidth = panelRect.rect.width;

        // 1) 생성
        GameObject note = Instantiate(notePrefab, noteParent);
        note.SetActive(true);
        RectTransform noteRect = note.GetComponent<RectTransform>();
        noteRect.anchoredPosition = Vector2.zero;

        // 2) UI 세팅
        var noteUI = note.GetComponent<NoteUI>();
        if (noteUI != null)
        {
            noteUI.active = true;
            noteUI.SetWord(
                noteData.word,
                noteTargetY,
                noteData.startTime + syncOffset + userCalibSec,
                noteTravelTime,
                noteStartY,
                musicSource // 전달은 유지(내부는 DSP 기반 CurrentSongTime 사용)
            );

            // ✅ TopBarController 주입 추가
            if (topBarController != null)
            {
                noteUI.SetTopBar(topBarController);
            }
            else
            {
                Debug.LogWarning("[NoteSpawner] TopBarController not assigned in Inspector!");
            }



            // 배경 스킨(선택)
            var bgSprite = Resources.Load<Sprite>($"buttonbg/{noteData.buttonBg}");
            var img = note.GetComponent<UnityEngine.UI.Image>();
            if (bgSprite != null && img != null)
            {
                img.sprite = bgSprite;
            }

            noteUI.shardColorKey = InferColorKeyFromBg(noteData.buttonBg);

        }

        Canvas.ForceUpdateCanvases();

        // 3) 글자폭 반영 후 가로 위치 결정(겹침 최소화)
        float noteWidth = noteRect.rect.width;
        float minX = -panelWidth / 2f + noteWidth / 2f;
        float maxX = panelWidth / 2f - noteWidth / 2f;

        float randomX = 0f;
        int maxAttempts = 30;
        bool found = false;
        float bestX = minX;
        float maxDist = float.MinValue;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float candidateX = Random.Range(minX, maxX);
            if (float.IsNaN(prevNoteX) || Mathf.Abs(candidateX - prevNoteX) >= noteWidth + noteHorizontalPadding)
            {
                randomX = candidateX;
                found = true;
                break;
            }
            float dist = Mathf.Abs(candidateX - prevNoteX);
            if (dist > maxDist)
            {
                maxDist = dist;
                bestX = candidateX;
            }
        }
        if (!found) randomX = bestX;
        prevNoteX = randomX;

        // 4) 위치 적용
        noteRect.anchoredPosition = new Vector2(randomX, noteStartY);

        Debug.Log($"[Spawner-SpawnNote] word={noteData.word}, start={noteData.startTime:F3}, " +
            $"syncOffset={syncOffset:F3}, userCalibSec={userCalibSec:F3}, " +
            $"CurrentSongTime={CurrentSongTime:F3}");
    }

    public void RetryGame()
    {
        Debug.Log("[NoteSpawner] RetryGame called.");

        NoteSpawner.Freeze = false;
        NoteSpawner.FreezeTime = 0f;
        // ✅ 게임 재개
        Time.timeScale = 1f;
        musicSource.UnPause();

        // ✅ 라이프 복구
        topBarController.AddLife(3);

        // ✅ 노트 리셋
        foreach (Transform child in noteParent)
            Destroy(child.gameObject);

        StartPlayback();
    }


    public void PauseGame()
    {
        // 현재 곡 진행 시간 저장
        Freeze = true;
        FreezeTime = CurrentSongTime;

        if (musicSource != null)
            musicSource.Pause();

        Time.timeScale = 0f;

        Debug.Log($"[NoteSpawner] PauseGame() FreezeTime={FreezeTime:F3}");
    }

    public void ResumeGame()
    {
        // 혹시라도 타임스케일이 0인 상태 방지
        Time.timeScale = 1f;

        // Freeze 상태가 아니면 그냥 오디오만 재개 시도
        if (!Freeze)
        {
            if (musicSource != null && !musicSource.isPlaying)
                musicSource.UnPause();

            Debug.Log("[NoteSpawner] ResumeGame() called but not frozen. Just UnPause.");
            return;
        }

        // DSP 기준으로 재정렬
        double nowDsp = AudioSettings.dspTime;

        dspSongStart = nowDsp - FreezeTime - OutputLatencySec - (double)userCalibSec;
        dspInit = true;

        Freeze = false;

        if (musicSource != null)
            musicSource.UnPause();

        Debug.Log($"[NoteSpawner] ResumeGame() -> resume at t={FreezeTime:F3}, dspStart={dspSongStart:F3}");
    }   

    private string InferColorKeyFromBg(string bg)
    {
        if (string.IsNullOrEmpty(bg)) return "white";
        string k = bg.ToLower();
        if (k.Contains("blue")) return "blue";
        if (k.Contains("red")) return "red";
        if (k.Contains("orange")) return "orange";
        if (k.Contains("white")) return "white";
        if (k.Contains("green")) return "green";
        if (k.Contains("violet")) return "purple"; // NoteUI에서 puple로 매핑됨
        return "white";
    }   

    public void RealignDSPTimeAfterOffsetChanged()
    {
        if (!dspInit) return;

        double now = AudioSettings.dspTime;
        dspSongStart = now - CurrentSongTime - OutputLatencySec - (double)userCalibSec;

        Debug.Log($"[NoteSpawner] RealignDSPTimeAfterOffsetChanged -> dspSongStart={dspSongStart:F3}");
    }    

    private void UpdateProgressUI()
    {
        if (progressSlider == null || musicSource == null || musicSource.clip == null)
            return;

        float length = musicSource.clip.length;

        if (length > 0f)
            progressSlider.value = CurrentSongTime / length;
    }
     

    private void ShowResultPopup()
    {
        if (resultPopupDocument == null)
        {
            Debug.LogError("[NoteSpawner] ResultPopup UIDocument is NULL!");
            return;
        }

        resultPopupDocument.gameObject.SetActive(true);

        // 🎯 ResultPopupController 가져오기
        var popup = resultPopupDocument.gameObject.GetComponent<ResultPopupController>();
        if (popup == null)
        {
            Debug.LogError("[NoteSpawner] ResultPopupController not found!");
            return;
        }

        // 🔥 JudgementManager에서 결과 받아서 전달
        var data = new ResultData()
        {
            Score = judgementManager.score,
            Accuracy = judgementManager.Accuracy,
            MaxCombo = judgementManager.MaxCombo,
            Perfect = judgementManager.Perfect,
            Great = judgementManager.Great,
            Good = judgementManager.Good,
            Miss = judgementManager.Miss,
            //RewardCoin = judgementManager.CalcRewardCoin()
        };

        popup.SetResult(data);

        Time.timeScale = 0f;  // 게임 멈춤
        Debug.Log("[Result] Result popup opened.");
    }

}