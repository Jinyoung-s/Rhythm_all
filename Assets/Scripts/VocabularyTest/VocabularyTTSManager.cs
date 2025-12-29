using UnityEngine;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;
using System;

/// <summary>
/// RT-Voice PRO를 사용한 TTS 음성 관리 싱글톤
/// 단어 발음을 동적으로 생성하고 재생합니다.
/// </summary>
public class VocabularyTTSManager : MonoBehaviour
{
    private static VocabularyTTSManager _instance;
    public static VocabularyTTSManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("VocabularyTTSManager");
                _instance = go.AddComponent<VocabularyTTSManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private SmartVoiceSelector voiceSelector;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        voiceSelector = gameObject.AddComponent<SmartVoiceSelector>();
        
        Debug.Log("[VocabularyTTS] VocabularyTTSManager initialized.");
    }

    void Start()
    {
        // Android에서는 TTS 엔진이 초기화되는 시간이 필요할 수 있음
        StartCoroutine(InitializeRTVoice());
    }

    /// <summary>
    /// RT-Voice 초기화 코루틴 (Android에서 필요)
    /// </summary>
    private System.Collections.IEnumerator InitializeRTVoice()
    {
        Debug.Log("[VocabularyTTS] Initializing RT-Voice...");
        
        // 약간의 대기 시간을 줌
        yield return new WaitForSeconds(0.5f);

        if (Speaker.Instance != null)
        {
            Debug.Log($"[VocabularyTTS] RT-Voice initialized successfully.");
            Debug.Log($"[VocabularyTTS] Available voices: {Speaker.Instance.Voices?.Count ?? 0}");
            
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android에서 음성 목록 로그
            if (Speaker.Instance.Voices != null && Speaker.Instance.Voices.Count > 0)
            {
                Debug.Log("[VocabularyTTS] Android voices:");
                foreach (var voice in Speaker.Instance.Voices)
                {
                    Debug.Log($"  - {voice.Name} ({voice.Vendor}) [{voice.Culture}]");
                }
            }
            else
            {
                Debug.LogWarning("[VocabularyTTS] No voices available on Android! User may need to install Google TTS.");
            }
#endif
        }
        else
        {
            Debug.LogError("[VocabularyTTS] Speaker.Instance is still null after initialization!");
        }
    }

    /// <summary>
    /// 단어를 발음합니다.
    /// </summary>
    /// <param name="word">발음할 단어</param>
    /// <param name="rate">발음 속도 (0.5 ~ 2.0, 기본 1.0)</param>
    public void SpeakWord(string word, float rate = 1.0f)
    {
        if (string.IsNullOrEmpty(word))
        {
            Debug.LogWarning("[VocabularyTTS] Cannot speak empty word.");
            return;
        }

        Debug.Log($"[VocabularyTTS] Attempting to speak: {word}");

        // RT-Voice Speaker 상태 체크
        if (Speaker.Instance == null)
        {
            Debug.LogError("[VocabularyTTS] Speaker.Instance is NULL! RT-Voice is not initialized.");
            return;
        }

        Debug.Log($"[VocabularyTTS] Speaker.Instance exists. Platform: {Application.platform}");

        // voiceSelector null 체크
        if (voiceSelector == null)
        {
            Debug.LogError("[VocabularyTTS] voiceSelector is NULL!");
            return;
        }

        var voice = voiceSelector.SelectBestVoiceForEnglish();
        if (voice == null)
        {
            Debug.LogWarning("[VocabularyTTS] Engine is not ready yet. Please wait a moment for voices to load.");
            
            // 디버깅을 위해 가능한 상태 출력
            if (Speaker.Instance != null && Speaker.Instance.Voices != null)
            {
                Debug.Log($"[VocabularyTTS] Current internal voices count: {Speaker.Instance.Voices.Count}");
            }
            return;
        }

        Debug.Log($"[VocabularyTTS] Using voice: {voice.Name} ({voice.Vendor}) [{voice.Culture}]");

        try
        {
            Speaker.Instance.Speak(word, null, voice, true, rate);
            Debug.Log($"[VocabularyTTS] Speak command sent successfully for: {word}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VocabularyTTS] Exception while speaking: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 단어를 발음하고 완료 시 콜백을 호출합니다.
    /// </summary>
    /// <param name="word">발음할 단어</param>
    /// <param name="onComplete">완료 콜백</param>
    /// <param name="rate">발음 속도</param>
    public void SpeakWithCallback(string word, Action onComplete, float rate = 1.0f)
    {
        if (string.IsNullOrEmpty(word))
        {
            Debug.LogWarning("[VocabularyTTS] Cannot speak empty word.");
            onComplete?.Invoke();
            return;
        }

        var voice = voiceSelector.SelectBestVoiceForEnglish();
        if (voice == null)
        {
            Debug.LogError("[VocabularyTTS] No suitable voice found.");
            onComplete?.Invoke();
            return;
        }

        // 발음 시작
        Speaker.Instance.Speak(word, null, voice, true, rate);

        // 코루틴으로 완료 대기
        StartCoroutine(WaitForSpeakComplete(onComplete));
    }

    /// <summary>
    /// 발음 완료를 대기하는 코루틴
    /// </summary>
    private System.Collections.IEnumerator WaitForSpeakComplete(Action onComplete)
    {
        // 발음이 끝날 때까지 대기
        while (Speaker.Instance.isSpeaking)
        {
            yield return null;
        }

        // 완료 콜백 호출
        onComplete?.Invoke();
    }

    /// <summary>
    /// 현재 재생 중인 음성을 중지합니다.
    /// </summary>
    public void Stop()
    {
        Speaker.Instance.Silence();
    }

    /// <summary>
    /// 현재 음성이 재생 중인지 확인합니다.
    /// </summary>
    public bool IsSpeaking()
    {
        return Speaker.Instance.isSpeaking;
    }
}
