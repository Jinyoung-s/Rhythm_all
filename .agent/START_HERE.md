# 🎵 초간단 사용법 (복사-붙여넣기만!)

## ⚡ 2단계로 끝!

---

### 1단계: 새 대화 시작 (처음 한 번만)

**이 텍스트를 복사해서 → 새 대화창에 붙여넣기:**

```
Rhythm English 프로젝트

워크플로우 준비:
- c:\Users\User\Rhythm English_urp\.agent\README.md 읽기
- .agent\workflows\whisper-automation.md 참고

준비되면 "준비 완료!"라고 알려줘
```

**AI가 "준비 완료!" 하면 → 2단계로**

---

### 2단계: 노래 정보 입력

**아래 템플릿을 복사 → 빈칸 채우기 → 붙여넣기:**

```
노래 처리:
- 챕터/스텝: pvb_chap_002 / step_001
- 제목: Daily Actions
- 패턴: look at, look for, look after
- 테마: Daily life verbs
- 난이도: 2
```

**↑ 이 5줄을 채워서 AI에게 보내면 끝!**

---

## 📋 빈칸 채우는 법

### ✏️ 수정할 부분만 표시:

```
노래 처리:
- 챕터/스텝: [여기 수정] / [여기 수정]
- 제목: [여기 수정]
- 패턴: [여기 수정]
- 테마: [여기 수정]
- 난이도: [여기 수정]
```

### 📝 구체적 예시:

**내가 추가할 노래가:**
- 챕터 2, 첫 번째 노래
- 제목이 "Wake Up Song"
- wake up, get up, turn off를 배우는 노래
- 아침 루틴 주제
- 초급 난이도

**그러면 이렇게:**
```
노래 처리:
- 챕터/스텝: pvb_chap_002 / step_001
- 제목: Wake Up Song
- 패턴: wake up, get up, turn off
- 테마: Morning routine
- 난이도: 1
```

**↑ 이거 그대로 복사해서 AI한테 보내면 됨!**

---

## 🎯 완전 처음부터 끝까지

### Step 1: 파일 넣기
```
C:\Project\WhisperX_Input\pvb_chap_002\
└── step_001.mp3  ← 노래 파일 여기
```

### Step 2: 새 대화 시작
**복사:**
```
Rhythm English 프로젝트
워크플로우 준비:
- c:\Users\User\Rhythm English_urp\.agent\README.md 읽기
준비되면 "준비 완료!"라고 알려줘
```
**→ 새 대화창에 붙여넣기**

### Step 3: AI가 "준비 완료!" 하면
**복사 (빈칸 채워서):**
```
노래 처리:
- 챕터/스텝: pvb_chap_002 / step_001
- 제목: Wake Up Song
- 패턴: wake up, get up, turn off
- 테마: Morning routine
- 난이도: 1
```
**→ 대화창에 붙여넣기**

### Step 4: 대기
AI가 자동으로 JSON 5개 만듦 (5-10분)

### Step 5: 확인
```
C:\Users\User\Rhythm English_urp\Assets\Resources\json\pvb_chap_002\
├── step_001_singalong.json ✓
├── step_001_role.json ✓
├── step_001_learn.json ✓
├── step_001_test.json ✓
└── step_001_lyrics.json ✓
```

---

## 💡 헷갈리지 않게 정리

| 무엇을 | 어디서 | 언제 |
|--------|--------|------|
| **1단계 메시지** 복사 | 이 파일 위쪽 | 새 대화 시작할 때 |
| **2단계 템플릿** 복사 | 이 파일 아래쪽 | AI 준비 완료 후 |
| 빈칸 채우기 | 5줄만 | 보내기 전에 |
| AI한테 보내기 | 새 대화창 | 채운 후 바로 |

---

## 🎬 GIF로 보면 이런 느낌

```
1. 새 대화창 열기
   ↓
2. [1단계 메시지 붙여넣기]
   "Rhythm English 프로젝트
    워크플로우 준비..."
   → 전송
   ↓
3. AI: "준비 완료!"
   ↓
4. [2단계 템플릿 빈칸 채워서 붙여넣기]
   "노래 처리:
    - 챕터/스텝: pvb_chap_002 / step_001
    - 제목: Wake Up Song
    ..."
   → 전송
   ↓
5. AI가 자동으로 처리
   ↓
6. 완료!
```

---

## ✂️ 지금 바로 복사할 것들

### 📌 1단계용 (새 대화 시작)
```
Rhythm English 프로젝트
워크플로우 준비:
- c:\Users\User\Rhythm English_urp\.agent\README.md 읽기
준비되면 "준비 완료!"라고 알려줘
```

### 📌 2단계용 (노래 정보)
```
노래 처리:
- 챕터/스텝: _________ / _________
- 제목: _________________
- 패턴: _________________
- 테마: _________________
- 난이도: ___
```

**위 두 개만 알면 됩니다!** 😊

---

## 🆘 자주 하는 실수

❌ **잘못:** 빈칸 안 채우고 보냄
```
노래 처리:
- 챕터/스텝: _________ / _________
```

✅ **올바름:** 빈칸 다 채우고 보냄
```
노래 처리:
- 챕터/스텝: pvb_chap_002 / step_001
- 제목: Wake Up Song
...
```

---

이제 명확하죠? 두 번 복사-붙여넣기만 하면 됩니다! 🚀

---

## 💡 알아두면 좋은 팁

### 📝 가사 파일에 구조 태그 넣어도 됩니다!

가사 파일(lyrics.txt)을 만들 때 이렇게 써도 됩니다:

```
Verse 1:
I got up early, five AM
I couldn't sleep, this was my chance

Chorus:
I got up, I got out, I'm on my way
I got on that bus to face the day

Verse 2:
I got in the shower, washed my fears away
```

**AI가 자동으로 "Verse 1:", "Chorus:" 같은 태그를 제거하고 순수 가사만 추출합니다!**

처리 후:
```
I got up early, five AM
I couldn't sleep, this was my chance
I got up, I got out, I'm on my way
I got on that bus to face the day
I got in the shower, washed my fears away
```

**지원하는 구조 태그:**
- Verse 1, Verse 2, Verse 3...
- Chorus, Pre-Chorus
- Bridge
- Intro, Outro
- Refrain, Hook

모두 자동으로 제거되니 편하게 쓰세요! 📝

