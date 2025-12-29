using UnityEngine;
using UnityEngine.UIElements;

public class CardListController : MonoBehaviour
{
    public Sprite coverSprite1;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var image = root.Q<Image>("cover1");
        if (image != null && coverSprite1 != null)
        {
            image.sprite = coverSprite1;
        }
    }
}