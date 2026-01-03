# AI 에이전트 실행 가이드

이 문서는 다른 AI 세션/컨텍스트에서 이 프로젝트를 인계받았을 때 참고하는 가이드입니다.

## 프로젝트 개요

**Rhythm English** - AI 생성 음악으로 영어를 배우는 Unity 기반 모바일 앱

사용자는 노래를 들으며 영어 표현을 학습하고, 리듬 게임으로 연습합니다.

## 주요 워크플로우

### 1. WhisperX 자동화 (`/whisper-automation`)

**목적:** 노래 MP3에서 앱용 JSON 파일 자동 생성

**사용자 요청 형식:**
```
다음 노래를 처리해줘:
- 챕터 ID: pvb_chap_001
- 스텝 ID: step_003
- 노래 제목: Going Places
- 핵심 패턴: stand up, step back, come back
- 주제/테마: Movement
- 난이도: 1
```

**AI 처리 단계:**
1. WhisperX로 가사 추출 (타임스탬프 포함)
2. 가사 후처리 (문장 단위 그룹핑)
3. AI로 품사/역할 분석
4. AI로 학습 콘텐츠 생성 (5-10개)
5. AI로 퀴즈 문제 생성 (15개)
6. lyrics.json 생성 (평탄한 단어 배열)
7. **JSON 유효성 검증 (필수)**
8. JSON 파일 5개 Unity 프로젝트에 저장

**출력 파일:**
- `{step_id}_singalong.json` - 타임스탬프가 있는 가사 (sentence 구조)
- `{step_id}_role.json` - 단어별 품사/색상
- `{step_id}_learn.json` - 학습 콘텐츠
- `{step_id}_test.json` - 퀴즈 문제
- `{step_id}_lyrics.json` - **평탄한 word 배열** (singalong과 다른 형식!)

**입력 폴더:** `C:\Project\WhisperX_Input\{chapter_id}\`
**출력 폴더:** `C:\Users\User\Rhythm English_urp\Assets\Resources\json\{chapter_id}\`

**중요:**
- **WhisperX는 가상환경에서 실행**: `C:\Project\WhisperX\venv\Scripts\python.exe` 사용
- 모든 명령어는 `// turbo-all` 모드로 자동 실행
- JSON은 UTF-8, 들여쓰기 2칸, CRLF 줄바꿈
- **lyrics.json은 반드시 평탄한 배열로 생성** (singalong을 단순 복사X)
- **모든 JSON 파일 유효성 검증 필수**


### 2. 프로필 디자인 (`/profile-design`)

사용자 프로필 화면 디자인 문서 - 참고용

## 프로젝트 구조

```
C:\Users\User\Rhythm English_urp\
├── Assets\
│   ├── Resources\
│   │   ├── json\              # 노래별 JSON 데이터
│   │   │   ├── pvb_chap_001\
│   │   │   ├── pvb_chap_002\
│   │   │   └── beg_chap_001\
│   │   ├── mp3\               # 노래 MP3 파일
│   │   └── steps\             # 챕터 메타데이터
│   └── Scripts\               # Unity C# 스크립트
├── .agent\
│   ├── workflows\             # 워크플로우 문서들
│   │   ├── whisper-automation.md
│   │   ├── quick-start-whisper.md
│   │   └── profile-design.md
│   └── design\                # 디자인 문서
└── (Unity 프로젝트 파일들)

C:\Project\
├── WhisperX\
│   └── whisperx\              # WhisperX Python 패키지
├── WhisperX_Input\            # 입력 MP3 파일들
│   ├── pvb_chap_001\
│   └── pvb_chap_002\
└── WhisperX_Output\           # WhisperX 출력 (임시)
```

## JSON 파일 형식 요약

### singalong.json
```json
[
  {
    "sentence": "문장 텍스트",
    "start": 11.32,
    "end": 15.604,
    "words": [
      {"word": "단어", "start": 11.32, "end": 11.7}
    ]
  }
]
```

