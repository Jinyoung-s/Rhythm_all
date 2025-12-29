using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Vocabulary 시스템의 단일 진입점 (Single Source of Truth)
/// - 단어 수집 기준: 실제 플레이에 사용된 NoteData
/// - 저장 방식: Repository 패턴
/// </summary>
public class VocabularyManager
{
    // -------------------------
    // Singleton
    // -------------------------
    private static VocabularyManager _instance;
    public static VocabularyManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new VocabularyManager();
            return _instance;
        }
    }

    // -------------------------
    // Core Dependencies
    // -------------------------
    private IVocabularyRepository repository;
    private VocabularyMaster master;
    private UserVocabulary userVocabulary;


    // -------------------------
    // Constructor (private)
    // -------------------------
    private VocabularyManager()
    {
        // 🔹 저장소 (현재는 로컬 JSON)
        repository = new LocalVocabularyRepository();

        // 🔹 정적 사전
        master = new VocabularyMaster();

        // 🔹 유저 Vocabulary 로드
        userVocabulary = repository.Load();
        if (userVocabulary == null)
        {
            userVocabulary = new UserVocabulary();
        }
    }

    // =========================================================
    // Public API
    // =========================================================

    /// <summary>
    /// 🔁 스텝(가사 플레이) 완료 시 호출
    /// 실제 플레이에 사용된 NoteData 기준으로 단어 수집
    /// </summary>
    public void OnLyricsCompleted(IEnumerable<NoteData> notes)
    {
        if (notes == null)
        {
            Debug.LogWarning("[VocabularyManager] OnLyricsCompleted: notes is null");
            return;
        }

        HashSet<string> uniqueWords = ExtractUniqueWords(notes);
        Debug.Log($"[VocabularyManager] Extracted {uniqueWords.Count} unique words");
        
        int newWordCount = 0;
        int existingWordCount = 0;
        int skippedWordCount = 0;

        foreach (string word in uniqueWords)
        {
            // 🔒 정적 사전에 없는 단어는 무시
            if (!master.Contains(word))
            {
                skippedWordCount++;
                Debug.LogWarning($"[VocabularyManager] ⚠️ Skipped '{word}' - not in master dictionary");
                continue;
            }

            if (!userVocabulary.HasWord(word))
            {
                // 신규 단어
                userVocabulary.AddNewWord(word);
                newWordCount++;
                Debug.Log($"[VocabularyManager] ✅ Added new word: '{word}'");
            }
            else
            {
                // 이미 학습한 단어
                userVocabulary.IncreaseSeenCount(word);
                existingWordCount++;
                Debug.Log($"[VocabularyManager] 🔄 Existing word: '{word}'");
            }
        }
        
        Debug.Log($"[VocabularyManager] Summary: {newWordCount} new, {existingWordCount} existing, {skippedWordCount} skipped");
        Debug.Log($"[VocabularyManager] Total words in vocabulary: {userVocabulary.words.Count}");

        repository.Save(userVocabulary);
        Debug.Log("[VocabularyManager] ✅ Vocabulary saved to repository");
    }

    /// <summary>
    /// 🧪 Word Test 출제 후보 반환
    /// (memoryScore 낮은 순)
    /// </summary>
    public List<string> GetTestCandidates(int maxCount)
    {
        if (userVocabulary == null)
            return new List<string>();

        return userVocabulary
            .GetWordsSortedByMemory()
            .Take(maxCount)
            .ToList();
    }

    /// <summary>
    /// 🧠 테스트 결과 반영
    /// </summary>
    public void ApplyTestResult(string word, bool correct)
    {
        if (string.IsNullOrEmpty(word))
            return;

        word = NormalizeWord(word);

        if (!userVocabulary.HasWord(word))
            return;

        if (correct)
            userVocabulary.IncreaseMemory(word);
        else
            userVocabulary.DecreaseMemory(word);

        repository.Save(userVocabulary);
    }

    /// <summary>
    /// 📘 단어의 정적 사전 정보 조회
    /// </summary>
    public WordInfo GetWordInfo(string word)
    {
        if (string.IsNullOrEmpty(word))
            return null;

        word = NormalizeWord(word);
        return master.Get(word);
    }

    /// <summary>
    /// 📊 유저가 학습한 전체 단어 수
    /// </summary>
    public int GetLearnedWordCount()
    {
        return userVocabulary?.words?.Count ?? 0;
    }

    /// <summary>
    /// 📝 스텝 테스트 완료 시 단어 등록
    /// </summary>
    public void RegisterStepCompletion(string chapterId, string stepId)
    {
        Debug.Log($"[VocabularyManager] RegisterStepCompletion called: chapterId='{chapterId}', stepId='{stepId}'");
        
        if (string.IsNullOrEmpty(chapterId) || string.IsNullOrEmpty(stepId))
        {
            Debug.LogWarning("[VocabularyManager] RegisterStepCompletion: Invalid chapterId or stepId.");
            return;
        }

        // StepData 로드
        var step = GetStepData(chapterId, stepId);
        if (step == null)
        {
            Debug.LogWarning($"[VocabularyManager] Step '{stepId}' not found in chapter '{chapterId}'.");
            return;
        }
        
        Debug.Log($"[VocabularyManager] Step data loaded: {step.id}");

        // NoteData 로드
        var notes = LoadNotesFromStep(chapterId, step);
        
        if (notes == null)
        {
            Debug.LogWarning($"[VocabularyManager] Notes is null for {chapterId}/{stepId}");
            return;
        }
        
        int noteCount = notes.Count();
        Debug.Log($"[VocabularyManager] Loaded {noteCount} notes from lyrics");
        
        // 기존 OnLyricsCompleted 호출하여 단어 수집
        OnLyricsCompleted(notes);
        
        Debug.Log($"[VocabularyManager] ✅ Registered step completion: {chapterId}/{stepId}");
    }

    /// <summary>
    /// 🎯 오답 선택지 생성 (같은 품사 기반)
    /// </summary>
    public List<string> GetDistractors(string word, int count = 3)
    {
        List<string> distractors = new List<string>();
        
        if (string.IsNullOrEmpty(word))
            return distractors;

        word = NormalizeWord(word);
        var wordInfo = master.Get(word);
        
        if (wordInfo == null)
            return distractors;

        // 학습한 단어 중에서 같은 품사인 단어들을 후보로
        var candidates = userVocabulary.words
            .Where(w => w.word != word)
            .Select(w => w.word)
            .Where(w => 
            {
                var info = master.Get(w);
                return info != null && info.partOfSpeech == wordInfo.partOfSpeech;
            })
            .OrderBy(x => UnityEngine.Random.value)
            .Take(count)
            .ToList();

        distractors.AddRange(candidates);

        // 부족하면 다른 학습 단어로 채움
        if (distractors.Count < count)
        {
            var additionals = userVocabulary.words
                .Where(w => w.word != word && !distractors.Contains(w.word))
                .Select(w => w.word)
                .OrderBy(x => UnityEngine.Random.value)
                .Take(count - distractors.Count)
                .ToList();
            
            distractors.AddRange(additionals);
        }
        
        // 여전히 부족하면 VocabularyMaster의 모든 단어에서 가져오기 (Fallback)
        if (distractors.Count < count)
        {
            Debug.Log($"[VocabularyManager] Not enough learned words for distractors. Using fallback from master dictionary.");
            
            var allMasterWords = master.GetAllWords();
            var fallbackWords = allMasterWords
                .Where(w => w != word && !distractors.Contains(w))
                .Where(w =>
                {
                    var info = master.Get(w);
                    return info != null && info.partOfSpeech == wordInfo.partOfSpeech;
                })
                .OrderBy(x => UnityEngine.Random.value)
                .Take(count - distractors.Count)
                .ToList();
            
            distractors.AddRange(fallbackWords);
            
            // 같은 품사가 부족하면 아무 품사나 사용
            if (distractors.Count < count)
            {
                var anyWords = allMasterWords
                    .Where(w => w != word && !distractors.Contains(w))
                    .OrderBy(x => UnityEngine.Random.value)
                    .Take(count - distractors.Count)
                    .ToList();
                
                distractors.AddRange(anyWords);
            }
        }

        return distractors;
    }

    /// <summary>
    /// 📚 학습한 모든 단어 목록 반환
    /// </summary>
    public List<string> GetAllLearnedWords()
    {
        if (userVocabulary == null || userVocabulary.words == null)
            return new List<string>();

        return userVocabulary.words.Select(w => w.word).ToList();
    }

    /// <summary>
    /// 📋 유저 단어 데이터 전체 반환 (UI용)
    /// </summary>
    public List<UserWordData> GetAllUserWordData()
    {
        if (userVocabulary == null)
            return new List<UserWordData>();
            
        return userVocabulary.words;
    }

    /// <summary>
    /// ✨ 단어 암기 상태 수동 토글
    /// </summary>
    public void SetWordMastery(string word, bool mastered)
    {
        if (string.IsNullOrEmpty(word)) return;
        
        word = NormalizeWord(word);
        if (userVocabulary.HasWord(word))
        {
            userVocabulary.SetMastered(word, mastered);
            repository.Save(userVocabulary);
            Debug.Log($"[VocabularyManager] Set '{word}' mastery to {mastered}");
        }
    }

    // =========================================================
    // Internal Utilities
    // =========================================================

    /// <summary>
    /// 특정 스텝의 StepData 가져오기
    /// </summary>
    private StepData GetStepData(string chapterId, string stepId)
    {
        if (!CurriculumRepository.TryGetChapter(chapterId, out var chapter))
        {
            Debug.LogWarning($"[VocabularyManager] Chapter '{chapterId}' not found.");
            return null;
        }

        var step = chapter.Steps?.FirstOrDefault(s => s.id == stepId);
        
        if (step == null)
        {
            Debug.LogWarning($"[VocabularyManager] Step '{stepId}' not found in chapter '{chapterId}'.");
        }

        return step;
    }

    /// <summary>
    /// 특정 스텝의 lyrics JSON에서 NoteData 로드
    /// </summary>
    private IEnumerable<NoteData> LoadNotesFromStep(string chapterId, StepData step)
    {
        if (step == null)
            return Enumerable.Empty<NoteData>();

        // 1. Try loading lyrics from step's lyricsFile property
        var lyricsAsset = StepResourceResolver.LoadLyricsAsset(chapterId, step);
        
        if (lyricsAsset != null)
        {
            try
            {
                // Lyrics files are in array format: [{word, start, end}, ...]
                // Use Newtonsoft.Json for proper array parsing
                var lyricsArray = Newtonsoft.Json.JsonConvert.DeserializeObject<List<NoteData>>(lyricsAsset.text);
                if (lyricsArray != null && lyricsArray.Count > 0)
                {
                    Debug.Log($"[VocabularyManager] ✅ Loaded {lyricsArray.Count} words from lyrics file: {step.lyricsFile}");
                    return lyricsArray;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VocabularyManager] Failed to parse lyrics JSON: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[VocabularyManager] Lyrics asset not found for step: {step.id}, lyricsFile: {step.lyricsFile}");
        }
        
        // 2. Fallback: Extract words from Test JSON
        Debug.Log($"[VocabularyManager] No lyrics found, trying test JSON fallback...");
        return LoadWordsFromTestJson(chapterId, step);
    }
    
    /// <summary>
    /// Test JSON에서 단어 추출 (Fallback)
    /// </summary>
    private IEnumerable<NoteData> LoadWordsFromTestJson(string chapterId, StepData step)
    {
        var testAsset = StepResourceResolver.LoadTestAsset(chapterId, step);
        if (testAsset == null)
        {
            Debug.LogWarning($"[VocabularyManager] Test JSON also not found");
            return Enumerable.Empty<NoteData>();
        }
        
        try
        {
            var testData = JsonUtility.FromJson<TestData>(testAsset.text);
            if (testData == null || testData.items == null)
            {
                return Enumerable.Empty<NoteData>();
            }
            
            List<NoteData> notes = new List<NoteData>();
            foreach (var item in testData.items)
            {
                if (item.correctOrder != null)
                {
                    foreach (var word in item.correctOrder)
                    {
                        notes.Add(new NoteData
                        {
                            word = word,
                            startTime = 0f
                        });
                    }
                }
            }
            
            Debug.Log($"[VocabularyManager] Extracted {notes.Count} words from test JSON");
            return notes;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VocabularyManager] Failed to parse test JSON: {ex.Message}");
            return Enumerable.Empty<NoteData>();
        }
    }
    
    [System.Serializable]
    private class NoteDataArray
    {
        public List<NoteData> items;
    }

    /// <summary>
    /// NoteData 목록에서 중복 제거된 단어 Set 추출
    /// </summary>
    private HashSet<string> ExtractUniqueWords(IEnumerable<NoteData> notes)
    {
        HashSet<string> set = new HashSet<string>();

        foreach (var note in notes)
        {
            if (note == null || string.IsNullOrEmpty(note.word))
                continue;

            string normalized = NormalizeWord(note.word);
            if (string.IsNullOrEmpty(normalized))
                continue;

            // 🔥 기능어 제거
            if (FunctionWords.Contains(normalized))
                continue;

            set.Add(normalized);
        }

        return set;
    }

    /// <summary>
    /// 단어 정규화 (공백 제거 + 소문자)
    /// </summary>
    private string NormalizeWord(string word)
    {
        return word.Trim().ToLowerInvariant();
    }

    private static readonly HashSet<string> FunctionWords =
        new HashSet<string>
    {
        "i", "you", "he", "she", "it", "we", "they",
        "me", "him", "her", "us", "them",
        "my", "your", "his", "her", "its", "our", "their",
        "mine", "yours", "hers", "ours", "theirs",
        "the", "a", "an",
        "and", "or", "but",
        "to", "of", "in", "on", "at", "for", "with", "from",
        "is", "am", "are", "was", "were", "be", "been", "being",
        "do", "does", "did",
        "this", "that", "these", "those"
    };    
}
