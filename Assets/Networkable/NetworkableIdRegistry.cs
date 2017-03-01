using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Each NetworkableIdRegistry represents item<->ID mappings for items of one or several types. It can assign IDs dynamically as well as
///   be set up to manage mappings for items where the IDs have been decided ahead-of-time.
/// The NetworkableIdRegistry does not understand object types and inheritance hierarchies; that is handled by NetworkableId<T>.
/// </summary>
public class NetworkableIdRegistry
{
    public static Dictionary<Type, NetworkableIdRegistry> AllRootRegistries = new Dictionary<Type, NetworkableIdRegistry>();

    public static NetworkableIdRegistry GetRootRegistry(Type type)
    {
        return AllRootRegistries[type];
    }

    public static NetworkableIdRegistry CreateRootRegistry(Type type)
    {
        Assert.IsFalse(AllRootRegistries.ContainsKey(type), "Attempted to create root NetworkableIdRegistry for root type " + type.FullName + " twice");
        NetworkableIdRegistry rootRegistry = new NetworkableIdRegistry(type);
        AllRootRegistries[type] = rootRegistry;
        return rootRegistry;
    }

    public static void DestroyRootRegistry(Type type)
    {
        Assert.IsTrue(AllRootRegistries.ContainsKey(type), "Attempted to destroy root NetworkableIdRegistry for root type " + type.FullName + " which does not exist");
        AllRootRegistries.Remove(type);
    }

    /////////////////////////////////////////////////////////////////////////////

    public Type Type;
    public Dictionary<object, int> ItemsToIds = new Dictionary<object, int>();
    public Dictionary<int, object> IdsToItems = new Dictionary<int, object>();
    public int NextFreeId = 100000;

    public NetworkableIdRegistry(Type type)
    {
        Assert.IsNotNull(type);
        Type = type;
    }

    public int GetNextFreeId()
    {
        while (IdsToItems.ContainsKey(NextFreeId))
            NextFreeId++;
        return NextFreeId++;
    }

    /// <summary>
    /// Add an item to the registry. Once added, the registry can perform item<->ID translation for that item.
    /// Adding the same item twice is not supported.
    /// </summary>
    public int Add(object item)
    {
        //Debug.Log("Adding item " + item.ToString() + " of type " + item.GetType().FullName + " to container of type " + Type.FullName);
        if (ItemsToIds.ContainsKey(item))
            throw new ArgumentException("Attempted to add the same item twice to NetworkableIdRegistry of root type " + Type.FullName);
        int id = GetNextFreeId();
        ItemsToIds.Add(item, id);
        IdsToItems.Add(id, item);
        //Debug.Log("Adding NetworkableId. Registry type: " + Type.Name + " Object type: " + item.GetType().Name + " id: " + id);
        return id;
    }

    /// <summary>
    /// Add an item to the registry, with a predetermined ID. Once added, the registry can perform item<->ID translation for that item.
    /// Adding the same item twice is not supported.
    /// Adding two items with the same ID is not supported.
    /// </summary>
    public int AddWithId(object item, int id)
    {
        //Debug.Log("Adding item " + item.ToString() + " of type " + item.GetType().FullName + " to container of type " + Type.FullName);
        if (ItemsToIds.ContainsKey(item))
            throw new ArgumentException("Attempted to add the same item twice to NetworkableIdRegistry of root type " + Type.FullName, "item");
        if (IdsToItems.ContainsKey(id))
            throw new ArgumentException("Attempted to add the same ID twice to NetworkableIdRegistry of root type " + Type.FullName, "id");
        ItemsToIds.Add(item, id);
        IdsToItems.Add(id, item);
        //Debug.Log("Adding NetworkableId. Registry type: " + Type.Name + " Object type: " + item.GetType().Name + " id: " + id);
        return id;
    }

    /// <summary>
    /// Remove an item from the registry.
    /// Removing an item which is not in the registry is not supported.
    /// </summary>
    public void Remove(object item)
    {
        if (!ItemsToIds.ContainsKey(item))
            throw new ArgumentException("Attempted to remove an item which does not exist in NetworkableIdRegistry of root type " + Type.FullName);
        int id = ItemsToIds[item];
        //Debug.Log("Removing NetworkableId. Registry type: " + Type.Name + " Object type: " + item.GetType().Name + " id: " + id);
        ItemsToIds.Remove(item);
        IdsToItems.Remove(id);
    }

    /// <summary>
    /// Translate item->ID. The item must already exist in the registry.
    /// </summary>
    public int ToId(object item)
    {
        if (!ItemsToIds.ContainsKey(item))
            throw new ArgumentException("Attempted look up an item that does not exist in NetworkableIdRegistry of root type " + Type.FullName);
        return ItemsToIds[item];
    }

    /// <summary>
    /// Translate ID->item. The item must already exist in the registry.
    /// </summary>
    public object FromId(int id)
    {
        if (!IdsToItems.ContainsKey(id))
            throw new ArgumentException("Attempted look up item for an ID that does not exist in NetworkableIdRegistry of root type " + Type.FullName);
        return IdsToItems[id];
    }
}
