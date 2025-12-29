using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Central manager that tracks all learning progress across courses, chapters, steps, and stages.
/// Singleton pattern + PlayerPrefs storage.
/// Ready for hybrid (local + Firebase) extension.
/// </summary>
public class ProgressManager
{
    // -----------------------------
    // 🔹 Singleton
    // -----------------------------
    private static ProgressManager _instance;
    public static ProgressManager Instance => _instance ??= new ProgressManager();

    private CourseDataList allCoursesData;
    
    // 테스트 완료 데이터 저장 경로
    private readonly string testCompletedFilePath;

    private ProgressManager()
    {
        testCompletedFilePath = Path.Combine(Application.persistentDataPath, "user_test_completed.json");
        LoadProgress();
        LoadTestCompletedData();
    }

    // -----------------------------
    // 🔹 Current State
    // -----------------------------
    public string CurrentCourseId { get; private set; }
    public string CurrentChapterId { get; private set; }
    public string CurrentStepId { get; private set; }

    // -----------------------------
    // 🔹 All Courses Progress Data
    // -----------------------------
    public Dictionary<string, CourseProgress> Courses { get; private set; } = new();

    // 🔹 Event: when progress changes (UI can subscribe)
    public event Action OnProgressChanged;

    // -----------------------------
    // 🔹 Initialization
    // -----------------------------
    private void InitializeDefaultProgress()
    {
        // CurriculumRepository에서 첫 코스/첫 챕터 기준으로 초기화
        var firstCourse = CurriculumRepository.GetFirstCourseOrDefault();
        if (firstCourse == null || firstCourse.Chapters == null || firstCourse.Chapters.Count == 0)
        {
            Debug.LogError("[ProgressManager] No valid course/chapter found for initialization.");
            CurrentCourseId = "";
            CurrentChapterId = "";
            CurrentStepId = "";
            return;
        }

        CurrentCourseId = firstCourse.Id;
        CurrentChapterId = firstCourse.Chapters[0].Id;
        CurrentStepId = "step_001";

        var courseProgress = GetOrCreateCourseProgress(CurrentCourseId);
        courseProgress.LastChapterId = CurrentChapterId;
        courseProgress.LastStepId = CurrentStepId;

        SaveProgress();
    }

    // -----------------------------
    // 🔹 Helpers (GetOrCreate)
    // -----------------------------
    private CourseProgress GetOrCreateCourseProgress(string courseId)
    {
        if (!Courses.TryGetValue(courseId, out var courseProgress))
        {
            courseProgress = new CourseProgress(courseId);
            Courses[courseId] = courseProgress;
        }

        return courseProgress;
    }

    private ChapterProgress GetOrCreateChapterProgress(string courseId, string chapterId)
    {
        var courseProgress = GetOrCreateCourseProgress(courseId);

        if (!courseProgress.Chapters.TryGetValue(chapterId, out var chapterProgress))
        {
            chapterProgress = new ChapterProgress(chapterId);
            courseProgress.Chapters[chapterId] = chapterProgress;
        }

        return chapterProgress;
    }

    private StepProgress GetOrCreateStepProgress(string courseId, string chapterId, string stepId)
    {
        var chapterProgress = GetOrCreateChapterProgress(courseId, chapterId);
        return chapterProgress.GetOrCreateStepProgress(stepId);
    }

    private StepProgress GetStepProgress(string courseId, string chapterId, string stepId)
    {
        if (!Courses.TryGetValue(courseId, out var courseProgress))
            return null;

        if (!courseProgress.Chapters.TryGetValue(chapterId, out var chapterProgress))
            return null;

        if (!chapterProgress.Steps.TryGetValue(stepId, out var stepProgress))
            return null;

        return stepProgress;
    }

    // -----------------------------
    // 🔹 Change Current Position
    // -----------------------------
    public void SetCurrent(string courseId, string chapterId, string stepId)
    {
        CurrentCourseId = courseId;
        CurrentChapterId = chapterId;
        CurrentStepId = stepId;

        // Update last known position for this course
        var courseProgress = GetOrCreateCourseProgress(courseId);
        courseProgress.LastChapterId = chapterId;
        courseProgress.LastStepId = stepId;

        SaveProgress();
        OnProgressChanged?.Invoke();
    }

