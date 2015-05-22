// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;
using SymbolDisplay = Microsoft.CodeAnalysis.CSharp.SymbolDisplay;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using ObjectFormatterFixtures;
using Microsoft.CodeAnalysis.Scripting.CSharp;

#region Fixtures


#pragma warning disable 169 // unused field
#pragma warning disable 649 // field not set, will always be default value
namespace ObjectFormatterFixtures
{
    internal class Outer
    {
        public class Nested<T>
        {
            public readonly int A = 1;
            public readonly int B = 2;
            public static readonly int S = 3;
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
            public Proxy(Node node) { x = node.value; y = node.next; }

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
        private int Foo()
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

        [DebuggerDisplay("{Foo}")]
        public readonly int _26_5;

        [DebuggerDisplay("{foo}")]
        public readonly int _26_6;

        private int foo()
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

#pragma warning restore 169 // unused field
#pragma warning restore 649 // field not set, will always be default value

#endregion

namespace Microsoft.CodeAnalysis.Scripting.UnitTests
{
    public class ObjectFormatterTests
    {
        private static readonly ObjectFormattingOptions s_hexa = new ObjectFormattingOptions(useHexadecimalNumbers: true);
        private static readonly ObjectFormattingOptions s_memberList = new ObjectFormattingOptions(memberFormat: MemberDisplayFormat.List);
        private static readonly ObjectFormattingOptions s_inline = new ObjectFormattingOptions(memberFormat: MemberDisplayFormat.Inline);

        private void AssertMembers(string str, params string[] expected)
        {
            int i = 0;
            foreach (var line in str.Split(new[] { "\r\n  " }, StringSplitOptions.None))
            {
                if (i == 0)
                {
                    Assert.Equal(expected[i] + " {", line);
                }
                else if (i == expected.Length - 1)
                {
                    Assert.Equal(expected[i] + "\r\n}\r\n", line);
                }
                else
                {
                    Assert.Equal(expected[i] + ",", line);
                }

                i++;
            }
            Assert.Equal(expected.Length, i);
        }

        private string FilterDisplayString(string str)
        {
            str = System.Text.RegularExpressions.Regex.Replace(str, @"Id = \d+", "Id = *");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"Id=\d+", "Id=*");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"Id: \d+", "Id: *");

            return str;
        }

        [Fact]
        public void Objects()
        {
            string str;
            object nested = new Outer.Nested<int>();

            str = CSharpObjectFormatter.Instance.FormatObject(nested, s_inline);
            Assert.Equal(@"Outer.Nested<int> { A=1, B=2 }", str);

            str = CSharpObjectFormatter.Instance.FormatObject(nested, new ObjectFormattingOptions(memberFormat: MemberDisplayFormat.NoMembers));
            Assert.Equal(@"Outer.Nested<int>", str);

            str = CSharpObjectFormatter.Instance.FormatObject(A<int>.X, new ObjectFormattingOptions(memberFormat: MemberDisplayFormat.NoMembers));
            Assert.Equal(@"A<int>.B<int>", str);

            object obj = new A<int>.B<bool>.C.D<string, double>.E();
            str = CSharpObjectFormatter.Instance.FormatObject(obj, new ObjectFormattingOptions(memberFormat: MemberDisplayFormat.NoMembers));
            Assert.Equal(@"A<int>.B<bool>.C.D<string, double>.E", str);

            var sort = new Sort();
            str = CSharpObjectFormatter.Instance.FormatObject(sort, new ObjectFormattingOptions(maxLineLength: 51, memberFormat: MemberDisplayFormat.Inline));
            Assert.Equal(@"Sort { aB=-1, ab=1, Ac=-1, Ad=1, ad=-1, aE=1, a ...", str);
            Assert.Equal(51, str.Length);

            str = CSharpObjectFormatter.Instance.FormatObject(sort, new ObjectFormattingOptions(maxLineLength: 5, memberFormat: MemberDisplayFormat.Inline));
            Assert.Equal(@"S ...", str);
            Assert.Equal(5, str.Length);

            str = CSharpObjectFormatter.Instance.FormatObject(sort, new ObjectFormattingOptions(maxLineLength: 4, memberFormat: MemberDisplayFormat.Inline));
            Assert.Equal(@"...", str);

            str = CSharpObjectFormatter.Instance.FormatObject(sort, new ObjectFormattingOptions(maxLineLength: 3, memberFormat: MemberDisplayFormat.Inline));
            Assert.Equal(@"...", str);

            str = CSharpObjectFormatter.Instance.FormatObject(sort, new ObjectFormattingOptions(maxLineLength: 2, memberFormat: MemberDisplayFormat.Inline));
            Assert.Equal(@"...", str);

            str = CSharpObjectFormatter.Instance.FormatObject(sort, new ObjectFormattingOptions(maxLineLength: 1, memberFormat: MemberDisplayFormat.Inline));
            Assert.Equal(@"...", str);

            str = CSharpObjectFormatter.Instance.FormatObject(sort, new ObjectFormattingOptions(maxLineLength: 80, memberFormat: MemberDisplayFormat.Inline));
            Assert.Equal(@"Sort { aB=-1, ab=1, Ac=-1, Ad=1, ad=-1, aE=1, aF=-1, AG=1 }", str);
        }

