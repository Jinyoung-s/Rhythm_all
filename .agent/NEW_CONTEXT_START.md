# 새로운 AI 세션 시작 템플릿

## 📋 새로운 대화를 시작할 때 이렇게 요청하세요:

---

### 1단계: 프로젝트 컨텍스트 로드

```
안녕! 나는 Rhythm English 프로젝트를 진행 중이야.

먼저 다음 파일을 읽어서 프로젝트를 이해해줘:
- c:\Users\User\Rhythm English_urp\.agent\README.md

그리고 이 워크플로우도 확인해줘:
- c:\Users\User\Rhythm English_urp\.agent\workflows\whisper-automation.md

프로젝트를 이해했으면 "준비 완료! 노래를 처리할 준비가 되었습니다."라고 알려줘.
```

---

### 2단계: 노래 처리 요청

준비가 완료되면, 아래 템플릿을 복사해서 필요한 정보만 수정해서 보내세요:

```
다음 노래를 처리해줘:

**파일 정보:**
- 챕터 ID: pvb_chap_002
- 스텝 ID: step_001
- 노래 제목: Daily Actions
- 핵심 패턴: look at, look for, look after
- 주제/테마: Daily life verbs with 'look'
- 난이도: 2
- 입력 폴더: C:\Project\WhisperX_Input\pvb_chap_002

**작업:**
1. WhisperX로 가사 추출
2. 품사/역할 분석
3. 학습 콘텐츠 생성 (8개)
4. 퀴즈 문제 생성 (15개)
5. 모든 JSON 파일 생성 및 Unity 프로젝트에 저장

참조 가사 파일(step_001_lyrics.txt)이 있으면 검증에 사용해줘.
```

---

## 🎯 실제 사용 예시

### 예시 1: 기본 요청
```
다음 노래를 처리해줘:

- 챕터 ID: pvb_chap_001
- 스텝 ID: step_003
- 노래 제목: Going Places
- 핵심 패턴: stand up, step back, come back
- 주제/테마: Movement and Transition
- 난이도: 1
- 입력 폴더: C:\Project\WhisperX_Input\pvb_chap_001
```

### 예시 2: 간단한 요청 (최소 정보만)
```
노래 처리:
- pvb_chap_001 / step_004
- 제목: Little Struggles
- 패턴: hold on, hang up, figure out
- 난이도: 1
```

---

## ⚙️ 파일 준비 미리 체크

노래를 처리하기 전에 파일이 준비되었는지 확인하려면:

```
C:\Project\WhisperX_Input\pvb_chap_002\ 폴더를 확인해줘.
step_001.mp3 파일이 있는지, 참조 가사 파일(step_001_lyrics.txt)이 있는지 알려줘.
```

---

## 🔧 문제가 생겼을 때

### JSON 수정 요청
```
방금 생성한 step_001_learn.json에서 2번째 학습 항목의 번역을 
"~할 수 있나요?"에서 "~해 주시겠어요?"로 바꿔줘.
```

### 특정 단계만 다시 실행
```
step_001의 퀴즈 문제만 다시 생성해줘.
이번에는 난이도를 조금 더 낮춰서.
```

### 결과 확인
```
방금 생성한 파일들의 통계를 알려줘:
- 몇 개의 문장이 있는지
- 몇 개의 고유 단어가 있는지
- 학습 항목과 퀴즈 문제 개수
```

---

## 💡 팁

1. **한 번에 하나씩**: 처음에는 노래 1개씩 처리하면서 결과 확인
2. **참조 가사 제공**: 가능하면 lyrics.txt 파일을 제공하면 정확도 향상
3. **검토 후 수정**: AI가 생성한 내용은 검토 후 필요시 수정 요청
4. **명확한 패턴**: 핵심 패턴을 명확히 지정하면 더 좋은 학습 콘텐츠 생성

---

## 📝 현재 작업 중인 파일들

이 프로젝트에는 다음 챕터들이 있습니다:
- `pvb_chap_001`: Phrasal Verbs Basics Chapter 1
- `pvb_chap_002`: Phrasal Verbs Basics Chapter 2
- `beg_chap_001`: Beginner Chapter 1
- `foun_chap_002`: Foundation Chapter 2

새로운 챕터를 추가하려면 먼저 알려주세요!
