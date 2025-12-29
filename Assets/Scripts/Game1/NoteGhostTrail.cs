using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NoteGhostTrail : MonoBehaviour
{
    [Header("Prefab")]
    public Image ghostPrefab;        // 방금 만든 NoteGhostTemplate (Image)

    [Header("Pool / Spawn")]
    public int poolSize = 24;        // 미리 만들어 둘 잔상 개수
    public float spawnInterval = 0.05f; // 잔상 생성 간격(초)
    public float minDistance = 4f;   // 마지막 생성 위치와의 최소 이동거리(픽셀/유닛)

    [Header("Fade")]
    public float fadeDuration = 0.25f; // 사라지는 시간
    [Range(0f,1f)] public float startAlpha = 0.9f; // 시작 알파

    RectTransform _rect;
    Image _src;
    float _timer;
    Vector3 _lastSpawnPos;

    Queue<Image> _pool;
    readonly List<Image> _active = new List<Image>();

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _src  = GetComponent<Image>();

        if (_src == null) Debug.LogError("NoteGhostTrail: 같은 오브젝트에 Image가 필요합니다.");
        if (ghostPrefab == null) Debug.LogError("NoteGhostTrail: ghostPrefab(Image)을 할당하세요.");

        _pool = new Queue<Image>(poolSize);
        Prewarm();
        _lastSpawnPos = _rect.position;
    }

    void Prewarm()
    {
        Transform parent = transform.parent != null ? transform.parent : transform;
        for (int i = 0; i < poolSize; i++)
        {
            Image g = Instantiate(ghostPrefab, parent);
            g.raycastTarget = false;
            g.gameObject.SetActive(false);
            _pool.Enqueue(g);
        }
    }

    Image GetFromPool()
    {
        if (_pool.Count > 0) return _pool.Dequeue();
        Image g = Instantiate(ghostPrefab, transform.parent);
        g.raycastTarget = false;
        g.gameObject.SetActive(false);
        return g;
    }

    void ReturnToPool(Image g)
    {
        g.gameObject.SetActive(false);
        _active.Remove(g);
        _pool.Enqueue(g);
    }

    void Update()
    {
        _timer += Time.deltaTime;

        // 너무 안 움직일 땐 생성 안 함
        if ((_rect.position - _lastSpawnPos).sqrMagnitude < (minDistance * minDistance))
            return;

        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            SpawnGhost();
        }
    }

    void SpawnGhost()
    {
        Image g = GetFromPool();

        // 원본 Image의 외형 복사
        g.sprite = _src.sprite;
        g.type   = _src.type;
        g.pixelsPerUnitMultiplier = _src.pixelsPerUnitMultiplier;
        g.material = _src.material;

        // RectTransform 동기화
        RectTransform gr = g.rectTransform;
        gr.SetParent(transform.parent, worldPositionStays: false);
        gr.position   = _rect.position;
        gr.rotation   = _rect.rotation;
        gr.localScale = _rect.localScale;
        gr.sizeDelta  = _rect.sizeDelta;
        gr.pivot      = _rect.pivot;
        gr.anchorMin  = _rect.anchorMin;
        gr.anchorMax  = _rect.anchorMax;

        // 노트 뒤에 렌더되도록 형제 인덱스 조정
        int behind = Mathf.Max(0, transform.GetSiblingIndex() - 1);
        gr.SetSiblingIndex(behind);

        // 시작 색상(알파)
        Color c = _src.color; c.a = startAlpha;
        g.color = c;

        g.gameObject.SetActive(true);
        _active.Add(g);

        StartCoroutine(FadeAndRecycle(g));
        _lastSpawnPos = _rect.position;
    }

    IEnumerator FadeAndRecycle(Image g)
    {
        float t = 0f;
        Color c = g.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, 0f, t / fadeDuration);
            g.color = c;
            yield return null;
        }
        ReturnToPool(g);
    }

    // 필요 시 외부에서 호출: 잔상 모두 정리
    public void ResetTrail()
    {
        StopAllCoroutines();
        for (int i = 0; i < _active.Count; i++)
        {
            Image g = _active[i];
            if (g != null) { g.gameObject.SetActive(false); _pool.Enqueue(g); }
        }
        _active.Clear();
        _lastSpawnPos = _rect.position;
        _timer = 0f;
    }
}
