using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class WordOrderController : MonoBehaviour
{
    [Serializable]
    public class Question
    {
        public string sentence;
        public Question(string s) { sentence = s; }
    }

    // 샘플 문제 (추후 JSON/ScriptableObject로 대체 가능)
    [SerializeField]
    private List<Question> questions = new List<Question>
    {
        new Question("I’m studying English because I want to travel."),
        new Question("She doesn't like cold weather in Toronto."),
        new Question("If I had known about the problem, I would have helped you.")
    };

    UIDocument _doc;
    VisualElement _root, _slots, _bank, _toast;
    Button _undoBtn, _clearBtn, _shuffleBtn, _hintBtn, _skipBtn, _checkBtn;
    Label _accLabel, _streakLabel, _timerLabel;
    ProgressBar _progress;

    readonly List<string> _chosen = new();
    readonly List<string> _bankTokens = new();
    readonly System.Random _rng = new System.Random();

    int _idx = 0, _streak = 0, _attempts = 0, _correct = 0;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (_doc == null)
            _doc = GetComponent<UIDocument>();

        if (_doc == null || _doc.rootVisualElement == null)
        {
            Debug.LogError("UIDocument or rootVisualElement is not set up correctly.");
            return;
        }

        // UIElements 로딩이 완료된 뒤에 SetupUI 실행
        _doc.rootVisualElement.schedule.Execute(() =>
        {
            SetupUI();
        }).StartingIn(0);
    }

