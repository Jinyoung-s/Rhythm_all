using System;
using System.Collections.Generic;

namespace RhythmEnglish.MusicPlayer
{
    /// <summary>
    /// 반복 모드
    /// </summary>
    public enum RepeatMode
    {
        Off,    // 반복 없음
        All,    // 전체 반복
        One     // 한 곡 반복
    }

    /// <summary>
    /// 최근 재생 기록
    /// </summary>
    [Serializable]
    public class RecentlyPlayedItem
    {
        public string songId;
        public string lastPlayedAt;

        public RecentlyPlayedItem() { }

        public RecentlyPlayedItem(string songId)
        {
            this.songId = songId;
            this.lastPlayedAt = DateTime.UtcNow.ToString("o");
        }
    }

    /// <summary>
    /// 사용자 플레이리스트 데이터 (JSON 저장용)
    /// </summary>
    [Serializable]
    public class UserPlaylistData
    {
        /// <summary>
        /// 현재 재생 중인 곡 ID
        /// </summary>
        public string currentSongId;

        /// <summary>
        /// 현재 재생 위치 (0.0 ~ 1.0)
        /// </summary>
        public float currentPosition;

        /// <summary>
        /// 셔플 모드 상태
        /// </summary>
        public bool isShuffleOn;

        /// <summary>
        /// 반복 모드 (Off, All, One)
        /// </summary>
        public string repeatMode;

        /// <summary>
        /// 즐겨찾기 곡 ID 목록
        /// </summary>
        public List<string> favorites;

        /// <summary>
        /// 최근 재생 목록
        /// </summary>
        public List<RecentlyPlayedItem> recentlyPlayed;

        /// <summary>
        /// 마지막 업데이트 시간
        /// </summary>
        public string lastUpdated;

        public UserPlaylistData()
        {
            currentSongId = "";
            currentPosition = 0f;
            isShuffleOn = false;
            repeatMode = RepeatMode.Off.ToString();
            favorites = new List<string>();
            recentlyPlayed = new List<RecentlyPlayedItem>();
            lastUpdated = DateTime.UtcNow.ToString("o");
        }

        /// <summary>
        /// 반복 모드 Enum 반환
        /// </summary>
        public RepeatMode GetRepeatMode()
        {
            if (Enum.TryParse<RepeatMode>(repeatMode, out var mode))
                return mode;
            return RepeatMode.Off;
        }

        /// <summary>
        /// 반복 모드 설정
        /// </summary>
        public void SetRepeatMode(RepeatMode mode)
        {
            repeatMode = mode.ToString();
            lastUpdated = DateTime.UtcNow.ToString("o");
        }

        /// <summary>
        /// 즐겨찾기 토글
        /// </summary>
        public bool ToggleFavorite(string songId)
        {
            if (favorites.Contains(songId))
            {
                favorites.Remove(songId);
                return false;
            }
            else
            {
                favorites.Add(songId);
                return true;
            }
        }

        /// <summary>
        /// 최근 재생에 추가
        /// </summary>
        public void AddToRecentlyPlayed(string songId)
        {
            // 기존 항목 제거
            recentlyPlayed.RemoveAll(r => r.songId == songId);

            // 맨 앞에 추가
            recentlyPlayed.Insert(0, new RecentlyPlayedItem(songId));

            // 최대 20개 유지
            if (recentlyPlayed.Count > 20)
            {
                recentlyPlayed.RemoveRange(20, recentlyPlayed.Count - 20);
            }

            lastUpdated = DateTime.UtcNow.ToString("o");
        }
    }
}
