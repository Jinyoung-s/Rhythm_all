using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Text;
using TMPro;

public class SingAlongManager : MonoBehaviour
{
    [Header("References")]
    public AudioSource songSource;   // 반주 재생용
    public AudioSource cueSource;    // STT cue 효과음
    public SingAlongUI ui;           // UI 업데이트용

    private AndroidSpeechBridge speechBridge;

    [Header("Playback Scheduling")]
    public double scheduleLeadSec = 0.20;
    public float preRollSec = 0.5f;

    [Header("Flow / STT")]
    public bool autoMode = true;
    public float sttTimeoutSec = 10f;
    public float afterAnalyzeDelaySec = 0.8f;

    // 내부 상태
    private List<SingAlongLine> lines;
    private int currentIndex = -1;

    // 오디오 상태
    private int clipFrequency = 44100;
    private float originalVolume = 1f;
    private Coroutine playingCo;
    private Coroutine sttTimeoutCo;

    // 플래그
    private bool isPlayingSegment = false;
    private bool isWaitingSTT = false;
    private double tailBuffer = 0.15;

    // UI/진행 제어
    private int retryCount = 0;
    private HashSet<string> matchedWords = new HashSet<string>(); // 누적 하이라이트용
    private bool canGoNext = false;       // ⏭️ 오토 OFF일 때 STT 한 번 끝나야만 다음 이동 허용
    private bool isPaused = false;        // ▶/⏸ 상태

    // (선택) 인스펙터에서 연결 가능
    public TMP_Text autoModeLabel;

    public UnityEngine.UI.Image playPauseButtonImage;
    public Sprite playIcon;   // btn_play
    public Sprite pauseIcon;  // btn_pause

    private bool[] linePassed;    // 각 라인별 통과 여부
    private int passedCount = 0;

    void Awake()
    {
        if (ui == null) ui = FindFirstObjectByType<SingAlongUI>();
        speechBridge = gameObject.AddComponent<AndroidSpeechBridge>();
    }

    /*
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ESC 또는 Android Back key
            goEscape();
        }
    }    
    */

    public void goEscape()
    {
        SceneNavigator.Load("StepScene");      
    }

    IEnumerator Start()
    {
        yield return LoadJsonData();
        if (lines == null || lines.Count == 0)
        {
            Debug.LogError("[SingAlongManager] ❌ No valid lines in JSON.");
            yield break;
        }

        linePassed = new bool[lines.Count];
    
        var clip = LoadSongClip();
        if (clip == null) yield break;

        clipFrequency = clip.frequency;
        originalVolume = Mathf.Clamp01(songSource.volume);
        songSource.playOnAwake = false;
        songSource.loop = false;

        GoToLine(0);
        ui?.UpdateAutoMode(autoMode); // 초기 버튼 라벨 동기화
    }

    // -------------------- JSON --------------------
    private IEnumerator LoadJsonData()
    {
        var dm = GameDataManager.Instance;
        var step = dm.CurrentStep ?? StepResourceResolver.CreateFallbackStep();
        var chapterId = string.IsNullOrEmpty(dm.CurrentChapterId)
            ? StepResourceResolver.GetFallbackChapterId()
            : dm.CurrentChapterId;

        string path = $"json/{chapterId}/{step.id}_singalong";
        TextAsset json = Resources.Load<TextAsset>(path);
        if (json == null)
        {
            Debug.LogError($"[SingAlongManager] JSON not found at Resources/{path}.json");
            yield break;
        }

        try
        {
            lines = JsonHelper.FromJson<SingAlongLine>(json.text)?.ToList() ?? new List<SingAlongLine>();
            lines = lines
                .Where(l => l != null && !string.IsNullOrWhiteSpace(l.sentence) && l.start >= 0 && l.end > l.start)
                .ToList();
            Debug.Log($"[SingAlongManager] ✅ Loaded {lines.Count} lines.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SingAlongManager] JSON parse error: {ex.Message}");
        }

        yield return null;
    }

