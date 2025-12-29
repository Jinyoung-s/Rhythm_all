# VocabularyTestScene Setup Guide

This guide explains how to set up the VocabularyTestScene in Unity using UI Toolkit.

## 📁 File Locations

All UI files are located in:
```
Assets/Resources/UI/VocabularyTest/
├── VocabularyTestUI.uxml
├── VocabularyTestStyle.uss
├── TTSSetupPopup.uxml
└── TTSSetupStyle.uss
```

## 🎬 Scene Setup Steps

### 1. Create New Scene

1. In Unity, go to **File → New Scene**
2. Choose **Empty** template
3. Save as `VocabularyTestScene` in `Assets/Scenes/`

### 2. Add UI Document

1. In Hierarchy, right-click → **UI Toolkit → UI Document**
2. Rename the GameObject to `VocabularyTestUI`
3. In the Inspector:
   - **Panel Settings**: Use your project's default Panel Settings
   - **Source Asset**: Drag `VocabularyTestUI.uxml` from `Resources/UI/VocabularyTest/`

### 3. Add Canvas Scaler (if needed)

If you need responsive scaling:
1. Select the `VocabularyTestUI` GameObject
2. Add Component → **UI Toolkit → Panel Settings**
3. Configure scale mode as needed

### 4. Add VocabularyTestManager Script

1. Create an empty GameObject named `VocabularyTestManager`
2. Add the `VocabularyTestManager.cs` component (will be created in Phase 4)
3. This will handle all test logic and UI interactions

### 5. Verify TTS Setup

The TTS system is already set up via:
- `VocabularyTTSManager` (singleton, auto-created)
- `TTSSetupManager` (shows popup on first run)
- `SmartVoiceSelector` (auto-selects best voice)
- `GoogleTTSChecker` (Android TTS detection)

No additional setup needed for TTS!

## 🎨 UI Structure Overview

### Main Test UI (`VocabularyTestUI.uxml`)

The main UI contains:
- **Header**: Back button, progress bar (1/10), settings button
- **Type Badge**: Shows current question type (듣고 고르기, 뜻→단어, etc.)
- **Question Area**: Three modes that swap visibility:
  - `ListenMode`: Speaker button + instruction
  - `ReadingMode`: Question text + instruction
  - `TypingMode`: Small speaker + text input
- **Options**: 4 option buttons (for multiple choice)
- **Feedback Panel**: Shows after answer with word details
- **Actions**: Submit button / Next button

### TTS Setup Popup (`TTSSetupPopup.uxml`)

Optional popup shown on first launch:
- Icon and title
- Feature list (3 benefits)
- Info box (skippable notice)
- Action buttons: "나중에" / "설치 안내 보기"

## 🔗 UI Element Names (for C# code)

Use these names in `VocabularyTestManager.cs` to query elements:

```csharp
// Header
Button backButton = root.Q<Button>("BackButton");
Button settingsButton = root.Q<Button>("SettingsButton");
Label progressLabel = root.Q<Label>("ProgressLabel");
VisualElement progressBar = root.Q<VisualElement>("ProgressBar");

// Badge
Label typeBadge = root.Q<Label>("TypeBadge");

// Question Modes
VisualElement listenMode = root.Q<VisualElement>("ListenMode");
VisualElement readingMode = root.Q<VisualElement>("ReadingMode");
VisualElement typingMode = root.Q<VisualElement>("TypingMode");

Button speakerButton = root.Q<Button>("SpeakerButton");
Label questionText = root.Q<Label>("QuestionText");
Label instruction = root.Q<Label>("Instruction");
TextField typingInput = root.Q<TextField>("TypingInput");

// Options
VisualElement optionsContainer = root.Q<VisualElement>("OptionsContainer");
Button option0 = root.Q<Button>("Option0");
Button option1 = root.Q<Button>("Option1");
Button option2 = root.Q<Button>("Option2");
Button option3 = root.Q<Button>("Option3");

// Feedback
VisualElement feedbackPanel = root.Q<VisualElement>("FeedbackPanel");
Label feedbackIcon = root.Q<Label>("FeedbackIcon");
Label feedbackTitle = root.Q<Label>("FeedbackTitle");
Label wordMain = root.Q<Label>("WordMain");
Label wordPhonetic = root.Q<Label>("WordPhonetic");
Label wordPOS = root.Q<Label>("WordPOS");
Label wordMeaning = root.Q<Label>("WordMeaning");

// Actions
Button submitButton = root.Q<Button>("SubmitButton");
Button nextButton = root.Q<Button>("NextButton");
```

## 🎯 Next Steps (Phase 4)

After scene setup, implement:
1. `VocabularyTestManager.cs` - Main controller
2. `QuestionGenerator.cs` - Question creation logic
3. Wire up all UI interactions
4. Handle 4 question types dynamically

## 📝 Notes

- The UI uses **visible**/**hidden** classes to toggle question modes
- Feedback panel starts hidden, shows after answer submission
- Progress bar width is animated (10%, 20%, 30%, etc.)
- All styling follows the project's existing design system
- Modern, clean aesthetic with smooth transitions
