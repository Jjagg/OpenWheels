using System;
using System.Collections.Generic;

namespace OpenWheels.Game
{
    /// <summary>
    /// A simple non thread-safe object pool to reset and reuse objects instead of creating new ones.
    /// Used to avoid allocation and garbage collection of objects.
    /// </summary>
    public class ObjectPool<T> where T : class, new()
    {
        private static ObjectPool<T> _shared;

        /// <summary>
        /// Retrieve a shared <see cref="ObjectPool{T}"/> instance.
        /// </summary>
        public static ObjectPool<T> Shared => System.Threading.Volatile.Read(ref _shared) ?? EnsureShared();

        private static ObjectPool<T> EnsureShared()
        {
            System.Threading.Interlocked.CompareExchange(ref _shared, new ObjectPool<T>(), null);
            return _shared;
        }

        private List<T> _objects;
        private int _capacity;
        private Action<T> _reset;

        /// <summary>
        /// Get the number of objects the pool can hold. Returns int.MaxValue in case no capacity limit is set.
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// <c>True</c> if a capacity is set for this pool. <c>False</c> if this pool can grow as large as necessary.
        /// </summary>
        public bool Limited => _capacity == int.MaxValue;

        /// <summary>
        /// Get the number of objects available in the pool.
        /// </summary>
        public int Available => _objects.Count;

        /// <summary>
        /// Invoked when a new object is created in response to a <see cref="Get" /> call.
        /// If this is invoked often, consider increasing the pool capacity or setting an unlimited capacity.
        /// </summary>
        public EventHandler<EventArgs> ObjectCreated;

        /// <summary>
        /// Create a new object pool that can grow as necessary without a capacity limit.
        /// </summary>
        /// <param name="create">Function to create an object.</param>
        /// <param name="initial">The number of objects to initialize the pool with. Defaults to 0.</param>
        public ObjectPool(int initial = 0)
            : this(null, initial)
        {
        }

        /// <summary>
        /// Create a new object pool that can grow as necessary without a capacity limit.
        /// </summary>
        /// <param name="reset">Function to reset an object when it's returned to the pool.</param>
        /// <param name="initial">The number of objects to initialize the pool with. Defaults to 0.</param>
        public ObjectPool(Action<T> reset, int initial = 0)
        {
            _capacity = int.MaxValue;
            _reset = reset;
            _objects = new List<T>();

            EnsureAvailable(initial);
        }

        /// <summary>
        /// Create a new object pool with the given capacity.
        /// </summary>
        /// <param name="capacity">The number of objects the pool can hold.</param>
        /// <param name="create">Function to create an object.</param>
        /// <param name="reset">Function to reset an object when it's returned to the pool.</param>
        /// <param name="fill">If <c>true</c> the pool is initialized with objects.</param>
        public ObjectPool(int capacity, Func<T> create, Action<T> reset, bool fill = false)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity should be larger than 0.");
            if (create == null)
                throw new ArgumentNullException(nameof(create));

            _capacity = capacity;
            _reset = reset;
            _objects = new List<T>(capacity);

            if (fill)
                EnsureAvailable(capacity);
        }

        private void EnsureAvailable(int amount)
        {
            while (_objects.Count < amount)
                _objects.Add(new T());
        }

        /// <summary>
        /// Get an object from the pool. If the pool is empty an item is created.
        /// </summary>
        /// <returns>An object from the pool.</returns>
        public T Get()
        {
            if (_objects.Count == 0)
            {
                ObjectCreated?.Invoke(this, EventArgs.Empty);
                return new T();
            }
            
            var i = _objects.Count - 1;
            var obj = _objects[i];
            _objects.RemoveAt(i);
            return obj;
        }

        /// <summary>
        /// Resets an object and returns it to the pool. If the pool is at full capacity the object is not reset and not added to the pool.
        /// </summary>
        /// <param name="obj">The object to return to the pool.</param>
        /// <returns><c>True</c> if the object was returned, <c>false</c> if the pool was full and the object was not added back to the pool.</returns>
        public bool Return(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (_objects.Count >= _capacity)
                return false;

            _reset?.Invoke(obj);
            _objects.Add(obj);
            return true;
        }
    }
}