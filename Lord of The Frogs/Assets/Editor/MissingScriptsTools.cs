// Assets/Editor/MissingScriptsTools.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

public static class MissingScriptsTools
{
    [MenuItem("Tools/Missing Scripts/Remove In Open Scenes")]
    static void RemoveMissingInOpenScenes()
    {
        int count = 0;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            foreach (var root in scene.GetRootGameObjects())
                count += RemoveMissingOnHierarchy(root);
        }
        Debug.Log($"Removed {count} missing script component(s) from open scenes.");
        EditorSceneManager.MarkAllScenesDirty();
    }

    [MenuItem("Tools/Missing Scripts/Remove In All Prefabs")]
    static void RemoveMissingInAllPrefabs()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab");
        int total = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) continue;

            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
            if (removed > 0)
            {
                total += removed;
                EditorUtility.SetDirty(prefab);
            }
        }
        if (total > 0) AssetDatabase.SaveAssets();
        Debug.Log($"Removed {total} missing script component(s) from prefabs.");
    }

    static int RemoveMissingOnHierarchy(GameObject root)
    {
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
        var stack = new Stack<Transform>();
        stack.Push(root.transform);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            foreach (Transform child in t) stack.Push(child);
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }
        return removed;
    }
}
