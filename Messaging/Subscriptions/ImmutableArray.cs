using System;
using System.Collections;
using System.Collections.Generic;

namespace Messaging.Subscriptions
{
    /// <summary>A copy-on-write array list that replaces the backing array on <see cref="Add(T)"/> and <see cref="Remove(T)"/> is called.</summary>
    [System.ComponentModel.ImmutableObject(true)]
    public struct ImmutableArray<T> : IReadOnlyCollection<T>
    {
        public static readonly ImmutableArray<T> Empty = new ImmutableArray<T>();

        readonly T[] _items;

        private ImmutableArray(T[] items)
        {
            _items = items;
        }
     
        public int Count => _items == null ? 0 : _items.Length;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
                return _items[index];
            }
        }

        public ImmutableArray<T> Add(T value)
        {
            var copy = new T[Count + 1];
            if (Count > 0)
                Array.Copy(_items, copy, _items.Length);
            copy[copy.Length - 1] = value;
            return new ImmutableArray<T>(copy);
        }

        public int IndexOf(T value)
        {
            if (_items == null) return -1;
            return Array.IndexOf(_items, value);
        }

        public int IndexOf(Predicate<T> predicate)
        {
            if (_items == null) return -1;
            int i = 0;
            foreach (var item in _items)
            {
                if (predicate(item))
                    return i;
                i++;
            }
            return -1;
        }

        public ImmutableArray<T> Remove(T value)
        {
            int index = IndexOf(value);
            if (index < 0) return this;
            return RemoveAt(index);
        }

        public ImmutableArray<T> RemoveAt(int index)
        {
            if (index < 0 || index >= Count) throw new IndexOutOfRangeException();

            var copy = new T[_items.Length - 1];
            if (index > 0)
                Array.Copy(_items, 0, copy, 0, index); // copy items before index
            if (index < _items.Length)
                Array.Copy(_items, index + 1, copy, index, _items.Length - index - 1); // copy item after index
            return new ImmutableArray<T>(copy);
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in _items)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        public IEnumerable<TOut> Select<TOut>(Func<T, TOut> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            if (_items == null)
                yield break;
            foreach (var item in _items)
                yield return func(item);
        }
    }
}