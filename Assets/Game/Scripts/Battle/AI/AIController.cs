using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public static AIController Instance;

    [Header("AI Settings")]
    public float thinkDelay = 0.6f;       // thời gian 'nghĩ' trước khi quyết định
    public float afterSelectDelay = 0.12f; // thời gian sau khi SetSelectedItem trước khi Swap
    public float maxWaitTurnSeconds = 8f; // timeout khi chờ lượt thay đổi (phòng lỗi)

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Coroutine chính được GameManager gọi và chờ.
    /// </summary>
    public IEnumerator MakeMove()
    {
        yield return new WaitForSeconds(thinkDelay);

        AIStats aiStats = GameManager.Instance != null ? GameManager.Instance.ai : null;
        if (aiStats != null && aiStats.CanUseEquippedSkill())
        {
            Debug.Log("AI: ?? t?i nguy?n, ?u ti?n d?ng skill.");
            aiStats.Attack();

            float timer = 0f;
            while (GameManager.Instance != null && aiStats != null && aiStats.isAttacking && timer < maxWaitTurnSeconds)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (GameManager.Instance != null && GameManager.Instance.currentTurn == GameManager.Turn.AI)
                GameManager.Instance.EndTurn(null);

            yield break;
        }

        Board board = Board.Instance;
        if (board == null)
        {
            Debug.LogWarning("AI: Board.Instance == null");
            yield break;
        }

        Items itemA = null;
        Items itemB = null;

        FindBestMove(board, out itemA, out itemB);

        if (itemA != null && itemB != null)
        {
            // Thực hiện swap giống như người chơi
            Debug.Log($"AI: swap ({itemA.column},{itemA.row}) <-> ({itemB.column},{itemB.row})");
            board.SetSelectedItem(itemA);
            yield return new WaitForSeconds(afterSelectDelay);
            board.SwapItems(itemB);

            // Chờ cho tới khi lượt AI kết thúc (GameManager.currentTurn != AI) hoặc timeout
            float timer = 0f;
            while (GameManager.Instance != null && GameManager.Instance.currentTurn == GameManager.Turn.AI && timer < maxWaitTurnSeconds)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            if (timer >= maxWaitTurnSeconds) Debug.LogWarning("AI: timeout waiting for turn to finish.");
        }
        else
        {
            Debug.Log("AI: không tìm thấy nước đi hợp lệ → bỏ lượt.");
            // gọi EndTurn để trả về player
            GameManager.Instance.EndTurn(null);

            // chờ lượt thay đổi (phòng trường hợp EndTurn có UI sequence)
            float timer = 0f;
            while (GameManager.Instance != null && GameManager.Instance.currentTurn == GameManager.Turn.AI && timer < maxWaitTurnSeconds)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    /// <summary>
    /// Quét toàn bộ bảng, trả về 1 nước đi hợp lệ (nếu có).
    /// Thực hiện kiểm tra swap sang phải và lên (đủ để cover toàn board).
    /// </summary>
    private void FindBestMove(Board board, out Items itemA, out Items itemB)
    {
        itemA = null;
        itemB = null;

        int cols = board.reelsArray.Length;
        int rows = board.reelsArray[0].itemCount;

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                GameObject g = board.GetItemAt(c, r);
                if (g == null) continue;
                Items it = g.GetComponent<Items>();
                if (it == null) continue;

                // thử sang phải
                if (c + 1 < cols)
                {
                    GameObject right = board.GetItemAt(c + 1, r);
                    if (right != null)
                    {
                        Items itR = right.GetComponent<Items>();
                        if (itR != null && CheckPotentialMatch(board, it, itR))
                        {
                            itemA = it;
                            itemB = itR;
                            return;
                        }
                    }
                }

                // thử lên
                if (r + 1 < rows)
                {
                    GameObject up = board.GetItemAt(c, r + 1);
                    if (up != null)
                    {
                        Items itU = up.GetComponent<Items>();
                        if (itU != null && CheckPotentialMatch(board, it, itU))
                        {
                            itemA = it;
                            itemB = itU;
                            return;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tạm hoán đổi itemId giữa a và b, gọi các hàm CheckHorizontalMatches/CheckVerticalMatches trong Board (private) bằng reflection,
    /// nếu có match trả về true. Sau đó revert lại itemId.
    /// </summary>
    private bool CheckPotentialMatch(Board board, Items a, Items b)
    {
        if (a == null || b == null) return false;

        // lưu lại
        int aId = a.itemId;
        int bId = b.itemId;

        // swap id tạm
        a.itemId = bId;
        b.itemId = aId;

        bool hasMatch = false;
        // gọi reflect để lấy method
        MethodInfo hMethod = board.GetType().GetMethod("CheckHorizontalMatches", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo vMethod = board.GetType().GetMethod("CheckVerticalMatches", BindingFlags.NonPublic | BindingFlags.Instance);

        if (hMethod != null && vMethod != null)
        {
            var h1 = hMethod.Invoke(board, new object[] { a.column, a.row }) as List<GameObject>;
            var v1 = vMethod.Invoke(board, new object[] { a.column, a.row }) as List<GameObject>;
            var h2 = hMethod.Invoke(board, new object[] { b.column, b.row }) as List<GameObject>;
            var v2 = vMethod.Invoke(board, new object[] { b.column, b.row }) as List<GameObject>;

            if ((h1 != null && h1.Count >= 3) || (v1 != null && v1.Count >= 3) ||
                (h2 != null && h2.Count >= 3) || (v2 != null && v2.Count >= 3))
            {
                hasMatch = true;
            }
        }
        else
        {
            // fallback: nếu không tìm được method private thì return false
            Debug.LogWarning("AI: không tìm thấy CheckHorizontalMatches/CheckVerticalMatches bằng reflection.");
        }

        // revert id
        a.itemId = aId;
        b.itemId = bId;

        return hasMatch;
    }
}
