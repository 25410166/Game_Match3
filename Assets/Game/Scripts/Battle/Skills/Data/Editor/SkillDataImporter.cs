using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[CreateAssetMenu(fileName = "SkillDataImporter", menuName = "Battle/Skill Data Importer")]
public class SkillDataImporter : ScriptableObject
{
    [SerializeField] private string googleSheetCsvUrl;
    [SerializeField] private SkillDatabase targetSkillDatabase;

    private const string OutputFolder = "Assets/Game/Data/Skills";
    private const string SkillSpriteFolder = "Assets/Game/Sprites/Skills";

    public void ImportSkillsFromGoogleSheet()
    {
        if (string.IsNullOrWhiteSpace(googleSheetCsvUrl))
        {
            Debug.LogError("[SkillDataImporter] Google Sheet URL is empty.");
            return;
        }

        string csvText = DownloadCsv(googleSheetCsvUrl.Trim());
        if (string.IsNullOrEmpty(csvText))
        {
            Debug.LogError("[SkillDataImporter] Failed to download CSV.");
            return;
        }

        EnsureOutputFolder();

        List<Dictionary<string, string>> rows = ParseCsv(csvText);
        if (rows.Count == 0)
        {
            Debug.LogWarning("[SkillDataImporter] CSV has no data rows.");
            return;
        }

        Dictionary<int, SkillData> existingById = LoadExistingSkillsById();

        int created = 0;
        int updated = 0;
        int skipped = 0;

        foreach (Dictionary<string, string> row in rows)
        {
            if (!TryParseInt(Get(row, "skillId"), out int skillId) || skillId <= 0)
            {
                skipped++;
                continue;
            }

            SkillData asset;
            if (!existingById.TryGetValue(skillId, out asset) || asset == null)
            {
                asset = CreateInstance<SkillData>();
                string assetPath = string.Format("{0}/Skill_{1}.asset", OutputFolder, skillId);
                AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(assetPath));
                existingById[skillId] = asset;
                created++;
            }
            else
            {
                updated++;
            }

            ApplyRow(asset, row);
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SyncSkillDatabase(existingById);

        Debug.Log($"[SkillDataImporter] Import complete. Created={created}, Updated={updated}, Skipped={skipped}");
    }

    private void SyncSkillDatabase(Dictionary<int, SkillData> skillsById)
    {
        if (targetSkillDatabase == null)
        {
            Debug.LogWarning("[SkillDataImporter] targetSkillDatabase is null. Imported SkillData assets were created/updated but not synced to a central SkillDatabase.");
            return;
        }

        List<SkillData> ordered = new List<SkillData>();
        foreach (KeyValuePair<int, SkillData> pair in skillsById)
        {
            if (pair.Value != null)
                ordered.Add(pair.Value);
        }

        ordered.Sort((a, b) => a.skillId.CompareTo(b.skillId));
        targetSkillDatabase.SetSkills(ordered);
        targetSkillDatabase.MarkDirty();
        EditorUtility.SetDirty(targetSkillDatabase);
        AssetDatabase.SaveAssets();
    }

