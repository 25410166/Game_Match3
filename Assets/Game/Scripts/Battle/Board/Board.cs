using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
public class Board : MonoBehaviour
{


    public event Action OnSuccessfulPlayerSwapResolved;
    public static Board Instance;

    [Header("Board")]
    public Reels[] reelsArray;
    public Match3Resource SpriteResource;

    [Header("Selection")]
    [SerializeField] private float swapDuration = 0.18f;

    [Header("Gem Value Progression")]
    [SerializeField] private int firstBoostTurn = 2;
    [SerializeField] private int turnsPerValueStep = 2;
    [SerializeField] private int maxGemValue = 5;

    [Header("FX")]
    [SerializeField] private GameObject explosionFxPrefab;

    private readonly List<GameObject> itemList = new List<GameObject>();
    private Items selectedItem;
    private bool isSwapping;
    private CanvasGroup presentationCanvasGroup;
    private List<int> destroyedIdsThisTurn = null;
    private List<GemEffectMatchEntry> effectEntriesThisTurn = null;

    private void Awake()
    {
        Instance = this;
    }


    private void Start()
    {
        InitializeBoard();
    }

    public void InitializeBoard()
    {
        if (reelsArray == null || reelsArray.Length == 0)
            reelsArray = GetComponentsInChildren<Reels>(true);

        itemList.Clear();

        if (reelsArray == null || reelsArray.Length == 0)
        {
            Debug.LogWarning("[Board] InitializeBoard failed: no reels found.");
            return;
        }

        for (int i = 0; i < reelsArray.Length; i++)
        {
            Reels reel = reelsArray[i];
            if (reel == null)
                continue;

            for (int childIndex = reel.transform.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = reel.transform.GetChild(childIndex);
                if (child == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            reel.InitializeItems();
        }

        EnsureBoardHasPotentialMove(true);
        Debug.Log($"[Board] InitializeBoard complete. Spawned items: {itemList.Count}");
    }
    public void SetPresentationVisible(bool visible)
    {
        if (presentationCanvasGroup == null)
        {
            presentationCanvasGroup = GetComponent<CanvasGroup>();
            if (presentationCanvasGroup == null)
                presentationCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        presentationCanvasGroup.alpha = visible ? 1f : 0f;
        presentationCanvasGroup.blocksRaycasts = visible;
        presentationCanvasGroup.interactable = visible;
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

            // check d?c
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

        // fallback n?u v?n fail sau maxTries
        if (chosenId == -1)
        {
            int randomIndex = Random.Range(0, SpriteResource.spriteItems.Length);
            chosenId = SpriteResource.spriteItems[randomIndex].id;
            chosenSprite = SpriteResource.spriteItems[randomIndex].sprite;
        }

        item.SetItem(chosenId, chosenSprite, item != null ? Mathf.Max(1, item.gemValue) : 1);
    }



    // Reels g?i hï¿½m nï¿½y d? Board qu?n lï¿½ item m?i
    public void RegisterItem(GameObject item)
    {
        if (!itemList.Contains(item)) itemList.Add(item);
    }

    // ========== Qu?n lï¿½ selection ==========

    public int RollGemValueForCurrentTurn()
    {
        int highestValue = GetMaxGemValueForCurrentTurn();
        if (highestValue <= 1)
            return 1;

        return Random.Range(1, highestValue + 1);
    }

    public int GetMaxGemValueForCurrentTurn()
    {
        int turnIndex = GameManager.Instance != null ? GameManager.Instance.CurrentTurnIndex : 0;
        if (turnIndex < firstBoostTurn)
            return 1;

        int step = Mathf.Max(0, ((turnIndex - firstBoostTurn) / Mathf.Max(1, turnsPerValueStep)) + 2);
        return Mathf.Clamp(step, 1, Mathf.Max(1, maxGemValue));
    }
    public void SetSelectedItem(Items item)
    {
        if (selectedItem != null)
        {
            selectedItem.SetSelectedVisual(false);
        }
        selectedItem = item;
        if (selectedItem != null)
        {
            selectedItem.SetSelectedVisual(true);
        }
    }

    public Items GetSelectedItem() => selectedItem;

    private void ResetSelection()
    {
        if (selectedItem != null)
        {
            selectedItem.SetSelectedVisual(false);
        }
        selectedItem = null;
    }


    private void AddDestroyedGemValue(Items comp)
    {
        if (comp == null)
            return;

        if (destroyedIdsThisTurn == null)
            destroyedIdsThisTurn = new List<int>();

        int gemAmount = Mathf.Max(1, comp.gemValue);
        for (int i = 0; i < gemAmount; i++)
            destroyedIdsThisTurn.Add(comp.itemId);
    }

    private void AddRemovedGemValues(List<int> removed, Items comp)
    {
        if (removed == null || comp == null)
            return;

        int gemAmount = Mathf.Max(1, comp.gemValue);
        for (int i = 0; i < gemAmount; i++)
            removed.Add(comp.itemId);
    }

    public bool HasAnyPotentialMove()
    {
        if (reelsArray == null || reelsArray.Length == 0)
            return false;

        for (int col = 0; col < reelsArray.Length; col++)
        {
            Reels reel = reelsArray[col];
            if (reel == null)
                continue;

            for (int row = 0; row < reel.itemCount; row++)
            {
                if (HasPotentialMoveAt(col, row, col + 1, row) || HasPotentialMoveAt(col, row, col, row + 1))
                    return true;
            }
        }

        return false;
    }

    public bool EnsureBoardHasPotentialMove(bool reshuffleIfNeeded = true)
    {
        if (HasAnyPotentialMove())
            return true;

        if (!reshuffleIfNeeded)
            return false;

        return ReshuffleBoardUntilPlayable();
    }

    private bool ReshuffleBoardUntilPlayable()
    {
        List<Items> items = GetAllBoardItemsOrdered();
        if (items.Count == 0)
            return false;

        for (int attempt = 0; attempt < 50; attempt++)
        {
            for (int i = 0; i < items.Count; i++)
                AssignRandomIdAvoidMatch(items[i]);

            if (GetAllMatches().Count == 0 && HasAnyPotentialMove())
                return true;
        }

        return false;
    }

    private List<Items> GetAllBoardItemsOrdered()
    {
        List<Items> result = new List<Items>();
        if (reelsArray == null)
            return result;

        for (int row = 0; row < (reelsArray.Length > 0 && reelsArray[0] != null ? reelsArray[0].itemCount : 0); row++)
        {
            for (int col = 0; col < reelsArray.Length; col++)
            {
                GameObject item = GetItemAt(col, row);
                Items comp = item != null ? item.GetComponent<Items>() : null;
                if (comp != null)
                    result.Add(comp);
            }
        }

        return result;
    }

    private bool HasPotentialMoveAt(int colA, int rowA, int colB, int rowB)
    {
        GameObject aObj = GetItemAt(colA, rowA);
        GameObject bObj = GetItemAt(colB, rowB);
        if (aObj == null || bObj == null)
            return false;

        Items a = aObj.GetComponent<Items>();
        Items b = bObj.GetComponent<Items>();
        if (a == null || b == null)
            return false;

        int aId = a.itemId;
        int bId = b.itemId;
        a.itemId = bId;
        b.itemId = aId;

        bool hasMatch = CheckHorizontalMatches(colA, rowA).Count >= 3 ||
                        CheckVerticalMatches(colA, rowA).Count >= 3 ||
                        CheckHorizontalMatches(colB, rowB).Count >= 3 ||
                        CheckVerticalMatches(colB, rowB).Count >= 3;

        a.itemId = aId;
        b.itemId = bId;
        return hasMatch;
    }
    // ========== Spawn Explosion Effect ==========
    private void SpawnExplosionFx(Vector3 position)
    {
        if (explosionFxPrefab != null)
        {
            BattleFxUtility.SpawnAutoDestroy(explosionFxPrefab, position, Quaternion.identity, 1f);
        }
        else
        {
            // Fallback: create a simple scale animation effect
            StartCoroutine(CreateFallbackExplosionFx(position));
        }
    }

    private IEnumerator CreateFallbackExplosionFx(Vector3 position)
    {
        // T?o m?t GameObject don gi?n d? hi?n hi?u ?ng explosion
        GameObject fx = new GameObject("ExplosionFx_Fallback");
        fx.transform.position = position;

        Image img = fx.AddComponent<Image>();
        img.color = new Color(1f, 0.8f, 0f, 0.8f); // vï¿½ng cam

        RectTransform rect = fx.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(80, 80);
        }

        // Scale up r?i fade out
        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 startScale = fx.transform.localScale;
        Vector3 targetScale = startScale * 1.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            fx.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            Color col = img.color;
            col.a = Mathf.Lerp(0.8f, 0f, t);
            img.color = col;

            yield return null;
        }

        Destroy(fx);
    }

    // ========== Swap ==========
    // Animate 2 gems swap positions with smooth movement
    private IEnumerator AnimateSwap(Transform selTrans, Transform tarTrans, Vector3 startSelPos, Vector3 startTarPos, Vector3 endSelPos, Vector3 endTarPos, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            selTrans.position = Vector3.Lerp(startSelPos, endSelPos, t);
            tarTrans.position = Vector3.Lerp(startTarPos, endTarPos, t);
            
            yield return null;
        }
        
        selTrans.position = endSelPos;
        tarTrans.position = endTarPos;
    }

