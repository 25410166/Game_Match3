#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

public class GuardianSheetImporter : EditorWindow
{
    private string sheetUrl = "";
    private string databasePath = "Assets/Game/Data/GuardianDatabase.asset";
    private int maxLevel = 10;
    private const string GuardianPrefabFolder = "Assets/Game/Prefabs/GuadiantPrefabs";
    private const string GuardianSpriteFolder = "Assets/Game/Sprites/Guadiant";

    [MenuItem("Tools/Import Guardians To Database")]
    public static void ShowWindow()
    {
        GetWindow<GuardianSheetImporter>("Guardian Sheet Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Guardian Sheet Importer", EditorStyles.boldLabel);

        sheetUrl = EditorGUILayout.TextField("Sheet CSV URL", sheetUrl);
        databasePath = EditorGUILayout.TextField("Database Path", databasePath);
        maxLevel = EditorGUILayout.IntField("Max Level", maxLevel);

        if (GUILayout.Button("Import Now"))
        {
            ImportData();
        }
    }

    private void ImportData()
    {
        if (string.IsNullOrWhiteSpace(sheetUrl))
        {
            Debug.LogError("Sheet URL is empty.");
            return;
        }

        UnityWebRequest www = UnityWebRequest.Get(sheetUrl);
        var async = www.SendWebRequest();

        while (!async.isDone) { }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load Google Sheet: " + www.error);
            return;
        }

