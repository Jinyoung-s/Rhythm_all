---
description: Profile Screen Design Document
---

# 프로필 화면 설계 문서

## 📋 개요
Rhythm English 앱의 프로필(Profile) 화면 설계 문서입니다. 사용자의 학습 진행 상황, 통계, 설정 등을 확인하고 관리할 수 있는 통합 화면입니다.

---

## 🎯 핵심 기능 영역

### 1. 사용자 정보 섹션 (User Info Section)
**위치**: 화면 상단

**포함 항목**:
- **프로필 이미지**: 
  - 기본: 이니셜 또는 기본 아바타 아이콘
  - 추후: 사용자 지정 이미지 업로드
- **사용자 이름**: 
  - 표시: DisplayName (예: "Jinyoung")
  - 편집 버튼 (연필 아이콘)
- **회원 상태**:
  - 게스트 / 로그인 상태 표시
  - 로그인 제공자 (Google, Apple, Email)
  - 구독 상태 배지 (Premium/Free)
- **가입일**: "Joined Dec 2024"

**액션**:
- 프로필 편집 버튼
- 로그인/로그아웃 버튼

---

### 2. 학습 통계 대시보드 (Learning Stats Dashboard)
**위치**: 사용자 정보 아래

**포함 항목**:

#### 2.1 포인트 현황
- **현재 보유 Notes (코인)**:
  - 큰 숫자로 표시
  - 코인 아이콘 (♪ 또는 음표 아이콘)
- **총 획득 Notes**: 누적 획득량
- **총 사용 Notes**: 누적 사용량

#### 2.2 학습 진행 통계
- **완료한 Step 수**: 
  - "42 Steps Completed"
  - 전체 코스/챕터 기준
- **학습 중인 코스**:
  - 현재 진행 중인 코스 이름
  - 진행률 바 (%)
- **총 학습 시간**: 
  - 추후 구현 (앱 사용 시간 트래킹)
- **연속 학습 일수 (Streak)**:
  - 추후 구현
  - "🔥 7 Days Streak!"

#### 2.3 성적 통계
- **평균 정확도**: 
  - 모든 테스트의 평균 정확도 (%)
- **최고 점수**: 
  - Game1에서 최고 점수
- **완성한 노래 수**:
  - TestCompleted == true인 Step 수

---

### 3. 학습 기록 (Learning History)
**위치**: 통계 대시보드 아래

**포함 항목**:
- **포인트 히스토리 (Notes History)**:
  - 최근 획득/사용 내역 (최대 20개)
  - 항목별 표시:
    - 시간 (예: "2 hours ago", "Yesterday")
    - 금액 (+50, -100)
    - 출처/설명 ("Game1 - Perfect Clear", "Purchase - Shape of You")
    - 아이콘 (게임 타입별 다른 아이콘)
  - "전체 보기" 버튼

- **최근 완료한 Steps**:
  - 최근 5개의 완료된 Step 표시
  - 각 항목:
    - Step 제목
    - 완료 날짜
    - 획득 점수

---

### 4. 저장된 콘텐츠 (Saved Content)
**위치**: 학습 기록 아래

**포함 항목**:
- **저장한 문장 (Saved Sentences)**:
  - 개수 배지: "25 Saved"
  - 탭 버튼 → SavedSentencesView로 이동
  
- **복습 필요 문장 (Wrong Sentences)**:
  - 개수 배지: "12 to Review"
  - 탭 버튼 → WrongSentencesView로 이동

- **Favorites / Playlists**:
  - 좋아요한 노래 목록
  - 커스텀 플레이리스트 (추후 구현)

---

### 5. 성취 & 배지 (Achievements & Badges)
**위치**: 저장된 콘텐츠 아래
**우선순위**: 낮음 (추후 구현)

**포함 항목**:
- **획득한 배지**:
  - "First Step Complete"
  - "10 Steps Master"
  - "Perfect Score x5"
  - "1000 Notes Earned"
  - "7 Days Streak"
- 그리드 레이아웃으로 배지 아이콘 표시
- 잠금/잠금해제 상태 구분

---

### 6. 설정 (Settings)
**위치**: 화면 하단 (또는 별도 섹션)

**포함 항목**:

#### 6.1 앱 설정
- **언어 설정**: 한국어/English
- **알림 설정**: 
  - 푸시 알림 On/Off
  - 학습 리마인더 설정
- **사운드 설정**:
  - 효과음 볼륨
  - 음악 볼륨
  - TTS 볼륨

#### 6.2 학습 설정
- **난이도 설정**: Easy/Normal/Hard
- **자막 표시**: On/Off
- **자동 재생**: On/Off

#### 6.3 계정 관리
- **이메일 변경**
- **비밀번호 변경** (Email 로그인 시)
- **계정 연동**: Google/Apple 계정 연결
- **로그아웃**
- **계정 삭제** (위험한 작업 - 확인 팝업)

#### 6.4 앱 정보
- **버전 정보**: "Version 1.0.0"
- **이용약관**
- **개인정보 처리방침**
- **오픈소스 라이선스**
- **문의하기**: 이메일 또는 지원 페이지

---

## 🎨 UI/UX 디자인 가이드

