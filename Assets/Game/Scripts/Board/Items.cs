using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Items : MonoBehaviour, IPointerClickHandler
{
    private Image imageComponent;
    public int itemId;
    public int column;
    public int row;

    void Awake()
    {
        imageComponent = GetComponent<Image>();
    }

    // Board sẽ gọi hàm này để gán id + sprite
    public void SetItem(int id, Sprite sprite)
    {
        itemId = id;
        if (imageComponent == null) imageComponent = GetComponent<Image>();
        if (imageComponent != null) imageComponent.sprite = sprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!GameManager.Instance.CanPlayerMove()) return;

        Board board = Board.Instance;
        if (board != null)
        {
            Items previousItem = board.GetSelectedItem();
            if (previousItem != null && previousItem != this)
            {
                board.SwapItems(this);
            }
            else
            {
                board.SetSelectedItem(this);
                if (imageComponent != null)
                    imageComponent.color = Color.yellow;
            }
        }
    }

    public RectTransform GetRectTransform()
    {
        return GetComponent<RectTransform>();
    }
}
