using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameOverController : MonoBehaviour
{
    private VisualElement root;
    private VisualElement panel;
    private Button retryBtn;
    private Button nextBtn;

    public System.Action OnRetry;
    public System.Action OnNextLesson;

    private NoteSpawner noteSpawner;

    private TopBarController topBar;

    [Header("SFX")]
    public AudioSource uiAudioSource;
    public AudioClip gameOverBgm;    

    void Awake()
    {
        var document = GetComponent<UIDocument>();
        root = document.rootVisualElement;
        panel = root.Q<VisualElement>("game-over-root");
        retryBtn = root.Q<Button>("retry-btn");
        nextBtn = root.Q<Button>("next-btn");

        noteSpawner = FindObjectOfType<NoteSpawner>();
        topBar = FindObjectOfType<TopBarController>();        

        if (retryBtn != null)
            retryBtn.clicked += OnRetryClicked;

        if (nextBtn != null)
            nextBtn.clicked += OnNextLessonClicked;

        Hide();
    }

    public void Show()
    {
        if (panel == null) return;
        panel.style.display = DisplayStyle.Flex;
        noteSpawner.PauseGame(); // 게임 일시정지

        // 🔥 게임오버 음악 재생
        if (uiAudioSource != null && gameOverBgm != null)
            uiAudioSource.PlayOneShot(gameOverBgm, 1f);
        
    }

    public void Hide()
    {
        if (panel == null) return;
        panel.style.display = DisplayStyle.None;
    }

    private void OnRetryClicked()
    {
        StopAllCoroutines();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }    

    private void OnNextLessonClicked()
    {
        Debug.Log("[GameOverController] Next Lesson clicked!");
        // 나중에 챕터 이동 로직 추가
    }    
}
