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

    private static IEnumerable<string> EnumerateLyricsResourceKeys(string chapterId, StepData step)
    {
        if (!string.IsNullOrEmpty(step.lyricsFile))
            yield return $"json/{chapterId}/{RemoveExtension(step.lyricsFile)}";
            
        yield return $"json/{chapterId}/{step.id}_lyrics";
        yield return $"json/{chapterId}/{step.id}_singalong";
    }

    private static string RemoveExtension(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        int dot = name.LastIndexOf('.');
        return (dot == -1) ? name : name.Substring(0, dot);
    }
}
