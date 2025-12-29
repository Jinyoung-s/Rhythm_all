using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class WordData
{
    public string word;
    public float start;
    public float end;
    public string role;
}

public class RhythmDemoManager : MonoBehaviour
{
    public static RhythmDemoManager Instance;

    [Header("References")]
    public AudioSource audioSource;
    public RectTransform notesParent;
    public GameObject notePrefab;
    public RectTransform perfectLine;

    [Header("Settings")]
    public float fallDuration = 2.0f;
    public float noteSpeed = 600f;

    private List<WordData> words = new();

    [Header("Lane References")]
    public RectTransform laneSubject;
    public RectTransform laneVerb;
    public RectTransform laneObject;
    public RectTransform laneEtc;

    [Header("Note Backgrounds")]
    public Sprite bgSubject;
    public Sprite bgVerb;
    public Sprite bgObject;
    public Sprite bgEtc;

    [Header("Sync Offset")]
    public float offsetSeconds = 0f;   // 🔥 오디오-노트 싱크 보정값 (초 단위)

    public bool isGamePaused = false;

    [Header("Debug / Test Mode")]
    public bool firstWordTestMode = false;

    public UnityEngine.UI.Slider progressSlider;

    // 🔥 디버그용 카운터
    private int debugNoteCount = 0;
    private float debugUpdateTick = 0f;

    private double dspSongStart = 0f;

    void Awake()
    {
        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0;
        Instance = this;
        Debug.Log($"[RDM] Awake. offsetSeconds={offsetSeconds:F3}, isGamePaused={isGamePaused}");
        offsetSeconds = GameSettings.AudioOffsetSeconds;
        isGamePaused = false;
    }

    void Start()
    {
        Debug.Log("[RDM] Start() begin.");
        if (perfectLine != null)
        {
            Debug.Log($"[RDM] perfectLine localPos={perfectLine.localPosition}, anchoredPos={perfectLine.anchoredPosition}");
        }

        LoadJson();
        Debug.Log($"[RDM] Loaded words. count={words.Count}");

        StartMusic();
        StartCoroutine(SpawnNotes());

        if (firstWordTestMode)
            StartCoroutine(StopAfterFirstWord());
    }

    void Update()
    {
        if (audioSource == null)
            return;

        if (isGamePaused)
        {
            // 0.5초마다 한 번만 로그
            if (Time.time > debugUpdateTick)
            {
                debugUpdateTick = Time.time + 0.5f;
                //Debug.Log($"[RDM] Update SKIP (paused). audioTime={audioSource.time:F3}, AudioTime={AudioTime:F3}");
            }
            return;
        }

        // 진행도 슬라이더
        if (progressSlider != null && words.Count > 0)
        {
            float totalLength = words[words.Count - 1].end;
            float t = Mathf.Clamp01(AudioTime / totalLength);
            progressSlider.value = t;
        }

        // 0.5초마다 한 번 상태 로그
        if (Time.time > debugUpdateTick)
        {
            debugUpdateTick = Time.time + 0.5f;
            //Debug.Log($"[RDM] Update RUN. audioSource.time={audioSource.time:F3}, AudioTime={AudioTime:F3}, slider={(progressSlider ? progressSlider.value : -1f):F3}");
        }
    }

    // ===========================================================
    // 🔁 GameDataManager + StepResourceResolver 컨텍스트 공통 처리
    // ===========================================================
    private void ResolveContext(out string chapterId, out StepData step)
    {
        var dataManager = GameDataManager.Instance;

        // Step 결정 (없으면 fallback 생성)
        step = dataManager.CurrentStep ?? StepResourceResolver.CreateFallbackStep();
        if (dataManager.CurrentStep == null)
        {
            dataManager.CurrentStep = step;
        }

        // ChapterId 결정 (없으면 fallback)
        chapterId = string.IsNullOrEmpty(dataManager.CurrentChapterId)
            ? StepResourceResolver.GetFallbackChapterId()
            : dataManager.CurrentChapterId;

        dataManager.CurrentChapterId = chapterId;
    }

