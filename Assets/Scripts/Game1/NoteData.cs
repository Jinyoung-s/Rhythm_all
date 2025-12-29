using System.Collections.Generic;

[System.Serializable]
public class NoteData
{
    public string word;
    public float startTime;  // Legacy field for backward compatibility
    public float start;      // Used in lyrics JSON files
    public float end;        // Used in lyrics JSON files
    public string buttonBg;  // 추가
}

[System.Serializable]
public class NoteDataList
{
    public List<NoteData> words;
}
