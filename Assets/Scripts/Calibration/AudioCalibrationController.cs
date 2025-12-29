using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class AudioCalibrationController : MonoBehaviour
{
    [Header("UI Document & Audio")]
    [SerializeField] private UIDocument uiDocument;        // 씬에 붙은 UIDocument
    [SerializeField] private AudioSource metronomeSource;  // 0.6초짜리 메트로놈 mp3

    [Header("Metronome Settings")]
    [SerializeField] private float clickInterval = 0.6f;   // 메트로놈 간격(초)
    [SerializeField] private int requiredTaps = 10;        // 탭 횟수

    [Header("AudioSource Pool")]
    [SerializeField] private int poolSize = 16;            // 풀 크기

    // UI Elements
    private Label countdownLabel;
    private Label tapNowLabel;
    private Slider offsetSlider;
    private Label offsetLabel;
    private Label resultLabel;
    private Button startTestButton;
    private Button applyButton;
    private VisualElement tapElement;
    private readonly List<VisualElement> tapDots = new List<VisualElement>();

    // 상태
    private bool isCalibrating = false;
    private readonly List<double> clickDspTimes = new List<double>(); // 각 클릭 DSP 스케줄 시간
    private readonly List<float> tapOffsets = new List<float>();      // 탭 offset (초 단위)

    // AudioSource 풀
    private AudioSource[] sourcePool;
    private bool poolReady = false;

    // 실제 오디오 출력 지연 (DSP 예약 시점 → 실제 출력 시작)
    private float audioOutputDelaySeconds = 0f;

    private void Awake()
    {
        Debug.Log("==== [Calib] Awake() ====");

        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
            Debug.Log($"[Calib] uiDocument from GetComponent: {(uiDocument ? "OK" : "NULL")}");
        }

        if (uiDocument == null)
        {
            Debug.LogError("[Calib] UIDocument is not assigned. Abort.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        Debug.Log("[Calib] RootVisualElement acquired.");

        countdownLabel  = root.Q<Label>("CountdownLabel");
        tapNowLabel     = root.Q<Label>("TapNowLabel");
        offsetSlider    = root.Q<Slider>("OffsetSlider");
        offsetLabel     = root.Q<Label>("OffsetLabel");
        resultLabel     = root.Q<Label>("ResultLabel");
        startTestButton = root.Q<Button>("StartTestButton");
        applyButton     = root.Q<Button>("ApplyButton");

        Debug.Log($"[Calib] UI refs: " +
                  $"countdownLabel={(countdownLabel!=null)}, " +
                  $"tapNowLabel={(tapNowLabel!=null)}, " +
                  $"offsetSlider={(offsetSlider!=null)}, " +
                  $"offsetLabel={(offsetLabel!=null)}, " +
                  $"resultLabel={(resultLabel!=null)}, " +
                  $"startTestButton={(startTestButton!=null)}, " +
                  $"applyButton={(applyButton!=null)}");

        tapElement = root.Q<VisualElement>("TapArea") ?? tapNowLabel;
        Debug.Log($"[Calib] tapElement: {(tapElement != null ? tapElement.name : "NULL")}");

        // Tap 도트
        var progressRow = root.Q<VisualElement>("TapProgressRow");
        tapDots.Clear();
        if (progressRow != null)
        {
            for (int i = 0; i < requiredTaps; i++)
            {
                var dot = progressRow.Q<VisualElement>($"Dot{i}");
                tapDots.Add(dot);
                Debug.Log($"[Calib] TapDot[{i}]: {(dot != null ? "FOUND" : "NULL")}");
            }
        }
        else
        {
            Debug.LogWarning("[Calib] TapProgressRow not found.");
        }

        if (tapNowLabel != null)
            tapNowLabel.style.opacity = 0f;

        if (resultLabel != null)
            resultLabel.text = "";

        if (offsetSlider != null && offsetLabel != null)
        {
            offsetLabel.text = $"Offset: {offsetSlider.value:F0} ms";
            Debug.Log($"[Calib] Initial OffsetSlider value = {offsetSlider.value} ms");
        }

        if (startTestButton != null)
            startTestButton.clicked += OnStartTestClicked;

        if (applyButton != null)
            applyButton.clicked += OnApplyClicked;

        if (tapElement != null)
            tapElement.RegisterCallback<PointerDownEvent>(OnTap);

        SetupSourcePool();
    }

    private void OnDestroy()
    {
        Debug.Log("[Calib] OnDestroy()");
        if (startTestButton != null) startTestButton.clicked -= OnStartTestClicked;
        if (applyButton != null) applyButton.clicked -= OnApplyClicked;
        if (tapElement != null) tapElement.UnregisterCallback<PointerDownEvent>(OnTap);
    }

    // =========================================================
    // AudioSource Pool 생성
    // =========================================================
    private void SetupSourcePool()
    {
        Debug.Log("==== [Calib] SetupSourcePool() ====");

        if (metronomeSource == null)
        {
            Debug.LogError("[Calib] metronomeSource is NULL. Pool cannot be created.");
            poolReady = false;
            return;
        }
        if (metronomeSource.clip == null)
        {
            Debug.LogError("[Calib] metronomeSource.clip is NULL. Pool cannot be created.");
            poolReady = false;
            return;
        }

        Debug.Log($"[Calib] MetronomeSource OK. clip='{metronomeSource.clip.name}', length={metronomeSource.clip.length:F3}s");

        sourcePool = new AudioSource[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            var obj = new GameObject($"MetronomeSource_{i}");
            obj.transform.SetParent(this.transform);
            var src = obj.AddComponent<AudioSource>();
            src.clip = metronomeSource.clip;
            src.playOnAwake = false;
            src.volume = metronomeSource.volume;
            src.pitch = metronomeSource.pitch;
            src.loop = false;

            sourcePool[i] = src;

            Debug.Log($"[Calib] Pool[{i}] created. volume={src.volume}, pitch={src.pitch}");
        }

        poolReady = true;
        Debug.Log("[Calib] AudioSource pool READY.");
    }

    // =========================================================
    // 버튼 / 이벤트 핸들러
    // =========================================================
    private void OnStartTestClicked()
    {
        Debug.Log("==== [Calib] OnStartTestClicked() ====");
        if (!poolReady)
        {
            Debug.LogWarning("[Calib] AudioSource pool is not ready.");
            return;
        }

        foreach (var dot in tapDots)
            dot?.RemoveFromClassList("filled");

        StopAllCoroutines();
        if (sourcePool != null)
        {
            foreach (var src in sourcePool)
                src?.Stop();
        }

        Debug.Log("[Calib] Coroutines stopped, all pool sources stopped. Starting CalibrationRoutine.");
        StartCoroutine(CalibrationRoutine());
    }

    private void OnApplyClicked()
    {
        if (offsetSlider == null)
        {
            Debug.LogWarning("[Calib] OnApplyClicked but offsetSlider is NULL.");
            return;
        }

        float finalOffsetMs = offsetSlider.value;
        GameSettings.AudioOffsetMs = finalOffsetMs;

        Debug.Log($"[Calib] APPLY OFFSET clicked. Saved Offset = {finalOffsetMs} ms");
    }

    private void OnTap(PointerDownEvent evt)
    {
        if (!isCalibrating)
        {
            Debug.Log("[CalibTap] OnTap called but !isCalibrating. Ignored.");
            return;
        }

        if (tapOffsets.Count >= requiredTaps)
        {
            Debug.Log("[CalibTap] OnTap called but tapOffsets already full.");
            return;
        }

        if (clickDspTimes.Count < requiredTaps)
        {
            Debug.LogWarning($"[CalibTap] clickDspTimes.Count({clickDspTimes.Count}) < requiredTaps({requiredTaps}).");
            return;
        }

        double tapDsp = AudioSettings.dspTime;
        int index = tapOffsets.Count;
        double clickDsp = clickDspTimes[index];

        double clickHeardDsp = clickDsp + audioOutputDelaySeconds;
        float offsetSeconds = (float)(tapDsp - clickHeardDsp);
        tapOffsets.Add(offsetSeconds);

        if (index >= 0 && index < tapDots.Count && tapDots[index] != null)
            tapDots[index].AddToClassList("filled");

        Debug.Log(
            $"[CalibTap] idx={index}, tapDsp={tapDsp:F6}, " +
            $"clickDsp={clickDsp:F6}, audioOutputDelay={audioOutputDelaySeconds:F6}, " +
            $"clickHeardDsp={clickHeardDsp:F6}, offset={offsetSeconds*1000f:F1} ms, " +
            $"tapCount={tapOffsets.Count}/{requiredTaps}");

        if (tapOffsets.Count >= requiredTaps)
        {
            Debug.Log("[CalibTap] Required taps reached. isCalibrating = false.");
            isCalibrating = false;
        }
    }

    // =========================================================
    // 칼리브레이션 루틴
    // =========================================================
    private IEnumerator CalibrationRoutine()
    {
        Debug.Log("==== [Calib] CalibrationRoutine START ====");

        tapOffsets.Clear();
        clickDspTimes.Clear();
        isCalibrating = false;

        if (resultLabel != null)
            resultLabel.text = "";
        if (tapNowLabel != null)
            tapNowLabel.style.opacity = 0f;

        // STEP 1: 오디오 출력 딜레이 측정
        Debug.Log("[Calib] Step1: MeasureAudioOutputDelay() 시작");
        yield return StartCoroutine(MeasureAudioOutputDelay());
        Debug.Log($"[Calib] Step1 완료. audioOutputDelaySeconds={audioOutputDelaySeconds:F6}s ({audioOutputDelaySeconds*1000f:F1} ms)");

        // STEP 2: 카운트다운
        if (countdownLabel != null)
        {
            countdownLabel.text = "3";
            Debug.Log("[Calib] Countdown: 3");
            yield return new WaitForSeconds(0.6f);

            countdownLabel.text = "2";
            Debug.Log("[Calib] Countdown: 2");
            yield return new WaitForSeconds(0.6f);

            countdownLabel.text = "1";
            Debug.Log("[Calib] Countdown: 1");
            yield return new WaitForSeconds(0.6f);

            countdownLabel.text = "";
            Debug.Log("[Calib] Countdown done.");
        }

        if (tapNowLabel != null)
            tapNowLabel.style.opacity = 1f;

        // STEP 3: DSP 기반 메트로놈 스케줄
        double dspNow = AudioSettings.dspTime;
        double firstClickDsp = dspNow + 0.5f; // 0.5s 후 첫 클릭

        Debug.Log($"[Calib] Step3: Scheduling clicks. dspNow={dspNow:F6}, firstClickDsp={firstClickDsp:F6}, clickInterval={clickInterval:F3}, requiredTaps={requiredTaps}");

        clickDspTimes.Clear();

        for (int i = 0; i < requiredTaps; i++)
        {
            double sched = firstClickDsp + i * clickInterval;

            var src = sourcePool[i % poolSize];
            if (src != null)
            {
                src.PlayScheduled(sched);
                Debug.Log($"[Calib]   Click[{i}] scheduled at dsp={sched:F6} using pool[{i % poolSize}]");
            }
            else
            {
                Debug.LogWarning($"[Calib]   Click[{i}] src NULL for pool index {i % poolSize}");
            }

            clickDspTimes.Add(sched);
        }

        isCalibrating = true;
        Debug.Log("[Calib] Waiting for taps...");

        while (isCalibrating)
            yield return null;

        if (tapNowLabel != null)
            tapNowLabel.style.opacity = 0f;

        Debug.Log($"[Calib] Taps finished. tapOffsets.Count={tapOffsets.Count}");
        ShowResult();
        Debug.Log("==== [Calib] CalibrationRoutine END ====");
    }

    // =========================================================
    // 실제 오디오 출력 딜레이 측정
    // =========================================================
    private IEnumerator MeasureAudioOutputDelay()
    {
        Debug.Log("---- [Calib] MeasureAudioOutputDelay() START ----");

        AudioSource testSrc = metronomeSource;
        if (testSrc == null)
        {
            Debug.LogError("[Calib] MeasureAudioOutputDelay: metronomeSource NULL.");
            yield break;
        }
        if (testSrc.clip == null)
        {
            Debug.LogError("[Calib] MeasureAudioOutputDelay: metronomeSource.clip NULL.");
            yield break;
        }

        double dspNow = AudioSettings.dspTime;
        double scheduled = dspNow + 0.2f;   // 0.2초 후 재생

        testSrc.Stop();
        testSrc.PlayScheduled(scheduled);

        Debug.Log($"[Calib] Measure: dspNow={dspNow:F6}, scheduled={scheduled:F6}, clipLen={testSrc.clip.length:F3}s");

        float[] buffer = new float[256];
        bool detected = false;
        int frameCount = 0;

        while (!detected)
        {
            frameCount++;
            testSrc.GetOutputData(buffer, 0);

            float sum = 0f;
            for (int i = 0; i < buffer.Length; i++)
                sum += buffer[i] * buffer[i];

            float rms = Mathf.Sqrt(sum / buffer.Length);
            double now = AudioSettings.dspTime;

            if (rms > 0.001f)
            {
                detected = true;
                audioOutputDelaySeconds = (float)(now - scheduled);

                Debug.Log(
                    $"[Calib] Measure DETECTED. frame={frameCount}, rms={rms:F6}, " +
                    $"dspNow={now:F6}, scheduled={scheduled:F6}, " +
                    $"delay={audioOutputDelaySeconds:F6}s ({audioOutputDelaySeconds*1000f:F1} ms)");
                break;
            }

            if (frameCount % 10 == 0)
            {
                Debug.Log($"[Calib] Measure loop... frame={frameCount}, rms={rms:F6}, dspNow={now:F6}");
            }

            yield return null;
        }

        if (!detected)
        {
            Debug.LogWarning("[Calib] MeasureAudioOutputDelay finished without detection.");
        }

        Debug.Log("---- [Calib] MeasureAudioOutputDelay() END ----");
    }

    // =========================================================
    // 결과 계산 / 표시
    // =========================================================
    private void ShowResult()
    {
        Debug.Log("---- [Calib] ShowResult() ----");

        if (tapOffsets.Count < 3)
        {
            Debug.LogWarning($"[Calib] Not enough taps to calculate offset. tapOffsets.Count={tapOffsets.Count}");
            return;
        }

        // 원본 로그
        for (int i = 0; i < tapOffsets.Count; i++)
        {
            Debug.Log($"[Calib] tapOffsets[{i}] = {tapOffsets[i]*1000f:F3} ms");
        }

        List<float> processed = new List<float>(tapOffsets);

        float min = processed.Min();
        float max = processed.Max();

        processed.Remove(min);
        processed.Remove(max);

        Debug.Log($"[Calib] Removed min={min*1000f:F3} ms, max={max*1000f:F3} ms");
        Debug.Log($"[Calib] Remaining {processed.Count} values:");

        for (int i = 0; i < processed.Count; i++)
        {
            Debug.Log($"[Calib]   processed[{i}] = {processed[i]*1000f:F3} ms");
        }

        float avgSeconds = processed.Average();
        float avgMs = avgSeconds * 1000f;

        Debug.Log($"[Calib] Average(before clamp) = {avgMs:F3} ms");

        if (offsetSlider != null)
        {
            float clamped = Mathf.Clamp(avgMs, offsetSlider.lowValue, offsetSlider.highValue);
            Debug.Log($"[Calib] Slider range = [{offsetSlider.lowValue}, {offsetSlider.highValue}] ms, clamped={clamped:F3} ms");
            offsetSlider.SetValueWithoutNotify(clamped);
            avgMs = clamped;
        }

        string sign = avgMs >= 0 ? "+" : "-";
        float absMs = Mathf.Abs(avgMs);

        if (offsetLabel != null)
            offsetLabel.text = $"Offset: {sign}{absMs:F0} ms";

        if (resultLabel != null)
            resultLabel.text = $"Your average offset: {sign}{absMs:F0} ms";

        Debug.Log($"[Calib] FINAL OFFSET = {sign}{absMs:F3} ms  (audioOutputDelay={audioOutputDelaySeconds*1000f:F1} ms)");
        Debug.Log("---- [Calib] ShowResult() END ----");
    }
}
