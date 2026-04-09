using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Board: quản lý swap, kiểm tra match, destroy, dồn và refill (item mới spawn trên cao rồi rơi).
/// Khi phá hủy sẽ lưu tạm id item đã bị phá vào destroyedIdsThisTurn.
/// Sau khi xử lý hết chain sẽ gọi GameManager.Instance.EndTurn(destroyedIdsThisTurn).
/// Nếu swap sai thì gọi GameManager.Instance.EndTurn() ngay.
/// </summary>
public class Board : MonoBehaviour
{
    public static Board Instance;

    public Match3Resource SpriteResource; // tài nguyên sprite (gồm id + sprite)
    public Reels[] reelsArray;            // mảng cột (đặt 8 reels trong inspector)

    private List<GameObject> itemList;    // danh sách tất cả item hiện có (Board quản lý)
    private Items selectedItem = null;    // item được chọn để swap

    // Lưu tạm id item bị phá trong 1 lần swap (dùng để truyền cho GameManager/UI)
    private List<int> destroyedIdsThisTurn = null;

    void Awake()
    {
        Instance = this;
        itemList = new List<GameObject>();
    }

    void Start()
    {
        InitializeBoard();
    }

    void InitializeBoard()
    {
        if (reelsArray == null || reelsArray.Length == 0)
        {
            Debug.LogError("Reels chưa được thiết lập trên Board!");
            return;
        }

        for (int col = 0; col < reelsArray.Length; col++)
        {
            Reels reel = reelsArray[col];
            if (reel != null)
            {
                RectTransform reelRect = reel.GetComponent<RectTransform>();
                if (reelRect != null)
                    reelRect.anchoredPosition = new Vector2(col * 100 - (reelsArray.Length * 50f), 0);

                reel.InitializeItems();

                // sau khi reel tạo item → gán id tránh match 3
                foreach (Transform child in reel.transform)
                {
                    Items it = child.GetComponent<Items>();
                    if (it != null)
                    {
                        AssignRandomIdAvoidMatch(it);
                        RegisterItem(it.gameObject);
                    }
                }
            }
        }
    }
    private void AssignRandomIdAvoidMatch(Items item)
    {
        if (SpriteResource == null || SpriteResource.spriteItems == null || SpriteResource.spriteItems.Length == 0)
            return;

        int maxTries = 10;
        int chosenId = -1;
        Sprite chosenSprite = null;

        for (int t = 0; t < maxTries; t++)
        {
            int randomIndex = Random.Range(0, SpriteResource.spriteItems.Length);
            int candidateId = SpriteResource.spriteItems[randomIndex].id;
            Sprite candidateSprite = SpriteResource.spriteItems[randomIndex].sprite;

            // check ngang
            bool makesHorizontalMatch = false;
            if (item.column >= 2)
            {
                GameObject left1 = GetItemAt(item.column - 1, item.row);
                GameObject left2 = GetItemAt(item.column - 2, item.row);
                if (left1 != null && left2 != null)
                {
                    var l1 = left1.GetComponent<Items>();
                    var l2 = left2.GetComponent<Items>();
                    if (l1 != null && l2 != null && l1.itemId == candidateId && l2.itemId == candidateId)
                        makesHorizontalMatch = true;
                }
            }

            // check dọc
            bool makesVerticalMatch = false;
            if (item.row >= 2)
            {
                GameObject down1 = GetItemAt(item.column, item.row - 1);
                GameObject down2 = GetItemAt(item.column, item.row - 2);
                if (down1 != null && down2 != null)
                {
                    var d1 = down1.GetComponent<Items>();
                    var d2 = down2.GetComponent<Items>();
                    if (d1 != null && d2 != null && d1.itemId == candidateId && d2.itemId == candidateId)
                        makesVerticalMatch = true;
                }
            }

            if (!makesHorizontalMatch && !makesVerticalMatch)
            {
                chosenId = candidateId;
                chosenSprite = candidateSprite;
                break;
            }
        }

        // fallback nếu vẫn fail sau maxTries
        if (chosenId == -1)
        {
            int randomIndex = Random.Range(0, SpriteResource.spriteItems.Length);
            chosenId = SpriteResource.spriteItems[randomIndex].id;
            chosenSprite = SpriteResource.spriteItems[randomIndex].sprite;
        }

        item.SetItem(chosenId, chosenSprite);
    }



