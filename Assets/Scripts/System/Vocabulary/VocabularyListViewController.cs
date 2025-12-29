using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controls the "My Vocabulary" List Popup.
/// Handles data loading, list generation, and user interactions (TTS, Mystery Check).
/// </summary>
public class VocabularyListViewController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement overlay;
    
    // UI References
    private Label totalCountLabel;
    private Label masteredCountLabel;
    private ScrollView wordListScroll;
    private Button closeButton;
    private Toggle showMeaningsToggle;
    private Toggle hideMasteredToggle;
    
    // Pagination UI
    private Button prevPageButton;
    private Button nextPageButton;
    private Label pageInfoLabel;
    
    // External UI Elements (to hide/show)
    private VisualElement bottomMenu;
    private VisualElement reviewHeader;
    private HeaderUI headerUI;
    
    // Template
    private VisualTreeAsset itemTemplate;
    
    // State
    private List<UserWordData> currentData;
    private bool showMeanings = true;
    private bool hideMastered = false;
    
    // Pagination State
    private const int ITEMS_PER_PAGE = 15; // Mobile-friendly count
    private int currentPage = 0;
    private int totalPages = 1;

    // Singleton access or simple component lookup
    // Since this is likely instantiated by ReviewPageController, we can just be a component.

    private void Awake()
    {
        // If attached to the same object as UIDocument, valid.
        // But typically this script might be separate. 
        // We will assume SetVisualElement is called or we find it.
    }

    /// <summary>
    /// Initialize with the specific View Root (Overlay) and external UI elements
    /// </summary>
    public void Initialize(VisualElement viewRoot, VisualElement bottomMenuRoot, VisualElement reviewTitleElement, HeaderUI mainHeaderUI)
    {
        this.root = viewRoot;
        this.overlay = root.Q<VisualElement>("Overlay");
        
        // Store external UI references
        this.bottomMenu = bottomMenuRoot;
        this.reviewHeader = reviewTitleElement;
        this.headerUI = mainHeaderUI;
        
        // Find UI components
        totalCountLabel = root.Q<Label>("TotalCountLabel");
        masteredCountLabel = root.Q<Label>("MasteredCountLabel");
        wordListScroll = root.Q<ScrollView>("WordListScroll");
        closeButton = root.Q<Button>("CloseButton");
        showMeaningsToggle = root.Q<Toggle>("ShowMeaningsToggle");
        hideMasteredToggle = root.Q<Toggle>("HideMasteredToggle");
        
        // Pagination UI
        prevPageButton = root.Q<Button>("PrevPageButton");
        nextPageButton = root.Q<Button>("NextPageButton");
        pageInfoLabel = root.Q<Label>("PageInfoLabel");

        // Load Template
        itemTemplate = Resources.Load<VisualTreeAsset>("UI/Vocabulary/VocabularyListItem");

        // Bind Events
        closeButton?.RegisterCallback<ClickEvent>(evt => Hide());
        
        showMeaningsToggle?.RegisterCallback<ChangeEvent<bool>>(evt => 
        {
            showMeanings = evt.newValue;
            RefreshMeaningsVisibility();
        });
        
        hideMasteredToggle?.RegisterCallback<ChangeEvent<bool>>(evt =>
        {
            hideMastered = evt.newValue;
            
            // Reset to first page when filter changes
            currentPage = 0;
            
            // Recalculate total pages based on filtered data
            int filteredCount = hideMastered 
                ? currentData.Count(w => w.memoryScore < 100) 
                : currentData.Count;
            totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredCount / ITEMS_PER_PAGE));
            
            UpdatePaginationUI();
            RenderList(); // Re-render with filter
        });
        
        // Pagination Events
        prevPageButton?.RegisterCallback<ClickEvent>(evt => GoToPreviousPage());
        nextPageButton?.RegisterCallback<ClickEvent>(evt => GoToNextPage());
        
        // Debug log for external elements
        if (bottomMenu != null)
            Debug.Log("[VocabularyList] BottomMenu reference received");
        else
            Debug.LogWarning("[VocabularyList] BottomMenu reference is null");
            
        if (reviewHeader != null)
            Debug.Log("[VocabularyList] ReviewTitle reference received");
        else
            Debug.LogWarning("[VocabularyList] ReviewTitle reference is null");
            
        if (headerUI != null)
            Debug.Log("[VocabularyList] HeaderUI reference received");
        else
            Debug.LogWarning("[VocabularyList] HeaderUI reference is null");

        // Hide Scroller Bars intentionally for mobile feel
        if (wordListScroll != null)
        {
            wordListScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            wordListScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            wordListScroll.mode = ScrollViewMode.Vertical;
            // Elastic scroll effect if supported
            wordListScroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
        }

        // Hide initially
        overlay.AddToClassList("hidden");
        overlay.style.display = DisplayStyle.None;
    }

    public void Show()
    {
        if (overlay == null) return;
        
        // Hide bottom menu and review header for fullscreen modal
        if (bottomMenu != null)
            bottomMenu.style.display = DisplayStyle.None;
        if (reviewHeader != null)
            reviewHeader.style.display = DisplayStyle.None;
        if (headerUI != null)
            headerUI.gameObject.SetActive(false);

        overlay.style.display = DisplayStyle.Flex;
        // Small delay for fade-in if needed, using CSS transition
        overlay.AddToClassList("visible");
        
        LoadData();
    }

    public void Hide()
    {
        if (overlay == null) return;
        
        overlay.RemoveFromClassList("visible");
        overlay.style.display = DisplayStyle.None;
        
        // Restore bottom menu and review header
        if (bottomMenu != null)
            bottomMenu.style.display = DisplayStyle.Flex;
        if (reviewHeader != null)
            reviewHeader.style.display = DisplayStyle.Flex;
        if (headerUI != null)
            headerUI.gameObject.SetActive(true);
    }

    private void LoadData()
    {
        if (VocabularyManager.Instance == null) return;

        currentData = VocabularyManager.Instance.GetAllUserWordData();
        
        // Sort: Mastered last, then alphabetical? Or newly learned first.
        // Let's do: Not Mastered First, then Alphabetical.
        currentData.Sort((a, b) => 
        {
            bool aMastered = a.memoryScore >= 100;
            bool bMastered = b.memoryScore >= 100;
            
            if (aMastered != bMastered) return aMastered.CompareTo(bMastered); // False comes first (0 vs 1)
            return string.Compare(a.word, b.word);
        });

        // Reset to first page when loading new data
        currentPage = 0;
        totalPages = Mathf.Max(1, Mathf.CeilToInt((float)currentData.Count / ITEMS_PER_PAGE));
        
        UpdateStats();
        UpdatePaginationUI();
        RenderList();
    }

    private void UpdateStats()
    {
        if (currentData == null) return;
        
        int total = currentData.Count;
        int mastered = currentData.Count(w => w.memoryScore >= 100);

        if (totalCountLabel != null) totalCountLabel.text = $"Total: {total}";
        if (masteredCountLabel != null) masteredCountLabel.text = $"Mastered: {mastered}";
    }

    private void RenderList()
    {
        if (wordListScroll == null || itemTemplate == null) return;

        wordListScroll.Clear();
        
        // IMPORTANT: Filter data FIRST before pagination
        List<UserWordData> filteredData = currentData;
        if (hideMastered)
        {
            filteredData = currentData.Where(w => w.memoryScore < 100).ToList();
        }
        
        // Calculate pagination range on FILTERED data
        int startIndex = currentPage * ITEMS_PER_PAGE;
        int endIndex = Mathf.Min(startIndex + ITEMS_PER_PAGE, filteredData.Count);
        
        // Render only current page items from filtered data
        for (int i = startIndex; i < endIndex; i++)
        {
            var userWord = filteredData[i];
            
            VisualElement card = itemTemplate.Instantiate();
            VisualElement cardRoot = card.Q<VisualElement>("CardRoot");
            
            // Get Word Info from Master
            WordInfo info = VocabularyManager.Instance.GetWordInfo(userWord.word);
            string meaningText = info != null ? info.meaning : "???";

            // Bind Data
            Label wordLabel = card.Q<Label>("WordLabel");
            Label meaningLabel = card.Q<Label>("MeaningLabel");
            Button speakerBtn = card.Q<Button>("SpeakerButton");
            Button checkBtn = card.Q<Button>("MasterCheckButton");
            VisualElement statusIndicator = card.Q<VisualElement>("StatusIndicator");

            if (wordLabel != null) wordLabel.text = userWord.word;
            
            if (meaningLabel != null)
            {
                meaningLabel.text = meaningText;
                meaningLabel.style.visibility = showMeanings ? Visibility.Visible : Visibility.Hidden;
            }

            // Status Logic
            bool isMastered = userWord.memoryScore >= 100;
            if (isMastered)
            {
                cardRoot.AddToClassList("mastered");
                if (checkBtn != null) checkBtn.text = "✔";
            }
            else
            {
                cardRoot.RemoveFromClassList("mastered");
                if (checkBtn != null) checkBtn.text = "✔";
            }

            // Events
            speakerBtn.clicked += () => 
            {
                if (VocabularyTTSManager.Instance != null)
                    VocabularyTTSManager.Instance.SpeakWord(userWord.word);
            };

            checkBtn.clicked += () =>
        {
            // Read current state from data (not captured variable!)
            bool currentMastered = userWord.memoryScore >= 100;
            bool newState = !currentMastered;
            
            VocabularyManager.Instance.SetWordMastery(userWord.word, newState);
            
            // Update Local UI instantly for responsiveness
            userWord.memoryScore = newState ? 100 : 30;
            
            if (newState) 
                cardRoot.AddToClassList("mastered");
            else 
                cardRoot.RemoveFromClassList("mastered");
            
            // Update stats
            UpdateStats();
        };

        wordListScroll.Add(card);
    }
    }

    private void RefreshMeaningsVisibility()
    {
        if (wordListScroll == null) return;
        
        // Iterate over children and update label visibility
        // Note: Hierarchy is scroll -> TemplateContainer -> CardRoot -> Content -> Label
        
        var labels = wordListScroll.Query<Label>("MeaningLabel").Build();
        foreach (var label in labels)
        {
            label.style.visibility = showMeanings ? Visibility.Visible : Visibility.Hidden;
        }
    }
    
    // ============================================
    // PAGINATION METHODS
    // ============================================
    
    private void UpdatePaginationUI()
    {
        if (pageInfoLabel != null)
        {
            pageInfoLabel.text = $"{currentPage + 1} / {totalPages}";
        }
        
        // Enable/Disable buttons based on current page
        if (prevPageButton != null)
        {
            prevPageButton.SetEnabled(currentPage > 0);
        }
        
        if (nextPageButton != null)
        {
            nextPageButton.SetEnabled(currentPage < totalPages - 1);
        }
    }
    
    private void GoToPreviousPage()
    {
        if (currentPage <= 0) return;
        
        currentPage--;
        UpdatePaginationUI();
        RenderList();
        
        // Scroll to top of list for better UX
        if (wordListScroll != null)
        {
            wordListScroll.scrollOffset = Vector2.zero;
        }
    }
    
    private void GoToNextPage()
    {
        if (currentPage >= totalPages - 1) return;
        
        currentPage++;
        UpdatePaginationUI();
        RenderList();
        
        // Scroll to top of list for better UX
        if (wordListScroll != null)
        {
            wordListScroll.scrollOffset = Vector2.zero;
        }
    }
}
