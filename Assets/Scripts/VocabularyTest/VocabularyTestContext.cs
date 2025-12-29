using System.Collections.Generic;

/// <summary>
/// 씬 간 단어 테스트 데이터를 전달하기 위한 정적 컨텍스트
/// </summary>
public static class VocabularyTestContext
{
    /// <summary>
    /// 테스트할 단어 목록
    /// </summary>
    public static List<string> TargetWords { get; set; }
    
    /// <summary>
    /// 출처 챕터 ID (선택 사항)
    /// </summary>
    public static string SourceChapterId { get; set; }
    
    /// <summary>
    /// 출처 스텝 ID (선택 사항)
    /// </summary>
    public static string SourceStepId { get; set; }
    
    /// <summary>
    /// 컨텍스트를 초기화합니다.
    /// </summary>
    public static void Clear()
    {
        TargetWords = null;
        SourceChapterId = null;
        SourceStepId = null;
    }
    
    /// <summary>
    /// 컨텍스트 설정 헬퍼
    /// </summary>
    public static void Set(List<string> words, string chapterId = null, string stepId = null)
    {
        TargetWords = words;
        SourceChapterId = chapterId;
        SourceStepId = stepId;
    }
}