    // Tru?c khi swap, luu v? trï¿½ g?c d? revert n?u khï¿½ng cï¿½ match
    public void SwapItems(Items targetItem)
    {
        if (selectedItem == null || targetItem == null || selectedItem == targetItem)
        {
            ResetSelection();
            if (GameManager.Instance != null)
                GameManager.Instance.CancelPlayerBoardInteraction();
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
            if (GameManager.Instance != null)
                GameManager.Instance.CancelPlayerBoardInteraction();
            return;
        }

        // th?c hi?n swap parent & position
        Transform selTrans = selectedItem.transform;
        Transform tarTrans = targetItem.transform;

        Reels selReel = reelsArray[selColOrig];
        Reels tarReel = reelsArray[tarColOrig];

        Vector3 selPos = selTrans.position;
        Vector3 tarPos = tarTrans.position;

        // swap parent
        selTrans.SetParent(tarReel.transform, true);
        tarTrans.SetParent(selReel.transform, true);

        // d?i giï¿½ tr? row/column trong Items
        selectedItem.column = tarColOrig;
        selectedItem.row = tarRowOrig;
        targetItem.column = selColOrig;
        targetItem.row = selRowOrig;

        // Animate swap positions thay vï¿½ set tr?c ti?p
        StartCoroutine(AnimateSwap(selTrans, tarTrans, selPos, tarPos, tarPos, selPos, swapDuration));

        // KH?I ch?y ki?m tra match SAU khi animate hoï¿½n t?t
        StartCoroutine(CheckAndDestroyMatchesAfterSwap(selectedItem, targetItem, selColOrig, selRowOrig, tarColOrig, tarRowOrig));

        ResetSelection();
    }

