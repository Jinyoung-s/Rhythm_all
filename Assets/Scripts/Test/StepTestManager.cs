using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine.SceneManagement;


public class StepTestManager : MonoBehaviour

{
    private DateTime? skipSpeakUntil = null;
    public VisualTreeAsset uxmlAsset;

    private List<TestItem> items = null;
    private int currentIndex = 0;

    private VisualElement root;
    private VisualElement _blanksContainer;
    private VisualElement _questionContainer;
    private VisualElement _optionsContainer;

    private TextField _typingInput;

    private Button _submitButton;
    private Button _playButton;
    private Label _questionLabel;

    private Button _nextQuestionButton;
    private Button _closeButton;

    private List<Label> _blankSlots = new List<Label>();
    private List<Button> _optionButtons = new List<Button>();
    private List<string> _answer = new List<string>();
    private List<Button> _usedButtons = new List<Button>();
    [SerializeField] private VisualTreeAsset retryModalUxml;
    [SerializeField] private StyleSheet extrasStyle;

    private VisualElement _overlay;
    private Button _retryBtnOnModal;
    private Button _backBtnOnModal;
    private Button _nextBtnOnModal;
    private Button _rejectBtnOnModal;

    private string chapterId;
    private string stepId;

    private AudioSource _audioSource;
    private AudioClip _currentClip;

    private Button _micButton;

    private AudioClip _recordedClip;

    private bool _isRecording = false;

    private AndroidSpeechBridge _speechBridge;

    private bool isQeuestionEnd = false;

    public AudioClip correctSound;

    JsonDataManager dataManager;

    // 라운드(회차) 진행 관리용
    private List<int> _currentLoopIndices;   // 이번 라운드에 풀 문제 인덱스들
    private int _loopPos;                    // 이번 라운드에서 진행 중인 위치
    private List<int> _nextLoopIndices;      // 이번 라운드 오답(다음 라운드에 다시 풀 목록)    
    private Label _modalMessageLabel; // 모달(오버레이) 텍스트 갱신용

    private ProgressBar _progressBar; //


    private void Awake()
    {
        var dataManagerInstance = GameDataManager.Instance;
        var fallbackStep = StepResourceResolver.CreateFallbackStep();
        var step = dataManagerInstance.CurrentStep ?? fallbackStep;
        if (dataManagerInstance.CurrentStep == null)
        {
            dataManagerInstance.CurrentStep = step;
        }

        dataManager = new JsonDataManager();
        _speechBridge = gameObject.AddComponent<AndroidSpeechBridge>();

        chapterId = string.IsNullOrEmpty(dataManagerInstance.CurrentChapterId)
            ? StepResourceResolver.GetFallbackChapterId()
            : dataManagerInstance.CurrentChapterId;
        dataManagerInstance.CurrentChapterId = chapterId;

        stepId = string.IsNullOrEmpty(step.id) ? fallbackStep.id : step.id;

        Debug.Log($"Current Chapter ID: {chapterId}, Current Step ID: {stepId}");

        TextAsset testTA = StepResourceResolver.LoadTestAsset(chapterId, step);
        if (testTA == null)
        {
            Debug.LogError($"[StepTestManager] Test JSON not found for {chapterId}/{stepId}.");
            return;
        }

        string testJson = testTA.text;
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 0f;

        try
        {
            var wrapped = JsonConvert.DeserializeObject<TestData>(testJson);
            items = (wrapped != null && wrapped.items != null && wrapped.items.Count > 0)
                ? wrapped.items
                : JsonConvert.DeserializeObject<List<TestItem>>(testJson);
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ JSON 역직렬화 실패: {ex.Message}");
            return;
        }

        if (items == null)
        {
            Debug.LogError("❌ 파싱된 TestItem 리스트가 null 입니다.");
            return;
        }

        foreach (var item in items)
        {
            string id = string.IsNullOrEmpty(item.id) ? "(no-id)" : item.id;
            string type = string.IsNullOrEmpty(item.type) ? "(no-type)" : item.type;
            Debug.Log($"✅ Test Item ID: {id}, Type: {type}");
        }

        root = GetComponent<UIDocument>().rootVisualElement;
        if (extrasStyle != null && !root.styleSheets.Contains(extrasStyle))
            root.styleSheets.Add(extrasStyle);

        // 캐싱
        _questionLabel = root.Q<Label>("QuestionLabel");
        _playButton = root.Q<Button>("PlayButton");
        _submitButton = root.Q<Button>("SubmitButton");
        _micButton = root.Q<Button>("MicButton");
        _blanksContainer = root.Q<VisualElement>("BlanksContainer");
        _questionContainer = root.Q<VisualElement>("QuestionContainer");
        _optionsContainer = root.Q<VisualElement>("OptionsContainer");
        _nextQuestionButton = root.Q<Button>("NextQuestionButton");

        _closeButton = root.Q<Button>("CloseButton");

        if (_closeButton != null)
        {
            _closeButton.clicked += OnCloseButtonClicked;
        }



        _progressBar = root.Q<ProgressBar>("ProgressBar");
        if (_progressBar != null)
        {
            _progressBar.lowValue = 0f;
            _progressBar.highValue = 100f;
            _progressBar.value = 0f;
        }
        else
        {
            Debug.LogError("❌ ProgressBar not found in UXML!");
        }        

        _typingInput = root.Q<TextField>("TypingInput");
        if (_typingInput != null)
        {
            _typingInput.AddToClassList("hidden"); // 기본은 숨김
        }

        _currentLoopIndices = Enumerable.Range(0, items.Count).ToList();
        _nextLoopIndices = new List<int>();

        // currentIndex를 시작 위치로 정렬
        _loopPos = Mathf.Clamp(currentIndex, 0, items.Count - 1);
        currentIndex = _currentLoopIndices[_loopPos];

        ShowQuestion(currentIndex);
    }

