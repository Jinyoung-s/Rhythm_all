using UnityEngine;
using TMPro;
using System.Collections;

public class JudgementManager : MonoBehaviour
{
    public static JudgementManager Instance;
    public TMP_Text feedbackText;
    public TMP_Text scoreText;
    public TMP_Text comboText;

    [Header("Judge Offset (Reaction Delay Fix)")]
    public float judgeOffset = 0.06f; // 60ms 기본값

    public int score = 0;
    private int combo = 0;

    private Vector3 originalScale;
    private Vector3 originalPosition;

    // 🔥 ScoreText 원본 위치/스케일 저장
    private Vector3 scoreOriginalScale;
    private Vector3 scoreOriginalPosition;

    public int Perfect { get; private set; } = 0;
    public int Great   { get; private set; } = 0;
    public int Good    { get; private set; } = 0;
    public int Miss    { get; private set; } = 0;
    public int MaxCombo { get; private set; } = 0;    

    void Awake()
    {
        Instance = this;
        originalScale = feedbackText.transform.localScale;
        originalPosition = feedbackText.transform.localPosition;

        // 🔥 Score용 원본값 저장
        scoreOriginalScale = scoreText.transform.localScale;
        scoreOriginalPosition = scoreText.transform.localPosition;
    }

    public enum JudgeResult { Perfect, Great, Good, Miss }

    public JudgeResult GetJudge(float rawDelta)
    {
        float delta = Mathf.Abs(rawDelta - judgeOffset);

        if (delta <= 0.08f) return JudgeResult.Perfect;
        if (delta <= 0.18f) return JudgeResult.Great;
        if (delta <= 0.30f) return JudgeResult.Good;
        return JudgeResult.Miss;
    }

    public void ApplyJudge(JudgeResult result)
    {
        switch (result)
        {
            case JudgeResult.Perfect:
                score += 100;
                combo++;
                Perfect++;

                if (combo >= 2)
                    ShowFeedback($"PERFECT! x{combo}", new Color(0.4f, 1f, 0.6f));
                else
                    ShowFeedback("PERFECT!", new Color(0.3f, 0.7f, 1f));
                break;

            case JudgeResult.Great:
                score += 70;
                combo = 0;
                Great++;
                ShowFeedback("GREAT!", new Color(1f, 0.85f, 0.4f));
                break;

            case JudgeResult.Good:
                score += 30;
                combo = 0;
                Good++;
                ShowFeedback("GOOD!", new Color(0.8f, 0.8f, 0.8f));
                break;

            case JudgeResult.Miss:
                combo = 0;
                Miss++;
                ShowFeedback("MISS!", new Color(1f, 0.3f, 0.3f));
                break;
        }

        scoreText.text = $"{score}";
        //comboText.text = $"Combo: x{combo}";

        // 🔥 Score에도 동일한 애니메이션 적용
        StopCoroutine("AnimateScore");
        StartCoroutine("AnimateScore");
    }

    public void JudgeMiss() => ApplyJudge(JudgeResult.Miss);

    void ShowFeedback(string msg, Color c)
    {
        feedbackText.text = msg;
        feedbackText.color = c;
        feedbackText.alpha = 1f;

        feedbackText.transform.localScale = originalScale * 0.85f;
        feedbackText.transform.localPosition = originalPosition;

        StopCoroutine("AnimateFeedback");
        StartCoroutine("AnimateFeedback");
    }

    // 🔥 기존 Feedback 애니메이션 그대로 유지
    System.Collections.IEnumerator AnimateFeedback()
    {
        float t = 0f;

        Vector3 startScale = originalScale * 0.85f;
        Vector3 midScale = originalScale * 1.20f;
        Vector3 endScale = originalScale;

        Vector3 startPos = originalPosition;
        Vector3 endPos = originalPosition + new Vector3(0, 45f, 0);

        while (t < 1f)
        {
            t += Time.deltaTime * 3f;

            if (t < 0.3f)
                feedbackText.transform.localScale = Vector3.Lerp(startScale, midScale, t / 0.3f);
            else
                feedbackText.transform.localScale = Vector3.Lerp(midScale, endScale, (t - 0.3f) / 0.7f);

            feedbackText.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            feedbackText.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }
    }

    // 🔥 ScoreText 전용 애니메이션
    IEnumerator AnimateScore()
    {
        float t = 0f;

        Vector3 startScale = scoreOriginalScale * 0.85f;
        Vector3 midScale   = scoreOriginalScale * 1.20f;
        Vector3 endScale   = scoreOriginalScale;

        Vector3 startPos = scoreOriginalPosition;
        Vector3 endPos   = scoreOriginalPosition + new Vector3(0, 45f, 0);

        Color startColor = new Color(76f/255f, 195f/255f, 255f/255f);

        while (t < 1f)
        {
            t += Time.deltaTime * 3f;

            if (t < 0.3f)
                scoreText.transform.localScale = Vector3.Lerp(startScale, midScale, t / 0.3f);
            else
                scoreText.transform.localScale = Vector3.Lerp(midScale, endScale, (t - 0.3f) / 0.7f);

            scoreText.transform.localPosition = Vector3.Lerp(startPos, endPos, t);

            scoreText.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, t));

            yield return null;
        }

        scoreText.transform.localScale = endScale;
        scoreText.transform.localPosition = scoreOriginalPosition;
        //scoreText.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        scoreText.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
    }
    

    public float Accuracy
    {
        get
        {
            int total = Perfect + Great + Good + Miss;
            if (total == 0) return 0f;
            return (Perfect + Great + Good) / (float)total * 100f;
        }
    }
}
