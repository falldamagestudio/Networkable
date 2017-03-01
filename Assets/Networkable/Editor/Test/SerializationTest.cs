using ExitGames.Client.Photon;
using NUnit.Framework;
using System;
using UnityEngine;

namespace Test
{

    /// <summary>
    /// Contains a number of test cases that use Photon's serialization/deserialization to serialize & deserialize types by-value & by-id, also using class hierarchies.
    /// NOTE that at the moment the test fixture's SetUp/TearDown has problems with Photon: there is a RegisterType() but no UnregisterType() there.
    /// You need to comment out some asserts in PhotonRegisterSerializer to make the tests pass.
    /// </summary>
    [TestFixture]
    internal class SerializationTest
    {
        public static void SerializeObject(object obj, StreamBuffer outBuffer)
        {
            // Use Protocol.ProtocolDefault operations since Protocol's own have built-in size limitations
            Protocol.ProtocolDefault.Serialize(outBuffer, obj, true);
        }

        public static object DeserializeObject(StreamBuffer inBuffer)
        {
            // Use Protocol.ProtocolDefault operations since Protocol's own API does not tell us how large the deserialized object is
            object obj = Protocol.ProtocolDefault.Deserialize(inBuffer, (byte)inBuffer.ReadByte());
            return obj;
        }

        public abstract class AbstractBaseByValue
        {
        }

        public class ConcreteChildByValue : AbstractBaseByValue
        {
            public int a;
            public int b;

            public ConcreteChildByValue(int a, int b)
            {
                this.a = a;
                this.b = b;
            }

            static byte[] serializationBuffer = new byte[2 * sizeof(int)];

            public static short Serialize(StreamBuffer outBuffer, object obj)
            {
                ConcreteChildByValue obj2 = (ConcreteChildByValue)obj;

                int offset = 0;
                Protocol.Serialize(obj2.a, serializationBuffer, ref offset);
                Protocol.Serialize(obj2.b, serializationBuffer, ref offset);
                Assert.AreEqual(offset, serializationBuffer.Length);
                outBuffer.Write(serializationBuffer, 0, offset);

                return (short) offset;
            }

            static byte[] deserializationBuffer = new byte[2 * sizeof(int)];

            public static object Deserialize(StreamBuffer inBuffer, short length)
            {
                Assert.IsTrue(deserializationBuffer.Length <= length);
                inBuffer.Read(deserializationBuffer, 0, deserializationBuffer.Length);

                int offset = 0;

                int a;
                Protocol.Deserialize(out a, deserializationBuffer, ref offset);
                int b;
                Protocol.Deserialize(out b, deserializationBuffer, ref offset);

                Assert.AreEqual(offset, deserializationBuffer.Length);

                return new ConcreteChildByValue(a, b);
            }
        }

        public abstract class AbstractBaseById
        {
        }

        public class ConcreteChildById : AbstractBaseById
        {
            int a, b;

            public ConcreteChildById(int a, int b)
            {
                this.a = a;
                this.b = b;
            }
        }

        public class NullMembers
        {
            public ConcreteChildByValue nullMember1;
            public ConcreteChildById nullMember2;
            public int nonNullMember3;

            public NullMembers(ConcreteChildByValue nullMember1, ConcreteChildById nullMember2, int nonNullMember3)
            {
                this.nullMember1 = nullMember1;
                this.nullMember2 = nullMember2;
                this.nonNullMember3 = nonNullMember3;
            }

            static byte[] serializationBuffer = new byte[64];

            public static short Serialize(StreamBuffer outBuffer, object obj)
            {
                NullMembers obj2 = (NullMembers)obj;

                long startPosition = outBuffer.Position;
                SerializeObject(obj2.nullMember1, outBuffer);
                SerializeObject(obj2.nullMember1, outBuffer);

                int offset;
                byte[] buf = outBuffer.GetBufferAndAdvance(sizeof(int), out offset);
                Protocol.Serialize(obj2.nonNullMember3, buf, ref offset);

                return (short) (outBuffer.Position - startPosition);
            }

            static byte[] deinitializationBuffer = new byte[2 * sizeof(int)];

            public static object Deserialize(StreamBuffer inBuffer, short length)
            {
                ConcreteChildByValue nullMember1 = (ConcreteChildByValue) DeserializeObject(inBuffer);
                ConcreteChildById nullMember2 = (ConcreteChildById) DeserializeObject(inBuffer);
                int nonNullMember3;
                int offset;
                byte[] buf = inBuffer.GetBufferAndAdvance(sizeof(int), out offset);
                Protocol.Deserialize(out nonNullMember3, buf, ref offset);
                return new NullMembers(nullMember1, nullMember2, nonNullMember3);
            }
        }

        [SetUp]
        public void SetUp()
        {
            NetworkableId<Type>.Create(null);

            // Assign IDs for all networkable types

            NetworkableId<Type>.AddWithId(typeof(AbstractBaseByValue), 128);
            NetworkableId<Type>.AddWithId(typeof(ConcreteChildByValue), 129);
            NetworkableId<Type>.AddWithId(typeof(AbstractBaseById), 130);
            NetworkableId<Type>.AddWithId(typeof(ConcreteChildById), 131);
            NetworkableId<Type>.AddWithId(typeof(NullMembers), 132);

            // Items that get networked by ID need to have an ID hierarchy setup

            NetworkableId<AbstractBaseById>.Create(null);
            NetworkableId<ConcreteChildById>.Create(typeof(AbstractBaseById));

            // Register Photon serializer/deserializer callbacks for the types

            PhotonRegisterSerializers registerSerializers = new PhotonRegisterSerializers();

            registerSerializers.RegisterSerializersForDefaultObjectType(typeof(AbstractBaseByValue));
            registerSerializers.RegisterSerializersForValueType(typeof(ConcreteChildByValue));
            registerSerializers.RegisterSerializersForDefaultObjectType(typeof(AbstractBaseById));
            registerSerializers.RegisterSerializersForIdType(typeof(ConcreteChildById));
            registerSerializers.RegisterSerializersForValueType(typeof(NullMembers));
        }

