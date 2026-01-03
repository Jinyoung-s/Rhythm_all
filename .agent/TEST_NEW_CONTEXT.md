# 🧪 테스트용 간단 명령어

새로운 대화를 시작할 때 아래 내용을 **그대로 복사해서 붙여넣으세요**:

---

## 방법 1: 가장 간단한 버전 (권장)

```
안녕! 나는 Rhythm English 프로젝트를 진행 중이야.

.agent/README.md와 .agent/workflows/whisper-automation.md를 읽고 
프로젝트를 이해했으면 "준비 완료!"라고 알려줘.

그 다음 내가 노래 정보를 주면 워크플로우대로 처리해줘.
```

그 다음:

```
노래 처리:
- pvb_chap_001 / step_003
- 제목: Going Places
- 패턴: stand up, step back, come back
- 테마: Movement
- 난이도: 1
```

---

## 방법 2: 더 명시적인 버전

```
Rhythm English 프로젝트 시작:

1. c:\Users\User\Rhythm English_urp\.agent\README.md 읽기
2. c:\Users\User\Rhythm English_urp\.agent\workflows\whisper-automation.md 읽기
3. 워크플로우 실행 준비

준비되면 알려줘.
```

---

## 방법 3: 한 번에 모두 (테스트용)

```
Rhythm English 프로젝트 - 노래 자동 처리 요청

프로젝트 위치: c:\Users\User\Rhythm English_urp
.agent/README.md와 .agent/workflows/whisper-automation.md 참고

이 노래를 처리해줘:
- pvb_chap_001 / step_003
- 제목: Going Places  
- 패턴: stand up, step back, come back
- 난이도: 1
- 입력: C:\Project\WhisperX_Input\pvb_chap_001\step_003.mp3

워크플로우의 Step 3.1~3.7 실행해서 JSON 5개 생성해줘.
```

---

## 🎯 기대되는 AI 응답

### 1단계 응답:
```
✅ 준비 완료!

Rhythm English 프로젝트를 이해했습니다.

프로젝트: AI 생성 음악으로 영어 학습하는 Unity 앱
주요 작업: 노래 MP3 → WhisperX → JSON 자동 생성

노래 정보를 알려주시면 처리하겠습니다.
```

### 2단계 응답 (노래 처리 중):
```
🎵 Going Places 처리 시작...

[1/7] WhisperX 실행 중...
[2/7] 가사 후처리 중...
[3/7] AI 품사 분석 중...
[4/7] 학습 콘텐츠 생성 중...
[5/7] 퀴즈 문제 생성 중...
[6/7] JSON 파일 저장 중...
[7/7] 완료!

✅ step_003_singalong.json (12개 문장)
✅ step_003_role.json (28개 단어)
✅ step_003_learn.json (8개 항목)
✅ step_003_test.json (15개 문제)

저장: Assets\Resources\json\pvb_chap_001\
```

---

## ✅ 테스트 체크리스트

- [ ] 새 대화 시작
- [ ] 방법 1, 2, 3 중 하나 복사 붙여넣기
- [ ] AI가 "준비 완료" 응답 확인
- [ ] 노래 정보 입력
- [ ] JSON 파일 5개 생성 확인
- [ ] Unity 프로젝트에서 파일 확인

---

## 💾 저장할 파일 경로

```
C:\Users\User\Rhythm English_urp\Assets\Resources\json\pvb_chap_001\
├── step_003_singalong.json
├── step_003_role.json
├── step_003_learn.json
├── step_003_test.json
└── step_003_lyrics.json
```

---

지금 바로 테스트해보세요! 🚀
