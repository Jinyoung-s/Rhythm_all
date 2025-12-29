using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class CoursesController : MonoBehaviour
{
    private VisualElement chapterGrid;
    private Label levelTitle;
    private Label progressText;
    private ProgressBar levelProgress;

    private Label courseTitle;
    private Label courseDescription;

    private string selectedCourseId;

    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        levelTitle = root.Q<Label>("LevelTitle");
        progressText = root.Q<Label>("ProgressText");
        levelProgress = root.Q<ProgressBar>("LevelProgress");
        chapterGrid = root.Q<VisualElement>("ChapterGrid");

        var topInfo = root.Q<VisualElement>("TopInfoContainer");
        topInfo.pickingMode = PickingMode.Ignore;

        var scroll = root.Q<ScrollView>("ChapterScroll");
        scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        scroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic; // (Clamped도 가능)
        scroll.mouseWheelScrollSize = 40;   // 휠 민감도(선택)

        // ✅ 상단 설명 박스
        courseTitle = root.Q<Label>("CourseTitle");
        courseDescription = root.Q<Label>("CourseDescription");
        PopulateCourses();

        // 코스 변경 버튼 클릭 이벤트 등록
        var changeCourseButton = root.Q<Button>("ChangeCourseButton");
        changeCourseButton.clicked += () => CourseChange();

    }

    private void OnCourseClicked(string courseId)
    {
        selectedCourseId = courseId;
        var course = CurriculumRepository.Courses.FirstOrDefault(c => c.Id == courseId);

        if (course != null)
        {
            // ✅ 상단 설명 업데이트
            courseTitle.text = course.Name;
            courseDescription.text = course.Description;
        }

        // 기존 동작 유지
        Debug.Log($"[CoursesController] Clicked Course: {courseId}");
        PlayerPrefs.SetString("SelectedCourseId", courseId);
        PlayerPrefs.Save();
    }

    private void PopulateCourses()
    {
        var courses = CurriculumRepository.Courses;
        if (courses == null || courses.Count == 0)
        {
            Debug.LogError("[CoursesController] No courses found in CurriculumRepository.");
            return;
        }

        levelTitle.text = "코스 선택";
        progressText.text = $"총 {courses.Count}개의 코스";
        levelProgress.value = 0;
        chapterGrid.Clear();

        for (int i = 0; i < courses.Count; i++)
        {
            var course = courses[i];
            var card = new VisualElement();
            card.AddToClassList("chapter-card");

            // 상태 계산 (예시)
            string status;
            string styleClass;
            if (i == 0)
            {
                status = "● 진행 중 (40%)";
                styleClass = "in-progress";
            }
            else if (i == 1)
            {
                status = "✓ 완료";
                styleClass = "completed";
            }
            else
            {
                status = "⏳ 시작 전";
                styleClass = "not-started";
            }
            card.AddToClassList(styleClass);

            /* -----------------------------
            * Label 각각을 컨테이너로 감싸기
            * ----------------------------- */

            // 번호
            var numberContainer = new VisualElement();
            numberContainer.style.width = new Length(100, LengthUnit.Percent);
            numberContainer.style.paddingBottom = 6;
            numberContainer.style.alignItems = Align.Center;

            var labelNumber = new Label((i + 1).ToString());
            labelNumber.AddToClassList("chapter-number");
            numberContainer.Add(labelNumber);
            card.Add(numberContainer);

            // 제목
            var titleContainer = new VisualElement();
            titleContainer.style.width = new Length(100, LengthUnit.Percent);
            titleContainer.style.paddingBottom = 8;
            titleContainer.style.alignItems = Align.Center;

            var labelTitle = new Label(course.Name);
            labelTitle.AddToClassList("chapter-title");
            titleContainer.Add(labelTitle);
            card.Add(titleContainer);

            // 설명
            /*
            var descriptionContainer = new VisualElement();
            descriptionContainer.style.width = new Length(100, LengthUnit.Percent);
            descriptionContainer.style.paddingBottom = 8;
            descriptionContainer.style.alignItems = Align.Center;   

            var labelDescription = new Label(course.Description);
            labelDescription.AddToClassList("description-text");
            descriptionContainer.Add(labelDescription);
            card.Add(descriptionContainer);
            */

            // 상태
            var statusContainer = new VisualElement();
            statusContainer.style.width = new Length(100, LengthUnit.Percent);
            statusContainer.style.alignItems = Align.Center;

            var labelStatus = new Label(status);
            labelStatus.AddToClassList("chapter-status");
            statusContainer.Add(labelStatus);
            card.Add(statusContainer);

            // 클릭 이벤트
            card.RegisterCallback<ClickEvent>(evt => OnCourseClicked(course.Id));

            chapterGrid.Add(card);
        }
    }

    
    private void CourseChange()
    {
        Debug.Log($"[CoursesController] Clicked Course: {selectedCourseId}");
        //PlayerPrefs.SetString("SelectedCourseId", selectedCourseId);
        //PlayerPrefs.Save();
        ProgressManager.Instance.ResumeCourse(selectedCourseId);

        SceneNavigator.Load("MainMenuScene");
    }    
}
