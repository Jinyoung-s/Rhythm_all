using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[System.Serializable]
public class CourseDataList
{
    public CourseData[] Courses;
}
[System.Serializable]
public class CourseData
{
    public string id;
    public string name;
    public string description;
    public ChapterData[] chapters;
}
[System.Serializable]
public class ChapterData
{
    public string id;
    public string name;
    public bool unlockedByDefault;
    public string[] nextChapterIds;
    public int lessonCount;
}


public static class CurriculumRepository
{
    private static readonly object SyncRoot = new object();

    private static bool _initialized;
    private static string _courseDataFile = "courses_data";
    private static string _stepFolder = "steps";

    private static List<CurriculumCourse> _courses = new List<CurriculumCourse>();
    private static Dictionary<string, CurriculumCourse> _coursesById = new Dictionary<string, CurriculumCourse>();
    private static Dictionary<string, CurriculumChapter> _chaptersById = new Dictionary<string, CurriculumChapter>();
    private static CourseDataList _rawCourseData;

    /// <summary>
    /// Override the default resource paths before the first access.
    /// </summary>
    public static void Configure(string courseDataFile = null, string stepFolder = null)
    {
        lock (SyncRoot)
        {
            if (_initialized)
            {
                bool configurationDiffers =
                    (!string.IsNullOrEmpty(courseDataFile) && courseDataFile != _courseDataFile) ||
                    (!string.IsNullOrEmpty(stepFolder) && stepFolder != _stepFolder);

                if (configurationDiffers)
                {
                    Debug.LogWarning("[CurriculumRepository] Configure called after initialization. Configuration change ignored.");
                }

                return;
            }

            if (!string.IsNullOrEmpty(courseDataFile))
            {
                _courseDataFile = courseDataFile;
            }

            if (!string.IsNullOrEmpty(stepFolder))
            {
                _stepFolder = stepFolder;
            }
        }
    }

    public static IReadOnlyList<CurriculumCourse> Courses
    {
        get
        {
            EnsureInitialized();
            return _courses;
        }
    }

    public static bool TryGetCourse(string courseId, out CurriculumCourse course)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(courseId))
        {
            course = null;
            return false;
        }

        return _coursesById.TryGetValue(courseId, out course);
    }

    public static bool TryGetChapter(string chapterId, out CurriculumChapter chapter)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(chapterId))
        {
            chapter = null;
            return false;
        }

        return _chaptersById.TryGetValue(chapterId, out chapter);
    }

    public static IReadOnlyList<StepData> GetStepsForChapter(string chapterId)
    {
        return TryGetChapter(chapterId, out var chapter)
            ? chapter.Steps
            : Array.Empty<StepData>();
    }

    /// <summary>
    /// Legacy accessor so existing systems can still retrieve the raw model.
    /// </summary>
    public static CourseDataList GetRawCourseData()
    {
        EnsureInitialized();
        return _rawCourseData;
    }

    public static CurriculumCourse GetFirstCourseOrDefault()
    {
        EnsureInitialized();
        return _courses.Count > 0 ? _courses[0] : null;
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_initialized)
            {
                return;
            }

            BuildCache();
            _initialized = true;
        }
    }

    private static void BuildCache()
    {
        _courses = new List<CurriculumCourse>();
        _coursesById = new Dictionary<string, CurriculumCourse>();
        _chaptersById = new Dictionary<string, CurriculumChapter>();
        _rawCourseData = new CourseDataList { Courses = Array.Empty<CourseData>() };

        TextAsset jsonAsset = Resources.Load<TextAsset>(_courseDataFile);
        if (jsonAsset == null)
        {
            Debug.LogError($"[CurriculumRepository] Resources/{_courseDataFile}.json not found.");
            return;
        }

        try
        {
            _rawCourseData = JsonUtility.FromJson<CourseDataList>(jsonAsset.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CurriculumRepository] Failed to parse {_courseDataFile}.json: {ex.Message}");
            _rawCourseData = new CourseDataList { Courses = Array.Empty<CourseData>() };
            return;
        }

        if (_rawCourseData?.Courses == null)
        {
            Debug.LogError("[CurriculumRepository] Parsed course data is null.");
            _rawCourseData = new CourseDataList { Courses = Array.Empty<CourseData>() };
            return;
        }

        foreach (var courseData in _rawCourseData.Courses)
        {
            if (courseData == null || string.IsNullOrEmpty(courseData.id))
            {
                Debug.LogWarning("[CurriculumRepository] Encountered invalid course entry; skipping.");
                continue;
            }

            var course = new CurriculumCourse(courseData, _stepFolder);
            _courses.Add(course);
            _coursesById[course.Id] = course;

            foreach (var chapter in course.Chapters)
            {
                if (_chaptersById.ContainsKey(chapter.Id))
                {
                    Debug.LogWarning($"[CurriculumRepository] Duplicate chapter id '{chapter.Id}' detected. Overwriting previous entry.");
                }

                _chaptersById[chapter.Id] = chapter;
            }
        }
    }
}

