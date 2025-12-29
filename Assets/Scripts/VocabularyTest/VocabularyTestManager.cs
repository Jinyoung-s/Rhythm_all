using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// Vocabulary Test의 메인 컨트롤러
/// UI Toolkit과 연동하여 4가지 문제 타입을 처리합니다.
/// </summary>
public class VocabularyTestManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int totalQuestions = 10;
    [SerializeField] private bool autoPlayListenMode = true;

    // Core Dependencies
    private QuestionGenerator questionGenerator;
    private VocabularyManager vocabManager;
    private VocabularyTTSManager ttsManager;

    // Test State
    private List<VocabQuestion> questions;
    private int currentQuestionIndex = 0;
    private int correctCount = 0;
    private string userAnswer = "";
    private bool hasAnswered = false;

    // UI Elements
    private UIDocument uiDocument;
    private VisualElement root;

    // Header
    private Button backButton;
    private Button settingsButton;
    private Label progressLabel;
    private VisualElement progressBar;

    // Badge
    private Label typeBadge;

    // Question Modes
    private VisualElement listenMode;
    private VisualElement readingMode;
    private VisualElement typingMode;

    // Listen Mode
    private Button speakerButton;
    private Label listenInstruction;

    // Reading Mode
    private Label questionText;
    private Label instruction;

    // Typing Mode
    private Button typingSpeakerButton;
    private TextField typingInput;

    // Options
    private VisualElement optionsContainer;
    private Button[] optionButtons;

    // Feedback
    private VisualElement feedbackPanel;
    private Label feedbackIcon;
    private Label feedbackTitle;
    private Label wordMain;
    private Label wordPhonetic;
    private Label wordPOS;
    private Label wordMeaning;

    // Actions
    private Button submitButton;
    private Button nextButton;

    // =========================================================
    // Initialization
    // =========================================================

    void Start()
    {
        // 싱글톤 초기화
        vocabManager = VocabularyManager.Instance;
        ttsManager = VocabularyTTSManager.Instance;
        questionGenerator = new QuestionGenerator();

        // UI 초기화
        InitializeUI();

        // 테스트 시작
        StartTest();
    }

    private void InitializeUI()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            // UIDocument가 같은 GameObject에 없으면 씬에서 찾기
            uiDocument = FindObjectOfType<UIDocument>();
        }
        
        if (uiDocument == null)
        {
            Debug.LogError("[VocabularyTestManager] UIDocument not found!");
            return;
        }

        root = uiDocument.rootVisualElement;

        // Header Elements
        backButton = root.Q<Button>("BackButton");
        settingsButton = root.Q<Button>("SettingsButton");
        progressLabel = root.Q<Label>("ProgressLabel");
        progressBar = root.Q<VisualElement>("ProgressBar");

        // Badge
        typeBadge = root.Q<Label>("TypeBadge");

        // Question Modes
        listenMode = root.Q<VisualElement>("ListenMode");
        readingMode = root.Q<VisualElement>("ReadingMode");
        typingMode = root.Q<VisualElement>("TypingMode");

        // Listen Mode Elements
        speakerButton = root.Q<Button>("SpeakerButton");
        
        // Reading Mode Elements
        questionText = root.Q<Label>("QuestionText");
        instruction = root.Q<Label>("Instruction");

        // Typing Mode Elements
        typingSpeakerButton = root.Q<Button>("TypingSpeakerButton");
        typingInput = root.Q<TextField>("TypingInput");

        // Options
        optionsContainer = root.Q<VisualElement>("OptionsContainer");
        optionButtons = new Button[4];
        optionButtons[0] = root.Q<Button>("Option0");
        optionButtons[1] = root.Q<Button>("Option1");
        optionButtons[2] = root.Q<Button>("Option2");
        optionButtons[3] = root.Q<Button>("Option3");

        // Feedback
        feedbackPanel = root.Q<VisualElement>("FeedbackPanel");
        feedbackIcon = root.Q<Label>("FeedbackIcon");
        feedbackTitle = root.Q<Label>("FeedbackTitle");
        wordMain = root.Q<Label>("WordMain");
        wordPhonetic = root.Q<Label>("WordPhonetic");
        wordPOS = root.Q<Label>("WordPOS");
        wordMeaning = root.Q<Label>("WordMeaning");

        // Action Buttons
        submitButton = root.Q<Button>("SubmitButton");
        nextButton = root.Q<Button>("NextButton");

        // Event Bindings
        backButton?.RegisterCallback<ClickEvent>(evt => OnBackButtonClicked());
        settingsButton?.RegisterCallback<ClickEvent>(evt => OnSettingsButtonClicked());
        speakerButton?.RegisterCallback<ClickEvent>(evt => OnSpeakerButtonClicked());
        typingSpeakerButton?.RegisterCallback<ClickEvent>(evt => OnSpeakerButtonClicked());
        submitButton?.RegisterCallback<ClickEvent>(evt => OnSubmitButtonClicked());
        nextButton?.RegisterCallback<ClickEvent>(evt => OnNextButtonClicked());

        // Option buttons
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i; // Capture for closure
            optionButtons[i]?.RegisterCallback<ClickEvent>(evt => OnOptionClicked(index));
        }

        // Typing input
        typingInput?.RegisterCallback<ChangeEvent<string>>(evt => OnTypingInputChanged(evt.newValue));
    }

    // =========================================================
    // Test Flow
    // =========================================================

    private void StartTest()
    {
        // VocabularyTestContext에서 단어 목록 가져오기
        List<string> targetWords = VocabularyTestContext.TargetWords;

        if (targetWords == null || targetWords.Count == 0)
        {
            // Context가 비어있으면 학습한 모든 단어 사용
            targetWords = vocabManager.GetAllLearnedWords();
            Debug.Log("[VocabularyTestManager] Using all learned words.");
        }

        if (targetWords.Count == 0)
        {
            Debug.LogError("[VocabularyTestManager] No words available for test!");
            return;
        }

        // 문제 생성
        questions = questionGenerator.GenerateQuestions(targetWords, totalQuestions);

        if (questions.Count == 0)
        {
            Debug.LogError("[VocabularyTestManager] Failed to generate questions!");
            return;
        }

        Debug.Log($"[VocabularyTestManager] Generated {questions.Count} questions.");

        // 첫 문제 표시
        currentQuestionIndex = 0;
        correctCount = 0;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (currentQuestionIndex >= questions.Count)
        {
            ShowResults();
            return;
        }

        VocabQuestion question = questions[currentQuestionIndex];
        hasAnswered = false;
        userAnswer = "";

        // UI 초기화
        ResetUI();

        // Progress 업데이트
        UpdateProgress();

        // 문제 타입에 따라 UI 표시
        switch (question.type)
        {
            case VocabQuestionType.ListenAndChoose:
                ShowListenAndChooseQuestion(question);
                break;
            case VocabQuestionType.MeaningToWord:
                ShowMeaningToWordQuestion(question);
                break;
            case VocabQuestionType.WordToMeaning:
                ShowWordToMeaningQuestion(question);
                break;
            case VocabQuestionType.Typing:
                ShowTypingQuestion(question);
                break;
        }
    }

    // =========================================================
    // Question Type Display Methods
    // =========================================================

    private void ShowListenAndChooseQuestion(VocabQuestion question)
    {
        typeBadge.text = "듣고 고르기";

        // Listen Mode 표시
        listenMode.RemoveFromClassList("hidden");
        listenMode.AddToClassList("visible");
        readingMode.RemoveFromClassList("visible");
        readingMode.AddToClassList("hidden");
        typingMode.RemoveFromClassList("visible");
        typingMode.AddToClassList("hidden");

        // 선택지 표시 (단어들)
        ShowOptions(question.options);

        // 자동 재생
        if (autoPlayListenMode)
        {
            PlayWordAudio(question.word);
        }
    }

    private void ShowMeaningToWordQuestion(VocabQuestion question)
    {
        typeBadge.text = "뜻 → 단어";

        // Reading Mode 표시
        readingMode.RemoveFromClassList("hidden");
        readingMode.AddToClassList("visible");
        listenMode.RemoveFromClassList("visible");
        listenMode.AddToClassList("hidden");
        typingMode.RemoveFromClassList("visible");
        typingMode.AddToClassList("hidden");

        // 질문 텍스트: 한글 뜻
        WordInfo wordInfo = vocabManager.GetWordInfo(question.word);
        questionText.text = wordInfo?.meaning ?? "의미 없음";
        instruction.text = "올바른 영어 단어를 선택하세요";

        // 선택지 표시 (단어들)
        ShowOptions(question.options);
    }

    private void ShowWordToMeaningQuestion(VocabQuestion question)
    {
        typeBadge.text = "단어 → 뜻";

        // Reading Mode 표시
        readingMode.RemoveFromClassList("hidden");
        readingMode.AddToClassList("visible");
        listenMode.RemoveFromClassList("visible");
        listenMode.AddToClassList("hidden");
        typingMode.RemoveFromClassList("visible");
        typingMode.AddToClassList("hidden");

        // 질문 텍스트: 영어 단어
        questionText.text = question.word;
        instruction.text = "올바른 뜻을 선택하세요";

        // 선택지 표시 (한글 뜻들)
        ShowOptions(question.options);
    }

    private void ShowTypingQuestion(VocabQuestion question)
    {
        typeBadge.text = "듣고 쓰기";

        // Typing Mode 표시
        typingMode.RemoveFromClassList("hidden");
        typingMode.AddToClassList("visible");
        listenMode.AddToClassList("hidden");
        readingMode.AddToClassList("hidden");

        // 선택지 숨김
        optionsContainer.AddToClassList("hidden");

        // Input 초기화
        typingInput.value = "";
        typingInput.Focus();

        // 자동 재생
        PlayWordAudio(question.word);
    }

    // =========================================================
    // UI Update Methods
    // =========================================================

    private void UpdateProgress()
    {
        int current = currentQuestionIndex + 1;
        int total = questions.Count;

        progressLabel.text = $"{current} / {total}";

        // Progress bar width (0% ~ 100%)
        float percentage = (float)current / total * 100f;
        progressBar.style.width = Length.Percent(percentage);
    }

    private void ShowOptions(List<string> options)
    {
        optionsContainer.RemoveFromClassList("hidden");

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < options.Count)
            {
                optionButtons[i].text = options[i];
                optionButtons[i].style.display = DisplayStyle.Flex;
                optionButtons[i].SetEnabled(true);

                // 스타일 초기화
                optionButtons[i].RemoveFromClassList("selected");
                optionButtons[i].RemoveFromClassList("correct");
                optionButtons[i].RemoveFromClassList("incorrect");
            }
            else
            {
                optionButtons[i].style.display = DisplayStyle.None;
            }
        }
    }

    private void ResetUI()
    {
        // Feedback 숨김
        feedbackPanel.RemoveFromClassList("visible");
        feedbackPanel.AddToClassList("hidden");

        // Submit 버튼 표시, Next 버튼 숨김
        submitButton.RemoveFromClassList("hidden");
        submitButton.SetEnabled(false);
        nextButton.RemoveFromClassList("visible");
        nextButton.AddToClassList("hidden");

        // 모든 모드 숨김
        listenMode.RemoveFromClassList("visible");
        listenMode.AddToClassList("hidden");
        readingMode.RemoveFromClassList("visible");
        readingMode.AddToClassList("hidden");
        typingMode.RemoveFromClassList("visible");
        typingMode.AddToClassList("hidden");
    }

    // =========================================================
    // Event Handlers
    // =========================================================

    private void OnOptionClicked(int optionIndex)
    {
        if (hasAnswered)
            return;

        VocabQuestion question = questions[currentQuestionIndex];
        userAnswer = question.options[optionIndex];

        // 선택된 옵션 하이라이트
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].RemoveFromClassList("selected");
        }
        optionButtons[optionIndex].AddToClassList("selected");

        // Submit 버튼 활성화
        submitButton.SetEnabled(true);
    }

    private void OnTypingInputChanged(string value)
    {
        userAnswer = value.Trim();

        // Submit 버튼 활성화 조건
        submitButton.SetEnabled(!string.IsNullOrEmpty(userAnswer));
    }

    private void OnSpeakerButtonClicked()
    {
        VocabQuestion question = questions[currentQuestionIndex];
        PlayWordAudio(question.word);
    }

    private void OnSubmitButtonClicked()
    {
        if (hasAnswered || string.IsNullOrEmpty(userAnswer))
            return;

        VocabQuestion question = questions[currentQuestionIndex];
        bool isCorrect = CheckAnswer(question);

        if (isCorrect)
        {
            correctCount++;
        }

        // 결과 기록
        vocabManager.ApplyTestResult(question.word, isCorrect);

        // Feedback 표시
        ShowFeedback(question, isCorrect);

        hasAnswered = true;

        // 마지막 문제인 경우 결과 팝업 즉시 표시
        if (currentQuestionIndex == questions.Count - 1)
        {
            // 약간의 딜레이를 주어 피드백(정답/오답 색상 등)을 아주 잠깐이라도 볼 수 있게 함
            // 바로 보여달라고 하셨으니 바로 호출하지만, 필요하시다면 Invoke나 Coroutine으로 지연 가능합니다.
            ShowResults();
        }
    }

    private void OnNextButtonClicked()
    {
        currentQuestionIndex++;
        ShowQuestion();
    }

    private void OnBackButtonClicked()
    {
        Debug.Log("[VocabularyTestManager] Back button clicked.");
        
        // VocabularyTestContext 초기화
        VocabularyTestContext.Clear();
        
        // 이전 씬으로 돌아가기
        SceneNavigator.Back();
    }

    private void OnSettingsButtonClicked()
    {
        Debug.Log("[VocabularyTestManager] Settings button clicked.");
        // 설정 팝업 표시 (구현 필요)
    }

    // =========================================================
    // Answer Checking
    // =========================================================

    private bool CheckAnswer(VocabQuestion question)
    {
        switch (question.type)
        {
            case VocabQuestionType.ListenAndChoose:
            case VocabQuestionType.MeaningToWord:
                // 선택한 단어가 정답 단어와 일치하는지
                return userAnswer.Equals(question.word, System.StringComparison.OrdinalIgnoreCase);

            case VocabQuestionType.WordToMeaning:
                // 선택한 뜻이 정답 뜻과 일치하는지
                return userAnswer.Equals(question.correctAnswer, System.StringComparison.Ordinal);

            case VocabQuestionType.Typing:
                // 타이핑한 단어가 정답 단어와 일치하는지 (대소문자 무시)
                return userAnswer.Equals(question.word, System.StringComparison.OrdinalIgnoreCase);

            default:
                return false;
        }
    }

    // =========================================================
    // Feedback Display
    // =========================================================

    private void ShowFeedback(VocabQuestion question, bool isCorrect)
    {
        // Feedback panel 표시
        feedbackPanel.RemoveFromClassList("hidden");
        feedbackPanel.AddToClassList("visible");

        // 정답/오답 스타일 적용
        if (isCorrect)
        {
            feedbackPanel.RemoveFromClassList("incorrect");
            feedbackPanel.AddToClassList("correct");
            feedbackIcon.text = "✓";
            feedbackTitle.text = "정답입니다!";
        }
        else
        {
            feedbackPanel.RemoveFromClassList("correct");
            feedbackPanel.AddToClassList("incorrect");
            feedbackIcon.text = "✗";
            feedbackTitle.text = "오답입니다";
        }

        // 단어 정보 표시
        WordInfo wordInfo = vocabManager.GetWordInfo(question.word);
        if (wordInfo != null)
        {
            wordMain.text = wordInfo.word;
            wordPhonetic.text = ""; // 발음 기호는 추후 추가
            wordPOS.text = wordInfo.partOfSpeech ?? "";
            wordMeaning.text = wordInfo.meaning ?? "";
        }

        // 선택지 정답/오답 표시 (Multiple Choice일 경우)
        if (question.type != VocabQuestionType.Typing)
        {
            HighlightCorrectOption(question);
        }

        // 옵션 컨테이너 숨기기 (UI 깔끔하게)
        optionsContainer.AddToClassList("hidden");

        // 옵션 버튼 비활성화
        foreach (var btn in optionButtons)
        {
            btn.SetEnabled(false);
        }

        // Submit 버튼 숨기고 Next 버튼 표시
        submitButton.AddToClassList("hidden");

        // 마지막 문제가 아니면 다음 버튼 표시
        if (currentQuestionIndex < questions.Count - 1)
        {
            nextButton.RemoveFromClassList("hidden");
            nextButton.AddToClassList("visible");
            nextButton.text = "다음 문제 ▶";
        }
    }

    private void HighlightCorrectOption(VocabQuestion question)
    {
        for (int i = 0; i < question.options.Count; i++)
        {
            if (i == question.correctAnswerIndex)
            {
                // 정답 옵션
                optionButtons[i].AddToClassList("correct");
            }
            else if (question.options[i] == userAnswer)
            {
                // 사용자가 선택한 오답
                optionButtons[i].AddToClassList("incorrect");
            }
        }
    }

    // =========================================================
    // TTS Methods
    // =========================================================

    private void PlayWordAudio(string word)
    {
        if (ttsManager != null)
        {
            ttsManager.SpeakWord(word);
        }
        else
        {
            Debug.LogWarning("[VocabularyTestManager] TTS Manager not found.");
        }
    }

    // =========================================================
    // Results
    // =========================================================

    private void ShowResults()
    {
        // ⭐ 포인트 획득 로직 추가
        int earnedNotes = PointManager.CalculateVocabTestScore(correctCount, questions.Count, 0);
        if (earnedNotes > 0)
        {
            PointManager.Instance.AddNotes(
                earnedNotes,
                RhythmEnglish.Economy.PointSource.VocabularyTest,
                $"VocabTest - {correctCount}/{questions.Count} correct"
            );
        }

        string title = "테스트 완료!";
        string message = $"{questions.Count}문제 중 {correctCount}문제를 맞췄습니다!\n+{earnedNotes} Notes 획득!";

        // PopupManager를 사용하여 결과 표시
        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.ShowPopup(
                title,
                message,
                "메인으로",
                () => {
                    // MainMenuScene 로드 전 타겟 페이지 설정
                    MainUIController.TargetPage = PageType.Review;
                    SceneManager.LoadScene("MainMenuScene");
                }
            );
        }
        else
        {
            // Fallback: PopupManager가 없는 경우 바로 이동
            Debug.LogWarning("[VocabularyTestManager] PopupManager not found, skipping results popup.");
            MainUIController.TargetPage = PageType.Review;
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}
