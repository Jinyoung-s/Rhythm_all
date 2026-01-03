---
description: WhisperX를 활용한 노래 자동 변환 파이프라인
---

# WhisperX 자동화 파이프라인

## 개요
노래 파일(MP3)과 가사를 특정 폴더에 넣고 AI에게 요청하면, WhisperX 실행부터 모든 JSON 파일 생성까지 자동으로 처리합니다.

**장점:**
- ✅ 별도 Python 스크립트 불필요
- ✅ API 키 설정 불필요 (AI가 직접 처리)
- ✅ 간단한 대화만으로 모든 작업 완료
- ✅ 즉시 시작 가능

## 📁 폴더 구조

```
C:\Project\WhisperX_Input\
└── {chapter_id}\
    ├── {step_id}.mp3              # 필수: 노래 파일
    ├── {step_id}_vocal.mp3        # 선택: 보컬 트랙
    ├── {step_id}_inst.mp3         # 선택: 반주 트랙
    └── {step_id}_lyrics.txt       # 선택: 참조 가사 (한 줄에 한 문장씩)

예시:
C:\Project\WhisperX_Input\pvb_chap_001\
├── step_003.mp3
├── step_003_vocal.mp3
├── step_003_inst.mp3
└── step_003_lyrics.txt
```

## 📋 필요한 파일 형식

### 입력 파일
1. **노래 파일** (필수): `{step_id}.mp3`
2. **보컬 파일** (선택): `{step_id}_vocal.mp3`
3. **반주 파일** (선택): `{step_id}_inst.mp3`
4. **참조 가사** (선택): `{step_id}_lyrics.txt`
   - 문장별로 한 줄씩 작성
   - WhisperX 결과 검증에 사용
   - **노래 구조 태그 사용 가능** (Verse, Chorus, Bridge 등)
   - AI가 자동으로 구조 태그를 제거하고 순수 가사만 추출
   - 예시:
     ```
     Verse 1:
     I wake up when the sunlight hits my face.
     I get up and make my little space.
     
     Chorus:
     I wake up, I get up, every single day.
     
     Verse 2:
     I turn off the alarm beside my bed.
     I put on my shirt — blue and red.
     ```
   - 위 파일은 자동으로 다음과 같이 처리됨:
     ```
     I wake up when the sunlight hits my face.
     I get up and make my little space.
     I wake up, I get up, every single day.
     I turn off the alarm beside my bed.
     I put on my shirt — blue and red.
     ```

### 출력 파일 (자동 생성)
앱에서 사용하는 모든 JSON 파일이 자동으로 생성됩니다:

1. **`{chapterId}_steps.json`**: 챕터 메타데이터
   ```json
   {
     "chapterId": "pvb_chap_001",
     "steps": [
       {
         "id": "step_001",
         "title": "노래 제목",
         "sentence": "주요 표현",
         "songFile": "step_001.mp3",
         "vocalFile": "step_001_vocal.mp3",
         "instrumentalFile": "step_001_inst.mp3",
         "lyricsFile": "step_001_lyrics.json",
         "unlocked": true
       }
     ]
   }
   ```

2. **`{step_id}_singalong.json`**: 타이밍 동기화된 가사
   ```json
   [
     {
       "sentence": "I wake up when the sunlight hits my face.",
       "start": 11.32,
       "end": 15.604,
       "words": [
         {
           "word": "I",
           "start": 11.32,
           "end": 11.7
         },
         ...
       ]
     }
   ]
   ```

3. **`{step_id}_role.json`**: 단어별 품사/역할 및 색상
   ```json
   [
     {
       "word": "I",
       "role": "subject",
       "buttonBg": "neon_boder_blue"
     },
     {
       "word": "wake",
       "role": "verb",
       "buttonBg": "neon_boder_red"
     }
   ]
   ```

