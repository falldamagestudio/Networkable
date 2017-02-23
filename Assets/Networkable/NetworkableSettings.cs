using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent data store for Networkable system.
/// Contains a list of types that are networkable, together with their assigned IDs,
///   as well as a list of static assets that are networkable, together with their assigned IDs.
/// The IDs for classes and static assets are assigned in the editor, instead of assigned at runtime because
///   then you have explicit control over when you break backwards commpabitility for your application.
/// </summary>
[CreateAssetMenu(fileName = "NetworkableSettings", menuName = "NetworkableSettings", order = 900)]
[Serializable]
public class NetworkableSettings : ScriptableObject {

    /// <summary>
    /// Range of IDs used when assigning IDs to classes (both abstract and concrete).
    /// </summary>
    const int TypeIdRangeStart = 128;
    const int TypeIdRangeEnd = 255;

    /// <summary>
    /// Range of IDs used when assigning IDs to static assets.
    /// </summary>
    const int AssetIdRangeStart = 1000;
    const int AssetIdRangeEnd = 100000;

    [Serializable]
    public struct PersistentTypeId
    {
        public string TypeName;
        public int TypeId;

        public PersistentTypeId(string typeName, int typeId)
        {
            TypeName = typeName;
            TypeId = typeId;
        }
    }

    /// <summary>
    /// Type->ID mappings for all networkable types in the application.
    /// </summary>
    [SerializeField]
    public List<PersistentTypeId> PersistentTypeIds = new List<PersistentTypeId>();

    [Serializable]
    public struct PersistentAssetId
    {
        public UnityEngine.Object Asset;
        public int Id;

        public PersistentAssetId(UnityEngine.Object asset, int id)
        {
            Asset = asset;
            Id = id;
        }
    }

    /// <summary>
    /// Asset->ID mappings for all static assets of networkable type in the application.
    /// </summary>
    [SerializeField]
    public List<PersistentAssetId> PersistentAssetIds = new List<PersistentAssetId>();

    public int NextAvailableTypeId(int searchStart)
    {
        for (int id = searchStart; id <= TypeIdRangeEnd; id++)
            if (!PersistentTypeIds.Exists(persistentTypeId => persistentTypeId.TypeId == id))
                return id;

        return -1;
    }

    public int NextAvailableAssetId(int searchStart)
    {
        for (int id = searchStart; id <= AssetIdRangeEnd; id++)
            if (!PersistentAssetIds.Exists(persistentAssetId => persistentAssetId.Id == id))
                return id;

        return -1;
    }

    /// <summary>
    /// Ensure that all types in networkableTypes have IDs assigned and are persisted.
    /// For types that already are assigned, nothing happens.
    /// Types that are not yet in the persisted list will have new IDs assigned and go into the list.
    /// Old types will not be removed or re-numbered.
    /// </summary>
    public bool AddNewPersistentTypeIds(List<Type> networkableTypes)
    {
        int availableIdSearchPosition = TypeIdRangeStart;

        foreach (Type type in networkableTypes)
        {
            if (!PersistentTypeIds.Exists(persistentTypeId => persistentTypeId.TypeName == type.FullName))
            {
                int id = NextAvailableTypeId(availableIdSearchPosition);
                if (id != -1)
                {
                    PersistentTypeIds.Add(new PersistentTypeId(type.FullName, id));
                    availableIdSearchPosition = id + 1;
                }
                else
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Clear entire list of persisted types and their IDs.
    /// </summary>
    public void RemoveAllPersistentTypeIds()
    {
        PersistentTypeIds.Clear();
    }

    /// <summary>
    /// Prune all types which are not in networkableTypes from the persisted type list.
    /// Types which are in the networkableTypes list will not be affected.
    /// </summary>
    public void RemoveUnusedPersistentTypeIds(List<Type> networkableTypes)
    {
        PersistentTypeIds.RemoveAll(persistentTypeId => !networkableTypes.Exists(networkableType => networkableType.FullName == persistentTypeId.TypeName));
    }

    /// <summary>
    /// Ensure that all assets in networkableAssets have IDs assigned and are persisted.
    /// For assets that already are persisted, nothing happens.
    /// Assets that are not yet in the persisted list will have new IDs assigned and go into the list.
    /// Old assets will not be removed or re-numbered.
    /// </summary>
    public bool AddNewPersistentAssetIds(List<UnityEngine.Object> networkableAsset)
    {
        int availableIdSearchPosition = AssetIdRangeStart;

        foreach (UnityEngine.Object asset in networkableAsset)
        {
            if (!PersistentAssetIds.Exists(persistentAssetId => persistentAssetId.Asset == asset))
            {
                int id = NextAvailableAssetId(availableIdSearchPosition);
                if (id != -1)
                {
                    PersistentAssetIds.Add(new PersistentAssetId(asset, id));
                    availableIdSearchPosition = id + 1;
                }
                else
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Clear entire list of persisted assets and their IDs.
    /// </summary>
    public void RemoveAllPersistentAssetIds()
    {
        PersistentAssetIds.Clear();
    }

    /// <summary>
    /// Prune all assets which are not in networkableAssets from the persisted asset list.
    /// Assets which are in the networkableAssets list will not be affected.
    /// </summary>
    public void RemoveUnusedPersistentAssetIds(List<UnityEngine.Object> networkableAssets)
    {
        PersistentAssetIds.RemoveAll(persistentAssetId => !networkableAssets.Exists(networkableAsset => networkableAsset == persistentAssetId.Asset));
    }
}