    // -------------------- MP3 --------------------
    private AudioClip LoadSongClip()
    {
        var dm = GameDataManager.Instance;
        var step = dm.CurrentStep ?? StepResourceResolver.CreateFallbackStep();
        var chapterId = string.IsNullOrEmpty(dm.CurrentChapterId)
            ? StepResourceResolver.GetFallbackChapterId()
            : dm.CurrentChapterId;

        var clip = StepResourceResolver.LoadSongClip(chapterId, step);
        if (clip == null)
        {
            Debug.LogError($"[SingAlongManager] MP3 not found for {chapterId}/{step.id}");
            return null;
        }
        songSource.clip = clip;
        return clip;
    }

    // -------------------- Flow --------------------
    private void GoToLine(int index)
    {
        if (index < 0 || index >= lines.Count)
        {
            FinishAll();
            return;
        }

        currentIndex = index;
        retryCount = 0;
        matchedWords.Clear();  // 새로운 문장 시작 시 초기화
        canGoNext = false;     // 오토 OFF일 때 다음으로 넘어가려면 STT 1회 필요
        isPaused = false;

        var line = lines[currentIndex];
        ui?.UpdateCenter(line.sentence, "Listen, then repeat…");
        ui?.SetMicActive(false);

        // ✅ 현재 진행도 반영
        ui?.UpdateProgressBar(currentIndex, lines.Count);

        KillCoroutine(ref playingCo);
        KillCoroutine(ref sttTimeoutCo);
        isPlayingSegment = false;
        isWaitingSTT = false;

        songSource.volume = originalVolume;
        playingCo = StartCoroutine(PlaySegment_DSP(line.start, line.end));
    }


    private IEnumerator PlaySegment_DSP(float startSec, float endSec)
    {
        isPlayingSegment = true;
        ui?.SetMusicNoteIcon();

        ui?.UpdateStatus("Playing", false); // ✅ 추가

        if (songSource.clip == null)
        {
            Debug.LogError("[SingAlongManager] ❌ No clip on songSource.");
            yield break;
        }

        songSource.Stop();
        yield return null; // 1프레임 버퍼 클리어

        endSec += 0.08f;

        // 🎯 샘플 위치 계산
        int startSamples = Mathf.FloorToInt(startSec * songSource.clip.frequency);
        int endSamples = Mathf.FloorToInt(endSec * songSource.clip.frequency);
        double segDuration = (double)(endSamples - startSamples) / songSource.clip.frequency;

        // 🎯 DSP 예약 기반 정확한 재생
        double dspNow = AudioSettings.dspTime;
        double dspStart = dspNow + 0.05; // 아주 짧은 예약 리드타임 (0.05초)
        double dspEnd = dspStart + segDuration;

        songSource.timeSamples = startSamples;
        songSource.PlayScheduled(dspStart);
        songSource.SetScheduledEndTime(dspEnd);

        Debug.Log($"[SingAlong] ▶ DSP segment start={startSec:F2}s end={endSec:F2}s (duration={segDuration:F2}s)");

        // 🎯 세그먼트가 끝날 때까지 정확히 대기
        while (AudioSettings.dspTime < dspEnd)
            yield return null;

        // 🎯 이후 STT 전환
        ui?.UpdateCenter(lines[currentIndex].sentence, "Now repeat after me…");
        ui?.SetMicActive(false);
        songSource.volume = originalVolume;

        //cueSource?.Play();
        yield return new WaitForSeconds(0.3f);
        BeginSTT();
    }


    private void BeginSTT()
    {
        if (isWaitingSTT) return;
        isWaitingSTT = true;

        songSource.volume = 0f;
        ui?.SetMicActive(true);
        ui?.UpdateCenter(lines[currentIndex].sentence, "Recording…");
        ui?.UpdateStatus("Recording", true); // ✅ 추가

#if UNITY_ANDROID && !UNITY_EDITOR
        speechBridge.StartListening();
        KillCoroutine(ref sttTimeoutCo);
        sttTimeoutCo = StartCoroutine(STTTimeoutGuard());
#else
        KillCoroutine(ref sttTimeoutCo);
        sttTimeoutCo = StartCoroutine(EditorMockSTT());
#endif
    }

    private IEnumerator STTTimeoutGuard()
    {
        float t = 0f;
        while (t < sttTimeoutSec && isWaitingSTT)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!isWaitingSTT) yield break;

