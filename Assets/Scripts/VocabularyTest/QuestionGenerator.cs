using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 단어 테스트 문제 생성기
/// VocabularyManager를 사용하여 4가지 타입의 문제를 생성합니다.
/// </summary>
public class QuestionGenerator
{
    private VocabularyManager vocabManager;
    private System.Random random;

    public QuestionGenerator()
    {
        vocabManager = VocabularyManager.Instance;
        random = new System.Random();
    }

    /// <summary>
    /// 여러 단어로부터 문제 세트 생성
    /// </summary>
    /// <param name="words">출제할 단어 목록</param>
    /// <param name="questionCount">생성할 문제 수</param>
    /// <returns>생성된 문제 리스트</returns>
    public List<VocabQuestion> GenerateQuestions(List<string> words, int questionCount = 10)
    {
        List<VocabQuestion> questions = new List<VocabQuestion>();

        if (words == null || words.Count == 0)
        {
            Debug.LogWarning("[QuestionGenerator] No words provided.");
            return questions;
        }

        // 단어 수가 부족하면 사용 가능한 만큼만 생성
        int actualCount = Mathf.Min(questionCount, words.Count);

        // 단어 섞기
        List<string> shuffledWords = words.OrderBy(x => random.Next()).ToList();

        // 각 단어에 대해 문제 생성
        for (int i = 0; i < actualCount; i++)
        {
            string word = shuffledWords[i];
            VocabQuestionType type = GetRandomQuestionType();
            VocabQuestion question = GenerateQuestion(word, type);

            if (question != null)
            {
                questions.Add(question);
            }
        }

        return questions;
    }

    /// <summary>
    /// 단일 단어로부터 문제 생성
    /// </summary>
    public VocabQuestion GenerateQuestion(string word, VocabQuestionType type)
    {
        if (string.IsNullOrEmpty(word))
        {
            Debug.LogWarning("[QuestionGenerator] Cannot generate question for empty word.");
            return null;
        }

        // 단어 정보 가져오기
        WordInfo wordInfo = vocabManager.GetWordInfo(word);
        if (wordInfo == null)
        {
            Debug.LogWarning($"[QuestionGenerator] WordInfo not found for '{word}'.");
            return null;
        }

        // 타입별 문제 생성
        switch (type)
        {
            case VocabQuestionType.ListenAndChoose:
                return GenerateListenAndChoose(word, wordInfo);
            
            case VocabQuestionType.MeaningToWord:
                return GenerateMeaningToWord(word, wordInfo);
            
            case VocabQuestionType.WordToMeaning:
                return GenerateWordToMeaning(word, wordInfo);
            
            case VocabQuestionType.Typing:
                return GenerateTyping(word, wordInfo);
            
            default:
                Debug.LogWarning($"[QuestionGenerator] Unknown question type: {type}");
                return null;
        }
    }

    // =========================================================
    // Question Type Generators
    // =========================================================

    /// <summary>
    /// 듣고 고르기: 음성을 듣고 올바른 단어 선택
    /// </summary>
    private VocabQuestion GenerateListenAndChoose(string word, WordInfo wordInfo)
    {
        VocabQuestion question = new VocabQuestion
        {
            word = word,
            type = VocabQuestionType.ListenAndChoose,
            correctAnswer = word
        };

        // 오답 선택지 3개 생성
        List<string> distractors = vocabManager.GetDistractors(word, 3);

        // 선택지 구성 (정답 + 오답 3개)
        question.options = new List<string> { word };
        question.options.AddRange(distractors);

        // 선택지 섞기
        question.options = question.options.OrderBy(x => random.Next()).ToList();

        // 정답 인덱스 찾기
        question.correctAnswerIndex = question.options.IndexOf(word);

        return question;
    }

    /// <summary>
    /// 뜻 → 단어: 한글 뜻을 보고 올바른 영어 단어 선택
    /// </summary>
    private VocabQuestion GenerateMeaningToWord(string word, WordInfo wordInfo)
    {
        VocabQuestion question = new VocabQuestion
        {
            word = word,
            type = VocabQuestionType.MeaningToWord,
            correctAnswer = word
        };

        // 오답 선택지 3개 생성
        List<string> distractors = vocabManager.GetDistractors(word, 3);

        // 선택지 구성 (정답 + 오답 3개)
        question.options = new List<string> { word };
        question.options.AddRange(distractors);

        // 선택지 섞기
        question.options = question.options.OrderBy(x => random.Next()).ToList();

        // 정답 인덱스 찾기
        question.correctAnswerIndex = question.options.IndexOf(word);

        return question;
    }

