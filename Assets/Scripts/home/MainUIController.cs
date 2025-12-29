using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum PageType
{
    Home,
    Review,
    Play,    // ⭐ 추가: 뮤직 플레이어
    Profile
}

public class MainUIController : MonoBehaviour
{
    // Singleton for easy access
    public static MainUIController Instance { get; private set; }

    [Header("Pages")]
    [SerializeField] private GameObject homePage;
    [SerializeField] private GameObject reviewPage;
    [SerializeField] private GameObject playPage;    // ⭐ 추가
    [SerializeField] private GameObject profilePage;

    [Header("UI")]
    [SerializeField] private HeaderUI headerUI;
    [SerializeField] private ScrollRect scrollRect;

    // 앱 실행 후 또는 다른 씬에서 돌아올 때 타겟 페이지 지정용
    public static PageType TargetPage = PageType.Home;

    // ⚠️ 초기값 중요: enum 기본값(Home)을 쓰면 안 됨
    private PageType currentPage = (PageType)(-1);

    private void Awake()
    {
        // Singleton 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Debug.Log("[MainUI] Awake()");
        Debug.Log($"[MainUI] Awake state → Home={SafeActive(homePage)}, Review={SafeActive(reviewPage)}, Play={SafeActive(playPage)}, Profile={SafeActive(profilePage)}");
    }

    private void Start()
    {
        Debug.Log("[MainUI] Start()");
        Debug.Log($"[MainUI] Start currentPage = {currentPage}, TargetPage = {TargetPage}");

        // 지정된 타겟 페이지로 시작 (기본값 Home)
        SwitchPage(TargetPage);
        
        // 사용 후 초기화 (다음 번엔 다시 Home으로 오도록)
        TargetPage = PageType.Home;
    }

    public void SwitchPage(PageType target)
    {

        if (currentPage == target)
        {
            Debug.Log("[MainUI] ❌ Early return (currentPage == target)");
            return;
        }

        currentPage = target;

        Debug.Log("[MainUI] Applying SetActive...");

        if (homePage != null) homePage.SetActive(target == PageType.Home);
        if (reviewPage != null) reviewPage.SetActive(target == PageType.Review);
        if (playPage != null) playPage.SetActive(target == PageType.Play);      // ⭐ 추가
        if (profilePage != null) profilePage.SetActive(target == PageType.Profile);

        Debug.Log(
            $"[MainUI] After SetActive → " +
            $"Home={SafeActive(homePage)}, " +
            $"Review={SafeActive(reviewPage)}, " +
            $"Play={SafeActive(playPage)}, " +           // ⭐ 추가
            $"Profile={SafeActive(profilePage)}"
        );

        ResetScroll();
        UpdateHeader(target);

        if (target == PageType.Home)
        {
            Debug.Log("[MainUI] Starting ShowHomePageDeferred()");
            StartCoroutine(ShowHomePageDeferred());
        }
    }

    private IEnumerator ShowHomePageDeferred()
    {
        Debug.Log("[MainUI] ShowHomePageDeferred() begin");
        yield return null;

        Debug.Log($"[MainUI] After yield → Home activeInHierarchy={SafeActive(homePage)}");

        if (homePage == null)
        {
            Debug.LogError("[MainUI] ❌ homePage is NULL");
            yield break;
        }

        var generator = homePage.GetComponentInChildren<LearningPathGenerator>(true);
        Debug.Log($"[MainUI] LearningPathGenerator found = {generator != null}");

        if (generator != null)
        {
            Debug.Log("[MainUI] Calling LearningPathGenerator.OnPageShow()");
            generator.OnPageShow();
        }
        else
        {
            Debug.LogError("[MainUI] ❌ LearningPathGenerator NOT FOUND under HomePage");
        }
    }

    private void ResetScroll()
    {
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            Debug.Log("[MainUI] Scroll reset to top");
        }
        else
        {
            Debug.LogWarning("[MainUI] ScrollRect is NULL");
        }
    }

    private void UpdateHeader(PageType page)
    {
        if (headerUI == null)
        {
            Debug.LogWarning("[MainUI] HeaderUI is NULL");
            return;
        }

        switch (page)
        {
            case PageType.Home:
                //headerUI.SetTitle(GetCurrentCourseTitle());
                break;
            case PageType.Review:
                headerUI.SetTitle("Review");
                break;
            case PageType.Play:                      // ⭐ 추가
                headerUI.SetTitle("Music Player");
                break;
            case PageType.Profile:
                headerUI.SetTitle("Profile");
                break;
        }

        Debug.Log($"[MainUI] Header updated for page={page}");
    }

    private string GetCurrentCourseTitle()
    {
        // 임시
        return "";
    }

    // Play 탭 전용 - 이제 페이지 전환 사용
    public void GoToPlayScene()
    {
        Debug.Log("[MainUI] GoToPlayScene() → SwitchPage(Play)");
        SwitchPage(PageType.Play);
    }

    // ===== Utility =====

    private bool SafeActive(GameObject go)
    {
        return go != null && go.activeInHierarchy;
    }
}
