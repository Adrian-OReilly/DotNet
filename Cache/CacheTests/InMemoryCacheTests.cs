using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

/// <summary>
/// Test suite for InMemoryCache
/// </summary>
namespace Cache.Tests
{
    [TestClass()]
    public class InMemoryCacheTests
    {
        /// <summary>
        /// Test initilization with default values
        /// </summary>
        [TestMethod()]
        public void GetInstanceTest1()
        {
            using (var cache = InMemoryCache<string, string>.GetInstance())
            {
                Assert.IsNotNull(cache);

                Assert.IsInstanceOfType(cache, typeof(InMemoryCache<string, string>));

                Assert.AreEqual(cache.SizeLimit, 1024);

                Assert.IsNull(cache.RetreiveFunction);
            }
        }

        /// <summary>
        /// test initilizing size limit
        /// </summary>
        [TestMethod()]
        public void GetInstanceTest2()
        {
            using (var cache = InMemoryCache<string, string>.GetInstance(sizeLimit: 512))
            {
                Assert.IsNotNull(cache);

                Assert.IsInstanceOfType(cache, typeof(InMemoryCache<string, string>));

                Assert.AreEqual(cache.SizeLimit, 512);

                Assert.IsNull(cache.RetreiveFunction);

                cache.SizeLimit = 1024;

                Assert.AreEqual(cache.SizeLimit, 1024);
            }
        }

        /// <summary>
        /// Test initilization of retreive function
        /// </summary>
        [TestMethod()]
        public void GetInstanceTest3()
        {
            using (var cache = InMemoryCache<string, string>.GetInstance(retreiveFunction: () => "default value"))
            {
                Assert.IsNotNull(cache);

                Assert.IsInstanceOfType(cache, typeof(InMemoryCache<string, string>));

                Assert.AreEqual(cache.SizeLimit, 1024);

                Assert.IsNotNull(cache.RetreiveFunction);

                // verify that if the key does not exist in the cache then the value is returned by the retreive function
                Assert.AreEqual(cache.Get("KeyDoesNotExist").ToString(), "default value");
            }
        }

        /// <summary>
        /// test adding item to cache
        /// </summary>
        [TestMethod()]
        public void AddTest1()
        {
            using (var cache = InMemoryCache<string, string>.GetInstance())
            {
                cache.Add("Key1", "test string 1");
                cache.Add("Key2", "test string 2");
                cache.Add("Key3", "test string 3");

                Assert.AreEqual(cache.Count, 3);
            }
        }

        /// <summary>
        /// test adding item to cache, when the key already exists in the cache
        /// </summary>
        [TestMethod()]
        public void AddTest2()
        {
            using (var cache = InMemoryCache<string, string>.GetInstance())
            {
                cache.Add("Key1", "test string 1");

                Assert.AreEqual(cache.Get("Key1"), "test string 1");

                Assert.AreEqual(cache.Count, 1);

                cache.Add("Key1", "replace test string 1");

                Assert.AreEqual(cache.Get("Key1"), "replace test string 1");

                Assert.AreEqual(cache.Count, 1);
            }
        }

        /// <summary>
        /// test get keys when key exists in cache
        /// </summary>
        [TestMethod()]
        public void GetTest()
        {
            using (var cache = InMemoryCache<string, string>.GetInstance())
            {
                cache.Add("Key1", "test string 1");
                cache.Add("Key2", "test string 2");

                Assert.AreEqual(cache.Get("Key1"), "test string 1");
                Assert.AreEqual(cache.Get("Key2"), "test string 2");
            }
        }

        /// <summary>
        /// test get item when key does not exist in cache, using retreive function initilzed in GetInstance()
        /// </summary>
        [TestMethod()]
        public void GetTest2()
        {
            using (var cache = InMemoryCache<string, string>.GetInstance(retreiveFunction: () => "default value"))
            {
                cache.Add("Key1", "test string 1");
                cache.Add("Key2", "test string 2");

                Assert.AreEqual(cache.Get("Key1"), "test string 1");
                Assert.AreEqual(cache.Get("Key2"), "test string 2");

                // if the key is not in the cache, and a retreive function is not passed into the Get()
                // then the default retreive function will be run to provide a value
                Assert.AreEqual(cache.Get("Key3"), "default value");
            }
        }

