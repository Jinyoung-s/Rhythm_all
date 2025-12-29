using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using RhythmEnglish.Economy;

/// <summary>
/// 포인트(Notes) 시스템 관리
/// - 포인트 획득/차감
/// - 포인트 저장/로드
/// - 포인트 히스토리 관리
/// </summary>
public class PointManager
{
    private static PointManager _instance;
    public static PointManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new PointManager();
            }
            return _instance;
        }
    }
    
    private UserPointsData pointsData;
    private readonly string saveFilePath;
    
    /// <summary>
    /// 포인트 변경 시 호출되는 이벤트
    /// </summary>
    public event Action<int> OnPointsChanged;
    
    /// <summary>
    /// 포인트 획득 시 호출되는 이벤트
    /// </summary>
    public event Action<int, PointSource, string> OnPointsEarned;
    
    /// <summary>
    /// 포인트 사용 시 호출되는 이벤트
    /// </summary>
    public event Action<int, string> OnPointsSpent;
    
    private const int MAX_HISTORY_COUNT = 100;
    
    private PointManager()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "user_points.json");
        LoadPointsData();
    }
    
    // ========== 공개 API ==========
    
    /// <summary>
    /// 현재 사용 가능한 포인트 조회
    /// </summary>
    public int GetAvailableNotes()
    {
        return pointsData.availableNotes;
    }
    
    /// <summary>
    /// 총 누적 획득 포인트 조회
    /// </summary>
    public int GetTotalEarnedNotes()
    {
        return pointsData.totalEarnedNotes;
    }
    
    /// <summary>
    /// 총 사용 포인트 조회
    /// </summary>
    public int GetTotalSpentNotes()
    {
        return pointsData.totalSpentNotes;
    }
    
    /// <summary>
    /// 포인트 획득
    /// </summary>
    /// <param name="amount">획득할 포인트 양 (양수)</param>
    /// <param name="source">획득 출처</param>
    /// <param name="description">상세 설명</param>
    public void AddNotes(int amount, PointSource source, string description = "")
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[PointManager] AddNotes: amount must be positive. Got {amount}");
            return;
        }
        
        pointsData.availableNotes += amount;
        pointsData.totalEarnedNotes += amount;
        
        RecordHistory(amount, source, description);
        SavePointsData();
        
        Debug.Log($"[PointManager] +{amount} Notes from {source}. Total: {pointsData.availableNotes}");
        
        OnPointsEarned?.Invoke(amount, source, description);
        OnPointsChanged?.Invoke(pointsData.availableNotes);
    }
    
    /// <summary>
    /// 포인트 사용 (구매 등)
    /// </summary>
    /// <param name="amount">사용할 포인트 양 (양수)</param>
    /// <param name="description">사용 설명 (예: "Purchase - Shape of You")</param>
    /// <returns>성공 여부</returns>
    public bool SpendNotes(int amount, string description = "")
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[PointManager] SpendNotes: amount must be positive. Got {amount}");
            return false;
        }
        
        if (pointsData.availableNotes < amount)
        {
            Debug.LogWarning($"[PointManager] SpendNotes: Not enough notes. Available: {pointsData.availableNotes}, Required: {amount}");
            return false;
        }
        
        pointsData.availableNotes -= amount;
        pointsData.totalSpentNotes += amount;
        
        RecordHistory(-amount, PointSource.Purchase, description);
        SavePointsData();
        
        Debug.Log($"[PointManager] -{amount} Notes for {description}. Remaining: {pointsData.availableNotes}");
        
        OnPointsSpent?.Invoke(amount, description);
        OnPointsChanged?.Invoke(pointsData.availableNotes);
        
        return true;
    }
    
    /// <summary>
    /// 구매 가능 여부 확인
    /// </summary>
    public bool CanAfford(int amount)
    {
        return pointsData.availableNotes >= amount;
    }
    
    /// <summary>
    /// 포인트 히스토리 조회
    /// </summary>
    /// <param name="maxCount">최대 조회 개수</param>
    /// <returns>최근 히스토리 목록 (최신순)</returns>
    public List<PointHistory> GetHistory(int maxCount = 50)
    {
        return pointsData.history
            .OrderByDescending(h => h.timestamp)
            .Take(maxCount)
            .ToList();
    }
    
    /// <summary>
    /// 데이터 강제 저장
    /// </summary>
    public void ForceSave()
    {
        SavePointsData();
    }
    
    /// <summary>
    /// 데이터 강제 로드
    /// </summary>
    public void ForceLoad()
    {
        LoadPointsData();
    }
    
    // ========== 내부 메서드 ==========
    
    private void RecordHistory(int amount, PointSource source, string description)
    {
        var history = new PointHistory(amount, source, description);
        pointsData.history.Insert(0, history);
        
        // 히스토리 개수 제한
        if (pointsData.history.Count > MAX_HISTORY_COUNT)
        {
            pointsData.history = pointsData.history.Take(MAX_HISTORY_COUNT).ToList();
        }
        
        pointsData.lastUpdated = DateTime.UtcNow.ToString("o");
    }
    
    private void SavePointsData()
    {
        try
        {
            string json = JsonUtility.ToJson(pointsData, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"[PointManager] Data saved to {saveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PointManager] Failed to save data: {e.Message}");
        }
    }
    
    private void LoadPointsData()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                pointsData = JsonUtility.FromJson<UserPointsData>(json);
                Debug.Log($"[PointManager] Data loaded. Available: {pointsData.availableNotes} Notes");
            }
            else
            {
                // 신규 사용자 - 기본 데이터 생성
                pointsData = UserPointsData.CreateDefault();
                SavePointsData();
                Debug.Log("[PointManager] New user - created default data with 500 Notes welcome bonus");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PointManager] Failed to load data: {e.Message}. Creating default.");
            pointsData = UserPointsData.CreateDefault();
            SavePointsData();
        }
    }
    
    // ========== 점수 계산 헬퍼 ==========
    
    /// <summary>
    /// Game1 (리듬 게임) 점수 계산
    /// </summary>
    public static int CalculateGame1Score(int successfulNotes, int maxCombo, float perfectRate)
    {
        int baseScore = successfulNotes * 10;
        
        // 콤보 보너스
        int comboBonus = 0;
        if (maxCombo >= 100) comboBonus = 500;
        else if (maxCombo >= 50) comboBonus = 250;
        else if (maxCombo >= 20) comboBonus = 100;
        else if (maxCombo >= 10) comboBonus = 50;
        
        // 정확도 보너스
        int accuracyBonus = 0;
        if (perfectRate >= 0.8f) accuracyBonus = 200;
        else if (perfectRate >= 0.6f) accuracyBonus = 100;
        else if (perfectRate >= 0.4f) accuracyBonus = 50;
        
        return baseScore + comboBonus + accuracyBonus;
    }
    
    /// <summary>
    /// SingAlong (따라 부르기) 점수 계산
    /// </summary>
    public static int CalculateSingAlongScore(int passedLines, int totalLines)
    {
        if (totalLines <= 0) return 0;
        
        float completionRate = (float)passedLines / totalLines;
        int baseScore = Mathf.RoundToInt(completionRate * 100);
        
        // 완주 보너스
        int bonus = completionRate >= 0.9f ? 100 : 0;
        
        return baseScore + bonus;
    }
    
    /// <summary>
    /// StepTest (문장 조립) 점수 계산
    /// </summary>
    public static int CalculateStepTestScore(int correctCount, int totalCount)
    {
        if (totalCount <= 0) return 0;
        
        float accuracy = (float)correctCount / totalCount;
        int baseScore = Mathf.RoundToInt(accuracy * 150);
        
        // 전체 정답 보너스
        int bonus = 0;
        if (accuracy == 1.0f) bonus = 300;
        else if (accuracy >= 0.8f) bonus = 200;
        
        return baseScore + bonus;
    }
    
    /// <summary>
    /// VocabularyTest (단어 테스트) 점수 계산
    /// </summary>
    public static int CalculateVocabTestScore(int correctCount, int totalCount, int consecutiveCorrect)
    {
        if (totalCount <= 0) return 0;
        
        float accuracy = (float)correctCount / totalCount;
        int baseScore = Mathf.RoundToInt(accuracy * 100);
        
        // 연속 정답 보너스
        int consecutiveBonus = 0;
        if (consecutiveCorrect >= 10) consecutiveBonus = 100;
        else if (consecutiveCorrect >= 5) consecutiveBonus = 50;
        
        // 전체 정답 보너스
        int perfectBonus = accuracy == 1.0f ? 150 : 0;
        
        return baseScore + consecutiveBonus + perfectBonus;
    }
}