4. **`{step_id}_learn.json`**: 학습 콘텐츠
   ```json
   {
     "steps": [
       {
         "stepId": "step_001_01",
         "sentence": "Can you tell me the way?",
         "translation": "길을 알려주실 수 있나요?",
         "audioUrl": "Audio/asking_directions_01.mp3",
         "grammarNote": "'Can you ~?'는 정중한 요청 표현입니다.",
         "examples": [
           {
             "sentence": "Can you tell me how to get to the station?",
             "translation": "역에 어떻게 가는지 알려주실 수 있나요?"
           }
         ],
         "highlights": [
           {
             "text": "Can you",
             "color": "#4FC3F7"
           }
         ]
       }
     ]
   }
   ```

5. **`{step_id}_test.json`**: 게임/퀴즈 문제
   ```json
   {
     "version": "1.0",
     "course": {
       "id": "eng-beginner-01",
       "title": "Beginner Core",
       "locale": "ko-KR",
       "targetLocale": "en-US"
     },
     "items": [
       {
         "id": "A1-001",
         "type": "assemble",
         "prompt": {
           "sourceLang": "ko",
           "text": "나는 학생이야."
         },
         "wordBank": ["I", "am", "a", "student", "are", "the", "teacher"],
         "correctOrder": ["I", "am", "a", "student"],
         "meta": {
           "tags": ["be-verb", "present-simple"],
           "difficulty": 1,
           "timeLimitSec": 45
         }
       }
     ]
   }
   ```


## 🚀 사용 방법 (AI 실행 가이드)

### Step 1: 파일 준비 확인
```powershell
# 입력 폴더 확인
Test-Path "C:\Project\WhisperX_Input\{chapter_id}\{step_id}.mp3"
# 참조 가사가 있는지 확인
Test-Path "C:\Project\WhisperX_Input\{chapter_id}\{step_id}_lyrics.txt"
```

### Step 2: 사용자 요청 파싱

사용자가 다음 형식으로 요청할 것입니다:

```
다음 노래를 처리해줘:

**파일 정보:**
- 챕터 ID: pvb_chap_001
- 스텝 ID: step_003
- 노래 제목: Going Places
- 핵심 패턴: stand up, step back, come back
- 주제/테마: Movement and Transition
- 난이도: 1 (1-5)
- 입력 폴더: C:\Project\WhisperX_Input\pvb_chap_001
```

**AI는 다음 정보를 추출:**
- `chapter_id`: "pvb_chap_001"
- `step_id`: "step_003"
- `title`: "Going Places"
- `patterns`: ["stand up", "step back", "come back"]
- `theme`: "Movement and Transition"
- `difficulty`: 1
- `input_folder`: "C:\Project\WhisperX_Input\pvb_chap_001"

### Step 3: AI 자동 처리 단계

AI가 다음 작업을 **순서대로** 진행합니다:

// turbo-all

#### 3.1. WhisperX 실행

**중요: 반드시 가상환경의 Python을 사용해야 합니다!**

WhisperX는 `C:\Project\WhisperX\venv`에 설치되어 있으므로, 가상환경의 Python을 직접 지정해야 합니다.

**명령어:**
```powershell
# 가상환경의 Python 사용 (필수)
C:\Project\WhisperX\venv\Scripts\python.exe -m whisperx "C:\Project\WhisperX_Input\{chapter_id}\{step_id}.mp3" `
  --model base `
  --output_dir "C:\Project\WhisperX_Output\{chapter_id}" `
  --compute_type float16 `
  --language en `
  --output_format json
```

**주의:**
- ❌ `cd` 후 `python` 실행 → 시스템 Python 사용 (모듈 없음)
- ✅ 가상환경 전체 경로 사용 → 올바른 Python 환경

**대안 (activate 후 실행):**
```powershell
& C:\Project\WhisperX\venv\Scripts\Activate.ps1
python -m whisperx "C:\Project\WhisperX_Input\{chapter_id}\{step_id}.mp3" `
  --model base `
  --output_dir "C:\Project\WhisperX_Output\{chapter_id}" `
  --compute_type float16 `
  --language en `
  --output_format json
```