    // ï¿½?i animation swap hoï¿½n t?t tru?c khi check match
    private IEnumerator CheckAndDestroyMatchesAfterSwap(
      Items selected, Items target,
      int selColOrig, int selRowOrig,
      int tarColOrig, int tarRowOrig)
    {
        yield return new WaitForSeconds(swapDuration + 0.05f);
        yield return StartCoroutine(CheckAndDestroyMatches(selected, target, selColOrig, selRowOrig, tarColOrig, tarRowOrig));
    }

    // ========== Ki?m tra match cho swap c? th? ==========
    private IEnumerator CheckAndDestroyMatches(
      Items selected, Items target,
      int selColOrig, int selRowOrig,
      int tarColOrig, int tarRowOrig)
    {
        yield return new WaitForEndOfFrame(); // d?i layout ?n d?nh

        // reset danh sï¿½ch destroyed ids cho l?n swap nï¿½y
        destroyedIdsThisTurn = new List<int>();
        effectEntriesThisTurn = new List<GemEffectMatchEntry>();

        List<GameObject> itemsToDestroy = new List<GameObject>();

        itemsToDestroy.AddRange(CheckHorizontalMatches(selected.column, selected.row));
        itemsToDestroy.AddRange(CheckVerticalMatches(selected.column, selected.row));
        itemsToDestroy.AddRange(CheckHorizontalMatches(target.column, target.row));
        itemsToDestroy.AddRange(CheckVerticalMatches(target.column, target.row));

        itemsToDestroy = itemsToDestroy.Distinct().ToList();

        if (itemsToDestroy.Count > 0)
        {
            CollectEffectEntries(itemsToDestroy, 1);

            // phï¿½ h?y ï¿½ tru?c khi Destroy thï¿½ luu id vï¿½ spawn explosion
            foreach (GameObject it in itemsToDestroy)
            {
                if (it != null)
                {
                    Items comp = it.GetComponent<Items>();
                    if (comp != null)
                    {
                        AddDestroyedGemValue(comp);
                    }

                    // Spawn explosion effect t?i v? trï¿½ gem tru?c khi destroy
                    SpawnExplosionFx(it.transform.position);

                    itemList.Remove(it);
                    Destroy(it);
                }
            }

            yield return new WaitForEndOfFrame();

            // d?n vï¿½ refill
            yield return StartCoroutine(CollapseAndRefill());

            // sau khi d?n xong, x? lï¿½ chain-match (n?u cï¿½)
            yield return StartCoroutine(ResolveChainMatches(2));

            // Sau khi x? lï¿½ chain xong, g?i EndTurn vï¿½ truy?n destroyedIdsThisTurn d? UIManager hi?n th? trong EndTurn
            List<int> idsToSend = new List<int>(destroyedIdsThisTurn); // copy d? an toï¿½n
            List<GemEffectMatchEntry> effectsToSend = new List<GemEffectMatchEntry>(effectEntriesThisTurn);
            destroyedIdsThisTurn = null;
            effectEntriesThisTurn = null;
            OnSuccessfulPlayerSwapResolved?.Invoke();
            GameManager.Instance.EndTurn(idsToSend, effectsToSend);
        }
        else
        {
            // khï¿½ng cï¿½ match -> revert swap (tr? v? v? trï¿½ ban d?u)
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

            // Swap sai -> tr? v? v? trï¿½ cu vï¿½ ti?p t?c lu?t hi?n t?i
            if (GameManager.Instance != null)
                GameManager.Instance.ResumePlayerTurnAfterInvalidSwap();
        }
    }

