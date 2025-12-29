using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class LearningPathGenerator : MonoBehaviour
{
    [Header("UI References")]
    public GameObject chapterNodePrefab;
    public GameObject linePrefab;
    public RectTransform learningPathContainer; // ScrollRect의 Content RectTransform에 연결

    [SerializeField] private HeaderUI headerUI;

    [Header("Layout Settings")]
    // 이 firstChapterPosition.y는 이제 Content의 Y=0 지점을 기준으로 한 상대적 위치입니다.
    // Content의 Y=0 지점이 Viewport의 Y=0과 일치하므로,
    // 이 값을 0 또는 음수로 설정하여 노드가 Viewport의 상단에서 시작하도록 합니다.
    public Vector2 firstChapterPosition = new Vector2(-300, 0); // 맨 처음 노드의 위치 (예시: -50은 상단 패딩)
    public float topPadding = 50f; // Amount of space above the first node
    public float bottomPadding = 300f; // Amount of space below the last node
    public float verticalSpacing = 280f; // 챕터 간의 세로 간격
    public float horizontalOffset = 270f; // 챕터 간의 가로 스윙 간격

    [Header("Node Colors")]
    public Color completedNodeColor = new Color(0.2f, 0.7f, 0.2f, 1f);
    public Color activeNodeColor = new Color(0.1f, 0.4f, 0.8f, 1f);
    public Color lockedNodeColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    private CourseDataList allCoursesData;
    private Dictionary<string, GameObject> chapterNodeInstances = new Dictionary<string, GameObject>();

    public string currentActiveChapterId = "chapter_02";
    public RectTransform viewportRect;

    

    void Start()
    {
        StartCoroutine(GenerateAfterLayout());
    }


    public void OnPageShow()
    {
        //StartCoroutine(GenerateAfterLayout());
    }

    IEnumerator GenerateAfterLayout()
    {
        yield return new WaitForEndOfFrame();

        float containerHeight = learningPathContainer.sizeDelta.y;
        float viewportHeight = viewportRect.rect.height;
        Debug.Log($"[AFTER LAYOUT] containerHeight: {containerHeight}, viewportHeight: {viewportHeight}");

        var progress = ProgressManager.Instance;
        string selectedCourseId = progress.CurrentCourseId;
        string selectedChapterId = progress.CurrentChapterId;
        currentActiveChapterId = selectedChapterId;

        GeneratePathForCourse(selectedCourseId);
    }

    public void GeneratePathForCourse(string courseId)
    {
        ClearExistingPath();
        chapterNodeInstances.Clear();

        // ✅ CurriculumRepository에서 코스 가져오기
        var selectedCourse = CurriculumRepository.Courses.FirstOrDefault(c => c.Id == courseId);
        if (selectedCourse == null)
        {
            Debug.LogError($"Course with ID '{courseId}' not found!");
            return;
        }

        if (headerUI != null)
            headerUI.SetTitle(selectedCourse.Name);

        Debug.Log($"Generating path for course: {selectedCourse.Name}");

        Vector2 currentPosition = firstChapterPosition;
        float lowestNodeY = float.MaxValue;
        float highestNodeY = float.MinValue;

        float nodeHalfHeight = 0f;
        if (chapterNodePrefab != null && chapterNodePrefab.GetComponent<RectTransform>() != null)
        {
            nodeHalfHeight = chapterNodePrefab.GetComponent<RectTransform>().sizeDelta.y / 2f;
        }
        else
        {
            Debug.LogError("Chapter Node Prefab is missing or does not have a RectTransform! Cannot calculate nodeHalfHeight.");
            return;
        }

        var chapters = selectedCourse.Chapters;
        for (int i = 0; i < chapters.Count; i++)
        {
            var chapter = chapters[i];

            GameObject nodeInstance = Instantiate(chapterNodePrefab, learningPathContainer);
            nodeInstance.name = $"ChapterNode_{chapter.Id}";

            TextMeshProUGUI chapterText = nodeInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (chapterText != null)
            {
                chapterText.text = $"Chapter {i + 1}";
            }

            Image innerCircleImage = GetChildByNameRecursive(nodeInstance.transform, "InnerCircle")?.GetComponent<Image>();
            Transform lockIconTransform = GetChildByNameRecursive(nodeInstance.transform, "LockIcon");

            // 클릭 시 씬 전환
            Button nodeButton = nodeInstance.GetComponent<Button>();
            if (nodeButton != null)
            {
                string selectedChapterId = chapter.Id;
                nodeButton.onClick.AddListener(() =>
                {
                    string courseIdLocal = selectedCourse.Id;
                    ProgressManager.Instance.SetCurrent(courseIdLocal, selectedChapterId, "step_001");
                    StepSceneLoader.LoadStepScene(selectedChapterId);
                });
            }

            if (innerCircleImage != null)
            {
                bool isChapterCompleted = (i == 0); // TODO: 실제 완료 상태 로직으로 교체 가능
                bool isChapterUnlocked = chapter.UnlockedByDefault || isChapterCompleted;

                if (isChapterCompleted)
                {
                    innerCircleImage.color = completedNodeColor;
                    if (lockIconTransform != null) lockIconTransform.gameObject.SetActive(false);
                }
                else if (chapter.Id == currentActiveChapterId)
                {
                    innerCircleImage.color = activeNodeColor;
                    if (lockIconTransform != null) lockIconTransform.gameObject.SetActive(false);
                }
                else if (isChapterUnlocked)
                {
                    innerCircleImage.color = activeNodeColor;
                    if (lockIconTransform != null) lockIconTransform.gameObject.SetActive(false);
                }
                else
                {
                    innerCircleImage.color = lockedNodeColor;
                    if (lockIconTransform != null) lockIconTransform.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning($"InnerCircle Image component not found for ChapterNode_{chapter.Id}. Cannot set color.");
            }

            RectTransform nodeRect = nodeInstance.GetComponent<RectTransform>();
            nodeRect.anchoredPosition = currentPosition;

            chapterNodeInstances.Add(chapter.Id, nodeInstance);

            float currentNodeTopEdge = currentPosition.y + nodeHalfHeight;
            float currentNodeBottomEdge = currentPosition.y - nodeHalfHeight;

            if (currentNodeTopEdge > highestNodeY) highestNodeY = currentNodeTopEdge;
            if (currentNodeBottomEdge < lowestNodeY) lowestNodeY = currentNodeBottomEdge;

            if (i < chapters.Count - 1)
            {
                currentPosition.y -= verticalSpacing;
                if (Mathf.Abs(currentPosition.x - firstChapterPosition.x) < 0.1f)
                    currentPosition.x += horizontalOffset;
                else
                    currentPosition.x -= horizontalOffset;
            }
        }

        if (chapterNodeInstances.Count > 0)
        {
            float contentRequiredHeight = highestNodeY - lowestNodeY;
            learningPathContainer.sizeDelta = new Vector2(learningPathContainer.sizeDelta.x, contentRequiredHeight + 50f);

            Debug.Log($"[DEBUG] firstChapterPosition.y (adjusted): {firstChapterPosition.y}");
            Debug.Log($"[DEBUG] Calculated highestNodeY: {highestNodeY}");
            Debug.Log($"[DEBUG] Calculated lowestNodeY: {lowestNodeY}");
            Debug.Log($"[DEBUG] Content Required Height (nodes span): {contentRequiredHeight}");
            Debug.Log($"[DEBUG] Final Content SizeDelta Y: {learningPathContainer.sizeDelta.y}");

            RectTransform viewportRect = learningPathContainer.parent.GetComponent<RectTransform>();
            if (viewportRect != null)
            {
                Debug.Log($"[DEBUG] Viewport Height: {viewportRect.rect.height}");
                if (learningPathContainer.sizeDelta.y <= viewportRect.rect.height)
                {
                    Debug.LogWarning("[DEBUG] Content height is not greater than Viewport height! Scroll will not work.");
                }
            }
        }
        else
        {
            learningPathContainer.sizeDelta = new Vector2(learningPathContainer.sizeDelta.x, 0);
        }

        // 연결선 생성
        foreach (var chapter in chapters)
        {
            if (chapter.NextChapterIds != null && chapter.NextChapterIds.Count > 0)
            {
                if (chapterNodeInstances.TryGetValue(chapter.Id, out GameObject startNode))
                {
                    foreach (string nextChapterId in chapter.NextChapterIds)
                    {
                        if (chapterNodeInstances.TryGetValue(nextChapterId, out GameObject endNode))
                        {
                            Debug.LogWarning($"chapter.id:: {chapter.Id}");
                            Debug.LogWarning($"nextChapterId:: {nextChapterId}");
                            DrawLineBetweenNodes(startNode, endNode);
                        }
                        else
                        {
                            Debug.LogWarning($"Next chapter ID '{nextChapterId}' not found for chapter '{chapter.Id}'. Skipping line.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Chapter node for ID '{chapter.Id}' not found. Skipping line generation for its connections.");
                }
            }
        }
    }

    private void DrawLineBetweenNodes(GameObject startNode, GameObject endNode)
    {
        Debug.Log($"[LineDebug] Attempting to draw line from {startNode.name} to {endNode.name}");

        GameObject lineInstance = Instantiate(linePrefab, learningPathContainer);
        UILineRenderer lineRenderer = lineInstance.GetComponent<UILineRenderer>();

        if (lineRenderer == null)
        {
            Debug.LogError("LinePrefab에 UILineRenderer 컴포넌트가 없습니다!");
            Destroy(lineInstance);
            return;
        }

        // 시작과 끝 좌표 (UI 좌표)
        Vector2 startPos = startNode.GetComponent<RectTransform>().anchoredPosition;
        Vector2 endPos = endNode.GetComponent<RectTransform>().anchoredPosition;

        // 두 점만 연결
        lineRenderer.SetPoints(new Vector2[] { startPos, endPos });

        // 두께 및 색상 옵션 조정 (선택)
        //lineRenderer.LineThickness = 8f;
        /*
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        lineRenderer.SetGradient(g);
        */
        lineInstance.transform.SetSiblingIndex(0);
        //lineRenderer.color = new Color32(220, 220, 220, 255);
    }

    private void ClearExistingPath()
    {
        foreach (Transform child in learningPathContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private Transform GetChildByNameRecursive(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent;
        }
        foreach (Transform child in parent)
        {
            Transform result = GetChildByNameRecursive(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}