**출력:** `C:\Project\WhisperX_Output\{chapter_id}\{step_id}.json`

**WhisperX 출력 형식:**
```json
{
  "segments": [
    {
      "start": 11.32,
      "end": 15.604,
      "text": "I wake up when the sunlight hits my face.",
      "words": [
        {"word": "I", "start": 11.32, "end": 11.7},
        {"word": "wake", "start": 11.72, "end": 12.121},
        {"word": "up", "start": 12.241, "end": 12.341}
      ]
    }
  ]
}
```

#### 3.2. 가사 후처리 (문장 단위 그룹핑)

**입력:** WhisperX JSON
**처리:** 
- segments를 문장 단위로 그룹핑
- 참조 가사가 있으면 WhisperX 텍스트와 비교/보정
- 각 문장의 start/end 타임스탬프 유지
- **중요: 노래 구조 태그 제거**

**노래 구조 태그 처리:**
참조 가사 파일에 다음과 같은 구조 태그가 있을 수 있습니다:
- `Verse 1:`, `Verse 2:`, `Verse 3:`, etc.
- `Chorus:`, `Pre-Chorus:`
- `Bridge:`
- `Intro:`, `Outro:`
- `Refrain:`, `Hook:`

**이런 태그들은 모두 무시하고 순수 가사 문장만 추출하세요.**

**예시:**
```
입력 (참조 가사):
Verse 1:
I got up early, five AM
I couldn't sleep, this was my chance

Chorus:
I got up, I got out, I'm on my way
I got on that bus to face the day

출력 (처리된 가사):
I got up early, five AM
I couldn't sleep, this was my chance
I got up, I got out, I'm on my way
I got on that bus to face the day
```

**출력 형식 (singalong.json):**
```json
[
  {
    "sentence": "I wake up when the sunlight hits my face.",
    "start": 11.32,
    "end": 15.604,
    "words": [
      {"word": "I", "start": 11.32, "end": 11.7},
      {"word": "wake", "start": 11.72, "end": 12.121},
      {"word": "up", "start": 12.241, "end": 12.341},
      {"word": "when", "start": 13.002, "end": 13.262},
      {"word": "the", "start": 13.362, "end": 13.442},
      {"word": "sunlight", "start": 13.462, "end": 14.383},
      {"word": "hits", "start": 14.483, "end": 14.623},
      {"word": "my", "start": 14.743, "end": 15.124},
      {"word": "face", "start": 15.244, "end": 15.604}
    ]
  }
]
```

#### 3.3. AI 품사/역할 분석 (role.json 생성)

**AI 프롬프트:**
```
다음 가사의 각 **고유 단어**를 품사와 역할로 분석하여 JSON 배열을 생성해주세요.

가사:
{전체 가사 텍스트}

각 단어에 대해:
- word: 단어 (원본 그대로, 대소문자 구분)
- role: 다음 중 하나
  * subject - 주어 (I, you, he, she, it, they, 주어로 쓰이는 명사)
  * verb - 동사 (wake, get, make, turn, is, are 등)
  * object - 목적어 (동사의 대상이 되는 명사)
  * modifier - 수식어 (형용사, 부사, 관사, 소유격, 전치사)
  * conjunction - 접속사 (and, when, or, but)
  * complement - 보어 (주어/목적어를 보충 설명)
- buttonBg: 역할별 색상 매핑
  * subject → "neon_boder_blue"
  * verb → "neon_boder_red"
  * object → "neon_boder_green"
  * modifier → "neon_boder_orange"
  * conjunction → "neon_boder_white"
  * complement → "neon_boder_violet"

중요:
- 가사에 나온 모든 고유 단어를 포함
- 같은 단어가 여러 역할로 쓰일 경우, 가장 빈번한 역할 선택
- JSON 배열만 출력, 설명 없이

출력 형식:
[
  {"word": "I", "role": "subject", "buttonBg": "neon_boder_blue"},
  {"word": "wake", "role": "verb", "buttonBg": "neon_boder_red"},
  {"word": "up", "role": "modifier", "buttonBg": "neon_boder_orange"}
]
```

