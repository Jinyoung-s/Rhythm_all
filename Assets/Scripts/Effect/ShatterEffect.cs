using UnityEngine;
using UnityEngine.UI;

public class ShatterEffect : MonoBehaviour
{
    public float moveSpeed = 200f;   // 튀는 속도
    public float rotationSpeed = 180f; // 회전 속도
    public float lifeTime = 0.5f;    // 몇 초 뒤 삭제

    private Vector2[] directions;

    private Image[] shardImages;


    void Awake()
    {
        // 자식에 붙은 모든 UI Image 수집 (비활성 포함)
        shardImages = GetComponentsInChildren<Image>(includeInactive: true);
    }

    void Start()
    {
        // 파편 개수만큼 랜덤 방향 생성
        int count = transform.childCount;
        directions = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform shard = transform.GetChild(i).GetComponent<RectTransform>();
            shard.anchoredPosition += directions[i] * moveSpeed * Time.deltaTime;
            shard.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }

    public void SetShardSprite(Sprite sprite)
    {
        if (sprite == null) return;

        // 혹시 Awake 전에 호출됐거나, 런타임에 자식이 붙었다면 한 번 더 수집
        if (shardImages == null || shardImages.Length == 0)
            shardImages = GetComponentsInChildren<Image>(includeInactive: true);

        foreach (var img in shardImages)
        {
            if (img == null) continue;
            img.sprite = sprite;
            // img.SetNativeSize(); // 필요하면 켜세요
        }
    }

}
