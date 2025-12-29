using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SingAlongUI : MonoBehaviour
{
    [Header("Center UI Elements")]
    public TMP_Text currentLyric;
    public TMP_Text repeatHint;
    public Image micPulse;
    private MicPulseAnim micAnim;

    [Header("Icons")]
    public Sprite micSprite;
    public Sprite musicNoteSprite;

    private Coroutine feedbackCo;
    private string lastHintText = "";
    private float defaultHintFontSize;   // ✅ 초기 크기 저장용
    private Color defaultHintColor;      // ✅ 초기 색상 저장용

    public TMP_Text autoModeLabel;

    public TMP_Text passCounterText;   // "✅ Passed: 1 / 10"

    [Header("Top UI Elements")]
    public Slider progressBar;

    [Header("Status (Top Right)")]
    public Image statusIcon;     // 🎵 또는 🎙️ 아이콘
    public TMP_Text statusLabel; // "Playing" 또는 "Recording"
    public Sprite playingIcon;
    public Sprite recordingIcon;

    [Header("Finish Panel")]
    public GameObject finishPanel;      // FinishPanel 오브젝트
    public TMP_Text finishTitle;        // Title
    public TMP_Text finishSubtitle;     // Subtitle
    public TMP_Text finishProgress;     // ProgressText
    public Button confirmButton;        // ConfirmButton
    public Button replayButton;         // ReplayButton

    void Awake()
    {
        micAnim = micPulse?.GetComponent<MicPulseAnim>();

        if (repeatHint != null)
        {
            defaultHintFontSize = repeatHint.fontSize;
            defaultHintColor = repeatHint.color;
        }

        // 버튼 이벤트 연결 (Inspector에서 연결해도 됨)
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmButton);

        if (replayButton != null)
            replayButton.onClick.AddListener(OnReplayButton);

    }

    public void UpdateCenter(string lyric, string hint)
    {
        if (currentLyric != null)
            currentLyric.text = lyric;

        if (repeatHint != null)
        {
            repeatHint.text = hint;
            repeatHint.fontSize = defaultHintFontSize;
            repeatHint.color = defaultHintColor;
        }
    }

    public void ShowRecognized(string message)
    {
        if (repeatHint != null)
        {
            repeatHint.text = message;
            repeatHint.fontSize = defaultHintFontSize * 0.9f;
            repeatHint.color = Color.cyan;
        }
    }

    public void SetMicActive(bool active)
    {
        if (micAnim != null)
            micAnim.SetActive(active);

        if (micPulse != null && micSprite != null)
            micPulse.sprite = micSprite;
    }

    public void SetMusicNoteIcon()
    {
        if (micPulse != null && musicNoteSprite != null)
            micPulse.sprite = musicNoteSprite;
    }

    public void ShowCountdown(int number)
    {
        if (repeatHint == null) return;

        repeatHint.text = number.ToString();
        repeatHint.fontSize = defaultHintFontSize * 2f;
        repeatHint.color = Color.yellow;

        var rect = repeatHint.GetComponent<RectTransform>();
        rect.localScale = Vector3.one * 1.5f;
        LeanTween.scale(rect, Vector3.one, 0.4f).setEaseOutBack();
    }

    // ✅ 노란 영역에 피드백 표시 (폰트 크기 복원 포함)
    public void ShowFeedback(string message)
    {
        if (repeatHint == null) return;

        lastHintText = repeatHint.text;

        if (feedbackCo != null)
            StopCoroutine(feedbackCo);

        feedbackCo = StartCoroutine(FeedbackRoutine(message));
    }

    private IEnumerator FeedbackRoutine(string message)
    {
        // 피드백 표시
        repeatHint.text = message;
        repeatHint.fontSize = defaultHintFontSize * 1.1f;
        repeatHint.color = message.Contains("Good") ? new Color(0f, 1f, 0.5f) : Color.red;

        var rect = repeatHint.GetComponent<RectTransform>();
        rect.localScale = Vector3.one * 1.2f;
        LeanTween.scale(rect, Vector3.one, 0.3f).setEaseOutBack();

        yield return new WaitForSeconds(1.2f);

        // 원래 문구 및 스타일 복귀
        repeatHint.text = lastHintText;
        repeatHint.fontSize = defaultHintFontSize;
        repeatHint.color = defaultHintColor;
    }



    public void UpdateAutoMode(bool isOn)
    {
        if (autoModeLabel == null) return;

        autoModeLabel.text = isOn ? "Auto Mode: On" : "Auto Mode: Off";
        autoModeLabel.color = isOn ? Color.white : new Color(1f, 0.8f, 0.8f);
    }

    public void UpdateProgress(int passed, int total)
    {
        if (passCounterText != null)
            passCounterText.text = $"Passed: {passed} / {total}";
    }

    public void UpdateProgressBar(int currentIndex, int total)
    {
        if (progressBar != null && total > 0)
        {
            progressBar.value = Mathf.Clamp01((float)(currentIndex + 1) / total);
        }
    }

    public void UpdateStatus(string text, bool isRecording)
    {
        if (statusLabel != null)
            statusLabel.text = text;

        if (statusIcon != null)
            statusIcon.sprite = isRecording ? recordingIcon : playingIcon;
    }

    public void ShowFinishPanel(int passed, int total)
    {
        float passRate = total > 0 ? (float)passed / total : 0f;

        string titleText;
        string subtitleText;

        if (passRate >= 0.9f)
        {
            titleText = "Perfect!";
            subtitleText = "You nailed every line!";
        }
        else if (passRate >= 0.7f)
        {
            titleText = "Great Job!";
            subtitleText = "You’ve finished this step!";
        }
        else
        {
            titleText = "Keep Practicing!";
            subtitleText = "Try again to improve your score!";
        }

        if (finishTitle != null)
            finishTitle.text = titleText;

        if (finishSubtitle != null)
            finishSubtitle.text = subtitleText;

        if (finishProgress != null)
            finishProgress.text = $"{passed}/{total}";

        if (finishPanel != null)
            finishPanel.SetActive(true);
    }


    // 확인 → 이전 화면
    public void OnConfirmButton()
    {
        //HideFinishPanel();
        SceneNavigator.Load("StepScene");
    }

    // 다시하기 → 1번 라인부터 재시작
    public System.Action OnReplayRequested; // Manager에서 받을 콜백

    public void OnReplayButton()
    {
        if (finishPanel != null) finishPanel.SetActive(false);
        OnReplayRequested?.Invoke();
    }

}