**출력:** `{step_id}_role.json`

#### 3.4. AI 학습 콘텐츠 생성 (learn.json)

**AI 프롬프트:**
```
다음 노래를 기반으로 영어 학습 콘텐츠를 생성해주세요.

**노래 정보:**
- 제목: {title}
- 주제: {theme}
- 핵심 패턴: {patterns}
- 난이도: {difficulty}/5
- 대상: 한국 영어 초급 학습자

**가사:**
{전체 가사}

**요구사항:**
1. 핵심 패턴을 중심으로 5-10개의 학습 항목 생성
2. 각 항목은 실생활에서 바로 쓸 수 있는 표현 위주
3. 번역은 자연스러운 한국어로
4. 문법 설명은 간단명료하게
5. 예문은 초급자가 이해하기 쉽게

**JSON 형식:**
{
  "steps": [
    {
      "stepId": "{step_id}_01",
      "sentence": "영어 예문",
      "translation": "한국어 번역",
      "audioUrl": "Audio/{step_id}_01.mp3",
      "grammarNote": "문법 설명 또는 사용 팁 (한국어)",
      "examples": [
        {
          "sentence": "추가 예문 1",
          "translation": "번역"
        },
        {
          "sentence": "추가 예문 2",
          "translation": "번역"
        }
      ],
      "highlights": [
        {
          "text": "강조할 단어/구문",
          "color": "#4FC3F7"
        }
      ]
    }
  ]
}

JSON만 출력하세요.
```

**출력:** `{step_id}_learn.json`

#### 3.5. AI 퀴즈 문제 생성 (test.json)

**AI 프롬프트:**
```
다음 표현들을 학습하기 위한 퀴즈 15문제를 생성해주세요.

**학습 목표:**
- 핵심 패턴: {patterns}
- 난이도: {difficulty}/5
- 가사: {전체 가사}

**문제 구성:**
1. assemble (단어 배열): 6문제
2. assemble_listen (듣고 배열): 3문제
3. speak1, speak2 (따라 말하기): 3문제
4. typing (타이핑): 3문제

**JSON 형식:**
{
  "version": "1.0",
  "course": {
    "id": "eng-beginner-01",
    "title": "English Learning",
    "locale": "ko-KR",
    "targetLocale": "en-US"
  },
  "items": [
    {
      "id": "Q001",
      "type": "assemble",
      "prompt": {
        "sourceLang": "ko",
        "text": "한국어 문제"
      },
      "wordBank": ["단어", "목록", "뒤섞여"],
      "correctOrder": ["정답", "순서"],
      "acceptedAlternatives": [
        ["대안", "답안"]
      ],
      "ui": {
        "hint": "힌트 (한국어)"
      },
      "meta": {
        "tags": ["태그1", "태그2"],
        "difficulty": 1,
        "timeLimitSec": 45
      }
    },
    {
      "id": "Q002",
      "type": "assemble_listen",
      "prompt": {
        "sourceLang": "audio",
        "text": "오디오를 듣고 문장을 완성하세요."
      },
      "media": {
        "audioRef": "{step_id}_q002",
        "transcript": "정답 문장"
      },
      "wordBank": ["단어", "목록"],
      "correctOrder": ["정답", "순서"],
      "acceptedAlternatives": [],
      "ui": {
        "hint": "힌트"
      },
      "meta": {
        "tags": ["listening"],
        "difficulty": 2,
        "timeLimitSec": 60
      }
    },
    {
      "id": "Q003",
      "type": "speak1",
      "prompt": {
        "sourceLang": "ko",
        "text": "한국어 문제"
      },
      "correctOrder": ["정답", "문장"],
      "acceptedAlternatives": [
        ["대안", "답안"]
      ],
      "media": {
        "audioRef": "{step_id}_q003",
        "transcript": "정답 문장"
      },      
      "evaluation": {
        "mode": "speech",
        "minConfidence": 0.65,
        "pronunciationFocus": ["중요", "단어"]
      },
      "ui": {
        "hint": "힌트",
        "showMicIcon": true
      },
      "meta": {
        "tags": ["speaking"],
        "difficulty": 2,
        "timeLimitSec": 40
      }
    },
    {
      "id": "Q004",
      "type": "typing",
      "prompt": {
        "sourceLang": "ko",
        "text": "한국어 문제"
      },
      "answers": {
        "canonical": "정답 문장",
        "acceptedAlternatives": [
          "대안 답안 1",
          "대안 답안 2"
        ]
      },
      "correctOrder": ["정답", "단어", "순서"],
      "acceptedAlternatives": [],
      "evaluation": {
        "mode": "typed",
        "spellingTolerance": 0.95
      },
      "ui": {
        "hint": "힌트"
      },
      "meta": {
        "tags": ["typing"],
        "difficulty": 2,
        "timeLimitSec": 60
      }
    }
  ]
}

15문제 전체를 JSON으로 출력하세요.
```