public sealed class CurriculumCourse
{
    private readonly ReadOnlyCollection<CurriculumChapter> _chapters;

    internal CurriculumCourse(CourseData source, string stepFolder)
    {
        Id = source.id;
        Name = source.name;
        Description = source.description;

        var chapterList = new List<CurriculumChapter>();
        if (source.chapters != null)
        {
            foreach (var chapter in source.chapters)
            {
                if (chapter == null)
                {
                    continue;
                }

                chapterList.Add(new CurriculumChapter(this, chapter, stepFolder));
            }
        }

        _chapters = new ReadOnlyCollection<CurriculumChapter>(chapterList);
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }

    public IReadOnlyList<CurriculumChapter> Chapters => _chapters;

    public override string ToString()
    {
        return $"{Id} ({Name})";
    }
}

public sealed class CurriculumChapter
{
    private readonly ReadOnlyCollection<string> _nextChapterIds;
    private readonly ReadOnlyCollection<StepData> _steps;

    internal CurriculumChapter(CurriculumCourse course, ChapterData source, string stepFolder)
    {
        Course = course;
        Id = source.id;
        Name = source.name;
        UnlockedByDefault = source.unlockedByDefault;
        LessonCount = source.lessonCount;

        var nextIds = source.nextChapterIds != null
            ? new List<string>(source.nextChapterIds)
            : new List<string>();
        _nextChapterIds = new ReadOnlyCollection<string>(nextIds);

        _steps = new ReadOnlyCollection<StepData>(LoadSteps(stepFolder));
    }

    public CurriculumCourse Course { get; }
    public string Id { get; }
    public string Name { get; }
    public bool UnlockedByDefault { get; }
    public int LessonCount { get; }

    public IReadOnlyList<string> NextChapterIds => _nextChapterIds;
    public IReadOnlyList<StepData> Steps => _steps;

    private List<StepData> LoadSteps(string stepFolder)
    {
        var result = new List<StepData>();

        if (string.IsNullOrEmpty(Id))
        {
            return result;
        }

        string resourcePath = $"{stepFolder}/{Id}_steps";
        TextAsset json = Resources.Load<TextAsset>(resourcePath);
        if (json == null)
        {
            Debug.LogWarning($"[CurriculumRepository] Steps data not found at Resources/{resourcePath}.json");
            return result;
        }

        StepList parsed;
        try
        {
            parsed = JsonUtility.FromJson<StepList>(json.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CurriculumRepository] Failed to parse steps for '{Id}': {ex.Message}");
            return result;
        }

        if (parsed?.steps == null)
        {
            return result;
        }

        foreach (var step in parsed.steps)
        {
            if (step == null || string.IsNullOrEmpty(step.id))
            {
                Debug.LogWarning($"[CurriculumRepository] Skipping invalid step entry in chapter '{Id}'.");
                continue;
            }

            result.Add(step);
        }

        return result;
    }

    public override string ToString()
    {
        return $"{Id} ({Name})";
    }
}
