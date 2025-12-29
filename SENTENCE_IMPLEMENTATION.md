# Sentence Management System 구현 완료

## 📋 개요

Review 페이지에 **Wrong Sentences**와 **Saved Sentences** 기능을 구현했습니다.

- **Wrong Sentences**: 테스트에서 틀린 문장 자동 추적 및 복습
- **Saved Sentences**: 사용자가 저장한 중요 문장 관리

---

## 🎯 구조

```
Review
├─ WORDS
│  ├─ Word Test
│  └─ Word List
│
└─ SENTENCES
   ├─ Wrong Sentences (틀린 문장)
   └─ Saved Sentences (저장한 문장)
```

---

## 📁 생성된 파일

### **Scripts**
1. `SentenceManager.cs` - 문장 데이터 관리 싱글톤
2. `WrongSentencesViewController.cs` - 틀린 문장 UI 컨트롤러
3. `SavedSentencesViewController.cs` - 저장한 문장 UI 컨트롤러

### **UI Resources**
4. `WrongSentencesView.uxml` - 틀린 문장 UI
5. `WrongSentencesView.uss` - 틀린 문장 스타일
6. `SavedSentencesView.uxml` - 저장한 문장 UI
7. `SavedSentencesView.uss` - 저장한 문장 스타일

### **수정된 파일**
8. `ReviewPageController.cs` - 문장 뷰 통합
9. `ReviewPage.uxml` - Daily 5 Sentences → Wrong Sentences 변경
10. `StepTestManager.cs` - 틀린 문장 기록 통합

---

## ✨ 주요 기능

### **Wrong Sentences**

#### 자동 추적
- StepTest에서 틀린 문장 자동 수집
- 정답률 계산 (성공/시도)
- 연속 성공 횟수 추적

#### 마스터 시스템
- 3회 연속 성공 시 마스터 처리
- 마스터된 문장은 목록에서 제거
- 30일 이상 된 마스터 문장 자동 삭제

#### UI 기능
- 정답률별 색상 표시 (❌ ⚠️ ✅)
- 정답률 낮은 순 정렬
- TTS 재생 버튼
- Practice 버튼 (TODO)

#### 빈 상태
```
🎉
No Wrong Sentences!
완벽해요! 틀린 문장이 없습니다.
테스트를 완료하면 틀린 문장이 여기에 나타납니다.
```

---

### **Saved Sentences**

#### 수동 관리
- 사용자가 직접 문장 저장 (⭐ 버튼)
- 저장 날짜 표시 (방금 전, N분 전, N일 전)
- 최근 저장 순 정렬

#### UI 기능
- TTS 재생 버튼
- Practice 버튼 (TODO)
- 삭제 버튼 (🗑️)

#### 빈 상태
```
📝
No Saved Sentences
저장한 문장이 없습니다
학습 중 중요한 문장을 ⭐ 버튼으로 저장해보세요!
```

---

## 🔧 데이터 구조

### **SentenceProgress** (틀린 문장)
```csharp
{
    string sentenceId;          // 문장 고유 ID
    string sentence;            // 영어 문장
    string translation;         // 한국어 번역
    int attemptCount;           // 총 시도 횟수
    int successCount;           // 성공 횟수
    int consecutiveSuccess;     // 연속 성공
    float accuracy;             // 정답률 (%)
    DateTime lastAttempt;       // 마지막 시도
    bool isMastered;            // 마스터 여부
}
```

### **SavedSentence** (저장한 문장)
```csharp
{
    string sentenceId;
    string sentence;
    string translation;
    DateTime savedDate;
    string note;                // 선택: 메모
}
```

---

## 📊 데이터 저장

- **PlayerPrefs** 사용
- **JSON 직렬화**
- 키:
  - `wrong_sentences` - 틀린 문장 데이터
  - `saved_sentences` - 저장한 문장 데이터

---

## 🔗 통합 포인트

### **StepTestManager**
```csharp
// OnSubmitClicked() 함수에서
SentenceManager.Instance.RecordAttempt(
    sentenceId,
    sentence,
    translation,
    isCorrect
);
```

### **TODO: SingAlong/Learn 통합**
학습 중 저장 기능 추가 필요:
```csharp
// 저장 버튼 클릭 시
SentenceManager.Instance.SaveSentence(
    sentenceId,
    sentence,
    translation
);
```

---

## 🎨 디자인

- **다크 모드** 테마
- **모바일 친화적** UI
- **정답률별 색상**:
  - 🔴 빨강 (< 33%)  
  - 🟡 노랑 (33-66%)
  - 🔵 파랑 (> 66%)
- **빈 상태 메시지** 포함

---

## 🚀 사용 방법

### **사용자 흐름**

1. **테스트 완료**
   - StepTest에서 문제 풀기
   - 틀린 문장 자동 기록

2. **Review 페이지**
   - "Wrong Sentences" 카드 클릭
   - 틀린 문장 목록 확인

3. **복습**
   - 🔊 Play: 문장 듣기
   - ↻ Practice: 연습하기
   - 3회 연속 성공 시 자동 제거

4. **문장 저장** (TODO)
   - 학습 중 ⭐ 버튼 클릭
   - Saved Sentences에 저장

5. **저장한 문장 관리**
   - "Saved Sentences" 카드 클릭
   - 🗑️ 삭제 가능

---

## ✅ 완료된 것

- [x] SentenceManager 구현
- [x] Wrong Sentences UI/UX
- [x] Saved Sentences UI/UX
- [x] ReviewPageController 통합
- [x] StepTestManager 통합
- [x] 빈 상태 UI
- [x] 정답률 추적
- [x] 마스터 시스템
- [x] TTS 통합

---

## 📝 TODO

### **우선순위 높음**
- [ ] Practice 버튼 기능 구현
  - 해당 문장만 나오는 테스트 모드?
  - 또는 SingAlong 재생?
  
- [ ] 문장 저장 버튼 추가
  - SingAlong 학습 중 UI 추가
  - Learn 팝업에 저장 버튼 추가

### **우선순위 중간**
- [ ] 통계 화면
  - 주간/월간 정답률 그래프
  - 가장 많이 틀린 문장
  - 마스터한 문장 수

- [ ] 알림
  - 틀린 문장 N개 이상 시 복습 권장
  
### **우선순위 낮음**
- [ ] 메모 기능
  - SavedSentence에 note 필드 활용
  - 메모 추가/수정 UI

- [ ] 공유 기능
  - 문장 텍스트 복사
  - SNS 공유

---

## 🐛 알려진 이슈

없음 (테스트 필요!)

---

## 🔍 테스트 방법

1. **Unity에서 MainMenuScene 실행**
2. **Review 탭 클릭**
3. **"Wrong Sentences" 클릭**
   - 빈 상태 확인
4. **테스트 실행**
   - StepTest로 이동
   - 일부러 틀린 답 제출
5. **다시 Review → Wrong Sentences**
   - 틀린 문장 표시 확인
   - 정답률 표시 확인
6. **Play 버튼 클릭**
   - TTS 재생 확인
7. **3회 연속 성공 테스트**
   - 같은 문장 3번 맞히기
   - 목록에서 사라지는지 확인

---

## 📞 문의

문제 발생 시:
1. Unity Console 로그 확인
2. `[SentenceManager]` 로그 검색
3. PlayerPrefs 데이터 확인

---

**구현 완료!** 🎉