**출력:** `{step_id}_test.json`

#### 3.6. lyrics.json 생성 및 모든 JSON 파일 저장

**중요: lyrics.json은 singalong과 다른 형식입니다!**

`{step_id}_lyrics.json`은 **평탄한 단어 배열**로 생성해야 합니다 (singalong처럼 sentence 구조가 아님):

**형식:**
```json
[
  {
    "word": "단어",
    "start": 시작시간,
    "end": 끝시간
  },
  ...
]
```

**생성 방법:**
- singalong.json의 모든 sentences에서 words 배열을 추출
- 각 단어의 word, start, end만 포함한 평탄한 배열로 변환
- 단어 내부에 쌍따옴표(")가 있으면 반드시 이스케이프(\")

**PowerShell 예시:**
```powershell
$singalong = Get-Content "{step_id}_singalong.json" -Raw | ConvertFrom-Json
$allWords = @()
foreach ($sentence in $singalong) {
  foreach ($word in $sentence.words) {
    $wordText = $word.word -replace '"', '\"'
    $allWords += "  {`n    `"word`": `"$wordText`",`n    `"start`": $($word.start),`n    `"end`": $($word.end)`n  }"
  }
}
$output = "[`n" + ($allWords -join ",`n") + "`n]`n"
[System.IO.File]::WriteAllText("{step_id}_lyrics.json", $output, [System.Text.UTF8Encoding]::new($false))
```

**모든 JSON 파일 저장 위치:**

```
C:\Users\User\Rhythm English_urp\Assets\Resources\json\{chapter_id}\
├── {step_id}_singalong.json  (sentence 구조)
├── {step_id}_role.json
├── {step_id}_learn.json
├── {step_id}_test.json
└── {step_id}_lyrics.json     (평탄한 word 배열 - 중요!)
```

각 파일은 UTF-8 인코딩 (BOM 없음), 들여쓰기 2칸, 줄바꿈 CRLF (`\r\n`)로 저장.

#### 3.7. JSON 유효성 검증 (필수)

**모든 생성된 JSON 파일의 유효성을 반드시 검증해야 합니다.**

```powershell
# 각 파일 검증
$files = @('singalong', 'role', 'learn', 'test', 'lyrics')
$errors = @()

foreach ($file in $files) {
  $path = "C:\Users\User\Rhythm English_urp\Assets\Resources\json\{chapter_id}\{step_id}_$file.json"
  try {
    $json = Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json
    Write-Host "✅ {step_id}_$file.json - Valid"
  } catch {
    Write-Host "❌ {step_id}_$file.json - INVALID: $_"
    $errors += "$file.json"
  }
}

