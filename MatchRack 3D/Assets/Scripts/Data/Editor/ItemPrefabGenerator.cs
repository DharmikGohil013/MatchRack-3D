// Path: Assets/Scripts/Data/Editor/ItemPrefabGenerator.cs
// Editor tool to generate item prefabs from Kenney food-kit FBX models.
// Menu: Tools → Generate Item Prefabs From Food Kit

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MatchItems.Editor
{
    public static class ItemPrefabGenerator
    {
        private const string FbxFolder = "Assets/kenney_food-kit/Models/FBX format";
        private const string OutputFolder = "Assets/Prefabs/Items";
        private const string ItemsLayer = "Items";
        private const string ItemTag = "Item";

        // Good food items to use as game pieces (no utensils, plates, containers, etc.)
        private static readonly string[] RecommendedModels = new string[]
        {
            // Already have: apple, banana, bread, burger, cake, carrot, cherries, cookie, donut, egg
            // New items (indexes 10-24 for levels 2 & 3):
            "broccoli",
            "corn",
            "croissant",
            "cupcake",
            "eggplant",
            "grapes",
            "hot-dog",
            "lemon",
            "mushroom",
            "orange",
            "pear",
            "pie",
            "pineapple",
            "strawberry",
            "watermelon",
            "tomato",
            "coconut",
            "muffin",
            "waffle",
            "taco",
        };

        [MenuItem("Tools/Generate Item Prefabs From Food Kit")]
        public static void GeneratePrefabs()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Items");
            }

            int itemLayer = LayerMask.NameToLayer(ItemsLayer);
            if (itemLayer < 0)
            {
                Debug.LogError($"[ItemPrefabGenerator] Layer '{ItemsLayer}' not found! Add it in Edit → Project Settings → Tags and Layers.");
                return;
            }

            // Find all FBX files in the food kit folder
            string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { FbxFolder });
            var fbxByName = new Dictionary<string, string>();
            foreach (string guid in fbxGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                fbxByName[name] = path;
            }

            int created = 0;
            int skipped = 0;

            foreach (string modelName in RecommendedModels)
            {
                string prefabPath = $"{OutputFolder}/{modelName}.prefab";

                // Skip if prefab already exists
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                {
                    skipped++;
                    continue;
                }

                // Find the FBX model
                if (!fbxByName.TryGetValue(modelName, out string fbxPath))
                {
                    Debug.LogWarning($"[ItemPrefabGenerator] FBX '{modelName}' not found in {FbxFolder}. Skipping.");
                    continue;
                }

                // Load the FBX model
                GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (modelAsset == null)
                {
                    Debug.LogWarning($"[ItemPrefabGenerator] Could not load model: {fbxPath}");
                    continue;
                }

                // Instantiate, configure, save as prefab
                GameObject instance = Object.Instantiate(modelAsset);
                instance.name = modelName;

                // Set layer and tag recursively
                SetLayerRecursive(instance, itemLayer);
                instance.tag = ItemTag;

                // Set scale to 2 (matching existing prefabs)
                instance.transform.localScale = Vector3.one * 2f;

                // Add ItemBehaviour
                if (instance.GetComponent<ItemBehaviour>() == null)
                {
                    instance.AddComponent<ItemBehaviour>();
                }

                // Add kinematic Rigidbody
                Rigidbody rb = instance.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = instance.AddComponent<Rigidbody>();
                }
                rb.isKinematic = true;
                rb.useGravity = false;

                // Add BoxCollider fitted to renderer bounds
                Renderer rend = instance.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    BoxCollider box = rend.gameObject.GetComponent<BoxCollider>();
                    if (box == null)
                    {
                        box = rend.gameObject.AddComponent<BoxCollider>();
                    }
                    box.center = rend.localBounds.center;
                    box.size = rend.localBounds.size;

                    // Ensure the renderer child also has the Items layer
                    rend.gameObject.layer = itemLayer;
                }
                else
                {
                    // Fallback: add box collider to root
                    if (instance.GetComponent<BoxCollider>() == null)
                    {
                        instance.AddComponent<BoxCollider>();
                    }
                }

                // Save as prefab
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ItemPrefabGenerator] Done! Created {created} new prefabs, skipped {skipped} existing. " +
                      $"Total in {OutputFolder}: check folder. Now click 'Auto-Populate from Prefabs/Items' on the ItemRegistry.");

            // Ping the output folder in Project window
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(OutputFolder);
            if (folder != null) EditorGUIUtility.PingObject(folder);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
