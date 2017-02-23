using ExitGames.Client.Photon;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

public class PhotonRegisterSerializers : NetworkableInitializer.RegisterSerializers {

    class ByValue
    {
        /// <summary>
        /// Register serialization & deserialization mechanisms for transmitting the given class by value.
        /// The Photon implementation expects that any classes are sent by-value have implemented own Serialize() and Deserialize() methods
        ///   that match the SerializeStreamMethod & DeserializeStreamMethod delegates.
        /// </summary>
        public static void RegisterSerializerAndDeserializer(Type type)
        {
            int id = NetworkableId<Type>.ToId(type);

            Debug.Log("Using id " + id + " for tagging of class " + type.Name + " in network messages when sending/receiving NetworkableByValue types");

            // Find a method Serialize(StreamBuffer, object) in class

            MethodInfo serializeMethodInfo = type.GetMethod("Serialize", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new Type[] { typeof(StreamBuffer), typeof(object) }, null);
            Assert.IsNotNull(serializeMethodInfo, "Unable to find virtual Serialize(StreamBuffer, object) method in " + type.Name);
            SerializeStreamMethod serializer = (SerializeStreamMethod)Delegate.CreateDelegate(typeof(SerializeStreamMethod), serializeMethodInfo);

            // Find a method Deserialize(StreamBuffer, short) in class

            MethodInfo deserializeMethodInfo = type.GetMethod("Deserialize", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new Type[] { typeof(StreamBuffer), typeof(short) }, null);
            Assert.IsNotNull(serializeMethodInfo, "Unable to find static Deserialize(StreamBuffer, short) method in " + type.Name);
            DeserializeStreamMethod deserializer = (DeserializeStreamMethod)Delegate.CreateDelegate(typeof(DeserializeStreamMethod), deserializeMethodInfo);

            // Register serializer and deserializer methods for class

            bool success = PhotonPeer.RegisterType(type, (byte)id, serializer, deserializer);
            Assert.IsTrue(success, "Failed registering new serialization type for " + type.Name + " with code " + id);
        }
    }

    class ById
    {
        /// <summary>
        /// All classes that are sent by-ID need the same serialization/deserialization logic: send/receive the object ID.
        /// The templated versions of Serialize()/Deserialize() ensure that the ToId()/FromId() operations are done on
        ///   the appropriate type. Other than that, the remaining serialization/deserialization logic is identical for all types.
        /// </summary>
        public class SerializerAndDeserializer<T> where T : class
        {
            static byte[] serializationBuffer = new byte[sizeof(int)];

            public static short Serialize(StreamBuffer outBuffer, object customObject)
            {
                int length = 0;
                T obj = (T)customObject;
                int id = NetworkableId<T>.ToId(obj);
                Protocol.Serialize(id, serializationBuffer, ref length);
                outBuffer.Write(serializationBuffer, 0, length);
                Debug.Log("Serializing object of type " + typeof(T).Name + " with id " + id);
                return (short)length;
            }

            static byte[] deserializationBuffer = new byte[sizeof(int)];

            public static object Deserialize(StreamBuffer inBuffer, short length)
            {
                inBuffer.Read(deserializationBuffer, 0, deserializationBuffer.Length);

                int offset = 0;
                int id;
                Protocol.Deserialize(out id, deserializationBuffer, ref offset);
                Debug.Log("Deserializing object of type " + typeof(T).Name + " with id " + id);
                T obj = NetworkableId<T>.FromId(id);
                return obj;
            }
        }