if ($errors.Count -gt 0) {
  Write-Host "`n⚠️ 다음 파일에 오류가 있습니다: $($errors -join ', ')"
  Write-Host "파일을 수정한 후 다시 검증하세요."
  exit 1
} else {
  Write-Host "`n✅ 모든 JSON 파일이 유효합니다!"
}
```

**검증 항목:**
1. JSON 구문 오류 없음
2. 필수 필드 존재
3. 쌍따옴표 올바르게 이스케이프됨
4. UTF-8 인코딩 확인
5. 파일 크기가 0이 아님

#### 3.8. 사용자에게 결과 보고

생성 완료 후 다음 정보를 사용자에게 보고:

```
✅ {title} 처리 완료!

생성된 파일:
- {step_id}_singalong.json ({문장 개수}개 문장, {단어 개수}개 단어) ✅
- {step_id}_role.json ({고유 단어 개수}개 단어 분석) ✅
- {step_id}_learn.json ({학습 항목 개수}개 학습 항목) ✅
- {step_id}_test.json ({문제 개수}개 퀴즈 문제) ✅
- {step_id}_lyrics.json ({총 단어 개수}개 단어, 평탄한 배열) ✅

✅ 모든 JSON 파일 유효성 검증 완료

저장 위치: Assets\Resources\json\{chapter_id}\

다음 단계:
1. MP3 파일을 Assets\Resources\mp3\{chapter_id}\로 복사
2. Unity에서 노래 재생 테스트
3. 필요시 JSON 수정 요청
```

## 📝 요청 템플릿

### 기본 템플릿
```
노래 처리 요청:
- 챕터: {chapter_id}
- 스텝: {step_id}
- 제목: {title}
- 패턴: {patterns}
- 테마: {theme}
- 난이도: {difficulty}
```

### 전체 템플릿 (모든 옵션 포함)
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

**WhisperX 설정:**
- 모델: base
- 언어: en

**AI 생성 옵션:**
- 학습 항목 개수: 8개
- 퀴즈 문제 개수: 15개
- 퀴즈 유형: assemble(6), listen(3), speak(3), typing(3)

**출력 위치:**
- Unity: Assets\Resources\json\pvb_chap_002\

참조 가사가 step_001_lyrics.txt에 있으면 검증에 사용해줘.
```

## 🔍 세부 작업 설명

### 1. WhisperX 실행
```bash
python -m whisperx "C:\Project\WhisperX_Input\{chapter}\{step}.mp3" \
  --model base \
  --output_dir "C:\Project\WhisperX_Output\{chapter}" \
  --compute_type float16 \
  --language en
```

### 2. AI 분석 프롬프트

#### 품사/역할 분석
```
다음 가사의 각 단어에 대해 품사와 역할을 분석해주세요.

가사:
{전체 가사}

각 단어에 대해:
- word: 단어
- role: subject/verb/object/modifier/conjunction/complement
- buttonBg: 역할별 색상
  * subject → neon_boder_blue
  * verb → neon_boder_red
  * object → neon_boder_green
  * modifier → neon_boder_orange
  * conjunction → neon_boder_white
  * complement → neon_boder_violet

JSON 배열로 출력.
```

#### 학습 콘텐츠 생성
```
다음 노래의 핵심 표현들에 대한 학습 콘텐츠를 만들어주세요.

제목: {title}
테마: {theme}
패턴: {patterns}
난이도: {difficulty}/5

가사:
{전체 가사}

5-10개의 학습 항목을 만들어주세요.
각 항목: sentence, translation, grammarNote, examples, highlights

JSON 형식으로 출력.
```

