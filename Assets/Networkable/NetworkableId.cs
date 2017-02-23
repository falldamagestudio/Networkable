using System;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Each NetworkableId<T> is an more type-safe and inheritance-aware version of NetworkableIdRegistry.
///
/// If a type T is marked as [NetworkableById], then the item<->ID mapping is shared between NetworkableId<T> and Networkable<child type> for all child classes.
/// This allows an item that is registered with Networkable<child type>, to also be looked up via Networkable<T>.
/// 
/// In other words, NetworkableId<T> respects inheritance and supports casting within a group of [NetworkableById] classes.
/// </summary>
public class NetworkableId<T> where T : class
{
    public static NetworkableIdRegistry rootRegistry;

    /// <summary>
    /// Prepare NetworkableId<T> for use. 
    /// If root == null, create a new registry.
    /// Otherwise, attach to the root type's registry. The root type's NetworkableId<T> must already have been initialized.
    /// </summary>
    public static void Create(Type root)
    {
        if (root != null)
        {
            rootRegistry = NetworkableIdRegistry.GetRootRegistry(root);
            Assert.IsNotNull(rootRegistry, "No NetworkableIdRegistry available for root type " + root.FullName);
        }
        else
            rootRegistry = NetworkableIdRegistry.CreateRootRegistry(typeof(T));

    }

    /// <summary>
    /// Add an item to the registry. Once added, the registry can perform item<->ID translation for that item.
    /// Adding the same item twice is not supported.
    /// </summary>
    public static int Add(T item)
    {
        Assert.IsNotNull(rootRegistry, "NetworkableId<" + typeof(T).Name + "> registry has not yet been initialized");
        return rootRegistry.Add(item);
    }

    /// <summary>
    /// Add an item to the registry, with a predetermined ID. Once added, the registry can perform item<->ID translation for that item.
    /// Adding the same item twice is not supported.
    /// Adding two items with the same ID is not supported.
    /// </summary>
    public static int AddWithId(T item, int id)
    {
        Assert.IsNotNull(rootRegistry, "NetworkableId<" + typeof(T).Name + "> registry has not yet been initialized");
        return rootRegistry.AddWithId(item, id);
    }

    /// <summary>
    /// Remove an item from the registry.
    /// Removing an item which is not in the registry is not supported.
    /// </summary>
    public static void Remove(T item)
    {
        Assert.IsNotNull(rootRegistry, "NetworkableId<" + typeof(T).Name + "> registry has not yet been initialized");
        rootRegistry.Remove(item);
    }

    /// <summary>
    /// Translate item->ID. The item must already exist in the registry.
    /// </summary>
    public static int ToId(T item)
    {
        Assert.IsNotNull(rootRegistry, "NetworkableId<" + typeof(T).Name + "> registry has not yet been initialized");
        return rootRegistry.ToId(item);
    }

    /// <summary>
    /// Translate ID->item. The item must already exist in the registry.
    /// If the item happens to be of an incompatible type (the user has effectively requested an unsupported cast), the implementation will assert.
    /// </summary>
    public static T FromId(int id)
    {
        Assert.IsNotNull(rootRegistry, "NetworkableId<" + typeof(T).Name + "> registry has not yet been initialized");
        object obj = rootRegistry.FromId(id);
        Assert.IsNotNull(obj, "Object with id " + id + " cannot be found in registry for root type " + rootRegistry.Type.Name);
        T result = obj as T;
        Assert.IsNotNull(result, "Object with id " + id + " is of type " + obj.GetType().Name + " -- this cannot be casted to type " + typeof(T).GetType().Name);
        return result;
    }
}
