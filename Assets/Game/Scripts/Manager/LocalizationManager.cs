using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LocalizationManager : MonoBehaviour
{
    private const string KeyLanguage = "settings_language";

    private static LocalizationManager instance;
    public static LocalizationManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<LocalizationManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject(nameof(LocalizationManager));
                    instance = go.AddComponent<LocalizationManager>();
                }
            }
            return instance;
        }
    }

    [Header("Google Sheet CSV URL")]
    [SerializeField] private string sheetCsvUrl;
    [SerializeField] private bool loadOnAwake = true;

    [Header("Cached Data On Manager")]
    [SerializeField] private bool useCachedDataOnAwake = true;
    [SerializeField] private List<LocalizationEntry> cachedEntries = new List<LocalizationEntry>();

    [Header("Optional Scriptable Data")]
    [SerializeField] private LocalizationTableData localizationTableData;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private readonly Dictionary<string, LocalizationEntry> localizedRows = new Dictionary<string, LocalizationEntry>(StringComparer.OrdinalIgnoreCase);

    public event Action OnLocalizationLoaded;
    public event Action OnLanguageChanged;

    public bool IsLoaded { get; private set; }
    public string SheetCsvUrl => sheetCsvUrl;
    public LocalizationTableData TableData => localizationTableData;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (debugMode)
            Debug.Log($"[LocalizationManager] Awake - useCachedDataOnAwake: {useCachedDataOnAwake}, localizationTableData: {localizationTableData != null}, cachedEntries: {cachedEntries.Count}");

        if (useCachedDataOnAwake)
        {
            if (localizationTableData != null && localizationTableData.entries != null && localizationTableData.entries.Count > 0)
            {
                if (debugMode)
                    Debug.Log($"[LocalizationManager] Loading from LocalizationTableData: {localizationTableData.entries.Count} entries");
                SetData(localizationTableData.entries, false);
            }
            else if (cachedEntries != null && cachedEntries.Count > 0)
            {
                if (debugMode)
                    Debug.Log($"[LocalizationManager] Loading from cachedEntries: {cachedEntries.Count} entries");
                SetData(cachedEntries, false);
            }
            else
            {
                if (debugMode)
                    Debug.LogWarning("[LocalizationManager] No cached data found!");
            }
        }

        if (debugMode)
            Debug.Log($"[LocalizationManager] Awake complete - IsLoaded: {IsLoaded}");
    }

    private void Start()
    {
        if (debugMode)
            Debug.Log($"[LocalizationManager] Start - loadOnAwake: {loadOnAwake}, sheetCsvUrl: {sheetCsvUrl}");

        if (loadOnAwake && !string.IsNullOrWhiteSpace(sheetCsvUrl))
        {
            StartCoroutine(LoadFromGoogleSheet());
        }
    }

    public void NotifyLanguageChanged()
    {
        if (OnLanguageChanged != null)
            OnLanguageChanged.Invoke();
    }

    public void ReloadLocalization()
    {
        if (OnLocalizationLoaded != null)
            OnLocalizationLoaded.Invoke();
        if (OnLanguageChanged != null)
            OnLanguageChanged.Invoke();
    }

    public void SetSheetUrl(string url)
    {
        sheetCsvUrl = url;
    }

    public void Reload()
    {
        if (string.IsNullOrWhiteSpace(sheetCsvUrl))
        {
            Debug.LogWarning("[LocalizationManager] Sheet URL is empty.");
            return;
        }

        StartCoroutine(LoadFromGoogleSheet());
    }

    public void ApplyFromScriptable()
    {
        if (localizationTableData == null)
            return;

        SetData(localizationTableData.entries, true);
    }

    public bool ApplyCachedDataToScriptable()
    {
        if (localizationTableData == null)
            return false;

        localizationTableData.SetEntries(cachedEntries);
        return true;
    }

    public int ImportCsvData(string csv)
    {
        List<LocalizationEntry> parsed = ParseCsvToEntries(csv);
        SetData(parsed, true);
        return cachedEntries.Count;
    }

    public int ImportCsvDataToScriptable(string csv)
    {
        List<LocalizationEntry> parsed = ParseCsvToEntries(csv);

        if (localizationTableData != null)
        {
            localizationTableData.SetEntries(parsed);
            SetData(localizationTableData.entries, true);
            return localizationTableData.entries != null ? localizationTableData.entries.Count : 0;
        }

        SetData(parsed, true);
        return cachedEntries.Count;
    }

    public IEnumerator LoadFromGoogleSheet()
    {
        if (string.IsNullOrWhiteSpace(sheetCsvUrl))
            yield break;

        UnityWebRequest www = UnityWebRequest.Get(sheetCsvUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[LocalizationManager] Load failed: " + www.error);
            yield break;
        }

        int count = ImportCsvData(www.downloadHandler.text);
        Debug.Log($"[LocalizationManager] Loaded {count} localized texts from Google Sheet.");
    }

    public string GetText(string id, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(id))
            return fallback;

        if (!localizedRows.TryGetValue(id.Trim(), out LocalizationEntry row))
        {
            if (debugMode)
                Debug.LogWarning($"[LocalizationManager] GetText - ID not found: {id}");
            return string.IsNullOrEmpty(fallback) ? id : fallback;
        }

        string value = row.GetByLanguage(GetCurrentLanguageIndex());
        if (!string.IsNullOrEmpty(value))
            return value;

        if (!string.IsNullOrEmpty(row.en))
            return row.en;

        return string.IsNullOrEmpty(fallback) ? id : fallback;
    }

    public string FormatText(string id, params object[] args)
    {
        string template = GetText(id, id);
        if (args == null || args.Length == 0)
            return template;

        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return template;
        }
    }

    private void SetData(List<LocalizationEntry> entries, bool notify)
    {
        cachedEntries = new List<LocalizationEntry>();
        localizedRows.Clear();

        if (entries == null)
        {
            IsLoaded = false;
            if (debugMode)
                Debug.LogWarning("[LocalizationManager] SetData - entries is null!");
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            LocalizationEntry source = entries[i];
            if (source == null || string.IsNullOrWhiteSpace(source.id))
                continue;

            LocalizationEntry cloned = source.Clone();
            cloned.id = cloned.id.Trim();

            localizedRows[cloned.id] = cloned;
        }

        foreach (KeyValuePair<string, LocalizationEntry> pair in localizedRows)
        {
            cachedEntries.Add(pair.Value.Clone());
        }

        IsLoaded = cachedEntries.Count > 0;

        if (debugMode)
            Debug.Log($"[LocalizationManager] SetData - Loaded {cachedEntries.Count} entries, IsLoaded: {IsLoaded}, notify: {notify}");

        if (!notify)
            return;

        if (OnLocalizationLoaded != null)
            OnLocalizationLoaded.Invoke();
        if (OnLanguageChanged != null)
            OnLanguageChanged.Invoke();
    }

    private int GetCurrentLanguageIndex()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(KeyLanguage, 0), 0, 4);
    }

    private List<LocalizationEntry> ParseCsvToEntries(string csv)
    {
        List<LocalizationEntry> result = new List<LocalizationEntry>();

        if (string.IsNullOrWhiteSpace(csv))
            return result;

        List<List<string>> rows = ParseCsvTable(csv);
        if (rows.Count <= 1)
            return result;

        List<string> header = rows[0];
        int idIndex = FindHeaderIndex(header, "id");
        int enIndex = FindHeaderIndex(header, "en");
        int jpIndex = FindHeaderIndex(header, "jp");
        int krIndex = FindHeaderIndex(header, "kr");
        int cnIndex = FindHeaderIndex(header, "cn");
        int vnIndex = FindHeaderIndex(header, "vn");

        if (idIndex < 0)
        {
            Debug.LogError("[LocalizationManager] Missing 'Id' column.");
            return result;
        }

        for (int i = 1; i < rows.Count; i++)
        {
            List<string> cols = rows[i];
            if (cols == null || cols.Count == 0)
                continue;

            string id = GetValue(cols, idIndex);
            if (string.IsNullOrWhiteSpace(id))
                continue;

            LocalizationEntry row = new LocalizationEntry
            {
                id = id.Trim(),
                en = GetValue(cols, enIndex),
                jp = GetValue(cols, jpIndex),
                kr = GetValue(cols, krIndex),
                cn = GetValue(cols, cnIndex),
                vn = GetValue(cols, vnIndex)
            };

            result.Add(row);
        }

        return result;
    }

    private int FindHeaderIndex(List<string> headers, string key)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            if (NormalizeHeader(headers[i]) == key)
                return i;
        }
        return -1;
    }

    private string GetValue(List<string> cols, int index)
    {
        if (index < 0 || index >= cols.Count)
            return string.Empty;
        return StripQuotes(cols[index]);
    }

    private string NormalizeHeader(string value)
    {
        if (value == null) return string.Empty;
        return value.Trim().ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
    }

    private string StripQuotes(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        string trimmed = value.Trim();
        if (trimmed.Length >= 2 && trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
            trimmed = trimmed.Substring(1, trimmed.Length - 2);

        return trimmed;
    }

    private List<string> ParseCsvLine(string line)
    {
        List<string> fields = new List<string>();
        if (line == null)
            return fields;

        StringBuilder current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Length = 0;
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    private List<List<string>> ParseCsvTable(string csv)
    {
        List<List<string>> rows = new List<List<string>>();
        if (string.IsNullOrEmpty(csv))
            return rows;

        List<string> row = new List<string>();
        StringBuilder current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char ch = csv[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                row.Add(current.ToString());
                current.Length = 0;
            }
            else if ((ch == '\n' || ch == '\r') && !inQuotes)
            {
                if (ch == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                    i++;

                row.Add(current.ToString());
                current.Length = 0;
                rows.Add(row);
                row = new List<string>();
            }
            else
            {
                current.Append(ch);
            }
        }

        row.Add(current.ToString());
        rows.Add(row);
        return rows;
    }
}