        /// <summary>
        /// Register serialization & deserialization mechanisms for transmitting the given class by ID.
        /// The given class does not need to implements its own serialization logic.
        /// </summary>
        public static void RegisterSerializerAndDeserializer(Type type)
        {
            int id = NetworkableId<Type>.ToId(type);

            Debug.Log("Using id " + id + " for tagging of class " + type.Name + " in network messages when sending/receiving NetworkableById types");

            // Construct SerializerAndDeserializer<type>

            Type serializerAndDeserializerType = typeof(SerializerAndDeserializer<>);
            Type serializerAndDeserializerTypeSpecialized = serializerAndDeserializerType.MakeGenericType(type);

            // Find a method Serialize(StreamBuffer, object) in SerializerAndDeserializer<type>

            MethodInfo serializeMethodInfo = serializerAndDeserializerTypeSpecialized.GetMethod("Serialize", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new Type[] { typeof(StreamBuffer), typeof(object) }, null);
            Assert.IsNotNull(serializeMethodInfo, "Unable to find virtual Serialize(StreamBuffer, object) method in " + serializerAndDeserializerTypeSpecialized.Name);
            SerializeStreamMethod serializer = (SerializeStreamMethod)Delegate.CreateDelegate(typeof(SerializeStreamMethod), serializeMethodInfo);

            // Find a method Deserialize(StreamBuffer, short) in SerializerAndDeserializer<type>

            MethodInfo deserializeMethodInfo = serializerAndDeserializerTypeSpecialized.GetMethod("Deserialize", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new Type[] { typeof(StreamBuffer), typeof(short) }, null);
            Assert.IsNotNull(serializeMethodInfo, "Unable to find static Deserialize(StreamBuffer, short) method in " + serializerAndDeserializerTypeSpecialized.Name);
            DeserializeStreamMethod deserializer = (DeserializeStreamMethod)Delegate.CreateDelegate(typeof(DeserializeStreamMethod), deserializeMethodInfo);

            // Register serializer and deserializer methods for class

            bool success = PhotonPeer.RegisterType(type, (byte)id, serializer, deserializer);
            Assert.IsTrue(success, "Failed registering new serialization type for " + type.Name + " with code " + id);
        }
    }

    class DefaultObject
    {
        /// <summary>
        /// Abstract base classes that are sent by-value will wrap the concrete class.
        /// Photon has concrete type lookup in Protocol.Serialize()/Protocol.Deserialize();
        ///   therefore, all that is needed is for the serializer/deserializer logic to repeat the
        ///   serialization again; this will perform serialization/deserialization of the concrete type.
        /// </summary>
        public class SerializerAndDeserializer
        {
            public static byte[] Serialize(object obj)
            {
                Debug.Log("Serializing object of an abstract base type begins, with concrete type " + obj.GetType());
                byte[] serializationBuffer = Protocol.Serialize(obj);
                Debug.Log("Serializing object of an abstract base type ends, results in " + serializationBuffer.Length + " bytes");
                return serializationBuffer;
            }

            public static object Deserialize(byte[] buffer)
            {
                Debug.Log("Deserialized object of an abstract base type begins, size " + buffer.Length);
                object customObject = Protocol.Deserialize(buffer);
                Debug.Log("Deserialized object of an abstract base type ends, with concrete type " + customObject.GetType());
                return customObject;
            }
        }

        /// <summary>
        /// Register serialization & deserialization mechanisms for transmitting an abstract base class by value.
        /// The given class does not need to implements its own serialization logic.
        /// </summary>
        public static void RegisterSerializerAndDeserializer(Type type)
        {
            int id = NetworkableId<Type>.ToId(type);

            Debug.Log("Using id " + id + " for tagging of class " + type.Name + " in network messages when sending/receiving NetworkableByValue types");

            bool success = PhotonPeer.RegisterType(type, (byte)id, SerializerAndDeserializer.Serialize, SerializerAndDeserializer.Deserialize);
            Assert.IsTrue(success, "Failed registering new serialization type for " + type.Name + " with code " + id);
        }
    }

    public void RegisterSerializersForValueType(Type type)
    {
        ByValue.RegisterSerializerAndDeserializer(type);
    }

    public void RegisterSerializersForIdType(Type type)
    {
        ById.RegisterSerializerAndDeserializer(type);
    }

    public void RegisterSerializersForDefaultObjectType(Type type)
    {
        DefaultObject.RegisterSerializerAndDeserializer(type);
    }
}
