using UnityEngine;
using UnityEngine.UIElements;

public class BottomMenuController : MonoBehaviour
{
    // Root
    private VisualElement root;

    // Tabs
    private VisualElement homeTab;
    private VisualElement reviewTab;
    private VisualElement playTab;
    private VisualElement profileTab;

    // 현재 선택된 탭
    private VisualElement currentTab;
    
    [SerializeField] private MainUIController mainUIController;

    void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // 다른 페이지들보다 위에 렌더링되도록 높은 sortingOrder 설정
        uiDocument.sortingOrder = 100;

        BindElements();
        RegisterEvents();

        // 초기 선택 탭 (Home)
        SelectTab(homeTab);
    }

    /// <summary>
    /// UXML 요소 바인딩
    /// </summary>
    private void BindElements()
    {
        homeTab = root.Q<VisualElement>("homeTab");
        reviewTab = root.Q<VisualElement>("reviewTab");
        playTab = root.Q<VisualElement>("playTab");
        profileTab = root.Q<VisualElement>("profileTab");

        // 안전성 체크
        Debug.Assert(homeTab != null, "homeTab not found");
        Debug.Assert(reviewTab != null, "reviewTab not found");
        Debug.Assert(playTab != null, "playTab not found");
        Debug.Assert(profileTab != null, "profileTab not found");
    }

    /// <summary>
    /// 클릭 이벤트 등록
    /// </summary>
    private void RegisterEvents()
    {
        homeTab.RegisterCallback<ClickEvent>(_ => OnTabClicked(homeTab, BottomTab.Home));
        reviewTab.RegisterCallback<ClickEvent>(_ => OnTabClicked(reviewTab, BottomTab.Review));
        playTab.RegisterCallback<ClickEvent>(_ => OnTabClicked(playTab, BottomTab.Play));
        profileTab.RegisterCallback<ClickEvent>(_ => OnTabClicked(profileTab, BottomTab.Profile));
    }

    /// <summary>
    /// 탭 클릭 공통 처리
    /// </summary>
    private void OnTabClicked(VisualElement tab, BottomTab tabType)
    {
        if (currentTab == tab)
            return;

        SelectTab(tab);

        switch (tabType)
        {
            case BottomTab.Home:
                mainUIController.SwitchPage(PageType.Home);
                break;

            case BottomTab.Review:
                mainUIController.SwitchPage(PageType.Review);
                break;

            case BottomTab.Profile:
                mainUIController.SwitchPage(PageType.Profile);
                break;

            case BottomTab.Play:
                mainUIController.SwitchPage(PageType.Play);
                break;
        }
    }


    /// <summary>
    /// 선택 상태 갱신
    /// </summary>
    private void SelectTab(VisualElement tab)
    {
        if (currentTab != null)
            currentTab.RemoveFromClassList("active");

        currentTab = tab;
        currentTab.AddToClassList("active");
    }

    /// <summary>
    /// 하단 탭 타입 정의
    /// </summary>
    public enum BottomTab
    {
        Home,
        Review,
        Play,
        Profile
    }
}
