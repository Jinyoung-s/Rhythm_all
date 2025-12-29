using UnityEngine;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;
using System.Linq;

/// <summary>
/// 최적의 TTS 음성을 자동으로 선택합니다.
/// Google TTS를 우선하고, 없으면 Fallback 음성을 사용합니다.
/// </summary>
public class SmartVoiceSelector : MonoBehaviour
{
    private Voice cachedBestVoice;

    /// <summary>
    /// 영어 발음을 위한 최적의 음성을 선택합니다.
    /// 우선순위: Google Neural > Google Standard > 기타 en-US > 기타 영어
    /// </summary>
    public Voice SelectBestVoiceForEnglish()
    {
        // 캐시된 음성이 있으면 재사용
        if (cachedBestVoice != null)
        {
            return cachedBestVoice;
        }

        // Speaker.Instance null 체크
        if (Speaker.Instance == null)
        {
            Debug.LogError("[SmartVoiceSelector] Speaker.Instance is null! RT-Voice might not be initialized.");
            return null;
        }

        var voices = Speaker.Instance.Voices;

        if (voices == null || voices.Count == 0)
        {
            // 초기화 중일 수 있으므로 에러 대신 경고로 출력
            Debug.LogWarning("[SmartVoiceSelector] Voices not loaded yet. RT-Voice might be initializing...");
            return null;
        }

        // 우선순위 1: Google en-US Neural
        cachedBestVoice = voices.FirstOrDefault(v =>
            v.Vendor != null && v.Vendor.Contains("Google") &&
            v.Culture == "en-US" &&
            v.Name != null && v.Name.Contains("Neural"));

        if (cachedBestVoice != null)
        {
            Debug.Log($"[SmartVoiceSelector] Selected: {cachedBestVoice.Name} (Google Neural)");
            return cachedBestVoice;
        }

        // 우선순위 2: Google en-US 일반
        cachedBestVoice = voices.FirstOrDefault(v =>
            v.Vendor != null && v.Vendor.Contains("Google") &&
            v.Culture != null && v.Culture.StartsWith("en-US"));

        if (cachedBestVoice != null)
        {
            Debug.Log($"[SmartVoiceSelector] Selected: {cachedBestVoice.Name} (Google Standard)");
            return cachedBestVoice;
        }

        // 우선순위 3: 다른 en-US 음성
        cachedBestVoice = voices.FirstOrDefault(v =>
            v.Culture != null && v.Culture.StartsWith("en-US"));

        if (cachedBestVoice != null)
        {
            Debug.Log($"[SmartVoiceSelector] Selected: {cachedBestVoice.Name} (Fallback en-US)");
            return cachedBestVoice;
        }

        // 우선순위 4: 첫 번째 영어 음성
        cachedBestVoice = voices.FirstOrDefault(v =>
            v.Culture != null && v.Culture.StartsWith("en"));

        if (cachedBestVoice != null)
        {
            Debug.Log($"[SmartVoiceSelector] Selected: {cachedBestVoice.Name} (Fallback English)");
            return cachedBestVoice;
        }

        // 최후의 수단: 첫 번째 음성
        cachedBestVoice = voices.FirstOrDefault();
        
        if (cachedBestVoice != null)
        {
            Debug.LogWarning($"[SmartVoiceSelector] No English voice found. Using: {cachedBestVoice.Name}");
        }
        else
        {
            Debug.LogError("[SmartVoiceSelector] No voices available at all.");
        }

        return cachedBestVoice;
    }

    /// <summary>
    /// 특정 문화권(언어)의 최적 음성을 선택합니다.
    /// </summary>
    /// <param name="culture">문화권 코드 (예: "en-US", "ko-KR")</param>
    public Voice SelectBestVoiceForCulture(string culture)
    {
        // Speaker.Instance null 체크
        if (Speaker.Instance == null)
        {
            Debug.LogError("[SmartVoiceSelector] Speaker.Instance is null in SelectBestVoiceForCulture!");
            return null;
        }

        var voices = Speaker.Instance.Voices;

        if (voices == null || voices.Count == 0)
        {
            Debug.LogError("[SmartVoiceSelector] No voices available on this device.");
            return null;
        }

        // Google Neural 우선
        var voice = voices.FirstOrDefault(v =>
            v.Vendor != null && v.Vendor.Contains("Google") &&
            v.Culture == culture &&
            v.Name != null && v.Name.Contains("Neural"));

        if (voice != null) return voice;

        // Google 일반
        voice = voices.FirstOrDefault(v =>
            v.Vendor != null && v.Vendor.Contains("Google") &&
            v.Culture != null && v.Culture.StartsWith(culture));

        if (voice != null) return voice;

        // 기타 해당 문화권
        voice = voices.FirstOrDefault(v =>
            v.Culture != null && v.Culture.StartsWith(culture));

        return voice;
    }

    /// <summary>
    /// 캐시된 음성을 초기화합니다. (음성 설정 변경 시 호출)
    /// </summary>
    public void ClearCache()
    {
        cachedBestVoice = null;
        Debug.Log("[SmartVoiceSelector] Voice cache cleared.");
    }
}
