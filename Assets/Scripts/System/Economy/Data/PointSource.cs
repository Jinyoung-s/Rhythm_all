using System;

namespace RhythmEnglish.Economy
{
    /// <summary>
    /// 포인트 획득/사용 출처
    /// </summary>
    public enum PointSource
    {
        Game1,          // 리듬 게임
        Game2,          // 리듬 데모
        SingAlong,      // 따라 부르기
        StepTest,       // 문장 조립 테스트
        VocabularyTest, // 단어 테스트
        Purchase,       // 노래 구매 (차감)
        Bonus,          // 보너스 지급
        Daily           // 일일 출석 보너스
    }
}
