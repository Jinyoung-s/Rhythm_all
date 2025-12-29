using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LearnStepData
{
    public string stepId;
    public string chapterId;
    public string title;
    public string sentence;
    public string translation;
    public string audioUrl;
    public string grammarNote;
    public List<ExampleData> examples;

    public List<HighlightData> highlights;
}

[System.Serializable]
public class LearnStepDataList
{
    public List<LearnStepData> steps;
}

[System.Serializable]
public class HighlightData
{
    public string text;   // 강조할 문자열
    public string color;  // HEX 컬러
}

[System.Serializable]
public class ExampleData
{
    public string sentence;
    public string translation;
}