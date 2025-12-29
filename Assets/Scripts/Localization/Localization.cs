using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public static class LocalizationManager
{
    private static Dictionary<string, string> _strings = new Dictionary<string, string>();
    private static string _locale = "ko-KR";

    /// <summary>Resources/i18n/{locale}.json 로드</summary>
    public static bool SetLocale(string locale)
    {
        _locale = string.IsNullOrEmpty(locale) ? "ko-KR" : locale;

        TextAsset ta = Resources.Load<TextAsset>($"i18n/{_locale}");
        if (ta == null)
        {
            Debug.LogWarning($"[Localization] Missing locale file: Resources/i18n/{_locale}.json");
            _strings = new Dictionary<string, string>();
            return false;
        }
        else
        {
            Debug.Log($"Locale file found: Resources/i18n/{_locale}.json");
        }

        try
        {
            _strings = JsonConvert.DeserializeObject<Dictionary<string, string>>(ta.text)
                       ?? new Dictionary<string, string>();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Localization] JSON parse failed for {_locale}: {ex.Message}");
            _strings = new Dictionary<string, string>();
            return false;
        }
    }

    /// <summary>키로 번역 문자열 획득 (없으면 fallback → key 순)</summary>
    public static string Get(string key, string fallback = null)
    {
        if (!string.IsNullOrEmpty(key) && _strings.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val))
            return val;
        return fallback ?? key;
    }

    /// <summary>string.Format 지원 버전</summary>
    public static string GetF(string key, string fallback, params object[] args)
    {
        var fmt = Get(key, fallback);
        try { return string.Format(fmt, args); }
        catch { return fmt; }
    }

    public static string CurrentLocale => _locale;
}
