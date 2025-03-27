// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#pragma warning disable 169 // unused field
#pragma warning disable 649 // field not set, will always be default value
#pragma warning disable IDE0051 // private member is unused

using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ObjectFormatterFixtures
{
    internal class Outer
    {
        public class Nested<T>
        {
            public readonly int A = 1;
            public readonly int B = 2;
            public const int S = 3;
        }
    }

    internal class A<T>
    {
        public class B<S>
        {
            public class C
            {
                public class D<Q, R>
                {
                    public class E
                    {
                    }
                }
            }
        }

        public static readonly B<T> X = new B<T>();
    }

    internal class Sort
    {
        public readonly byte ab = 1;
        public readonly sbyte aB = -1;
        public readonly short Ac = -1;
        public readonly ushort Ad = 1;
        public readonly int ad = -1;
        public readonly uint aE = 1;
        public readonly long aF = -1;
        public readonly ulong AG = 1;
    }

    internal class Signatures
    {
        public static readonly MethodInfo Arrays = typeof(Signatures).GetMethod(nameof(ArrayParameters));
        public void ArrayParameters(int[] arrayOne, int[,] arrayTwo, int[,,] arrayThree) { }
    }

    internal class RecursiveRootHidden
    {
        public readonly int A;
        public readonly int B;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public RecursiveRootHidden C;
    }

    internal class RecursiveProxy
    {
        private class Proxy
        {
            public Proxy() { }
            public Proxy(Node node)
            {
                x = node.value;
                y = node.next;
            }

            public readonly int x;
            public readonly Node y;
        }

        [DebuggerTypeProxy(typeof(Proxy))]
        public class Node
        {
            public Node(int value)
            {
                if (value < 5)
                {
                    next = new Node(value + 1);
                }
                this.value = value;
            }

            public readonly int value;
            public readonly Node next;
        }
    }

    internal class InvalidRecursiveProxy
    {
        private class Proxy
        {
            public Proxy() { }
            public Proxy(Node c) { }

            public readonly int x;
            public readonly Node p = new Node();
            public readonly int y;
        }

        [DebuggerTypeProxy(typeof(Proxy))]
        public class Node
        {
            public readonly int a;
            public readonly int b;
        }
    }

    internal class ComplexProxyBase
    {
        private int Goo()
        {
            return 1;
        }
    }

    internal class ComplexProxy : ComplexProxyBase
    {
        public ComplexProxy()
        {
        }

        public ComplexProxy(object b)
        {
        }

        [DebuggerDisplay("*1")]
        public int _02_public_property_dd { get { return 1; } }

        [DebuggerDisplay("*2")]
        private int _03_private_property_dd { get { return 1; } }

        [DebuggerDisplay("*3")]
        protected int _04_protected_property_dd { get { return 1; } }

        [DebuggerDisplay("*4")]
        internal int _05_internal_property_dd { get { return 1; } }

        [DebuggerDisplay("+1")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly int _06_public_field_dd_never;

        [DebuggerDisplay("+2")]
        private readonly int _07_private_field_dd;

        [DebuggerDisplay("+3")]
        protected readonly int _08_protected_field_dd;

        [DebuggerDisplay("+4")]
        internal readonly int _09_internal_field_dd;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly int _10_private_collapsed;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private readonly int _10_private_rootHidden;

        public readonly int _12_public;
        private readonly int _13_private;
        protected readonly int _14_protected;
        internal readonly int _15_internal;

        [DebuggerDisplay("==\r\n=\r\n=")]
        public readonly int _16_eolns;

        [DebuggerDisplay("=={==")]
        public readonly int _17_braces_0;

        [DebuggerDisplay("=={{==")]
        public readonly int _17_braces_1;

        [DebuggerDisplay("=={'{'}==")]
        public readonly int _17_braces_2;

        [DebuggerDisplay("=={'\\{'}==")]
        public readonly int _17_braces_3;

        [DebuggerDisplay("=={1/*{*/}==")]
        public readonly int _17_braces_4;

        [DebuggerDisplay("=={'{'/*\\}*/}==")]
        public readonly int _17_braces_5;

        [DebuggerDisplay("=={'{'/*}*/}==")]
        public readonly int _17_braces_6;

        [DebuggerDisplay("==\\{\\x\\t==")]
        public readonly int _19_escapes;

        [DebuggerDisplay("{1+1}")]
        public readonly int _21;

        [DebuggerDisplay("{\"xxx\"}")]
        public readonly int _22;

        [DebuggerDisplay("{\"xxx\",nq}")]
        public readonly int _23;

        [DebuggerDisplay("{'x'}")]
        public readonly int _24;

        [DebuggerDisplay("{'x',nq}")]
        public readonly int _25;

        [DebuggerDisplay("{new B()}")]
        public readonly int _26_0;

        [DebuggerDisplay("{new D()}")]
        public readonly int _26_1;

        [DebuggerDisplay("{new E()}")]
        public readonly int _26_2;

        [DebuggerDisplay("{ReturnVoid()}")]
        public readonly int _26_3;

        private void ReturnVoid() { }

        [DebuggerDisplay("{F1(1)}")]
        public readonly int _26_4;

        [DebuggerDisplay("{Goo}")]
        public readonly int _26_5;

        [DebuggerDisplay("{goo}")]
        public readonly int _26_6;

        private int goo()
        {
            return 2;
        }

        private int F1(int a) { return 1; }
        private int F2(short a) { return 2; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public readonly C _27_rootHidden = new C();

        public readonly C _28 = new C();

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public readonly C _29_collapsed = new C();

        public int _31 { get; set; }

        [CompilerGenerated]
        public readonly int _32;

        [CompilerGenerated]
        private readonly int _33;

        public int _34_Exception { get { throw new Exception("error1"); } }

        [DebuggerDisplay("-!-")]
        public int _35_Exception { get { throw new Exception("error2"); } }

        public readonly object _36 = new ToStringException();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int _37 { get { throw new Exception("error3"); } }

        public int _38_private_get_public_set { private get { return 1; } set { } }
        public int _39_public_get_private_set { get { return 1; } private set { } }
        private int _40_private_get_private_set { get { return 1; } set { } }
        private int _41_set_only_property { set { } }

        public override string ToString()
        {
            return "AStr";
        }
    }

    [DebuggerTypeProxy(typeof(ComplexProxy))]
    internal class TypeWithComplexProxy
    {
        public override string ToString()
        {
            return "BStr";
        }
    }

    [DebuggerTypeProxy(typeof(Proxy))]
    [DebuggerDisplay("DD")]
    internal class TypeWithDebuggerDisplayAndProxy
    {
        public override string ToString()
        {
            return "<ToString>";
        }

        [DebuggerDisplay("pxy")]
        private class Proxy
        {
            public Proxy(object x)
            {
            }

            public readonly int A;
            public readonly int B;
        }
    }

    internal class C
    {
        public readonly int A = 1;
        public readonly int B = 2;

        public override string ToString()
        {
            return "CStr";
        }
    }

    [DebuggerDisplay("DebuggerDisplayValue")]
    internal class BaseClassWithDebuggerDisplay
    {
    }

    internal class InheritedDebuggerDisplay : BaseClassWithDebuggerDisplay
    {
    }

    internal class ToStringException
    {
        public override string ToString()
        {
            throw new MyException();
        }
    }

    internal class MyException : Exception
    {
        public override string ToString()
        {
            return "my exception";
        }
    }

    public class ThrowingDictionary : IDictionary
    {
        private readonly int _throwAt;

        public ThrowingDictionary(int throwAt)
        {
            _throwAt = throwAt;
        }

        public void Add(object key, object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(object key)
        {
            throw new NotImplementedException();
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return new E(_throwAt);
        }

        public bool IsFixedSize
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public ICollection Keys
        {
            get { return new[] { 1, 2 }; }
        }

        public void Remove(object key)
        {
        }

        public ICollection Values
        {
            get { return new[] { 1, 2 }; }
        }

        public object this[object key]
        {
            get
            {
                return 1;
            }
            set
            {
            }
        }

        public void CopyTo(Array array, int index)
        {
        }

        public int Count
        {
            get { return 10; }
        }

        public bool IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new E(-1);
        }

        private class E : IEnumerator, IDictionaryEnumerator
        {
            private int _i;
            private readonly int _throwAt;

            public E(int throwAt)
            {
                _throwAt = throwAt;
            }

            public object Current
            {
                get { return new DictionaryEntry(_i, _i); }
            }

            public bool MoveNext()
            {
                _i++;
                if (_i == _throwAt)
                {
                    throw new Exception();
                }

                return _i < 5;
            }

            public void Reset()
            {
            }

            public DictionaryEntry Entry
            {
                get { return (DictionaryEntry)Current; }
            }

            public object Key
            {
                get { return _i; }
            }

            public object Value
            {
                get { return _i; }
            }
        }
    }

    public class ListNode
    {
        public ListNode next;
        public object data;
    }

    public class LongMembers
    {
        public readonly string LongName0123456789_0123456789_0123456789_0123456789_0123456789_0123456789_0123456789 = "hello";
        public readonly string LongValue = "0123456789_0123456789_0123456789_0123456789_0123456789_0123456789_0123456789";
    }
}
