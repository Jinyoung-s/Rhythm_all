using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownManager : MonoBehaviour
{
    public GameObject countdownPanel;
    public TextMeshProUGUI countdownText;

    public System.Action OnCountdownFinished;

    private void Awake()
    {
        Debug.Log($"[CountdownManager] Awake() instance={this.GetInstanceID()}");
    }

    private void OnEnable()
    {
        Debug.Log($"[CountdownManager] OnEnable() instance={this.GetInstanceID()}");
    }

    void Start()
    {
        Debug.Log($"[CountdownManager] Start() called. Starting countdown… instance={this.GetInstanceID()}");

        if (countdownPanel == null)
            Debug.LogError("[CountdownManager] countdownPanel is NULL!!");

        if (countdownText == null)
            Debug.LogError("[CountdownManager] countdownText is NULL!!");

        StartCoroutine(DoCountdown());
    }

    IEnumerator DoCountdown()
    {
        Debug.Log("[CountdownManager] DoCountdown() started.");

        countdownPanel.SetActive(true);
        Debug.Log("[CountdownManager] Show: '2'");

        countdownText.text = "2";
        yield return new WaitForSeconds(1f);

        Debug.Log("[CountdownManager] Show: '1'");
        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        Debug.Log("[CountdownManager] Show: 'Go!'");
        countdownText.text = "Go!";
        yield return new WaitForSeconds(0.7f);

        Debug.Log("[CountdownManager] Countdown finished → hiding panel");
        countdownPanel.SetActive(false);

        Debug.Log("[CountdownManager] Invoking OnCountdownFinished()");
        OnCountdownFinished?.Invoke();
    }

    private void OnDisable()
    {
        Debug.Log($"[CountdownManager] OnDisable() instance={this.GetInstanceID()}");
    }

    private void OnDestroy()
    {
        Debug.Log($"[CountdownManager] OnDestroy() instance={this.GetInstanceID()}");
    }
}
