using UnityEngine;
using UnityEngine.UIElements;

public class Game1MenuController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    public UnityEngine.UI.Button hamburgerButton;

    private VisualElement root;
    private VisualElement menuRoot;

    private Button closeButton;
    private Button backButton;
    private Button exitStepButton;
    private Button settingsButton;

    private SliderInt syncSlider;
    private Button syncPlusButton;
    private Button syncMinusButton;
    private Label syncValueLabel;

    private NoteSpawner spawner;

    private void Awake()
    {
        root = uiDocument.rootVisualElement;
        menuRoot = root.Q<VisualElement>("MenuDialogRoot");

        closeButton = root.Q<Button>("MenuCloseButton");
        backButton  = root.Q<Button>("BackButton");
        exitStepButton = root.Q<Button>("ExitStepButton");
        settingsButton  = root.Q<Button>("SettingsButton");

        syncSlider     = root.Q<SliderInt>("SyncSlider");
        syncPlusButton = root.Q<Button>("SyncPlusButton");
        syncMinusButton = root.Q<Button>("SyncMinusButton");
        syncValueLabel = root.Q<Label>("SyncValueLabel");

        spawner = FindObjectOfType<NoteSpawner>();
        if (spawner == null)
            Debug.LogError("[Game1Menu] NoteSpawner not found!");

        // 초기 UI 값 로드
        int savedMs = Mathf.RoundToInt(GameSettings.AudioOffsetMs);
        savedMs = Mathf.Clamp(savedMs, syncSlider.lowValue, syncSlider.highValue);

        syncSlider.SetValueWithoutNotify(savedMs);
        ApplySyncFromSlider();    // userCalibSec 갱신

        // 버튼 연결
        closeButton.clicked += OnCloseMenu;
        backButton.clicked += OnCloseMenu;
        exitStepButton.clicked += OnExitStep;
        settingsButton.clicked += OnSettingsClicked;

        syncSlider.RegisterValueChangedCallback(evt => OnSyncChanged());
        syncPlusButton.clicked += () => ChangeSyncBy(+5);
        syncMinusButton.clicked += () => ChangeSyncBy(-5);

        if (hamburgerButton != null)
            hamburgerButton.onClick.AddListener(OnHamburgerClicked);

        HideMenuOnly();
    }

    // ================================
    // 메뉴 열기
    // ================================
    private void OnHamburgerClicked()
    {
        if (spawner == null)
            spawner = FindObjectOfType<NoteSpawner>();

        int savedMs = Mathf.RoundToInt(GameSettings.AudioOffsetMs);
        syncSlider.SetValueWithoutNotify(savedMs);
        syncValueLabel.text = $"{savedMs} ms";

        if (spawner != null)
        {
            spawner.userCalibSec = GameSettings.AudioOffsetSeconds;
            Debug.Log($"[Menu] OnHamburgerClicked: savedMs={savedMs}, applySec={GameSettings.AudioOffsetSeconds:F3}, spawner.userCalibSec={spawner.userCalibSec:F3}");
            PauseGame();
        }

        ShowMenuOnly();
    }

    private void OnCloseMenu()
    {
        HideMenuOnly();
        ResumeGame();
    }

    private void OnExitStep()
    {
        // ✅ TimeScale 복구 (pause 상태에서 나갔을 수 있으므로)
        Time.timeScale = 1f;
        Debug.Log("[Game1Menu] OnExitStep: timeScale reset to 1");
        
        SceneNavigator.Load("StepScene");
    }

    private void ShowMenuOnly()
    {
        menuRoot.style.display = DisplayStyle.Flex;
    }

    private void HideMenuOnly()
    {
        menuRoot.style.display = DisplayStyle.None;
    }

    // ================================
    // 오디오 싱크 처리
    // ================================
    private void OnSyncChanged()
    {
        ApplySyncFromSlider();
        SaveSync();
    }

    private void ChangeSyncBy(int delta)
    {
        int newValue = Mathf.Clamp(
            syncSlider.value + delta,
            syncSlider.lowValue,
            syncSlider.highValue
        );

        syncSlider.SetValueWithoutNotify(newValue);
        OnSyncChanged();
    }

    private void ApplySyncFromSlider()
    {
        int ms = syncSlider.value;
        syncValueLabel.text = $"{ms} ms";

        float sec = ms / 1000f;

        if (spawner != null)
        {
            spawner.userCalibSec = sec;

            // 🔥 DSP 시간 재정렬 추가
            spawner.RealignDSPTimeAfterOffsetChanged();

            Debug.Log($"[Menu] ApplySyncFromSlider: ms={ms}, sec={sec:F3}");
        }
    }

    private void SaveSync()
    {
        GameSettings.AudioOffsetMs = syncSlider.value;
    }

    // ================================
    // Pause / Resume (게임 1 DSP 구조)
    // ================================
    private void PauseGame()
    {
        if (spawner == null) return;
        spawner.PauseGame();
    }

    private void ResumeGame()
    {
        if (spawner == null) return;
        spawner.ResumeGame();
    }

    private void OnSettingsClicked()
    {
        SceneNavigator.Load("CalibrationScreen");
    }
}
