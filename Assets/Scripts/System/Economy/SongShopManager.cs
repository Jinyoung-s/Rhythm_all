using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using RhythmEnglish.Economy;

/// <summary>
/// 노래 상점 매니저
/// - 구매 가능 노래 목록 관리
/// - 구매 처리
/// - 구매 상태 관리
/// </summary>
public class SongShopManager
{
    private static SongShopManager _instance;
    public static SongShopManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SongShopManager();
            }
            return _instance;
        }
    }

    private SongCatalogData catalogData;
    private readonly string catalogPath = "Data/song_catalog";
    private readonly string userPurchasePath;

    /// <summary>
    /// 구매 성공 이벤트
    /// </summary>
    public event Action<SongItem> OnPurchaseSuccess;

    /// <summary>
    /// 구매 실패 이벤트
    /// </summary>
    public event Action<string> OnPurchaseFailed;

    private SongShopManager()
    {
        userPurchasePath = Path.Combine(Application.persistentDataPath, "user_purchases.json");
        LoadCatalog();
    }

    // ========== 공개 API ==========

    /// <summary>
    /// 모든 노래 목록 반환
    /// </summary>
    public List<SongItem> GetAllSongs()
    {
        return catalogData?.songs ?? new List<SongItem>();
    }

    /// <summary>
    /// 구매한 노래 목록 반환
    /// </summary>
    public List<SongItem> GetPurchasedSongs()
    {
        return GetAllSongs().Where(s => s.isPurchased || s.isFree).ToList();
    }

    /// <summary>
    /// 구매 가능한 노래 목록 반환
    /// </summary>
    public List<SongItem> GetAvailableForPurchase()
    {
        return GetAllSongs().Where(s => !s.isPurchased && !s.isFree).ToList();
    }

    /// <summary>
    /// 무료 노래 목록 반환
    /// </summary>
    public List<SongItem> GetFreeSongs()
    {
        return GetAllSongs().Where(s => s.isFree).ToList();
    }

    /// <summary>
    /// 난이도별 노래 목록 반환
    /// </summary>
    public List<SongItem> GetSongsByDifficulty(string difficulty)
    {
        return GetAllSongs().Where(s => s.difficulty == difficulty).ToList();
    }

    /// <summary>
    /// 특정 노래 정보 조회
    /// </summary>
    public SongItem GetSongInfo(string chapterId)
    {
        return GetAllSongs().FirstOrDefault(s => s.chapterId == chapterId);
    }

    /// <summary>
    /// 구매 여부 확인
    /// </summary>
    public bool IsPurchased(string chapterId)
    {
        var song = GetSongInfo(chapterId);
        return song != null && (song.isPurchased || song.isFree);
    }

    /// <summary>
    /// 구매 가능 여부 확인
    /// </summary>
    public bool CanPurchase(string chapterId)
    {
        var song = GetSongInfo(chapterId);
        if (song == null) return false;
        if (song.isPurchased || song.isFree) return false;
        return PointManager.Instance.CanAfford(song.price);
    }

    /// <summary>
    /// 노래 구매 시도
    /// </summary>
    /// <param name="chapterId">노래 ID</param>
    /// <param name="errorMessage">실패 시 에러 메시지</param>
    /// <returns>성공 여부</returns>
    public bool TryPurchaseSong(string chapterId, out string errorMessage)
    {
        errorMessage = "";

        var song = GetSongInfo(chapterId);
        if (song == null)
        {
            errorMessage = "노래를 찾을 수 없습니다.";
            OnPurchaseFailed?.Invoke(errorMessage);
            return false;
        }

        if (song.isPurchased)
        {
            errorMessage = "이미 구매한 노래입니다.";
            OnPurchaseFailed?.Invoke(errorMessage);
            return false;
        }

        if (song.isFree)
        {
            errorMessage = "무료 노래입니다.";
            OnPurchaseFailed?.Invoke(errorMessage);
            return false;
        }

        // 포인트 차감 시도
        if (!PointManager.Instance.SpendNotes(song.price, $"Purchase - {song.title} ({song.artist})"))
        {
            errorMessage = $"Notes가 부족합니다. (필요: {song.price}, 보유: {PointManager.Instance.GetAvailableNotes()})";
            OnPurchaseFailed?.Invoke(errorMessage);
            return false;
        }

        // 구매 처리
        song.MarkAsPurchased();
        SavePurchases();

        Debug.Log($"[SongShopManager] ✅ Purchased: {song.title} for {song.price} Notes");

        OnPurchaseSuccess?.Invoke(song);
        return true;
    }

    /// <summary>
    /// 미리듣기 오디오 로드
    /// </summary>
    public AudioClip LoadPreviewAudio(string chapterId)
    {
        var song = GetSongInfo(chapterId);
        if (song == null || string.IsNullOrEmpty(song.previewAudioPath))
            return null;

        return Resources.Load<AudioClip>(song.previewAudioPath);
    }

    /// <summary>
    /// 카탈로그 새로고침
    /// </summary>
    public void RefreshCatalog()
    {
        LoadCatalog();
    }

    // ========== 내부 메서드 ==========

    private void LoadCatalog()
    {
        try
        {
            // 마스터 데이터 로드
            TextAsset catalogJson = Resources.Load<TextAsset>(catalogPath);
            if (catalogJson != null)
            {
                catalogData = JsonUtility.FromJson<SongCatalogData>(catalogJson.text);
                Debug.Log($"[SongShopManager] Loaded {catalogData.songs.Count} songs from catalog");
            }
            else
            {
                Debug.LogWarning($"[SongShopManager] Catalog not found at Resources/{catalogPath}");
                catalogData = new SongCatalogData();
            }

            // 사용자 구매 정보 로드 및 병합
            LoadPurchases();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SongShopManager] Failed to load catalog: {e.Message}");
            catalogData = new SongCatalogData();
        }
    }

    private void LoadPurchases()
    {
        try
        {
            if (File.Exists(userPurchasePath))
            {
                string json = File.ReadAllText(userPurchasePath);
                var purchases = JsonUtility.FromJson<UserPurchasesData>(json);

                // 구매 정보 병합
                foreach (var purchasedId in purchases.purchasedSongIds)
                {
                    var song = catalogData.songs.FirstOrDefault(s => s.chapterId == purchasedId);
                    if (song != null)
                    {
                        song.isPurchased = true;
                    }
                }

                Debug.Log($"[SongShopManager] Loaded {purchases.purchasedSongIds.Count} purchased songs");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SongShopManager] Failed to load purchases: {e.Message}");
        }
    }

    private void SavePurchases()
    {
        try
        {
            var purchases = new UserPurchasesData
            {
                purchasedSongIds = catalogData.songs
                    .Where(s => s.isPurchased && !s.isFree)
                    .Select(s => s.chapterId)
                    .ToList(),
                lastUpdated = DateTime.UtcNow.ToString("o")
            };

            string json = JsonUtility.ToJson(purchases, true);
            File.WriteAllText(userPurchasePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SongShopManager] Failed to save purchases: {e.Message}");
        }
    }
}

/// <summary>
/// 사용자 구매 정보 저장용
/// </summary>
[Serializable]
public class UserPurchasesData
{
    public List<string> purchasedSongIds = new List<string>();
    public string lastUpdated;
}
