using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.UIElements;

public class TopBarController : MonoBehaviour
{
    private Label lifeLabel;

    [Header("Life Settings")]
    [SerializeField] private int maxLife = 3;
    private int currentLife;

    public System.Action OnGameOver; // 라이프 0일 때 이벤트

    [SerializeField]
    private GameOverController gameOverController;

    [SerializeField]
    private NoteSpawner noteSpawner;



    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        lifeLabel = root.Q<Label>("life");

        root.Q<Button>("btn-back").clicked += () =>
        {
            // Go back to lesson selection
            SceneNavigator.Load("StepScene");
        };

        currentLife = maxLife;
        UpdateLife();
    }

    public void LoseLife(int count = 1)
    {
        currentLife = Mathf.Max(0, currentLife - count);
        UpdateLife();

        if (currentLife <= 0)
        {
            Debug.Log("[TopBarController] Life reached 0 → Game Over");

            // ✅ 게임 정지
            Time.timeScale = 0f;


            // ✅ NoteSpawner 쪽 Freeze로 전체 정지
            NoteSpawner.Freeze = true;
            NoteSpawner.FreezeTime = NoteSpawner.CurrentSongTime;

            if (noteSpawner != null && noteSpawner.musicSource != null)
            {
                noteSpawner.musicSource.Pause();
            }

            // ✅ GameOver UI 표시
            if (gameOverController != null)
            {
                gameOverController.Show();
            }
            else
            {
                Debug.LogWarning("[TopBarController] GameOverController not assigned!");
            }

            // ✅ 이벤트 발행 (다른 곳에서 필요할 때 대비)
            OnGameOver?.Invoke();
        }
    }

    public void AddLife(int count = 1)
    {
        currentLife = Mathf.Min(maxLife, currentLife + count);
        UpdateLife();
    }

    private void UpdateLife()
    {
        Debug.LogWarning("Updating life display!!!!!!!!!!!!!!!!: " + currentLife);


        if (lifeLabel == null)
        {
            Debug.LogError("[TopBarController] lifeLabel reference is missing!");
          return;  
        } 

        string hearts = "";
        for (int i = 0; i < currentLife; i++) hearts += "❤️";
        lifeLabel.text = hearts;
    }
}
