using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public static class MicPermissionHelper
{
    /// <summary>
    /// 마이크 권한이 있는지 확인
    /// </summary>
    public static bool HasMicPermission()
    {
#if UNITY_ANDROID
        return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#elif UNITY_IOS
        // iOS에서는 Application.HasUserAuthorization 으로 체크 가능
        return Application.HasUserAuthorization(UserAuthorization.Microphone);
#else
        return true; // PC 등 다른 플랫폼은 권한 필요 없음
#endif
    }

    /// <summary>
    /// 마이크 권한 요청
    /// </summary>
    public static void RequestMicPermission()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#elif UNITY_IOS
        // iOS는 Info.plist 에 NSMicrophoneUsageDescription 키가 있어야 함
        Application.RequestUserAuthorization(UserAuthorization.Microphone);
#else
        Debug.Log("이 플랫폼에서는 마이크 권한 요청 불필요.");
#endif
    }
}
