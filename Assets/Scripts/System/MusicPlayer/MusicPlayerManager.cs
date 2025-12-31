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
    private AudioSource mrSource;
    private AudioSource vocalSource;

    // 플레이리스트
    private List<SongItem> playlist = new List<SongItem>();
    private List<SongItem> shuffledPlaylist = new List<SongItem>();
    private int currentIndex = -1;

    // 상태
    private UserPlaylistData playlistData;
    private string saveFilePath;
    private float vocalVolume = 1.0f;

    // 프로퍼티
    public bool IsPlaying => mrSource != null && mrSource.isPlaying;
    public SongItem CurrentSong => currentIndex >= 0 && currentIndex < GetCurrentPlaylist().Count 
        ? GetCurrentPlaylist()[currentIndex] 
        : null;
    public float CurrentPosition => mrSource != null && mrSource.clip != null 
        ? mrSource.time / mrSource.clip.length 
        : 0f;
    public float CurrentTime => mrSource != null ? mrSource.time : 0f;
    public float Duration => mrSource != null && mrSource.clip != null 
        ? mrSource.clip.length 
        : 0f;
    public bool IsShuffleOn => playlistData?.isShuffleOn ?? false;
    public RepeatMode CurrentRepeatMode => playlistData?.GetRepeatMode() ?? RepeatMode.Off;
    public float VocalVolume
    {
        get => vocalVolume;
        set
        {
            vocalVolume = Mathf.Clamp01(value);
            if (vocalSource != null) vocalSource.volume = vocalVolume;
        }
    }

    // Queue 정보 노출
    public List<SongItem> CurrentPlaylist => GetCurrentPlaylist();
    public int CurrentIndex => currentIndex;

    public void SetVocalVolume(float volume)
    {
        VocalVolume = volume;
    }

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

        // 저장 경로 초기화
        saveFilePath = Path.Combine(Application.persistentDataPath, "user_playlist.json");

        // AudioSource 설정
        mrSource = gameObject.AddComponent<AudioSource>();
        mrSource.playOnAwake = false;
        mrSource.loop = false;

        vocalSource = gameObject.AddComponent<AudioSource>();
        vocalSource.playOnAwake = false;
        vocalSource.loop = false;
        vocalSource.volume = vocalVolume;

        LoadPlaylistData();
    }
    
    private bool wasPlayingLastFrame = false;

    private void Update()
    {
        if (mrSource != null && mrSource.clip != null)
        {
            // 재생 중일 때 위치 업데이트
            if (mrSource.isPlaying)
            {
                OnPositionChanged?.Invoke(CurrentPosition);
                wasPlayingLastFrame = true;
            }
            // 재생이 멈췄고, 이전 프레임에서는 재생 중이었다면 곡이 끝난 것
            else if (wasPlayingLastFrame && mrSource.time >= mrSource.clip.length - 0.1f)
            {
                wasPlayingLastFrame = false;
                OnSongEnded();
            }
        }
    }

    // ========== 재생 제어 ==========

    public void Play(SongItem song)
    {
        if (song == null) return;

        var currentList = GetCurrentPlaylist();
        int index = currentList.FindIndex(s => s.chapterId == song.chapterId);

        if (index >= 0)
        {
            currentIndex = index;
        }
        else
        {
            playlist.Add(song);
            if (IsShuffleOn)
            {
                shuffledPlaylist.Add(song);
            }
            currentIndex = GetCurrentPlaylist().Count - 1;
        }

        LoadAndPlaySong(song);
    }

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

    public void Play()
    {
        if (mrSource.clip != null)
        {
            mrSource.Play();
            if (vocalSource.clip != null) vocalSource.Play();
            OnPlayStateChanged?.Invoke(true);
        }
    }

    public void Pause()
    {
        mrSource.Pause();
        vocalSource.Pause();
        OnPlayStateChanged?.Invoke(false);
        SavePlaylistData();
    }

    public void Stop()
    {
        mrSource.Stop();
        vocalSource.Stop();
        mrSource.time = 0;
        vocalSource.time = 0;
        OnPlayStateChanged?.Invoke(false);
        SavePlaylistData();
    }

    public void Next()
    {
        var currentList = GetCurrentPlaylist();
        if (currentList.Count == 0) return;

        if (CurrentRepeatMode == RepeatMode.One)
        {
            Seek(0);
            Play();
            return;
        }

        currentIndex++;

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

    public void Previous()
    {
        var currentList = GetCurrentPlaylist();
        if (currentList.Count == 0) return;

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

    public void Seek(float position)
    {
        if (mrSource.clip == null) return;

        position = Mathf.Clamp01(position);
        float time = position * mrSource.clip.length;
        mrSource.time = time;
        if (vocalSource.clip != null) vocalSource.time = time;
        
        OnPositionChanged?.Invoke(position);
    }

    // ========== 내부 메서드 ==========

    private void LoadAndPlaySong(SongItem song)
    {
        if (song == null) return;

        mrSource.Stop();
        vocalSource.Stop();

        // 1. MR 로드
        string mrPath = !string.IsNullOrEmpty(song.instrumentalAudioPath) ? song.instrumentalAudioPath : song.fullAudioPath;
        AudioClip mrClip = Resources.Load<AudioClip>(mrPath);
        
        // 2. Vocal 로드 (있을 경우만)
        AudioClip vocalClip = null;
        if (!string.IsNullOrEmpty(song.vocalAudioPath))
        {
            vocalClip = Resources.Load<AudioClip>(song.vocalAudioPath);
        }

        if (mrClip == null)
        {
            Debug.LogWarning($"[MusicPlayerManager] Main Audio not found: {mrPath}");
            return;
        }

        mrSource.clip = mrClip;
        mrSource.time = 0;
        
        vocalSource.clip = vocalClip;
        vocalSource.time = 0;
        vocalSource.volume = vocalVolume;

        mrSource.Play();
        if (vocalClip != null) vocalSource.Play();

        // 상태 업데이트
        playlistData.currentSongId = song.chapterId;
        playlistData.currentPosition = 0;
        playlistData.AddToRecentlyPlayed(song.chapterId);
        SavePlaylistData();

        Debug.Log($"[MusicPlayerManager] Now playing (Dual): {song.title}");

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
        if (pauseStatus) SavePlaylistData();
    }

    private void OnApplicationQuit()
    {
        SavePlaylistData();
    }

    // ========== 플레이리스트 관리 ==========
    public void ToggleShuffle()
    {
        playlistData.isShuffleOn = !playlistData.isShuffleOn;
        if (playlistData.isShuffleOn)
        {
            ShufflePlaylist();
            if (CurrentSong != null)
            {
                shuffledPlaylist.Remove(CurrentSong);
                shuffledPlaylist.Insert(0, CurrentSong);
                currentIndex = 0;
            }
        }
        else
        {
            if (CurrentSong != null)
            {
                currentIndex = playlist.FindIndex(s => s.chapterId == CurrentSong.chapterId);
                if (currentIndex < 0) currentIndex = 0;
            }
        }
        OnShuffleChanged?.Invoke(playlistData.isShuffleOn);
        SavePlaylistData();
    }

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

    public List<SongItem> GetCurrentPlaylist() => IsShuffleOn ? shuffledPlaylist : playlist;

    public List<SongItem> GetUpNext(int count = 5)
    {
        var currentList = GetCurrentPlaylist();
        var upNext = new List<SongItem>();
        for (int i = 1; i <= count && currentIndex + i < currentList.Count; i++)
            upNext.Add(currentList[currentIndex + i]);
        return upNext;
    }

    public bool ToggleFavorite(string songId)
    {
        bool isFavorite = playlistData.ToggleFavorite(songId);
        SavePlaylistData();
        return isFavorite;
    }

    public bool IsFavorite(string songId) => playlistData.favorites.Contains(songId);
}
