using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Collections.Immutable
{
    internal class ImmutableStack<T> : IEnumerable<T>
    {
        private readonly T[] items;
        private readonly int offset;
        private readonly int count;
        private readonly ImmutableStack<T> next;

        private ImmutableStack(T[] items, int offset, ImmutableStack<T> next, int nextCount)
        {
            Debug.Assert(items != null);
            Debug.Assert(offset >= 0 && (offset < items.Length || (offset == 0 && items.Length == 0)));
            Debug.Assert(nextCount >= 0);
            this.items = items;
            this.offset = offset;
            this.next = next;
            this.count = nextCount + (items.Length - offset);
        }

        public bool IsEmpty
        {
            get
            {
                return this.count == 0;
            }
        }

        private int Count
        {
            get { return this.count; }
        }

        [ObsoleteAttribute("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  // used to hide from code coverage tools.
        public bool TryPeek(out T item)
        {
            if (this.count > 0)
            {
                item = this.items[this.offset];
                return true;
            }
            else
            {
                item = default(T);
                return false;
            }
        }

        public T Peek()
        {
            if (this.count > 0)
            {
                return this.items[this.offset];
            }

            return default(T);
        }

        public T Peek(int n)
        {
            if (n >= this.count)
            {
                throw new ArgumentOutOfRangeException();
            }

            int itemCount = this.items.Length - this.offset;
            if (n < itemCount)
            {
                return this.items[this.offset + n];
            }
            else
            {
                return this.next.Peek(n - itemCount);
            }
        }

        public ImmutableStack<T> Push(T item)
        {
            int len = this.items.Length - this.offset;
            if (len > 0 && len < 10)
            {
                var array = new T[len + 1];
                Array.Copy(this.items, this.offset, array, 1, len);
                array[0] = item;
                return new ImmutableStack<T>(array, 0, this.next, this.count - len);
            }

            return new ImmutableStack<T>(new[] { item }, 0, this, this.count);
        }

        public ImmutableStack<T> Push(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            var array = items.ToArray();
            return PushInternal(array);
        }

        internal ImmutableStack<T> PushInternal(T[] array)
        {
            if (array.Length == 0)
            {
                return this;
            }
            else
            {
                return new ImmutableStack<T>(array, 0, this, this.count);
            }
        }

        [ObsoleteAttribute("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  // used to hide from code coverage tools.
        public ImmutableStack<T> Push(params T[] items)
        {
            return Push((IEnumerable<T>)items);
        }

        public ImmutableStack<T> Pop()
        {
            if (this.offset < this.items.Length - 1)
            {
                return new ImmutableStack<T>(this.items, this.offset + 1, this.next, this.count - (this.items.Length - this.offset));
            }
            else if (this.next != null)
            {
                return this.next;
            }
            else
            {
                return Empty;
            }
        }

        [ObsoleteAttribute("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  // used to hide from code coverage tools.
        public IEnumerator<T> GetEnumerator()
        {
            for (var stack = this; stack != null; stack = stack.next)
            {
                var items = stack.items;
                if (items != null)
                {
                    for (int i = 0; i < items.Length; i++)
                    {
                        yield return items[i];
                    }
                }
            }
        }

        [ObsoleteAttribute("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  // used to hide from code coverage tools.
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private static readonly ImmutableStack<T> Empty = new ImmutableStack<T>(new T[] { }, 0, null, 0);

        // Temporary hack to allow access to the creation methods until we can use the BCL version
        internal static ImmutableStack<T> PrivateEmpty_DONOTUSE()
        {
            return Empty;
        }
    }
}