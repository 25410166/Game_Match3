using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;


public class ShopManager : MonoBehaviour
{
    private const string NotEnoughGoldKey = "shop_not_enough_gold";
    private const string PurchaseSuccessKey = "shop_purchase_success";

    private enum ShopTab
    {
        Card,
        Gem
    }

    [Serializable]
    private class ShopEntry
    {
        public bool isCard;
        public int id;
        public int level;
        public int elementId;
        public string itemName;
        public string description;
        public Sprite sprite;
        public int priceGold;
    }

    [Header("Home -> Popup")]
    [SerializeField] private Button btnShopOnHome;
    [SerializeField] private GameObject popupShop;
    [SerializeField] private Button btnCloseShop;

    [Header("Tab Buttons")]
    [SerializeField] private Button btnTabCard;
    [SerializeField] private Button btnTabGem;
    [SerializeField] private Color tabNormalColor = Color.white;
    [SerializeField] private Color tabSelectedColor = Color.green;
    [SerializeField] private float tabSelectedScale = 1.08f;

    [Header("List")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform itemRoot;
    [SerializeField] private ShopItemUI itemShopPrefab;

    [Header("Resource Data Name")]
    [SerializeField] private string cardResourceName = "CardDatabase";
    [SerializeField] private string gemResourceName = "GemCollection";

    [Header("Direct Data Reference (Recommended)")]
    [SerializeField] private UnityEngine.Object cardDatabaseAsset;
    [SerializeField] private UnityEngine.Object gemCollectionAsset;

    [Header("Not Enough Gold")]
    [SerializeField] private TextMeshProUGUI txtNotEnoughGold;

    [Header("Card Info Popup")]
    [SerializeField] private GameObject popupCardInfo;
    [SerializeField] private TextMeshProUGUI txtCardInfoDescription;
    [SerializeField] private RectTransform popupCardInfoRect;

    private UnityEngine.Object cardDatabase;
    private UnityEngine.Object gemCollection;
    private readonly List<ShopItemUI> spawnedItems = new List<ShopItemUI>();

    private Vector3 notEnoughGoldStartLocalPos;

    private void Start()
    {
        CacheDataResources();
        BindButtons();
        EnsureCardInfoReferences();

        if (popupShop != null) popupShop.SetActive(false);
        if (popupCardInfo != null) popupCardInfo.SetActive(false);

        if (txtNotEnoughGold != null)
        {
            txtNotEnoughGold.gameObject.SetActive(false);
            RectTransform rt = txtNotEnoughGold.GetComponent<RectTransform>();
            if (rt != null) notEnoughGoldStartLocalPos = rt.localPosition;
        }

        if (scrollRect != null && itemRoot == null)
            itemRoot = scrollRect.content;
    }

    private void Update()
    {
        if (popupCardInfo == null || !popupCardInfo.activeSelf) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (popupCardInfoRect != null && RectTransformUtility.RectangleContainsScreenPoint(popupCardInfoRect, Input.mousePosition, null))
                return;

            popupCardInfo.SetActive(false);
        }
    }

    private void BindButtons()
    {
        if (btnShopOnHome != null)
        {
            btnShopOnHome.onClick.RemoveAllListeners();
            btnShopOnHome.onClick.AddListener(OpenPopupShop);
        }

        if (btnCloseShop != null)
        {
            btnCloseShop.onClick.RemoveAllListeners();
            btnCloseShop.onClick.AddListener(ClosePopupShop);
        }

        if (btnTabCard != null)
        {
            btnTabCard.onClick.RemoveAllListeners();
            btnTabCard.onClick.AddListener(() => SelectTab(ShopTab.Card));
        }

        if (btnTabGem != null)
        {
            btnTabGem.onClick.RemoveAllListeners();
            btnTabGem.onClick.AddListener(() => SelectTab(ShopTab.Gem));
        }
    }

