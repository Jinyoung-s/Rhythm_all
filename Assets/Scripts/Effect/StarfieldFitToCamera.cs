using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class StarfieldFitToCamera : MonoBehaviour
{
    void Start() { Fit(); }
    void OnValidate() { if (!Application.isPlaying) Fit(); }

    void Fit()
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        var ps = GetComponent<ParticleSystem>();
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width * 1.2f, 0.1f, 0f);

        // 스폰 위치를 화면 위쪽으로
        transform.position = new Vector3(cam.transform.position.x,
                                         cam.transform.position.y + height * 0.55f,
                                         transform.position.z);
    }
}
