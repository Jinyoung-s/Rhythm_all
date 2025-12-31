using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class StepListLoader : MonoBehaviour
{
    [SerializeField] public string chapterId = "foun_chap_002";
    [SerializeField] public string stepFolder = "steps";
    [SerializeField] public LearnPopupController popupController;

    private VisualElement root;
    private VisualTreeAsset cardTemplate;
    private List<LearnStepData> learnSteps;
    private GameDataManager dataManager;

    private UIDocument uiDocument;

    void Awake()
    {
        Debug.Log($"[StepList] PanelSettings = {GetComponent<UIDocument>().panelSettings}");
        dataManager = GameDataManager.Instance;

        CurriculumRepository.Configure(stepFolder: stepFolder);

        var selectedChapter = StepSceneLoader.SelectedChapterId;
        if (!string.IsNullOrEmpty(selectedChapter))
        {
            chapterId = selectedChapter;
        }
        else if (string.IsNullOrEmpty(chapterId))
        {
            chapterId = StepResourceResolver.GetFallbackChapterId();
        }

        uiDocument = GetComponent<UIDocument>();
        
        root = uiDocument.rootVisualElement;

        ApplySafeArea();
        var scroll = root.Q<ScrollView>("cardListRoot");
        if (scroll != null)
        {
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        cardTemplate = Resources.Load<VisualTreeAsset>("UI/Step/CardTemplate");
        if (cardTemplate == null)
        {
            Debug.LogError("[StepListLoader] Resources/UI/CardTemplate.uxml not found.");
            return;
        }

        // Back button setup
        Button backButton = root.Q<Button>("BackButton");
        if (backButton != null)
        {
            backButton.clicked += () =>
            {
                SceneManager.LoadScene("MainMenuScene");
            };
        }

        //titleLabel.text = $"Chapter: {chapterId}";    
        Label titleLabel = root.Q<Label>("title");
        if (titleLabel != null)
        {
            if (CurriculumRepository.TryGetChapter(chapterId, out var chapter))
            {
                titleLabel.text = chapter.Name;
            }
        }

        LoadStepsAndGenerateCards();
    }

    private void LoadStepsAndGenerateCards()
    {
        if (!CurriculumRepository.TryGetChapter(chapterId, out var chapter))
        {
            Debug.LogError($"[StepListLoader] Chapter '{chapterId}' not found in CurriculumRepository.");
            return;
        }

        var steps = chapter.Steps;
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning($"[StepListLoader] Chapter '{chapterId}' has no steps.");
        }

        ScrollView scroll = root.Q<ScrollView>("cardListRoot");
        if (scroll == null)
        {
            Debug.LogError("[StepListLoader] Could not find ScrollView 'cardListRoot' in UXML.");
            return;
        }

        scroll.Clear();

        string courseId = ResolveCourseId();
        var progress = ProgressManager.Instance;

        foreach (StepData step in steps)
        {
            TemplateContainer card = cardTemplate.Instantiate();

            Label titleLabel = card.Q<Label>("title");
            if (titleLabel != null)
            {
                titleLabel.text = step.title;
            }

            Image cover = card.Q<Image>("cover");
            if (cover != null)
            {
                string imagePath = $"Covers/{chapterId}/{step.id}";

                Sprite sprite = Resources.Load<Sprite>(imagePath);
                if (sprite != null)
                {
                    cover.sprite = sprite;
                    cover.scaleMode = ScaleMode.ScaleAndCrop;
                }
                else
                {
                    Debug.LogWarning($"[StepListLoader] Cover image not found at path: Resources/{imagePath}");
                }
            }

            if (!step.unlocked)
            {
                card.AddToClassList("locked");
            }

            var stepContext = step;

            void PersistContext()
            {
                dataManager.SetContext(courseId, chapterId, stepContext);

                // ✅ UserProgressManager 제거 → ProgressManager로 통합
                ProgressManager.Instance.SetCurrent(courseId, chapterId, stepContext.id);
            }

            Button learnButton = card.Q<Button>("learnButton");
            if (learnButton != null)
            {
                learnButton.clicked += () =>
                {
                    Debug.Log($"[StepListLoader] Learn button clicked for step: {stepContext.id}");
                    
                    if (popupController == null)
                    {
                        Debug.LogError($"[StepListLoader] PopupController is NULL! Cannot show popup.");
                        return;
                    }
                    
                    PersistContext();
                    var learnDataList = StepResourceResolver.LoadLearnAsset(chapterId, stepContext);
                    
                    if (learnDataList == null)
                    {
                        Debug.LogError($"[StepListLoader] Learn data is NULL for step: {stepContext.id}");
                        return;
                    }
                    
                    if (learnDataList.steps == null || learnDataList.steps.Count == 0)
                    {
                        Debug.LogError($"[StepListLoader] Learn steps are NULL or empty for step: {stepContext.id}");
                        return;
                    }
                    
                    Debug.Log($"[StepListLoader] Showing popup with {learnDataList.steps.Count} learn steps");
                    popupController.Show(learnDataList.steps);
                };
            }

            Button singAlongButton = card.Q<Button>("singAlongButton");
            if (singAlongButton != null)
            {
                singAlongButton.clicked += () =>
                {
                    PersistContext();                    
                    SceneNavigator.Load("SingAlongScene");
                };
            }

            Button playButton1 = card.Q<Button>("playButton1");
            if (playButton1 != null)
            {
                playButton1.clicked += () =>
                {
                    PersistContext();                    
                    SceneNavigator.Load("GameScene");
                };
            }

            Button playButton2 = card.Q<Button>("playButton2");
            if (playButton2 != null)
            {
                playButton2.clicked += () =>
                {
                    PersistContext();                    
                    SceneNavigator.Load("Game1Scene");
                };
            }

            Button testButton = card.Q<Button>("testButton");
            if (testButton != null)
            {
                testButton.clicked += () =>
                {
                    PersistContext();
                    SceneNavigator.Load("TestScene");
                };
            }

            bool isCurrentStep =
                progress.CurrentCourseId == courseId &&
                progress.CurrentChapterId == chapterId &&
                progress.CurrentStepId == stepContext.id;

            if (isCurrentStep)
            {
                var cardMain = card.Q<VisualElement>("cardRoot");
                cardMain.AddToClassList("current-step");
            }

            scroll.Add(card);
        }
    }

    private string ResolveCourseId()
    {
        string courseId = dataManager.CurrentCourseId;

        // ✅ ProgressManager 기반 복원
        if (string.IsNullOrEmpty(courseId))
        {
            courseId = ProgressManager.Instance.CurrentCourseId;
        }

        if (string.IsNullOrEmpty(courseId) && CurriculumRepository.TryGetChapter(chapterId, out var chapter))
        {
            courseId = chapter.Course.Id;
        }

        if (string.IsNullOrEmpty(courseId))
        {
            var firstCourse = CurriculumRepository.GetFirstCourseOrDefault();
            if (firstCourse != null)
            {
                courseId = firstCourse.Id;
            }
        }

        if (string.IsNullOrEmpty(courseId))
        {
            courseId = "beginner_01";
        }

        dataManager.CurrentCourseId = courseId;
        return courseId;
    }

    void ApplySafeArea()
    {
        VisualElement root = uiDocument.rootVisualElement;

        Rect safe = Screen.safeArea;

        float topPadding = safe.yMin;
        float bottomPadding = Screen.height - safe.yMax;

        // 좌/우 노치는 거의 없음 (필요하면 동일 방식으로 계산)
        float leftPadding = safe.xMin;
        float rightPadding = Screen.width - safe.xMax;

        root.style.paddingTop = topPadding;
        root.style.paddingBottom = bottomPadding;
        root.style.paddingLeft = leftPadding;
        root.style.paddingRight = rightPadding;
    }
}
