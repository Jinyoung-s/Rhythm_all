using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class StepResourceResolver
{
    public static string DefaultButtonBg = "neon_boder_blue";

    public static string GetFallbackChapterId() => "pvb_chap_001";

    public static StepData CreateFallbackStep()
    {
        return new StepData
        {
            id = "step_001",
            songFile = "wakeup",
            lyricsFile = "wake_up_lyrics",
            roleFile = "step_001_role"
        };
    }

    public static string NormalizeButtonBackgroundKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return DefaultButtonBg;
        if (key.StartsWith("btn_") || key.StartsWith("neon_") || key.Contains("_")) return key;
        return "btn_" + key;
    }

    public static AudioClip LoadSongClip(string chapterId, StepData step)
    {
        if (step == null) return null;
        foreach (var candidate in EnumerateSongResourceKeys(chapterId, step))
        {
            var clip = Resources.Load<AudioClip>(candidate);
            if (clip != null) return clip;
        }
        return null;
    }

    public static TextAsset LoadLyricsAsset(string chapterId, StepData step)
    {
        if (step == null) return null;
        foreach (var candidate in EnumerateLyricsResourceKeys(chapterId, step))
        {
            var asset = Resources.Load<TextAsset>(candidate);
            if (asset != null) return asset;
        }
        return null;
    }

    /// <summary>
    /// MusicPlayer 전용: 문장 단위 가사 파일(singalong)을 우선적으로 로드
    /// </summary>
    public static TextAsset LoadSingAlongAsset(string chapterId, StepData step)
    {
        Debug.Log($"[StepResourceResolver] LoadSingAlongAsset called for {chapterId}/{step?.id}");
        if (step == null) return null;
        foreach (var candidate in EnumerateSingAlongResourceKeys(chapterId, step))
        {
            Debug.Log($"[StepResourceResolver] Trying to load: {candidate}");
            var asset = Resources.Load<TextAsset>(candidate);
            if (asset != null) 
            {
                Debug.Log($"[StepResourceResolver] ✅ Found: {asset.name}");
                return asset;
            }
        }
        Debug.LogWarning($"[StepResourceResolver] ❌ No singalong asset found for {chapterId}/{step.id}");
        return null;
    }

    public static TextAsset LoadRoleAsset(string chapterId, StepData step)
    {
        if (step == null) return null;
        if (!string.IsNullOrEmpty(step.roleFile))
        {
            string path = $"json/{chapterId}/{RemoveExtension(step.roleFile)}";
            var asset = Resources.Load<TextAsset>(path);
            if (asset != null) return asset;
        }
        
        // Fallback
        string fallbackPath = $"json/{chapterId}/{step.id}_role";
        return Resources.Load<TextAsset>(fallbackPath);
    }

    public static TextAsset LoadTestAsset(string chapterId, StepData step)
    {
        if (step == null) return null;
        if (!string.IsNullOrEmpty(step.testFile))
        {
            string path = $"json/{chapterId}/{RemoveExtension(step.testFile)}";
            var asset = Resources.Load<TextAsset>(path);
            if (asset != null) return asset;
        }

        string fallbackPath = $"json/{chapterId}/{step.id}_test";
        return Resources.Load<TextAsset>(fallbackPath);
    }

    public static LearnStepDataList LoadLearnAsset(string chapterId, StepData step)
    {
        if (step == null) return null;
        string path = $"json/{chapterId}/{step.id}_learn";
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset != null)
        {
            return JsonUtility.FromJson<LearnStepDataList>(asset.text);
        }
        return null;
    }

    private static IEnumerable<string> EnumerateSongResourceKeys(string chapterId, StepData step)
    {
        if (!string.IsNullOrEmpty(step.songFile))
            yield return $"mp3/{chapterId}/{RemoveExtension(step.songFile)}";
        
        yield return $"mp3/{chapterId}/{step.id}";
    }

    /// <summary>
    /// 게임용: 단어 단위 타이밍 파일(_lyrics)을 우선 로드
    /// </summary>
    private static IEnumerable<string> EnumerateLyricsResourceKeys(string chapterId, StepData step)
    {
        if (!string.IsNullOrEmpty(step.lyricsFile))
            yield return $"json/{chapterId}/{RemoveExtension(step.lyricsFile)}";
            
        // lyrics를 먼저 시도 (단어 단위 타이밍, 게임용)
        yield return $"json/{chapterId}/{step.id}_lyrics";
        // singalong은 두 번째 (문장 단위 가사, fallback)
        yield return $"json/{chapterId}/{step.id}_singalong";
    }

    /// <summary>
    /// MusicPlayer용: 문장 단위 가사 파일(_singalong)을 우선 로드
    /// step.lyricsFile을 무시하고 무조건 _singalong 우선
    /// </summary>
    private static IEnumerable<string> EnumerateSingAlongResourceKeys(string chapterId, StepData step)
    {
        // singalong을 먼저 시도 (문장 단위 가사, MusicPlayer용)
        yield return $"json/{chapterId}/{step.id}_singalong";
        // lyrics는 두 번째 (단어 단위 타이밍, fallback)
        yield return $"json/{chapterId}/{step.id}_lyrics";
    }

    private static string RemoveExtension(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        int dot = name.LastIndexOf('.');
        return (dot == -1) ? name : name.Substring(0, dot);
    }
}