    // Reels gọi hàm này để Board quản lý item mới
    public void RegisterItem(GameObject item)
    {
        if (!itemList.Contains(item)) itemList.Add(item);
    }

    // ========== Quản lý selection ==========
    public void SetSelectedItem(Items item)
    {
        if (selectedItem != null)
        {
            Image prevImg = selectedItem.GetComponent<Image>();
            if (prevImg != null) prevImg.color = Color.white;
        }
        selectedItem = item;
        if (selectedItem != null)
        {
            Image curr = selectedItem.GetComponent<Image>();
            if (curr != null) curr.color = Color.yellow;
        }
    }

    public Items GetSelectedItem() => selectedItem;

    private void ResetSelection()
    {
        if (selectedItem != null)
        {
            Image img = selectedItem.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
        selectedItem = null;
    }

    // ========== Swap ==========
    // Trước khi swap, lưu vị trí gốc để revert nếu không có match
    public void SwapItems(Items targetItem)
    {
        if (selectedItem == null || targetItem == null || selectedItem == targetItem)
        {
            ResetSelection();
            return;
        }

        int selColOrig = selectedItem.column;
        int selRowOrig = selectedItem.row;
        int tarColOrig = targetItem.column;
        int tarRowOrig = targetItem.row;

        int columnDiff = Mathf.Abs(selColOrig - tarColOrig);
        int rowDiff = Mathf.Abs(selRowOrig - tarRowOrig);

        bool isValidSwap = (columnDiff == 0 && rowDiff == 1) || (columnDiff == 1 && rowDiff == 0);
        if (!isValidSwap)
        {
            ResetSelection();
            return;
        }

        // thực hiện swap parent & position
        Transform selTrans = selectedItem.transform;
        Transform tarTrans = targetItem.transform;

        Reels selReel = reelsArray[selColOrig];
        Reels tarReel = reelsArray[tarColOrig];

        Vector3 selPos = selTrans.position;
        Vector3 tarPos = tarTrans.position;

        // swap parent
        selTrans.SetParent(tarReel.transform, true);
        tarTrans.SetParent(selReel.transform, true);

        // đổi giá trị row/column trong Items
        selectedItem.column = tarColOrig;
        selectedItem.row = tarRowOrig;
        targetItem.column = selColOrig;
        targetItem.row = selRowOrig;

        // giữ vị trí world (tránh layout đè)
        selTrans.position = tarPos;
        tarTrans.position = selPos;

        // KHỞI chạy kiểm tra match
        StartCoroutine(CheckAndDestroyMatches(selectedItem, targetItem, selColOrig, selRowOrig, tarColOrig, tarRowOrig));

        ResetSelection();
    }

    // ========== Kiểm tra match cho swap cụ thể ==========
    private IEnumerator CheckAndDestroyMatches(
      Items selected, Items target,
      int selColOrig, int selRowOrig,
      int tarColOrig, int tarRowOrig)
    {
        yield return new WaitForEndOfFrame(); // đợi layout ổn định

        // reset danh sách destroyed ids cho lần swap này
        destroyedIdsThisTurn = new List<int>();

        List<GameObject> itemsToDestroy = new List<GameObject>();

        itemsToDestroy.AddRange(CheckHorizontalMatches(selected.column, selected.row));
        itemsToDestroy.AddRange(CheckVerticalMatches(selected.column, selected.row));
        itemsToDestroy.AddRange(CheckHorizontalMatches(target.column, target.row));
        itemsToDestroy.AddRange(CheckVerticalMatches(target.column, target.row));

        itemsToDestroy = itemsToDestroy.Distinct().ToList();

        if (itemsToDestroy.Count > 0)
        {
            // phá hủy — trước khi Destroy thì lưu id
            foreach (GameObject it in itemsToDestroy)
            {
                if (it != null)
                {
                    Items comp = it.GetComponent<Items>();
                    if (comp != null)
                    {
                        destroyedIdsThisTurn.Add(comp.itemId);
                    }

                    itemList.Remove(it);
                    Destroy(it);
                }
            }

            yield return new WaitForEndOfFrame();

            // dồn và refill
            yield return StartCoroutine(CollapseAndRefill());

            // sau khi dồn xong, xử lý chain-match (nếu có)
            yield return StartCoroutine(ResolveChainMatches());

            // Sau khi xử lý chain xong, gọi EndTurn và truyền destroyedIdsThisTurn để UIManager hiển thị trong EndTurn
            List<int> idsToSend = new List<int>(destroyedIdsThisTurn); // copy để an toàn
            destroyedIdsThisTurn = null;
            GameManager.Instance.EndTurn(idsToSend);
        }
        else
        {
            // không có match -> revert swap (trả về vị trí ban đầu)
            yield return new WaitForSeconds(0.3f);

            Transform selTrans = selected.transform;
            Transform tarTrans = target.transform;

            Reels selReelOrigObj = reelsArray[selColOrig];
            Reels tarReelOrigObj = reelsArray[tarColOrig];

            selTrans.SetParent(selReelOrigObj.transform, true);
            tarTrans.SetParent(tarReelOrigObj.transform, true);

            RectTransform selRect = selTrans.GetComponent<RectTransform>();
            RectTransform tarRect = tarTrans.GetComponent<RectTransform>();
            float baseY = reelsArray[0].baseY;
            float spacing = reelsArray[0].rowSpacing;

            if (selRect != null) selRect.anchoredPosition = new Vector2(0, baseY + selRowOrig * spacing);
            if (tarRect != null) tarRect.anchoredPosition = new Vector2(0, baseY + tarRowOrig * spacing);

            selected.column = selColOrig;
            selected.row = selRowOrig;
            target.column = tarColOrig;
            target.row = tarRowOrig;

            // Swap sai -> EndTurn ngay (không có UI destroyed items)
            GameManager.Instance.EndTurn(null);
        }
    }

    // ========== Collapse & Refill (IEnumerator: gọi và đợi hoàn tất) ==========
    private IEnumerator CollapseAndRefill()
    {
        // Khoảng cách tối đa cần rơi (tính để chờ animation)
        float maxDistance = 0f;
        float minSpeed = float.MaxValue;

        // Duyệt từng cột
        for (int col = 0; col < reelsArray.Length; col++)
        {
            Reels reel = reelsArray[col];
            if (reel == null) continue;

            // thu thập item còn lại trong reel (lấy từ transform.children để tránh dữ liệu cũ)
            List<Items> remaining = new List<Items>();
            foreach (Transform child in reel.transform)
            {
                Items it = child.GetComponent<Items>();
                if (it != null) remaining.Add(it);
            }

            // sort theo row hiện tại (bottom -> top). Nếu row không tin cậy, có thể sort theo anchoredPosition.y
            remaining = remaining.OrderBy(x => x.row).ToList();

            // di chuyển tồn tại xuống từ row 0..n-1
            for (int r = 0; r < remaining.Count; r++)
            {
                Items it = remaining[r];
                RectTransform rect = it.GetComponent<RectTransform>();
                if (rect == null) continue;

                float currentY = rect.anchoredPosition.y;
                float targetY = reel.baseY + (r * reel.rowSpacing);

                if (Mathf.Abs(currentY - targetY) > 0.01f)
                {
                    // animation di chuyển
                    StartCoroutine(MoveRectTo(rect, targetY, reel.fallSpeed));
                }

                // cập nhật chỉ số row
                it.row = r;

                // tính distance/speed để biết phải chờ bao lâu
                maxDistance = Mathf.Max(maxDistance, Mathf.Abs(currentY - targetY));
                minSpeed = Mathf.Min(minSpeed, reel.fallSpeed);
            }

            // spawn thêm nếu thiếu
            int missing = reel.itemCount - remaining.Count;
            for (int i = 0; i < missing; i++)
            {
                int targetRow = remaining.Count + i;
                reel.SpawnNewItemAtRow(targetRow, false); // spawn trên cao rồi rơi
                // spawnOffset là distance mà item mới rơi
                maxDistance = Mathf.Max(maxDistance, reel.spawnOffset);
                minSpeed = Mathf.Min(minSpeed, reel.fallSpeed);
            }
        }

        // nếu không có item di chuyển thì không cần chờ lâu
        float waitTime = 0.6f;
        if (minSpeed > 0f && maxDistance > 0f) waitTime = (maxDistance / minSpeed) + 0.05f;
        yield return new WaitForSeconds(waitTime);
    }

    // Coroutine di chuyển rect tới targetY với tốc độ speed (px/s)
    private IEnumerator MoveRectTo(RectTransform rect, float targetY, float speed)
    {
        if (rect == null) yield break;
        while (Mathf.Abs(rect.anchoredPosition.y - targetY) > 0.05f)
        {
            float newY = Mathf.MoveTowards(rect.anchoredPosition.y, targetY, speed * Time.deltaTime);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, newY);
            yield return null;
        }
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, targetY);
    }

    // ========== Chain resolution: tìm tất cả match trên bảng, destroy và refill lặp đến khi hết ==========
    private IEnumerator ResolveChainMatches()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            List<GameObject> allMatches = GetAllMatches();
            if (allMatches.Count == 0) break;

            foreach (GameObject it in allMatches)
            {
                if (it != null)
                {
                    // lưu id trước khi destroy (nếu đang trong một lần swap xử lý)
                    Items comp = it.GetComponent<Items>();
                    if (comp != null)
                    {
                        if (destroyedIdsThisTurn == null) destroyedIdsThisTurn = new List<int>();
                        destroyedIdsThisTurn.Add(comp.itemId);
                    }

                    itemList.Remove(it);
                    Destroy(it);
                }
            }

            yield return new WaitForEndOfFrame();
            yield return StartCoroutine(CollapseAndRefill());
        }

        // khi hàng chuỗi kết thúc, không gọi EndTurn ở đây (Board sẽ gọi EndTurn sau khi ResolveChainMatches trả về)
    }

    // Lấy tất cả match hiện có (scan toàn bộ grid)
    private List<GameObject> GetAllMatches()
    {
        List<GameObject> matches = new List<GameObject>();
        for (int col = 0; col < reelsArray.Length; col++)
        {
            for (int row = 0; row < reelsArray[col].itemCount; row++)
            {
                matches.AddRange(CheckHorizontalMatches(col, row));
                matches.AddRange(CheckVerticalMatches(col, row));
            }
        }
        return matches.Distinct().ToList();
    }

    // ========== Kiểm tra match ngang / dọc (giữ nguyên logic cũ) ==========
    private List<GameObject> CheckHorizontalMatches(int col, int row)
    {
        List<GameObject> matches = new List<GameObject>();
        GameObject center = GetItemAt(col, row);
        if (center == null) return matches;

        Items centerItem = center.GetComponent<Items>();
        if (centerItem == null) return matches;
        int centerId = centerItem.itemId;
        matches.Add(center);

        int left = col - 1;
        while (left >= 0)
        {
            GameObject item = GetItemAt(left, row);
            if (item != null && item.GetComponent<Items>()?.itemId == centerId)
            {
                matches.Add(item);
                left--;
            }
            else break;
        }

        int right = col + 1;
        while (right < reelsArray.Length)
        {
            GameObject item = GetItemAt(right, row);
            if (item != null && item.GetComponent<Items>()?.itemId == centerId)
            {
                matches.Add(item);
                right++;
            }
            else break;
        }

        return (matches.Count >= 3) ? matches : new List<GameObject>();
    }

    private List<GameObject> CheckVerticalMatches(int col, int row)
    {
        List<GameObject> matches = new List<GameObject>();
        GameObject center = GetItemAt(col, row);
        if (center == null) return matches;

        Items centerItem = center.GetComponent<Items>();
        if (centerItem == null) return matches;
        int id = centerItem.itemId;
        matches.Add(center);

        int up = row - 1;
        while (up >= 0)
        {
            GameObject item = GetItemAt(col, up);
            if (item != null && item.GetComponent<Items>()?.itemId == id)
            {
                matches.Add(item);
                up--;
            }
            else break;
        }

        int down = row + 1;
        while (down < reelsArray[col].itemCount)
        {
            GameObject item = GetItemAt(col, down);
            if (item != null && item.GetComponent<Items>()?.itemId == id)
            {
                matches.Add(item);
                down++;
            }
            else break;
        }

        return (matches.Count >= 3) ? matches : new List<GameObject>();
    }

    // Lấy item theo col,row (dùng itemList do Reels đăng ký)
    public GameObject GetItemAt(int column, int row)
    {
        foreach (GameObject item in itemList)
        {
            if (item == null) continue;
            Items comp = item.GetComponent<Items>();
            if (comp != null && comp.column == column && comp.row == row)
                return item;
        }
        return null;
    }
}