    // ========== Collapse & Refill (IEnumerator: g?i vï¿½ d?i hoï¿½n t?t) ==========
    private IEnumerator CollapseAndRefill()
    {
        // Kho?ng cï¿½ch t?i da c?n roi (tï¿½nh d? ch? animation)
        float maxDistance = 0f;
        float minSpeed = float.MaxValue;

        // Duy?t t?ng c?t
        for (int col = 0; col < reelsArray.Length; col++)
        {
            Reels reel = reelsArray[col];
            if (reel == null) continue;

            // thu th?p item cï¿½n l?i trong reel (l?y t? transform.children d? trï¿½nh d? li?u cu)
            List<Items> remaining = new List<Items>();
            foreach (Transform child in reel.transform)
            {
                Items it = child.GetComponent<Items>();
                if (it != null) remaining.Add(it);
            }

            // sort theo row hi?n t?i (bottom -> top). N?u row khï¿½ng tin c?y, cï¿½ th? sort theo anchoredPosition.y
            remaining = remaining.OrderBy(x => x.row).ToList();

            // di chuy?n t?n t?i xu?ng t? row 0..n-1
            for (int r = 0; r < remaining.Count; r++)
            {
                Items it = remaining[r];
                RectTransform rect = it.GetComponent<RectTransform>();
                if (rect == null) continue;

                float currentY = rect.anchoredPosition.y;
                float targetY = reel.baseY + (r * reel.rowSpacing);

                if (Mathf.Abs(currentY - targetY) > 0.01f)
                {
                    // animation di chuy?n
                    StartCoroutine(MoveRectTo(rect, targetY, reel.fallSpeed));
                }

                // c?p nh?t ch? s? row
                it.row = r;

                // tï¿½nh distance/speed d? bi?t ph?i ch? bao lï¿½u
                maxDistance = Mathf.Max(maxDistance, Mathf.Abs(currentY - targetY));
                minSpeed = Mathf.Min(minSpeed, reel.fallSpeed);
            }

            // spawn thï¿½m n?u thi?u
            int missing = reel.itemCount - remaining.Count;
            for (int i = 0; i < missing; i++)
            {
                int targetRow = remaining.Count + i;
                reel.SpawnNewItemAtRow(targetRow, false); // spawn trï¿½n cao r?i roi
                // spawnOffset lï¿½ distance mï¿½ item m?i roi
                maxDistance = Mathf.Max(maxDistance, reel.spawnOffset);
                minSpeed = Mathf.Min(minSpeed, reel.fallSpeed);
            }
        }

        // n?u khï¿½ng cï¿½ item di chuy?n thï¿½ khï¿½ng c?n ch? lï¿½u
        float waitTime = 0.6f;
        if (minSpeed > 0f && maxDistance > 0f) waitTime = (maxDistance / minSpeed) + 0.05f;
        yield return new WaitForSeconds(waitTime);
    }

    // Coroutine di chuy?n rect t?i targetY v?i t?c d? speed (px/s)
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

    // ========== Chain resolution: tï¿½m t?t c? match trï¿½n b?ng, destroy vï¿½ refill l?p d?n khi h?t ==========
    private IEnumerator ResolveChainMatches(int startingComboIndex)
    {
        int comboIndex = Mathf.Max(1, startingComboIndex);

        while (true)
        {
            yield return new WaitForEndOfFrame();

            List<GameObject> allMatches = GetAllMatches();
            if (allMatches.Count == 0) break;

            CollectEffectEntries(allMatches, comboIndex);
            comboIndex++;

            foreach (GameObject it in allMatches)
            {
                if (it != null)
                {
                    // luu id tru?c khi destroy (n?u dang trong m?t l?n swap x? lï¿½)
                    Items comp = it.GetComponent<Items>();
                    if (comp != null)
                    {
                        if (destroyedIdsThisTurn == null) destroyedIdsThisTurn = new List<int>();
                        AddDestroyedGemValue(comp);
                    }

                    // Spawn explosion effect t?i v? trï¿½ gem tru?c khi destroy
                    SpawnExplosionFx(it.transform.position);

                    itemList.Remove(it);
                    Destroy(it);
                }
            }

            yield return new WaitForEndOfFrame();
            yield return StartCoroutine(CollapseAndRefill());
            EnsureBoardHasPotentialMove(true);
        }

        // khi hï¿½ng chu?i k?t thï¿½c, khï¿½ng g?i EndTurn ? dï¿½y (Board s? g?i EndTurn sau khi ResolveChainMatches tr? v?)
    }

    private void CollectEffectEntries(List<GameObject> matchedItems, int comboIndex)
    {
        if (matchedItems == null || matchedItems.Count == 0)
            return;

        if (effectEntriesThisTurn == null)
            effectEntriesThisTurn = new List<GemEffectMatchEntry>();

        Dictionary<int, int> counts = new Dictionary<int, int>();
        for (int i = 0; i < matchedItems.Count; i++)
        {
            GameObject item = matchedItems[i];
            if (item == null)
                continue;

            Items comp = item.GetComponent<Items>();
            if (comp == null)
                continue;

            int gemAmount = Mathf.Max(1, comp.gemValue);
            if (counts.ContainsKey(comp.itemId))
                counts[comp.itemId] += gemAmount;
            else
                counts[comp.itemId] = gemAmount;
        }

        foreach (var kv in counts)
        {
            int matchCount = Mathf.Max(3, kv.Value);
            effectEntriesThisTurn.Add(new GemEffectMatchEntry(kv.Key, matchCount, comboIndex));
        }
    }

    // L?y t?t c? match hi?n cï¿½ (scan toï¿½n b? grid)
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

    // ========== Ki?m tra match ngang / d?c (gi? nguyï¿½n logic cu) ==========
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

