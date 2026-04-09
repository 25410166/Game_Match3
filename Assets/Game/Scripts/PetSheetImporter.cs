#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;

/// <summary>
/// Editor tool: import CSV (Google Sheets export CSV) vào PetDatabase.asset
/// - Hỗ trợ quoted fields (CSV chuẩn)
/// - Map header tự động (không phụ thuộc thứ tự)
/// - Gán trực tiếp các cột: Pet Name, Element, Level, HP, Armor, Mana, Rage,
///   Crit Rate, Crit Damage, Weakness, Pet_ID, Attack Type
/// </summary>
public class PetSheetImporter : EditorWindow
{
    private string sheetUrl = "https://docs.google.com/spreadsheets/d/1DN9D3aiWbYA_6CQVMn6_YCKMxcT1BFNb/gviz/tq?tqx=out:csv&gid=64440507";
    private string databasePath = "Assets/Game/Data/PetDatabase.asset";

    [MenuItem("Tools/Import Pets To Database")]
    public static void ShowWindow()
    {
        GetWindow<PetSheetImporter>("Pet Sheet Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Google Sheet Importer", EditorStyles.boldLabel);

        sheetUrl = EditorGUILayout.TextField("Sheet CSV URL", sheetUrl);
        databasePath = EditorGUILayout.TextField("Database Path", databasePath);

        if (GUILayout.Button("Import Now"))
        {
            ImportData();
        }

        if (GUILayout.Button("Test Parse Selected CSV (debug)"))
        {
            // Debug: nếu bạn copy CSV content vào clipboard, có thể paste vào input prompt
            Debug.Log("Test parse: open console to see results.");
        }
    }

    private void ImportData()
    {
        if (string.IsNullOrEmpty(sheetUrl))
        {
            Debug.LogError("Sheet URL trống.");
            return;
        }

        UnityWebRequest www = UnityWebRequest.Get(sheetUrl);
        var async = www.SendWebRequest();

        // synchronous wait for editor (ok trong editor tool)
        while (!async.isDone) { }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Lỗi tải Google Sheet: " + www.error);
            return;
        }

