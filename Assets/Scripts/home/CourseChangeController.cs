using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class CourseChangeController : MonoBehaviour
{
    private Button changeSceneButton;

    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        changeSceneButton = root.Q<Button>("changeCourseButton"); 

        if (changeSceneButton != null)
        {
            changeSceneButton.clicked += OnChangeSceneClicked;
        }
        else
        {
            Debug.LogError("ChangeSceneButton not found in UXML!");
        }
    }

    private void OnChangeSceneClicked()
    {
        // 불러올 씬 이름을 지정합니다.        
        SceneNavigator.Load("CourseScene");
    }
}
