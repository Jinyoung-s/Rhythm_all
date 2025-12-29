using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UIElements;

public class NoteUI : MonoBehaviour, IPointerDownHandler
{
    [Header("Refs")]
    public TextMeshProUGUI wordText;
    public CountdownManager judgeManager;   // 기존 카운트다운 + 간이 판정 표시

    // SetWord로 주입
    float startTime;   // 이 시각에 타깃선 도달해야 함(초)
    float travelTime;
    float startY, targetY;
    AudioSource musicSource;

    [Header("Judge Windows (seconds)")]
    public float perfectWindow = 0.05f;
    public float greatWindow   = 0.10f;
    public float goodWindow    = 0.20f;
    [Tooltip("전체 판정 창에 곱해 느슨하게/타이트하게 조정")]
    public float leniency = 1.2f;

    [Header("Position Judge")]
    public float judgeRange = 30f;   // 타깃선 근처 허용 범위(px)

    [Header("Grace")]
    [Tooltip("타깃 도달 이후에도 잠깐 입력 허용(코요테 타임)")]
    public float lingerGrace = 0.15f;

    [Header("State")]
    public bool active = true;

    private bool inJudgeZone = false;
    private bool arrived     = false;
    private float arriveActualTime = -1f;

    [Header("SFX")]
    [Tooltip("퍼펙트 판정 시 재생할 효과음")]
    public AudioClip perfectSfx;

    [Header("SFX")]
    [Tooltip("실패 판정 시 재생할 효과음")]
    public AudioClip failSfx;

