using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 정적 단어 사전 (Static Dictionary)
/// - 단어 존재 여부 판단
/// - 단어 메타 정보 제공
/// </summary>
public class VocabularyMaster
{
    // key: normalized word
    private Dictionary<string, WordInfo> wordMap =
        new Dictionary<string, WordInfo>();

    public VocabularyMaster()
    {
        Load();
    }

    /// <summary>
    /// 정적 사전 로드
    /// </summary>
    private void Load()
    {
        wordMap.Clear();

        // 🔹 Resources/Vocabulary/vocabulary_master.json
        TextAsset asset = Resources.Load<TextAsset>(
            "Vocabulary/vocabulary_master"
        );

        if (asset == null)
        {
            Debug.LogWarning("[VocabularyMaster] vocabulary_master.json not found. All words will be ignored.");
            return;
        }

        VocabularyMasterData data =
            JsonUtility.FromJson<VocabularyMasterData>(asset.text);

        if (data == null || data.words == null)
        {
            Debug.LogWarning("[VocabularyMaster] Invalid vocabulary_master.json format.");
            return;
        }

        foreach (var info in data.words)
        {
            if (string.IsNullOrEmpty(info.word))
                continue;

            string key = Normalize(info.word);
            if (!wordMap.ContainsKey(key))
            {
                wordMap.Add(key, info);
            }
        }

        Debug.Log($"[VocabularyMaster] Loaded {wordMap.Count} words.");
    }

    /// <summary>
    /// 사전에 존재하는 단어인지
    /// </summary>
    public bool Contains(string word)
    {
        if (string.IsNullOrEmpty(word))
            return false;

        return wordMap.ContainsKey(Normalize(word));
    }

    /// <summary>
    /// 단어 정보 조회
    /// </summary>
    public WordInfo Get(string word)
    {
        if (string.IsNullOrEmpty(word))
            return null;

        wordMap.TryGetValue(Normalize(word), out var info);
        return info;
    }

    private string Normalize(string word)
    {
        return word.Trim().ToLowerInvariant();
    }
    
    /// <summary>
    /// 사전의 모든 단어 반환 (Fallback용)
    /// </summary>
    public List<string> GetAllWords()
    {
        return new List<string>(wordMap.Keys);
    }
}