    private static string DownloadCsv(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) { }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[SkillDataImporter] Download error: " + request.error);
                return null;
            }

            return request.downloadHandler != null ? request.downloadHandler.text : null;
        }
    }

    private static Dictionary<int, SkillData> LoadExistingSkillsById()
    {
        Dictionary<int, SkillData> map = new Dictionary<int, SkillData>();
        string[] guids = AssetDatabase.FindAssets("t:SkillData", new[] { OutputFolder });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill == null || skill.skillId <= 0)
                continue;

            map[skill.skillId] = skill;
        }

        return map;
    }

    private static void ApplyRow(SkillData asset, Dictionary<string, string> row)
    {
        asset.skillId = ParseInt(Get(row, "skillId"), asset.skillId);
        asset.skillName = Get(row, "skillName");
        asset.desSkill = Get(row, "DesSkill");
        asset.attackType = ParseEnum(Get(row, "attackType"), SkillAttackType.Range);
        asset.rangeType = ParseEnum(Get(row, "rangeType"), SkillRangeType.DirectFX);
        asset.typeSkill = ParseSkillType(GetAny(row, "typeSkill", "SkillType", "skillType"));
        asset.hitCount = Mathf.Max(1, ParseInt(Get(row, "hitCount"), 1));
        asset.hitDelay = Mathf.Max(0f, ParseFloat(Get(row, "hitDelay"), 0f));
        asset.damageMultiplier = Mathf.Max(0f, ParseFloat(Get(row, "damageMultiplier"), 1f));
        asset.effect = ParseStatusEffectType(Get(row, "effect"));
        asset.roundEffect = Mathf.Max(0, ParseInt(Get(row, "roundEffect"), 0));
        asset.animationPlayCount = Mathf.Max(1, ParseInt(Get(row, "AniCount"), 1));
        asset.fxPlayCount = Mathf.Max(1, ParseInt(Get(row, "FxCount"), 1));
        asset.skillSprite = LoadSkillSprite(asset.skillId);
        asset.manaCost = Mathf.Max(0, ParseInt(Get(row, "manaCost"), 0));
        asset.rageCost = Mathf.Max(0, ParseInt(Get(row, "rageCost"), 0));
        asset.hpCostPercent = Mathf.Clamp(ParseFloat(Get(row, "hpCostPercent"), 0f), 0f, 100f);
        asset.boardEffectType = ParseEnum(Get(row, "boardEffectType"), SkillBoardEffectType.None);
        asset.animationDuration = Mathf.Max(0f, ParseFloat(Get(row, "animationDuration"), 1.2f));
        asset.auditionSequenceLengthMin = Mathf.Max(1, ParseInt(Get(row, "auditionSequenceLengthMin"), asset.auditionSequenceLengthMin));
        asset.auditionSequenceLengthMax = Mathf.Max(asset.auditionSequenceLengthMin, ParseInt(Get(row, "auditionSequenceLengthMax"), asset.auditionSequenceLengthMax));
        asset.auditionRoundDuration = Mathf.Max(0.1f, ParseFloat(Get(row, "auditionRoundDuration"), asset.auditionRoundDuration));
        asset.auditionPerfectZoneStart = Mathf.Clamp01(ParseFloat(Get(row, "auditionPerfectZoneStart"), asset.auditionPerfectZoneStart));
        asset.auditionPerfectZoneEnd = Mathf.Clamp(ParseFloat(Get(row, "auditionPerfectZoneEnd"), asset.auditionPerfectZoneEnd), asset.auditionPerfectZoneStart, 1f);
        asset.auditionPerfectMultiplier = Mathf.Max(0f, ParseFloat(Get(row, "auditionPerfectMultiplier"), asset.auditionPerfectMultiplier));
        asset.auditionGreatMultiplier = Mathf.Max(0f, ParseFloat(Get(row, "auditionGreatMultiplier"), asset.auditionGreatMultiplier));
        asset.auditionMissMultiplier = Mathf.Max(0f, ParseFloat(Get(row, "auditionMissMultiplier"), asset.auditionMissMultiplier));
    }

    private static SkillType ParseSkillType(string raw)
    {
        string normalized = NormalizeKey(raw);
        if (string.IsNullOrEmpty(normalized) || normalized == "none")
            return SkillType.None;

        if (normalized == "audition")
            return SkillType.Audition;

        if (Enum.TryParse((raw ?? string.Empty).Trim(), true, out SkillType parsed))
            return parsed;

        return SkillType.None;
    }

    private static StatusEffectType ParseStatusEffectType(string raw)
    {
        string normalized = NormalizeKey(raw);
        if (string.IsNullOrEmpty(normalized) || normalized == "none")
            return StatusEffectType.None;

        if (normalized.Contains("doc") || normalized.Contains("poison"))
            return StatusEffectType.Poison;

        if (normalized.Contains("thieudot") || normalized.Contains("burn"))
            return StatusEffectType.Burn;

        if (normalized.Contains("camlang") || normalized.Contains("silence"))
            return StatusEffectType.Silence;

        if (normalized.Contains("giam50") || normalized.Contains("giamsatthuong") || normalized.Contains("damagereduction") || normalized.Contains("weaken"))
            return StatusEffectType.DamageReduction;

        if (Enum.TryParse((raw ?? string.Empty).Trim(), true, out StatusEffectType parsed))
            return parsed;

        return StatusEffectType.None;
    }

    private static string NormalizeKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string value = raw.Trim().ToLowerInvariant().Replace("đ", "d");
        string normalized = value.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder(normalized.Length);

        for (int i = 0; i < normalized.Length; i++)
        {
            char ch = normalized[i];
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
        }

        return sb.ToString();
    }

    private static Sprite LoadSkillSprite(int skillId)
    {
        if (skillId <= 0)
            return null;

        if (!AssetDatabase.IsValidFolder(SkillSpriteFolder))
            return null;

        string[] guids = AssetDatabase.FindAssets(string.Format("{0} t:Sprite", skillId), new[] { SkillSpriteFolder });
        if (guids == null || guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static List<Dictionary<string, string>> ParseCsv(string csv)
    {
        List<string> lines = SplitCsvLines(csv);
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
        if (lines.Count == 0)
            return rows;

        List<string> headers = ParseCsvLine(lines[0]);
        for (int i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            List<string> cols = ParseCsvLine(lines[i]);
            Dictionary<string, string> row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int c = 0; c < headers.Count; c++)
            {
                string key = headers[c];
                string value = c < cols.Count ? cols[c] : string.Empty;
                row[key] = value;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static List<string> SplitCsvLines(string csv)
    {
        List<string> lines = new List<string>();
        if (string.IsNullOrEmpty(csv))
            return lines;

        StringBuilder sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char ch = csv[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                sb.Append(ch);
                continue;
            }

            if (!inQuotes && (ch == '\n' || ch == '\r'))
            {
                if (sb.Length > 0)
                {
                    lines.Add(sb.ToString());
                    sb.Length = 0;
                }
                continue;
            }

            sb.Append(ch);
        }

        if (sb.Length > 0)
            lines.Add(sb.ToString());

        return lines;
    }

    private static List<string> ParseCsvLine(string line)
    {
        List<string> result = new List<string>();
        if (line == null)
            return result;

        StringBuilder sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '"')
            {
                bool escapedQuote = inQuotes && i + 1 < line.Length && line[i + 1] == '"';
                if (escapedQuote)
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString().Trim());
                sb.Length = 0;
                continue;
            }

            sb.Append(ch);
        }

        result.Add(sb.ToString().Trim());
        return result;
    }

    private static string Get(Dictionary<string, string> row, string key)
    {
        return row != null && row.TryGetValue(key, out string value) ? value : string.Empty;
    }

    private static string GetAny(Dictionary<string, string> row, params string[] keys)
    {
        if (row == null || keys == null || keys.Length == 0)
            return string.Empty;

        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (row.TryGetValue(key, out string directValue))
                return directValue;

            string normalizedKey = NormalizeKey(key);
            foreach (KeyValuePair<string, string> pair in row)
            {
                if (NormalizeKey(pair.Key) == normalizedKey)
                    return pair.Value;
            }
        }

        return string.Empty;
    }

    private static int ParseInt(string raw, int fallback)
    {
        return TryParseInt(raw, out int value) ? value : fallback;
    }

    private static bool TryParseInt(string raw, out int value)
    {
        return int.TryParse((raw ?? string.Empty).Trim(), out value);
    }

    private static float ParseFloat(string raw, float fallback)
    {
        if (float.TryParse((raw ?? string.Empty).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
            return value;
        if (float.TryParse((raw ?? string.Empty).Trim(), out value))
            return value;
        return fallback;
    }

    private static TEnum ParseEnum<TEnum>(string raw, TEnum fallback) where TEnum : struct
    {
        if (Enum.TryParse((raw ?? string.Empty).Trim(), true, out TEnum value))
            return value;
        return fallback;
    }

    private static void EnsureOutputFolder()
    {
        if (AssetDatabase.IsValidFolder("Assets/Game") && !AssetDatabase.IsValidFolder("Assets/Game/Data"))
            AssetDatabase.CreateFolder("Assets/Game", "Data");

        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Game/Data"))
                AssetDatabase.CreateFolder("Assets/Game", "Data");

            AssetDatabase.CreateFolder("Assets/Game/Data", "Skills");
        }
    }
}

[CustomEditor(typeof(SkillDataImporter))]
public class SkillDataImporterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8f);
        SkillDataImporter importer = (SkillDataImporter)target;
        if (GUILayout.Button("Import Skills from Google Sheet", GUILayout.Height(30f)))
        {
            importer.ImportSkillsFromGoogleSheet();
        }
    }
}
