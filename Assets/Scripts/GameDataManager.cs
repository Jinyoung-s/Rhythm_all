using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    private static GameDataManager _instance;
    public static GameDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var obj = new GameObject("GameDataManager");
                _instance = obj.AddComponent<GameDataManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    public StepData CurrentStep { get; set; }
    public string CurrentCourseId { get; set; }
    public string CurrentChapterId { get; set; }

    public void SetContext(string courseId, string chapterId, StepData step)
    {
        CurrentCourseId = courseId;
        CurrentChapterId = chapterId;
        CurrentStep = step;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}