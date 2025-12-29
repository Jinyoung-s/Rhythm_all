using UnityEngine;

public class PopupTest : MonoBehaviour
{
    void Update()
    {
        // Press 1 for a simple alert
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PopupManager.Instance.ShowPopup(
                "Notice", 
                "This is a simple alert message with one button.", 
                "OK"
            );
        }

        // Press 2 for a confirmation dialog
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PopupManager.Instance.ShowPopup(
                "Reset Progress?", 
                "This will permanently delete your current score. Continue?", 
                "Delete", 
                () => Debug.Log("Confirmed Delete"),
                "Cancel",
                () => Debug.Log("Canceled Delete")
            );
        }
    }
}
