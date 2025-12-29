using UnityEngine;

public class MicPulseAnim : MonoBehaviour
{
    public float speed = 2.0f;     // 펄스 속도
    public float minScale = 0.9f;  // 최소 스케일
    public float maxScale = 1.1f;  // 최대 스케일
    public bool isActive = false;  // 녹음 중 여부

    private RectTransform rectTransform;
    private float baseScale;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        baseScale = rectTransform.localScale.x;
    }

    void Update()
    {
        if (!isActive) return;

        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * speed) + 1f) / 2f);
        rectTransform.localScale = Vector3.one * (scale * baseScale);
    }

    public void SetActive(bool active)
    {
        isActive = active;
        rectTransform.localScale = Vector3.one * baseScale;
    }
}
