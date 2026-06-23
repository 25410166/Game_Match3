using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectUIController : MonoBehaviour
{
    [System.Serializable]
    public class EffectIconDefinition
    {
        public StatusEffectType type;
        public Sprite icon;
    }

    [Header("References")]
    [SerializeField] private StatusEffectIconItem itemPrefab;
    [SerializeField] private RectTransform playerRoot;
    [SerializeField] private RectTransform aiRoot;

    [Header("Icons")]
    [SerializeField] private EffectIconDefinition[] iconDefinitions;

    private readonly Dictionary<StatusEffectType, EffectIconDefinition> iconLookup = new Dictionary<StatusEffectType, EffectIconDefinition>();
    private readonly Dictionary<StatusEffectType, StatusEffectIconItem> playerItems = new Dictionary<StatusEffectType, StatusEffectIconItem>();
    private readonly Dictionary<StatusEffectType, StatusEffectIconItem> aiItems = new Dictionary<StatusEffectType, StatusEffectIconItem>();

    private PlayerStats player;
    private AIStats ai;
    private Coroutine bindCoroutine;

    private void Awake()
    {
        BuildIconLookup();

        if (playerRoot == null)
            playerRoot = GetComponent<RectTransform>();
        if (aiRoot == null)
            aiRoot = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (bindCoroutine != null)
            StopCoroutine(bindCoroutine);

        bindCoroutine = StartCoroutine(BindWhenReady());
    }

    private void OnDisable()
    {
        if (bindCoroutine != null)
            StopCoroutine(bindCoroutine);

        bindCoroutine = null;
        Unbind();
    }

    private IEnumerator BindWhenReady()
    {
        while (GameManager.Instance == null
            || (GameManager.Instance.player == null && GameManager.Instance.ai == null))
            yield return null;

        Bind(GameManager.Instance.player, GameManager.Instance.ai);
    }

    private void BuildIconLookup()
    {
        iconLookup.Clear();
        if (iconDefinitions == null)
            return;

        for (int i = 0; i < iconDefinitions.Length; i++)
        {
            EffectIconDefinition def = iconDefinitions[i];
            if (def == null)
                continue;

            iconLookup[def.type] = def;
        }
    }

    private void Bind(PlayerStats playerStats, AIStats aiStats)
    {
        Unbind();
        player = playerStats;
        ai = aiStats;

        if (player != null)
        {
            player.OnStatusEffectUpdated += OnPlayerEffectUpdated;
            player.OnStatusEffectRemoved += OnPlayerEffectRemoved;
            player.OnStatusEffectsReset += OnPlayerEffectsReset;
        }

        if (ai != null)
        {
            ai.OnStatusEffectUpdated += OnAIEffectUpdated;
            ai.OnStatusEffectRemoved += OnAIEffectRemoved;
            ai.OnStatusEffectsReset += OnAIEffectsReset;
        }

        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshBucket(player, playerItems, playerRoot);
        RefreshBucket(ai, aiItems, aiRoot);
    }

    private void RefreshBucket(PlayerStats stats, Dictionary<StatusEffectType, StatusEffectIconItem> bucket, RectTransform root)
    {
        ClearEffects(bucket);
        if (stats == null || root == null)
            return;

        List<StatusEffectEntry> snapshot = stats.GetStatusEffectsSnapshot();
        for (int i = 0; i < snapshot.Count; i++)
        {
            StatusEffectEntry entry = snapshot[i];
            if (entry == null)
                continue;

            UpdateEffect(bucket, root, entry.type, entry.remainingTurns);
        }
    }

    private void RefreshBucket(AIStats stats, Dictionary<StatusEffectType, StatusEffectIconItem> bucket, RectTransform root)
    {
        ClearEffects(bucket);
        if (stats == null || root == null)
            return;

        List<StatusEffectEntry> snapshot = stats.GetStatusEffectsSnapshot();
        for (int i = 0; i < snapshot.Count; i++)
        {
            StatusEffectEntry entry = snapshot[i];
            if (entry == null)
                continue;

            UpdateEffect(bucket, root, entry.type, entry.remainingTurns);
        }
    }

    private void Unbind()
    {
        if (player != null)
        {
            player.OnStatusEffectUpdated -= OnPlayerEffectUpdated;
            player.OnStatusEffectRemoved -= OnPlayerEffectRemoved;
            player.OnStatusEffectsReset -= OnPlayerEffectsReset;
        }

        if (ai != null)
        {
            ai.OnStatusEffectUpdated -= OnAIEffectUpdated;
            ai.OnStatusEffectRemoved -= OnAIEffectRemoved;
            ai.OnStatusEffectsReset -= OnAIEffectsReset;
        }

        player = null;
        ai = null;
    }

    private void OnPlayerEffectUpdated(StatusEffectType type, int remainingTurns)
    {
        UpdateEffect(playerItems, playerRoot, type, remainingTurns);
    }

    private void OnPlayerEffectRemoved(StatusEffectType type)
    {
        RemoveEffect(playerItems, type);
    }

    private void OnPlayerEffectsReset()
    {
        ClearEffects(playerItems);
    }

    private void OnAIEffectUpdated(StatusEffectType type, int remainingTurns)
    {
        UpdateEffect(aiItems, aiRoot, type, remainingTurns);
    }

    private void OnAIEffectRemoved(StatusEffectType type)
    {
        RemoveEffect(aiItems, type);
    }

    private void OnAIEffectsReset()
    {
        ClearEffects(aiItems);
    }

    private void UpdateEffect(Dictionary<StatusEffectType, StatusEffectIconItem> bucket, RectTransform root, StatusEffectType type, int remainingTurns)
    {
        if (remainingTurns <= 0)
        {
            Debug.Log($"[StatusEffectUI] Skip update, remainingTurns<=0 | type={type}");
            RemoveEffect(bucket, type);
            return;
        }

        if (itemPrefab == null || root == null)
        {
            Debug.LogWarning($"[StatusEffectUI] Missing itemPrefab/root | type={type} | itemPrefab={(itemPrefab != null)} | root={(root != null)}");
            return;
        }

        if (!iconLookup.TryGetValue(type, out EffectIconDefinition def) || def == null || def.icon == null)
        {
            Debug.LogWarning($"[StatusEffectUI] Missing icon definition | type={type}");
            return;
        }

        if (!bucket.TryGetValue(type, out StatusEffectIconItem item) || item == null)
        {
            item = Instantiate(itemPrefab, root);
            bucket[type] = item;
            Debug.Log($"[StatusEffectUI] Spawned icon | type={type} | parent={root.name}");
        }

        item.Setup(def.icon, remainingTurns);
    }

    private void RemoveEffect(Dictionary<StatusEffectType, StatusEffectIconItem> bucket, StatusEffectType type)
    {
        if (!bucket.TryGetValue(type, out StatusEffectIconItem item) || item == null)
            return;

        Destroy(item.gameObject);
        bucket.Remove(type);
        Debug.Log($"[StatusEffectUI] Removed icon | type={type}");
    }

    private void ClearEffects(Dictionary<StatusEffectType, StatusEffectIconItem> bucket)
    {
        if (bucket.Count == 0)
            return;

        List<StatusEffectType> keys = new List<StatusEffectType>(bucket.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            StatusEffectType type = keys[i];
            if (bucket.TryGetValue(type, out StatusEffectIconItem item) && item != null)
                Destroy(item.gameObject);
        }

        bucket.Clear();
        Debug.Log("[StatusEffectUI] Cleared all icons");
    }
}
