# 🎵 노래 처리 간단 템플릿

## 📝 사용 방법

### 1️⃣ 먼저 파일 준비
노래 파일을 이 폴더에 넣으세요:
```
C:\Project\WhisperX_Input\{챕터명}\
```

예시:
```
C:\Project\WhisperX_Input\pvb_chap_002\
├── step_001.mp3           ← 노래 파일 (필수)
└── step_001_lyrics.txt    ← 가사 (선택, 있으면 더 정확함)
```

### 2️⃣ 새 대화창에서 이렇게 시작

#### Option A: 한 번에 (처음 사용)
```
Rhythm English 프로젝트 시작

프로젝트: c:\Users\User\Rhythm English_urp
.agent/README.md 읽고 준비해줘
```

#### Option B: 간단하게 (이미 한 번 했으면)
```
Rhythm English 워크플로우 준비
.agent/README.md 참고
```

### 3️⃣ AI가 준비되면 아래 템플릿 사용

---

## 🎯 복사해서 사용할 템플릿 (여기만 수정!)

```
노래 처리:
- _________ / _________     ← 챕터ID / 스텝ID (예: pvb_chap_002 / step_001)
- 제목: _________________   ← 노래 제목 (예: Daily Actions)
- 패턴: _________________   ← 핵심 표현들, 쉼표로 구분 (예: look at, look for, look after)
- 테마: _________________   ← 주제 (예: Daily life verbs)
- 난이도: ___               ← 1~5 숫자 (1이 가장 쉬움)
```

---

## 📋 실제 예시들

### 예시 1: Phrasal Verbs 챕터
```
노래 처리:
- pvb_chap_001 / step_003
- 제목: Going Places
- 패턴: stand up, step back, come back
- 테마: Movement and Transition
- 난이도: 1
```

### 예시 2: 새로운 챕터 시작
```
노래 처리:
- pvb_chap_002 / step_001
- 제목: Daily Actions
- 패턴: look at, look for, look after
- 테마: Daily life with 'look'
- 난이도: 2
```

### 예시 3: Beginner 챕터
```
노래 처리:
- beg_chap_001 / step_005
- 제목: My Day
- 패턴: I am, I have, I like
- 테마: Talking about yourself
- 난이도: 1
```

---

## 🔧 단계별 체크리스트

### ✅ 파일 준비
- [ ] 챕터 폴더 만들기: `C:\Project\WhisperX_Input\{챕터명}\`
- [ ] 노래 파일 넣기: `{스텝명}.mp3`
- [ ] (선택) 가사 넣기: `{스텝명}_lyrics.txt`

### ✅ 정보 준비
- [ ] **챕터 ID**: 어떤 챕터? (pvb_chap_001, pvb_chap_002, beg_chap_001 등)
- [ ] **스텝 ID**: 몇 번째 노래? (step_001, step_002, step_003 등)
- [ ] **제목**: 노래 제목은?
- [ ] **패턴**: 핵심 표현 3-5개 (쉼표로 구분)
- [ ] **테마**: 한 줄로 주제 설명
- [ ] **난이도**: 1(초급) ~ 5(고급)

### ✅ 실행
- [ ] 새 대화 시작
- [ ] "Rhythm English 워크플로우 준비" 입력
- [ ] 위 템플릿에 정보 채워서 입력
- [ ] AI가 JSON 5개 생성할 때까지 대기 (5-10분)

### ✅ 확인
- [ ] Unity 프로젝트에서 JSON 파일 확인
- [ ] 노래 재생 테스트
- [ ] 필요하면 수정 요청

---

## 💡 빠른 참조

### 현재 사용 중인 챕터들
| 챕터 ID | 설명 |
|---------|------|
| `pvb_chap_001` | Phrasal Verbs Basics 1 |
| `pvb_chap_002` | Phrasal Verbs Basics 2 |
| `beg_chap_001` | Beginner Chapter 1 |
| `foun_chap_002` | Foundation Chapter 2 |

### 스텝 번호 규칙
- `step_001`, `step_002`, `step_003` ...
- 각 챕터마다 step_001부터 시작
- 3자리 숫자로 (001, 002, ...)

### 난이도 가이드
- **1**: 초급 (단순 현재형, 기본 단어)
- **2**: 초중급 (과거형, 일상 표현)
- **3**: 중급 (다양한 시제, 관용구)
- **4**: 중고급 (복잡한 문장, 추상적 개념)
- **5**: 고급 (고급 어휘, 복잡한 구조)

---

## 🎯 한눈에 보는 프로세스

```
1. 파일 준비
   C:\Project\WhisperX_Input\pvb_chap_002\step_001.mp3
   
2. 새 대화 시작
   "Rhythm English 워크플로우 준비"
   
3. 템플릿 채우기
   노래 처리:
   - pvb_chap_002 / step_001
   - 제목: Daily Actions
   - 패턴: look at, look for, look after
   - 테마: Daily life verbs
   - 난이도: 2
   
4. 대기 (5-10분)
   
5. 결과 확인
   Assets\Resources\json\pvb_chap_002\
   ├── step_001_singalong.json ✓
   ├── step_001_role.json ✓
   ├── step_001_learn.json ✓
   ├── step_001_test.json ✓
   └── step_001_lyrics.json ✓
```

---

## 🆘 자주 묻는 질문

**Q: 챕터 이름은 어떻게 정하나요?**
A: `{타입}_chap_{번호}` 형식
   - pvb = Phrasal Verbs Basics
   - beg = Beginner
   - foun = Foundation
   - int = Intermediate
   
**Q: 패턴은 몇 개까지 넣을 수 있나요?**
A: 3-5개 정도가 적당합니다. 너무 많으면 집중도가 떨어집니다.

**Q: 가사 파일(lyrics.txt)은 필수인가요?**
A: 필수는 아니지만, 있으면 WhisperX 결과를 검증해서 더 정확합니다.

**Q: 여러 노래를 한 번에 처리할 수 있나요?**
A: 가능합니다! 노래 정보를 여러 개 나열하면 됩니다:
```
노래 3개 처리:

1번:
- pvb_chap_002 / step_001
- 제목: Daily Actions
...

2번:
- pvb_chap_002 / step_002
- 제목: Morning Routine
...

3번:
- pvb_chap_002 / step_003
- 제목: Evening Calm
...
```

---

저장해서 매번 복사하세요! 📋
