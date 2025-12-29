using UnityEngine;

public class GameManager : MonoBehaviour
{
    public AudioManager audioManager;
    //public NoteSpawner noteSpawner;
    public CountdownManager countdownManager;
   

    void Start()
    {
        audioManager.PlayInstrumental();
        //countdownManager.OnCountdownFinished += OnCountdownEnd;
    }

    void OnCountdownEnd()
    {
        //audioManager.PlayVocal();
        
        
        //noteSpawner.StartSpawning();
    }
}