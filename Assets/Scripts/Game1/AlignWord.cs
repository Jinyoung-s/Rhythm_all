using System.Collections.Generic;

[System.Serializable]
public class AlignWord
{
    public float start;
    public float end;
    public string word;
}

[System.Serializable]
public class AlignData
{
    public List<AlignWord> words;
}
