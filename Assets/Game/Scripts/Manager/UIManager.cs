using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Header Item Settings")]
    public RectTransform headerItemParent;
    public GameObject headerItemPrefab;
    public float iconSpacing = 180f;
    public float fadeDuration = 0.5f;
    public float delayBetween = 1f;
    public Vector2 iconSize = new Vector2(100f, 100f);

    [Header("Turn Timer Settings")]
    public Sprite[] imgTime;
    public GameObject time;

    private Coroutine timerCoroutine;

    [Header("Player Effect")]
    public GameObject _effectHealthPl;
    public GameObject _effectManaPl;
    public GameObject _effectRagePl;
    public GameObject _effectShieldPl;
    public GameObject _txtNameAI;

    [Header("AI Effect")]
    public GameObject _effectHealthAI;
    public GameObject _effectManaAI;
    public GameObject _effectRageAI;
    public GameObject _effectShieldAI;
    public GameObject _txtNamePlayer;

    private void Awake()
    {
        Instance = this;
    }

    public IEnumerator PlayDestroyedItemsSequence(List<int> destroyedIds)
    {
        if (destroyedIds == null || destroyedIds.Count == 0)
        {
            if (Board.Instance != null)
                Board.Instance.gameObject.SetActive(true);
            yield break;
        }

        if (Board.Instance != null)
            Board.Instance.gameObject.SetActive(false);

        Dictionary<int, int> counts = new Dictionary<int, int>();
        foreach (int id in destroyedIds)
        {
            if (counts.ContainsKey(id)) counts[id]++;
            else counts[id] = 1;
        }

        List<int> keys = counts.Keys.OrderBy(k => k).ToList();

        if (headerItemParent != null)
        {
            foreach (Transform t in headerItemParent) Destroy(t.gameObject);
        }

        List<GameObject> icons = new List<GameObject>();
        int countUnique = keys.Count;
        float totalWidth = (countUnique - 1) * iconSpacing;
        float startX = -totalWidth / 2f;

        var spriteMap = Board.Instance?.SpriteResource;

        for (int i = 0; i < countUnique; i++)
        {
            int id = keys[i];
            int qty = counts[id];

            GameObject icon = Instantiate(headerItemPrefab, headerItemParent);
            icon.SetActive(true);

            RectTransform rect = icon.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(startX + i * iconSpacing, 0f);
                rect.sizeDelta = iconSize;
            }

            Image img = icon.GetComponent<Image>();
            if (img != null && spriteMap != null && spriteMap.spriteItems != null)
            {
                var match = spriteMap.spriteItems.FirstOrDefault(s => s.id == id);
                if (match != null && match.sprite != null)
                {
                    img.sprite = match.sprite;
                    RectTransform imgRect = img.GetComponent<RectTransform>();
                    if (imgRect != null) imgRect.sizeDelta = iconSize;
                }
            }

            GameObject textObj = new GameObject("CountText", typeof(RectTransform));
            textObj.transform.SetParent(icon.transform, false);

            RectTransform txtRect = textObj.GetComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0.5f, 0f);
            txtRect.anchorMax = new Vector2(0.5f, 0f);
            txtRect.pivot = new Vector2(0.5f, 1f);
            txtRect.anchoredPosition = new Vector2(0f, -20f);
            txtRect.sizeDelta = new Vector2(100f, 40f);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "x" + qty.ToString();
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            CanvasGroup cg = icon.GetComponent<CanvasGroup>();
            if (cg == null) cg = icon.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            icons.Add(icon);
        }

        yield return null;

        for (int i = 0; i < icons.Count; i++)
        {
            yield return new WaitForSeconds(delayBetween);

            GameObject icon = icons[i];
            if (icon == null) continue;

            CanvasGroup cg = icon.GetComponent<CanvasGroup>();
            if (cg == null) cg = icon.AddComponent<CanvasGroup>();

            // Kích hoạt hiệu ứng ngay trước khi fade và cập nhật UI
            int id = keys[i];
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.currentTurn == GameManager.Turn.Player && GameManager.Instance.player != null)
                {
                    switch (id)
                    {
                        case 0:
                            if (_effectShieldPl != null) _effectShieldPl.SetActive(true);
                            //GameManager.Instance.player.GainRage(10); // Cập nhật Rage
                            break;
                        case 1: // Tăng nộ
                            if (_effectRagePl != null) _effectRagePl.SetActive(true);
                            GameManager.Instance.player.GainRage(10); // Cập nhật Rage
                            break;
                        case 2: // Hồi máu
                            if (_effectHealthPl != null) _effectHealthPl.SetActive(true);
                            GameManager.Instance.player.Heal(10); // Cập nhật Health
                            break;
                        case 3: // Tăng mana
                            if (_effectManaPl != null) _effectManaPl.SetActive(true);
                            GameManager.Instance.player.GainMana(10); // Cập nhật Mana
                            break;
                    }
                }
                else if (GameManager.Instance.currentTurn == GameManager.Turn.AI && GameManager.Instance.ai != null)
                {
                    switch (id)
                    {
                        case 0:
                            if (_effectShieldAI != null) _effectShieldAI.SetActive(true);
                            //GameManager.Instance.ai.GainRage(10); // Cập nhật Rag
                            Debug.Log(" AI Rage");
                            break;
                        case 1: // Tăng nộ
                            if (_effectRageAI != null) _effectRageAI.SetActive(true);
                            GameManager.Instance.ai.GainRage(10); // Cập nhật Rag
                            Debug.Log(" AI Rage");

                            break;
                        case 2: 
                            if (_effectHealthAI != null) _effectHealthAI.SetActive(true);
                            GameManager.Instance.ai.Heal(10);
                            Debug.Log("AI Health");
                            break;
                        case 3: 
                            if (_effectManaAI != null) _effectManaAI.SetActive(true);
                            GameManager.Instance.ai.GainMana(10);
                            Debug.Log("AI Mana");
                            break;
                    }
                }
            }

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
                yield return null;
            }

            // Tắt hiệu ứng khi item biến mất
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.currentTurn == GameManager.Turn.Player)
                {
                    if (_effectShieldPl != null) _effectShieldPl.SetActive(false);
                    if (_effectRagePl != null) _effectRagePl.SetActive(false);
                    if (_effectHealthPl != null) _effectHealthPl.SetActive(false);
                    if (_effectManaPl != null) _effectManaPl.SetActive(false);
                }
                else if (GameManager.Instance.currentTurn == GameManager.Turn.AI)
                {
                    if (_effectShieldAI != null) _effectShieldAI.SetActive(false);
                    if (_effectRageAI != null) _effectRageAI.SetActive(false);
                    if (_effectHealthAI != null) _effectHealthAI.SetActive(false);
                    if (_effectManaAI != null) _effectManaAI.SetActive(false);
                }
            }

            Destroy(icon);
        }

        foreach (Transform t in headerItemParent)
        {
            Destroy(t.gameObject);
        }

        if (Board.Instance != null)
            Board.Instance.gameObject.SetActive(true);

        yield break;
    }

    public IEnumerator ShowTurnTimer(float duration, GameManager.Turn currentTurn)
    {
        if (time == null || imgTime == null || imgTime.Length < 10)
        {
            Debug.LogWarning("Chưa gán time GameObject hoặc imgTime không đủ 10 sprite!");
            yield return new WaitForSeconds(duration);
            yield break;
        }

        Image timeImage = time.GetComponent<Image>();
        if (timeImage == null)
        {
            Debug.LogWarning("GameObject time không có Image component!");
            yield return new WaitForSeconds(duration);
            yield break;
        }

        timerCoroutine = StartCoroutine(TimerDisplay(duration, timeImage));
        yield return timerCoroutine;
    }

    private IEnumerator TimerDisplay(float duration, Image timeImage)
    {
        time.SetActive(true);
        float timeLeft = duration;

        // Giả sử imgTime[0] là sprite số 9, imgTime[1] là số 8, ..., imgTime[9] là số 0
        while (timeLeft > 0)
        {
            int spriteIndex = Mathf.Clamp(Mathf.CeilToInt(timeLeft) - 1, 0, 9);
            timeImage.sprite = imgTime[spriteIndex];
            timeLeft -= Time.deltaTime;
            yield return null;
        }

        time.SetActive(false);
        timerCoroutine = null;
    }

    public void StopTurnTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        if (time != null)
        {
            time.SetActive(false);
        }
    }
}