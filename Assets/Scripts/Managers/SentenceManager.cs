using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 문장 학습 데이터 관리
/// - Wrong Sentences (테스트 실패 문장)
/// - Saved Sentences (저장한 문장)
/// </summary>
public class SentenceManager : MonoBehaviour
{
    private static SentenceManager instance;
    public static SentenceManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("SentenceManager");
                instance = go.AddComponent<SentenceManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private const string WRONG_SENTENCES_KEY = "wrong_sentences";
    private const string SAVED_SENTENCES_KEY = "saved_sentences";
    
    private SentenceDataWrapper wrongSentencesData;
    private SavedSentenceDataWrapper savedSentencesData;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadData();
    }

    #region Wrong Sentences (테스트 실패 문장)

    /// <summary>
    /// 테스트 시도 기록
    /// </summary>
    public void RecordAttempt(string id, string sentence, string translation, bool isCorrect)
    {
        var progress = wrongSentencesData.sentences.Find(s => s.sentenceId == id);
        
        if (progress == null)
        {
            // 처음 등장하는 문장인데 맞혔다면 저장할 필요 없음 (틀린 문장 관리 대상 아님)
            if (isCorrect) return;

            progress = new SentenceProgress
            {
                sentenceId = id,
                sentence = sentence,
                translation = translation,
                lastAttempt = DateTime.Now,
                attemptCount = 0,
                successCount = 0,
                consecutiveSuccess = 0,
                isMastered = false
            };
            wrongSentencesData.sentences.Add(progress);
        }

        // 이미 마스터된 문장은 기록 갱신 안 함
        if (progress.isMastered) return;

        progress.attemptCount++;
        if (isCorrect)
        {
            progress.successCount++;
            progress.consecutiveSuccess++;
            
            // 3회 연속 성공 시 마스터 (목록에서 영구 제외)
            if (progress.consecutiveSuccess >= 3)
            {
                progress.isMastered = true;
                Debug.Log($"[SentenceManager] Mastered: {sentence}");
            }
        }
        else
        {
            progress.consecutiveSuccess = 0; // 흐름 끊기면 초기화
        }

        progress.accuracy = (float)progress.successCount / progress.attemptCount * 100f;
        progress.lastAttempt = DateTime.Now;

        SaveData();
    }

    /// <summary>
    /// 틀린 문장 목록 가져오기 (마스터 안 되었고, 틀린 기록이 있는 것만)
    /// </summary>
    public List<SentenceProgress> GetWrongSentences()
    {
        return wrongSentencesData.sentences
            .Where(p => !p.isMastered && p.accuracy < 100f)
            .OrderBy(p => p.accuracy)
            .ThenByDescending(p => p.lastAttempt)
            .ToList();
    }

    /// <summary>
    /// 특정 문장의 진행 상황 가져오기
    /// </summary>
    public SentenceProgress GetProgress(string sentenceId)
    {
        return wrongSentencesData.sentences.Find(s => s.sentenceId == sentenceId);
    }

    /// <summary>
    /// 수동으로 마스터 처리
    /// </summary>
    public void MarkAsMastered(string sentenceId)
    {
        var progress = wrongSentencesData.sentences.Find(s => s.sentenceId == sentenceId);
        if (progress != null)
        {
            progress.isMastered = true;
            SaveData();
        }
    }

    /// <summary>
    /// 오래된 마스터 문장 삭제 (30일 이상)
    /// </summary>
    public void CleanupOldMasteredSentences()
    {
        var threshold = DateTime.Now.AddDays(-30).Ticks;
        wrongSentencesData.sentences.RemoveAll(s => 
            s.isMastered && s.lastAttemptTicks < threshold);
        SaveData();
    }

    #endregion

    #region Saved Sentences (저장한 문장)

    /// <summary>
    /// 문장 저장
    /// </summary>
    public void SaveSentence(string sentenceId, string sentence, string translation, string note = "")
    {
        if (savedSentencesData.sentences.Any(s => s.sentenceId == sentenceId))
        {
            Debug.Log($"[SentenceManager] Already saved: {sentence}");
            return;
        }

        var saved = new SavedSentence
        {
            sentenceId = sentenceId,
            sentence = sentence,
            translation = translation,
            savedDateTicks = DateTime.Now.Ticks,
            note = note
        };

        savedSentencesData.sentences.Add(saved);
        SaveData();
    }

    /// <summary>
    /// 저장한 문장 제거
    /// </summary>
    public void RemoveSavedSentence(string sentenceId)
    {
        savedSentencesData.sentences.RemoveAll(s => s.sentenceId == sentenceId);
        SaveData();
    }

    /// <summary>
    /// 저장한 문장 목록 가져오기
    /// </summary>
    public List<SavedSentence> GetSavedSentences()
    {
        return savedSentencesData.sentences
            .OrderByDescending(s => s.savedDateTicks)
            .ToList();
    }

    /// <summary>
    /// 문장이 저장되어 있는지 확인
    /// </summary>
    public bool IsSaved(string sentenceId)
    {
        return savedSentencesData.sentences.Any(s => s.sentenceId == sentenceId);
    }

    #endregion

    #region Data Persistence

    private void LoadData()
    {
        string wrongJson = PlayerPrefs.GetString(WRONG_SENTENCES_KEY, "");
        wrongSentencesData = !string.IsNullOrEmpty(wrongJson) 
            ? JsonUtility.FromJson<SentenceDataWrapper>(wrongJson) 
            : new SentenceDataWrapper();

        string savedJson = PlayerPrefs.GetString(SAVED_SENTENCES_KEY, "");
        savedSentencesData = !string.IsNullOrEmpty(savedJson)
            ? JsonUtility.FromJson<SavedSentenceDataWrapper>(savedJson)
            : new SavedSentenceDataWrapper();
    }

    private void SaveData()
    {
        try
        {
            PlayerPrefs.SetString(WRONG_SENTENCES_KEY, JsonUtility.ToJson(wrongSentencesData));
            PlayerPrefs.SetString(SAVED_SENTENCES_KEY, JsonUtility.ToJson(savedSentencesData));
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SentenceManager] Failed to save: {e.Message}");
        }
    }

    public void ClearAllData()
    {
        wrongSentencesData.sentences.Clear();
        savedSentencesData.sentences.Clear();
        SaveData();
    }

    #endregion
}

#region Data Structures

[Serializable]
public class SentenceProgress
{
    public string sentenceId;
    public string sentence;
    public string translation;
    public int attemptCount;
    public int successCount;
    public int consecutiveSuccess;
    public float accuracy;
    public long lastAttemptTicks; // JsonUtility는 DateTime을 직접 지원하지 않으므로 Ticks로 저장
    
    public DateTime lastAttempt 
    { 
        get => new DateTime(lastAttemptTicks); 
        set => lastAttemptTicks = value.Ticks; 
    }
    
    public bool isMastered;
}

[Serializable]
public class SentenceDataWrapper
{
    public List<SentenceProgress> sentences = new List<SentenceProgress>();
}

[Serializable]
public class SavedSentence
{
    public string sentenceId;
    public string sentence;
    public string translation;
    public long savedDateTicks;
    
    public DateTime savedDate 
    { 
        get => new DateTime(savedDateTicks); 
        set => savedDateTicks = value.Ticks; 
    }
    
    public string note;
}

[Serializable]
public class SavedSentenceDataWrapper
{
    public List<SavedSentence> sentences = new List<SavedSentence>();
}

#endregion