    /// <summary>
    /// 특정 인덱스의 문제를 세팅
    /// </summary>
    private void ShowQuestion(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            Debug.LogError($"❌ 잘못된 문제 인덱스: {index}");
            return;
        }

        var question = items[index];

        // 기본 UI 초기화
        _blankSlots.Clear();
        _optionButtons.Clear();
        _answer.Clear();
        _usedButtons.Clear();

        var optionsContainer = root.Q<VisualElement>("OptionsContainer");

        optionsContainer?.Clear();

        // 문제 타입별 UI 처리
        SetupQuestionUI(question);

        //createBlankLine(question);

        createWordBtn(question);

        // 제출 버튼 연결
        _submitButton.clicked -= OnSubmitClicked;
        _submitButton.clicked += OnSubmitClicked;

        _nextQuestionButton.clicked -= OnNextQuestionClicked;
        _nextQuestionButton.clicked += OnNextQuestionClicked;

        UpdateProgress();   
    }


    private void createBlankLine(TestItem question)
    {
        // 리스트 먼저 초기화
        _blankSlots.Clear();

        // 컨테이너 초기화
        _blanksContainer?.Clear();
        _blanksContainer.RemoveFromClassList("hidden");

        int correctOrderCount = question.correctOrder?.Count ?? 0;

        for (int i = 0; i < correctOrderCount; i++)
        {
            var blank = new Label("_______");
            blank.AddToClassList("question-blank");

            int idx = i; // 지역 변수 캡처 주의
            blank.RegisterCallback<ClickEvent>(_ => OnBlankClick(idx));

            _blankSlots.Add(blank);
            _blanksContainer?.Add(blank);
        }

        Debug.Log($"[createBlankLine] slots created = {_blankSlots.Count}");
    }

    private void createWordBtn(TestItem question)
    {
        // 단어 버튼 생성
        int wordCount = question.wordBank?.Count ?? 0;
        for (int i = 0; i < wordCount; i++)
        {
            string word = question.wordBank[i];
            var wordButton = new Button { text = word };
            wordButton.AddToClassList("word-button");

            Button capturedBtn = wordButton;
            string capturedWord = word;

            wordButton.RegisterCallback<ClickEvent>(_ => OnWordButtonClick(capturedWord, capturedBtn));
            _optionButtons.Add(wordButton);
            _optionsContainer?.Add(wordButton);
        }
    }


    /// <summary>
    /// 문제 타입별 UI (assemble / assemble_listen)
    /// </summary>
    private void SetupQuestionUI(TestItem question)
    {
        if (_questionLabel == null || _playButton == null)
        {
            Debug.LogError("❌ QuestionLabel 또는 PlayButton 을 찾을 수 없습니다.");
            return;
        }
        string mp3Key = "";
        if (question.media != null)
        {
            mp3Key = $"mp3/{chapterId}/test/{question.media.audioRef}";
            _currentClip = Resources.Load<AudioClip>(mp3Key);
        }

        if (question.type == "assemble_listen")
        {
            _questionLabel.AddToClassList("hidden");
            _playButton.RemoveFromClassList("hidden");


            Debug.Log($"clip null check: {_currentClip == null}");

            _playButton.clicked -= OnPlayButtonClicked;
            if (_currentClip != null)
            {
                _playButton.clicked += OnPlayButtonClicked;
            }
            else
            {
                Debug.LogWarning($"❌ AudioClip 로드 실패: {mp3Key}");
            }

            createBlankLine(question);
        }
        else if (question.type == "speak1" || question.type == "speak2")
        {
            // 재생버튼 노출, 빈칸 노출, 마이크 버튼 노출

            _questionLabel.text = question.prompt?.text ?? "";
            _playButton.RemoveFromClassList("hidden");
            _nextQuestionButton.RemoveFromClassList("hidden");

            createBlankLine(question);

            _playButton.clicked -= OnPlayButtonClicked;
            if (_currentClip != null)
            {
                _playButton.clicked += OnPlayButtonClicked;
            }

            if (_micButton != null)
            {
                _micButton.clicked -= OnMicClicked;
                _micButton.clicked += OnMicClicked;
            }
        }
        else // assemble
        {
            _questionLabel.RemoveFromClassList("hidden");
            _playButton.AddToClassList("hidden");
            _questionLabel.text = question.prompt?.text ?? "";

            createBlankLine(question);
        }


        if (question.type == "typing")
        {
            _questionLabel.text = question.prompt?.text ?? "";
            if (_typingInput != null)
            {
                _typingInput.RemoveFromClassList("hidden");
                _typingInput.value = "";
                
                // 커서 색상을 명시적으로 설정
                _typingInput.style.unityTextAlign = TextAnchor.MiddleCenter;
                
                // 지연 후 포커스를 주어 커서가 확실히 보이도록 함
                StartCoroutine(FocusInputFieldDelayed(_typingInput));
            }
            _optionsContainer.AddToClassList("hidden");
        }
        else
        {
            // typing이 아닐 때는 숨김
            _typingInput?.AddToClassList("hidden");
            _optionsContainer.RemoveFromClassList("hidden");
        }

        // 다음 문제 버튼은 speak 타입에서만 노출        
        if (IsSpeakType(question.type))
        {
            _nextQuestionButton.RemoveFromClassList("hidden");
            _micButton.RemoveFromClassList("hidden");

        }
        else
        {
            _nextQuestionButton.AddToClassList("hidden");
            _micButton.AddToClassList("hidden");

        }

    }

    private void OnPlayButtonClicked()
    {
        if (_currentClip != null)
        {
            Debug.Log($"▶ Playing {_currentClip.name} | Volume={_audioSource.volume} | Mute={_audioSource.mute}");
            _audioSource.volume = 1f;
            _audioSource.mute = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.PlayOneShot(_currentClip);
        }
    }

    private void OnWordButtonClick(string word, Button btn)
    {
        if (_answer.Count >= _blankSlots.Count) return;

        int targetIndex = _answer.Count;
        _blankSlots[targetIndex].text = word;
        _blankSlots[targetIndex].AddToClassList("answer-button");

        _answer.Add(word);
        _usedButtons.Add(btn);
        btn.SetEnabled(false);

        _blankSlots[targetIndex].RemoveFromClassList("correct");
        _blankSlots[targetIndex].RemoveFromClassList("incorrect");
    }

    private void OnBlankClick(int index)
    {
        if (index < 0 || index >= _blankSlots.Count) return;
        if (index >= _answer.Count) return;

        var btn = _usedButtons[index];
        if (btn != null) btn.SetEnabled(true);

        _answer.RemoveAt(index);
        _usedButtons.RemoveAt(index);

        RedrawBlanks();
    }

    private void RedrawBlanks()
    {
        for (int i = 0; i < _blankSlots.Count; i++)
        {
            var slot = _blankSlots[i];
            if (i < _answer.Count)
            {
                slot.text = _answer[i];
                slot.RemoveFromClassList("correct");
                slot.RemoveFromClassList("incorrect");
            }
            else
            {
                slot.text = "_______";
                slot.RemoveFromClassList("answer-button");
                slot.AddToClassList("question-blank");
                slot.RemoveFromClassList("correct");
                slot.RemoveFromClassList("incorrect");
            }
        }
    }

    private void OnSubmitClicked()
    {
        var correct = items[currentIndex].correctOrder ?? new List<string>();

        if (items[currentIndex].type == "typing" && _typingInput != null)
        {
            string typedText = _typingInput.value.Trim();
            _answer = typedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        bool lengthOk = _answer.Count == correct.Count;

        bool allOk = lengthOk;
        if (lengthOk)
        {
            for (int i = 0; i < correct.Count; i++)
            {
                bool match = string.Equals(_answer[i], correct[i], StringComparison.Ordinal);
                allOk &= match;

                _blankSlots[i].RemoveFromClassList("correct");
                _blankSlots[i].RemoveFromClassList("incorrect");
                _blankSlots[i].AddToClassList(match ? "correct" : "incorrect");
            }
        }
        else
        {
            for (int i = 0; i < _blankSlots.Count; i++)
            {
                _blankSlots[i].RemoveFromClassList("correct");
                _blankSlots[i].RemoveFromClassList("incorrect");
                if (i < _answer.Count)
                    _blankSlots[i].AddToClassList("incorrect");
            }
        }

        Debug.Log(allOk ? "✅ 정답!" : "❌ 오답!");

        // === SentenceManager 통합 ===
        var currentItem = items[currentIndex];
        string sentenceId = $"{chapterId}_{stepId}_{currentItem.id}";
        string sentence = string.Join(" ", correct);
        string translation = currentItem.prompt?.text ?? "";

        // 문장에 대한 시도 기록
        SentenceManager.Instance.RecordAttempt(sentenceId, sentence, translation, allOk);
        
        if (!allOk)
        {
            // 기존 저장 로직 유지
            UserData userData = dataManager.LoadUserData();
            var incorrectInfo = new IncorrectInformation
            {
                incorrectStep = GameDataManager.Instance.CurrentStep?.id ?? "unknown_step",
                incorrectChapter = GameDataManager.Instance.CurrentChapterId ?? "unknown_chapter",
            };
            incorrectInfo.incorrectIndexes.Add(currentIndex);
            incorrectInfo.correctCount = 0;
            userData.incorrectInfo.Add(incorrectInfo);
            dataManager.SaveUserData(userData);

            // 재도전 후보에 추가 (단, speak 타입이면 제외)
            var q = items[currentIndex];
            if (!IsSpeakType(q.type))
            {
                _nextLoopIndices.Add(currentIndex);
            }

            EnsureOverlayForNextQuestion();
            ShowOverlay();  // 모달에서 Next 누르면 다음 문제로 진행
        }
        else
        {
            // 정답이면 소리 재생 후 바로 다음으로
            PlayCorrectSound();
            GoNextInLoop();
        }
    }


    private void showNextQuestionOrEnd()
    {
        PlayCorrectSound();
        GoNextInLoop();
    }


    #region Overlay
    private void EnsureOverlayForNextQuestion()
    {
        // if (_overlay != null) return;
        if (retryModalUxml == null || root == null)
        {
            Debug.LogWarning("[RetryModal] UXML 또는 root가 비어있습니다.");
            return;
        }

        var modalTree = retryModalUxml.CloneTree();
        _overlay = modalTree.Q<VisualElement>("RetryOverlay");
        _nextBtnOnModal = modalTree.Q<Button>("NextButton");
        _rejectBtnOnModal = modalTree.Q<Button>("RejectButton");

        var title = modalTree.Q<Label>("ModalTitle");
        _modalMessageLabel = modalTree.Q<Label>("ModalMessage");
        _modalMessageLabel.RemoveFromClassList("hidden");

        LocalizationManager.SetLocale("ko-KR");
        title.text = LocalizationManager.Get("modal.title.incorrect");
        _nextBtnOnModal.text = LocalizationManager.Get("result.Next");
        _rejectBtnOnModal.AddToClassList("hidden");

        root.Add(_overlay);
        _overlay.StretchToParentSize();
        _overlay.pickingMode = PickingMode.Position;

        _overlay.style.position = Position.Absolute;
        _overlay.style.left = 0;
        _overlay.style.right = 0;
        _overlay.style.top = 0;
        _overlay.style.bottom = 0;
        _overlay.BringToFront();

        _nextBtnOnModal.clicked -= OnNextModalClicked;
        _nextBtnOnModal.clicked -= OnSkipSpeakClicked;
        _nextBtnOnModal.clicked += OnNextModalClicked;
    }

    private void EnsureOverlayForCanNotSpeak()
    {
        //if (_overlay != null) return;
        if (retryModalUxml == null || root == null)
        {
            Debug.LogWarning("[RetryModal] UXML 또는 root가 비어있습니다.");
            return;
        }        

        var modalTree = retryModalUxml.CloneTree();
        _overlay = modalTree.Q<VisualElement>("RetryOverlay");
        _nextBtnOnModal = modalTree.Q<Button>("NextButton");
        _rejectBtnOnModal = modalTree.Q<Button>("RejectButton");

        var title = modalTree.Q<Label>("ModalTitle");
        _modalMessageLabel = modalTree.Q<Label>("ModalMessage");
        _modalMessageLabel.AddToClassList("hidden");
        _rejectBtnOnModal.RemoveFromClassList("hidden");

        LocalizationManager.SetLocale("ko-KR");
        title.text = LocalizationManager.Get("modal.title.skipSpeak");
        _nextBtnOnModal.text = LocalizationManager.Get("button.yes");
        _rejectBtnOnModal.text = LocalizationManager.Get("button.no");


        root.Add(_overlay);
        _overlay.StretchToParentSize();
        _overlay.pickingMode = PickingMode.Position;

        _overlay.style.position = Position.Absolute;
        _overlay.style.left = 0;
        _overlay.style.right = 0;
        _overlay.style.top = 0;
        _overlay.style.bottom = 0;
        _overlay.BringToFront();

        _nextBtnOnModal.clicked -= OnNextModalClicked;
        _nextBtnOnModal.clicked -= OnSkipSpeakClicked;
        _nextBtnOnModal.clicked += OnSkipSpeakClicked;
        _rejectBtnOnModal.clicked -= OnBackModalClicked;
        
        _rejectBtnOnModal.clicked -= OnNextModalClicked;
        _rejectBtnOnModal.clicked += OnNextModalClicked;
    }

    private void UpdateOverlayTextForCurrent()
    {
        if (_modalMessageLabel == null) return;
        var question = items[currentIndex];
        _modalMessageLabel.text = question.correctOrder != null
            ? string.Join(" ", question.correctOrder)
            : "";
    }

    private void ShowOverlay()
    {
        UpdateOverlayTextForCurrent();
        _overlay?.RemoveFromClassList("hidden");
    }
    private void HideOverlay() => _overlay?.AddToClassList("hidden");

    private void OnRetryModalClicked()
    {
        HideOverlay();
        RetryCurrent();
    }

    private void OnNextModalClicked()
    {
        HideOverlay();
        GoNextInLoop();
    }

    private void OnBackModalClicked()
    {
        HideOverlay();
        ReturnToStepScene();
    }
    #endregion

    private void OnSkipSpeakClicked()
    {
    HideOverlay();
    skipSpeakUntil = DateTime.Now.AddMinutes(10);
    GoNextInLoop();
    }

    private void OnNextQuestionClicked()
    {

        Debug.Log("OnNextQuestionClicked called");
        Debug.Log("type::" + items[currentIndex].type);
        Debug.Log("IsSpeakType::" + IsSpeakType(items[currentIndex].type));
        if (IsSpeakType(items[currentIndex].type))
        {
            Debug.Log("Test" + IsSpeakType(items[currentIndex].type));
            EnsureOverlayForCanNotSpeak();
            ShowOverlay();
        }
        else
        {
            GoNextInLoop();
        }
    }

    private void RetryCurrent()
    {
        foreach (var b in _usedButtons) if (b != null) b.SetEnabled(true);
        _usedButtons.Clear();
        _answer.Clear();
        RedrawBlanks();
    }

    private void ReturnToStepScene()
    {
        try
        {
            var chapterId = GameDataManager.Instance?.CurrentChapterId ?? "beg_chap_001";
            StepSceneLoader.LoadStepScene(chapterId);
        }
        catch
        {
            Debug.Log("ℹ️ StepSceneLoader가 없으면 프로젝트에 맞는 씬 로더로 교체하세요.");
        }
    }


    private void OnMicClicked()
    {
        if (!MicPermissionHelper.HasMicPermission())
        {
            MicPermissionHelper.RequestMicPermission();
            return;
        }

        if (!_isRecording)
        {
            SetRecordingVisual(true);
            _speechBridge.StartListening();
            _isRecording = true;
        }
        else
        {
            SetRecordingVisual(false);
            _isRecording = false;
        }
    }

    // Android 콜백에서 이걸 호출



    public void OnSpeechRecognized(string recognizedText)
    {
        SetRecordingVisual(false);

        var correctOrder = items[currentIndex].correctOrder;
        if (correctOrder == null || _blankSlots.Count == 0) return;
        Debug.Log($"🎤 Recognized Text: {recognizedText}");
        Debug.Log($"blankSlotCount: {_blankSlots.Count}");

        // 1. 토큰화
        string[] tokens = recognizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // 2. 줄임말 정규화
        List<string> normalizedTokens = new List<string>();
        foreach (var token in tokens)
        {
            foreach (var expanded in ExpandContraction(token))
            {
                normalizedTokens.Add(expanded);
            }
        }

        Debug.Log($"Normalized Tokens: {string.Join(", ", normalizedTokens)}");

        // 3. 각 blank 슬롯 채우기
        for (int i = 0; i < correctOrder.Count; i++)
        {
            string expected = correctOrder[i];
            if (normalizedTokens.Any(t => string.Equals(t, expected, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.Log($"Filling slot {i} with '{expected}'");

                try
                {
                    _blankSlots[i].text = expected;
                    _blankSlots[i].RemoveFromClassList("question-blank");
                    _blankSlots[i].AddToClassList("answer-button");

                    Debug.Log("_blankSlots after filling:");
                    Debug.Log(_blankSlots);
                    Debug.Log("_blankSlots after filling:11111");

                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error logging _blankSlots: {ex.Message}");
                }



                if (_answer.Count <= i)
                {
                    _answer.Add(expected);
                    _usedButtons.Add(null); // STT에서 채운 경우 버튼 없음
                }
                else
                {
                    _answer[i] = expected;
                }
            }
        }
    }

    /// <summary>
    /// 축약형을 원형으로 확장
    /// "I'm" → ["I","am"], "can't" → ["cannot"]
    /// </summary>
    private IEnumerable<string> ExpandContraction(string word)
    {
        string lower = word.ToLowerInvariant();

        switch (lower)
        {
            case "i'm": return new[] { "I", "am" };
            case "you're": return new[] { "you", "are" };
            case "they're": return new[] { "they", "are" };
            case "we're": return new[] { "we", "are" };
            case "he's": return new[] { "he", "is" };
            case "she's": return new[] { "she", "is" };
            case "it's": return new[] { "it", "is" };
            case "don't": return new[] { "do", "not" };
            case "can't": return new[] { "cannot" };
            case "won't": return new[] { "will", "not" };
            // 필요한 축약형 더 추가 가능
            default: return new[] { word };
        }
    }

    private void SetRecordingVisual(bool recording)
    {
        if (_micButton == null) return;

        if (recording)
        {
            _micButton.text = "🎤 Recording...";
            _micButton.AddToClassList("recording");
        }
        else
        {
            _micButton.text = "🎤 Speak";
            _micButton.RemoveFromClassList("recording");
        }
    }

    private void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("❌ 마이크 장치가 없습니다.");
            return;
        }

        // 10초짜리 버퍼 생성, 루프 재생=false, 샘플레이트=44100
        _recordedClip = Microphone.Start(null, false, 10, 44100);
        Debug.Log("🎤 녹음 시작");
    }

    private void StopRecording()
    {
        if (_recordedClip == null) return;

        Microphone.End(null);
        Debug.Log("🛑 녹음 종료");

        // _recordedClip 안에 녹음된 오디오 데이터가 들어있음
        // → 여기서 STT API에 전송하는 단계가 필요
    }


    public void PlayCorrectSound()
    {
        AudioClip clip = Resources.Load<AudioClip>("mp3/effect/turning_page");
        if (clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }

    private void GoNextInLoop()
    {
        _loopPos++;

        while (_loopPos < _currentLoopIndices.Count)
        {
            int candidateIdx = _currentLoopIndices[_loopPos];
            var candidate = items[candidateIdx];
            if (!(skipSpeakUntil.HasValue && DateTime.Now < skipSpeakUntil.Value && IsSpeakType(candidate.type)))
            {
                currentIndex = candidateIdx;
                ShowQuestion(currentIndex);
                return;
            }
            _loopPos++;
        }

        // 라운드 종료 → 다음 라운드 준비
        if (_nextLoopIndices != null && _nextLoopIndices.Count > 0)
        {
            // 중복 제거
            _currentLoopIndices = _nextLoopIndices.Distinct().ToList();
            _nextLoopIndices = new List<int>();
            _loopPos = 0;

            currentIndex = _currentLoopIndices[_loopPos];
            ShowQuestion(currentIndex);
        }
        else
        {
            // 모든 문제 정답 → Step 씬으로 복귀
            isQeuestionEnd = true;

            if (_progressBar != null)
            {
                _progressBar.value = 100f;
            }

            // ⭐ 포인트 획득 로직 추가
            int correctCount = items.Count - _nextLoopIndices.Count;
            int earnedNotes = PointManager.CalculateStepTestScore(correctCount, items.Count);
            if (earnedNotes > 0)
            {
                PointManager.Instance.AddNotes(
                    earnedNotes,
                    RhythmEnglish.Economy.PointSource.StepTest,
                    $"StepTest - {correctCount}/{items.Count} correct - {chapterId}/{stepId}"
                );
            }

            // ⭐ 테스트 완료 기록 (Play 탭에서 곡 표시를 위함)
            string courseId = ProgressManager.Instance.CurrentCourseId;
            ProgressManager.Instance.MarkTestCompleted(courseId, chapterId, stepId);
            Debug.Log($"[StepTestManager] ✅ Test completed! Course: {courseId}, Chapter: {chapterId}, Step: {stepId}");

            // ⭐ 단어 저장 로직 추가
            SaveVocabularyData();

            ReturnToStepScene();
        }
    }       

    private void OnCloseButtonClicked()
    {
        EnsureOverlayForClose();
        ShowOverlay();
    }


    private void EnsureOverlayForClose()
    {
        if (retryModalUxml == null || root == null)
        {
            Debug.LogWarning("[CloseModal] UXML 또는 root가 비어있습니다.");
            return;
        }

        var modalTree = retryModalUxml.CloneTree();
        _overlay = modalTree.Q<VisualElement>("RetryOverlay");
        _nextBtnOnModal = modalTree.Q<Button>("NextButton");
        _rejectBtnOnModal = modalTree.Q<Button>("RejectButton");

        var title = modalTree.Q<Label>("ModalTitle");
        _modalMessageLabel = modalTree.Q<Label>("ModalMessage");
        
        // ⭐ 이 부분 수정: 정답 텍스트를 숨김
        _modalMessageLabel.AddToClassList("hidden");
        _rejectBtnOnModal.RemoveFromClassList("hidden");

        LocalizationManager.SetLocale("ko-KR");
        title.text = "테스트를 종료하시겠습니까?";
        _nextBtnOnModal.text = LocalizationManager.Get("button.yes");
        _rejectBtnOnModal.text = LocalizationManager.Get("button.no");

        root.Add(_overlay);
        _overlay.StretchToParentSize();
        _overlay.pickingMode = PickingMode.Position;

        _overlay.style.position = Position.Absolute;
        _overlay.style.left = 0;
        _overlay.style.right = 0;
        _overlay.style.top = 0;
        _overlay.style.bottom = 0;
        _overlay.BringToFront();

        _nextBtnOnModal.clicked -= OnConfirmCloseClicked;
        _nextBtnOnModal.clicked += OnConfirmCloseClicked;
        _rejectBtnOnModal.clicked -= OnCancelCloseClicked;
        _rejectBtnOnModal.clicked += OnCancelCloseClicked;
    }


    private void OnConfirmCloseClicked()
    {
        HideOverlay();
        UnityEngine.SceneManagement.SceneManager.LoadScene("StepScene");
    }

    private void OnCancelCloseClicked()
    {
        HideOverlay();
        // 모달만 닫고 테스트 계속 진행
    }



    private void UpdateProgress()
    {
        if (_progressBar == null)
        {
            Debug.LogWarning("⚠️ ProgressBar is null");
            return;
        }
        
        // 전체 문제 대비 현재 진행률 계산
        float progress = ((_loopPos + 1) / (float)_currentLoopIndices.Count) * 100f;
        _progressBar.value = progress;
        
        Debug.Log($"✅ Progress Updated: {progress}% (_loopPos={_loopPos}, total={_currentLoopIndices.Count})");
    }

    private bool IsSpeakType(string t)
    => !string.IsNullOrEmpty(t) && t.IndexOf("speak", StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>
    /// 테스트 완료 시 학습한 단어를 VocabularyManager에 저장
    /// </summary>
    private void SaveVocabularyData()
    {
        if (string.IsNullOrEmpty(chapterId) || string.IsNullOrEmpty(stepId))
        {
            Debug.LogWarning("[StepTestManager] Cannot save vocabulary: chapterId or stepId is empty");
            return;
        }

        try
        {
            VocabularyManager.Instance.RegisterStepCompletion(chapterId, stepId);
            Debug.Log($"[StepTestManager] ✅ Vocabulary saved for {chapterId}/{stepId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[StepTestManager] ❌ Failed to save vocabulary: {ex.Message}");
        }
    }

    /// <summary>
    /// TextField에 지연 후 포커스를 주어 커서가 확실히 표시되도록 함
    /// </summary>
    private System.Collections.IEnumerator FocusInputFieldDelayed(TextField textField)
    {
        // 1프레임 대기
        yield return null;
        
        // 포커스 설정
        if (textField != null)
        {
            textField.Focus();
            
            // 추가로 한 프레임 더 대기 후 재포커스 (모바일 키보드 대응)
            yield return null;
            textField.Focus();
        }
    }
}