        /// <summary>
        /// test get item when key does not exist in cache, using retreive function passed into Get()
        /// </summary>
        [TestMethod()]
        public void GetTest3()
        {
            using (var cache = InMemoryCache<string, string>.GetInstance(retreiveFunction: () => "default value"))
            {
                cache.Add("Key1", "test string 1");
                cache.Add("Key2", "test string 2");

                Assert.AreEqual(cache.Get("Key1"), "test string 1");
                Assert.AreEqual(cache.Get("Key2"), "test string 2");

                // if the key is not in the cache and a retreive function is passed into the Get()
                // then the retreive function will be run to provide a value
                Assert.AreEqual(cache.Get("Key3", retreiveFunction: () => "Item 3"), "Item 3");
            }
        }

        /// <summary>
        /// test that size of cache does not exceed limit
        /// </summary>
        [TestMethod()]
        public void SizeLimitTest1()
        {
            var sizeLimit = 4;
            using (var cache = InMemoryCache<string, string>.GetInstance(sizeLimit: sizeLimit))
            {
                cache.Add("Key1", "test string 1");
                cache.Add("Key2", "test string 2");
                cache.Add("Key3", "test string 3");
                cache.Add("Key4", "test string 4");
                cache.Add("Key5", "test string 5");
                cache.Add("Key6", "test string 6");

                Assert.AreEqual(cache.Count, sizeLimit);
            }
        }

        /// <summary>
        /// test that evicted item is least recently used
        /// </summary>
        [TestMethod()]
        public void SizeLimitTest2()
        {
            var sizeLimit = 4;
            using (var cache = InMemoryCache<string, string>.GetInstance(sizeLimit: sizeLimit, retreiveFunction: () => "default value"))
            {
                cache.Add("Key1", "test string 1");
                cache.Add("Key2", "test string 2");
                cache.Add("Key3", "test string 3");
                cache.Add("Key4", "test string 4");
                cache.Add("Key5", "test string 5");
                cache.Add("Key6", "test string 6");

                Assert.AreEqual(cache.Count, sizeLimit);

                // the last four entries should be in the cache
                Assert.AreEqual(cache.Get("Key3"), "test string 3");
                Assert.AreEqual(cache.Get("Key4"), "test string 4");
                Assert.AreEqual(cache.Get("Key5"), "test string 5");
                Assert.AreEqual(cache.Get("Key6"), "test string 6");

                // as Key1 and Key2 were the first keys put into the cache
                // they should be the first evicted
                // if they are not in the cache then the retreive function will be run to provide a value
                Assert.AreEqual(cache.Get("Key1"), "default value");
                Assert.AreEqual(cache.Get("Key2"), "default value");
            }
        }

        /// <summary>
        /// test that EvictionNotification event is raised when an item is removed from cache
        /// </summary>
        [TestMethod]
        public void EvictionNotificationRaisedTest()
        {
            var keysRemoved = new List<string>();

            using (var cache = InMemoryCache<string, string>.GetInstance())
            {
                cache.Add("Key1", "test string 1");
                cache.Add("Key2", "test string 2");
                cache.Add("Key3", "test string 3");
                cache.Add("Key4", "test string 4");

                // each time an Eviction notification event is raise add the key of the
                // removed item to keysRemoved
                cache.EvictionNotification += (object sender, string key) => keysRemoved.Add(key);

                cache.Remove("Key2");
                cache.Remove("Key3");

                // Assert that keysRemoved contains the keys of the two items removed
                Assert.AreEqual(keysRemoved.Count, 2);
                Assert.AreEqual("Key2", keysRemoved[0]);
                Assert.AreEqual("Key3", keysRemoved[1]);
            }
        }

        /// <summary>
        /// test looping through enumerator
        /// </summary>
        [TestMethod()]
        public void GetEnumeratorTest()
        {
            using (var cache = InMemoryCache<string, string>.GetInstance())
            {
                cache.Add("Key1", "test string 1");
                cache.Add("Key2", "test string 2");
                cache.Add("Key3", "test string 3");

                var cnt = 0;
                foreach (var item in cache)
                {
                    cnt++;
                }

                Assert.AreEqual(cache.Count, cnt);
            }
        }

        /// <summary>
        /// test removing an item
        /// </summary>
        [TestMethod()]
        public void RemoveTest()
        {
            using (var cache = InMemoryCache<string, string>.GetInstance())
            {
                cache.Add("Key1", "test string 1");
                cache.Add("Key2", "test string 2");
                cache.Add("Key3", "test string 3");

                Assert.AreEqual(cache.Count, 3);

                cache.Remove("Key1");

                Assert.AreEqual(cache.Count, 2);
            }
        }

        /// <summary>
        /// test accessing cache using indexer
        /// </summary>
        [TestMethod()]
        public void IndexerTest()
        {
            using (var cache = InMemoryCache<string, string>.GetInstance())
            {
                cache.Add("Key1", "test string 1");

                Assert.AreEqual(cache["Key1"], "test string 1");
            }
        }
    }
}