        string csvText = www.downloadHandler.text;
        ParseCSV(csvText);
    }

    private void ParseCSV(string csv)
    {
        if (string.IsNullOrEmpty(csv))
        {
            Debug.LogError("CSV trống.");
            return;
        }

        // normalize line endings and split
        string[] lines = csv.Replace("\r\n", "\n").Replace("\r", "\n").Split(new[] { '\n' }, System.StringSplitOptions.None);

        // load hoặc tạo mới database
        PetDatabase db = AssetDatabase.LoadAssetAtPath<PetDatabase>(databasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<PetDatabase>();
            db.pets = new List<PetDataAsset>();
            AssetDatabase.CreateAsset(db, databasePath);
        }

        db.pets.Clear();

        if (lines.Length == 0)
        {
            Debug.LogError("CSV không có dòng nào.");
            return;
        }

        // parse header
        List<string> headerFields = ParseCsvLine(lines[0]);
        // normalize header names -> dùng dictionary map từ normalized -> canonical field name
        var headerMap = new Dictionary<string, string>(); // normalized header -> canonical

        // mapping các tên cột (normalize: lowercase, remove spaces/underscores)
        var canonicalNames = new Dictionary<string, string>()
        {
            { "petname", "PetName" },
            { "pet_name", "PetName" },
            { "name", "PetName" },

            { "element", "Element" },

            { "level", "Level" },

            { "hp", "HP" },
            { "health", "HP" },

            { "armor", "Armor" },

            { "mana", "Mana" },

            { "rage", "Rage" },

            { "crit_rate", "CritRate" },
            { "critrate", "CritRate" },

            { "crit_damage", "CritDamage" },
            { "critdamage", "CritDamage" },

            { "weakness", "Weakness" },

            { "pet_id", "Pet_ID" },
            { "petid", "Pet_ID" },

            { "attack_type", "AttackType" },
            { "attacktype", "AttackType" },

            { "baseattack", "BaseAttack" },
            { "attack", "BaseAttack" }
        };

        // build header index mapping canonical -> index
        var headerIndex = new Dictionary<string, int>();
        for (int c = 0; c < headerFields.Count; c++)
        {
            string raw = headerFields[c] ?? "";
            string norm = NormalizeHeader(raw);
            if (canonicalNames.TryGetValue(norm, out string canonical))
            {
                headerIndex[canonical] = c;
            }
            else
            {
                // try flexible matches (e.g., "Pet Name" -> petname)
                // nothing: ignore unknown columns
            }
        }

        // debug header
        Debug.Log("Parsed header fields: " + string.Join(" | ", headerFields.Select(h => $"[{h}]")));
        Debug.Log("Mapped columns: " + string.Join(", ", headerIndex.Select(kv => kv.Key + "->" + kv.Value)));

        // parse each data row
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            List<string> cols = ParseCsvLine(line);

            // create pet entry
            PetDataAsset pet = new PetDataAsset();
            pet.id = i; // dòng index (bạn có thể đổi cách gán id)
            // helper to get value by canonical name
            string GetStr(string canonical)
            {
                if (!headerIndex.ContainsKey(canonical)) return "";
                int idx = headerIndex[canonical];
                if (idx < 0 || idx >= cols.Count) return "";
                return cols[idx].Trim();
            }

            pet.petName = StripQuotes(GetStr("PetName"));
            pet.element = StripQuotes(GetStr("Element"));
            pet.level = SafeParseInt(StripQuotes(GetStr("Level")));
            pet.baseHP = SafeParseInt(StripQuotes(GetStr("HP")));
            pet.armor = SafeParseInt(StripQuotes(GetStr("Armor")));
            pet.baseMana = SafeParseInt(StripQuotes(GetStr("Mana")));
            pet.baseRage = SafeParseInt(StripQuotes(GetStr("Rage")));
            pet.baseAttack = SafeParseInt(StripQuotes(GetStr("BaseAttack")));
            pet.critRate = SafeParseFloat(StripQuotes(GetStr("CritRate")));
            pet.critDamage = SafeParseFloat(StripQuotes(GetStr("CritDamage")));
            pet.weakness = StripQuotes(GetStr("Weakness"));
            pet.petId = SafeParseInt(StripQuotes(GetStr("Pet_ID")));
            string attackTypeStr = StripQuotes(GetStr("AttackType")).ToLower();

            if (attackTypeStr.Contains("ranged")) pet.attackType = AttackType.Ranged;
            else pet.attackType = AttackType.Melee;

            // add to db
            db.pets.Add(pet);
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Import thành công {db.pets.Count} pets từ Google Sheet");
    }

    // CSV line parser supporting quoted fields and double quotes inside quoted
    private List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        if (line == null) return fields;
        var cur = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '"')
            {
                // double quote inside quoted field -> add one quote
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cur.Append('"');
                    i++; // skip next
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                fields.Add(cur.ToString());
                cur.Length = 0;
            }
            else
            {
                cur.Append(ch);
            }
        }
        fields.Add(cur.ToString());
        return fields;
    }

    // normalize header: trim, lower, remove spaces and underscores
    private string NormalizeHeader(string s)
    {
        if (s == null) return "";
        string t = s.Trim().ToLowerInvariant();
        t = t.Replace(" ", "").Replace("_", "").Replace("-", "");
        // remove surrounding quotes if any
        if (t.StartsWith("\"") && t.EndsWith("\"") && t.Length >= 2)
            t = t.Substring(1, t.Length - 2);
        return t;
    }

    private string StripQuotes(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim();
        if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)
            return s.Substring(1, s.Length - 2);
        return s;
    }

    private int SafeParseInt(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        s = s.Trim();
        // try parsing directly with culture (handles "1,234" thousands)
        if (int.TryParse(s, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int v))
            return v;
        // remove any non-digit except - and .
        var cleaned = new string(s.Where(ch => char.IsDigit(ch) || ch == '-' || ch == '.').ToArray());
        if (string.IsNullOrEmpty(cleaned)) return 0;
        // parse as double then cast
        if (double.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out double dv))
            return (int)dv;
        return 0;
    }

    private float SafeParseFloat(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0f;
        s = s.Trim();
        if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float fv))
            return fv;
        var cleaned = new string(s.Where(ch => char.IsDigit(ch) || ch == '-' || ch == '.').ToArray());
        if (string.IsNullOrEmpty(cleaned)) return 0f;
        if (float.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out fv))
            return fv;
        return 0f;
    }
}
#endif
