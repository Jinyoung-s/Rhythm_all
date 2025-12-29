using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ReviewPageController : MonoBehaviour
{
    private VisualElement root;
    
    // External References
    [SerializeField] private BottomMenuController bottomMenuController;
    [SerializeField] private HeaderUI headerUI;
    
    // Review Cards
    private VisualElement wordTestCard;
    private VisualElement wordListCard;
    private VisualElement dailySentencesCard;
    private VisualElement savedSentencesCard;

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // TTS Pre-warming: 첫 클릭 시 초기화 지연 방지를 위해 미리 생성
        var tts = VocabularyTTSManager.Instance;

        BindElements();
        RegisterEvents();
    }

    /// <summary>
    /// UXML 요소 바인딩
    /// </summary>
    private void BindElements()
    {
        wordTestCard = root.Q<VisualElement>("WordTestCard");
        wordListCard = root.Q<VisualElement>("WordListCard");
        dailySentencesCard = root.Q<VisualElement>("DailySentencesCard");
        savedSentencesCard = root.Q<VisualElement>("SavedSentencesCard");

        // 안전성 체크
        Debug.Assert(wordTestCard != null, "WordTestCard not found");
        Debug.Assert(wordListCard != null, "WordListCard not found");
        Debug.Assert(dailySentencesCard != null, "DailySentencesCard not found");
        Debug.Assert(savedSentencesCard != null, "SavedSentencesCard not found");
    }

    /// <summary>
    /// 클릭 이벤트 등록
    /// </summary>
    private void RegisterEvents()
    {
        wordTestCard?.RegisterCallback<ClickEvent>(_ => OnWordTestClicked());
        wordListCard?.RegisterCallback<ClickEvent>(_ => OnWordListClicked());
        dailySentencesCard?.RegisterCallback<ClickEvent>(_ => OnDailySentencesClicked());
        savedSentencesCard?.RegisterCallback<ClickEvent>(_ => OnSavedSentencesClicked());
    }

    /// <summary>
    /// Word Test 카드 클릭
    /// </summary>
    private void OnWordTestClicked()
    {
        Debug.Log("[ReviewPage] Word Test clicked - Preparing VocabularyTestScene");
        
        // VocabularyManager에서 테스트 단어 가져오기
        var testWords = VocabularyManager.Instance.GetTestCandidates(20);
        
        if (testWords == null || testWords.Count == 0)
        {
            // 테스트 후보가 없으면 모든 학습 단어 사용
            testWords = VocabularyManager.Instance.GetAllLearnedWords();
            Debug.Log($"[ReviewPage] Using all learned words: {testWords.Count} words");
        }
        else
        {
            Debug.Log($"[ReviewPage] Using test candidates: {testWords.Count} words");
        }
        
        if (testWords.Count == 0)
        {
            Debug.LogWarning("[ReviewPage] No words available for test! Please complete some steps first.");
            return;
        }
        
        // VocabularyTestContext에 단어 설정
        VocabularyTestContext.Set(testWords);
        
        // VocabularyTestScene 로드
        SceneNavigator.Load("VocabularyTestScene");
    }


    // Cached Controllers
    private VocabularyListViewController vocabListController;
    private WrongSentencesViewController wrongSentencesController;
    private SavedSentencesViewController savedSentencesController;

    /// <summary>
    /// Word List 카드 클릭
    /// </summary>
    private void OnWordListClicked()
    {
        Debug.Log("[ReviewPage] Word List clicked");
        
        if (vocabListController == null)
        {
            // 1. Load UXML
            VisualTreeAsset listAsset = Resources.Load<VisualTreeAsset>("UI/Vocabulary/VocabularyListView");
            if (listAsset == null)
            {
                Debug.LogError("[ReviewPage] VocabularyListView.uxml not found in Resources/UI/Vocabulary/");
                return;
            }

            // 2. Instantiate and Add to Root (Absolute Overlay)
            VisualElement listView = listAsset.Instantiate();
            listView.style.position = Position.Absolute;
            listView.style.left = 0;
            listView.style.top = 0;
            listView.style.right = 0;
            listView.style.bottom = 0;
            listView.pickingMode = PickingMode.Ignore; // Let children handle events, but overlay container handles clicks?
            // Actually the overlay inside UXML handles coverage.
            
            root.Add(listView);

            // 3. Setup Controller with external UI references
            vocabListController = gameObject.AddComponent<VocabularyListViewController>();
            
            // Get BottomMenu root element
            VisualElement bottomMenuRoot = null;
            if (bottomMenuController != null)
            {
                var bottomMenuDoc = bottomMenuController.GetComponent<UIDocument>();
                if (bottomMenuDoc != null)
                    bottomMenuRoot = bottomMenuDoc.rootVisualElement;
            }
            
            // Get ReviewTitle
            var reviewTitle = root.Q<Label>("ReviewTitle");
            
            vocabListController.Initialize(listView, bottomMenuRoot, reviewTitle, headerUI);
        }

        vocabListController.Show();
    }

    /// <summary>
    /// Wrong Sentences 카드 클릭 (Daily Sentences 대체)
    /// </summary>
    private void OnDailySentencesClicked()
    {
        Debug.Log("[ReviewPage] Wrong Sentences clicked");
        
        if (wrongSentencesController == null)
        {
            // 1. Load UXML
            VisualTreeAsset wrongAsset = Resources.Load<VisualTreeAsset>("UI/Sentence/WrongSentencesView");
            if (wrongAsset == null)
            {
                Debug.LogError("[ReviewPage] WrongSentencesView.uxml not found in Resources/UI/Sentence/");
                return;
            }

            // 2. Instantiate and Add to Root
            VisualElement wrongView = wrongAsset.Instantiate();
            wrongView.style.position = Position.Absolute;
            wrongView.style.left = 0;
            wrongView.style.top = 0;
            wrongView.style.right = 0;
            wrongView.style.bottom = 0;
            
            root.Add(wrongView);

            // 3. Setup Controller
            wrongSentencesController = gameObject.AddComponent<WrongSentencesViewController>();
            
            VisualElement bottomMenuRoot = null;
            if (bottomMenuController != null)
            {
                var bottomMenuDoc = bottomMenuController.GetComponent<UIDocument>();
                if (bottomMenuDoc != null)
                    bottomMenuRoot = bottomMenuDoc.rootVisualElement;
            }
            
            var reviewTitle = root.Q<Label>("ReviewTitle");
            
            wrongSentencesController.Initialize(wrongView, bottomMenuRoot, reviewTitle, headerUI);
        }

        wrongSentencesController.Show();
    }

    /// <summary>
    /// Saved Sentences 카드 클릭
    /// </summary>
    private void OnSavedSentencesClicked()
    {
        Debug.Log("[ReviewPage] Saved Sentences clicked");
        
        if (savedSentencesController == null)
        {
            // 1. Load UXML
            VisualTreeAsset savedAsset = Resources.Load<VisualTreeAsset>("UI/Sentence/SavedSentencesView");
            if (savedAsset == null)
            {
                Debug.LogError("[ReviewPage] SavedSentencesView.uxml not found in Resources/UI/Sentence/");
                return;
            }

            // 2. Instantiate and Add to Root
            VisualElement savedView = savedAsset.Instantiate();
            savedView.style.position = Position.Absolute;
            savedView.style.left = 0;
            savedView.style.top = 0;
            savedView.style.right = 0;
            savedView.style.bottom = 0;
            
            root.Add(savedView);

            // 3. Setup Controller
            savedSentencesController = gameObject.AddComponent<SavedSentencesViewController>();
            
            VisualElement bottomMenuRoot = null;
            if (bottomMenuController != null)
            {
                var bottomMenuDoc = bottomMenuController.GetComponent<UIDocument>();
                if (bottomMenuDoc != null)
                    bottomMenuRoot = bottomMenuDoc.rootVisualElement;
            }
            
            var reviewTitle = root.Q<Label>("ReviewTitle");
            
            savedSentencesController.Initialize(savedView, bottomMenuRoot, reviewTitle, headerUI);
        }

        savedSentencesController.Show();
    }
}
