using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    internal struct ArraySlice<T>
    {
        private readonly T[] _array;
        private int _start;
        private int _length;

        public int Length => _length;

        public ArraySlice(T[] array) : this(array, 0, array.Length)
        {
        }

        public ArraySlice(T[] array, TextSpan span) : this(array, span.Start, span.Length)
        {
        }

        public ArraySlice(T[] array, int start, int length) : this()
        {
            _array = array;
            SetStartAndLength(start, length);
        }

        public T this[int i]
        {
            get
            {
                Debug.Assert(i < _length);
                return _array[i + _start];
            }
        }

        private void SetStartAndLength(int start, int length)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(start <= _array.Length);
            Debug.Assert(length >= 0);
            Debug.Assert(start + length <= _array.Length);
            _start = start;
            _length = length;
        }

        public void MoveStartForward(int amount)
        {
            SetStartAndLength(_start + amount, _length - amount);
        }

        public void SetLength(int length)
        {
            Debug.Assert(length >= 0);
            Debug.Assert(_start + length <= _array.Length);
            _length = length;
        }
    }
}
