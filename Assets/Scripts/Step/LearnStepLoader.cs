using UnityEngine;
using System.Collections.Generic;

public static class LearnStepLoader
{
    public static List<LearnStepData> LoadFromResources(string fileName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(fileName);
        if (jsonFile == null)
        {
            Debug.LogError($"{fileName}.json not found in Resources!");
            return null;
        }

        LearnStepDataList dataList = JsonUtility.FromJson<LearnStepDataList>(jsonFile.text);
        return dataList.steps;
    }
}