        [Fact]
        public void ArrayOtInt32_NoMembers()
        {
            CSharpObjectFormatter formatter = CSharpObjectFormatter.Instance;
            object o = new int[4] { 3, 4, 5, 6 };
            var str = formatter.FormatObject(o);
            Assert.Equal("int[4] { 3, 4, 5, 6 }", str);
        }

        #region DANGER: Debugging this method under VS2010 might freeze your machine.

        [Fact]
        public void RecursiveRootHidden()
        {
            var DO_NOT_ADD_TO_WATCH_WINDOW = new RecursiveRootHidden();
            DO_NOT_ADD_TO_WATCH_WINDOW.C = DO_NOT_ADD_TO_WATCH_WINDOW;

            string str = CSharpObjectFormatter.Instance.FormatObject(DO_NOT_ADD_TO_WATCH_WINDOW, s_inline);
            Assert.Equal(@"RecursiveRootHidden { A=0, B=0 }", str);
        }

        #endregion

        [Fact]
        public void DebuggerDisplay_ParseSimpleMemberName()
        {
            Test_ParseSimpleMemberName("foo", name: "foo", callable: false, nq: false);
            Test_ParseSimpleMemberName("foo  ", name: "foo", callable: false, nq: false);
            Test_ParseSimpleMemberName("   foo", name: "foo", callable: false, nq: false);
            Test_ParseSimpleMemberName("   foo   ", name: "foo", callable: false, nq: false);

            Test_ParseSimpleMemberName("foo()", name: "foo", callable: true, nq: false);
            Test_ParseSimpleMemberName("\nfoo (\r\n)", name: "foo", callable: true, nq: false);
            Test_ParseSimpleMemberName(" foo ( \t) ", name: "foo", callable: true, nq: false);

            Test_ParseSimpleMemberName("foo,nq", name: "foo", callable: false, nq: true);
            Test_ParseSimpleMemberName("foo  ,nq", name: "foo", callable: false, nq: true);
            Test_ParseSimpleMemberName("foo(),nq", name: "foo", callable: true, nq: true);
            Test_ParseSimpleMemberName("  foo \t( )   ,nq", name: "foo", callable: true, nq: true);
            Test_ParseSimpleMemberName("  foo \t( )   , nq", name: "foo", callable: true, nq: true);

            Test_ParseSimpleMemberName("foo,  nq", name: "foo", callable: false, nq: true);
            Test_ParseSimpleMemberName("foo(,nq", name: "foo(", callable: false, nq: true);
            Test_ParseSimpleMemberName("foo),nq", name: "foo)", callable: false, nq: true);
            Test_ParseSimpleMemberName("foo ( ,nq", name: "foo (", callable: false, nq: true);
            Test_ParseSimpleMemberName("foo ) ,nq", name: "foo )", callable: false, nq: true);

            Test_ParseSimpleMemberName(",nq", name: "", callable: false, nq: true);
            Test_ParseSimpleMemberName("  ,nq", name: "", callable: false, nq: true);
        }

