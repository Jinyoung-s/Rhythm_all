using UnityEngine;

public class AndroidSpeechBridge : MonoBehaviour
{
    private static AndroidJavaObject activity;
    private StepTestManager _stepManager;

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }
#endif
        _stepManager = GetComponent<StepTestManager>();
    }

    public void StartListening()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var speechBridge = new AndroidJavaClass("com.so.speechplugin.SpeechBridge"))
        {
            speechBridge.CallStatic("startListening", activity, gameObject.name, "OnSpeechResult");
        }
#else
        Debug.LogWarning("StartListening() is only available on Android device.");
#endif
    }

    public void OnSpeechResult(string result)
    {
        Debug.Log($"🎤 STT Result: {result}");

        if (_stepManager != null)
        {
            _stepManager.OnSpeechRecognized(result);
        }
    }
}