    public int CountItemsByTypes(int[] itemIds)
    {
        if (itemIds == null || itemIds.Length == 0 || reelsArray == null)
            return 0;

        HashSet<int> filter = new HashSet<int>(itemIds);
        int count = 0;

        for (int col = 0; col < reelsArray.Length; col++)
        {
            Reels reel = reelsArray[col];
            if (reel == null)
                continue;

            for (int row = 0; row < reel.itemCount; row++)
            {
                GameObject item = GetItemAt(col, row);
                if (item == null)
                    continue;

                Items comp = item.GetComponent<Items>();
                if (comp != null && filter.Contains(comp.itemId))
                    count++;
            }
        }

        return count;
    }


    public async UniTask<System.Collections.Generic.List<int>> ClearItemsByTypesAndRefillAnimatedAsync(int[] itemIds)
    {
        if (!CanRunBoardCoroutine())
            return new System.Collections.Generic.List<int>();

        if (itemIds == null || itemIds.Length == 0 || reelsArray == null || reelsArray.Length == 0)
            return new System.Collections.Generic.List<int>();

        var removed = ClearItemsByTypesAnimated(itemIds);
        if (removed.Count <= 0)
            return removed;

        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        if (!CanRunBoardCoroutine())
            return removed;

        await RunCoroutineAsync(CollapseAndRefill());
        await ResolveExistingMatchesAsync();
        return removed;
    }
    public int ClearRandomRowAndRefillImmediate()
    {
        if (reelsArray == null || reelsArray.Length == 0)
            return 0;

        int maxRows = reelsArray[0] != null ? reelsArray[0].itemCount : 0;
        if (maxRows <= 0)
            return 0;

        int row = Random.Range(0, maxRows);
        return ClearRowAndRefillImmediate(row);
    }

    // Clear a random row and return the list of cleared item IDs (for reporting/visualization)
    public System.Collections.Generic.List<int> ClearRandomRowAndRefillWithReport()
    {
        if (reelsArray == null || reelsArray.Length == 0)
            return new System.Collections.Generic.List<int>();

        int maxRows = reelsArray[0] != null ? reelsArray[0].itemCount : 0;
        if (maxRows <= 0)
            return new System.Collections.Generic.List<int>();

        int row = Random.Range(0, maxRows);
        return ClearRowAndRefillWithReport(row);
    }

    public int ClearRandomColumnAndRefillImmediate()
    {
        if (reelsArray == null || reelsArray.Length == 0)
            return 0;

        int col = Random.Range(0, reelsArray.Length);
        return ClearColumnAndRefillImmediate(col);
    }

    // Clear a random column and return the list of cleared item IDs (for reporting/visualization)
    public System.Collections.Generic.List<int> ClearRandomColumnAndRefillWithReport()
    {
        if (reelsArray == null || reelsArray.Length == 0)
            return new System.Collections.Generic.List<int>();

        int col = Random.Range(0, reelsArray.Length);
        return ClearColumnAndRefillWithReport(col);
    }

    // Animated versions: destroy the items in the row/column, then run CollapseAndRefill animation,
    // and return the list of removed item IDs. These can be awaited by callers.
    public async UniTask<System.Collections.Generic.List<int>> ClearRandomRowAndRefillAnimatedAsync()
    {
        if (!CanRunBoardCoroutine())
            return new System.Collections.Generic.List<int>();

        if (reelsArray == null || reelsArray.Length == 0)
            return new System.Collections.Generic.List<int>();

        int maxRows = reelsArray[0] != null ? reelsArray[0].itemCount : 0;
        if (maxRows <= 0)
            return new System.Collections.Generic.List<int>();

        int row = Random.Range(0, maxRows);
        var removed = ClearRowAndRefillAnimated(row);
        // Ensure destroyed GameObjects are processed by Unity before collapse/refill
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        if (!CanRunBoardCoroutine())
            return removed;
        await RunCoroutineAsync(CollapseAndRefill());
        await ResolveExistingMatchesAsync();
        return removed;
    }

    public async UniTask<System.Collections.Generic.List<int>> ClearRandomColumnAndRefillAnimatedAsync()
    {
        if (!CanRunBoardCoroutine())
            return new System.Collections.Generic.List<int>();

        if (reelsArray == null || reelsArray.Length == 0)
            return new System.Collections.Generic.List<int>();

        int col = Random.Range(0, reelsArray.Length);
        var removed = ClearColumnAndRefillAnimated(col);
        // Ensure destroyed GameObjects are processed by Unity before collapse/refill
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        if (!CanRunBoardCoroutine())
            return removed;
        await RunCoroutineAsync(CollapseAndRefill());
        await ResolveExistingMatchesAsync();
        return removed;
    }

    // Helper to run a coroutine and await its completion
    private UniTask RunCoroutineAsync(IEnumerator routine)
    {
        if (!CanRunBoardCoroutine() || routine == null)
            return UniTask.CompletedTask;

        var tcs = new UniTaskCompletionSource();
        StartCoroutine(RunCoroutineInternal(routine, tcs));
        return tcs.Task;
    }

    private bool CanRunBoardCoroutine()
    {
        return this != null && gameObject != null && gameObject.activeInHierarchy && isActiveAndEnabled;
    }