    // -----------------------------
    // 🔹 Resume a Course (remember where user left off)
    // -----------------------------
    public void ResumeCourse(string courseId)
    {
        if (!CurriculumRepository.TryGetCourse(courseId, out var course))
        {
            Debug.LogError($"[ProgressManager] ResumeCourse: course '{courseId}' not found in CurriculumRepository.");
            return;
        }

        var courseProgress = GetOrCreateCourseProgress(courseId);

        if (string.IsNullOrEmpty(courseProgress.LastChapterId))
        {
            // 처음 진입: 코스의 첫 챕터 기준
            string firstChapterId = (course.Chapters != null && course.Chapters.Count > 0)
                ? course.Chapters[0].Id
                : "";

            courseProgress.LastChapterId = firstChapterId;
            courseProgress.LastStepId = "step_001";
        }

        CurrentCourseId = courseId;
        CurrentChapterId = courseProgress.LastChapterId;
        CurrentStepId = courseProgress.LastStepId;

        SaveProgress();
        OnProgressChanged?.Invoke();
    }

    // -----------------------------
    // 🔹 Stage Completion API
    // -----------------------------
    public void MarkLearnCompleted(string courseId, string chapterId, string stepId)
    {
        var sp = GetOrCreateStepProgress(courseId, chapterId, stepId);
        if (sp.LearnCompleted) return;

        sp.LearnCompleted = true;

        // 현재 위치 업데이트는 필요하면 여기서 할 수도 있지만,
        // 전체 Step 완료(Test) 기준으로만 위치를 업데이트하는 쪽을 택함.
        SaveProgress();
        OnProgressChanged?.Invoke();
    }

    public void MarkSingalongCompleted(string courseId, string chapterId, string stepId)
    {
        var sp = GetOrCreateStepProgress(courseId, chapterId, stepId);
        if (sp.SingalongCompleted) return;

        sp.SingalongCompleted = true;
        SaveProgress();
        OnProgressChanged?.Invoke();
    }

    public void MarkGame1Completed(string courseId, string chapterId, string stepId)
    {
        var sp = GetOrCreateStepProgress(courseId, chapterId, stepId);
        if (sp.Game1Completed) return;

        sp.Game1Completed = true;
        SaveProgress();
        OnProgressChanged?.Invoke();
    }

    public void MarkGame2Completed(string courseId, string chapterId, string stepId)
    {
        var sp = GetOrCreateStepProgress(courseId, chapterId, stepId);
        if (sp.Game2Completed) return;

        sp.Game2Completed = true;
        SaveProgress();
        OnProgressChanged?.Invoke();
    }

    /// <summary>
    /// Test Stage 완료 = 이 Step 전체 완료.
    /// 여기서 "다음 Step unlock 조건"도 만족시키는 기반 데이터(TestCompleted)를 기록한다.
    /// </summary>
    public void MarkTestCompleted(string courseId, string chapterId, string stepId)
    {
        var sp = GetOrCreateStepProgress(courseId, chapterId, stepId);
        if (sp.TestCompleted) return;

        sp.TestCompleted = true;

        // CourseProgress 기준 마지막 위치 갱신
        var courseProgress = GetOrCreateCourseProgress(courseId);
        courseProgress.LastChapterId = chapterId;
        courseProgress.LastStepId = stepId;

        // 현재 위치도 이 Step으로 이동
        CurrentCourseId = courseId;
        CurrentChapterId = chapterId;
        CurrentStepId = stepId;

        SaveProgress();
        SaveTestCompletedData(); // 테스트 완료 데이터를 별도 파일로도 저장
        OnProgressChanged?.Invoke();
        
        Debug.Log($"[ProgressManager] ✅ Test completed! Course: {courseId}, Chapter: {chapterId}, Step: {stepId}");
    }

