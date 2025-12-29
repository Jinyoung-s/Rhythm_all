using UnityEngine;

public static class GameSettings
{
    public static float AudioOffsetMs
    {
        get => PlayerPrefs.GetFloat("AudioOffset", 0f);
        set
        {
            PlayerPrefs.SetFloat("AudioOffset", value);
            PlayerPrefs.Save();
        }
    }

    public static float AudioOffsetSeconds => AudioOffsetMs / 1000f;

    // 🔥 게임1 (DSP) 적용용
    public static float GetDSPUserCalib() => AudioOffsetMs / 1000f;

    // 🔥 게임2 (AudioSource) 적용용
    public static float GetAudioSourceOffset() => AudioOffsetMs / 1000f;
}