    private async UniTask ResolveExistingMatchesAsync()
    {
        if (!CanRunBoardCoroutine())
            return;

        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        if (!CanRunBoardCoroutine())
            return;

        List<GameObject> matches = GetAllMatches();
        if (matches.Count <= 0)
        {
            EnsureBoardHasPotentialMove(true);
            return;
        }

        bool createdDestroyedList = destroyedIdsThisTurn == null;
        bool createdEffectList = effectEntriesThisTurn == null;

        if (createdDestroyedList)
            destroyedIdsThisTurn = new List<int>();
        if (createdEffectList)
            effectEntriesThisTurn = new List<GemEffectMatchEntry>();

        CollectEffectEntries(matches, 1);
        DestroyMatchedItems(matches);
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        if (!CanRunBoardCoroutine())
            return;

        await RunCoroutineAsync(CollapseAndRefill());
        await RunCoroutineAsync(ResolveChainMatches(2));
        EnsureBoardHasPotentialMove(true);

        if (createdDestroyedList)
            destroyedIdsThisTurn = null;
        if (createdEffectList)
            effectEntriesThisTurn = null;
    }

    private IEnumerator RunCoroutineInternal(IEnumerator routine, UniTaskCompletionSource tcs)
    {
        if (!CanRunBoardCoroutine() || routine == null)
        {
            tcs.TrySetResult();
            yield break;
        }

        yield return StartCoroutine(routine);
        tcs.TrySetResult();
    }


    private System.Collections.Generic.List<int> ClearItemsByTypesAnimated(int[] itemIds)
    {
        var removed = new System.Collections.Generic.List<int>();
        if (itemIds == null || itemIds.Length == 0 || reelsArray == null)
            return removed;

        HashSet<int> filter = new HashSet<int>(itemIds);
        List<GameObject> itemsToDestroy = new List<GameObject>();

        for (int col = 0; col < reelsArray.Length; col++)
        {
            Reels reel = reelsArray[col];
            if (reel == null)
                continue;

            for (int row = 0; row < reel.itemCount; row++)
            {
                GameObject item = GetItemAt(col, row);
                if (item == null)
                    continue;

                Items comp = item.GetComponent<Items>();
                if (comp == null || !filter.Contains(comp.itemId))
                    continue;

                AddRemovedGemValues(removed, comp);
                itemsToDestroy.Add(item);
            }
        }

        for (int i = 0; i < itemsToDestroy.Count; i++)
        {
            GameObject item = itemsToDestroy[i];
            if (item == null)
                continue;

            SpawnExplosionFx(item.transform.position);
            itemList.Remove(item);
            Destroy(item);
        }

        return removed;
    }
    private System.Collections.Generic.List<int> ClearRowAndRefillAnimated(int row)
    {
        var removed = new System.Collections.Generic.List<int>();
        for (int col = 0; col < reelsArray.Length; col++)
        {
            GameObject item = GetItemAt(col, row);
            if (item == null) continue;
            Items comp = item.GetComponent<Items>();
            if (comp == null) continue;

            removed.Add(comp.itemId);
            itemList.Remove(item);
            Destroy(item);
        }

        return removed;
    }

    private System.Collections.Generic.List<int> ClearColumnAndRefillAnimated(int col)
    {
        var removed = new System.Collections.Generic.List<int>();
        if (col < 0 || col >= reelsArray.Length) return removed;

        Reels reel = reelsArray[col];
        if (reel == null) return removed;

        for (int row = 0; row < reel.itemCount; row++)
        {
            GameObject item = GetItemAt(col, row);
            if (item == null) continue;
            Items comp = item.GetComponent<Items>();
            if (comp == null) continue;

            removed.Add(comp.itemId);
            itemList.Remove(item);
            Destroy(item);
        }

        return removed;
    }

    public bool AutoSwapRandomAdjacent()
    {
        SwapCandidate candidate = FindBestSwapCandidate(null);
        if (!candidate.IsValid)
            return false;

        SwapItemData(candidate.First, candidate.Second);
        return true;
    }

    public async UniTask<System.Collections.Generic.List<int>> AutoSwapPreferredAdjacentAndResolveAsync(int[] preferredItemIds)
    {
        if (!CanRunBoardCoroutine() || reelsArray == null || reelsArray.Length == 0)
            return new System.Collections.Generic.List<int>();

        List<SwapCandidate> candidates = FindPreferredSwapCandidates(preferredItemIds);
        if (candidates == null || candidates.Count == 0)
        {
            SwapCandidate fallback = FindBestSwapCandidate(null);
            if (!fallback.IsValid)
                return new System.Collections.Generic.List<int>();
            candidates = new List<SwapCandidate> { fallback };
        }

        destroyedIdsThisTurn = new List<int>();
        effectEntriesThisTurn = new List<GemEffectMatchEntry>();

        for (int i = 0; i < candidates.Count; i++)
            SwapItemData(candidates[i].First, candidates[i].Second);

        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

        List<GameObject> matches = GetAllMatches();
        if (matches.Count <= 0)
        {
            destroyedIdsThisTurn = null;
            effectEntriesThisTurn = null;
            return new System.Collections.Generic.List<int>();
        }

        await ResolveExistingMatchesAsync();

        List<int> removed = destroyedIdsThisTurn != null ? new List<int>(destroyedIdsThisTurn) : new System.Collections.Generic.List<int>();
        destroyedIdsThisTurn = null;
        effectEntriesThisTurn = null;
        return removed;
    }


