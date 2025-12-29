using System;
using System.Collections.Generic;
using System.Linq;

namespace RhythmEnglish.Economy
{
    /// <summary>
    /// 노래 카탈로그 데이터 (JSON 저장용)
    /// </summary>
    [Serializable]
    public class SongCatalogData
    {
        /// <summary>
        /// 모든 노래 목록
        /// </summary>
        public List<SongItem> songs;
        
        public SongCatalogData()
        {
            songs = new List<SongItem>();
        }
        
        /// <summary>
        /// 구매한 노래 목록 반환
        /// </summary>
        public List<SongItem> GetPurchasedSongs()
        {
            return songs.Where(s => s.isPurchased || s.isFree).ToList();
        }
        
        /// <summary>
        /// 구매 가능한 노래 목록 반환 (미구매 + 유료)
        /// </summary>
        public List<SongItem> GetAvailableForPurchase()
        {
            return songs.Where(s => !s.isPurchased && !s.isFree).ToList();
        }
        
        /// <summary>
        /// 특정 노래 찾기
        /// </summary>
        public SongItem GetSong(string chapterId)
        {
            return songs.FirstOrDefault(s => s.chapterId == chapterId);
        }
        
        /// <summary>
        /// 난이도별 필터링
        /// </summary>
        public List<SongItem> GetSongsByDifficulty(string difficulty)
        {
            return songs.Where(s => s.difficulty == difficulty).ToList();
        }
    }
}
