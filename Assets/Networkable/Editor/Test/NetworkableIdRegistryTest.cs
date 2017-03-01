using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Test
{
    [TestFixture]
    internal class NetworkableIdRegistryTest
    {
        class DummyClass1
        {
        }

        class DummyClass2
        {
        }

        [Test]
        public void RootRegistries()
        {
            // There should be no root registries available at start of test
            Assert.That(NetworkableIdRegistry.AllRootRegistries.Count, Is.EqualTo(0));

            // Create one root registry
            NetworkableIdRegistry.CreateRootRegistry(typeof(DummyClass1));

            // After creating a root registry for a given type, that type should be available
            Assert.That(NetworkableIdRegistry.AllRootRegistries.Count, Is.EqualTo(1));
            Assert.That(NetworkableIdRegistry.GetRootRegistry(typeof(DummyClass1)), Is.Not.Null);

            // Other classes should not yet provide a root registry
            Assert.Throws<KeyNotFoundException>(() => NetworkableIdRegistry.GetRootRegistry(typeof(DummyClass2)));

            // Remove root registry
            Assert.DoesNotThrow(() => NetworkableIdRegistry.DestroyRootRegistry(typeof(DummyClass1)));
        }

        [Test]
        public void CreateRegistry()
        {
            Assert.DoesNotThrow(() => new NetworkableIdRegistry(typeof(DummyClass1)));
        }

        [Test]
        public void AddRemoveRegistryContents()
        {
            NetworkableIdRegistry registry = new NetworkableIdRegistry(typeof(DummyClass1));

            DummyClass1 obj1 = new DummyClass1();
            DummyClass1 obj2 = new DummyClass1();

            // Add a pair of items to registry
            Assert.DoesNotThrow(() => registry.Add(obj1));
            Assert.DoesNotThrow(() => registry.Add(obj2));

            // Once added, the item is assigned an ID, and translations to/from ID match
            int id1 = registry.ToId(obj1);
            Assert.That(registry.FromId(id1), Is.EqualTo(obj1));

            // Adding the same object twice is not allowed
            Assert.Throws<ArgumentException>(() => registry.Add(obj1));

            // Remove item from registry
            registry.Remove(obj1);

            // Once removed, the item is no longer available for lookup
            Assert.Throws<ArgumentException>(() => registry.ToId(obj1));

            // Multiple removal of the same object is not allowed
            Assert.Throws<ArgumentException>(() => registry.Remove(obj1));
        }

        [Test]
        public void AddWithId()
        {
            NetworkableIdRegistry registry = new NetworkableIdRegistry(typeof(DummyClass1));

            DummyClass1 obj1 = new DummyClass1();
            DummyClass1 obj2 = new DummyClass1();

            int id1 = 10;
            int id2 = 20;

            // Add item to registry
            Assert.DoesNotThrow(() => registry.AddWithId(obj1, id1));
            Assert.That(registry.ToId(obj1), Is.EqualTo(id1));

            // Re-adding the same object with a new ID is not allowed
            Assert.Throws<ArgumentException>(() => registry.AddWithId(obj1, id2));

            // Adding a different object with the same ID is not allowed
            Assert.Throws<ArgumentException>(() => registry.AddWithId(obj2, id1));
        }
    }

}

