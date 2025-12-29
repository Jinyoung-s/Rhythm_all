using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LifeManager : MonoBehaviour
{
    public static LifeManager Instance;

    [Header("Heart Images (Left → Right)")]
    public Image[] hearts;   // 5개

    [Header("Sprites")]
    public Sprite fullHeart;
    public Sprite brokenHeart;
    public Sprite emptyHeart;

    [Header("Effects")]
    public RectTransform heartsPanel; // 패널 전체 흔들기용

    [SerializeField]
    private GameOverController gameOverController;

    private int heartIndex;   // 현재 조작할 하트 인덱스
    private int lifeStep = 0; // 현재 라이프 단위 (0~9)

    private const int MaxLifeStep = 10; // 5 hearts × 2 steps

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ResetHearts();
    }

    public void ResetHearts()
    {
        lifeStep = 0;
        heartIndex = hearts.Length - 1;

        foreach (var h in hearts)
        {
            h.sprite = fullHeart;
            h.color = Color.white;
            h.rectTransform.localScale = Vector3.one;
        }
    }

    public void LoseLife()
    {
        if (lifeStep >= MaxLifeStep)
            return;

        lifeStep++;

        ApplyHeartChange();

        StartCoroutine(ShakePanel());

        if (lifeStep >= MaxLifeStep)
        {
            GameOver();
        }
    }

    private void ApplyHeartChange()
    {
        if (heartIndex < 0) return;

        Image img = hearts[heartIndex];

        if (img.sprite == fullHeart)
        {
            // Full → Broken
            StartCoroutine(PunchScale(img.rectTransform));
            StartCoroutine(Flash(img));

            img.sprite = brokenHeart;
        }
        else if (img.sprite == brokenHeart)
        {
            // Broken → Empty
            StartCoroutine(FadeToEmpty(img));
            heartIndex--;
        }
    }

    // =======================
    //  EFFECTS (Script Only)
    // =======================

    IEnumerator PunchScale(RectTransform rt)
    {
        Vector3 big = Vector3.one * 1.3f;
        Vector3 small = Vector3.one;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 12f;
            rt.localScale = Vector3.Lerp(big, small, t);
            yield return null;
        }
        rt.localScale = small;
    }

    IEnumerator Flash(Image img)
    {
        Color original = img.color;

        img.color = Color.white;
        yield return new WaitForSeconds(0.05f);
        img.color = original;
    }

    IEnumerator FadeToEmpty(Image img)
    {
        Color c = img.color;

        // Fade Out
        for (float a = 1f; a >= 0; a -= Time.deltaTime * 6f)
        {
            img.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }

        img.sprite = emptyHeart;

        // Fade In
        for (float a = 0; a <= 1f; a += Time.deltaTime * 6f)
        {
            img.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
    }

    IEnumerator ShakePanel()
    {
        if (heartsPanel == null) yield break;

        Vector2 original = heartsPanel.anchoredPosition;

        float strength = 10f;
        float duration = 0.05f;

        // 왔다갔다 2회
        for (int i = 0; i < 4; i++)
        {
            heartsPanel.anchoredPosition =
                original + new Vector2((i % 2 == 0 ? strength : -strength), 0);
            yield return new WaitForSeconds(duration);
        }

        heartsPanel.anchoredPosition = original;
    }

    private void GameOver()
    {
        Debug.Log("GAME OVER!");

        if (gameOverController != null)
        {
            gameOverController.Show();
        }
    }
}
