using System;
using System.Collections.Generic;

/// <summary>
/// 단어 테스트 문제 데이터
/// </summary>
[Serializable]
public class VocabQuestion
{
    public string word;                     // 문제 단어
    public VocabQuestionType type;          // 문제 타입
    public string correctAnswer;            // 정답 (표시용)
    public List<string> options;            // 선택지 4개
    public int correctAnswerIndex;          // 정답 인덱스 (0-3)
    
    public VocabQuestion()
    {
        options = new List<string>();
    }
}