private void SetupUI()
{
    _root = _doc.rootVisualElement;

    var page = _root.Q<VisualElement>("Page"); // 상위 컨테이너
    if (page == null)
    {
        Debug.LogError("Page not found in UXML.");
        return;
    }

    _slots = page.Q<VisualElement>("Slots");
    _bank = page.Q<VisualElement>("Bank");
    _toast = _root.Q<VisualElement>("Toast");

    _undoBtn = page.Q<Button>("UndoButton");
    _clearBtn = page.Q<Button>("ClearButton");
    _shuffleBtn = page.Q<Button>("ShuffleButton");
    _hintBtn = page.Q<Button>("HintButton");
    _skipBtn = page.Q<Button>("SkipButton");
    _checkBtn = page.Q<Button>("CheckButton");

    _accLabel = page.Q<Label>("AccLabel");
    _streakLabel = page.Q<Label>("StreakLabel");
    _timerLabel = _root.Q<Label>("TimerLabel");
    _progress = _root.Q<ProgressBar>("ProgressBar");

    // 이벤트 바인딩
    _undoBtn?.RegisterCallback<ClickEvent>(_ => Undo());
    _clearBtn?.RegisterCallback<ClickEvent>(_ => ClearAll());
    _shuffleBtn?.RegisterCallback<ClickEvent>(_ => ShuffleBank());
    _hintBtn?.RegisterCallback<ClickEvent>(_ => ShowToast("문장 끝에는 문장부호도 포함됩니다."));
    _skipBtn?.RegisterCallback<ClickEvent>(_ => { _streak = 0; Next(false); UpdateStats(); });
    _checkBtn?.RegisterCallback<ClickEvent>(_ => Check());

    LoadQuestion(_idx);
    UpdateStats();
}

    // 토큰화: 아포스트로피/쉼표/마침표 보정
    static string[] Tokenize(string s)
    {
        if (string.IsNullOrEmpty(s)) return Array.Empty<string>();
        s = s.Replace('’', '\'');
        s = Regex.Replace(s, "([.?!])$", " $1");
        s = s.Replace(",", " ,");
        return Regex.Split(s.Trim(), "\\s+").Where(t => !string.IsNullOrEmpty(t)).ToArray();
    }

    static string Normalize(string s)
    {
        s = s.Replace('’', '\'');
        s = Regex.Replace(s.Trim(), "\\s+", " ");
        return s;
    }

    void LoadQuestion(int idx)
    {
        _chosen.Clear();
        _bankTokens.Clear();

        var tokens = Tokenize(questions[idx].sentence);
        foreach (var t in tokens) _bankTokens.Add(t);
        Shuffle(_bankTokens);

        Render();
        UpdateProgress();
    }

    void Render()
    {
        _slots.Clear();
        _bank.Clear();

        // 선택된 토큰 렌더
        for (int i = 0; i < _chosen.Count; i++)
        {
            int ci = i;
            var chip = new VisualElement();
            chip.AddToClassList("chip");

            var label = new Label(_chosen[ci]);
            var x = new Button(() => RemoveFromChosen(ci)) { text = "×" };
            x.AddToClassList("chip-x");

            chip.Add(label);
            chip.Add(x);
            _slots.Add(chip);
        }

        // 가이드
        if (_chosen.Count == 0)
        {
            var guide = new Label("여기에 단어가 쌓입니다");
            guide.AddToClassList("slot-guide");
            _slots.Add(guide);
        }

        // 은행 렌더
        for (int i = 0; i < _bankTokens.Count; i++)
        {
            int bi = i; // 클로저 캡처 주의
            var btn = new Button(() => AddToChosen(bi)) { text = _bankTokens[bi] };
            btn.AddToClassList("chip");
            _bank.Add(btn);
        }
    }

    void AddToChosen(int bankIndex)
    {
        if (bankIndex < 0 || bankIndex >= _bankTokens.Count) return;
        string t = _bankTokens[bankIndex];
        _bankTokens.RemoveAt(bankIndex);
        _chosen.Add(t);
        Render();
    }

    void RemoveFromChosen(int chosenIndex)
    {
        if (chosenIndex < 0 || chosenIndex >= _chosen.Count) return;
        string t = _chosen[chosenIndex];
        _chosen.RemoveAt(chosenIndex);
        _bankTokens.Add(t);
        Render();
    }

    void ClearAll()
    {
        _bankTokens.AddRange(_chosen);
        _chosen.Clear();
        Render();
    }

    void Undo()
    {
        if (_chosen.Count == 0) return;
        string t = _chosen[_chosen.Count - 1];
        _chosen.RemoveAt(_chosen.Count - 1);
        _bankTokens.Add(t);
        Render();
    }

    void ShuffleBank()
    {
        Shuffle(_bankTokens);
        Render();
    }

    void Check()
    {
        _attempts++;
        var q = questions[_idx];
        string ans = Normalize(string.Join(" ", _chosen));
        string gold = Normalize(string.Join(" ", Tokenize(q.sentence)));

        if (ans == gold)
        {
            _correct++; _streak++;
            ShowToast("정답입니다! 👍", true);
            Next(true);
        }
        else
        {
            _streak = 0;
            ShowToast("다시 시도해보세요. 힌트: 시작은 \"" + Tokenize(q.sentence).FirstOrDefault() + "…\"", false);
        }

        UpdateStats();
    }

    void UpdateStats()
    {
        int acc = _attempts > 0 ? Mathf.RoundToInt((_correct / (float)_attempts) * 100f) : 100;
        if (_accLabel != null) _accLabel.text = $"정확도 {acc}%";
        if (_streakLabel != null) _streakLabel.text = $"연속 ×{_streak}";     

    }

    void UpdateProgress()
    {
        Debug.LogError("Progress Update");
        if (_progress == null)
        {
          Debug.LogError("ProgressBar not found.");
          return;  
        } 
        float step = 100f / Mathf.Max(1, questions.Count);
        _progress.value = step * (_idx + 1);
    }

    void Next(bool wasCorrect)
    {
        _idx = (_idx + 1) % questions.Count;
        LoadQuestion(_idx);
    }

    void ShowToast(string msg, bool ok = true)
    {
        _toast.Clear();
        _toast.RemoveFromClassList("toast-ok");
        _toast.RemoveFromClassList("toast-bad");
        _toast.AddToClassList(ok ? "toast-ok" : "toast-bad");
        _toast.style.display = DisplayStyle.Flex;
        _toast.Add(new Label(msg));
        StopAllCoroutines();
        StartCoroutine(HideToastAfter(1.6f));
    }

    IEnumerator HideToastAfter(float sec)
    {
        yield return new WaitForSeconds(sec);
        _toast.style.display = DisplayStyle.None;
    }

    void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}