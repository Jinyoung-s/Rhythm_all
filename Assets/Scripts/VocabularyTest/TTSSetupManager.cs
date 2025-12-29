using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// 앱 최초 실행 시 TTS 품질을 체크하고 Google TTS 설치를 권장합니다.
/// </summary>
public class TTSSetupManager : MonoBehaviour
{
    [SerializeField] private UIDocument setupUIDocument;
    
    private VisualElement root;
    private VisualElement setupPanel;
    private Label messageLabel;
    private Label titleLabel;
    private Button installButton;
    private Button skipButton;
    
    private GoogleTTSChecker ttsChecker;
    
    private const string PREF_KEY_SETUP_COMPLETE = "TTS_Setup_Complete";
    private const string PREF_KEY_SETUP_SKIPPED = "TTS_Setup_Skipped";
    private const string GOOGLE_TTS_PLAY_STORE_URL = 
        "https://play.google.com/store/apps/details?id=com.google.android.tts";

    void Start()
    {
        ttsChecker = gameObject.AddComponent<GoogleTTSChecker>();
        
        // 최초 실행 여부 확인
        if (!PlayerPrefs.HasKey(PREF_KEY_SETUP_COMPLETE))
        {
            CheckAndPromptTTSSetup();
        }
    }

    void CheckAndPromptTTSSetup()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android에서만 체크
        if (!ttsChecker.IsHighQualityEnglishAvailable())
        {
            ShowTTSSetupPrompt();
        }
        else
        {
            CompleteTTSSetup();
        }
#else
        // iOS나 다른 플랫폼은 바로 완료
        CompleteTTSSetup();
#endif
    }

    void ShowTTSSetupPrompt()
    {
        if (setupUIDocument == null)
        {
            Debug.LogWarning("[TTSSetup] setupUIDocument is not assigned. Skipping TTS setup prompt.");
            CompleteTTSSetup();
            return;
        }

        root = setupUIDocument.rootVisualElement;
        setupPanel = root.Q<VisualElement>("SetupPanel");
        titleLabel = root.Q<Label>("ModalTitle");
        messageLabel = root.Q<Label>("MessageLabel");
        installButton = root.Q<Button>("InstallButton");
        skipButton = root.Q<Button>("SkipButton");

        if (setupPanel == null)
        {
            Debug.LogWarning("[TTSSetup] SetupPanel not found in UXML. Skipping TTS setup prompt.");
            CompleteTTSSetup();
            return;
        }

        titleLabel.text = "음성 품질 최적화";
        messageLabel.text = 
            "더 나은 학습 경험을 위해\n" +
            "Google 음성 엔진 설치를 권장합니다.\n\n" +
            "고품질 영어 발음으로 단어를 학습하세요!";
        
        installButton.text = "설치하러 가기";
        skipButton.text = "나중에";
        
        installButton.clicked += OnInstallClicked;
        skipButton.clicked += OnSkipClicked;
        
        setupPanel.RemoveFromClassList("hidden");
    }

    void OnInstallClicked()
    {
        OpenGoogleTTSInstallPage();
        
        // 사용자가 설치 후 돌아왔을 때 재체크
        StartCoroutine(RecheckAfterDelay());
    }

    void OnSkipClicked()
    {
        if (setupPanel != null)
            setupPanel.AddToClassList("hidden");
        
        // "나중에" 선택 시 다음 실행 시 다시 표시
        PlayerPrefs.SetInt(PREF_KEY_SETUP_SKIPPED, 1);
        PlayerPrefs.Save();
        
        Debug.Log("[TTSSetup] User skipped TTS setup. Will prompt again next time.");
    }

    void CompleteTTSSetup()
    {
        PlayerPrefs.SetInt(PREF_KEY_SETUP_COMPLETE, 1);
        PlayerPrefs.Save();
        
        if (setupPanel != null)
            setupPanel.AddToClassList("hidden");
        
        Debug.Log("[TTSSetup] TTS setup completed.");
    }

    IEnumerator RecheckAfterDelay()
    {
        // 사용자가 앱으로 돌아올 때까지 대기
        yield return new WaitForSeconds(2f);
        
        if (ttsChecker.IsHighQualityEnglishAvailable())
        {
            if (messageLabel != null)
            {
                messageLabel.text = "✅ 설치 완료!\n고품질 음성을 사용할 수 있습니다.";
            }
            
            yield return new WaitForSeconds(2f);
            CompleteTTSSetup();
        }
    }

    void OpenGoogleTTSInstallPage()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[TTSSetup] Opening Google TTS Play Store page...");
        Application.OpenURL(GOOGLE_TTS_PLAY_STORE_URL);
#else
        Debug.LogWarning("[TTSSetup] OpenURL only works on Android devices.");
#endif
    }

    /// <summary>
    /// 설정 메뉴에서 호출: TTS 설정을 초기화하고 다시 프롬프트 표시
    /// </summary>
    public void ResetTTSSetup()
    {
        PlayerPrefs.DeleteKey(PREF_KEY_SETUP_COMPLETE);
        PlayerPrefs.DeleteKey(PREF_KEY_SETUP_SKIPPED);
        PlayerPrefs.Save();
        
        CheckAndPromptTTSSetup();
    }
}
