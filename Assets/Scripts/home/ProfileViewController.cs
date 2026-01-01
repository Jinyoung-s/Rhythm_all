using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using RhythmEnglish.Economy;

/// <summary>
/// 프로필 화면 컨트롤러
/// - 사용자 정보 표시
/// - 학습 통계 표시
/// - 포인트 히스토리 표시
/// - 저장된 콘텐츠 바로가기
/// - 설정 관리
/// </summary>
public class ProfileViewController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("References")]
    [SerializeField] private MainUIController mainUIController;

    // Root
    private VisualElement root;

    // User Info Elements
    private Label avatarInitial;
    private Label username;
    private Label userStatus;

    // Stats Elements
    private Label currentNotes;
    private Label totalEarned;
    private Label totalSpent;
    private Label stepsCompleted;
    private Label currentCourseName;
    private VisualElement progressBarFill;
    private Label progressPercent;

    // History Elements
    private VisualElement historyList;
    private Label noHistoryLabel;
    private Button viewAllHistoryBtn;

    // Settings Elements
    private Label appVersion;
    private Button languageSettingBtn;
    private Button notificationsSettingBtn;
    private Button logoutBtn;
    private Button aboutBtn;

    // Templates
    private VisualTreeAsset historyItemTemplate;

    void OnEnable()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        root = uiDocument.rootVisualElement;

        LoadTemplates();
        BindElements();
        RegisterEvents();
        RefreshUI();
    }

    void OnDisable()
    {
        UnregisterEvents();
    }

    /// <summary>
    /// 템플릿 로드
    /// </summary>
    private void LoadTemplates()
    {
        historyItemTemplate = Resources.Load<VisualTreeAsset>("UI/Profile/PointHistoryItem");
        if (historyItemTemplate == null)
        {
            Debug.LogWarning("[ProfileView] PointHistoryItem template not found");
        }
    }

    /// <summary>
    /// UI 요소 바인딩
    /// </summary>
    private void BindElements()
    {
        // User Info
        avatarInitial = root.Q<Label>("avatar-initial");
        username = root.Q<Label>("username");
        userStatus = root.Q<Label>("user-status");

        // Stats
        currentNotes = root.Q<Label>("current-notes");
        totalEarned = root.Q<Label>("total-earned");
        totalSpent = root.Q<Label>("total-spent");
        stepsCompleted = root.Q<Label>("steps-completed");
        currentCourseName = root.Q<Label>("current-course-name");
        progressBarFill = root.Q<VisualElement>("progress-bar-fill");
        progressPercent = root.Q<Label>("progress-percent");

        // History
        historyList = root.Q<VisualElement>("history-list");
        noHistoryLabel = root.Q<Label>("no-history-label");
        viewAllHistoryBtn = root.Q<Button>("view-all-history-btn");

        // Settings
        appVersion = root.Q<Label>("app-version");
        languageSettingBtn = root.Q<Button>("language-setting-btn");
        notificationsSettingBtn = root.Q<Button>("notifications-setting-btn");
        logoutBtn = root.Q<Button>("logout-btn");
        aboutBtn = root.Q<Button>("about-btn");
    }

    /// <summary>
    /// 이벤트 등록
    /// </summary>
    private void RegisterEvents()
    {
        logoutBtn?.RegisterCallback<ClickEvent>(_ => OnLogoutClicked());
        viewAllHistoryBtn?.RegisterCallback<ClickEvent>(_ => OnViewAllHistoryClicked());
        languageSettingBtn?.RegisterCallback<ClickEvent>(_ => OnLanguageSettingClicked());
        notificationsSettingBtn?.RegisterCallback<ClickEvent>(_ => OnNotificationsSettingClicked());
        aboutBtn?.RegisterCallback<ClickEvent>(_ => OnAboutClicked());
    }

    /// <summary>
    /// 이벤트 해제
    /// </summary>
    private void UnregisterEvents()
    {
        logoutBtn?.UnregisterCallback<ClickEvent>(_ => OnLogoutClicked());
        viewAllHistoryBtn?.UnregisterCallback<ClickEvent>(_ => OnViewAllHistoryClicked());
        languageSettingBtn?.UnregisterCallback<ClickEvent>(_ => OnLanguageSettingClicked());
        notificationsSettingBtn?.UnregisterCallback<ClickEvent>(_ => OnNotificationsSettingClicked());
        aboutBtn?.UnregisterCallback<ClickEvent>(_ => OnAboutClicked());
    }

    /// <summary>
    /// UI 새로고침 (페이지가 활성화될 때마다 호출)
    /// </summary>
    public void RefreshUI()
    {
        UpdateUserInfo();
        UpdateStats();
        UpdateHistory();
        UpdateSettings();
    }

    /// <summary>
    /// 사용자 정보 업데이트
    /// </summary>
    private void UpdateUserInfo()
    {
        var userProfile = UserProfileManager.Instance.CurrentUser;

        if (userProfile != null)
        {
            // 아바타 이니셜
            if (avatarInitial != null)
            {
                string name = userProfile.DisplayName ?? "User";
                avatarInitial.text = name.Length > 0 ? name[0].ToString().ToUpper() : "U";
            }

            // 사용자 이름
            if (username != null)
            {
                username.text = userProfile.DisplayName ?? "Guest User";
            }

            // 회원 상태
            if (userStatus != null)
            {
                string status = userProfile.IsGuest ? "Guest Account" : 
                               userProfile.IsSubscribed ? "Premium Account" : "Free Account";
                userStatus.text = status;
            }
        }
        else
        {
            // 로그인하지 않은 상태
            if (username != null) username.text = "Guest";
            if (userStatus != null) userStatus.text = "Not logged in";
            if (avatarInitial != null) avatarInitial.text = "?";
        }
    }

    /// <summary>
    /// 학습 통계 업데이트
    /// </summary>
    private void UpdateStats()
    {
        // 포인트 현황
        if (currentNotes != null)
        {
            currentNotes.text = PointManager.Instance.GetAvailableNotes().ToString("N0");
        }

        if (totalEarned != null)
        {
            totalEarned.text = PointManager.Instance.GetTotalEarnedNotes().ToString("N0");
        }

        if (totalSpent != null)
        {
            totalSpent.text = PointManager.Instance.GetTotalSpentNotes().ToString("N0");
        }

        // 완료한 Step 수
        int completedSteps = CalculateTotalCompletedSteps();
        if (stepsCompleted != null)
        {
            stepsCompleted.text = completedSteps.ToString();
        }

        // 현재 코스 진행률
        UpdateCourseProgress();
    }

    /// <summary>
    /// 현재 코스 진행률 업데이트
    /// </summary>
    private void UpdateCourseProgress()
    {
        var progressMgr = ProgressManager.Instance;
        string courseId = progressMgr.CurrentCourseId;

        if (string.IsNullOrEmpty(courseId))
        {
            if (currentCourseName != null) currentCourseName.text = "No course selected";
            if (progressPercent != null) progressPercent.text = "0%";
            if (progressBarFill != null) progressBarFill.style.width = Length.Percent(0);
            return;
        }

        // 코스 이름 표시
        if (currentCourseName != null)
        {
            // courseId를 사람이 읽을 수 있는 이름으로 변환
            string displayName = GetCourseDisplayName(courseId);
            currentCourseName.text = displayName;
        }

        // 진행률 계산
        float totalProgress = CalculateCourseProgress(courseId);
        if (progressPercent != null)
        {
            progressPercent.text = $"{totalProgress:F0}%";
        }

        if (progressBarFill != null)
        {
            progressBarFill.style.width = Length.Percent(totalProgress);
        }
    }

    /// <summary>
    /// 전체 완료한 Step 수 계산
    /// </summary>
    private int CalculateTotalCompletedSteps()
    {
        int total = 0;
        var progressMgr = ProgressManager.Instance;

        foreach (var coursePair in progressMgr.Courses)
        {
            foreach (var chapterPair in coursePair.Value.Chapters)
            {
                foreach (var stepPair in chapterPair.Value.Steps)
                {
                    if (stepPair.Value.TestCompleted)
                    {
                        total++;
                    }
                }
            }
        }

        return total;
    }

    /// <summary>
    /// 코스 진행률 계산 (%)
    /// </summary>
    private float CalculateCourseProgress(string courseId)
    {
        var progressMgr = ProgressManager.Instance;
        
        // Courses 딕셔너리에 직접 접근
        if (!progressMgr.Courses.TryGetValue(courseId, out var courseProgress))
        {
            return 0f;
        }

        int totalSteps = 0;
        int completedSteps = 0;

        foreach (var chapterPair in courseProgress.Chapters)
        {
            foreach (var stepPair in chapterPair.Value.Steps)
            {
                totalSteps++;
                if (stepPair.Value.TestCompleted)
                {
                    completedSteps++;
                }
            }
        }

        if (totalSteps == 0) return 0f;
        return (float)completedSteps / totalSteps * 100f;
    }

    /// <summary>
    /// 코스 표시 이름 가져오기
    /// </summary>
    private string GetCourseDisplayName(string courseId)
    {
        // 간단한 변환 로직 (추후 다국어 지원 시 개선)
        switch (courseId.ToLower())
        {
            case "pvb":
                return "Pre-beginner";
            case "beg":
                return "Beginner";
            case "int":
                return "Intermediate";
            case "adv":
                return "Advanced";
            default:
                return courseId.ToUpper();
        }
    }

    /// <summary>
    /// 포인트 히스토리 업데이트
    /// </summary>
    private void UpdateHistory()
    {
        if (historyList == null) return;

        // 기존 히스토리 삭제 (템플릿으로 추가된 항목만)
        var existingItems = historyList.Query<VisualElement>(className: "history-item").ToList();
        foreach (var item in existingItems)
        {
            historyList.Remove(item);
        }

        // 히스토리 가져오기 (최근 5개만)
        var history = PointManager.Instance.GetHistory(5);

        if (history == null || history.Count == 0)
        {
            // 히스토리 없음
            if (noHistoryLabel != null)
            {
                noHistoryLabel.style.display = DisplayStyle.Flex;
            }
            return;
        }

        // 히스토리 있음
        if (noHistoryLabel != null)
        {
            noHistoryLabel.style.display = DisplayStyle.None;
        }

        // 히스토리 아이템 추가
        foreach (var entry in history)
        {
            AddHistoryItem(entry);
        }
    }

    /// <summary>
    /// 히스토리 아이템 추가
    /// </summary>
    private void AddHistoryItem(PointHistory entry)
    {
        if (historyItemTemplate == null)
        {
            Debug.LogWarning("[ProfileView] Cannot add history item - template is null");
            return;
        }

        var item = historyItemTemplate.Instantiate();
        var itemRoot = item.Q<VisualElement>("history-item");

        // 아이콘 설정
        var icon = itemRoot.Q<Label>("history-icon");
        if (icon != null)
        {
            icon.text = GetIconForSource(entry.source);
        }

        // 설명 설정
        var description = itemRoot.Q<Label>("history-description");
        if (description != null)
        {
            description.text = entry.description;
        }

        // 시간 설정
        var time = itemRoot.Q<Label>("history-time");
        if (time != null)
        {
            time.text = GetRelativeTime(entry.GetDateTime());
        }

        // 금액 설정
        var amount = itemRoot.Q<Label>("history-amount");
        if (amount != null)
        {
            bool isPositive = entry.amount > 0;
            amount.text = (isPositive ? "+" : "") + entry.amount.ToString();
            amount.RemoveFromClassList(isPositive ? "negative" : "positive");
            amount.AddToClassList(isPositive ? "positive" : "negative");
        }

        historyList.Add(item);
    }

    /// <summary>
    /// 포인트 출처에 따른 아이콘 반환
    /// </summary>
    private string GetIconForSource(string source)
    {
        switch (source)
        {
            case "Game1": return "🎮";
            case "Game2": return "🎵";
            case "SingAlong": return "🎤";
            case "StepTest": return "📝";
            case "VocabularyTest": return "📚";
            case "Purchase": return "🛒";
            case "Bonus": return "🎁";
            case "Daily": return "📅";
            default: return "💰";
        }
    }

    /// <summary>
    /// 상대적 시간 문자열 반환
    /// </summary>
    private string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
        
        return dateTime.ToString("MMM dd, yyyy");
    }

    /// <summary>
    /// 설정 업데이트
    /// </summary>
    private void UpdateSettings()
    {
        // 앱 버전 표시
        if (appVersion != null)
        {
            appVersion.text = $"Version {Application.version} ›";
        }
    }

    // ==========================================
    // Event Handlers
    // ==========================================

    private void OnLogoutClicked()
    {
        Debug.Log("[ProfileView] Logout clicked");
        
        // TODO: 확인 팝업 표시
        // 지금은 바로 로그아웃
        UserProfileManager.Instance.SignOut();
        
        // UI 새로고침
        RefreshUI();
    }

    private void OnViewAllHistoryClicked()
    {
        Debug.Log("[ProfileView] View All History clicked - Not implemented yet");
        // TODO: 전체 히스토리 화면으로 이동
    }

    private void OnLanguageSettingClicked()
    {
        Debug.Log("[ProfileView] Language Setting clicked - Not implemented yet");
        // TODO: 언어 설정 팝업 표시
    }

    private void OnNotificationsSettingClicked()
    {
        Debug.Log("[ProfileView] Notifications Setting clicked - Not implemented yet");
        // TODO: 알림 설정 팝업 표시
    }

    private void OnAboutClicked()
    {
        Debug.Log("[ProfileView] About clicked - Not implemented yet");
        // TODO: 앱 정보 팝업 표시
    }
}
