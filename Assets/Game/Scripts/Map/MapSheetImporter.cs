#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class MapSheetImporter : EditorWindow
{
    private string mapsUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQkmMnMdWklPn7WiDKoX5Rv4akv6TPrD3isqufTh97munc_D2qIXNeZjXKN8Tp5kA/pub?gid=1117593257&single=true&output=csv";
    private string rewardsUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQkmMnMdWklPn7WiDKoX5Rv4akv6TPrD3isqufTh97munc_D2qIXNeZjXKN8Tp5kA/pub?gid=1311626418&single=true&output=csv";
    private string mapDatabasePath = "Assets/Game/Data/MapDatabase.asset";
    private string mapDataFolder = "Assets/Game/Data/Maps";
    private string gemCollectionPath = "Assets/Game/Resource/GemCollection.asset";

    [MenuItem("Tools/Import Maps To Database")]
    public static void ShowWindow()
    {
        GetWindow<MapSheetImporter>("Map Sheet Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Map CSV Importer", EditorStyles.boldLabel);

        mapsUrl = EditorGUILayout.TextField("Maps CSV URL", mapsUrl);
        rewardsUrl = EditorGUILayout.TextField("Map Reward CSV URL", rewardsUrl);
        mapDatabasePath = EditorGUILayout.TextField("Map Database Path", mapDatabasePath);
        mapDataFolder = EditorGUILayout.TextField("MapData Folder", mapDataFolder);
        gemCollectionPath = EditorGUILayout.TextField("GemCollection Path", gemCollectionPath);

        if (GUILayout.Button("Import Maps + Rewards"))
            Import();
    }

    private void Import()
    {
        string mapsCsv = DownloadCsv(mapsUrl);
        string rewardsCsv = DownloadCsv(rewardsUrl);
        if (string.IsNullOrWhiteSpace(mapsCsv) || string.IsNullOrWhiteSpace(rewardsCsv))
            return;

        UnityEngine.Object gemCollection = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(gemCollectionPath);
        Dictionary<string, List<MapRewardData>> rewardsByMapId = ParseRewards(rewardsCsv, gemCollection);
        ParseMapsAndCreateAssets(mapsCsv, rewardsByMapId);
    }

    private string DownloadCsv(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogError("CSV URL tr?ng.");
            return null;
        }

        UnityWebRequest www = UnityWebRequest.Get(url);
        var op = www.SendWebRequest();
        while (!op.isDone) { }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("T?i CSV th?t b?i: " + www.error);
            return null;
        }

        return www.downloadHandler.text;
    }

    private void ParseMapsAndCreateAssets(string mapsCsv, Dictionary<string, List<MapRewardData>> rewardsByMapId)
    {
        string[] lines = NormalizeLines(mapsCsv);
        if (lines.Length == 0)
        {
            Debug.LogError("Maps CSV r?ng.");
            return;
        }

        EnsureFolder(mapDataFolder);

        MapDatabase db = AssetDatabase.LoadAssetAtPath<MapDatabase>(mapDatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<MapDatabase>();
            AssetDatabase.CreateAsset(db, mapDatabasePath);
        }

        db.maps.Clear();

        Dictionary<string, int> header = BuildHeaderMap(ParseCsvLine(lines[0]));
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            List<string> cols = ParseCsvLine(lines[i]);
            string mapId = GetValue(cols, header, "mapid");
            if (string.IsNullOrWhiteSpace(mapId))
                continue;

            string assetPath = mapDataFolder + "/" + SanitizeFileName(mapId) + ".asset";
            MapDataAsset mapAsset = AssetDatabase.LoadAssetAtPath<MapDataAsset>(assetPath);
            if (mapAsset == null)
            {
                mapAsset = ScriptableObject.CreateInstance<MapDataAsset>();
                AssetDatabase.CreateAsset(mapAsset, assetPath);
            }

            mapAsset.area = ParseAreaIndex(GetValue(cols, header, "area"));
            mapAsset.mapId = mapId;
            mapAsset.mapName = GetValue(cols, header, "mapname");
            mapAsset.reqUserLevel = SafeInt(GetValue(cols, header, "requserlevel"));
            mapAsset.petIdSpawn = SafeInt(GetValue(cols, header, "petidspawn"));
            mapAsset.petLevelSpawn = SafeInt(GetValue(cols, header, "petlevelspawn"));
            mapAsset.idGuadiant = SafeInt(GetValue(cols, header, "idguadiant"));
            mapAsset.levelGuadiant = Mathf.Max(1, SafeInt(GetValue(cols, header, "levelguadiant")));
            int rewardPetId = SafeInt(GetValue(cols, header, "rewardpetid"));
            if (rewardPetId <= 0)
                rewardPetId = SafeInt(GetValue(cols, header, "petidreward"));
            mapAsset.rewardPetId = rewardPetId > 0 ? rewardPetId : -1;
            int rewardGuardiantId = SafeInt(GetValue(cols, header, "reward_guardiant_id"));
            if (rewardGuardiantId <= 0)
                rewardGuardiantId = SafeInt(GetValue(cols, header, "rewardguardianid"));
            if (rewardGuardiantId <= 0)
                rewardGuardiantId = SafeInt(GetValue(cols, header, "reward_pet_id"));
            mapAsset.rewardGuardiantId = rewardGuardiantId > 0 ? rewardGuardiantId : -1;
            mapAsset.reqWinsPet = SafeInt(GetValue(cols, header, "reqwinspet"));

            if (rewardsByMapId.TryGetValue(mapId, out List<MapRewardData> rewards))
                mapAsset.rewards = new List<MapRewardData>(rewards);
            else
                mapAsset.rewards = new List<MapRewardData>();

            EditorUtility.SetDirty(mapAsset);
            db.maps.Add(mapAsset);
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("? Import Maps + Rewards thï¿½nh cï¿½ng: " + db.maps.Count + " maps");
    }

    private Dictionary<string, List<MapRewardData>> ParseRewards(string rewardsCsv, UnityEngine.Object gemCollection)
    {
        Dictionary<string, List<MapRewardData>> result = new Dictionary<string, List<MapRewardData>>();

        string[] lines = NormalizeLines(rewardsCsv);
        if (lines.Length == 0)
            return result;

        Dictionary<string, int> header = BuildHeaderMap(ParseCsvLine(lines[0]));
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            List<string> cols = ParseCsvLine(lines[i]);
            string mapId = GetValue(cols, header, "mapid");
            if (string.IsNullOrWhiteSpace(mapId))
                continue;

            MapRewardData reward = new MapRewardData();
            reward.mapId = mapId;
            reward.rewardType = ParseRewardType(GetValue(cols, header, "rewardtype"));
            reward.rewardId = GetValue(cols, header, "rewardid");
            
            // Try to extract level tá»« rewardId (format: "Fire_1" hoáº·c "Fire_Gem_Lv1")
            int extractedLevel = ExtractGemLevelFromRewardId(reward.rewardId);
            if (extractedLevel > 0)
                reward.gemLevel = Mathf.Clamp(extractedLevel, 1, 5);
            else
                reward.gemLevel = Mathf.Clamp(SafeInt(GetValue(cols, header, "gemlevel")), 1, 5);
            
            reward.amountMin = SafeInt(GetValue(cols, header, "amountmin"));
            reward.amountMax = SafeInt(GetValue(cols, header, "amountmax"));
            reward.weight = SafeInt(GetValue(cols, header, "weight"));
            reward.gemElementId = ResolveGemElementId(reward.rewardId, gemCollection);

            if (!result.TryGetValue(mapId, out List<MapRewardData> list))
            {
                list = new List<MapRewardData>();
                result[mapId] = list;
            }

            list.Add(reward);
        }

        return result;
    }

    private int ResolveGemElementId(string rewardId, UnityEngine.Object gemCollection)
    {
        if (gemCollection == null || string.IsNullOrWhiteSpace(rewardId))
            return -1;

        FieldInfo elementsField = gemCollection.GetType().GetField("elements", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (elementsField == null)
            return -1;

        IList elements = elementsField.GetValue(gemCollection) as IList;
        if (elements == null)
            return -1;

        // Extract element name tá»« rewardId
        // Format há»— trá»£:
        // - "Fire" (old format - chá»‰ element name)
        // - "Fire_1" (new format - Fire_Level)
        // - "Fire_Gem_Lv1" (new format - Fire_Gem_LvX)
        string elementName = ExtractGemElementName(rewardId.Trim());

        for (int i = 0; i < elements.Count; i++)
        {
            object elem = elements[i];
            if (elem == null)
                continue;

            FieldInfo elementNameField = elem.GetType().GetField("element", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (elementNameField == null)
                continue;

            string collectionElementName = elementNameField.GetValue(elem) as string;
            if (string.IsNullOrWhiteSpace(collectionElementName))
                continue;

            if (string.Equals(elementName, collectionElementName.Trim(), StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    // Extract element name tá»« format "Fire_1" hoáº·c "Fire_Gem_Lv1" hoáº·c "Fire"
    private string ExtractGemElementName(string rewardId)
    {
        if (string.IsNullOrWhiteSpace(rewardId))
            return "";

        // TÃ¬m underscore Ä‘áº§u tiÃªn
        int underscoreIndex = rewardId.IndexOf('_');
        
        // Náº¿u khÃ´ng cÃ³ underscore, tráº£ vá» toÃ n bá»™ chuá»—i (format cÅ©: "Fire")
        if (underscoreIndex < 0)
            return rewardId;
        
        // Tráº£ vá» pháº§n trÆ°á»›c underscore Ä‘áº§u tiÃªn (format má»›i: "Fire_1" â†’ "Fire", "Fire_Gem_Lv1" â†’ "Fire")
        return rewardId.Substring(0, underscoreIndex);
    }

    // Extract gemLevel tá»« rewardId
    // Format: "Fire_1" â†’ 1, "Fire_Gem_Lv3" â†’ 3, "Fire" â†’ -1 (khÃ´ng chá»‰ Ä‘á»‹nh)
    private int ExtractGemLevelFromRewardId(string rewardId)
    {
        if (string.IsNullOrWhiteSpace(rewardId))
            return -1;

        int underscoreIndex = rewardId.IndexOf('_');
        if (underscoreIndex < 0)
            return -1; // KhÃ´ng cÃ³ underscore, level tá»« column gemlevel

        string afterUnderscore = rewardId.Substring(underscoreIndex + 1).Trim();
        
        // Try to parse "1" hoáº·c "Gem_Lv1"
        // Náº¿u lÃ  sá»‘ Ä‘Æ¡n giáº£n
        if (int.TryParse(afterUnderscore, out int level))
            return level;

        // Náº¿u lÃ  format "Gem_Lv1", tÃ¬m sá»‘ cuá»‘i cÃ¹ng
        for (int i = afterUnderscore.Length - 1; i >= 0; i--)
        {
            if (char.IsDigit(afterUnderscore[i]))
            {
                // TÃ¬m dÃ£y sá»‘ liÃªn tiáº¿p tá»« vá»‹ trÃ­ nÃ y
                int endDigitIndex = i + 1;
                int startDigitIndex = i;
                while (startDigitIndex > 0 && char.IsDigit(afterUnderscore[startDigitIndex - 1]))
                    startDigitIndex--;
                
                string numberStr = afterUnderscore.Substring(startDigitIndex, endDigitIndex - startDigitIndex);
                if (int.TryParse(numberStr, out int extractedLevel))
                    return extractedLevel;
                break;
            }
        }

        return -1;
    }

    private MapRewardType ParseRewardType(string typeText)
    {
        if (string.IsNullOrWhiteSpace(typeText))
            return MapRewardType.GOLD;

        string t = typeText.Trim().ToUpperInvariant();
        if (t == "EXP") return MapRewardType.EXP;
        if (t == "GEM") return MapRewardType.GEM;
        if (t == "DIAMOND") return MapRewardType.DIAMOND;
        return MapRewardType.GOLD;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string[] NormalizeLines(string csv)
    {
        return csv.Replace("\r\n", "\n").Replace("\r", "\n")
            .Split(new[] { '\n' }, StringSplitOptions.None);
    }

    private static Dictionary<string, int> BuildHeaderMap(List<string> header)
    {
        Dictionary<string, int> map = new Dictionary<string, int>();
        for (int i = 0; i < header.Count; i++)
        {
            string key = NormalizeHeader(header[i]);
            if (!map.ContainsKey(key))
                map.Add(key, i);
        }

        return map;
    }

    private static string GetValue(List<string> cols, Dictionary<string, int> header, string key)
    {
        if (!header.TryGetValue(key, out int index))
            return string.Empty;

        if (index < 0 || index >= cols.Count)
            return string.Empty;

        return StripQuotes(cols[index]).Trim();
    }

    private static string NormalizeHeader(string s)
    {
        if (s == null) return string.Empty;
        return s.Trim().ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
    }

    private static List<string> ParseCsvLine(string line)
    {
        List<string> fields = new List<string>();
        if (line == null) return fields;

        StringBuilder cur = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
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

    private static string StripQuotes(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        string t = s.Trim();
        if (t.Length >= 2 && t[0] == '"' && t[t.Length - 1] == '"')
            return t.Substring(1, t.Length - 2);
        return t;
    }

    private static int SafeInt(string s)
    {
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            return v;
        return 0;
    }

    private static int ParseAreaIndex(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return 0;

        string trimmed = raw.Trim();
        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int direct))
            return Mathf.Clamp(direct, 1, 7);

        // Extract digits from formats like "Name_area1"
        string digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int areaNumber))
            return Mathf.Clamp(areaNumber, 1, 7);

        return 0;
    }

    private static string SanitizeFileName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "Map_Unknown";

        string result = raw;
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            result = result.Replace(c, '_');

        return result;
    }
}
#endif

