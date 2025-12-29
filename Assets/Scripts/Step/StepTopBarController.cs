using UnityEngine;
using UnityEngine.SceneManagement;

public class StepTopBarController : MonoBehaviour
{
    public void OnClickBack()
    {
        SceneNavigator.Load("MainMenuScene");
    }
}