    private void CacheDataResources()
    {
        if (GameDataManager.Instance != null)
        {
            if (GameDataManager.Instance.CardDatabaseObject != null)
                cardDatabase = GameDataManager.Instance.CardDatabaseObject;

            if (GameDataManager.Instance.GemCollectionObject != null)
                gemCollection = GameDataManager.Instance.GemCollectionObject;
        }

        if (cardDatabase == null && cardDatabaseAsset != null)
            cardDatabase = cardDatabaseAsset;
        else if (cardDatabase == null)
            cardDatabase = LoadResourceByTypeName("CardDatabase", cardResourceName);

        if (gemCollection == null && gemCollectionAsset != null)
            gemCollection = gemCollectionAsset;
        else if (gemCollection == null)
            gemCollection = LoadResourceByTypeName("GemCollection", gemResourceName);

#if UNITY_EDITOR
        // Fallback cho project hi?n t?i: data n?m ? Assets/Game/Resource (kh�ng ph?i Resources)
        if (cardDatabase == null)
            cardDatabase = LoadEditorAssetByTypeName("CardDatabase", "Assets/Game/Resource/CardDatabase.asset");

        if (gemCollection == null)
            gemCollection = LoadEditorAssetByTypeName("GemCollection", "Assets/Game/Resource/GemCollection.asset");
#endif

        if (cardDatabase == null)
            Debug.LogWarning("[ShopManager] Kh�ng load ???c Card database. H�y g�n tr?c ti?p CardDatabase v�o ShopManager.");

        if (gemCollection == null)
            Debug.LogWarning("[ShopManager] Kh�ng load ???c Gem collection. H�y g�n tr?c ti?p GemCollection v�o ShopManager.");
    }

#if UNITY_EDITOR
    private UnityEngine.Object LoadEditorAssetByTypeName(string typeName, string preferredPath)
    {
        Type t = FindTypeInLoadedAssemblies(typeName);
        if (t == null) return null;

        if (!string.IsNullOrEmpty(preferredPath))
        {
            UnityEngine.Object direct = UnityEditor.AssetDatabase.LoadAssetAtPath(preferredPath, t);
            if (direct != null) return direct;
        }

        string filter = "t:" + typeName;
        string[] guids = UnityEditor.AssetDatabase.FindAssets(filter);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            UnityEngine.Object asset = UnityEditor.AssetDatabase.LoadAssetAtPath(path, t);
            if (asset != null) return asset;
        }

        return null;
    }
