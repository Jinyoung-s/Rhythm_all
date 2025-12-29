using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class NoteController : MonoBehaviour, IPointerDownHandler
{
    private RectTransform rect;
    private float startTime;
    private float endTime;
    private float speed;
    private float targetY;
    private bool judged;

    private float logTick; 
    private bool loggedNearLine = false;

    public float DebugSpeed => speed;

    [Header("Hit Particle")]
    public GameObject hitParticlePrefab;    

    public void Initialize(float start, float end, float fallSpeed, float targetLineY)
    {
        rect = GetComponent<RectTransform>();
        startTime = start;
        endTime = end;
        speed = fallSpeed;
        targetY = targetLineY;
    }

    void Update()
    {
        if (RhythmDemoManager.Instance.isGamePaused)
        return; 

        if (rect == null) return;

        // 위치 이동
        Vector3 pos = rect.localPosition;
        pos.y -= speed * Time.deltaTime;

        // 1px 스냅
        pos.y = Mathf.Round(pos.y);
        rect.localPosition = pos;

        float currentY = pos.y;

        float missWindow = 0.35f;

        float audioT = RhythmDemoManager.Instance.AudioTime;
        float delta = audioT - startTime;

        // 노트가 너무 늦게 지나갔을 경우 MISS
        if (!judged && delta > missWindow)
        {
            JudgementManager.Instance.ApplyJudge(JudgementManager.JudgeResult.Miss);
            judged = true;
            Destroy(gameObject);
        }

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (judged) return;

        float audioT = RhythmDemoManager.Instance.AudioTime;
        float delta = Mathf.Abs(audioT - startTime);
        var result = JudgementManager.Instance.GetJudge(delta);

        JudgementManager.Instance.ApplyJudge(result);
        judged = true;

        Destroy(gameObject, 0.05f);
        PlayHitParticle();
    }

    public void PlayTouchEffect()
    {
        StartCoroutine(TouchEffectCoroutine());
    }

    IEnumerator TouchEffectCoroutine()
    {
        var img = GetComponent<UnityEngine.UI.Image>();
        var startColor = img.color;
        var glowColor = startColor * 1.5f;
        glowColor.a = 1f;

        Vector3 baseScale = transform.localScale;
        Vector3 upScale = baseScale * 1.15f;

        float t = 0;
        float duration = 0.12f;

        // up scale + glow
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            transform.localScale = Vector3.Lerp(baseScale, upScale, lerp);
            img.color = Color.Lerp(startColor, glowColor, lerp);

            yield return null;
        }

        // revert
        t = 0;
        duration = 0.10f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            transform.localScale = Vector3.Lerp(upScale, baseScale, lerp);
            img.color = Color.Lerp(glowColor, startColor, lerp);

            yield return null;
        }

        transform.localScale = baseScale;
        img.color = startColor;
    }

    private void PlayHitParticle()
    {
        if (hitParticlePrefab == null) return;

        RectTransform noteRect = GetComponent<RectTransform>();
        RectTransform canvasRect = noteRect.GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        // 파티클 UI 오브젝트 생성 (Canvas 밑으로)
        GameObject fx = Instantiate(hitParticlePrefab, canvasRect);
        RectTransform fxRect = fx.GetComponent<RectTransform>();

        // NoteRect의 화면 위치를 Canvas 좌표로 변환
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(null, noteRect.position),
            null,
            out anchoredPos
        );

        // UI 파티클 위치 설정
        fxRect.anchoredPosition = anchoredPos;

        // UI 파티클 스케일 고정
        fxRect.localScale = Vector3.one * 0.7f;  // 필요시 조정

        Destroy(fx, 0.5f);
    }

}