        [TearDown]
        public void TearDown()
        {
            // Unregister Photon serializer/deserializer callbacks for the types

            PhotonRegisterSerializers registerSerializers = new PhotonRegisterSerializers();

            registerSerializers.DeregisterSerializer(typeof(NullMembers));
            registerSerializers.DeregisterSerializer(typeof(ConcreteChildById));
            registerSerializers.DeregisterSerializer(typeof(AbstractBaseById));
            registerSerializers.DeregisterSerializer(typeof(ConcreteChildByValue));
            registerSerializers.DeregisterSerializer(typeof(AbstractBaseByValue));

            // Deregister types & destroy type registries

            NetworkableId<Type>.Remove(typeof(NullMembers));

            NetworkableId<Type>.Remove(typeof(AbstractBaseById));
            NetworkableId<Type>.Remove(typeof(ConcreteChildById));

            NetworkableId<AbstractBaseById>.Destroy();
            NetworkableId<ConcreteChildById>.Destroy();
            NetworkableIdRegistry.DestroyRootRegistry(typeof(AbstractBaseById));

            NetworkableId<Type>.Remove(typeof(AbstractBaseByValue));
            NetworkableId<Type>.Remove(typeof(ConcreteChildByValue));

            NetworkableId<Type>.Destroy();
            NetworkableIdRegistry.DestroyRootRegistry(typeof(Type));
        }

        [Test]
        public void TestSerializationByValue()
        {
            ConcreteChildByValue concreteObj = new ConcreteChildByValue(12345678, 87654321);

            byte[] buffer = Protocol.Serialize(concreteObj);
            ConcreteChildByValue concreteObj2 = (ConcreteChildByValue) Protocol.Deserialize(buffer);

            Assert.That(concreteObj.a, Is.EqualTo(concreteObj2.a));
            Assert.That(concreteObj.b, Is.EqualTo(concreteObj2.b));
        }

        [Test]
        public void TestSerializationByValueNullReference()
        {
            byte[] buffer = Protocol.Serialize((ConcreteChildByValue)null);
            ConcreteChildByValue concreteObj2 = (ConcreteChildByValue)Protocol.Deserialize(buffer);

            Assert.That(concreteObj2, Is.Null);
        }

        [Test]
        public void TestSerializationByValueBaseType()
        {
            ConcreteChildByValue concreteObj = new ConcreteChildByValue(12345678, 87654321);

            byte[] buffer = Protocol.Serialize((AbstractBaseByValue) concreteObj);
            ConcreteChildByValue concreteObj2 = (ConcreteChildByValue)Protocol.Deserialize(buffer);

            Assert.That(concreteObj.a, Is.EqualTo(concreteObj2.a));
            Assert.That(concreteObj.b, Is.EqualTo(concreteObj2.b));
        }

        [Test]
        public void TestSerializationById()
        {
            ConcreteChildById concreteObj = new ConcreteChildById(12345678, 87654321);
            NetworkableId<ConcreteChildById>.Add(concreteObj);

            byte[] buffer = Protocol.Serialize(concreteObj);
            ConcreteChildById concreteObj2 = (ConcreteChildById)Protocol.Deserialize(buffer);

            Assert.That(concreteObj2, Is.EqualTo(concreteObj));

            NetworkableId<ConcreteChildById>.Remove(concreteObj);
        }

        [Test]
        public void TestSerializationByIdNullReference()
        {
            byte[] buffer = Protocol.Serialize((ConcreteChildById) null);
            ConcreteChildById concreteObj2 = (ConcreteChildById)Protocol.Deserialize(buffer);

            Assert.That(concreteObj2, Is.Null);
        }

        [Test]
        public void TestSerializationByIdBaseType()
        {
            ConcreteChildById concreteObj = new ConcreteChildById(12345678, 87654321);
            NetworkableId<ConcreteChildById>.Add(concreteObj);

            byte[] buffer = Protocol.Serialize((AbstractBaseById) concreteObj);
            ConcreteChildById concreteObj2 = (ConcreteChildById)Protocol.Deserialize(buffer);

            Assert.That(concreteObj2, Is.EqualTo(concreteObj));

            NetworkableId<ConcreteChildById>.Remove(concreteObj);
        }

        [Test]
        public void TestSerializationByValueNullMembers()
        {
            NullMembers obj = new NullMembers(null, null, 12345678);

            byte[] buffer = Protocol.Serialize(obj);
            NullMembers obj2 = (NullMembers)Protocol.Deserialize(buffer);

            Assert.That(obj2.nullMember1, Is.EqualTo(obj.nullMember1));
            Assert.That(obj2.nullMember2, Is.EqualTo(obj.nullMember2));
            Assert.That(obj2.nonNullMember3, Is.EqualTo(obj.nonNullMember3));
        }

    }

}