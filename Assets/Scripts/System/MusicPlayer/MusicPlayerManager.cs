using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using RhythmEnglish.Economy;
using RhythmEnglish.MusicPlayer;

/// <summary>
/// 뮤직 플레이어 매니저
/// - 오디오 재생/일시정지/정지
/// - 이전/다음 곡
/// - 셔플/반복 모드
/// - 진행 위치 제어
/// </summary>
public class MusicPlayerManager : MonoBehaviour
{
    private static MusicPlayerManager _instance;
    public static MusicPlayerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("MusicPlayerManager");
                _instance = go.AddComponent<MusicPlayerManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 오디오 소스
    private AudioSource audioSource;

    // 플레이리스트
    private List<SongItem> playlist = new List<SongItem>();
    private List<SongItem> shuffledPlaylist = new List<SongItem>();
    private int currentIndex = -1;

    // 상태
    private UserPlaylistData playlistData;
    private string saveFilePath;

    // 프로퍼티
    public bool IsPlaying => audioSource != null && audioSource.isPlaying;
    public SongItem CurrentSong => currentIndex >= 0 && currentIndex < GetCurrentPlaylist().Count 
        ? GetCurrentPlaylist()[currentIndex] 
        : null;
    public float CurrentPosition => audioSource != null && audioSource.clip != null 
        ? audioSource.time / audioSource.clip.length 
        : 0f;
    public float CurrentTime => audioSource != null ? audioSource.time : 0f;
    public float Duration => audioSource != null && audioSource.clip != null 
        ? audioSource.clip.length 
        : 0f;
    public bool IsShuffleOn => playlistData?.isShuffleOn ?? false;
    public RepeatMode CurrentRepeatMode => playlistData?.GetRepeatMode() ?? RepeatMode.Off;

    // 이벤트
    public event Action<SongItem> OnSongChanged;
    public event Action<float> OnPositionChanged;
    public event Action<bool> OnPlayStateChanged;
    public event Action<bool> OnShuffleChanged;
    public event Action<RepeatMode> OnRepeatModeChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 저장 경로 초기화 (Awake에서만 호출 가능)
        saveFilePath = Path.Combine(Application.persistentDataPath, "user_playlist.json");

