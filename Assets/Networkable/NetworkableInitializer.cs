using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// NetworkableInitializer class performs all run-time initialization logic for the Networkable system.
/// It should be invoked once during application startup, before any network-related ID activity or serialization has occurred.
/// </summary>

public class NetworkableInitializer
{
    /// <summary>
    /// Setup that needs to be customized depending on which network stack is used (Photon, UNET etc)
    /// </summary>
    public interface RegisterSerializers
    {
        /// <summary>
        /// Register serializer and deserializer mechanisms for a concrete class that is going to be sent by-value over the network
        /// </summary>
        void RegisterSerializersForValueType(Type type);

        /// <summary>
        /// Register serializer and deserializer mechanisms for a concrete or abstract class that is going to be sent by-ID over the network
        /// </summary>
        void RegisterSerializersForIdType(Type type);

        /// <summary>
        /// Register serializer and deserializer mechanisms for an abstract base class in a [NetworkableByValue] hierarchy
        /// The base class serializer/deserializer is expected to wrap the serialization/deserialization of the concrete
        ///   class, so that the sender & receiver both think they are dealing with the base class, but the concrete type
        ///   and the internal data survives transmission over the network.
        /// </summary>
        void RegisterSerializersForDefaultObjectType(Type type);
    }

    static void RegisterNetworkableId(Type type, int id)
    {
        NetworkableId<Type>.AddWithId(type, id);
    }

    static void RegisterNetworkableIdHierarchy(Type child, Type root)
    {
        // Call NetworkableId<child>.Create(root)

        Type NetworkableIdType = typeof(NetworkableId<>);
        Type NetworkableIdTypeSpecialized = NetworkableIdType.MakeGenericType(child);

        MethodInfo createMethodInfo = NetworkableIdTypeSpecialized.GetMethod("Create", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new Type[] { typeof(Type) }, null);
        Assert.IsNotNull(createMethodInfo, "Unable to find Create(Type) method in " + NetworkableIdTypeSpecialized.Name);
        createMethodInfo.Invoke(null, new object[] { root });
    }

    /// <summary>
    /// Register IDs for all types which are listed in the NetworkableSettings.
    /// </summary>
    static void RegisterNetworkableTypes(List<Type> types, List<NetworkableSettings.PersistentTypeId> persistentTypeIds)
    {
        Dictionary<string, int> persistentTypeNameToId = new Dictionary<string, int>();
        foreach (NetworkableSettings.PersistentTypeId persistentTypeId in persistentTypeIds)
            persistentTypeNameToId[persistentTypeId.TypeName] = persistentTypeId.TypeId;

        foreach (Type type in types)
        {
            Assert.IsTrue(persistentTypeNameToId.ContainsKey(type.FullName), "NetworkableSettings is out of date: no persistent ID for type " + type.FullName);
            RegisterNetworkableId(type, persistentTypeNameToId[type.FullName]);
        }
    }

    /// <summary>
    /// Register IDs for all assets which are listed in the NetworkableSettings.
    /// This includes all assets that are visible in the Assets folder natively, all prefabs,
    ///   but not any GameObjects inside of scenes.
    /// Assets placed in Resources may or may not work - untested.
    /// </summary>
    static void RegisterNetworkableAssets(List<NetworkableSettings.PersistentAssetId> persistentAssetIds)
    {
        foreach (NetworkableSettings.PersistentAssetId persistentAssetId in persistentAssetIds)
        {
            if (persistentAssetId.Asset != null)
            {
                Type type = persistentAssetId.Asset.GetType();

                Debug.Log("Adding ID for asset. Name: " + persistentAssetId.Asset.name + " Type: " + type.FullName + " ID: " + persistentAssetId.Id);

                // Call NetworkableId<type>.AddWithId(persistentAssetId.Asset, persistentAssetId.Id)

                Type NetworkableIdType = typeof(NetworkableId<>);
                Type NetworkableIdTypeSpecialized = NetworkableIdType.MakeGenericType(type);

                MethodInfo createMethodInfo = NetworkableIdTypeSpecialized.GetMethod("AddWithId", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new Type[] { type, typeof(int) }, null);
                Assert.IsNotNull(createMethodInfo, "Unable to find AddWithId(" + type.FullName + ") method in " + NetworkableIdTypeSpecialized.Name);
                createMethodInfo.Invoke(null, new object[] { persistentAssetId.Asset, persistentAssetId.Id });
            }
            else
                Debug.LogWarning("NetworkableSettings is out of date: Persistent asset reference with id " + persistentAssetId.Id + " is null");
        }
    }


