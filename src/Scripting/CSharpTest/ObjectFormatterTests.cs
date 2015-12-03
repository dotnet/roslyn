// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests;
using ObjectFormatterFixtures;
using Roslyn.Test.Utilities;
using Xunit;
using Formatter = Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests.TestCSharpObjectFormatter;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests
{
    public class ObjectFormatterTests : ObjectFormatterTestBase
    {
        [Fact]
        public void Objects()
        {
            string str;
            object nested = new Outer.Nested<int>();

            str = Formatter.SingleLine.FormatObject(nested);
            Assert.Equal(@"Outer.Nested<int> { A=1, B=2 }", str);

            str = Formatter.Hidden.FormatObject(nested);
            Assert.Equal(@"Outer.Nested<int>", str);

            str = Formatter.Hidden.FormatObject(A<int>.X);
            Assert.Equal(@"A<int>.B<int>", str);

            object obj = new A<int>.B<bool>.C.D<string, double>.E();
            str = Formatter.Hidden.FormatObject(obj);
            Assert.Equal(@"A<int>.B<bool>.C.D<string, double>.E", str);

            var sort = new Sort();
            str = new Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit: 51).FormatObject(sort);
            Assert.Equal(@"Sort { aB=-1, ab=1, Ac=-1, Ad=1, ad=-1, aE=1, a ...", str);
            Assert.Equal(51, str.Length);

            str = new Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit: 5).FormatObject(sort);
            Assert.Equal(@"S ...", str);
            Assert.Equal(5, str.Length);

            str = new Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit: 4).FormatObject(sort);
            Assert.Equal(@"...", str);

            str = new Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit: 3).FormatObject(sort);
            Assert.Equal(@"...", str);

            str = new Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit: 2).FormatObject(sort);
            Assert.Equal(@"...", str);

            str = new Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit: 1).FormatObject(sort);
            Assert.Equal(@"...", str);

            str = new Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit: 80).FormatObject(sort);
            Assert.Equal(@"Sort { aB=-1, ab=1, Ac=-1, Ad=1, ad=-1, aE=1, aF=-1, AG=1 }", str);
        }

        [Fact]
        public void ArrayOfInt32_NoMembers()
        {
            object o = new int[4] { 3, 4, 5, 6 };
            var str = Formatter.Hidden.FormatObject(o);
            Assert.Equal("int[4] { 3, 4, 5, 6 }", str);
        }

        #region DANGER: Debugging this method under VS2010 might freeze your machine.

        [Fact]
        public void RecursiveRootHidden()
        {
            var DO_NOT_ADD_TO_WATCH_WINDOW = new RecursiveRootHidden();
            DO_NOT_ADD_TO_WATCH_WINDOW.C = DO_NOT_ADD_TO_WATCH_WINDOW;

            string str = Formatter.SingleLine.FormatObject(DO_NOT_ADD_TO_WATCH_WINDOW);
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
            string actualName = ObjectFormatterHelpers.ParseSimpleMemberName(value, 0, value.Length, out actualNoQuotes, out actualIsCallable);
            Assert.Equal(name, actualName);
            Assert.Equal(nq, actualNoQuotes);
            Assert.Equal(callable, actualIsCallable);

            actualName = ObjectFormatterHelpers.ParseSimpleMemberName("---" + value + "-", 3, 3 + value.Length, out actualNoQuotes, out actualIsCallable);
            Assert.Equal(name, actualName);
            Assert.Equal(nq, actualNoQuotes);
            Assert.Equal(callable, actualIsCallable);
        }

        [Fact]
        public void DebuggerDisplay()
        {
            string str;
            var a = new ComplexProxy();

            str = Formatter.SeparateLines.FormatObject(a);

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
            str = Formatter.SeparateLines.FormatObject(b);

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
        public void DebuggerDisplay_Inherited()
        {
            var obj = new InheritedDebuggerDisplay();

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("InheritedDebuggerDisplay(DebuggerDisplayValue)", str);
        }

        [Fact]
        public void DebuggerProxy_DebuggerDisplayAndProxy()
        {
            var obj = new TypeWithDebuggerDisplayAndProxy();

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("TypeWithDebuggerDisplayAndProxy(DD) { A=0, B=0 }", str);

            str = Formatter.SeparateLines.FormatObject(obj);
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
            str = Formatter.SeparateLines.FormatObject(obj);

            AssertMembers(str, "Node",
                "x: 0",
                "y: Node { x=1, y=Node { x=2, y=Node { x=3, y=Node { x=4, y=Node { x=5, y=null } } } } }"
            );

            obj = new InvalidRecursiveProxy.Node();
            str = Formatter.SeparateLines.FormatObject(obj);

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

            str = Formatter.SeparateLines.FormatObject(obj);

            AssertMembers(str, "object[5]",
                "1",
                "{ ... }",
                "ListNode { data={ ... }, next=ListNode { data=object[4] { 7, ListNode { ... }, 8, { ... } }, next=ListNode { ... } } }",
                "object[5] { 4, 5, { ... }, 6, ListNode { data=null, next=null } }",
                "3"
            );

            str = Formatter.SingleLine.FormatObject(obj);
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
                var str = new Formatter(MemberDisplayFormat.SingleLine, totalLengthLimit: i).FormatObject(obj);

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

            var str = new Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit: 20).FormatObject(obj);
            Assert.Equal("LongMembers { Lo ...", str);

            str = new Formatter(MemberDisplayFormat.SeparateLines, lineLengthLimit: 20).FormatObject(obj);
            Assert.Equal("LongMembers {\r\n  LongName012345 ...\r\n  LongValue: \"01 ...\r\n}\r\n", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Array()
        {
            var obj = new Object[] { new C(), 1, "str", 'c', true, null, new bool[] { true, false, true, false } };
            var str = Formatter.SeparateLines.FormatObject(obj);

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

            str = Formatter.SingleLine.FormatObject(a);
            Assert.Equal("int[2, 3, 4] { { { 0, 1, 2, 3 }, { 10, 11, 12, 13 }, { 20, 21, 22, 23 } }, { { 100, 101, 102, 103 }, { 110, 111, 112, 113 }, { 120, 121, 122, 123 } } }", str);

            str = Formatter.SeparateLines.FormatObject(a);
            AssertMembers(str, "int[2, 3, 4]",
                "{ { 0, 1, 2, 3 }, { 10, 11, 12, 13 }, { 20, 21, 22, 23 } }",
                "{ { 100, 101, 102, 103 }, { 110, 111, 112, 113 }, { 120, 121, 122, 123 } }"
            );

            int[][,][,,,] obj = new int[2][,][,,,];
            obj[0] = new int[1, 2][,,,];
            obj[0][0, 0] = new int[1, 2, 3, 4];

            str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("int[2][,][,,,] { int[1, 2][,,,] { { int[1, 2, 3, 4] { { { { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 } }, { { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 } } } }, null } }, null }", str);

            Array x = Array.CreateInstance(typeof(Object), lengths: new int[] { 2, 3 }, lowerBounds: new int[] { 2, 9 });
            str = Formatter.SingleLine.FormatObject(x);
            Assert.Equal("object[2..4, 9..12] { { null, null, null }, { null, null, null } }", str);

            Array y = Array.CreateInstance(typeof(Object), lengths: new int[] { 1, 1 }, lowerBounds: new int[] { 0, 0 });
            str = Formatter.SingleLine.FormatObject(y);
            Assert.Equal("object[1, 1] { { null } }", str);

            Array z = Array.CreateInstance(typeof(Object), lengths: new int[] { 0, 0 }, lowerBounds: new int[] { 0, 0 });
            str = Formatter.SingleLine.FormatObject(z);
            Assert.Equal("object[0, 0] { }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_IEnumerable()
        {
            string str;
            object obj;

            obj = Enumerable.Range(0, 10);
            str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("RangeIterator { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_IEnumerable_Exception()
        {
            string str;
            object obj;

            obj = Enumerable.Range(0, 10).Where(i => { if (i == 5) throw new Exception("xxx"); return i < 7; });
            str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("Enumerable.WhereEnumerableIterator<int> { 0, 1, 2, 3, 4, !<Exception> ... }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_IDictionary()
        {
            string str;
            object obj;

            obj = new ThrowingDictionary(throwAt: -1);
            str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("ThrowingDictionary(10) { { 1, 1 }, { 2, 2 }, { 3, 3 }, { 4, 4 } }", str);

            str = Formatter.SeparateLines.FormatObject(obj);
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
            str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("ThrowingDictionary(10) { { 1, 1 }, { 2, 2 }, !<Exception> ... }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_BitArray()
        {
            // BitArray doesn't have debugger proxy/display
            var obj = new System.Collections.BitArray(new int[] { 1 });
            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("BitArray(32) { true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Queue()
        {
            var obj = new Queue<int>();
            obj.Enqueue(1);
            obj.Enqueue(2);
            obj.Enqueue(3);

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("Queue<int>(3) { 1, 2, 3 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Stack()
        {
            var obj = new Stack<int>();
            obj.Push(1);
            obj.Push(2);
            obj.Push(3);

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("Stack<int>(3) { 3, 2, 1 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Dictionary()
        {
            var obj = new Dictionary<string, int>
            {
                { "x", 1 },
            };

            var str = Formatter.SeparateLines.FormatObject(obj);

            AssertMembers(str, "Dictionary<string, int>(1)",
                "{ \"x\", 1 }"
            );

            str = Formatter.SingleLine.FormatObject(obj);

            Assert.Equal("Dictionary<string, int>(1) { { \"x\", 1 } }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_KeyValuePair()
        {
            var obj = new KeyValuePair<int, string>(1, "x");

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("KeyValuePair<int, string> { 1, \"x\" }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_List()
        {
            var obj = new List<object> { 1, 2, 'c' };

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("List<object>(3) { 1, 2, 'c' }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_LinkedList()
        {
            var obj = new LinkedList<int>();
            obj.AddLast(1);
            obj.AddLast(2);
            obj.AddLast(3);

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("LinkedList<int>(3) { 1, 2, 3 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SortedList()
        {
            var obj = new SortedList<int, int>();
            obj.Add(3, 4);
            obj.Add(1, 5);
            obj.Add(2, 6);

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("SortedList<int, int>(3) { { 1, 5 }, { 2, 6 }, { 3, 4 } }", str);

            var obj2 = new SortedList<int[], int[]>();
            obj2.Add(new[] { 3 }, new int[] { 4 });

            str = Formatter.SingleLine.FormatObject(obj2);
            Assert.Equal("SortedList<int[], int[]>(1) { { int[1] { 3 }, int[1] { 4 } } }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SortedDictionary()
        {
            var obj = new SortedDictionary<int, int>();
            obj.Add(1, 0x1a);
            obj.Add(3, 0x3c);
            obj.Add(2, 0x2b);

            var str = new Formatter(MemberDisplayFormat.SingleLine, useHexadecimalNumbers: true).
                FormatObject(obj);

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
            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("HashSet<int>(Count = 2) { 1, 2 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SortedSet()
        {
            var obj = new SortedSet<int>();
            obj.Add(1);
            obj.Add(2);

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("SortedSet<int>(2) { 1, 2 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_ConcurrentDictionary()
        {
            var obj = new ConcurrentDictionary<string, int>();
            obj.AddOrUpdate("x", 1, (k, v) => v);

            var str = Formatter.SingleLine.FormatObject(obj);

            Assert.Equal("ConcurrentDictionary<string, int>(1) { { \"x\", 1 } }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_ConcurrentQueue()
        {
            var obj = new ConcurrentQueue<object>();
            obj.Enqueue(1);
            obj.Enqueue(2);
            obj.Enqueue(3);

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("ConcurrentQueue<object>(3) { 1, 2, 3 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_ConcurrentStack()
        {
            var obj = new ConcurrentStack<object>();
            obj.Push(1);
            obj.Push(2);
            obj.Push(3);

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("ConcurrentStack<object>(3) { 3, 2, 1 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_BlockingCollection()
        {
            var obj = new BlockingCollection<int>();
            obj.Add(1);
            obj.Add(2, new CancellationToken());

            var str = Formatter.SingleLine.FormatObject(obj);
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

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("ReadOnlyCollection<int>(3) { 1, 2, 3 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Lazy()
        {
            var obj = new Lazy<int[]>(() => new int[] { 1, 2 }, LazyThreadSafetyMode.None);

            // Lazy<T> has both DebuggerDisplay and DebuggerProxy attributes and both display the same information.

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("Lazy<int[]>(ThreadSafetyMode=None, IsValueCreated=false, IsValueFaulted=false, Value=null) { IsValueCreated=false, IsValueFaulted=false, Mode=None, Value=null }", str);

            str = Formatter.SeparateLines.FormatObject(obj);
            AssertMembers(str, "Lazy<int[]>(ThreadSafetyMode=None, IsValueCreated=false, IsValueFaulted=false, Value=null)",
                "IsValueCreated: false",
                "IsValueFaulted: false",
                "Mode: None",
                "Value: null"
            );

            Assert.NotNull(obj.Value);

            str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("Lazy<int[]>(ThreadSafetyMode=None, IsValueCreated=true, IsValueFaulted=false, Value=int[2] { 1, 2 }) { IsValueCreated=true, IsValueFaulted=false, Mode=None, Value=int[2] { 1, 2 } }", str);

            str = Formatter.SeparateLines.FormatObject(obj);
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

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal(
                @"Task(Id = *, Status = Created, Method = ""Void TaskMethod()"") { AsyncState=null, CancellationPending=false, CreationOptions=None, Exception=null, Id=*, Status=Created }",
                FilterDisplayString(str));

            str = Formatter.SeparateLines.FormatObject(obj);
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

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("SpinLock(IsHeld = false) { IsHeld=false, IsHeldByCurrentThread=false, OwnerThreadID=0 }", str);

            str = Formatter.SeparateLines.FormatObject(obj);
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
                var str = Formatter.SingleLine.FormatObject(obj);
                Assert.Equal("DiagnosticBag(Count = 2) { =error CS0180: 'bar' cannot be both extern and abstract, =error CS1679: Invalid extern alias for '/reference'; 'foo' is not a valid identifier }", str);

                str = Formatter.SeparateLines.FormatObject(obj);
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

            var str = Formatter.SingleLine.FormatObject(obj);
            Assert.Equal("ArrayBuilder<int>(Count = 5) { 1, 2, 3, 4, 5 }", str);

            str = Formatter.SeparateLines.FormatObject(obj);
            AssertMembers(str, "ArrayBuilder<int>(Count = 5)",
                 "1",
                 "2",
                 "3",
                 "4",
                 "5"
            );
        }
    }
}