    // -----------------------------
    // 🔹 Step Completion (Backward Compatible)
    // -----------------------------
    /// <summary>
    /// 기존 "Step 완료" 개념은 곧 "Test Stage 완료"와 동일하게 간주.
    /// 외부 코드에서 호출 시에도 TestCompleted로 처리된다.
    /// </summary>
    public void MarkStepComplete(string courseId, string chapterId, string stepId)
    {
        MarkTestCompleted(courseId, chapterId, stepId);
    }

    // -----------------------------
    // 🔹 Unlock / Status Queries
    // -----------------------------

    /// <summary>
    /// Step이 "열려 있는지" 판단.
    /// 규칙:
    ///  - chapter.Steps[0] 은 항상 unlock (첫 Step)
    ///  - 그 외 Step은 "이전 Step의 TestCompleted == true" 일 때 unlock
    /// </summary>
    public bool IsStepUnlocked(string courseId, string chapterId, string stepId)
    {
        if (!CurriculumRepository.TryGetChapter(chapterId, out var chapter) ||
            chapter.Steps == null || chapter.Steps.Count == 0)
        {
            return false;
        }


        int index = -1;
        for (int i = 0; i < chapter.Steps.Count; i++)
        {
            if (chapter.Steps[i].id == stepId)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            Debug.LogWarning($"[ProgressManager] IsStepUnlocked: step '{stepId}' not found in chapter '{chapterId}'.");
            return false;
        }

        // 첫 Step은 항상 unlock
        if (index == 0)
            return true;

        // 이전 Step의 TestCompleted 여부를 확인
        var prevStep = chapter.Steps[index - 1];
        var prevSp = GetStepProgress(courseId, chapterId, prevStep.id);

        return prevSp != null && prevSp.TestCompleted;
    }

    /// <summary>
    /// 특정 Stage(learn / singalong / game1 / game2 / test)가 unlock 되었는지 여부.
    /// - Learn: 항상 true (Step만 열려 있다면)
    /// - Singalong/Game1/Game2: LearnCompleted == true
    /// - Test: Singalong/Game1/Game2 중 하나라도 완료되었을 때
    /// </summary>
    public bool IsStageUnlocked(string courseId, string chapterId, string stepId, string stageId)
    {
        var sp = GetStepProgress(courseId, chapterId, stepId);

        switch (stageId)
        {
            case "learn":
                // Step이 열려있다고 가정하고, Learn 자체는 처음부터 사용 가능
                return true;

            case "singalong":
            case "game1":
            case "game2":
                return sp != null && sp.LearnCompleted;

            case "test":
                return sp != null && sp.IsAnyPracticeCompleted;

            default:
                Debug.LogWarning($"[ProgressManager] IsStageUnlocked: unknown stageId '{stageId}'.");
                return false;
        }
    }

    /// <summary>
    /// 특정 Stage가 완료되었는지 여부.
    /// </summary>
    public bool IsStageCompleted(string courseId, string chapterId, string stepId, string stageId)
    {
        var sp = GetStepProgress(courseId, chapterId, stepId);
        if (sp == null) return false;

        switch (stageId)
        {
            case "learn":      return sp.LearnCompleted;
            case "singalong":  return sp.SingalongCompleted;
            case "game1":      return sp.Game1Completed;
            case "game2":      return sp.Game2Completed;
            case "test":       return sp.TestCompleted;
            default:
                Debug.LogWarning($"[ProgressManager] IsStageCompleted: unknown stageId '{stageId}'.");
                return false;
        }
    }
    // -----------------------------
    // 🔹 Save & Load
    // -----------------------------
    public void SaveProgress()
    {
        try
        {
            string json = JsonUtility.ToJson(new ProgressData(this));
            PlayerPrefs.SetString("UserProgress", json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProgressManager] Save failed: {e.Message}");
        }
    }