    private struct SwapCandidate
    {
        public Items First;
        public Items Second;
        public int PreferredMatchCount;
        public int TotalMatchCount;
        public bool IsValid => First != null && Second != null && TotalMatchCount > 0;
    }

    private SwapCandidate FindBestSwapCandidate(int[] preferredItemIds)
    {
        SwapCandidate best = default;
        HashSet<int> preferred = preferredItemIds != null && preferredItemIds.Length > 0 ? new HashSet<int>(preferredItemIds) : null;

        for (int col = 0; col < reelsArray.Length; col++)
        {
            Reels reel = reelsArray[col];
            if (reel == null)
                continue;

            for (int row = 0; row < reel.itemCount; row++)
            {
                EvaluateSwapCandidate(col, row, col + 1, row, preferred, ref best);
                EvaluateSwapCandidate(col, row, col, row + 1, preferred, ref best);
            }
        }

        return best;
    }

    private void EvaluateSwapCandidate(int colA, int rowA, int colB, int rowB, HashSet<int> preferred, ref SwapCandidate best)
    {
        GameObject aObj = GetItemAt(colA, rowA);
        GameObject bObj = GetItemAt(colB, rowB);
        if (aObj == null || bObj == null)
            return;

        Items a = aObj.GetComponent<Items>();
        Items b = bObj.GetComponent<Items>();
        if (a == null || b == null)
            return;

        SwapItemData(a, b);
        List<GameObject> matches = GetMatchesForSwap(a, b);
        int totalMatchCount = matches.Count;
        int preferredMatchCount = CountPreferredMatches(matches, preferred);
        SwapItemData(a, b);

        if (totalMatchCount <= 0)
            return;

        bool better = !best.IsValid || preferredMatchCount > best.PreferredMatchCount ||
                      (preferredMatchCount == best.PreferredMatchCount && totalMatchCount > best.TotalMatchCount);
        if (!better)
            return;

        best = new SwapCandidate
        {
            First = a,
            Second = b,
            PreferredMatchCount = preferredMatchCount,
            TotalMatchCount = totalMatchCount
        };
    }


    private List<SwapCandidate> FindPreferredSwapCandidates(int[] preferredItemIds)
    {
        List<SwapCandidate> candidates = CollectSwapCandidates(preferredItemIds, true);
        if (candidates.Count <= 0)
            return candidates;

        candidates.Sort((a, b) =>
        {
            int preferredCompare = b.PreferredMatchCount.CompareTo(a.PreferredMatchCount);
            if (preferredCompare != 0)
                return preferredCompare;

            return b.TotalMatchCount.CompareTo(a.TotalMatchCount);
        });

        List<SwapCandidate> selected = new List<SwapCandidate>();
        HashSet<Items> used = new HashSet<Items>();
        for (int i = 0; i < candidates.Count; i++)
        {
            SwapCandidate candidate = candidates[i];
            if (!candidate.IsValid)
                continue;

            if (used.Contains(candidate.First) || used.Contains(candidate.Second))
                continue;

            selected.Add(candidate);
            used.Add(candidate.First);
            used.Add(candidate.Second);
        }

        return selected;
    }

    private List<SwapCandidate> CollectSwapCandidates(int[] preferredItemIds, bool requirePreferred)
    {
        List<SwapCandidate> result = new List<SwapCandidate>();
        HashSet<int> preferred = preferredItemIds != null && preferredItemIds.Length > 0 ? new HashSet<int>(preferredItemIds) : null;

        for (int col = 0; col < reelsArray.Length; col++)
        {
            Reels reel = reelsArray[col];
            if (reel == null)
                continue;

            for (int row = 0; row < reel.itemCount; row++)
            {
                AddSwapCandidate(col, row, col + 1, row, preferred, requirePreferred, result);
                AddSwapCandidate(col, row, col, row + 1, preferred, requirePreferred, result);
            }
        }

        return result;
    }

    private void AddSwapCandidate(int colA, int rowA, int colB, int rowB, HashSet<int> preferred, bool requirePreferred, List<SwapCandidate> result)
    {
        GameObject aObj = GetItemAt(colA, rowA);
        GameObject bObj = GetItemAt(colB, rowB);
        if (aObj == null || bObj == null)
            return;

        Items a = aObj.GetComponent<Items>();
        Items b = bObj.GetComponent<Items>();
        if (a == null || b == null)
            return;

        SwapItemData(a, b);
        List<GameObject> matches = GetMatchesForSwap(a, b);
        int totalMatchCount = CountMatchedGemValues(matches);
        int preferredMatchCount = CountPreferredMatches(matches, preferred);
        SwapItemData(a, b);

        if (totalMatchCount <= 0)
            return;

        if (requirePreferred && preferredMatchCount <= 0)
            return;

        result.Add(new SwapCandidate
        {
            First = a,
            Second = b,
            PreferredMatchCount = preferredMatchCount,
            TotalMatchCount = totalMatchCount
        });
    }