#endif

    private UnityEngine.Object LoadResourceByTypeName(string typeName, string desiredName)
    {
        Type t = FindTypeInLoadedAssemblies(typeName);
        if (t == null) return null;

        if (!string.IsNullOrEmpty(desiredName))
        {
            UnityEngine.Object direct = Resources.Load(desiredName, t);
            if (direct != null) return direct;

            UnityEngine.Object inFolder = Resources.Load("Game/Resource/" + desiredName, t);
            if (inFolder != null) return inFolder;
        }

        UnityEngine.Object[] all = Resources.LoadAll(string.Empty, t);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == desiredName)
                return all[i];
        }

        return all.Length > 0 ? all[0] : null;
    }

    public void OpenPopupShop()
    {
        if (cardDatabase == null || gemCollection == null)
            CacheDataResources();

        if (popupShop != null)
            popupShop.SetActive(true);

        SelectTab(ShopTab.Card);
    }

    public void ClosePopupShop()
    {
        if (popupShop != null)
            popupShop.SetActive(false);

        if (popupCardInfo != null)
            popupCardInfo.SetActive(false);
    }

    private void SelectTab(ShopTab tab)
    {
        SetTabVisual(btnTabCard, tab == ShopTab.Card);
        SetTabVisual(btnTabGem, tab == ShopTab.Gem);

        RebuildShopItems(tab == ShopTab.Card ? BuildCardEntries() : BuildGemEntries());
    }

    private void SetTabVisual(Button tabButton, bool selected)
    {
        if (tabButton == null) return;

        Image img = tabButton.GetComponent<Image>();
        if (img != null)
            img.color = selected ? tabSelectedColor : tabNormalColor;

        tabButton.transform.DOScale(selected ? tabSelectedScale : 1f, 0.15f).SetEase(Ease.OutQuad);
    }

    private void RebuildShopItems(List<ShopEntry> entries)
    {
        if (itemRoot == null || itemShopPrefab == null)
            return;

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }
        spawnedItems.Clear();

        for (int i = 0; i < entries.Count; i++)
        {
            ShopEntry e = entries[i];
            ShopItemUI ui = Instantiate(itemShopPrefab, itemRoot);
            ui.Setup(
                e.itemName,
                e.sprite,
                e.priceGold,
                e.isCard,
                qty => BuyEntry(e, qty),
                () => ShowCardInfo(e.id, e.level, e.isCard)
            );
            spawnedItems.Add(ui);
        }

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private List<ShopEntry> BuildCardEntries()
    {
        List<ShopEntry> list = new List<ShopEntry>();
        if (cardDatabase == null) return list;

        IList cards = GetListField(cardDatabase, "cards");
        if (cards == null) return list;

        for (int i = 0; i < cards.Count; i++)
        {
            object card = cards[i];
            if (card == null) continue;

            string cardName = GetStringField(card, "cardName");
            string cardDesc = GetStringField(card, "description");
            int cardId = GetIntField(card, "id", i);
            Sprite cardSprite = GetSpriteField(card, "mainSprite");

            IList levels = GetIListField(card, "levels");
            if (levels == null) continue;

            for (int lv = 0; lv < levels.Count; lv++)
            {
                object lvData = levels[lv];
                if (lvData == null) continue;

                int level = Mathf.Max(1, GetIntField(lvData, "level", lv + 1));
                Sprite sprite = GetSpriteField(lvData, "sprite") ?? cardSprite;
                int priceGold = Mathf.Max(0, GetIntField(lvData, "priceGold", 0));

                list.Add(new ShopEntry
                {
                    isCard = true,
                    id = cardId,
                    level = level,
                    itemName = GetLocalizedText(cardName, cardName) + " Lv" + level,
                    description = string.IsNullOrEmpty(cardDesc) ? "No description" : cardDesc,
                    sprite = sprite,
                    priceGold = priceGold
                });
            }
        }

        return list;
    }

    private List<ShopEntry> BuildGemEntries()
    {
        List<ShopEntry> list = new List<ShopEntry>();
        if (gemCollection == null) return list;

        IList elements = GetIListField(gemCollection, "elements");
        if (elements == null) return list;

        for (int e = 0; e < elements.Count; e++)
        {
            object element = elements[e];
            if (element == null) continue;

            string elementName = GetStringField(element, "element");
            IList gemLevels = GetIListField(element, "gemLevels");
            if (gemLevels == null) continue;

            for (int lv = 0; lv < gemLevels.Count; lv++)
            {
                object gem = gemLevels[lv];
                if (gem == null) continue;

                int level = Mathf.Clamp(GetIntField(gem, "level", lv + 1), 1, 5);
                float bonus = GetFloatField(gem, "upgradeBonus", 0f);
                int priceGold = Mathf.Max(0, GetIntField(gem, "priceGold", 0));
                Sprite sprite = GetSpriteField(gem, "sprite");

                list.Add(new ShopEntry
                {
                    isCard = false,
                    elementId = e,
                    level = level,
                    itemName = GetLocalizedText(elementName, elementName) + " Gem Lv" + level,
                    description = "Upgrade Bonus: " + bonus.ToString("0.##") + "%",
                    sprite = sprite,
                    priceGold = priceGold
                });
            }
        }

        return list;
    }

    private void BuyEntry(ShopEntry entry, int quantity)
    {
        object playerManager = GetPlayerManagerInstance();
        if (entry == null || playerManager == null) return;

        int qty = Mathf.Clamp(quantity, 1, 99);
        int totalPrice = Mathf.Max(0, entry.priceGold) * qty;
        int currentGold = GetCurrentGold(playerManager);

        if (currentGold < totalPrice)
        {
            ShowNotEnoughGold();
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPurchaseFailedSound();
            return;
        }

        InvokePlayerManagerMethod(playerManager, "AddGold", -totalPrice);

        if (entry.isCard)
            InvokePlayerManagerMethod(playerManager, "AddOrUpdateOwnedCard", entry.id, entry.level, qty);
        else
            InvokePlayerManagerMethod(playerManager, "AddOrUpdateOwnedGem", entry.elementId, entry.level, qty);

        InvokePlayerManagerMethod(playerManager, "SaveData");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPurchaseSuccessSound();

        SteamManager.Instance.ReportShopPurchase();
        ShowPurchaseSuccess();
    }

    private void ShowNotEnoughGold()
    {
        if (txtNotEnoughGold == null) return;

        SetComponentText(txtNotEnoughGold, GetLocalizedText(NotEnoughGoldKey, "Not Enough Gold"));
        txtNotEnoughGold.gameObject.SetActive(true);

        RectTransform rt = txtNotEnoughGold.GetComponent<RectTransform>();
        if (rt != null)
            rt.localPosition = notEnoughGoldStartLocalPos;

        CanvasGroup cg = txtNotEnoughGold.GetComponent<CanvasGroup>();
        if (cg == null) cg = txtNotEnoughGold.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        Sequence seq = DOTween.Sequence();
        if (rt != null)
            seq.Join(rt.DOLocalMoveY(notEnoughGoldStartLocalPos.y + 60f, 3f).SetEase(Ease.OutCubic));

        seq.Join(DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, 3f));
        seq.OnComplete(() =>
        {
            if (txtNotEnoughGold != null)
                txtNotEnoughGold.gameObject.SetActive(false);
        });
    }

    private void ShowPurchaseSuccess()
    {
        if (txtNotEnoughGold == null) return;

        SetComponentText(txtNotEnoughGold, GetLocalizedText(PurchaseSuccessKey, "Purchase Successful"));
        txtNotEnoughGold.gameObject.SetActive(true);

        RectTransform rt = txtNotEnoughGold.GetComponent<RectTransform>();
        if (rt != null)
            rt.localPosition = notEnoughGoldStartLocalPos;

        CanvasGroup cg = txtNotEnoughGold.GetComponent<CanvasGroup>();
        if (cg == null) cg = txtNotEnoughGold.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        Sequence seq = DOTween.Sequence();
        if (rt != null)
            seq.Join(rt.DOLocalMoveY(notEnoughGoldStartLocalPos.y + 60f, 3f).SetEase(Ease.OutCubic));

        seq.Join(DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, 3f));
        seq.OnComplete(() =>
        {
            if (txtNotEnoughGold != null)
                txtNotEnoughGold.gameObject.SetActive(false);
        });
    }

    private void ShowCardInfo(int cardId, int cardLevel, bool isCard)
    {
        if (!isCard)
            return;

        if (GameDataManager.Instance == null)
            return;

        CardDatabase cardDb = GameDataManager.Instance.CardDatabaseObject as CardDatabase;
        if (cardDb == null)
            return;

        CardDatabase.Card card = cardDb.GetCardById(cardId);
        if (card == null)
            return;

        CardDatabase.CardLevel levelData = card.GetLevel(cardLevel);
        // Localize description từ key trước khi format coloring
        string localizedDesc = GetLocalizedText(card.description, card.description ?? "No description");
        string formattedDesc = FormatCardDescription(localizedDesc, levelData, card.type);

        EnsureCardInfoReferences();

        if (popupCardInfo == null || txtCardInfoDescription == null)
            return;

        SetComponentText(txtCardInfoDescription, formattedDesc);
        popupCardInfo.SetActive(true);
    }

    private string FormatCardDescription(string description, CardDatabase.CardLevel levelData, CardDatabase.CardType cardType)
    {
        if (string.IsNullOrEmpty(description))
            return "No description";

        string value = levelData != null ? levelData.value.ToString() : "0";
        string colorHex = GetColorForCardType(cardType);

        // Thay thế $ bằng value với màu
        string formatted = description.Replace("$", $"<color={colorHex}>{value}</color>");
        return formatted;
    }

    private string GetColorForCardType(CardDatabase.CardType cardType)
    {
        return cardType switch
        {
            CardDatabase.CardType.Heal => "#6FFF00",   // Xanh lá
            CardDatabase.CardType.Mana => "#4080FF",   // Xanh dương
            CardDatabase.CardType.Rage => "#FF0000",   // Đỏ
            CardDatabase.CardType.Attack => "#FFFF00", // Vàng
            _ => "#FFFFFF"                             // Trắng (default)
        };
    }

    private string GetLocalizedText(string keyOrText, string fallback)
    {
        if (string.IsNullOrWhiteSpace(keyOrText))
            return fallback;

        LocalizationManager lm = LocalizationManager.Instance;
        if (lm != null && lm.IsLoaded)
            return lm.GetText(keyOrText, fallback);

        return fallback;
    }

    private void EnsureCardInfoReferences()
    {
        if (popupCardInfo == null)
            return;

        if (popupCardInfoRect == null)
            popupCardInfoRect = popupCardInfo.GetComponent<RectTransform>();

        if (txtCardInfoDescription == null)
        {
            TMPro.TextMeshProUGUI tmp = popupCardInfo.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmp != null)
                txtCardInfoDescription = tmp;
        }
    }

    private object GetPlayerManagerInstance()
    {
        Type managerType = FindTypeInLoadedAssemblies("PlayerManager");
        if (managerType == null) return null;

        FieldInfo instanceField = managerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
        return instanceField != null ? instanceField.GetValue(null) : null;
    }

    private int GetCurrentGold(object playerManager)
    {
        if (playerManager == null) return 0;

        PropertyInfo dataProp = playerManager.GetType().GetProperty("Data", BindingFlags.Public | BindingFlags.Instance);
        if (dataProp == null) return 0;

        object dataObj = dataProp.GetValue(playerManager, null);
        if (dataObj == null) return 0;

        FieldInfo goldField = dataObj.GetType().GetField("gold", BindingFlags.Public | BindingFlags.Instance);
        if (goldField == null) return 0;

        object value = goldField.GetValue(dataObj);
        return value is int ? (int)value : 0;
    }

    private void InvokePlayerManagerMethod(object playerManager, string methodName, params object[] args)
    {
        if (playerManager == null) return;

        MethodInfo method = playerManager.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (method == null) return;

        method.Invoke(playerManager, args);
    }

    private Type FindTypeInLoadedAssemblies(string typeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Type t = assemblies[i].GetType(typeName);
            if (t != null) return t;
        }
        return null;
    }

    private IList GetListField(object obj, string fieldName)
    {
        if (obj == null) return null;
        FieldInfo f = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (f == null) return null;
        return f.GetValue(obj) as IList;
    }

    private IList GetIListField(object obj, string fieldName)
    {
        if (obj == null) return null;
        FieldInfo f = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (f == null) return null;

        object value = f.GetValue(obj);
        return value as IList;
    }

    private int GetIntField(object obj, string fieldName, int fallback)
    {
        if (obj == null) return fallback;
        FieldInfo f = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (f == null) return fallback;
        object value = f.GetValue(obj);
        return value is int ? (int)value : fallback;
    }

    private float GetFloatField(object obj, string fieldName, float fallback)
    {
        if (obj == null) return fallback;
        FieldInfo f = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (f == null) return fallback;
        object value = f.GetValue(obj);
        return value is float ? (float)value : fallback;
    }

    private string GetStringField(object obj, string fieldName)
    {
        if (obj == null) return string.Empty;
        FieldInfo f = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (f == null) return string.Empty;
        object value = f.GetValue(obj);
        return value as string ?? string.Empty;
    }

    private Sprite GetSpriteField(object obj, string fieldName)
    {
        if (obj == null) return null;
        FieldInfo f = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (f == null) return null;
        return f.GetValue(obj) as Sprite;
    }

    private void SetComponentText(Component target, string value)
    {
        if (target == null) return;

        PropertyInfo p = target.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
        if (p != null && p.PropertyType == typeof(string))
        {
            p.SetValue(target, value, null);
            return;
        }

        FieldInfo f = target.GetType().GetField("text", BindingFlags.Public | BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(string))
            f.SetValue(target, value);
    }
}

