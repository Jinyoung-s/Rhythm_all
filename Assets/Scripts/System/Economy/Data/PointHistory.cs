using System;

namespace RhythmEnglish.Economy
{
    /// <summary>
    /// 포인트 획득/사용 기록
    /// </summary>
    [Serializable]
    public class PointHistory
    {
        /// <summary>
        /// 기록 시간 (ISO 8601 형식)
        /// </summary>
        public string timestamp;
        
        /// <summary>
        /// 금액 (양수: 획득, 음수: 사용)
        /// </summary>
        public int amount;
        
        /// <summary>
        /// 포인트 출처
        /// </summary>
        public string source;
        
        /// <summary>
        /// 상세 설명
        /// </summary>
        public string description;
        
        public PointHistory() { }
        
        public PointHistory(int amount, PointSource source, string description)
        {
            this.timestamp = DateTime.UtcNow.ToString("o");
            this.amount = amount;
            this.source = source.ToString();
            this.description = description;
        }
        
        /// <summary>
        /// 기록 시간을 DateTime으로 변환
        /// </summary>
        public DateTime GetDateTime()
        {
            return DateTime.Parse(timestamp);
        }
    }
}
