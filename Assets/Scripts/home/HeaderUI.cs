using UnityEngine;
using UnityEngine.UIElements;
using RhythmEnglish.Economy;

[RequireComponent(typeof(UIDocument))]
public class HeaderUI : MonoBehaviour
{
    private Label titleLabel; 
    private Label pointsLabel;
    private VisualElement rootContainer;
    private VisualElement pointsContainer;

    private void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // 다른 페이지들보다 위에 렌더링되도록 높은 sortingOrder 설정
        uiDocument.sortingOrder = 100;

        // 최상위 컨테이너 또는 root 자체를 참조
        rootContainer = root.Q<VisualElement>("headerContainer") ?? root;
        titleLabel = root.Q<Label>("titleLabel");
        pointsLabel = root.Q<Label>("pointsLabel");
        pointsContainer = root.Q<VisualElement>("pointsContainer");
    }

    private void Start()
    {
        // 초기 포인트 표시
        UpdatePoints();
        
        // 포인트 변경 이벤트 구독
        PointManager.Instance.OnPointsChanged += OnPointsChanged;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PointManager.Instance != null)
        {
            PointManager.Instance.OnPointsChanged -= OnPointsChanged;
        }
    }

    private void OnPointsChanged(int newAmount)
    {
        UpdatePointsDisplay(newAmount);
    }

    public void SetTitle(string newTitle)
    {
        if (titleLabel != null)
            titleLabel.text = newTitle;
        else
            Debug.LogWarning("[HeaderUI] TitleLabel not found in UXML.");
    }

    /// <summary>
    /// 현재 포인트를 PointManager에서 가져와서 표시
    /// </summary>
    public void UpdatePoints()
    {
        int currentPoints = PointManager.Instance.GetAvailableNotes();
        UpdatePointsDisplay(currentPoints);
    }

    /// <summary>
    /// 포인트 숫자 업데이트
    /// </summary>
    private void UpdatePointsDisplay(int points)
    {
        if (pointsLabel != null)
        {
            pointsLabel.text = FormatPoints(points);
        }
    }

    /// <summary>
    /// 포인트 숫자 포맷팅 (1000 이상일 경우 쉼표 추가)
    /// </summary>
    private string FormatPoints(int points)
    {
        return points.ToString("N0");
    }

    /// <summary>
    /// 포인트 표시 영역 숨기기
    /// </summary>
    public void HidePoints()
    {
        if (pointsContainer != null)
            pointsContainer.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// 포인트 표시 영역 보이기
    /// </summary>
    public void ShowPoints()
    {
        if (pointsContainer != null)
            pointsContainer.style.display = DisplayStyle.Flex;
    }

    public void Show()
    {
        if (rootContainer != null) rootContainer.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (rootContainer != null) rootContainer.style.display = DisplayStyle.None;
    }
}
