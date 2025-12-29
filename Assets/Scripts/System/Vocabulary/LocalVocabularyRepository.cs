using System;
using System.IO;
using UnityEngine;   

public class LocalVocabularyRepository : IVocabularyRepository
{
    private readonly string filePath;

    public LocalVocabularyRepository()
    {
        filePath = Path.Combine(
            Application.persistentDataPath,
            "user_vocabulary.json"
        );
    }

    public UserVocabulary Load()
    {
        if (!File.Exists(filePath))
        {
            Debug.Log("[Vocabulary] No local vocabulary file. Create new.");
            return new UserVocabulary();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<UserVocabulary>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Vocabulary] Load failed: {e}");
            return new UserVocabulary();
        }
    }

    public void Save(UserVocabulary data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"[VocabularyRepository] ✅ Saved to: {filePath}");
            Debug.Log($"[VocabularyRepository] Word count: {data.words?.Count ?? 0}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Vocabulary] Save failed: {e}");
        }
    }
}
