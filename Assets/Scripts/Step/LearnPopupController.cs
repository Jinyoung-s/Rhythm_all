using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class LearnPopupController : MonoBehaviour
{
    private VisualElement popupContainer;
    private VisualElement grammarBox;
    private VisualElement examplesContainer;

    private Label sentenceLabel;
    private Label translationLabel;
    private Label grammarNoteLabel;

    private Button closeButton;
    private Button playAudioButton;
    private Button saveSentenceButton;
    private Button toggleGrammarButton;
    private Button nextButton;

    private Label progressLabel;
    private Button prevButton;    

    private List<LearnStepData> currentSteps;
    private int currentIndex = 0;

    private bool isGrammarVisible = false;

    private VisualElement translationBubble;
    private Label bubbleText;    

    private bool isBubbleVisible = false;

    void Start()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogError("[LearnPopupController] UIDocument is missing.");
            return;
        }

        var root = doc.rootVisualElement;

        popupContainer = root.Q<VisualElement>("popupContainer");
        sentenceLabel = root.Q<Label>("sentenceLabel");
        translationLabel = root.Q<Label>("translationLabel");
        grammarNoteLabel = root.Q<Label>("grammarNoteLabel");

        grammarBox = root.Q<VisualElement>("grammarBox");
        examplesContainer = root.Q<VisualElement>("examplesContainer");

        closeButton = root.Q<Button>("closeButton");
        playAudioButton = root.Q<Button>("playAudioButton");
        saveSentenceButton = root.Q<Button>("saveSentenceButton");
        toggleGrammarButton = root.Q<Button>("toggleGrammarButton");
        nextButton = root.Q<Button>("nextButton");

        progressLabel = root.Q<Label>("progressLabel");
        prevButton = root.Q<Button>("prevButton");

        translationBubble = root.Q<VisualElement>("translationBubble");
        bubbleText = root.Q<Label>("bubbleText");

        if (prevButton != null) prevButton.clicked += OnPrevClicked;

        // ✅ closeButton이 null이면 클릭 연결 자체가 안 됩니다.
        if (closeButton != null) closeButton.clicked += Hide;
        if (playAudioButton != null) playAudioButton.clicked += OnPlayAudioClicked;
        if (saveSentenceButton != null) saveSentenceButton.clicked += OnSaveSentenceClicked;
        if (toggleGrammarButton != null) toggleGrammarButton.clicked += ToggleGrammar;
        if (nextButton != null) nextButton.clicked += OnNextClicked;

        // 시작 시 숨김
        if (popupContainer != null)
            popupContainer.style.display = DisplayStyle.None;

        popupContainer.RegisterCallback<ClickEvent>(OnBackgroundClicked);            
    }

    // ✅ 여러개 표시
    public void Show(List<LearnStepData> steps)
    {
        if (popupContainer == null) return;
        if (steps == null || steps.Count == 0) return;

        currentSteps = steps;
        currentIndex = 0;

        popupContainer.style.display = DisplayStyle.Flex;
        StartCoroutine(FadeIn());

        UpdateView();

        // ✅ 형님 요청: 다음 문장이 없으면 NEXT 숨김 (닫기만 사용)
        if (nextButton != null)
        {
            nextButton.style.display = (steps.Count > 1) ? DisplayStyle.Flex : DisplayStyle.None;
            nextButton.text = "NEXT"; // 항상 NEXT
        }
    }

    // (옵션) 기존 단일 호출 유지하고 싶으면 남겨도 됨
    public void Show(LearnStepData data)
    {
        if (data == null) return;
        Show(new List<LearnStepData> { data });
    }

    private void UpdateView()
    {
        if (currentSteps == null || currentSteps.Count == 0)
            return;

        var data = currentSteps[currentIndex];

        // ✅ 문장/번역/설명
        if (sentenceLabel != null)
        {
            sentenceLabel.text = ApplyHighlights(
                data.sentence,
                data.highlights
            );
        }

        if (translationLabel != null) translationLabel.text = data.translation;
        if (grammarNoteLabel != null) grammarNoteLabel.text = data.grammarNote;

        // ✅ examples
        if (examplesContainer != null)
        {
            examplesContainer.Clear();

            if (data.examples != null)
            {
                foreach (var ex in data.examples)
                {
                    if (string.IsNullOrEmpty(ex.sentence))
                        continue;

                    var label = new Label($"›  {ex.sentence}");
                    label.AddToClassList("example-line");

                    // ✅ 2단계 핵심: 클릭 이벤트 연결
                        label.RegisterCallback<ClickEvent>(evt =>
                        {
                            ShowTranslationBubble(label, ex.translation);
                            evt.StopPropagation();
                        });

                    examplesContainer.Add(label);
                }
            }
        }

        // ✅ 진행 표시: "1 / N"
        if (progressLabel != null)
        {
            progressLabel.text = $"{currentIndex + 1} / {currentSteps.Count}";
        }

        // ✅ PREV 버튼: 첫 문장에서는 숨김
        if (prevButton != null)
        {
            prevButton.style.display = (currentIndex > 0) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ✅ NEXT 버튼: 다음 문장이 없으면 숨김 (형님 요구사항)
        if (nextButton != null)
        {
            bool hasNext = (currentIndex < currentSteps.Count - 1);
            nextButton.style.display = hasNext ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ✅ 설명 영역 기본 닫힘 + 버튼 텍스트 초기화
        isGrammarVisible = false;
        if (grammarBox != null) grammarBox.AddToClassList("hidden");
        if (toggleGrammarButton != null) toggleGrammarButton.text = "ⓘ 표현 설명 보기";

        // ✅ 저장 여부 업데이트
        UpdateSaveButtonState();

        // (선택) 설명 열려있을 때 예문 스크롤이 남아있으면 초기화하고 싶으면 아래 활성화
        // if (examplesScrollView != null) examplesScrollView.scrollOffset = Vector2.zero;
    }


    private void OnNextClicked()
    {
        // nextButton은 steps.Count > 1일 때만 보이므로 여기서는 “다음”만 처리
        if (currentSteps == null) return;

        if (currentIndex < currentSteps.Count - 1)
        {
            currentIndex++;
            UpdateView();
        }
        else
        {
            // 마지막에서 NEXT가 눌리는 경우는 사실상 거의 없음(UX상)
            // 그래도 안전하게 닫기
            Hide();
        }
    }

    private void OnPrevClicked()
    {
        if (currentSteps == null) return;

        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateView();
        }
    }    

    private void ToggleGrammar()
    {
        if (grammarBox == null || toggleGrammarButton == null) return;

        isGrammarVisible = !isGrammarVisible;

        if (isGrammarVisible)
        {
            grammarBox.RemoveFromClassList("hidden");
            toggleGrammarButton.text = "ⓘ 표현 설명 닫기";
        }
        else
        {
            grammarBox.AddToClassList("hidden");
            toggleGrammarButton.text = "ⓘ 표현 설명 보기";
        }
    }

    public void Hide()
    {
        if (popupContainer == null) return;

        HideTranslationBubble();
        StartCoroutine(FadeOut());
    }

    private void OnPlayAudioClicked()
    {
        Debug.Log("🔊 Play audio button clicked");
    }

    private void OnSaveSentenceClicked()
    {
        if (currentSteps == null || currentIndex < 0 || currentIndex >= currentSteps.Count)
            return;

        var data = currentSteps[currentIndex];
        bool isAlreadySaved = SentenceManager.Instance.IsSaved(data.stepId);

        if (isAlreadySaved)
        {
            SentenceManager.Instance.RemoveSavedSentence(data.stepId);
        }
        else
        {
            SentenceManager.Instance.SaveSentence(data.stepId, data.sentence, data.translation);
        }

        UpdateSaveButtonState();
    }

    private void UpdateSaveButtonState()
    {
        if (saveSentenceButton == null || currentSteps == null || currentIndex >= currentSteps.Count)
            return;

        var data = currentSteps[currentIndex];
        bool isSaved = SentenceManager.Instance.IsSaved(data.stepId);

        if (isSaved)
        {
            saveSentenceButton.text = "★ Saved";
            saveSentenceButton.AddToClassList("saved");
        }
        else
        {
            saveSentenceButton.text = "☆ Save";
            saveSentenceButton.RemoveFromClassList("saved");
        }
    }

    IEnumerator FadeIn()
    {
        popupContainer.style.opacity = 0;
        popupContainer.style.display = DisplayStyle.Flex;

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            popupContainer.style.opacity = Mathf.Lerp(0, 1, elapsed / 0.3f);
            yield return null;
        }
        popupContainer.style.opacity = 1;
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            popupContainer.style.opacity = Mathf.Lerp(1, 0, elapsed / 0.3f);
            yield return null;
        }

        popupContainer.style.display = DisplayStyle.None;
        popupContainer.style.opacity = 1;
    }

    private string ApplyHighlights(string sentence, List<HighlightData> highlights)
    {
        if (string.IsNullOrEmpty(sentence) || highlights == null)
            return sentence;

        string result = sentence;

        foreach (var h in highlights)
        {
            if (string.IsNullOrEmpty(h.text) || string.IsNullOrEmpty(h.color))
                continue;

            result = result.Replace(
                h.text,
                $"<color={h.color}>{h.text}</color>"
            );
        }

        return result;
    }

    private void ShowTranslationBubble(VisualElement target, string translation)
    {
        if (translationBubble == null || bubbleText == null || popupContainer == null)
            return;

        bubbleText.text = translation;

        // 기준 좌표 계산
        Vector2 worldPos = target.worldBound.position;
        Vector2 localPos = popupContainer.WorldToLocal(worldPos);

        float margin = 12f;

        // 먼저 임시로 표시해서 실제 높이를 계산
        translationBubble.RemoveFromClassList("hidden");
        translationBubble.style.left = localPos.x;

        // 레이아웃 갱신 강제 (중요)
        translationBubble.MarkDirtyRepaint();

        float bubbleHeight = translationBubble.layout.height;
        float bubbleWidth = translationBubble.layout.width;

        float containerHeight = popupContainer.layout.height;

        // 기본: 아래에 배치
        float belowY = localPos.y + target.layout.height + margin;
        float aboveY = localPos.y - bubbleHeight - margin;

        // 🔥 아래 공간 부족하면 위로
        if (belowY + bubbleHeight > containerHeight)
        {
            translationBubble.style.top = Mathf.Max(aboveY, margin);
        }
        else
        {
            translationBubble.style.top = belowY;
        }

        isBubbleVisible = true;
    }

    private void OnBackgroundClicked(ClickEvent evt)
    {
        if (!isBubbleVisible || translationBubble == null)
            return;

        // 말풍선 내부 클릭이면 무시
        if (translationBubble.worldBound.Contains(evt.position))
            return;

        HideTranslationBubble();
    }    

    private void HideTranslationBubble()
    {
        if (translationBubble == null)
            return;

        translationBubble.AddToClassList("hidden");
        isBubbleVisible = false;
    }    

}