### role.json
```json
[
  {
    "word": "단어",
    "role": "subject|verb|object|modifier|conjunction|complement",
    "buttonBg": "neon_boder_blue|red|green|orange|white|violet"
  }
]
```

### learn.json
```json
{
  "steps": [
    {
      "stepId": "step_001_01",
      "sentence": "영어 예문",
      "translation": "한국어 번역",
      "audioUrl": "Audio/file.mp3",
      "grammarNote": "문법 설명",
      "examples": [{"sentence": "예문", "translation": "번역"}],
      "highlights": [{"text": "강조", "color": "#4FC3F7"}]
    }
  ]
}
```

### test.json
```json
{
  "version": "1.0",
  "course": {...},
  "items": [
    {
      "id": "Q001",
      "type": "assemble|assemble_listen|speak1|speak2|typing",
      "prompt": {"sourceLang": "ko", "text": "문제"},
      "wordBank": ["단어", "목록"],
      "correctOrder": ["정답", "순서"],
      "meta": {"difficulty": 1, "timeLimitSec": 45}
    }
  ]
}
```

## 자주 사용하는 명령어

### WhisperX 실행

**중요: 가상환경의 Python을 사용해야 합니다!**

```powershell
# 가상환경 Python 직접 사용 (권장)
C:\Project\WhisperX\venv\Scripts\python.exe -m whisperx "C:\Project\WhisperX_Input\{chapter}\{step}.mp3" `
  --model base `
  --output_dir "C:\Project\WhisperX_Output\{chapter}" `
  --compute_type float16 `
  --language en `
  --output_format json

# 또는 가상환경 활성화 후 실행
& C:\Project\WhisperX\venv\Scripts\Activate.ps1
python -m whisperx "C:\Project\WhisperX_Input\{chapter}\{step}.mp3" `
  --model base `
  --output_dir "C:\Project\WhisperX_Output\{chapter}" `
  --compute_type float16 `
  --language en `
  --output_format json
```


### JSON 파일 생성
```powershell
# 특정 챕터 폴더 생성
New-Item -ItemType Directory -Path "C:\Users\User\Rhythm English_urp\Assets\Resources\json\{chapter_id}" -Force

# JSON 저장
$json | ConvertTo-Json -Depth 10 | Out-File -FilePath "{파일명}" -Encoding UTF8
```

## 역할별 색상 매핑

- `subject` (주어) → `neon_boder_blue`
- `verb` (동사) → `neon_boder_red`
- `object` (목적어) → `neon_boder_green`
- `modifier` (수식어) → `neon_boder_orange`
- `conjunction` (접속사) → `neon_boder_white`
- `complement` (보어) → `neon_boder_violet`

## 주의사항

1. **WhisperX 경로**: 반드시 `C:\Project\WhisperX\whisperx`에서 실행
2. **인코딩**: 모든 JSON은 UTF-8
3. **들여쓰기**: 2칸
4. **참조 가사**: 있으면 반드시 검증에 사용
5. **타임스탬프**: 소수점 3자리까지
6. **파일명**: `{step_id}_*.json` 형식 준수

## 트러블슈팅

### WhisperX 실행 실패
- Python 환경 확인
- MP3 파일 경로 확인
- 출력 디렉토리 권한 확인

### JSON 저장 실패
- Unity 프로젝트 경로 확인
- 폴더 존재 여부 확인
- UTF-8 인코딩 확인

## 다음 세션에서 할 일

사용자가 다음과 같이 요청하면:
```
다음 노래를 처리해줘:
- 챕터 ID: ...
- 스텝 ID: ...
...
```

1. `/whisper-automation` 워크플로우 확인
2. Step 3의 3.1~3.7 단계를 순서대로 실행
3. 모든 단계는 `// turbo-all` 모드로 자동 실행
4. 완료 후 결과 보고

**중요:** 워크플로우 문서에 모든 AI 프롬프트와 JSON 스키마가 상세히 기술되어 있으므로, 반드시 참고할 것.