    public static void Initialize(NetworkableSettings networkableSettings, RegisterSerializers registerSerializers)
    {
        Assembly currAssembly = Assembly.GetExecutingAssembly();

        Dictionary<Type, Type> typeToRoot = new Dictionary<Type, Type>();

        List<Type> networkable = new List<Type>();
        List<Type> roots = new List<Type>();
        List<Type> children = new List<Type>();
        List<Type> needsDefaultObjectSerializers = new List<Type>();
        List<Type> needsReferenceSerializers = new List<Type>();
        List<Type> needsValueSerializers = new List<Type>();

        List<Type> loadResourcesAndInitializeIds = new List<Type>();

        foreach (Type type in currAssembly.GetTypes())
        {
            object[] attributesWithInheritance = type.GetCustomAttributes(true);
            bool isNetworkableById = Array.Exists<object>(attributesWithInheritance, attribute => attribute is NetworkableById);
            bool isNetworkableByValue = Array.Exists<object>(attributesWithInheritance, attribute => attribute is NetworkableByValue);
            bool isNetworkable = (isNetworkableById || isNetworkableByValue);

            Assert.IsFalse(isNetworkableById && isNetworkableByValue, "Class " + type.FullName + " is tagged both as NetworkableByValue and NetworkableById. This is not supported.");

            object[] baseAttributesWithInheritance = (type.BaseType != null ? type.BaseType.GetCustomAttributes(true) : new object[0]);
            bool isBaseNetworkableById = Array.Exists<object>(baseAttributesWithInheritance, attribute => attribute is NetworkableById);
            bool isBaseNetworkableByValue = Array.Exists<object>(baseAttributesWithInheritance, attribute => attribute is NetworkableByValue);
            bool isBaseNetworkable = (isBaseNetworkableById || isBaseNetworkableByValue);

            bool isNetworkableRoot = (isNetworkable && !isBaseNetworkable);

            if (isNetworkable)
            {
                networkable.Add(type);

                if (isNetworkableRoot)
                    roots.Add(type);
                else
                    children.Add(type);

                if (isNetworkableByValue && !type.IsAbstract)
                    needsValueSerializers.Add(type);
                else if (isNetworkableById)
                {
                    needsReferenceSerializers.Add(type);
                }
                else
                    needsDefaultObjectSerializers.Add(type);

                if (isNetworkableById && isNetworkableRoot)
                    loadResourcesAndInitializeIds.Add(type);
            }
        }

        foreach (Type type in networkable)
        {
            Type root = type;
            while (!roots.Exists(rootEntry => rootEntry == root))
                root = root.BaseType;

            typeToRoot[type] = root;
        }

        NetworkableId<Type>.Create(null);

        Debug.Log("========= register networkable classes =====");

        RegisterNetworkableTypes(networkable, networkableSettings.PersistentTypeIds);

        Debug.Log("========= setup NetworkableId registries for root classes =====");

        foreach (Type root in roots)
            RegisterNetworkableIdHierarchy(root, null);

        Debug.Log("========= set Networkableid registries for child classes =====");

        foreach (Type child in children)
            RegisterNetworkableIdHierarchy(child, typeToRoot[child]);

        Debug.Log("========= register byValue serializers =====");

        foreach (Type type in needsValueSerializers)
            registerSerializers.RegisterSerializersForValueType(type);

        Debug.Log("========= register byId serializers =====");

        foreach (Type type in needsReferenceSerializers)
            registerSerializers.RegisterSerializersForIdType(type);

        Debug.Log("========= register default-object serializers =====");

        foreach (Type type in needsDefaultObjectSerializers)
            registerSerializers.RegisterSerializersForDefaultObjectType(type);

        Debug.Log("========= register assets that are by-id and present in asset database =====");

        RegisterNetworkableAssets(networkableSettings.PersistentAssetIds);
    }

    /// <summary>
    /// Find all types in the application that are part of [NetworkableById] or [NetworkableByValue] hierarchies.
    /// </summary>
    public static List<Type> FindNetworkableTypesInAssembly()
    {
        Assembly currAssembly = Assembly.GetExecutingAssembly();

        List<Type> networkableTypes = new List<Type>();

        foreach (Type type in currAssembly.GetTypes())
        {
            object[] attributesWithInheritance = type.GetCustomAttributes(true);
            bool isNetworkableById = Array.Exists<object>(attributesWithInheritance, attribute => attribute is NetworkableById);
            bool isNetworkableByValue = Array.Exists<object>(attributesWithInheritance, attribute => attribute is NetworkableByValue);
            bool isNetworkable = (isNetworkableById || isNetworkableByValue);

            if (isNetworkable)
                networkableTypes.Add(type);
        }

        return networkableTypes;
    }
}
