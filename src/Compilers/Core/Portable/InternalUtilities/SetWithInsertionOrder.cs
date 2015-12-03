using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A set that returns the inserted values in insertion order.
    /// The mutation operations are not thread-safe.
    /// </summary>
    internal sealed class SetWithInsertionOrder<T> : IEnumerable<T>
    {
        private HashSet<T> _set = new HashSet<T>();
        private uint _nextElementValue = 0;
        private T[] _elements = null;

        public bool Add(T value)
        {
            if (!_set.Add(value)) return false;
            var thisValue = _nextElementValue++;
            if (_elements == null)
            {
                _elements = new T[10];
            }
            else if (_elements.Length <= thisValue)
            {
                Array.Resize(ref _elements, _elements.Length * 2);
            }

            _elements[thisValue] = value;
            return true;
        }

        public int Count => (int)_nextElementValue;

        public bool Contains(T value) => _set.Contains(value);

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _nextElementValue; i++) yield return _elements[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// An enumerable that yields the set's elements in insertion order.
        /// </summary>
        public SetWithInsertionOrder<T> InInsertionOrder => this;

        public ImmutableArray<T> AsImmutable()
        {
            return (_elements == null) ? ImmutableArray<T>.Empty : ImmutableArray.Create(_elements, 0, (int)_nextElementValue);
        }
    }
}
