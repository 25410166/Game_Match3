using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public static AIController Instance;

    [Header("AI Settings")]
    public float thinkDelay = 0.6f;
    public float afterSelectDelay = 0.12f;
    public float maxWaitTurnSeconds = 8f;

    [Header("AI Difficulty Tuning")]
    [SerializeField] private int swordPriorityStartTurn = 6;
    [SerializeField] private int harderMapThreshold = 124;
    [SerializeField] private float baseMatchScorePerGem = 10f;
    [SerializeField] private float swordPriorityScore = 35f;
    [SerializeField] private float manaPriorityScore = 18f;
    [SerializeField] private float biggerMatchBonusPerExtraGem = 12f;
    [SerializeField] private float harderMapSwordBonus = 30f;
    [SerializeField] private float harderMapManaBonus = 20f;
    [SerializeField] private float harderMapBigMatchBonus = 10f;

    private MethodInfo horizontalMatchMethod;
    private MethodInfo verticalMatchMethod;

    private struct MoveCandidate
    {
        public Items itemA;
        public Items itemB;
        public int totalMatchedGems;
        public int dominantItemId;
        public float score;
    }

    private void Awake()
    {
        Instance = this;
    }

    public IEnumerator MakeMove()
    {
        yield return new WaitForSeconds(thinkDelay);

        AIStats aiStats = GameManager.Instance != null ? GameManager.Instance.ai : null;
        if (aiStats != null && aiStats.CanUseEquippedSkill())
        {
            Debug.Log("AI: đủ tài nguyên, ưu tiên dùng skill.");
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
            Debug.Log($"AI: swap ({itemA.column},{itemA.row}) <-> ({itemB.column},{itemB.row})");
            board.SetSelectedItem(itemA);
            yield return new WaitForSeconds(afterSelectDelay);
            board.SwapItems(itemB);

            float timer = 0f;
            while (GameManager.Instance != null && GameManager.Instance.currentTurn == GameManager.Turn.AI && timer < maxWaitTurnSeconds)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (timer >= maxWaitTurnSeconds)
                Debug.LogWarning("AI: timeout waiting for turn to finish.");
        }
        else
        {
            Debug.Log("AI: không tìm thấy nước đi hợp lệ → bỏ lượt.");
            GameManager.Instance.EndTurn(null);

            float timer = 0f;
            while (GameManager.Instance != null && GameManager.Instance.currentTurn == GameManager.Turn.AI && timer < maxWaitTurnSeconds)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void FindBestMove(Board board, out Items itemA, out Items itemB)
    {
        itemA = null;
        itemB = null;

        if (board == null || board.reelsArray == null || board.reelsArray.Length == 0)
            return;

        CacheBoardReflection(board);
        if (horizontalMatchMethod == null || verticalMatchMethod == null)
        {
            Debug.LogWarning("AI: không tìm thấy CheckHorizontalMatches/CheckVerticalMatches bằng reflection.");
            return;
        }

        MoveCandidate bestMove = default;
        bool found = false;

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

                TryEvaluateNeighbor(board, it, c + 1, r, ref found, ref bestMove);
                TryEvaluateNeighbor(board, it, c, r + 1, ref found, ref bestMove);
            }
        }

        if (!found)
            return;

        itemA = bestMove.itemA;
        itemB = bestMove.itemB;

        Debug.Log($"AI: chọn move score={bestMove.score:0.##}, gemType={bestMove.dominantItemId}, matched={bestMove.totalMatchedGems}, turn={GetCurrentTurnIndex()}, hardMap={IsHarderMap()}");
    }

    private void TryEvaluateNeighbor(Board board, Items origin, int targetCol, int targetRow, ref bool found, ref MoveCandidate bestMove)
    {
        if (origin == null)
            return;

        int cols = board.reelsArray.Length;
        int rows = board.reelsArray[0].itemCount;
        if (targetCol < 0 || targetCol >= cols || targetRow < 0 || targetRow >= rows)
            return;

        GameObject neighborObject = board.GetItemAt(targetCol, targetRow);
        if (neighborObject == null)
            return;

        Items neighbor = neighborObject.GetComponent<Items>();
        if (neighbor == null)
            return;

        if (!TryEvaluateMove(board, origin, neighbor, out MoveCandidate candidate))
            return;

        if (!found || candidate.score > bestMove.score)
        {
            bestMove = candidate;
            found = true;
        }
    }

    private bool TryEvaluateMove(Board board, Items a, Items b, out MoveCandidate candidate)
    {
        candidate = default;
        if (a == null || b == null)
            return false;

        int aId = a.itemId;
        int bId = b.itemId;

        a.itemId = bId;
        b.itemId = aId;

        Dictionary<int, int> matchCounts = CollectMatchCounts(board, a, b);

        a.itemId = aId;
        b.itemId = bId;

        if (matchCounts.Count == 0)
            return false;

        int totalMatchedGems = matchCounts.Values.Sum();
        int dominantItemId = matchCounts
            .OrderByDescending(kv => kv.Value)
            .ThenByDescending(kv => GetItemPriority(kv.Key))
            .Select(kv => kv.Key)
            .FirstOrDefault();

        candidate.itemA = a;
        candidate.itemB = b;
        candidate.totalMatchedGems = totalMatchedGems;
        candidate.dominantItemId = dominantItemId;
        candidate.score = CalculateMoveScore(matchCounts, totalMatchedGems);
        return true;
    }

    private Dictionary<int, int> CollectMatchCounts(Board board, Items a, Items b)
    {
        Dictionary<int, int> counts = new Dictionary<int, int>();
        HashSet<GameObject> uniqueMatches = new HashSet<GameObject>();

        AddMatchesFromCell(board, a.column, a.row, counts, uniqueMatches);
        AddMatchesFromCell(board, b.column, b.row, counts, uniqueMatches);

        return counts;
    }

    private void AddMatchesFromCell(Board board, int column, int row, Dictionary<int, int> counts, HashSet<GameObject> uniqueMatches)
    {
        AddMatchList(horizontalMatchMethod.Invoke(board, new object[] { column, row }) as List<GameObject>, counts, uniqueMatches);
        AddMatchList(verticalMatchMethod.Invoke(board, new object[] { column, row }) as List<GameObject>, counts, uniqueMatches);
    }

    private void AddMatchList(List<GameObject> matches, Dictionary<int, int> counts, HashSet<GameObject> uniqueMatches)
    {
        if (matches == null || matches.Count < 3)
            return;

        for (int i = 0; i < matches.Count; i++)
        {
            GameObject match = matches[i];
            if (match == null || !uniqueMatches.Add(match))
                continue;

            Items item = match.GetComponent<Items>();
            if (item == null)
                continue;

            int gemValue = Mathf.Max(1, item.gemValue);
            if (counts.ContainsKey(item.itemId))
                counts[item.itemId] += gemValue;
            else
                counts[item.itemId] = gemValue;
        }
    }

    private float CalculateMoveScore(Dictionary<int, int> matchCounts, int totalMatchedGems)
    {
        float score = totalMatchedGems * baseMatchScorePerGem;
        bool preferSwordAndMana = ShouldPreferSwordAndMana();
        bool harderMap = IsHarderMap();

        foreach (KeyValuePair<int, int> kv in matchCounts)
        {
            int itemId = kv.Key;
            int gemCount = kv.Value;

            if (itemId == BoardEffectSystem.AttackItemId)
            {
                score += swordPriorityScore;
                if (preferSwordAndMana)
                    score += swordPriorityScore;
                if (harderMap)
                    score += harderMapSwordBonus;
            }
            else if (itemId == BoardEffectSystem.ManaItemId)
            {
                if (preferSwordAndMana)
                    score += manaPriorityScore;
                if (harderMap)
                    score += harderMapManaBonus;
            }

            if (gemCount > 3)
            {
                score += (gemCount - 3) * biggerMatchBonusPerExtraGem;
                if (harderMap)
                    score += (gemCount - 3) * harderMapBigMatchBonus;
            }
        }

        return score;
    }

    private int GetItemPriority(int itemId)
    {
        if (itemId == BoardEffectSystem.AttackItemId)
            return 3;
        if (itemId == BoardEffectSystem.ManaItemId)
            return 2;
        return 1;
    }

    private bool ShouldPreferSwordAndMana()
    {
        return GetCurrentTurnIndex() >= swordPriorityStartTurn || IsHarderMap();
    }

    private int GetCurrentTurnIndex()
    {
        return GameManager.Instance != null ? GameManager.Instance.CurrentTurnIndex : 0;
    }

    private bool IsHarderMap()
    {
        if (string.IsNullOrWhiteSpace(PrebattleSelectionData.MapId))
            return false;

        if (!int.TryParse(PrebattleSelectionData.MapId, out int mapId))
            return false;

        return mapId > harderMapThreshold;
    }

    private void CacheBoardReflection(Board board)
    {
        if (horizontalMatchMethod != null && verticalMatchMethod != null)
            return;

        System.Type boardType = board.GetType();
        horizontalMatchMethod = boardType.GetMethod("CheckHorizontalMatches", BindingFlags.NonPublic | BindingFlags.Instance);
        verticalMatchMethod = boardType.GetMethod("CheckVerticalMatches", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