    // ===========================================================
    // 데이터 로드 (JSON + 역할 매핑) - 로딩 방식만 통일
    // ===========================================================
    void LoadJson()
    {
        // 🔁 여기서 더 이상 Resources.Load + 하드코딩 경로 사용하지 않고,
        // GameDataManager + StepResourceResolver 패턴으로 통일
        ResolveContext(out var chapterId, out var step);

        TextAsset timingJson = StepResourceResolver.LoadLyricsAsset(chapterId, step);
        TextAsset roleJson   = StepResourceResolver.LoadRoleAsset(chapterId, step);

        if (timingJson == null)
        {
            Debug.LogError($"[RDM] timingJson not found for {chapterId}/{step.id}.");
            return;
        }
        if (roleJson == null)
        {
            Debug.LogError($"[RDM] roleJson not found for {chapterId}/{step.id}.");
            return;
        }

        var timingList = JsonHelper.FromJson<WordData>(timingJson.text);
        var roleList   = JsonHelper.FromJson<WordData>(roleJson.text);

        if (timingList == null || timingList.Length == 0)
        {
            Debug.LogError("[RDM] timingList is empty.");
            return;
        }
        if (roleList == null || roleList.Length == 0)
        {
            Debug.LogWarning("[RDM] roleList is empty. All roles will be 'etc'.");
        }

        words.Clear();
        foreach (var t in timingList)
        {
            if (t == null || string.IsNullOrEmpty(t.word))
                continue;

            WordData match = null;
            if (roleList != null)
            {
                match = System.Array.Find(roleList, r =>
                    r != null &&
                    !string.IsNullOrEmpty(r.word) &&
                    r.word.ToLower() == t.word.ToLower());
            }

            t.role = (match != null && !string.IsNullOrEmpty(match.role)) ? match.role : "etc";
            words.Add(t);
        }

        Debug.Log($"[RDM] LoadJson() completed for {chapterId}/{step.id}. words={words.Count}");
    }

    // ===========================================================
    // 오디오 재생 - 로딩 방식만 StepResourceResolver로 통일
    // ===========================================================
    void StartMusic()
    {
        if (audioSource == null)
        {
            Debug.LogError("[RDM] StartMusic() failed: audioSource is null.");
            return;
        }

        // 인스펙터에서 클립이 비어있으면 로드
        if (audioSource.clip == null)
        {
            ResolveContext(out var chapterId, out var step);

            var clip = StepResourceResolver.LoadSongClip(chapterId, step);
            if (clip == null)
            {
                Debug.LogError($"[RDM] StartMusic() failed: AudioClip not found for {chapterId}/{step.id}.");
                return;
            }

            audioSource.clip = clip;
            Debug.Log($"[RDM] Loaded clip '{clip.name}' for {chapterId}/{step.id}.");
        }

        // DSP 기반 예약 재생
        double startDsp = AudioSettings.dspTime + 0.2f;   // 0.2초 후 시작
        dspSongStart = startDsp;

        audioSource.Stop();
        audioSource.PlayScheduled(startDsp);

        Debug.Log("=== StartMusic DSP Scheduled ===");
        Debug.Log($"Clip name: {audioSource.clip.name}");
        Debug.Log($"Clip length: {audioSource.clip.length:F3}s");
        Debug.Log($"DSP start = {startDsp:F6}");
    }

    // 🔥 AudioTime: 오직 audioSource.time + offsetSeconds 만 사용
    public float AudioTime
    {
        get
        {
            double now = AudioSettings.dspTime;
            double t = now - dspSongStart - offsetSeconds;

            if (t < 0) 
                t = 0;

            return (float)t;
        }
    }

    // ===========================================================
    // 노트 스폰 코루틴
    // ===========================================================
    IEnumerator SpawnNotes()
    {
        Debug.Log("[RDM] SpawnNotes() start.");

        if (words == null || words.Count == 0)
        {
            Debug.LogWarning("[RDM] SpawnNotes() aborted: words list is empty.");
            yield break;
        }

        for (int i = 0; i < words.Count - 1; i++)
        {
            var a = words[i];
            var b = words[i + 1];

            bool sameLane = a.role == b.role;
            bool nearTime = Mathf.Abs(b.start - a.start) <= 0.20f;

            float spawnAt = a.start - fallDuration;
            Debug.Log($"[RDM] Wait spawn word='{a.word}' index={i}, spawnAt={spawnAt:F3}");

            // 🔥 Pause 중이면 대기
            yield return new WaitUntil(() =>
                !isGamePaused && AudioTime >= spawnAt
            );

            if (sameLane && nearTime && false)
            {
                CreateNote_Long(a, b);
                i++;
            }
            else
            {
                CreateNote(a);
            }
        }

        Debug.Log("[RDM] SpawnNotes() end.");
    }