        string csvText = www.downloadHandler.text;
        ParseCSV(csvText);
    }

    private void ParseCSV(string csv)
    {
        if (string.IsNullOrEmpty(csv))
        {
            Debug.LogError("CSV is empty.");
            return;
        }

        List<List<string>> rows = ParseCsvTable(csv);
        if (rows.Count == 0)
        {
            Debug.LogError("CSV has no rows.");
            return;
        }

        GuardianDatabase db = AssetDatabase.LoadAssetAtPath<GuardianDatabase>(databasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<GuardianDatabase>();
            db.guardians = new List<GuardianDataAsset>();
            AssetDatabase.CreateAsset(db, databasePath);
        }

        db.guardians.Clear();

        List<string> headerFields = rows[0];
        Dictionary<string, int> headerIndex = BuildHeaderIndex(headerFields);
        bool hasPerLevelValues = HasPerLevelValues(headerIndex);

        Debug.Log("Guardian header fields: " + string.Join(" | ", headerFields.Select(h => "[" + h + "]")));

        int nextId = 1;
        int safeMaxLevel = Mathf.Max(1, maxLevel);

        for (int i = 1; i < rows.Count; i++)
        {
            List<string> cols = rows[i];
            if (cols == null || cols.Count == 0)
                continue;

            bool allEmpty = true;
            for (int c = 0; c < cols.Count; c++)
            {
                if (!string.IsNullOrWhiteSpace(cols[c]))
                {
                    allEmpty = false;
                    break;
                }
            }
            if (allEmpty)
                continue;

            GuardianDataAsset guardian = new GuardianDataAsset();

            int id = SafeParseInt(Get(cols, headerIndex, "Id"));
            guardian.guardianId = id > 0 ? id : nextId++;
            guardian.guardianName = StripQuotes(Get(cols, headerIndex, "Name"));
            guardian.element = ParseElement(StripQuotes(Get(cols, headerIndex, "Element")));
            guardian.description = StripQuotes(Get(cols, headerIndex, "Description"));
            guardian.story = StripQuotes(Get(cols, headerIndex, "Story"));

            guardian.applyOnBattleStart = SafeParseBool(Get(cols, headerIndex, "ApplyOnBattleStart"));
            bool applyOnPlayerTurn = SafeParseBool(Get(cols, headerIndex, "ApplyOnPlayerTurn"));
            guardian.applyOnPlayerTurn = applyOnPlayerTurn || !headerIndex.ContainsKey("ApplyOnPlayerTurn");

            float baseValue1 = SafeParseFloat(Get(cols, headerIndex, "Value1"));
            float value2 = hasPerLevelValues ? 0f : SafeParseFloat(Get(cols, headerIndex, "Value2"));
            float value3 = hasPerLevelValues ? 0f : SafeParseFloat(Get(cols, headerIndex, "Value3"));
            int diamondCost = SafeParseInt(Get(cols, headerIndex, "DiamondCost"));

            guardian.guardianPrefab = FindGuardianPrefabByName(guardian.guardianName);

            string avatarIconName = StripQuotes(Get(cols, headerIndex, "AvatarIcon"));
            if (!string.IsNullOrWhiteSpace(avatarIconName))
                guardian.avatarIcon = FindSpriteByNameInFolder(avatarIconName, GuardianSpriteFolder);

            string vfxName = StripQuotes(Get(cols, headerIndex, "VfxPrefab"));
            if (!string.IsNullOrWhiteSpace(vfxName))
                guardian.vfxPrefab = FindPrefabByName(vfxName);

            guardian.levels = new List<GuardianLevelData>(safeMaxLevel);
            for (int lv = 1; lv <= safeMaxLevel; lv++)
            {
                float perLevelValue1 = baseValue1;
                if (hasPerLevelValues)
                {
                    string perLevelKey = "Value" + lv;
                    string perLevelRaw = Get(cols, headerIndex, perLevelKey);
                    perLevelValue1 = string.IsNullOrWhiteSpace(perLevelRaw)
                        ? baseValue1
                        : SafeParseFloat(perLevelRaw);
                }

                guardian.levels.Add(new GuardianLevelData
                {
                    level = lv,
                    value1 = perLevelValue1,
                    value2 = value2,
                    value3 = value3,
                    diamondCost = lv == 1 ? Mathf.Max(0, diamondCost) : 0
                });
            }

            db.guardians.Add(guardian);
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Import guardians success: " + db.guardians.Count);
    }

    private Dictionary<string, int> BuildHeaderIndex(List<string> headerFields)
    {
        var headerIndex = new Dictionary<string, int>();
        if (headerFields == null)
            return headerIndex;

        var canonicalNames = new Dictionary<string, string>
        {
            { "id", "Id" },
            { "guardianid", "Id" },

            { "name", "Name" },
            { "guardianname", "Name" },
            { "guardian_name", "Name" },

            { "element", "Element" },

            { "desc", "Description" },
            { "description", "Description" },

            { "story", "Story" },
            { "lore", "Story" },

            { "value1", "Value1" },
            { "value2", "Value2" },
            { "value3", "Value3" },
            { "value4", "Value4" },
            { "value5", "Value5" },
            { "value6", "Value6" },
            { "value7", "Value7" },
            { "value8", "Value8" },
            { "value9", "Value9" },
            { "value10", "Value10" },

            { "diamond", "DiamondCost" },
            { "diamondcost", "DiamondCost" },
            { "price", "DiamondCost" },
            { "cost", "DiamondCost" },
            { "price_diamond", "DiamondCost" },

            { "icon", "Icon" },
            { "iconname", "Icon" },

            { "avatar", "AvatarIcon" },
            { "avataricon", "AvatarIcon" },
            { "avatar_icon", "AvatarIcon" },

            { "vfx", "VfxPrefab" },
            { "vfxprefab", "VfxPrefab" },
            { "vfx_prefab", "VfxPrefab" },

            { "applyonbattlestart", "ApplyOnBattleStart" },
            { "onbattlestart", "ApplyOnBattleStart" },
            { "battle_start", "ApplyOnBattleStart" },

            { "applyonplayerturn", "ApplyOnPlayerTurn" },
            { "onplayerturn", "ApplyOnPlayerTurn" },
            { "player_turn", "ApplyOnPlayerTurn" }
        };

        for (int c = 0; c < headerFields.Count; c++)
        {
            string raw = headerFields[c] ?? string.Empty;
            string norm = NormalizeHeader(raw);
            if (canonicalNames.TryGetValue(norm, out string canonical))
            {
                headerIndex[canonical] = c;
            }
        }

        return headerIndex;
    }

    private bool HasPerLevelValues(Dictionary<string, int> headerIndex)
    {
        if (headerIndex == null)
            return false;

        return headerIndex.ContainsKey("Value4")
            || headerIndex.ContainsKey("Value5")
            || headerIndex.ContainsKey("Value6")
            || headerIndex.ContainsKey("Value7")
            || headerIndex.ContainsKey("Value8")
            || headerIndex.ContainsKey("Value9")
            || headerIndex.ContainsKey("Value10");
    }

    private List<List<string>> ParseCsvTable(string csv)
    {
        var rows = new List<List<string>>();
        if (string.IsNullOrEmpty(csv)) return rows;

        var row = new List<string>();
        var cur = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char ch = csv[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    cur.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                row.Add(cur.ToString());
                cur.Length = 0;
            }
            else if ((ch == '\n' || ch == '\r') && !inQuotes)
            {
                if (ch == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                    i++;

                row.Add(cur.ToString());
                cur.Length = 0;
                rows.Add(row);
                row = new List<string>();
            }
            else
            {
                cur.Append(ch);
            }
        }

        row.Add(cur.ToString());
        rows.Add(row);
        return rows;
    }

    private string NormalizeHeader(string s)
    {
        if (s == null) return string.Empty;
        string t = s.Trim().ToLowerInvariant();
        t = t.Replace(" ", "").Replace("_", "").Replace("-", "");
        if (t.StartsWith("\"") && t.EndsWith("\"") && t.Length >= 2)
            t = t.Substring(1, t.Length - 2);
        return t;
    }

    private string Get(List<string> cols, Dictionary<string, int> headerIndex, string canonical)
    {
        if (cols == null || headerIndex == null || !headerIndex.ContainsKey(canonical))
            return string.Empty;

        int idx = headerIndex[canonical];
        if (idx < 0 || idx >= cols.Count)
            return string.Empty;

        return cols[idx];
    }

    private string StripQuotes(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Trim();
        if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)
            return s.Substring(1, s.Length - 2);
        return s;
    }

    private int SafeParseInt(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        s = StripQuotes(s).Trim();
        if (int.TryParse(s, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int v))
            return v;

        string cleaned = new string(s.Where(ch => char.IsDigit(ch) || ch == '-' || ch == '.').ToArray());
        if (string.IsNullOrEmpty(cleaned)) return 0;
        if (double.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out double dv))
            return (int)dv;
        return 0;
    }

    private float SafeParseFloat(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0f;
        s = StripQuotes(s).Trim();
        if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float fv))
            return fv;

        string cleaned = new string(s.Where(ch => char.IsDigit(ch) || ch == '-' || ch == '.').ToArray());
        if (string.IsNullOrEmpty(cleaned)) return 0f;
        if (float.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out fv))
            return fv;
        return 0f;
    }

    private bool SafeParseBool(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        string cleaned = StripQuotes(s).Trim().ToLowerInvariant();
        return cleaned == "1" || cleaned == "true" || cleaned == "yes" || cleaned == "y";
    }

    private GuardianElement ParseElement(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return GuardianElement.Leaf;

        string cleaned = raw.Trim().ToLowerInvariant();
        if (cleaned.Contains("leaf")) return GuardianElement.Leaf;
        if (cleaned.Contains("fire")) return GuardianElement.Fire;
        if (cleaned.Contains("metal")) return GuardianElement.Metal;
        if (cleaned.Contains("earth")) return GuardianElement.Earth;
        if (cleaned.Contains("water")) return GuardianElement.Water;
        if (cleaned.Contains("dark")) return GuardianElement.Dark;
        if (cleaned.Contains("light")) return GuardianElement.Light;

        return GuardianElement.Leaf;
    }

    private Sprite FindSpriteByName(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:Sprite");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null && string.Equals(sprite.name, name, System.StringComparison.OrdinalIgnoreCase))
                return sprite;
        }

        return null;
    }

    private Sprite FindSpriteByNameInFolder(string name, string folder)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(folder))
            return null;

        string[] guids = AssetDatabase.FindAssets(name + " t:Sprite", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null && string.Equals(sprite.name, name, System.StringComparison.OrdinalIgnoreCase))
                return sprite;
        }

        return null;
    }

    private GameObject FindPrefabByName(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:GameObject");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && string.Equals(prefab.name, name, System.StringComparison.OrdinalIgnoreCase))
                return prefab;
        }

        return null;
    }

    private GameObject FindGuardianPrefabByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        string[] guids = AssetDatabase.FindAssets(name + " t:GameObject", new[] { GuardianPrefabFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && string.Equals(prefab.name, name, System.StringComparison.OrdinalIgnoreCase))
                return prefab;
        }

        return null;
    }
}
#endif
