using System;
using System.Collections.Generic;

namespace RhythmEnglish.Economy
{
    /// <summary>
    /// 사용자 포인트 데이터 (JSON 저장용)
    /// </summary>
    [Serializable]
    public class UserPointsData
    {
        /// <summary>
        /// 현재 사용 가능한 포인트
        /// </summary>
        public int availableNotes;
        
        /// <summary>
        /// 총 누적 획득 포인트
        /// </summary>
        public int totalEarnedNotes;
        
        /// <summary>
        /// 총 사용 포인트
        /// </summary>
        public int totalSpentNotes;
        
        /// <summary>
        /// 마지막 업데이트 시간 (ISO 8601 형식)
        /// </summary>
        public string lastUpdated;
        
        /// <summary>
        /// 포인트 히스토리 (최근 100개)
        /// </summary>
        public List<PointHistory> history;
        
        public UserPointsData()
        {
            availableNotes = 0;
            totalEarnedNotes = 0;
            totalSpentNotes = 0;
            lastUpdated = DateTime.UtcNow.ToString("o");
            history = new List<PointHistory>();
        }
        
        /// <summary>
        /// 기본 데이터 생성 (신규 사용자용)
        /// 시작 보너스로 500 Notes 지급
        /// </summary>
        public static UserPointsData CreateDefault()
        {
            var data = new UserPointsData
            {
                availableNotes = 500, // 시작 보너스
                totalEarnedNotes = 500,
                totalSpentNotes = 0,
                lastUpdated = DateTime.UtcNow.ToString("o"),
                history = new List<PointHistory>
                {
                    new PointHistory(500, PointSource.Bonus, "Welcome Bonus - 시작 보너스")
                }
            };
            return data;
        }
    }
}
