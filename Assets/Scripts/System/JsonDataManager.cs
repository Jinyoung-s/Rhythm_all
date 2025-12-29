using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;

[System.Serializable]
public class UserData
{
    public string currentClass;
    public string currentStep;
    public string currentChapter;

    public List<IncorrectInformation> incorrectInfo;
    public int score;
}

[System.Serializable]
public class IncorrectInformation
{
    public List<int> incorrectIndexes = new List<int>();
    public string incorrectStep;
    public string incorrectChapter;
    public int correctCount;
}


public class JsonDataManager
{
    private string filePath;

    public JsonDataManager(string fileName = "userdata.json")
    {
        filePath = Path.Combine(Application.persistentDataPath, fileName);
    }

    public void SaveUserData(UserData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
    }

    public UserData LoadUserData()
    {
        if (!File.Exists(filePath))
        {
            return new UserData
            {
                currentClass = "",
                currentStep = "",
                incorrectInfo = new List<IncorrectInformation>(), // 
                score = 0   
            };
        }
        string json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<UserData>(json);
    }
}