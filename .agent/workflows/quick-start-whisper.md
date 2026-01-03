# WhisperX 자동화 빠른 시작 가이드

## 🎯 5분 만에 시작하기

### 1️⃣ 파일 준비 (1분)

1. 챕터 폴더 생성:
   ```
   C:\Project\WhisperX_Input\pvb_chap_002\
   ```

2. 파일 넣기:
   - `step_001.mp3` (노래 파일)
   - `step_001_lyrics.txt` (가사 - 한 줄에 한 문장씩)

### 2️⃣ AI에게 요청 (30초)

다음 메시지를 복사해서 AI에게 보내세요:

```
다음 노래를 처리해줘:

**파일 정보:**
- 챕터 ID: pvb_chap_002
- 스텝 ID: step_001
- 노래 제목: [노래 제목을 여기에]
- 핵심 패턴: [주요 표현들, 쉼표로 구분]
- 주제/테마: [노래 주제]
- 난이도: 1
- 입력 폴더: C:\Project\WhisperX_Input\pvb_chap_002

**작업:**
1. WhisperX로 step_001.mp3에서 가사 추출
2. 각 단어의 품사/역할 분석
3. 학습 콘텐츠 생성 (8개 항목)
4. 퀴즈 문제 생성 (15개)
5. 모든 JSON 파일 생성 및 Unity 프로젝트에 저장

참조 가사가 있으면 검증에 사용해줘.
```

### 3️⃣ AI가 자동 처리 (5-10분)

AI가 다음을 자동으로 수행:
- ✅ WhisperX 실행
- ✅ 가사 정제
- ✅ AI 분석 (품사, 학습, 퀴즈)
- ✅ JSON 파일 5개 생성
- ✅ Unity 프로젝트에 복사

### 4️⃣ 결과 확인 (2분)

Unity 프로젝트에서:
```
Assets\Resources\json\pvb_chap_002\
├── step_001_singalong.json
├── step_001_role.json
├── step_001_learn.json
├── step_001_test.json
└── step_001_lyrics.json
```

MP3 파일은 수동으로 복사:
```
Assets\Resources\mp3\pvb_chap_002\
└── step_001.mp3
```

## ✨ 끝!

별도의 설치, 설정, API 키가 필요 없습니다. 
그냥 파일 넣고 AI에게 요청하면 됩니다!

---

더 자세한 내용은 `/whisper-automation` 워크플로우를 참고하세요.
