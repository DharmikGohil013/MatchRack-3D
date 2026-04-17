// Path: Assets/Scripts/Data/ItemRegistry.cs
// ScriptableObject that holds all available item prefabs.
// The array index serves as the typeID for each item.
// Create via Assets → Create → Game → ItemRegistry.

using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// Central registry of item prefabs. Index in the array == TypeID.
    /// LevelManager picks the first N types from this registry based on LevelData.uniqueItemTypes.
    /// Each prefab must have an <see cref="ItemBehaviour"/> component attached.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/ItemRegistry", fileName = "ItemRegistry")]
    public class ItemRegistry : ScriptableObject
    {
        [Tooltip("Array of item prefabs. Index = TypeID. Each must have an ItemBehaviour component.")]
        public GameObject[] itemPrefabs;

        /// <summary>
        /// Returns the prefab for the given typeID.
        /// Logs an error and returns null if out of range.
        /// </summary>
        /// <param name="typeID">Index into the itemPrefabs array.</param>
        /// <returns>The prefab GameObject, or null if invalid.</returns>
        public GameObject GetPrefab(int typeID)
        {
            if (itemPrefabs == null || typeID < 0 || typeID >= itemPrefabs.Length)
            {
                Debug.LogError($"[ItemRegistry] Invalid typeID {typeID}. Registry has {itemPrefabs?.Length ?? 0} entries.");
                return null;
            }
            return itemPrefabs[typeID];
        }

        /// <summary>Total number of registered item types.</summary>
        public int Count => itemPrefabs != null ? itemPrefabs.Length : 0;
    }
}