    private int zCounter = 10000;

    void CreateNote(WordData w)
    {
        if (notePrefab == null || notesParent == null)
        {
            Debug.LogError("[RDM] CreateNote() failed: notePrefab or notesParent is null.");
            return;
        }

        var go = Instantiate(notePrefab, notesParent);
        go.name = $"Note_{w.word}_{w.start:F3}";
        var rect = go.GetComponent<RectTransform>();

        float laneX = GetLaneX(w.role);
        float startY = GetStartY_Local();
        float targetY = perfectLine ? perfectLine.localPosition.y : 0f;
        float speed = CalcSpeed(startY, targetY);

        rect.localPosition = new Vector3(laneX, startY, 0f);
        rect.sizeDelta = new Vector2(260, 240);

        var img = go.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            img.sprite = GetBackgroundByRole(w.role);
        }

        var label = go.GetComponentInChildren<TMPro.TMP_Text>();
        if (label)
        {
            label.text = w.word;
            label.ForceMeshUpdate();                      // 🔥 텍스트 갱신 강제
            float textHeight = label.preferredWidth;     // 🔥 텍스트에 필요한 실제 높이(px)

            float baseHeight = 260f;                      // 기본 노트 높이
            float padding = 40f;                          // 여유 공간(필요하면 조정)

            float finalHeight = Mathf.Max(baseHeight, textHeight + padding);

            rect.sizeDelta = new Vector2(finalHeight, 240);
        }

        var ctrl = go.GetComponent<NoteController>();
        if (ctrl != null)
        {
            ctrl.Initialize(w.start, w.end, speed, targetY);
        }

        go.transform.SetAsFirstSibling();

