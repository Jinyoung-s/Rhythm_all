using System;

public enum AuthProviderType
{
    Guest,
    Google,
    Apple,
    EmailPassword
}

[Serializable]
public class UserProfile
{
    public string UserId;        // Firebase UID
    public string DisplayName;
    public string Email;
    public bool IsGuest;
    public bool IsSubscribed;
    public string LastLoginProvider; // "google", "apple", "email", "guest"
}

public class UserProfileManager
{
    private static UserProfileManager _instance;
    public static UserProfileManager Instance => _instance ??= new UserProfileManager();

    public UserProfile CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.UserId);

    private UserProfileManager()
    {
        // TODO: 앱 시작 시, 로컬에 저장된 마지막 로그인 유저 복원 (선택)
    }

    // 아래 메서드들은 Firebase Auth 연동 시 실제 구현 필요
    public void SignInWithGoogle()
    {
        // TODO: Firebase Auth Google 로그인 수행
        // 로그인 성공 시 SetCurrentUser(...) 호출
        throw new NotImplementedException();
    }

    public void SignInWithApple()
    {
        // TODO: Firebase Auth Apple 로그인 수행
        throw new NotImplementedException();
    }

    public void SignInWithEmailPassword(string email, string password)
    {
        // TODO: Firebase Auth Email/Password 로그인 수행
        throw new NotImplementedException();
    }

    public void SignInAsGuest()
    {
        // TODO: Firebase의 익명 로그인 사용
        throw new NotImplementedException();
    }

    public void SignOut()
    {
        // TODO: Firebase Auth SignOut 호출
        CurrentUser = null;
        // ProgressManager 등에도 알림을 보내서 현재 유저 기준 데이터 정리
    }

    public void SetCurrentUser(string userId, string email, string displayName, bool isGuest, string providerId)
    {
        CurrentUser = new UserProfile
        {
            UserId = userId,
            Email = email,
            DisplayName = displayName,
            IsGuest = isGuest,
            IsSubscribed = false,
            LastLoginProvider = providerId
        };

        // 여기서 ProgressManager에게 "이 유저 기준으로 진행도 로딩하라"고 알려줄 수 있음
        // 예: ProgressManager.Instance.AttachToUser(userId);
    }
}