#### 퀴즈 문제 생성
```
다음 표현들을 학습하기 위한 15개 퀴즈 문제를 만들어주세요.

패턴: {patterns}
난이도: {difficulty}/5

가사:
{전체 가사}

문제 유형:
- assemble: 6문제
- assemble_listen: 3문제
- speak1, speak2: 3문제
- typing: 3문제

JSON 형식으로 출력.
```

## 📂 출력 파일 위치

생성된 파일들은 자동으로 다음 위치에 저장됩니다:

```
Unity 프로젝트:
C:\Users\User\Rhythm English_urp\Assets\Resources\json\{chapter_id}\
├── {step_id}_singalong.json
├── {step_id}_role.json
├── {step_id}_learn.json
├── {step_id}_test.json
└── {step_id}_lyrics.json

MP3 파일 (수동 복사 필요):
C:\Users\User\Rhythm English_urp\Assets\Resources\mp3\{chapter_id}\
├── {step_id}.mp3
├── {step_id}_vocal.mp3
└── {step_id}_inst.mp3
```

## 🎯 체크리스트

### 파일 준비
- [ ] `C:\Project\WhisperX_Input\{chapter_id}\` 폴더 생성
- [ ] `{step_id}.mp3` 파일 복사
- [ ] (선택) `{step_id}_vocal.mp3` 파일 복사
- [ ] (선택) `{step_id}_inst.mp3` 파일 복사
- [ ] (선택) `{step_id}_lyrics.txt` 작성

### AI 요청
- [ ] 요청 템플릿 준비 (챕터, 스텝, 제목, 패턴, 테마, 난이도)
- [ ] AI에게 처리 요청
- [ ] 진행 상황 확인

### 결과 확인
- [ ] `{step_id}_singalong.json` 생성 확인
- [ ] `{step_id}_role.json` 생성 확인
- [ ] `{step_id}_learn.json` 생성 확인
- [ ] `{step_id}_test.json` 생성 확인
- [ ] Unity 프로젝트에 파일 복사 확인
- [ ] MP3 파일 수동 복사 (Resources/mp3/)

### 검증
- [ ] Unity에서 노래 재생 테스트
- [ ] 가사 타이밍 확인
- [ ] 학습 콘텐츠 검토
- [ ] 퀴즈 문제 테스트

## 💡 팁

1. **참조 가사 제공**: `{step_id}_lyrics.txt` 파일을 제공하면 WhisperX 결과를 검증하고 보정할 수 있어 정확도가 높아집니다.

2. **핵심 패턴 명확히**: 핵심 패턴을 명확히 지정하면 더 집중된 학습 콘텐츠가 생성됩니다.

3. **난이도 설정**: 난이도를 적절히 설정하면 학습자 레벨에 맞는 예문과 퀴즈가 생성됩니다.

4. **검토 및 수정**: AI가 생성한 콘텐츠는 검토 후 필요시 수정 요청을 하세요.

## 🔧 문제 해결

### WhisperX 실행 오류
- WhisperX 경로 확인: `C:\Project\WhisperX\whisperx`
- Python 환경 활성화 확인
- MP3 파일 경로 확인

### JSON 생성 오류
- 가사 파일 인코딩 확인 (UTF-8)
- JSON 형식 검증
- 필수 필드 누락 확인

### Unity 복사 오류
- Unity 프로젝트 경로 확인
- 폴더 권한 확인
- 기존 파일 백업

## 📊 예상 소요 시간

- WhisperX 실행: 1-3분 (노래 길이에 따라)
- AI 분석 및 생성: 2-5분
- **총 소요 시간: 5-10분/곡**

## 🎉 완료!

이제 다음과 같이 간단하게 사용할 수 있습니다:

1. MP3와 가사 파일을 폴더에 넣기
2. AI에게 "이 노래 처리해줘" 요청
3. 5-10분 대기
4. 생성된 JSON 확인 및 Unity에서 테스트

별도의 코딩, 설정, API 키가 필요 없습니다! 🚀