        private void Test_ParseSimpleMemberName(string value, string name, bool callable, bool nq)
        {
            bool actualNoQuotes, actualIsCallable;
            string actualName = CSharpObjectFormatter.Formatter.ParseSimpleMemberName(value, 0, value.Length, out actualNoQuotes, out actualIsCallable);
            Assert.Equal(name, actualName);
            Assert.Equal(nq, actualNoQuotes);
            Assert.Equal(callable, actualIsCallable);

            actualName = CSharpObjectFormatter.Formatter.ParseSimpleMemberName("---" + value + "-", 3, 3 + value.Length, out actualNoQuotes, out actualIsCallable);
            Assert.Equal(name, actualName);
            Assert.Equal(nq, actualNoQuotes);
            Assert.Equal(callable, actualIsCallable);
        }

        [Fact]
        public void DebuggerDisplay()
        {
            string str;
            var a = new ComplexProxy();

            str = CSharpObjectFormatter.Instance.FormatObject(a, s_memberList);

            AssertMembers(str, @"[AStr]",
                @"_02_public_property_dd: *1",
                @"_03_private_property_dd: *2",
                @"_04_protected_property_dd: *3",
                @"_05_internal_property_dd: *4",
                @"_07_private_field_dd: +2",
                @"_08_protected_field_dd: +3",
                @"_09_internal_field_dd: +4",
                @"_10_private_collapsed: 0",
                @"_12_public: 0",
                @"_13_private: 0",
                @"_14_protected: 0",
                @"_15_internal: 0",
                "_16_eolns: ==\r\n=\r\n=",
                @"_17_braces_0: =={==",
                @"_17_braces_1: =={{==",
                @"_17_braces_2: ==!<Member ''{'' not found>==",
                @"_17_braces_3: ==!<Member ''\{'' not found>==",
                @"_17_braces_4: ==!<Member '1/*{*/' not found>==",
                @"_17_braces_5: ==!<Member ''{'/*\' not found>*/}==",
                @"_17_braces_6: ==!<Member ''{'/*' not found>*/}==",
                @"_19_escapes: ==\{\x\t==",
                @"_21: !<Member '1+1' not found>",
                @"_22: !<Member '""xxx""' not found>",
                @"_23: !<Member '""xxx""' not found>",
                @"_24: !<Member ''x'' not found>",
                @"_25: !<Member ''x'' not found>",
                @"_26_0: !<Method 'new B' not found>",
                @"_26_1: !<Method 'new D' not found>",
                @"_26_2: !<Method 'new E' not found>",
                @"_26_3: ",
                @"_26_4: !<Member 'F1(1)' not found>",
                @"_26_5: 1",
                @"_26_6: 2",
                @"A: 1",
                @"B: 2",
                @"_28: [CStr]",
                @"_29_collapsed: [CStr]",
                @"_31: 0",
                @"_32: 0",
                @"_33: 0",
                @"_34_Exception: !<Exception>",
                @"_35_Exception: -!-",
                @"_36: !<MyException>",
                @"_38_private_get_public_set: 1",
                @"_39_public_get_private_set: 1",
                @"_40_private_get_private_set: 1"
            );

            var b = new TypeWithComplexProxy();
            str = CSharpObjectFormatter.Instance.FormatObject(b, s_memberList);

            AssertMembers(str, @"[BStr]",
                @"_02_public_property_dd: *1",
                @"_04_protected_property_dd: *3",
                @"_08_protected_field_dd: +3",
                @"_10_private_collapsed: 0",
                @"_12_public: 0",
                @"_14_protected: 0",
                "_16_eolns: ==\r\n=\r\n=",
                @"_17_braces_0: =={==",
                @"_17_braces_1: =={{==",
                @"_17_braces_2: ==!<Member ''{'' not found>==",
                @"_17_braces_3: ==!<Member ''\{'' not found>==",
                @"_17_braces_4: ==!<Member '1/*{*/' not found>==",
                @"_17_braces_5: ==!<Member ''{'/*\' not found>*/}==",
                @"_17_braces_6: ==!<Member ''{'/*' not found>*/}==",
                @"_19_escapes: ==\{\x\t==",
                @"_21: !<Member '1+1' not found>",
                @"_22: !<Member '""xxx""' not found>",
                @"_23: !<Member '""xxx""' not found>",
                @"_24: !<Member ''x'' not found>",
                @"_25: !<Member ''x'' not found>",
                @"_26_0: !<Method 'new B' not found>",
                @"_26_1: !<Method 'new D' not found>",
                @"_26_2: !<Method 'new E' not found>",
                @"_26_3: ",
                @"_26_4: !<Member 'F1(1)' not found>",
                @"_26_5: 1",
                @"_26_6: 2",
                @"A: 1",
                @"B: 2",
                @"_28: [CStr]",
                @"_29_collapsed: [CStr]",
                @"_31: 0",
                @"_32: 0",
                @"_34_Exception: !<Exception>",
                @"_35_Exception: -!-",
                @"_36: !<MyException>",
                @"_38_private_get_public_set: 1",
                @"_39_public_get_private_set: 1"
            );
        }

