using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource instrumentalSource;
    public AudioSource vocalSource;

    public void PlayInstrumental()
    {
        if (instrumentalSource == null)
        {
            Debug.LogError("[AudioManager] Instrumental AudioSource is not assigned.");
            return;
        }

        var dataManager = GameDataManager.Instance;
        var step = dataManager.CurrentStep ?? StepResourceResolver.CreateFallbackStep();
        var chapterId = string.IsNullOrEmpty(dataManager.CurrentChapterId)
            ? StepResourceResolver.GetFallbackChapterId()
            : dataManager.CurrentChapterId;

        var clip = StepResourceResolver.LoadSongClip(chapterId, step);
        if (clip == null)
        {
            Debug.LogError($"[AudioManager] Unable to load clip for chapter '{chapterId}' step '{step.id}'.");
            return;
        }

        Debug.Log($"[AudioManager] Playing clip '{clip.name}' for {chapterId}/{step.id}.");
        instrumentalSource.clip = clip;
        instrumentalSource.Play();
    }

    public void PlayVocal()
    {
        if (vocalSource == null)
        {
            Debug.LogWarning("[AudioManager] Vocal AudioSource is not assigned.");
            return;
        }

        if (vocalSource.clip == null)
        {
            Debug.LogWarning("[AudioManager] Vocal AudioSource has no clip assigned.");
            return;
        }

        vocalSource.Play();
    }

    public void PauseMusic()
    {
        if (instrumentalSource != null)
        {
            instrumentalSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (instrumentalSource != null)
        {
            instrumentalSource.UnPause();
        }
    }

    public void StopMusic()
    {
        if (instrumentalSource != null)
        {
            instrumentalSource.Stop();
        }
    }
}