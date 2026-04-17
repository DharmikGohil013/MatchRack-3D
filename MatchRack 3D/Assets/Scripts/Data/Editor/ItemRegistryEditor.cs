// Path: Assets/Scripts/Data/Editor/ItemRegistryEditor.cs
// Custom editor for ItemRegistry that adds a button to auto-populate prefabs.

using UnityEditor;
using UnityEngine;

namespace MatchItems.Editor
{
    [CustomEditor(typeof(ItemRegistry))]
    public class ItemRegistryEditor : UnityEditor.Editor
    {
        private const string DefaultPrefabFolder = "Assets/Prefabs/Items";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Auto-Populate from Prefabs/Items"))
            {
                AutoPopulate();
            }
        }

        private void AutoPopulate()
        {
            var registry = (ItemRegistry)target;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { DefaultPrefabFolder });

            if (guids.Length == 0)
            {
                Debug.LogWarning($"[ItemRegistryEditor] No prefabs found in {DefaultPrefabFolder}");
                return;
            }

            var prefabs = new GameObject[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            Undo.RecordObject(registry, "Auto-Populate ItemRegistry");
            registry.itemPrefabs = prefabs;
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ItemRegistryEditor] Populated {prefabs.Length} prefabs into ItemRegistry.");
        }
    }
}