        [Fact]
        public void DebuggerProxy_DebuggerDisplayAndProxy()
        {
            var obj = new TypeWithDebuggerDisplayAndProxy();

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("TypeWithDebuggerDisplayAndProxy(DD) { A=0, B=0 }", str);

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);
            AssertMembers(str, "TypeWithDebuggerDisplayAndProxy(DD)",
                "A: 0",
                "B: 0"
            );
        }

        [Fact]
        public void DebuggerProxy_Recursive()
        {
            string str;

            object obj = new RecursiveProxy.Node(0);
            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);

            AssertMembers(str, "Node",
                "x: 0",
                "y: Node { x=1, y=Node { x=2, y=Node { x=3, y=Node { x=4, y=Node { x=5, y=null } } } } }"
            );

            obj = new InvalidRecursiveProxy.Node();
            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);

            // TODO: better overflow handling
            Assert.Equal("!<Stack overflow while evaluating object>", str);
        }

        [Fact]
        public void Array_Recursive()
        {
            string str;

            ListNode n2;
            ListNode n1 = new ListNode();
            object[] obj = new object[5];
            obj[0] = 1;
            obj[1] = obj;
            obj[2] = n2 = new ListNode() { data = obj, next = n1 };
            obj[3] = new object[] { 4, 5, obj, 6, new ListNode() };
            obj[4] = 3;
            n1.next = n2;
            n1.data = new object[] { 7, n2, 8, obj };

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);

            AssertMembers(str, "object[5]",
                "1",
                "{ ... }",
                "ListNode { data={ ... }, next=ListNode { data=object[4] { 7, ListNode { ... }, 8, { ... } }, next=ListNode { ... } } }",
                "object[5] { 4, 5, { ... }, 6, ListNode { data=null, next=null } }",
                "3"
            );

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal(str, "object[5] { 1, { ... }, ListNode { data={ ... }, next=ListNode { data=object[4] { 7, ListNode { ... }, 8, { ... } }, next=ListNode { ... } } }, object[5] { 4, 5, { ... }, 6, ListNode { data=null, next=null } }, 3 }");
        }

        [Fact]
        public void LargeGraph()
        {
            var list = new LinkedList<object>();
            object obj = list;
            for (int i = 0; i < 10000; i++)
            {
                var node = list.AddFirst(i);
                var newList = new LinkedList<object>();
                list.AddAfter(node, newList);
                list = newList;
            }

            string output = "LinkedList<object>(2) { 0, LinkedList<object>(2) { 1, LinkedList<object>(2) { 2, LinkedList<object>(2) {";

            for (int i = 100; i > 4; i--)
            {
                var options = new ObjectFormattingOptions(maxOutputLength: i, memberFormat: MemberDisplayFormat.Inline);
                var str = CSharpObjectFormatter.Instance.FormatObject(obj, options);

                var expected = output.Substring(0, i - " ...".Length);
                if (!expected.EndsWith(" ", StringComparison.Ordinal))
                {
                    expected += " ";
                }

                expected += "...";

                Assert.Equal(expected, str);
            }
        }

        [Fact]
        public void LongMembers()
        {
            object obj = new LongMembers();

            var options = new ObjectFormattingOptions(maxLineLength: 20, memberFormat: MemberDisplayFormat.Inline);
            //str = ObjectFormatter.Instance.FormatObject(obj, options);
            //Assert.Equal("LongMembers { Lo ...", str);

            options = new ObjectFormattingOptions(maxLineLength: 20, memberFormat: MemberDisplayFormat.List);
            var str = CSharpObjectFormatter.Instance.FormatObject(obj, options);
            Assert.Equal("LongMembers {\r\n  LongName012345 ...\r\n  LongValue: \"01 ...\r\n}\r\n", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Array()
        {
            var obj = new Object[] { new C(), 1, "str", 'c', true, null, new bool[] { true, false, true, false } };
            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);

            AssertMembers(str, "object[7]",
                "[CStr]",
                "1",
                "\"str\"",
                "'c'",
                "true",
                "null",
                "bool[4] { true, false, true, false }"
            );
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_MdArray()
        {
            string str;

            int[,,] a = new int[2, 3, 4]
            {
                {
                    { 000, 001, 002, 003 },
                    { 010, 011, 012, 013 },
                    { 020, 021, 022, 023 },
                },
                {
                    { 100, 101, 102, 103 },
                    { 110, 111, 112, 113 },
                    { 120, 121, 122, 123 },
                }
            };

            str = CSharpObjectFormatter.Instance.FormatObject(a, s_inline);
            Assert.Equal("int[2, 3, 4] { { { 0, 1, 2, 3 }, { 10, 11, 12, 13 }, { 20, 21, 22, 23 } }, { { 100, 101, 102, 103 }, { 110, 111, 112, 113 }, { 120, 121, 122, 123 } } }", str);

            str = CSharpObjectFormatter.Instance.FormatObject(a, s_memberList);
            AssertMembers(str, "int[2, 3, 4]",
                "{ { 0, 1, 2, 3 }, { 10, 11, 12, 13 }, { 20, 21, 22, 23 } }",
                "{ { 100, 101, 102, 103 }, { 110, 111, 112, 113 }, { 120, 121, 122, 123 } }"
            );

            int[][,][,,,] obj = new int[2][,][,,,];
            obj[0] = new int[1, 2][,,,];
            obj[0][0, 0] = new int[1, 2, 3, 4];

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("int[2][,][,,,] { int[1, 2][,,,] { { int[1, 2, 3, 4] { { { { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 } }, { { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 } } } }, null } }, null }", str);

            Array x = Array.CreateInstance(typeof(Object), lengths: new int[] { 2, 3 }, lowerBounds: new int[] { 2, 9 });
            str = CSharpObjectFormatter.Instance.FormatObject(x, s_inline);
            Assert.Equal("object[2..4, 9..12] { { null, null, null }, { null, null, null } }", str);

            Array y = Array.CreateInstance(typeof(Object), lengths: new int[] { 1, 1 }, lowerBounds: new int[] { 0, 0 });
            str = CSharpObjectFormatter.Instance.FormatObject(y, s_inline);
            Assert.Equal("object[1, 1] { { null } }", str);

            Array z = Array.CreateInstance(typeof(Object), lengths: new int[] { 0, 0 }, lowerBounds: new int[] { 0, 0 });
            str = CSharpObjectFormatter.Instance.FormatObject(z, s_inline);
            Assert.Equal("object[0, 0] { }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_IEnumerable()
        {
            string str;
            object obj;

            obj = Enumerable.Range(0, 10);
            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("RangeIterator { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_IEnumerable_Exception()
        {
            string str;
            object obj;

            obj = Enumerable.Range(0, 10).Where(i => { if (i == 5) throw new Exception("xxx"); return i < 7; });
            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("Enumerable.WhereEnumerableIterator<int> { 0, 1, 2, 3, 4, !<Exception> ... }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_IDictionary()
        {
            string str;
            object obj;

            obj = new ThrowingDictionary(throwAt: -1);
            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("ThrowingDictionary(10) { { 1, 1 }, { 2, 2 }, { 3, 3 }, { 4, 4 } }", str);

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);
            AssertMembers(str, "ThrowingDictionary(10)",
                "{ 1, 1 }",
                "{ 2, 2 }",
                "{ 3, 3 }",
                "{ 4, 4 }"
            );
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_IDictionary_Exception()
        {
            string str;
            object obj;

            obj = new ThrowingDictionary(throwAt: 3);
            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("ThrowingDictionary(10) { { 1, 1 }, { 2, 2 }, !<Exception> ... }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_ArrayList()
        {
            var obj = new ArrayList { 1, 2, true, "foo" };
            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);

            Assert.Equal("ArrayList(4) { 1, 2, true, \"foo\" }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_BitArray()
        {
            // BitArray doesn't have debugger proxy/display
            var obj = new System.Collections.BitArray(new int[] { 1 });
            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("BitArray(32) { true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Hashtable()
        {
            var obj = new Hashtable
            {
                { new byte[] { 1, 2 }, new[] { 1,2,3 } },
            };

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);

            AssertMembers(str, "Hashtable(1)",
                "{ byte[2] { 1, 2 }, int[3] { 1, 2, 3 } }"
            );
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Queue()
        {
            var obj = new Queue();
            obj.Enqueue(1);
            obj.Enqueue(2);
            obj.Enqueue(3);

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("Queue(3) { 1, 2, 3 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Stack()
        {
            var obj = new Stack();
            obj.Push(1);
            obj.Push(2);
            obj.Push(3);

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("Stack(3) { 3, 2, 1 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Dictionary()
        {
            var obj = new Dictionary<string, int>
            {
                { "x", 1 },
            };

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);

            AssertMembers(str, "Dictionary<string, int>(1)",
                "{ \"x\", 1 }"
            );

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);

            Assert.Equal("Dictionary<string, int>(1) { { \"x\", 1 } }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_KeyValuePair()
        {
            var obj = new KeyValuePair<int, string>(1, "x");

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("KeyValuePair<int, string> { 1, \"x\" }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_List()
        {
            var obj = new List<object> { 1, 2, 'c' };

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("List<object>(3) { 1, 2, 'c' }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_LinkedList()
        {
            var obj = new LinkedList<int>();
            obj.AddLast(1);
            obj.AddLast(2);
            obj.AddLast(3);

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("LinkedList<int>(3) { 1, 2, 3 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SortedList()
        {
            SortedList obj = new SortedList();
            obj.Add(3, 4);
            obj.Add(1, 5);
            obj.Add(2, 6);

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("SortedList(3) { { 1, 5 }, { 2, 6 }, { 3, 4 } }", str);

            obj = new SortedList();
            obj.Add(new[] { 3 }, new int[] { 4 });

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("SortedList(1) { { int[1] { 3 }, int[1] { 4 } } }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SortedDictionary()
        {
            var obj = new SortedDictionary<int, int>();
            obj.Add(1, 0x1a);
            obj.Add(3, 0x3c);
            obj.Add(2, 0x2b);

            var str = CSharpObjectFormatter.Instance.
                FormatObject(obj, new ObjectFormattingOptions(useHexadecimalNumbers: true, memberFormat: MemberDisplayFormat.Inline));

            Assert.Equal("SortedDictionary<int, int>(3) { { 0x00000001, 0x0000001a }, { 0x00000002, 0x0000002b }, { 0x00000003, 0x0000003c } }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_HashSet()
        {
            var obj = new HashSet<int>();
            obj.Add(1);
            obj.Add(2);

            // HashSet doesn't implement ICollection (it only implements ICollection<T>) so we don't call Count, 
            // instead a DebuggerDisplay.Value is used.
            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("HashSet<int>(Count = 2) { 1, 2 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SortedSet()
        {
            var obj = new SortedSet<int>();
            obj.Add(1);
            obj.Add(2);

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("SortedSet<int>(2) { 1, 2 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_ConcurrentDictionary()
        {
            var obj = new ConcurrentDictionary<string, int>();
            obj.AddOrUpdate("x", 1, (k, v) => v);

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);

            Assert.Equal("ConcurrentDictionary<string, int>(1) { { \"x\", 1 } }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_ConcurrentQueue()
        {
            var obj = new ConcurrentQueue<object>();
            obj.Enqueue(1);
            obj.Enqueue(2);
            obj.Enqueue(3);

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("ConcurrentQueue<object>(3) { 1, 2, 3 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_ConcurrentStack()
        {
            var obj = new ConcurrentStack<object>();
            obj.Push(1);
            obj.Push(2);
            obj.Push(3);

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("ConcurrentStack<object>(3) { 3, 2, 1 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_BlockingCollection()
        {
            var obj = new BlockingCollection<int>();
            obj.Add(1);
            obj.Add(2, new CancellationToken());

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("BlockingCollection<int>(2) { 1, 2 }", str);
        }

        // TODO(tomat): this only works with System.dll file version 30319.16644 (Win8 build)
        //[Fact]
        //public void DebuggerProxy_FrameworkTypes_ConcurrentBag()
        //{
        //    var obj = new ConcurrentBag<int>();
        //    obj.Add(1);

        //    var str = ObjectFormatter.Instance.FormatObject(obj, quoteStrings: true, memberFormat: MemberDisplayFormat.Inline);
        //    Assert.Equal("ConcurrentBag<int>(1) { 1 }", str);
        //}

        [Fact]
        public void DebuggerProxy_FrameworkTypes_ReadOnlyCollection()
        {
            var obj = new System.Collections.ObjectModel.ReadOnlyCollection<int>(new[] { 1, 2, 3 });

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("ReadOnlyCollection<int>(3) { 1, 2, 3 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Lazy()
        {
            var obj = new Lazy<int[]>(() => new int[] { 1, 2 }, LazyThreadSafetyMode.None);

            // Lazy<T> has both DebuggerDisplay and DebuggerProxy attributes and both display the same information.

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("Lazy<int[]>(ThreadSafetyMode=None, IsValueCreated=false, IsValueFaulted=false, Value=null) { IsValueCreated=false, IsValueFaulted=false, Mode=None, Value=null }", str);

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);
            AssertMembers(str, "Lazy<int[]>(ThreadSafetyMode=None, IsValueCreated=false, IsValueFaulted=false, Value=null)",
                "IsValueCreated: false",
                "IsValueFaulted: false",
                "Mode: None",
                "Value: null"
            );

            Assert.NotNull(obj.Value);

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("Lazy<int[]>(ThreadSafetyMode=None, IsValueCreated=true, IsValueFaulted=false, Value=int[2] { 1, 2 }) { IsValueCreated=true, IsValueFaulted=false, Mode=None, Value=int[2] { 1, 2 } }", str);

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);
            AssertMembers(str, "Lazy<int[]>(ThreadSafetyMode=None, IsValueCreated=true, IsValueFaulted=false, Value=int[2] { 1, 2 })",
                "IsValueCreated: true",
                "IsValueFaulted: false",
                "Mode: None",
                "Value: int[2] { 1, 2 }"
            );
        }

        public void TaskMethod()
        {
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Task()
        {
            var obj = new System.Threading.Tasks.Task(TaskMethod);

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal(
                @"Task(Id = *, Status = Created, Method = ""Void TaskMethod()"") { AsyncState=null, CancellationPending=false, CreationOptions=None, Exception=null, Id=*, Status=Created }",
                FilterDisplayString(str));

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);
            AssertMembers(FilterDisplayString(str), @"Task(Id = *, Status = Created, Method = ""Void TaskMethod()"")",
                "AsyncState: null",
                "CancellationPending: false",
                "CreationOptions: None",
                "Exception: null",
                "Id: *",
                "Status: Created"
            );
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SpinLock()
        {
            var obj = new SpinLock();

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("SpinLock(IsHeld = false) { IsHeld=false, IsHeldByCurrentThread=false, OwnerThreadID=0 }", str);

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);
            AssertMembers(str, "SpinLock(IsHeld = false)",
                "IsHeld: false",
                "IsHeldByCurrentThread: false",
                "OwnerThreadID: 0"
            );
        }

        [Fact]
        public void DebuggerProxy_DiagnosticBag()
        {
            var obj = new DiagnosticBag();
            obj.Add(new DiagnosticInfo(MessageProvider.Instance, (int)ErrorCode.ERR_AbstractAndExtern, "bar"), NoLocation.Singleton);
            obj.Add(new DiagnosticInfo(MessageProvider.Instance, (int)ErrorCode.ERR_BadExternIdentifier, "foo"), NoLocation.Singleton);

            using (new EnsureEnglishUICulture())
            {
                var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
                Assert.Equal("DiagnosticBag(Count = 2) { =error CS0180: 'bar' cannot be both extern and abstract, =error CS1679: Invalid extern alias for '/reference'; 'foo' is not a valid identifier }", str);

                str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);
                AssertMembers(str, "DiagnosticBag(Count = 2)",
                     ": error CS0180: 'bar' cannot be both extern and abstract",
                     ": error CS1679: Invalid extern alias for '/reference'; 'foo' is not a valid identifier"
                );
            }
        }

        [Fact]
        public void DebuggerProxy_ArrayBuilder()
        {
            var obj = new ArrayBuilder<int>();
            obj.AddRange(new[] { 1, 2, 3, 4, 5 });

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_inline);
            Assert.Equal("ArrayBuilder<int>(Count = 5) { 1, 2, 3, 4, 5 }", str);

            str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);
            AssertMembers(str, "ArrayBuilder<int>(Count = 5)",
                 "1",
                 "2",
                 "3",
                 "4",
                 "5"
            );
        }

        [Fact]
        public void VBBackingFields_DebuggerBrowsable()
        {
            string source = @"
Imports System

Class C
   Public WithEvents WE As C
   Public Event E As Action
   Public Property A As Integer
End Class
";
            var compilation = VB.VisualBasicCompilation.Create(
                "foo",
                new[] { VB.VisualBasicSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromAssembly(typeof(object).Assembly) },
                new VB.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug));

            Assembly a;
            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);
                a = Assembly.Load(stream.ToArray());
            }

            var c = a.GetType("C");
            var obj = Activator.CreateInstance(c);

            var str = CSharpObjectFormatter.Instance.FormatObject(obj, s_memberList);
            AssertMembers(str, "C",
                "A: 0",
                "WE: null"
            );

            var attrsA = c.GetField("_A", BindingFlags.Instance | BindingFlags.NonPublic).GetCustomAttributes(typeof(DebuggerBrowsableAttribute), true);
            var attrsWE = c.GetField("_WE", BindingFlags.Instance | BindingFlags.NonPublic).GetCustomAttributes(typeof(DebuggerBrowsableAttribute), true);
            var attrsE = c.GetField("EEvent", BindingFlags.Instance | BindingFlags.NonPublic).GetCustomAttributes(typeof(DebuggerBrowsableAttribute), true);

            Assert.Equal(1, attrsA.Length);
            Assert.Equal(1, attrsWE.Length);
            Assert.Equal(1, attrsE.Length);
        }
    }
}
