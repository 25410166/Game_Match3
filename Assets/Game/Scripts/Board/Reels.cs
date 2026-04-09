using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Reels: tạo item ban đầu (instant) và spawn item mới (rơi xuống).
/// InitializeItems() chỉ dành cho khởi tạo ban đầu.
/// SpawnNewItemAtRow(row, instant=false) sẽ spawn trên cao rồi rơi.
/// </summary>
public class Reels : MonoBehaviour
{
    public GameObject items;       // Prefab item (đặt prefab trong inspector; prefab thường để inactive)
    public int itemCount = 8;      // Số ô trong cột
    public float fallSpeed = 800f; // tốc độ rơi (px/s)
    public float spawnOffset = 300f; // spawn cao hơn targetY (px)

    // các tham số layout (nếu bạn thay đổi layout, chỉnh ở đây)
    public float baseY = -400f;    // y của row 0 (dưới cùng)
    public float rowSpacing = 105f; // khoảng cách giữa các row

    // Initialize: dùng cho tạo ban đầu (không rơi)
    public void InitializeItems()
    {
        if (items != null) items.SetActive(false);

        for (int i = 0; i < itemCount; i++)
        {
            SpawnNewItemAtRow(i, true); // instant = true -> đặt ngay tại vị trí row i
        }
    }

    /// <summary>
    /// Spawn item tại row. Nếu instant = false -> spawn ở trên (targetY + spawnOffset) rồi rơi xuống.
    /// Hàm sẽ set sprite random dựa trên Board.Instance.SpriteResource và gọi Board.Instance.RegisterItem().
    /// </summary>
    public void SpawnNewItemAtRow(int row, bool instant = false)
    {
        if (items == null) return;

        GameObject newItem = Instantiate(items, transform);
        newItem.SetActive(true);

        RectTransform rect = newItem.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            float targetY = baseY + (row * rowSpacing);
            rect.anchoredPosition = instant ? new Vector2(0, targetY) : new Vector2(0, targetY + spawnOffset);
        }

        Items comp = newItem.GetComponent<Items>();
        if (comp != null)
        {
            comp.row = row;
            comp.column = transform.GetSiblingIndex();

            // Gán sprite random từ Board.SpriteResource (nếu có)
            var spriteRes = Board.Instance?.SpriteResource;
            if (spriteRes != null && spriteRes.spriteItems != null && spriteRes.spriteItems.Length > 0)
            {
                int idx = Random.Range(0, spriteRes.spriteItems.Length);
                comp.itemId = spriteRes.spriteItems[idx].id;
                Image img = newItem.GetComponent<Image>();
                if (img != null) img.sprite = spriteRes.spriteItems[idx].sprite;
            }

            // Đăng ký item với Board
            Board.Instance?.RegisterItem(newItem);
        }

        // Nếu không instant -> rơi xuống animation
        if (!instant && rect != null)
        {
            float targetY = baseY + (row * rowSpacing);
            StartCoroutine(FallToPosition(rect, targetY));
        }
    }

    // Coroutine rơi xuống tới targetY (sử dụng fallSpeed)
    private IEnumerator FallToPosition(RectTransform rect, float targetY)
    {
        if (rect == null) yield break;
        while (rect.anchoredPosition.y > targetY + 0.05f)
        {
            float newY = Mathf.MoveTowards(rect.anchoredPosition.y, targetY, fallSpeed * Time.deltaTime);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, newY);
            yield return null;
        }
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, targetY);
    }
}
