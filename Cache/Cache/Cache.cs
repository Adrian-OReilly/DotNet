using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Cache
{
    /// <summary>
    /// Exercise in writting an in-memory cache
    /// </summary>
    /// <Remarks> Author: Adrian O'Reilly </Remarks>
    /// <typeparam name="K">Type of key</typeparam>
    /// <typeparam name="T">Type of item to be stored</typeparam>
    public sealed class InMemoryCache<K, T> : IEnumerable, IDisposable
    {
        private static readonly object _lock = new object();

        private static InMemoryCache<K, T> _instance;

        // Hashtable to facilitate accessing the cache linked list by key
        private readonly Hashtable _ht = new Hashtable();

        // Linked list to store cache
        private readonly LinkedList<KeyValuePair<K, T>> _ll = new LinkedList<KeyValuePair<K, T>>();

        private Func<T> _retreiveFunction;

        private int _sizeLimit;

        // declare private empty constructor to prevent class being created by default public constructor
        private InMemoryCache()
        {
        }

        /// <summary>
        /// Event raised on eviction of item from cache.
        /// <remarks>
        /// Event handler should have parameters (object sender, string args),
        /// where the string parameter is the key of the item evicted
        /// </remarks>
        /// </summary>
        public event EventHandler<K> EvictionNotification;

        /// <summary>
        /// Count of items in cache
        /// </summary>
        public int Count { get { return _ll.Count(); } }

        /// <summary>
        /// Default function to retreive items if they are not in the cache
        /// and if a retreive function is not passed into Get()
        /// </summary>
        public Func<T> RetreiveFunction
        {
            get { return _retreiveFunction; }
            set { _retreiveFunction = value; }
        }

        /// <summary>
        /// Maximum number of items that can be stored in cache
        /// </summary>
        public int SizeLimit
        {
            get { return _sizeLimit; }
            set
            {
                lock (_lock)
                {
                    _sizeLimit = value;

                    // if new size limit is below the current count, then trim the cache
                    while (_ll.Count() > _sizeLimit)
                    {
                        var key = _ll.Last().Key;
                        _ht.Remove(key);
                        _ll.RemoveLast();
                        OnEviction(key);
                    }
                }
            }
        }

        /// <summary>
        /// Indexer
        /// </summary>
        /// <param name="key">Key to identify item in cache</param>
        /// <returns>Item itendified by key</returns>
        /// <exception>Null exception thrown if Key does not exist </exception>
        public T this[K key]
        {
            get
            {
                var node = (LinkedListNode<KeyValuePair<K, T>>)_ht[key];
                return (_ll.Find(node.Value)).Value.Value;
            }
        }

        /// <summary>
        /// Get an singleton instance of InMemoryCache<K, T>
        /// </summary>
        /// <param name="sizeLimit">Maximum number of items that can be stored in cache</param>
        /// <param name="retreiveFunction">Default function to retreive items if they are not in the cache</param>
        /// <returns>instance of InMemoryCache<K, T></returns>
        public static InMemoryCache<K, T> GetInstance(int sizeLimit = 1024, Func<T> retreiveFunction = null)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new InMemoryCache<K, T>();
                        _instance.SizeLimit = sizeLimit;
                        _instance.RetreiveFunction = retreiveFunction;
                    }
                }
            }

            return _instance;
        }

        /// <summary>
        /// Add an item to cache.
        /// </summary>
        /// <remarks>
        /// If the key already exists then the new entry will replace the old entry
        /// </remarks>
        /// <param name="key">Key to identify item</param>
        /// <param name="item">Item to add to cache</param>
        /// <returns></returns>
        public void Add(K key, T item)
        {
            lock (_lock)
            {
                if (_ht.Contains(key))
                {
                    // if key already exists, then remove the node and put it at the head
                    // and update the hash table
                    var node = (LinkedListNode<KeyValuePair<K, T>>)_ht[key];
                    _ll.Remove(node.Value);

                    _ht[key] = _ll.AddFirst(new KeyValuePair<K, T>(key: key, value: item));
                }
                else
                {
                    if (_ll.Count() >= _sizeLimit)
                    {
                        _ht.Remove(_ll.Last().Key);
                        _ll.RemoveLast();
                        OnEviction(key);
                    }

                    _ht.Add(key, _ll.AddFirst(new KeyValuePair<K, T>(key: key, value: item)));
                }
            }
        }

        /// <summary>
        /// Get an item from cache
        /// </summary>
        /// <param name="key">Key to identify item to retreive from cache</param>
        /// <param name="retreiveFunction">Function to retreive item if it is not in cache</param>
        /// <returns>Item identified by key</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when retreive function parameter is null and default retreive function is null </exception>
        public T Get(K key, Func<T> retreiveFunction = null)
        {
            lock (_lock)
            {
                T item;

                // if the item does not exist in the cache then retreive it
                if (!_ht.ContainsKey(key))
                {
                    // retreive using either the function passed in as a
                    // parameter, or a default function, which will need to be set previously
                    if (retreiveFunction == null)
                    {
                        if (_retreiveFunction != null)
                        {
                            retreiveFunction = _retreiveFunction;
                        }
                        else
                        {
                            throw new ArgumentNullException(nameof(retreiveFunction));
                        }
                    }

                    item = retreiveFunction();

                    // once retrieved, add the item to the cache
                    Add(key, item);
                }
                else
                {
                    var node = (LinkedListNode<KeyValuePair<K, T>>)_ht[key];

                    item = node.Value.Value;
                }

                return item;
            }
        }

        /// <summary>
        /// GetEnumerator
        /// </summary>
        /// <returns>IEnumerable</returns>
        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)_ll).GetEnumerator();
        }

        /// <summary>
        /// Remove an item from cache
        /// </summary>
        /// <param name="key">Key to identify item to remove</param>
        public void Remove(K key)
        {
            lock (_lock)
            {
                if (_ht.Contains(key))
                {
                    // if key exists,  remove the node and update the hash table
                    var node = (LinkedListNode<KeyValuePair<K, T>>)_ht[key];
                    _ll.Remove(node.Value);
                    _ht.Remove(key);

                    OnEviction(key);
                }
            }
        }

        /// <summary>
        /// Raise EvictionNotification event
        /// </summary>
        /// <param name="key"></param>
        private void OnEviction(K key)
        {
            EvictionNotification?.Invoke(this, key);
        }

        /// <summary>
        /// used for test suite, so can create a number of instances passing in different 
        /// parameters in GetInstance()
        /// </summary>
        public void Dispose()
        {
            _instance = null;
        }
    }
}
