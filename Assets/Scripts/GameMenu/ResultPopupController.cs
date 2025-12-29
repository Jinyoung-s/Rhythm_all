using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ResultPopupController : MonoBehaviour
{
    private VisualElement root;

    // UI Elements
    private Label scoreValue;
    private Label accuracyValue;
    private Label maxComboValue;

    private Label perfectValue;
    private Label greatValue;
    private Label goodValue;
    private Label missValue;

    private Label coinValue;

    private Button confirmButton;

        void Awake()
        {
            var ui = GetComponent<UIDocument>();
            root = ui.rootVisualElement;

            // 상단 점수
            scoreValue    = root.Q<Label>("ScoreValue");

            // 스탯 (UXML에 name 추가한 것들)
            accuracyValue = root.Q<Label>("AccuracyValue");
            maxComboValue = root.Q<Label>("MaxComboValue");

            perfectValue  = root.Q<Label>("PerfectValue");
            greatValue    = root.Q<Label>("GreatValue");
            goodValue     = root.Q<Label>("GoodValue");
            missValue     = root.Q<Label>("MissValue");

            // 코인
            coinValue     = root.Q<Label>("CoinValue");

            // 버튼
            confirmButton = root.Q<Button>("RetryButton");
            if (confirmButton != null)
                confirmButton.clicked += OnConfirm;

            Debug.Log($"[ResultPopupController] Awake - " +
                    $"Score:{scoreValue != null}, Acc:{accuracyValue != null}, " +
                    $"Perfect:{perfectValue != null}, Coin:{coinValue != null}");
        }

    public void SetResult(ResultData data)
    {
        if (scoreValue != null)
            scoreValue.text = data.Score.ToString("N0");

        if (accuracyValue != null)
            accuracyValue.text = $"{data.Accuracy:0.0}%";

        if (maxComboValue != null)
            maxComboValue.text = data.MaxCombo.ToString();

        if (perfectValue != null)
            perfectValue.text = data.Perfect.ToString();

        if (greatValue != null)
            greatValue.text = data.Great.ToString();

        if (goodValue != null)
            goodValue.text = data.Good.ToString();

        if (missValue != null)
            missValue.text = data.Miss.ToString();

        if (coinValue != null)
            coinValue.text = "+" + data.RewardCoin.ToString();

        Debug.Log("[ResultPopupController] SetResult applied.");
    }

    private void OnConfirm()
    {
        // 팝업 닫기
        gameObject.SetActive(false);
        SceneNavigator.Load("StepScene");
        // 이어서 원하는 기능 호출 가능 (예: 씬 전환)
        // SceneManager.LoadScene("MainMenu");
    }
}


// 형님의 Result 데이터 구조
public struct ResultData
{
    public int Score;
    public float Accuracy;
    public int MaxCombo;

    public int Perfect;
    public int Great;
    public int Good;
    public int Miss;

    public int RewardCoin;
}