        isWaitingSTT = false;
        ui?.SetMicActive(false);
        songSource.volume = originalVolume;

        // 타임아웃도 "시도 1회"로 간주 → 다음 이동 가능(오토 OFF 기준)
        canGoNext = true;

        if (autoMode)
        {
            ui?.ShowRecognized("(No response) Let’s try the next line.");
            ui?.UpdateStatus("Playing", false); 
            StartCoroutine(NextLineAfterDelay());
        }
        else
        {
            ui?.ShowRecognized("No response. Tap ▶ to continue.");
        }
    }

    private IEnumerator EditorMockSTT()
    {
        yield return new WaitForSeconds(Mathf.Min(2.0f, sttTimeoutSec * 0.5f));
        OnSpeechResult("mock recognized text");
    }

    private void FinishAll()
    {
        KillCoroutine(ref playingCo);
        KillCoroutine(ref sttTimeoutCo);
        isPlayingSegment = false;
        isWaitingSTT = false;
        isPaused = false;

        //ui?.UpdateCenter("Great job!", "You’ve finished the song!");
        ui?.SetMicActive(false);
        songSource.volume = originalVolume;
        ui?.ShowFinishPanel(passedCount, lines.Count);
    }

    private void KillCoroutine(ref Coroutine co)
    {
        if (co != null) StopCoroutine(co);
        co = null;
    }

    // -------------------- Buttons --------------------
    public void OnPrevLine()
    {
        if (currentIndex <= 0) return;
        GoToLine(currentIndex - 1);
    }

    public void OnNextLine()
    {
        Debug.Log($"[SingAlong] OnNextLine() clicked. autoMode={autoMode}, canGoNext={canGoNext}, currentIndex={currentIndex}/{lines?.Count-1}");
        if (!autoMode && !canGoNext)
        {
            Debug.Log("[SingAlong] Next blocked: autoMode OFF & STT not completed yet.");
            ui?.ShowFeedback("Please try speaking first!");
            return;
        }
        if (currentIndex >= lines.Count - 1)
        {
            Debug.Log("[SingAlong] Next at last line → FinishAll()");
            FinishAll();
            return;
        }
        GoToLine(currentIndex + 1);
    }

    // ▶/⏸ 토글
    public void TogglePlayPause()
    {
        if (isPaused)
        {
            // ⏸ → ▶ : 현재 라인 처음부터 재생
            ResumeFromStart();

            // 아이콘 변경
            if (playPauseButtonImage != null && pauseIcon != null)
                playPauseButtonImage.sprite = pauseIcon;
        }
        else
        {
            // ▶ → ⏸ : 세그먼트/녹음 상태를 정지하고 대기
            PauseCurrent();

            // 아이콘 변경
            if (playPauseButtonImage != null && playIcon != null)
                playPauseButtonImage.sprite = playIcon;
        }
    }

    // 오토 모드 토글 (UI 버튼 OnClick에 연결)
    public void ToggleAutoMode()
    {
        autoMode = !autoMode;
        ui?.UpdateAutoMode(autoMode);
        Debug.Log($"[SingAlong] AutoMode = {autoMode}");
    }

    // -------------------- Pause/Resume 구현 --------------------
    private void PauseCurrent()
    {
        // 세그먼트 재생 중이면 정지
        if (isPlayingSegment)
        {
            KillCoroutine(ref playingCo);
            songSource.Stop();          // DSP 스케줄 중단 포함 안전 정지
            isPlayingSegment = false;
        }

        // STT 대기 중이면 정지(마이크 중지 명령은 호출하지 않음: 외부 브릿지 의존 제거)
        if (isWaitingSTT)
        {
            KillCoroutine(ref sttTimeoutCo);
            isWaitingSTT = false;
            ui?.SetMicActive(false);
            songSource.volume = originalVolume;
        }

        isPaused = true;
        ui?.ShowRecognized("Paused. Tap ▶ to replay this line.");
    }

    private void ResumeFromStart()
    {
        if (currentIndex < 0 || currentIndex >= (lines?.Count ?? 0))
        {
            isPaused = false;
            return;
        }

        // 현재 라인을 처음부터 다시 재생 → 재생 후 자동으로 STT 진입
        var line = lines[currentIndex];

        // 안전 초기화
        KillCoroutine(ref sttTimeoutCo);
        KillCoroutine(ref playingCo);
        isPlayingSegment = false;
        isWaitingSTT = false;

        songSource.volume = originalVolume;
        playingCo = StartCoroutine(PlaySegment_DSP(line.start, line.end));

        isPaused = false;
    }

    // -------------------- 정규화 및 매칭 --------------------
    private string CleanWord(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = s.Replace('’', '\'').Replace('‘', '\'').Replace('“', '"').Replace('”', '"');
        s = new string(s.Where(c => !char.IsPunctuation(c)).ToArray());
        s = s.Normalize(NormalizationForm.FormKC);
        return s.Trim().ToLower();
    }

    // -------------------- STT Callback --------------------
    public void OnSpeechResult(string recognizedText)
    {
        if (!isWaitingSTT) return;
        isWaitingSTT = false;

        ui?.UpdateStatus("Playing", false);        

        var targetLine = lines[currentIndex].sentence;
        var recogWords = Regex.Matches(recognizedText ?? "", @"\b[\w']+\b")
            .Select(m => CleanWord(m.Value))
            .ToHashSet();

        var targetWords = Regex.Matches(targetLine, @"\b[\w']+\b")
            .Select(m => CleanWord(m.Value))
            .ToList();

        foreach (var w in targetWords)
            if (recogWords.Contains(w))
                matchedWords.Add(w); // 누적 갱신

        float score = (float)matchedWords.Count / targetWords.Count;
        string highlighted = HighlightMatchesIncremental(targetLine);

        ui?.SetMicActive(false);
        ui?.UpdateCenter(highlighted, null);
        songSource.volume = originalVolume;

        // STT가 끝났으므로 다음 이동 가능(오토 OFF 기준)
        canGoNext = true;

        if (score >= 0.6f)
        {
            ui?.ShowFeedback("Good job!");
            retryCount = 0;

        if (!linePassed[currentIndex])
        {
            linePassed[currentIndex] = true;
            passedCount = linePassed.Count(p => p); // or manually ++
            ui?.UpdateProgress(passedCount, lines.Count);
        }            

            if (autoMode)
                StartCoroutine(NextLineAfterDelay());
            else
                ui?.ShowRecognized("Ready for next line. Tap ▶ to continue.");
        }
        else
        {
            retryCount++;
            if (retryCount < 2)
            {
                ui?.ShowFeedback("Try again!");
                StartCoroutine(RestartCurrentLine());
            }
            else
            {
                ui?.ShowFeedback("Let's move on!");
                retryCount = 0;

                if (autoMode)
                    StartCoroutine(NextLineAfterDelay());
                else
                    ui?.ShowRecognized("Tap ▶ when you're ready for the next line.");
            }
        }
    }

    private string HighlightMatchesIncremental(string target)
    {
        var words = Regex.Matches(target, @"\b[\w']+\b")
            .Cast<Match>()
            .Select(m => m.Value)
            .ToList();

        return string.Join(" ",
            words.Select(w =>
                matchedWords.Contains(CleanWord(w))
                    ? $"<color=#00FF88>{w}</color>"
                    : $"<color=#FFFFFF>{w}</color>"));
    }

    private IEnumerator RestartCurrentLine(bool replaySegment = true)
    {
        KillCoroutine(ref sttTimeoutCo);
        KillCoroutine(ref playingCo);
        isPlayingSegment = false;
        isWaitingSTT = false;

        yield return new WaitForSeconds(afterAnalyzeDelaySec);

        if (replaySegment)
        {
            var line = lines[currentIndex];
            playingCo = StartCoroutine(PlaySegment_DSP(line.start, line.end));
        }
        else
        {
            BeginSTT();
        }
    }

    private IEnumerator NextLineAfterDelay()
    {
        yield return new WaitForSeconds(afterAnalyzeDelaySec);
        OnNextLine();
    }
}

// -------------------- Data --------------------
[Serializable]
public class SingAlongLine
{
    public string sentence;
    public float start;
    public float end;
    public List<WordTiming> words;
}

[Serializable]
public class WordTiming
{
    public string word;
    public float start;
    public float end;
}