using UnityEngine;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;
using System.Linq;

/// <summary>
/// Android 기기에서 Google TTS 엔진 설치 여부를 확인합니다.
/// </summary>
public class GoogleTTSChecker : MonoBehaviour
{
    private const string GOOGLE_TTS_VENDOR = "Google";

    /// <summary>
    /// Google TTS 엔진이 설치되어 있는지 확인합니다.
    /// </summary>
    /// <returns>Google TTS 설치 여부</returns>
    public bool IsGoogleTTSInstalled()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var voices = Speaker.Instance.Voices;
        
        if (voices == null || voices.Count == 0)
        {
            Debug.LogWarning("[GoogleTTSChecker] No voices found on device.");
            return false;
        }

        bool hasGoogleVoice = voices.Any(v => 
            v.Vendor != null && v.Vendor.Contains(GOOGLE_TTS_VENDOR));

        Debug.Log($"[GoogleTTSChecker] Google TTS installed: {hasGoogleVoice}");
        return hasGoogleVoice;
#else
        // iOS나 Editor에서는 항상 true (체크 불필요)
        return true;
#endif
    }

    /// <summary>
    /// 고품질 영어 음성이 사용 가능한지 확인합니다.
    /// </summary>
    /// <returns>고품질 영어 음성 가용 여부</returns>
    public bool IsHighQualityEnglishAvailable()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var voices = Speaker.Instance.Voices;
        
        if (voices == null || voices.Count == 0)
        {
            Debug.LogWarning("[GoogleTTSChecker] No voices found on device.");
            return false;
        }

        bool hasQualityVoice = voices.Any(v =>
            v.Vendor != null && v.Vendor.Contains(GOOGLE_TTS_VENDOR) &&
            v.Culture != null && v.Culture.StartsWith("en-US"));

        Debug.Log($"[GoogleTTSChecker] High quality English available: {hasQualityVoice}");
        
        // 디버그: 사용 가능한 음성 목록 출력
        if (!hasQualityVoice)
        {
            Debug.Log("[GoogleTTSChecker] Available voices:");
            foreach (var voice in voices.Where(v => v.Culture != null && v.Culture.StartsWith("en")))
            {
                Debug.Log($"  - {voice.Name} ({voice.Vendor}) [{voice.Culture}]");
            }
        }

        return hasQualityVoice;
#else
        // iOS나 Editor에서는 항상 true
        return true;
#endif
    }

    /// <summary>
    /// 사용 가능한 모든 영어 음성 목록을 반환합니다.
    /// </summary>
    public System.Collections.Generic.List<Voice> GetAvailableEnglishVoices()
    {
        var voices = Speaker.Instance.Voices;
        
        if (voices == null || voices.Count == 0)
        {
            return new System.Collections.Generic.List<Voice>();
        }

        return voices.Where(v => 
            v.Culture != null && v.Culture.StartsWith("en")).ToList();
    }
}