        debugNoteCount++;
        if (debugNoteCount <= 5)
        {
            Debug.Log($"[RDM-Note] CreateNote #{debugNoteCount} word='{w.word}', start={w.start:F3}, " +
                      $"startY={startY:F1}, targetY={targetY:F1}, speed={speed:F3}, " +
                      $"AudioTime={AudioTime:F3}");
        }
    }

    Sprite GetBackgroundByRole(string role)
    {
        switch (role)
        {
            case "subject": return bgSubject;
            case "verb":    return bgVerb;
            case "object":  return bgObject;
            default:        return bgEtc;
        }
    }

    void CreateNote_Long(WordData a, WordData b)
    {
        if (notePrefab == null || notesParent == null)
        {
            Debug.LogError("[RDM] CreateNote_Long() failed: notePrefab or notesParent is null.");
            return;
        }

        var go = Instantiate(notePrefab, notesParent);
        var rect = go.GetComponent<RectTransform>();

        float laneX = GetLaneX(a.role);
        float startY = GetStartY_Local();
        float targetY = perfectLine ? perfectLine.localPosition.y : 0f;
        float speed = CalcSpeed(startY, targetY);

        rect.localPosition = new Vector3(laneX, startY, 0f);

        float timeDiff = b.end - a.start;
        float autoHeight = timeDiff * noteSpeed;
        float finalHeight = Mathf.Max(240f, autoHeight);
        rect.sizeDelta = new Vector2(260, finalHeight);

        var label = go.GetComponentInChildren<TMPro.TMP_Text>();
        if (label) label.text = a.word + " " + b.word;

        var ctrl = go.GetComponent<NoteController>();
        if (ctrl != null)
        {
            ctrl.Initialize(a.start, b.end, speed, targetY);
        }

        go.transform.localPosition =
            new Vector3(go.transform.localPosition.x, go.transform.localPosition.y, -zCounter * 0.01f);
        zCounter--;

        debugNoteCount++;
        if (debugNoteCount <= 5)
        {
            Debug.Log($"[RDM-Note] CreateNote_LONG #{debugNoteCount} word='{a.word} {b.word}', " +
                      $"start={a.start:F3}, startY={startY:F1}, targetY={targetY:F1}, speed={speed:F3}, " +
                      $"AudioTime={AudioTime:F3}");
        }
    }

    float GetStartY_Local()
    {
        float targetY = perfectLine ? perfectLine.localPosition.y : 0f;
        float startY = targetY + 1200f;
        return startY;
    }

    float GetLaneX(string role)
    {
        RectTransform lane = role switch
        {
            "subject" => laneSubject,
            "verb"    => laneVerb,
            "object"  => laneObject,
            _         => laneEtc,
        };

        if (lane == null || notesParent == null) return 0f;

        Vector3 worldCenter = lane.TransformPoint(Vector3.zero);
        Vector3 localPoint  = notesParent.InverseTransformPoint(worldCenter);

        return localPoint.x;
    }

    float CalcSpeed(float startY, float targetY)
    {
        float dist = Mathf.Max(10f, startY - targetY);
        float v = dist / Mathf.Max(0.1f, fallDuration);

        if (debugNoteCount < 5)
        {
            Debug.Log($"[RDM] CalcSpeed startY={startY:F1}, targetY={targetY:F1}, dist={dist:F1}, v={v:F3}");
        }
        return v;
    }

    // ===========================================================
    // 🔥 첫 단어 재생 후 바로 멈추는 테스트 기능
    // ===========================================================
    private IEnumerator StopAfterFirstWord()
    {
        if (words == null || words.Count == 0)
        {
            Debug.LogWarning("[Test] words 리스트 없음");
            yield break;
        }

        if (audioSource == null || audioSource.clip == null)
        {
            Debug.LogWarning("[Test] audioSource 또는 clip 없음");
            yield break;
        }

        var w0 = words[0];

        Debug.Log($"[Test] 첫 단어 테스트 시작: '{w0.word}' start={w0.start:F3}, end={w0.end:F3}");

        // 살짝 준비 시간
        yield return new WaitForSeconds(0.05f);

        // 🔥 '퍼펙트 타이밍' = JSON start 시점
        float targetTime = w0.start;

        // AudioTime = audioSource.time + offsetSeconds 기준으로 대기
        while (AudioTime < targetTime)
        {
            yield return null;
        }

        float nowAudioTime = AudioTime;
        float clipAudioTime = audioSource.time;

        Debug.Log(
            $"[Test] 첫 단어 퍼펙트 시점 도달! word='{w0.word}', " +
            $"JSON start={w0.start:F3}, end={w0.end:F3}, " +
            $"AudioTime={nowAudioTime:F3}, audioSource.time={clipAudioTime:F3}"
        );

        audioSource.Pause();
        isGamePaused = true;

        Debug.Log("[Test] 🔥 첫 퍼펙트 타이밍에서 Pause — 이 화면을 캡쳐해서 노트 위치랑 비교해봐.");
    }

    // ===========================================================
    // Pause / Resume
    // ===========================================================
    public void PauseGame()
    {
        isGamePaused = true;

        if (audioSource != null)
            audioSource.Pause();

        Debug.Log($"[RDM] PauseGame() called. audioSource.time={audioSource.time:F3}");
    }

    public void ResumeGame()
    {
        isGamePaused = false;

        if (audioSource == null)
        {
            Debug.LogWarning("[RDM] ResumeGame() audioSource null");
            return;
        }

        // 🔥 DSP 기준 재정렬
        double nowDsp = AudioSettings.dspTime;

        // 현재 재생중인 오디오 시점 (정확한 오디오 진행시간)
        double audioPos = audioSource.time;

        // dspSongStart를 재계산해서 AudioTime이 audioPos와 일치하도록 맞춘다
        dspSongStart = nowDsp - audioPos;

        audioSource.UnPause();

        Debug.Log(
            $"[RDM] ResumeGame() DSP realign --- nowDSP={nowDsp:F3}, " +
            $"audioPos={audioPos:F3}, new dspSongStart={dspSongStart:F3}"
        );
    }
}