using UnityEngine;

[System.Serializable]
public class StepList
{
    public string chapterId;
    public StepData[] steps;
}

[System.Serializable]
public class StepData
{
    public string id;
    public string title;
    public string sentence;
    
    // 레거시 호환 - 단일 트랙 (보컬+악기 합본)
    public string songFile;
    
    // 분리 트랙 재생용
    public string vocalFile;        // 보컬 트랙 (예: step_001_vocal.mp3)
    public string instrumentalFile; // 악기/MR 트랙 (예: step_001_inst.mp3)
    
    public string lyricsFile;
    public string roleFile;
    public string testFile;
    public bool unlocked;
}