    private int CountMatchedGemValues(List<GameObject> matches)
    {
        if (matches == null || matches.Count == 0)
            return 0;

        int count = 0;
        for (int i = 0; i < matches.Count; i++)
        {
            Items comp = matches[i] != null ? matches[i].GetComponent<Items>() : null;
            if (comp != null)
                count += Mathf.Max(1, comp.gemValue);
        }

        return count;
    }
    private int CountPreferredMatches(List<GameObject> matches, HashSet<int> preferred)
    {
        if (matches == null || matches.Count == 0 || preferred == null || preferred.Count == 0)
            return 0;

        int count = 0;
        for (int i = 0; i < matches.Count; i++)
        {
            Items comp = matches[i] != null ? matches[i].GetComponent<Items>() : null;
            if (comp != null && preferred.Contains(comp.itemId))
                count += Mathf.Max(1, comp.gemValue);
        }

        return count;
    }

    private List<GameObject> GetMatchesForSwap(Items first, Items second)
    {
        List<GameObject> matches = new List<GameObject>();
        if (first == null || second == null)
            return matches;

        matches.AddRange(CheckHorizontalMatches(first.column, first.row));
        matches.AddRange(CheckVerticalMatches(first.column, first.row));
        matches.AddRange(CheckHorizontalMatches(second.column, second.row));
        matches.AddRange(CheckVerticalMatches(second.column, second.row));
        return matches.Distinct().ToList();
    }

    private void DestroyMatchedItems(List<GameObject> itemsToDestroy)
    {
        if (itemsToDestroy == null)
            return;

        for (int i = 0; i < itemsToDestroy.Count; i++)
        {
            GameObject it = itemsToDestroy[i];
            if (it == null)
                continue;

            Items comp = it.GetComponent<Items>();
            if (comp != null)
                AddDestroyedGemValue(comp);

            SpawnExplosionFx(it.transform.position);
            itemList.Remove(it);
            Destroy(it);
        }
    }
    private int ClearRowAndRefillImmediate(int row)
    {
        int cleared = 0;
        for (int col = 0; col < reelsArray.Length; col++)
        {
            GameObject item = GetItemAt(col, row);
            if (item == null)
                continue;

            Items comp = item.GetComponent<Items>();
            if (comp == null)
                continue;

            AssignRandomIdAvoidMatch(comp);
            cleared++;
        }

        return cleared;
    }

    private System.Collections.Generic.List<int> ClearRowAndRefillWithReport(int row)
    {
        var removed = new System.Collections.Generic.List<int>();
        for (int col = 0; col < reelsArray.Length; col++)
        {
            GameObject item = GetItemAt(col, row);
            if (item == null)
                continue;

            Items comp = item.GetComponent<Items>();
            if (comp == null)
                continue;

            removed.Add(comp.itemId);
            AssignRandomIdAvoidMatch(comp);
        }

        return removed;
    }

    private int ClearColumnAndRefillImmediate(int col)
    {
        if (col < 0 || col >= reelsArray.Length)
            return 0;

        Reels reel = reelsArray[col];
        if (reel == null)
            return 0;

        int cleared = 0;
        for (int row = 0; row < reel.itemCount; row++)
        {
            GameObject item = GetItemAt(col, row);
            if (item == null)
                continue;

            Items comp = item.GetComponent<Items>();
            if (comp == null)
                continue;

            AssignRandomIdAvoidMatch(comp);
            cleared++;
        }

        return cleared;
    }

    private System.Collections.Generic.List<int> ClearColumnAndRefillWithReport(int col)
    {
        var removed = new System.Collections.Generic.List<int>();
        if (col < 0 || col >= reelsArray.Length)
            return removed;

        Reels reel = reelsArray[col];
        if (reel == null) return removed;

        for (int row = 0; row < reel.itemCount; row++)
        {
            GameObject item = GetItemAt(col, row);
            if (item == null) continue;
            Items comp = item.GetComponent<Items>();
            if (comp == null) continue;

            removed.Add(comp.itemId);
            AssignRandomIdAvoidMatch(comp);
        }

        return removed;
    }

    private static void SwapItemData(Items a, Items b)
    {
        int tempId = a.itemId;
        int tempGemValue = a.gemValue;
        Sprite tempSprite = null;

        Image aImage = a.GetComponent<Image>();
        Image bImage = b.GetComponent<Image>();
        if (aImage != null)
            tempSprite = aImage.sprite;

        a.itemId = b.itemId;
        a.SetGemValue(b.gemValue);
        b.itemId = tempId;
        b.SetGemValue(tempGemValue);

        if (aImage != null && bImage != null)
        {
            aImage.sprite = bImage.sprite;
            bImage.sprite = tempSprite;
        }
    }

    // L?y item theo col,row (dï¿½ng itemList do Reels dang kï¿½)
    public bool TryGetSuggestedSwap(out Items first, out Items second)
    {
        SwapCandidate candidate = FindBestSwapCandidate(null);
        first = candidate.First;
        second = candidate.Second;
        return candidate.IsValid;
    }
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














