using UnityEngine;
using UnityEngine.UIElements;

public class MenuDialogController : MonoBehaviour
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

    private void Awake()
    {
        root = uiDocument.rootVisualElement;
        menuRoot = root.Q<VisualElement>("MenuDialogRoot");

        closeButton = root.Q<Button>("MenuCloseButton");
        backButton = root.Q<Button>("BackButton");
        exitStepButton = root.Q<Button>("ExitStepButton");
        settingsButton = root.Q<Button>("SettingsButton");

        syncSlider = root.Q<SliderInt>("SyncSlider");
        syncPlusButton = root.Q<Button>("SyncPlusButton");
        syncMinusButton = root.Q<Button>("SyncMinusButton");
        syncValueLabel = root.Q<Label>("SyncValueLabel");

        // 🔥 Load offset (ms)
        int savedMs = Mathf.RoundToInt(GameSettings.AudioOffsetMs);
        savedMs = Mathf.Clamp(savedMs, syncSlider.lowValue, syncSlider.highValue);

        syncSlider.SetValueWithoutNotify(savedMs);
        ApplySyncFromSlider();

        closeButton.clicked += OnCloseMenu;
        backButton.clicked += OnCloseMenu;
        exitStepButton.clicked += OnExitStep;

        syncSlider.RegisterValueChangedCallback(evt => OnSyncChanged());
        syncPlusButton.clicked += () => ChangeSyncBy(+5);
        syncMinusButton.clicked += () => ChangeSyncBy(-5);
        settingsButton.clicked += OnSettingsClicked;

        hamburgerButton.onClick.AddListener(OnHamburgerClicked);

        HideMenuOnly();
    }

    private void OnHamburgerClicked()
    {
        // 1) 최신 오디오 오프셋 로드
        int savedMs = Mathf.RoundToInt(GameSettings.AudioOffsetMs);
        savedMs = Mathf.Clamp(savedMs, syncSlider.lowValue, syncSlider.highValue);

        syncSlider.SetValueWithoutNotify(savedMs);
        syncValueLabel.text = $"{savedMs} ms";

        
        if (RhythmDemoManager.Instance != null)
        {
            // 2) Apply to gameplay
            RhythmDemoManager.Instance.offsetSeconds = GameSettings.AudioOffsetSeconds;
            // 3) Pause + Show menu
            RhythmDemoManager.Instance.PauseGame();
        }            

        ShowMenuOnly();
    }

    private void OnCloseMenu()
    {
        HideMenuOnly();

        if (RhythmDemoManager.Instance != null)
            RhythmDemoManager.Instance.ResumeGame();
    }

    private void OnExitStep()
    {
        //HideMenuOnly();        
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

        if (RhythmDemoManager.Instance != null)
            RhythmDemoManager.Instance.offsetSeconds = ms / 1000f;
    }

    private void SaveSync()
    {
        GameSettings.AudioOffsetMs = syncSlider.value;
    }    

    private void OnSettingsClicked()
    {
        SceneNavigator.Load("CalibrationScreen");
    }
}
