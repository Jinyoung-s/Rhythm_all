using System;
using System.Collections.Generic;
using UnityEngine.Android;

[Serializable]
public class Prompt
{
    public string sourceLang;
    public string text;
}

[Serializable]
public class TestItem
{
    public string id;
    public string type;
    public Prompt prompt;

    // 항목별로 존재하지 않을 수 있으니 null 허용
    public List<string> wordBank;
    public List<string> correctOrder;

    // JSON에서 [["I'm","a","student"]] 형태를 지원하기 위해 중첩 리스트
    public List<List<string>> acceptedAlternatives;

    public Media media; // 오디오 및 트랜스크립트 정보

}

[Serializable]
public class TestData
{
    public List<TestItem> items;
}

public class Media
{
    public string audioRef;
    public string transcript;
}