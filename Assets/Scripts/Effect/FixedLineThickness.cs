using UnityEngine;

public class FixedLineThickness : MonoBehaviour
{
    public float screenPixels = 4f; // 실제 화면 픽셀 두께
    Canvas canvas; RectTransform rt;

    void Awake(){ rt = GetComponent<RectTransform>(); canvas = GetComponentInParent<Canvas>(); }
    void OnEnable(){ Apply(); }
    void OnRectTransformDimensionsChange(){ Apply(); }

    void Apply(){
        if (!rt || !canvas) return;
        var s = rt.sizeDelta; s.y = screenPixels / canvas.scaleFactor; rt.sizeDelta = s;
    }
}
