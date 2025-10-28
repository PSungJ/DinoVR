using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class CheckBrokenAssets : EditorWindow
{
    [MenuItem("Tools/Check Broken Prefabs")]
    public static void CheckBrokenPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        List<string> brokenFiles = new List<string>();

        int count = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (obj == null)
            {
                brokenFiles.Add(path);
            }

            count++;
            if (count % 100 == 0)
                EditorUtility.DisplayProgressBar("Checking Prefabs...", $"{count}/{prefabGuids.Length}", (float)count / prefabGuids.Length);
        }

        EditorUtility.ClearProgressBar();

        if (brokenFiles.Count > 0)
        {
            Debug.LogError($"❌ 깨진 Prefab/Asset {brokenFiles.Count}개 발견됨:");
            foreach (string path in brokenFiles)
            {
                Debug.LogError($" - {path}");
            }
        }
        else
        {
            Debug.Log("✅ 모든 Prefab 파일이 정상적으로 로드됩니다!");
        }
    }
}