    /// <summary>
    /// 단어 → 뜻: 영어 단어를 보고 올바른 한글 뜻 선택
    /// </summary>
    private VocabQuestion GenerateWordToMeaning(string word, WordInfo wordInfo)
    {
        VocabQuestion question = new VocabQuestion
        {
            word = word,
            type = VocabQuestionType.WordToMeaning,
            correctAnswer = wordInfo.meaning
        };

        // 오답 선택지: 다른 단어들의 뜻 3개
        List<string> distractorWords = vocabManager.GetDistractors(word, 3);
        List<string> distractorMeanings = new List<string>();

        foreach (string distWord in distractorWords)
        {
            WordInfo distInfo = vocabManager.GetWordInfo(distWord);
            if (distInfo != null && !string.IsNullOrEmpty(distInfo.meaning))
            {
                distractorMeanings.Add(distInfo.meaning);
            }
        }

        // 선택지 구성 (정답 + 오답 뜻들)
        question.options = new List<string> { wordInfo.meaning };
        question.options.AddRange(distractorMeanings);

        // 선택지 섞기
        question.options = question.options.OrderBy(x => random.Next()).ToList();

        // 정답 인덱스 찾기
        question.correctAnswerIndex = question.options.IndexOf(wordInfo.meaning);

        return question;
    }

    /// <summary>
    /// 듣고 쓰기: 음성을 듣고 단어 타이핑
    /// </summary>
    private VocabQuestion GenerateTyping(string word, WordInfo wordInfo)
    {
        VocabQuestion question = new VocabQuestion
        {
            word = word,
            type = VocabQuestionType.Typing,
            correctAnswer = word,
            options = new List<string>() // 타이핑 문제는 선택지 없음
        };

        return question;
    }

    // =========================================================
    // Helper Methods
    // =========================================================

    /// <summary>
    /// 랜덤 문제 타입 선택 (Typing 제외, 선택형만)
    /// </summary>
    private VocabQuestionType GetRandomQuestionType()
    {
        // 3가지 선택형 타입만 사용 (Typing 제외)
        int typeIndex = random.Next(0, 3);
        return (VocabQuestionType)typeIndex;
    }

    /// <summary>
    /// 특정 타입의 문제만 생성
    /// </summary>
    public List<VocabQuestion> GenerateQuestionsOfType(
        List<string> words, 
        VocabQuestionType type, 
        int questionCount = 10)
    {
        List<VocabQuestion> questions = new List<VocabQuestion>();

        if (words == null || words.Count == 0)
        {
            Debug.LogWarning("[QuestionGenerator] No words provided.");
            return questions;
        }

        int actualCount = Mathf.Min(questionCount, words.Count);
        List<string> shuffledWords = words.OrderBy(x => random.Next()).ToList();

        for (int i = 0; i < actualCount; i++)
        {
            VocabQuestion question = GenerateQuestion(shuffledWords[i], type);
            if (question != null)
            {
                questions.Add(question);
            }
        }

        return questions;
    }

    /// <summary>
    /// 타입 분포를 지정하여 문제 생성
    /// 예: ListenAndChoose 3개, MeaningToWord 3개, WordToMeaning 2개, Typing 2개
    /// </summary>
    public List<VocabQuestion> GenerateQuestionsWithDistribution(
        List<string> words,
        Dictionary<VocabQuestionType, int> distribution)
    {
        List<VocabQuestion> questions = new List<VocabQuestion>();

        if (words == null || words.Count == 0)
        {
            Debug.LogWarning("[QuestionGenerator] No words provided.");
            return questions;
        }

        List<string> shuffledWords = words.OrderBy(x => random.Next()).ToList();
        int wordIndex = 0;

        foreach (var kvp in distribution)
        {
            VocabQuestionType type = kvp.Key;
            int count = kvp.Value;

            for (int i = 0; i < count && wordIndex < shuffledWords.Count; i++)
            {
                VocabQuestion question = GenerateQuestion(shuffledWords[wordIndex], type);
                if (question != null)
                {
                    questions.Add(question);
                    wordIndex++;
                }
            }
        }

        // 최종 섞기 (타입 순서 랜덤화)
        questions = questions.OrderBy(x => random.Next()).ToList();

        return questions;
    }
}