    public void LoadProgress()
    {
        try
        {
            string json = PlayerPrefs.GetString("UserProgress", "");
            if (!string.IsNullOrEmpty(json))
            {
                ProgressData data = JsonUtility.FromJson<ProgressData>(json);
                ApplyProgress(data);
            }
            else
            {
                InitializeDefaultProgress();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProgressManager] Load failed: {e.Message}");
            InitializeDefaultProgress();
        }
    }

    private void ApplyProgress(ProgressData data)
    {
        CurrentCourseId = data.CurrentCourseId;
        CurrentChapterId = data.CurrentChapterId;
        CurrentStepId = data.CurrentStepId;
        Courses = data.Courses ?? new Dictionary<string, CourseProgress>();

        // 🔸 유효성 검증 추가
        if (!CurriculumRepository.TryGetCourse(CurrentCourseId, out var course) ||
            course.Chapters == null || course.Chapters.Count == 0)
        {
            Debug.LogWarning("[ProgressManager] Stored course/chapter not found in CurriculumRepository. Reinitializing progress...");
            InitializeDefaultProgress();
        }
    }

    // -----------------------------
    // 🔹 테스트 완료 데이터 저장/로드 (별도 파일)
    // -----------------------------
    private void SaveTestCompletedData()
    {
        try
        {
            var data = new TestCompletedData();
            
            // Dictionary를 List로 변환하여 저장
            foreach (var courseKvp in Courses)
            {
                foreach (var chapterKvp in courseKvp.Value.Chapters)
                {
                    foreach (var stepKvp in chapterKvp.Value.Steps)
                    {
                        if (stepKvp.Value.TestCompleted)
                        {
                            data.completedSteps.Add(new CompletedStepEntry
                            {
                                courseId = courseKvp.Key,
                                chapterId = chapterKvp.Key,
                                stepId = stepKvp.Key
                            });
                        }
                    }
                }
            }
            
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(testCompletedFilePath, json);
            Debug.Log($"[ProgressManager] Test completed data saved. Count: {data.completedSteps.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProgressManager] Failed to save test completed data: {e.Message}");
        }
    }
    
    private void LoadTestCompletedData()
    {
        try
        {
            if (!File.Exists(testCompletedFilePath))
            {
                Debug.Log("[ProgressManager] No test completed data file found.");
                return;
            }
            
            string json = File.ReadAllText(testCompletedFilePath);
            var data = JsonUtility.FromJson<TestCompletedData>(json);
            
            if (data == null || data.completedSteps == null)
            {
                Debug.LogWarning("[ProgressManager] Test completed data is null or invalid.");
                return;
            }
            
            // List에서 Dictionary로 복원
            foreach (var entry in data.completedSteps)
            {
                var sp = GetOrCreateStepProgress(entry.courseId, entry.chapterId, entry.stepId);
                sp.TestCompleted = true;
            }
            
            Debug.Log($"[ProgressManager] Test completed data loaded. Count: {data.completedSteps.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProgressManager] Failed to load test completed data: {e.Message}");
        }
    }

    // -----------------------------
    // 🔹 Utility
    // -----------------------------
    /// <summary>
    /// chapter 내 totalSteps 개수 기준으로,
    /// "TestCompleted == true 인 Step 수"를 퍼센트로 환산.
    /// </summary>
    public float GetChapterProgressPercent(string courseId, string chapterId, int totalSteps)
    {
        if (totalSteps <= 0) return 0f;
        if (!Courses.ContainsKey(courseId)) return 0f;
        if (!Courses[courseId].Chapters.ContainsKey(chapterId)) return 0f;

        var chapterProgress = Courses[courseId].Chapters[chapterId];
        int completed = chapterProgress.GetCompletedStepsCount();

        return (float)completed / totalSteps * 100f;
    }

    /// <summary>
    /// "이 Step이 완료되었는가?" = "해당 Step의 TestCompleted == true"
    /// </summary>
    public bool IsStepCompleted(string courseId, string chapterId, string stepId)
    {
        var sp = GetStepProgress(courseId, chapterId, stepId);
        return sp != null && sp.TestCompleted;
    }
}

