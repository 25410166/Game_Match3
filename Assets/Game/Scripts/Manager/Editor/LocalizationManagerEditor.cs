#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;

[CustomEditor(typeof(LocalizationManager))]
public class LocalizationManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

        LocalizationManager manager = (LocalizationManager)target;

        if (GUILayout.Button("Load Data From Google Sheet"))
        {
            LoadFromGoogleSheet(manager);
        }

        if (GUILayout.Button("Apply Cached Data -> Scriptable"))
        {
            ApplyCacheToScriptable(manager);
        }

        if (GUILayout.Button("Load Data From Scriptable"))
        {
            manager.ApplyFromScriptable();
            MarkDirty(manager);
            Debug.Log("[LocalizationManagerEditor] Applied scriptable data to LocalizationManager.");
        }
    }

    private void LoadFromGoogleSheet(LocalizationManager manager)
    {
        if (manager == null)
            return;

        if (string.IsNullOrWhiteSpace(manager.SheetCsvUrl))
        {
            Debug.LogError("[LocalizationManagerEditor] Sheet CSV URL is empty.");
            return;
        }

        UnityWebRequest www = UnityWebRequest.Get(manager.SheetCsvUrl);
        var async = www.SendWebRequest();
        while (!async.isDone) { }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[LocalizationManagerEditor] Load failed: " + www.error);
            return;
        }

        int count = manager.ImportCsvDataToScriptable(www.downloadHandler.text);
        MarkDirty(manager);

        if (manager.TableData != null)
        {
            EditorUtility.SetDirty(manager.TableData);
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[LocalizationManagerEditor] Imported {count} entries from Google Sheet to LocalizationTableData.");
    }

    private void ApplyCacheToScriptable(LocalizationManager manager)
    {
        if (manager == null)
            return;

        bool copied = manager.ApplyCachedDataToScriptable();
        if (!copied)
        {
            Debug.LogWarning("[LocalizationManagerEditor] Missing LocalizationTableData on manager.");
            return;
        }

        MarkDirty(manager);
        if (manager.TableData != null)
        {
            EditorUtility.SetDirty(manager.TableData);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[LocalizationManagerEditor] Cached data copied to LocalizationTableData.");
    }

    private void MarkDirty(LocalizationManager manager)
    {
        EditorUtility.SetDirty(manager);

        if (manager.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
    }
}
#endif
