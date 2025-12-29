using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : MaskableGraphic
{
    [SerializeField] private List<Vector2> points = new List<Vector2>();
    [SerializeField] private float lineThickness = 6f;
    [SerializeField] private Gradient lineGradient;

    private readonly List<float> segmentLengths = new List<float>();

    public float LineThickness
    {
        get => lineThickness;
        set
        {
            lineThickness = Mathf.Max(0.1f, value);
            SetVerticesDirty();
        }
    }

    public Gradient LineGradient
    {
        get => lineGradient;
        set
        {
            lineGradient = value;
            SetVerticesDirty();
        }
    }

    public IReadOnlyList<Vector2> Points => points;

    public void SetPoints(IEnumerable<Vector2> pts)
    {
        points.Clear();
        if (pts != null)
        {
            points.AddRange(pts);
        }

        SetVerticesDirty();
    }

    public void SetGradient(Gradient gradient)
    {
        lineGradient = gradient;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null || points.Count < 2)
        {
            return;
        }

        EnsureGradient();

        segmentLengths.Clear();
        float totalLength = 0f;
        for (int i = 0; i < points.Count - 1; i++)
        {
            float segmentLength = Vector2.Distance(points[i], points[i + 1]);
            segmentLengths.Add(segmentLength);
            totalLength += segmentLength;
        }

        float accumulatedLength = 0f;
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 start = points[i];
            Vector2 end = points[i + 1];

            float segmentLength = segmentLengths[i];
            if (segmentLength < 0.0001f)
            {
                accumulatedLength += segmentLength;
                continue;
            }

            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * (lineThickness * 0.5f);

            float t0 = totalLength > 0f ? accumulatedLength / totalLength : 0f;
            float t1 = totalLength > 0f ? (accumulatedLength + segmentLength) / totalLength : 1f;

            Color colorStart = lineGradient.Evaluate(t0) * color;
            Color colorEnd = lineGradient.Evaluate(t1) * color;

            var v1 = UIVertex.simpleVert;
            v1.color = colorStart;
            v1.position = start - normal;

            var v2 = UIVertex.simpleVert;
            v2.color = colorStart;
            v2.position = start + normal;

            var v3 = UIVertex.simpleVert;
            v3.color = colorEnd;
            v3.position = end + normal;

            var v4 = UIVertex.simpleVert;
            v4.color = colorEnd;
            v4.position = end - normal;

            vh.AddUIVertexQuad(new[] { v1, v2, v3, v4 });

            accumulatedLength += segmentLength;
        }
    }

    private void EnsureGradient()
    {
        if (lineGradient != null && lineGradient.colorKeys.Length > 0 && lineGradient.alphaKeys.Length > 0)
        {
            return;
        }

        lineGradient = new Gradient();
        lineGradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });
    }
}