// ============================================================================
// 🔹 Data Classes
// ============================================================================

[Serializable]
public class ProgressData
{
    public string CurrentCourseId;
    public string CurrentChapterId;
    public string CurrentStepId;
    public Dictionary<string, CourseProgress> Courses;

    // Newtonsoft.Json 역직렬화를 위한 기본 생성자
    public ProgressData() 
    {
        Courses = new Dictionary<string, CourseProgress>();
    }

    public ProgressData(ProgressManager mgr)
    {
        CurrentCourseId = mgr.CurrentCourseId;
        CurrentChapterId = mgr.CurrentChapterId;
        CurrentStepId = mgr.CurrentStepId;
        Courses = mgr.Courses;
    }
}

[Serializable]
public class CourseProgress
{
    public string CourseId;
    public Dictionary<string, ChapterProgress> Chapters = new();

    // Last known position in this course
    public string LastChapterId;
    public string LastStepId;

    // Newtonsoft.Json 역직렬화를 위한 기본 생성자
    public CourseProgress() 
    {
        Chapters = new Dictionary<string, ChapterProgress>();
    }

    public CourseProgress(string courseId)
    {
        CourseId = courseId;
        Chapters = new Dictionary<string, ChapterProgress>();
    }

    /// <summary>
    /// "Step 완료"는 결국 "해당 Step의 TestCompleted = true"를 의미한다.
    /// 실제 StepProgress 생성/갱신은 ChapterProgress 내부에서 처리된다.
    /// </summary>
    public void MarkStepComplete(string chapterId, string stepId)
    {
        if (!Chapters.ContainsKey(chapterId))
            Chapters[chapterId] = new ChapterProgress(chapterId);

        var chapterProgress = Chapters[chapterId];
        var stepProgress = chapterProgress.GetOrCreateStepProgress(stepId);
        stepProgress.TestCompleted = true;

        LastChapterId = chapterId;
        LastStepId = stepId;
    }
}

[Serializable]
public class ChapterProgress
{
    public string ChapterId;

    // 🔹 StepId 별 상세 StageProgress
    public Dictionary<string, StepProgress> Steps = new();

    // Newtonsoft.Json 역직렬화를 위한 기본 생성자
    public ChapterProgress() 
    {
        Steps = new Dictionary<string, StepProgress>();
    }

    public ChapterProgress(string chapterId)
    {
        ChapterId = chapterId;
        Steps = new Dictionary<string, StepProgress>();
    }

    public StepProgress GetOrCreateStepProgress(string stepId)
    {
        if (!Steps.TryGetValue(stepId, out var sp))
        {
            sp = new StepProgress(stepId);
            Steps[stepId] = sp;
        }

        return sp;
    }

    /// <summary>
    /// "완료된 Step 수" = TestCompleted == true 인 Step 수
    /// </summary>
    public int GetCompletedStepsCount()
    {
        int count = 0;
        foreach (var kvp in Steps)
        {
            var sp = kvp.Value;
            if (sp != null && sp.TestCompleted)
            {
                count++;
            }
        }
        return count;
    }
}

[Serializable]
public class StepProgress
{
    public string StepId;

    public bool LearnCompleted;
    public bool SingalongCompleted;
    public bool Game1Completed;
    public bool Game2Completed;
    public bool TestCompleted;

    // Practice 중 하나라도 완료되었는지
    public bool IsAnyPracticeCompleted =>
        SingalongCompleted || Game1Completed || Game2Completed;

    // Newtonsoft.Json 역직렬화를 위한 기본 생성자
    public StepProgress() { }

    public StepProgress(string stepId)
    {
        StepId = stepId;
    }
}

// ============================================================================
// 🔹 테스트 완료 데이터 (List 기반 - JsonUtility 호환)
// ============================================================================

[Serializable]
public class TestCompletedData
{
    public List<CompletedStepEntry> completedSteps = new List<CompletedStepEntry>();
}

[Serializable]
public class CompletedStepEntry
{
    public string courseId;
    public string chapterId;
    public string stepId;
}