### 레이아웃 구조
```
┌─────────────────────────────────┐
│        Header (Profile)         │
├─────────────────────────────────┤
│  [프로필 이미지]                │
│  Username                       │
│  Member Status                  │
│  [Edit] [Logout]                │
├─────────────────────────────────┤
│  📊 Learning Stats              │
│  ┌───────────┬───────────┐      │
│  │  Notes    │  Steps    │      │
│  │   500     │    42     │      │
│  └───────────┴───────────┘      │
│  ┌───────────────────────┐      │
│  │ Progress: 65% ████░░  │      │
│  └───────────────────────┘      │
├─────────────────────────────────┤
│  📜 Notes History               │
│  ┌─────────────────────────┐    │
│  │ +50 Game1 - Perfect     │    │
│  │ 2 hours ago             │    │
│  ├─────────────────────────┤    │
│  │ -100 Purchase - Song    │    │
│  │ Yesterday               │    │
│  └─────────────────────────┘    │
│  [View All]                     │
├─────────────────────────────────┤
│  💾 Saved Content               │
│  [Saved Sentences (25)]         │
│  [Wrong Sentences (12)]         │
├─────────────────────────────────┤
│  🏆 Achievements                │
│  [Badge Grid]                   │
├─────────────────────────────────┤
│  ⚙️ Settings                    │
│  [Language]                     │
│  [Notifications]                │
│  [Sound]                        │
│  [Account]                      │
│  [About]                        │
└─────────────────────────────────┘
│     Bottom Navigation           │
└─────────────────────────────────┘
```

### 색상 & 스타일
- **Primary Color**: 앱의 메인 컬러 (음표 테마)
- **Accent Color**: 중요한 정보 강조 (포인트, 배지 등)
- **Card Style**: 
  - 각 섹션을 카드 형태로 구분
  - 그림자 효과로 깊이감 추가
  - 둥근 모서리 (border-radius: 12px)
- **Typography**:
  - 헤더: Bold, 24px
  - 서브헤더: Semi-Bold, 18px
  - 본문: Regular, 14px
  - 캡션: Light, 12px

### 인터랙션
- **탭 효과**: 모든 버튼에 탭 피드백 (scale animation)
- **전환 애니메이션**: 화면 전환 시 fade-in 효과
- **스크롤**: 부드러운 스크롤 (smooth scroll)
- **로딩 상태**: 데이터 로드 시 스켈레톤 UI

---

## 📱 우선순위별 구현 계획

### Phase 1 (MVP) - 즉시 구현
1. ✅ 사용자 정보 섹션 (기본)
   - 이름, 로그인 상태 표시
   - 로그아웃 버튼
2. ✅ 포인트 현황
   - 현재 보유/총 획득/총 사용
3. ✅ 학습 진행 통계 (기본)
   - 완료한 Step 수
   - 현재 코스 진행률
4. ✅ 포인트 히스토리
   - 최근 20개 내역 표시
   - "전체 보기" 버튼
5. ✅ 저장된 콘텐츠 바로가기
   - Saved Sentences
   - Wrong Sentences
6. ✅ 기본 설정
   - 로그아웃
   - 앱 정보/버전

### Phase 2 - 중기 구현
1. 🔲 상세 학습 통계
   - 평균 정확도
   - 최고 점수
   - 그래프 형태로 시각화
2. 🔲 최근 완료한 Steps
3. 🔲 프로필 편집 기능
   - 이름 변경
   - 프로필 이미지 업로드
4. 🔲 상세 설정
   - 알림 설정
   - 사운드 설정
   - 학습 설정
5. 🔲 계정 관리
   - 이메일/비밀번호 변경
   - 계정 연동

### Phase 3 - 장기 구현
1. 🔲 성취 & 배지 시스템
2. 🔲 학습 시간 트래킹
3. 🔲 연속 학습 일수 (Streak)
4. 🔲 소셜 기능
   - 친구 추가
   - 랭킹/리더보드
5. 🔲 커스텀 테마/다크모드

---

## 🔗 기존 시스템 연동

### 필요한 데이터 소스
1. **UserProfileManager**:
   - CurrentUser (UserId, DisplayName, Email, IsGuest, IsSubscribed)
   
2. **PointManager**:
   - GetAvailableNotes()
   - GetTotalEarnedNotes()
   - GetTotalSpentNotes()
   - GetHistory(maxCount)

3. **ProgressManager**:
   - CurrentCourseId, CurrentChapterId, CurrentStepId
   - Courses (Dictionary)
   - GetChapterProgressPercent()
   - IsStepCompleted()

4. **SentenceManager**:
   - GetSavedSentences()
   - GetWrongSentences()

5. **MusicPlayerManager**:
   - 좋아요한 노래 목록 (추후 구현)

---

## 📄 필요한 파일

### Scripts
- `ProfileViewController.cs`: 프로필 화면 메인 컨트롤러
- `ProfileStatsCalculator.cs`: 통계 계산 유틸리티
- `ProfileSettingsManager.cs`: 설정 관리 (PlayerPrefs)

### UI Files
- `ProfileView.uxml`: 프로필 화면 레이아웃
- `ProfileView.uss`: 스타일시트
- `PointHistoryItem.uxml`: 포인트 히스토리 아이템 템플릿
- `SettingsPanel.uxml`: 설정 패널 (선택사항)

### Assets
- 프로필 아이콘/이미지
- 배지 아이콘 (추후)
- 설정 아이콘

---

## 🎯 다음 단계

1. **Phase 1 기능 구현**:
   - ProfileView.uxml 생성
   - ProfileView.uss 생성
   - ProfileViewController.cs 구현
   
2. **데이터 연동 테스트**:
   - PointManager 데이터 표시
   - ProgressManager 통계 연동
   
3. **UI 폴리싱**:
   - 모바일 반응형 디자인
   - 애니메이션 추가
   - 다크모드 고려

---

## 📝 참고사항

- **모바일 최적화**: 모든 UI는 작은 화면에서도 잘 보이도록 설계
- **접근성**: 충분한 터치 영역 (최소 44x44px)
- **성능**: 대량 데이터(히스토리) 로드 시 가상 스크롤 고려
- **보안**: 민감한 정보(이메일, 비밀번호)는 안전하게 처리
- **다국어**: 모든 텍스트는 다국어 지원 가능하도록 준비
