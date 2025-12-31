using System;

namespace RhythmEnglish.Economy
{
    /// <summary>
    /// 노래 난이도
    /// </summary>
    public enum DifficultyLevel
    {
        Beginner,
        Elementary,
        Intermediate,
        Advanced,
        Expert
    }
    
    /// <summary>
    /// 개별 노래 정보
    /// </summary>
    [Serializable]
    public class SongItem
    {
        /// <summary>
        /// 노래 ID (Chapter ID와 동일)
        /// </summary>
        public string chapterId;
        
        /// <summary>
        /// 스텝 ID (챕터 내 상세 구분)
        /// </summary>
        public string stepId;
        
        /// <summary>
        /// 노래 제목
        /// </summary>
        public string title;
        
        /// <summary>
        /// 아티스트
        /// </summary>
        public string artist;
        
        /// <summary>
        /// 난이도
        /// </summary>
        public string difficulty;
        
        /// <summary>
        /// 가격 (Notes) - 균일 500
        /// </summary>
        public int price;
        
        /// <summary>
        /// 곡 길이 (초 단위)
        /// </summary>
        public int duration;
        
        /// <summary>
        /// 무료 여부
        /// </summary>
        public bool isFree;
        
        /// <summary>
        /// 구매 여부
        /// </summary>
        public bool isPurchased;
        
        /// <summary>
        /// 구매 날짜 (ISO 8601)
        /// </summary>
        public string purchaseDate;
        
        /// <summary>
        /// 썸네일 이미지 경로
        /// </summary>
        public string thumbnailPath;
        
        /// <summary>
        /// 전체 곡 오디오 경로
        /// </summary>
        public string fullAudioPath;

        /// <summary>
        /// 보컬 트랙 경로
        /// </summary>
        public string vocalAudioPath;

        /// <summary>
        /// 인스트(MR) 트랙 경로
        /// </summary>
        public string instrumentalAudioPath;
        
        /// <summary>
        /// 미리듣기 오디오 경로 (30초)
        /// </summary>
        public string previewAudioPath;
        
        /// <summary>
        /// 태그 (Pop, Rock, Ballad 등)
        /// </summary>
        public string[] tags;
        
        /// <summary>
        /// 설명
        /// </summary>
        public string description;
        
        /// <summary>
        /// 분:초 형식의 재생 시간 반환
        /// </summary>
        public string GetFormattedDuration()
        {
            int minutes = duration / 60;
            int seconds = duration % 60;
            return $"{minutes}:{seconds:D2}";
        }
        
        /// <summary>
        /// 구매 처리
        /// </summary>
        public void MarkAsPurchased()
        {
            isPurchased = true;
            purchaseDate = DateTime.UtcNow.ToString("o");
        }
    }
}