    [Tooltip("지정 시 여기서 OneShot으로 재생. 비어 있으면 카메라 위치에서 재생")]
    public AudioSource sfxSource;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("shatterPrefab")]
    public GameObject shatterPrefab;

    public string shardColorKey = "white";
   
    private TopBarController topBar;

    [Header("Hit Particle")]
    public GameObject hitParticlePrefab;

    // 🔥 게임2 스타일 판정/스코어 매니저 (선택적)
    private JudgementManager judgementManager;

    public void SetWord(string word, float _targetY, float _startTime,
                        float _travelTime, float _startY, AudioSource _musicSource)
    {
        wordText.text = word;
        targetY    = _targetY;
        startTime  = _startTime;
        travelTime = _travelTime;
        startY     = _startY;
        musicSource = _musicSource; // 전달만. 시간 계산은 NoteSpawner.CurrentSongTime 사용.
        arrived         = false;
        arriveActualTime = -1f;
        active          = true;

        // 글자 폭에 맞게 최소 폭 확보
        float padding  = 100f;
        float minWidth = 500f;
        float width    = Mathf.Max(wordText.preferredWidth + padding, minWidth);
        var rect = GetComponent<RectTransform>();
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        wordText.color = Color.white;
        transform.localScale = Vector3.one;

        // 기존처럼 CountdownManager 찾기 (카운트다운 + 간이 피드백용)
        if (judgeManager == null)
            judgeManager = FindObjectOfType<CountdownManager>();

        // 게임2 JudgementManager는 있으면만 사용 (없으면 무시)
        if (judgementManager == null && JudgementManager.Instance != null)
            judgementManager = JudgementManager.Instance;
    }

    void Update()
    {
        if (!active) return;

        // DSP 기반 현재 곡 시간(Freeze 고려)
        float now = NoteSpawner.Freeze ? NoteSpawner.FreezeTime : NoteSpawner.CurrentSongTime;

        // 이동/위치 업데이트
        float appearTime = startTime - travelTime;
        float t = Mathf.Clamp01((now - appearTime) / travelTime);
        var rect = GetComponent<RectTransform>();
        float y  = Mathf.Lerp(startY, targetY, t);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);

        // 프리즈 중이면 여기서 정지
        if (NoteSpawner.Freeze) return;

        // 공간 판정 영역 트래킹
        float dist = Mathf.Abs(y - targetY);
        if (!inJudgeZone && dist <= judgeRange) inJudgeZone = true;
        else if (inJudgeZone && dist > judgeRange) inJudgeZone = false;

        // 타깃 도달 기록 + 그레이스 타임 후 Miss
        if (!arrived && t >= 1.0f)
        {
            arrived = true;
            arriveActualTime = now;
            StartCoroutine(MissAfterGrace(now));
        }
    }

    IEnumerator MissAfterGrace(float reachedAt)
    {
        // lingerGrace 동안 입력 대기 (Freeze면 대기 유지)
        while (active && (NoteSpawner.Freeze || (NoteSpawner.CurrentSongTime - reachedAt) <= lingerGrace))
            yield return null;

        if (active) MissEffect();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!active) return;

        PlayHitParticle();

        float now = NoteSpawner.Freeze ? NoteSpawner.FreezeTime : NoteSpawner.CurrentSongTime;

        // 공간 조건(타깃선 근처에서만 유효)
        float y  = GetComponent<RectTransform>().anchoredPosition.y;
        float dy = Mathf.Abs(y - targetY);
        if (dy > judgeRange)
        {
            FailEffect();
            return;
        }

        // 시간 판정 (목표시각과의 절대 오차)
        float delta = Mathf.Abs(now - startTime);
        float p = perfectWindow * leniency;
        float g = greatWindow   * leniency;
        float b = goodWindow    * leniency;

        if (delta <= p)        PerfectEffect();
        else if (delta <= g)   ShowJudge("GREAT", Color.cyan);
        else if (delta <= b)   SuccessEffect();
        else                   FailEffect();
    }

    // ===== 공통: 게임2 Judge 호출 (실패해도 노트는 무조건 사라지게 try/catch) =====
    void ApplyGame2JudgeSafe(JudgementManager.JudgeResult result)
    {
        if (judgementManager == null) return;

        try
        {
            judgementManager.ApplyJudge(result);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NoteUI] JudgementManager.ApplyJudge 예외: {ex}");
        }
    }

    // ===== 연출/표시 (기존 로직 유지) =====

    void ShowJudge(string text, Color color)
    {
        active = false;
        wordText.color = color;
        enabled = false;

        Debug.Log($"[판정] {text}!");

        // 🔥 먼저 노트 제거 예약
        Destroy(gameObject, 0.2f);

        // 🔥 그 다음에 게임2 스타일 점수/애니메이션 (있을 경우만)
        ApplyGame2JudgeSafe(JudgementManager.JudgeResult.Good);
    }

    void PerfectEffect()
    {
        active = false;
        wordText.color = Color.purple;
        enabled = false;

        if (perfectSfx != null)
        {
            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(perfectSfx, sfxVolume);
            }
            else
            {
                var cam = Camera.main;
                Vector3 pos = cam != null ? cam.transform.position : Vector3.zero;
                AudioSource.PlayClipAtPoint(perfectSfx, pos, sfxVolume);
            }
        }


        BreakNote();
        Debug.Log("[판정] 퍼펙트!");

        // 🔥 노트 제거 예약을 먼저
        Destroy(gameObject, 0.2f);

        // 🔥 그 다음에 게임2 스타일
        ApplyGame2JudgeSafe(JudgementManager.JudgeResult.Perfect);
    }

    void SuccessEffect()
    {
        active = false;
        wordText.color = Color.green;
        enabled = false;

        Debug.Log("[판정] 굿!");

        Destroy(gameObject, 0.2f);
        ApplyGame2JudgeSafe(JudgementManager.JudgeResult.Good);
    }

    void FailEffect()
    {
        active = false;
        wordText.color = Color.red;
        enabled = false;

        if (failSfx != null)
        {
            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(failSfx, sfxVolume);
            }
            else
            {
                var cam = Camera.main;
                Vector3 pos = cam != null ? cam.transform.position : Vector3.zero;
                AudioSource.PlayClipAtPoint(failSfx, pos, sfxVolume);
            }
        }


        Debug.Log("[판정] 미스!");

        Destroy(gameObject, 0.2f);
        ApplyGame2JudgeSafe(JudgementManager.JudgeResult.Miss);
    }

    void MissEffect()
    {
        if (active)
        {
            active = false;
            wordText.color = Color.gray;
            enabled = false;

            /*
            if (failSfx != null)
            {
                if (sfxSource != null)
                {
                    sfxSource.PlayOneShot(failSfx, sfxVolume);
                }
                else
                {
                    var cam = Camera.main;
                    Vector3 pos = cam != null ? cam.transform.position : Vector3.zero;
                    AudioSource.PlayClipAtPoint(failSfx, pos, sfxVolume);
                }
            }
            */

            LifeManager.Instance.LoseLife();

            Debug.Log("[판정] 도달 후 미스!");

            Destroy(gameObject, 0.2f);
            ApplyGame2JudgeSafe(JudgementManager.JudgeResult.Miss);
        }
    }

    public void BreakNote()
    {
        GameObject effect = Instantiate(shatterPrefab, transform.position, Quaternion.identity, transform.parent);
        effect.GetComponent<RectTransform>().anchoredPosition = GetComponent<RectTransform>().anchoredPosition;

        var shatter = effect.GetComponent<ShatterEffect>();
        if (shatter != null)
        {
            var shardSprite = LoadShardSprite();
            if (shardSprite != null)
                shatter.SetShardSprite(shardSprite);
            else
                Debug.LogWarning($"[Shatter] shard sprite not found for key");
        }

        gameObject.SetActive(false); // 원래 노트 숨김
    }

    private Sprite LoadShardSprite()
    {
        string path = $"Crash/craxh_{shardColorKey}";
        return Resources.Load<Sprite>(path);
    }    
    
    public void SetTopBar(TopBarController controller)
    {
        if (controller == null)
        {
            Debug.LogWarning("[NoteUI] Tried to set TopBarController, but it's null.");
            return;
        }

        topBar = controller;
        Debug.Log("[NoteUI] TopBarController assigned successfully.");
    }    

    private void PlayHitParticle()
    {
        if (hitParticlePrefab == null) return;

        RectTransform rect = GetComponent<RectTransform>();

        Vector3 worldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rect,
            rect.position,
            Camera.main,
            out worldPos
        );

        GameObject fx = Instantiate(hitParticlePrefab, worldPos, Quaternion.identity);

        // UI 기준 적정 크기
        fx.transform.localScale = Vector3.one * 0.2f;
        Destroy(fx, 0.8f);
    }    
}