        // AudioSource 설정
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        LoadPlaylistData();
    }

    private void Update()
    {
        if (IsPlaying)
        {
            OnPositionChanged?.Invoke(CurrentPosition);

            // 곡 끝났는지 체크
            if (!audioSource.isPlaying && audioSource.time >= audioSource.clip.length - 0.1f)
            {
                OnSongEnded();
            }
        }
    }

    // ========== 재생 제어 ==========

    /// <summary>
    /// 특정 곡 재생
    /// </summary>
    public void Play(SongItem song)
    {
        if (song == null) return;

        // 플레이리스트에서 인덱스 찾기
        var currentList = GetCurrentPlaylist();
        int index = currentList.FindIndex(s => s.chapterId == song.chapterId);

        if (index >= 0)
        {
            currentIndex = index;
        }
        else
        {
            // 플레이리스트에 없으면 추가
            playlist.Add(song);
            if (IsShuffleOn)
            {
                shuffledPlaylist.Add(song);
            }
            currentIndex = GetCurrentPlaylist().Count - 1;
        }

        LoadAndPlaySong(song);
    }

    /// <summary>
    /// 플레이리스트 설정 후 재생
    /// </summary>
    public void PlayPlaylist(List<SongItem> songs, int startIndex = 0)
    {
        if (songs == null || songs.Count == 0) return;

        playlist = new List<SongItem>(songs);
        
        if (IsShuffleOn)
        {
            ShufflePlaylist();
            currentIndex = 0;
        }
        else
        {
            currentIndex = Mathf.Clamp(startIndex, 0, playlist.Count - 1);
        }

        LoadAndPlaySong(GetCurrentPlaylist()[currentIndex]);
    }

    /// <summary>
    /// 일시정지 후 재개
    /// </summary>
    public void Play()
    {
        if (audioSource.clip != null)
        {
            audioSource.Play();
            OnPlayStateChanged?.Invoke(true);
        }
    }

    /// <summary>
    /// 일시정지
    /// </summary>
    public void Pause()
    {
        audioSource.Pause();
        OnPlayStateChanged?.Invoke(false);
        SavePlaylistData();
    }

    /// <summary>
    /// 정지
    /// </summary>
    public void Stop()
    {
        audioSource.Stop();
        audioSource.time = 0;
        OnPlayStateChanged?.Invoke(false);
        SavePlaylistData();
    }

    /// <summary>
    /// 다음 곡
    /// </summary>
    public void Next()
    {
        var currentList = GetCurrentPlaylist();
        if (currentList.Count == 0) return;

        // 한 곡 반복 모드면 처음부터 재생
        if (CurrentRepeatMode == RepeatMode.One)
        {
            Seek(0);
            Play();
            return;
        }

        currentIndex++;

        // 마지막 곡이면
        if (currentIndex >= currentList.Count)
        {
            if (CurrentRepeatMode == RepeatMode.All)
            {
                currentIndex = 0;
            }
            else
            {
                currentIndex = currentList.Count - 1;
                Stop();
                return;
            }
        }

        LoadAndPlaySong(currentList[currentIndex]);
    }

    /// <summary>
    /// 이전 곡
    /// </summary>
    public void Previous()
    {
        var currentList = GetCurrentPlaylist();
        if (currentList.Count == 0) return;

        // 3초 이상 재생했으면 처음으로
        if (CurrentTime > 3f)
        {
            Seek(0);
            return;
        }

        currentIndex--;

        if (currentIndex < 0)
        {
            if (CurrentRepeatMode == RepeatMode.All)
            {
                currentIndex = currentList.Count - 1;
            }
            else
            {
                currentIndex = 0;
            }
        }

        LoadAndPlaySong(currentList[currentIndex]);
    }

    /// <summary>
    /// 진행 위치 변경 (0.0 ~ 1.0)
    /// </summary>
    public void Seek(float position)
    {
        if (audioSource.clip == null) return;

        position = Mathf.Clamp01(position);
        audioSource.time = position * audioSource.clip.length;
        OnPositionChanged?.Invoke(position);
    }

    // ========== 모드 제어 ==========

    /// <summary>
    /// 셔플 토글
    /// </summary>
    public void ToggleShuffle()
    {
        playlistData.isShuffleOn = !playlistData.isShuffleOn;

        if (playlistData.isShuffleOn)
        {
            ShufflePlaylist();
            // 현재 곡을 셔플 리스트 맨 앞으로
            if (CurrentSong != null)
            {
                shuffledPlaylist.Remove(CurrentSong);
                shuffledPlaylist.Insert(0, CurrentSong);
                currentIndex = 0;
            }
        }
        else
        {
            // 셔플 해제 - 원래 플레이리스트에서 현재 곡 위치 찾기
            if (CurrentSong != null)
            {
                currentIndex = playlist.FindIndex(s => s.chapterId == CurrentSong.chapterId);
                if (currentIndex < 0) currentIndex = 0;
            }
        }

        OnShuffleChanged?.Invoke(playlistData.isShuffleOn);
        SavePlaylistData();
    }

    /// <summary>
    /// 반복 모드 토글 (Off → All → One → Off)
    /// </summary>
    public void ToggleRepeat()
    {
        var currentMode = CurrentRepeatMode;
        RepeatMode newMode = currentMode switch
        {
            RepeatMode.Off => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            RepeatMode.One => RepeatMode.Off,
            _ => RepeatMode.Off
        };

        playlistData.SetRepeatMode(newMode);
        OnRepeatModeChanged?.Invoke(newMode);
        SavePlaylistData();
    }

    // ========== 플레이리스트 관리 ==========

    /// <summary>
    /// 현재 플레이리스트 반환 (셔플 상태에 따라)
    /// </summary>
    public List<SongItem> GetCurrentPlaylist()
    {
        return IsShuffleOn ? shuffledPlaylist : playlist;
    }

    /// <summary>
    /// 다음 재생될 곡 목록
    /// </summary>
    public List<SongItem> GetUpNext(int count = 5)
    {
        var currentList = GetCurrentPlaylist();
        var upNext = new List<SongItem>();

        for (int i = 1; i <= count && currentIndex + i < currentList.Count; i++)
        {
            upNext.Add(currentList[currentIndex + i]);
        }

        return upNext;
    }

    /// <summary>
    /// 즐겨찾기 토글
    /// </summary>
    public bool ToggleFavorite(string songId)
    {
        bool isFavorite = playlistData.ToggleFavorite(songId);
        SavePlaylistData();
        return isFavorite;
    }

    /// <summary>
    /// 즐겨찾기 여부 확인
    /// </summary>
    public bool IsFavorite(string songId)
    {
        return playlistData.favorites.Contains(songId);
    }

    // ========== 내부 메서드 ==========

    private void LoadAndPlaySong(SongItem song)
    {
        if (song == null) return;

        // 오디오 클립 로드
        AudioClip clip = Resources.Load<AudioClip>(song.fullAudioPath);
        if (clip == null)
        {
            Debug.LogWarning($"[MusicPlayerManager] Audio not found: {song.fullAudioPath}");
            return;
        }

        audioSource.clip = clip;
        audioSource.time = 0;
        audioSource.Play();

        // 상태 업데이트
        playlistData.currentSongId = song.chapterId;
        playlistData.currentPosition = 0;
        playlistData.AddToRecentlyPlayed(song.chapterId);
        SavePlaylistData();

        Debug.Log($"[MusicPlayerManager] Now playing: {song.title} - {song.artist}");

        OnSongChanged?.Invoke(song);
        OnPlayStateChanged?.Invoke(true);
    }

    private void OnSongEnded()
    {
        Next();
    }

    private void ShufflePlaylist()
    {
        shuffledPlaylist = new List<SongItem>(playlist);
        
        // Fisher-Yates 셔플
        var rng = new System.Random();
        int n = shuffledPlaylist.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var temp = shuffledPlaylist[k];
            shuffledPlaylist[k] = shuffledPlaylist[n];
            shuffledPlaylist[n] = temp;
        }
    }

    private void SavePlaylistData()
    {
        try
        {
            playlistData.currentPosition = CurrentPosition;
            string json = JsonUtility.ToJson(playlistData, true);
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MusicPlayerManager] Failed to save: {e.Message}");
        }
    }

    private void LoadPlaylistData()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                playlistData = JsonUtility.FromJson<UserPlaylistData>(json);
            }
            else
            {
                playlistData = new UserPlaylistData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MusicPlayerManager] Failed to load: {e.Message}");
            playlistData = new UserPlaylistData();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SavePlaylistData();
        }
    }

    private void OnApplicationQuit()
    {
        SavePlaylistData();
    }
}
