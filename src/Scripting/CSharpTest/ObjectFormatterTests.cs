// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests;
using ObjectFormatterFixtures;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests
{
    public class ObjectFormatterTests : ObjectFormatterTestBase
    {
        private static readonly TestCSharpObjectFormatter s_formatter = new TestCSharpObjectFormatter();

        [Fact]
        public void Objects()
        {
            string str;
            object nested = new Outer.Nested<int>();

            str = s_formatter.FormatObject(nested, SingleLineOptions);
            Assert.Equal(@"Outer.Nested<int> { A=1, B=2 }", str);

            str = s_formatter.FormatObject(nested, HiddenOptions);
            Assert.Equal(@"Outer.Nested<int>", str);

            str = s_formatter.FormatObject(A<int>.X, HiddenOptions);
            Assert.Equal(@"A<int>.B<int>", str);

            object obj = new A<int>.B<bool>.C.D<string, double>.E();
            str = s_formatter.FormatObject(obj, HiddenOptions);
            Assert.Equal(@"A<int>.B<bool>.C.D<string, double>.E", str);

            var sort = new Sort();
            str = new TestCSharpObjectFormatter(maximumLineLength: 51).FormatObject(sort, SingleLineOptions);
            Assert.Equal(@"Sort { aB=-1, ab=1, Ac=-1, Ad=1, ad=-1, aE=1, aF=-1...", str);
            Assert.Equal(51 + 3, str.Length);

            str = new TestCSharpObjectFormatter(maximumLineLength: 5).FormatObject(sort, SingleLineOptions);
            Assert.Equal(@"Sort ...", str);
            Assert.Equal(5 + 3, str.Length);

            str = new TestCSharpObjectFormatter(maximumLineLength: 4).FormatObject(sort, SingleLineOptions);
            Assert.Equal(@"Sort...", str);

            str = new TestCSharpObjectFormatter(maximumLineLength: 3).FormatObject(sort, SingleLineOptions);
            Assert.Equal(@"Sor...", str);

            str = new TestCSharpObjectFormatter(maximumLineLength: 2).FormatObject(sort, SingleLineOptions);
            Assert.Equal(@"So...", str);

            str = new TestCSharpObjectFormatter(maximumLineLength: 1).FormatObject(sort, SingleLineOptions);
            Assert.Equal(@"S...", str);

            str = new TestCSharpObjectFormatter(maximumLineLength: 80).FormatObject(sort, SingleLineOptions);
            Assert.Equal(@"Sort { aB=-1, ab=1, Ac=-1, Ad=1, ad=-1, aE=1, aF=-1, AG=1 }", str);
        }

        [Fact]
        public void TupleType()
        {
            var tup = new Tuple<int, int>(1, 2);
            Assert.Equal("(1, 2)", s_formatter.FormatObject(tup));
        }

        [Fact]
        public void ValueTupleType()
        {
            (int, int) tup = (1, 2);
            Assert.Equal("(1, 2)", s_formatter.FormatObject(tup));
        }

        [Fact]
        public void ArrayMethodParameters()
        {
            var result = s_formatter.FormatMethodSignature(Signatures.Arrays);
            Assert.Equal("ObjectFormatterFixtures.Signatures.ArrayParameters(int[], int[,], int[,,])", result);
        }

        [Fact]
        public void ArrayOfInt32_NoMembers()
        {
            object o = new int[4] { 3, 4, 5, 6 };
            var str = s_formatter.FormatObject(o, HiddenOptions);
            Assert.Equal("int[4] { 3, 4, 5, 6 }", str);
        }

        #region DANGER: Debugging this method under VS2010 might freeze your machine.

        [Fact]
        public void RecursiveRootHidden()
        {
            var DO_NOT_ADD_TO_WATCH_WINDOW = new RecursiveRootHidden();
            DO_NOT_ADD_TO_WATCH_WINDOW.C = DO_NOT_ADD_TO_WATCH_WINDOW;

            string str = s_formatter.FormatObject(DO_NOT_ADD_TO_WATCH_WINDOW, SingleLineOptions);
            Assert.Equal(@"RecursiveRootHidden { A=0, B=0 }", str);
        }

        #endregion

        [Fact]
        public void DebuggerDisplay_ParseSimpleMemberName()
        {
            Test_ParseSimpleMemberName("goo", name: "goo", callable: false, nq: false);
            Test_ParseSimpleMemberName("goo  ", name: "goo", callable: false, nq: false);
            Test_ParseSimpleMemberName("   goo", name: "goo", callable: false, nq: false);
            Test_ParseSimpleMemberName("   goo   ", name: "goo", callable: false, nq: false);

            Test_ParseSimpleMemberName("goo()", name: "goo", callable: true, nq: false);
            Test_ParseSimpleMemberName("\ngoo (\r\n)", name: "goo", callable: true, nq: false);
            Test_ParseSimpleMemberName(" goo ( \t) ", name: "goo", callable: true, nq: false);

            Test_ParseSimpleMemberName("goo,nq", name: "goo", callable: false, nq: true);
            Test_ParseSimpleMemberName("goo  ,nq", name: "goo", callable: false, nq: true);
            Test_ParseSimpleMemberName("goo(),nq", name: "goo", callable: true, nq: true);
            Test_ParseSimpleMemberName("  goo \t( )   ,nq", name: "goo", callable: true, nq: true);
            Test_ParseSimpleMemberName("  goo \t( )   , nq", name: "goo", callable: true, nq: true);

            Test_ParseSimpleMemberName("goo,  nq", name: "goo", callable: false, nq: true);
            Test_ParseSimpleMemberName("goo(,nq", name: "goo(", callable: false, nq: true);
            Test_ParseSimpleMemberName("goo),nq", name: "goo)", callable: false, nq: true);
            Test_ParseSimpleMemberName("goo ( ,nq", name: "goo (", callable: false, nq: true);
            Test_ParseSimpleMemberName("goo ) ,nq", name: "goo )", callable: false, nq: true);

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

            str = s_formatter.FormatObject(a, SeparateLinesOptions);

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
            str = s_formatter.FormatObject(b, SeparateLinesOptions);

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

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("InheritedDebuggerDisplay(DebuggerDisplayValue)", str);
        }

        [Fact]
        public void DebuggerProxy_DebuggerDisplayAndProxy()
        {
            var obj = new TypeWithDebuggerDisplayAndProxy();

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("TypeWithDebuggerDisplayAndProxy(DD) { A=0, B=0 }", str);

            str = s_formatter.FormatObject(obj, SeparateLinesOptions);
            AssertMembers(str, "TypeWithDebuggerDisplayAndProxy(DD)",
                "A: 0",
                "B: 0"
            );
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10816")]
        public void DebuggerProxy_Recursive()
        {
            string str;

            object obj = new RecursiveProxy.Node(0);
            str = s_formatter.FormatObject(obj, SeparateLinesOptions);

            AssertMembers(str, "RecursiveProxy.Node",
                "x: 0",
                "y: RecursiveProxy.Node { x=1, y=RecursiveProxy.Node { x=2, y=RecursiveProxy.Node { x=3, y=RecursiveProxy.Node { x=4, y=RecursiveProxy.Node { x=5, y=null } } } } }"
            );

            obj = new InvalidRecursiveProxy.Node();
            str = s_formatter.FormatObject(obj, SeparateLinesOptions);

            // TODO: better overflow handling
            Assert.Equal(ScriptingResources.StackOverflowWhileEvaluating, str);
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

            str = s_formatter.FormatObject(obj, SeparateLinesOptions);

            AssertMembers(str, "object[5]",
                "1",
                "{ ... }",
                "ListNode { data={ ... }, next=ListNode { data=object[4] { 7, ListNode { ... }, 8, { ... } }, next=ListNode { ... } } }",
                "object[5] { 4, 5, { ... }, 6, ListNode { data=null, next=null } }",
                "3"
            );

            str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("object[5] { 1, { ... }, ListNode { data={ ... }, next=ListNode { data=object[4] { 7, ListNode { ... }, 8, { ... } }, next=ListNode { ... } } }, object[5] { 4, 5, { ... }, 6, ListNode { data=null, next=null } }, 3 }", str);
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
                var printOptions = new PrintOptions
                {
                    MaximumOutputLength = i,
                    MemberDisplayFormat = MemberDisplayFormat.SingleLine,
                };

                var actual = s_formatter.FormatObject(obj, printOptions);
                var expected = output[..i] + "...";
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void LongMembers()
        {
            object obj = new LongMembers();

            var str = new TestCSharpObjectFormatter(maximumLineLength: 20).FormatObject(obj, SingleLineOptions);
            Assert.Equal("LongMembers { LongNa...", str);

            str = new TestCSharpObjectFormatter(maximumLineLength: 20).FormatObject(obj, SeparateLinesOptions);
            Assert.Equal($"LongMembers {{{Environment.NewLine}  LongName0123456789...{Environment.NewLine}  LongValue: \"012345...{Environment.NewLine}}}{Environment.NewLine}", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Array()
        {
            var obj = new Object[] { new C(), 1, "str", 'c', true, null, new bool[] { true, false, true, false } };
            var str = s_formatter.FormatObject(obj, SeparateLinesOptions);

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

            str = s_formatter.FormatObject(a, SingleLineOptions);
            Assert.Equal("int[2, 3, 4] { { { 0, 1, 2, 3 }, { 10, 11, 12, 13 }, { 20, 21, 22, 23 } }, { { 100, 101, 102, 103 }, { 110, 111, 112, 113 }, { 120, 121, 122, 123 } } }", str);

            str = s_formatter.FormatObject(a, SeparateLinesOptions);
            AssertMembers(str, "int[2, 3, 4]",
                "{ { 0, 1, 2, 3 }, { 10, 11, 12, 13 }, { 20, 21, 22, 23 } }",
                "{ { 100, 101, 102, 103 }, { 110, 111, 112, 113 }, { 120, 121, 122, 123 } }"
            );

            int[][,][,,,] obj = new int[2][,][,,,];
            obj[0] = new int[1, 2][,,,];
            obj[0][0, 0] = new int[1, 2, 3, 4];

            str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("int[2][,][,,,] { int[1, 2][,,,] { { int[1, 2, 3, 4] { { { { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 } }, { { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 } } } }, null } }, null }", str);

            Array x = Array.CreateInstance(typeof(Object), lengths: new int[] { 2, 3 }, lowerBounds: new int[] { 2, 9 });
            str = s_formatter.FormatObject(x, SingleLineOptions);
            Assert.Equal("object[2..4, 9..12] { { null, null, null }, { null, null, null } }", str);

            Array y = Array.CreateInstance(typeof(Object), lengths: new int[] { 1, 1 }, lowerBounds: new int[] { 0, 0 });
            str = s_formatter.FormatObject(y, SingleLineOptions);
            Assert.Equal("object[1, 1] { { null } }", str);

            Array z = Array.CreateInstance(typeof(Object), lengths: new int[] { 0, 0 }, lowerBounds: new int[] { 0, 0 });
            str = s_formatter.FormatObject(z, SingleLineOptions);
            Assert.Equal("object[0, 0] { }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_IEnumerable_Core()
        {
            string str;
            object obj;

            obj = Range_Core(0, 10);
            str = s_formatter.FormatObject(obj, SingleLineOptions);

            Assert.Equal("ObjectFormatterTests.CoreRangeIterator(Count = 10)", str);
        }

        // This method and the class below emulate the behaviour of Enumerable.Range
        // in .NET Core. We use a custom type since not all runtime implementations
        // (e.g. Mono) apply precisely the same attributes, but we want to test behavior
        // under a specific set of attributes.
        private static IEnumerable<int> Range_Core(int start, int count)
            => new CoreRangeIterator(start, count);

        [DebuggerDisplay("Count = {CountForDebugger}")]
        private class CoreRangeIterator : IEnumerable<int>
        {
            private readonly int _start;
            private readonly int _end;

            private int CountForDebugger => _end - _start;

            public CoreRangeIterator(int start, int count)
                => (_start, _end) = (start, start + count);

            public IEnumerator<int> GetEnumerator() => null;
            IEnumerator IEnumerable.GetEnumerator() => null;
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_IEnumerable_Framework()
        {
            string str;
            object obj;

            obj = Range_Framework(0, 10);
            str = s_formatter.FormatObject(obj, SingleLineOptions);

            Assert.Equal("RangeIterator { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }", str);
        }

        // These methods emulate the .NET Framework Enumerable.Range method
        private static IEnumerable<int> Range_Framework(int start, int count)
            => RangeIterator(start, count);

        private static IEnumerable<int> RangeIterator(int start, int count)
        {
            for (var i = 0; i < count; i++)
                yield return start + i;
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_IEnumerable_Exception()
        {
            string str;
            object obj;

            obj = Enumerable.Range(0, 10).Where(i =>
            {
                if (i == 5)
                    throw new Exception("xxx");
                return i < 7;
            });
            str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("Enumerable.WhereEnumerableIterator<int> { 0, 1, 2, 3, 4, !<Exception> ... }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_IDictionary()
        {
            string str;
            object obj;

            obj = new ThrowingDictionary(throwAt: -1);
            str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("ThrowingDictionary(10) { { 1, 1 }, { 2, 2 }, { 3, 3 }, { 4, 4 } }", str);

            str = s_formatter.FormatObject(obj, SeparateLinesOptions);
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
            str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("ThrowingDictionary(10) { { 1, 1 }, { 2, 2 }, !<Exception> ... }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_BitArray()
        {
            // BitArray doesn't have debugger proxy/display
            var obj = new System.Collections.BitArray(new int[] { 1 });
            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("BitArray(32) { true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Queue()
        {
            var obj = new Queue<int>();
            obj.Enqueue(1);
            obj.Enqueue(2);
            obj.Enqueue(3);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("Queue<int>(3) { 1, 2, 3 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Stack()
        {
            var obj = new Stack<int>();
            obj.Push(1);
            obj.Push(2);
            obj.Push(3);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("Stack<int>(3) { 3, 2, 1 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Dictionary()
        {
            var obj = new Dictionary<string, int>
            {
                { "x", 1 },
            };

            var str = s_formatter.FormatObject(obj, SeparateLinesOptions);

            AssertMembers(str, "Dictionary<string, int>(1)",
                "{ \"x\", 1 }"
            );

            str = s_formatter.FormatObject(obj, SingleLineOptions);

            Assert.Equal("Dictionary<string, int>(1) { { \"x\", 1 } }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_KeyValuePair()
        {
            var obj = new KeyValuePair<int, string>(1, "x");

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("KeyValuePair<int, string> { 1, \"x\" }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_List()
        {
            var obj = new List<object> { 1, 2, 'c' };

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("List<object>(3) { 1, 2, 'c' }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_LinkedList()
        {
            var obj = new LinkedList<int>();
            obj.AddLast(1);
            obj.AddLast(2);
            obj.AddLast(3);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("LinkedList<int>(3) { 1, 2, 3 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SortedList()
        {
            var obj = new SortedList<int, int>();
            obj.Add(3, 4);
            obj.Add(1, 5);
            obj.Add(2, 6);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("SortedList<int, int>(3) { { 1, 5 }, { 2, 6 }, { 3, 4 } }", str);

            var obj2 = new SortedList<int[], int[]>();
            obj2.Add(new[] { 3 }, new int[] { 4 });

            str = s_formatter.FormatObject(obj2, SingleLineOptions);
            Assert.Equal("SortedList<int[], int[]>(1) { { int[1] { 3 }, int[1] { 4 } } }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SortedDictionary()
        {
            var obj = new SortedDictionary<int, int>();
            obj.Add(1, 0x1a);
            obj.Add(3, 0x3c);
            obj.Add(2, 0x2b);

            var str = s_formatter.
                FormatObject(obj, new PrintOptions { NumberRadix = ObjectFormatterHelpers.NumberRadixHexadecimal });

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
            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("HashSet<int>(Count = 2) { 1, 2 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SortedSet()
        {
            var obj = new SortedSet<int>();
            obj.Add(1);
            obj.Add(2);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("SortedSet<int>(2) { 1, 2 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_ConcurrentDictionary()
        {
            var obj = new ConcurrentDictionary<string, int>();
            obj.AddOrUpdate("x", 1, (k, v) => v);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);

            Assert.Equal("ConcurrentDictionary<string, int>(1) { { \"x\", 1 } }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_ConcurrentQueue()
        {
            var obj = new ConcurrentQueue<object>();
            obj.Enqueue(1);
            obj.Enqueue(2);
            obj.Enqueue(3);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("ConcurrentQueue<object>(3) { 1, 2, 3 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_ConcurrentStack()
        {
            var obj = new ConcurrentStack<object>();
            obj.Push(1);
            obj.Push(2);
            obj.Push(3);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("ConcurrentStack<object>(3) { 3, 2, 1 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_BlockingCollection()
        {
            var obj = new BlockingCollection<int>();
            obj.Add(1);
            obj.Add(2, new CancellationToken());

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
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

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("ReadOnlyCollection<int>(3) { 1, 2, 3 }", str);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        public void DebuggerProxy_FrameworkTypes_Lazy()
        {
            var obj = new Lazy<int[]>(() => new int[] { 1, 2 }, LazyThreadSafetyMode.None);

            // Lazy<T> has both DebuggerDisplay and DebuggerProxy attributes and both display the same information.

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("Lazy<int[]>(ThreadSafetyMode=None, IsValueCreated=false, IsValueFaulted=false, Value=null) { IsValueCreated=false, IsValueFaulted=false, Mode=None, Value=null }", str);

            str = s_formatter.FormatObject(obj, SeparateLinesOptions);
            AssertMembers(str, "Lazy<int[]>(ThreadSafetyMode=None, IsValueCreated=false, IsValueFaulted=false, Value=null)",
                "IsValueCreated: false",
                "IsValueFaulted: false",
                "Mode: None",
                "Value: null"
            );

            Assert.NotNull(obj.Value);

            str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("Lazy<int[]>(ThreadSafetyMode=None, IsValueCreated=true, IsValueFaulted=false, Value=int[2] { 1, 2 }) { IsValueCreated=true, IsValueFaulted=false, Mode=None, Value=int[2] { 1, 2 } }", str);

            str = s_formatter.FormatObject(obj, SeparateLinesOptions);
            AssertMembers(str, "Lazy<int[]>(ThreadSafetyMode=None, IsValueCreated=true, IsValueFaulted=false, Value=int[2] { 1, 2 })",
                "IsValueCreated: true",
                "IsValueFaulted: false",
                "Mode: None",
                "Value: int[2] { 1, 2 }"
            );
        }

        private void TaskMethod()
        {
        }

        [Fact, WorkItem("https://github.com/mono/mono/issues/10838")]
        public void DebuggerProxy_FrameworkTypes_Task()
        {
            var obj = new MockDesktopTask(TaskMethod);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal(
                "MockDesktopTask(Id = 1234, Status = Created, Method = \"Void TaskMethod()\") " +
                "{ AsyncState=null, CancellationPending=false, CreationOptions=None, Exception=null, Id=1234, Status=Created }",
                str);

            str = s_formatter.FormatObject(obj, SeparateLinesOptions);
            AssertMembers(str, "MockDesktopTask(Id = 1234, Status = Created, Method = \"Void TaskMethod()\")",
                "AsyncState: null",
                "CancellationPending: false",
                "CreationOptions: None",
                "Exception: null",
                "Id: 1234",
                "Status: Created"
            );
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SpinLock1()
        {
            var obj = new MockDesktopSpinLock(false);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("MockDesktopSpinLock(IsHeld = false) { IsHeld=false, IsHeldByCurrentThread=!<InvalidOperationException>, OwnerThreadID=null }", str);

            str = s_formatter.FormatObject(obj, SeparateLinesOptions);
            AssertMembers(str, "MockDesktopSpinLock(IsHeld = false)",
                "IsHeld: false",
                "IsHeldByCurrentThread: !<InvalidOperationException>",
                "OwnerThreadID: null"
            );
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SpinLock2()
        {
            var obj = new MockDesktopSpinLock(true);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("MockDesktopSpinLock(IsHeld = false) { IsHeld=false, IsHeldByCurrentThread=true, OwnerThreadID=0 }", str);

            str = s_formatter.FormatObject(obj, SeparateLinesOptions);
            AssertMembers(str, "MockDesktopSpinLock(IsHeld = false)",
                "IsHeld: false",
                "IsHeldByCurrentThread: true",
                "OwnerThreadID: 0"
            );
        }

        [Fact]
        public void DebuggerProxy_DiagnosticBag()
        {
            var obj = new DiagnosticBag();
            obj.Add(new DiagnosticInfo(MessageProvider.Instance, (int)ErrorCode.ERR_AbstractAndExtern, "bar"), NoLocation.Singleton);
            obj.Add(new DiagnosticInfo(MessageProvider.Instance, (int)ErrorCode.ERR_BadExternIdentifier, "goo"), NoLocation.Singleton);

            using (new EnsureEnglishUICulture())
            {
                var str = s_formatter.FormatObject(obj, SingleLineOptions);
                Assert.Equal("DiagnosticBag(Count = 2) { =error CS0180: 'bar' cannot be both extern and abstract, =error CS1679: Invalid extern alias for '/reference'; 'goo' is not a valid identifier }", str);

                str = s_formatter.FormatObject(obj, SeparateLinesOptions);
                AssertMembers(str, "DiagnosticBag(Count = 2)",
                     ": error CS0180: 'bar' cannot be both extern and abstract",
                     ": error CS1679: Invalid extern alias for '/reference'; 'goo' is not a valid identifier"
                );
            }
        }

        [Fact]
        public void DebuggerProxy_ArrayBuilder()
        {
            var obj = new ArrayBuilder<int>();
            obj.AddRange(new[] { 1, 2, 3, 4, 5 });

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("ArrayBuilder<int>(Count = 5) { 1, 2, 3, 4, 5 }", str);

            str = s_formatter.FormatObject(obj, SeparateLinesOptions);
            AssertMembers(str, "ArrayBuilder<int>(Count = 5)",
                 "1",
                 "2",
                 "3",
                 "4",
                 "5"
            );
        }

        [Fact, WorkItem(8542, "https://github.com/dotnet/roslyn/issues/8452")]
        public void FormatConstructorSignature()
        {
            var constructor = typeof(object).GetTypeInfo().DeclaredConstructors.Single();
            var signature = ((CommonObjectFormatter)s_formatter).FormatMethodSignature(constructor);
            Assert.Equal("object..ctor()", signature); // Checking for exceptions, more than particular output.
        }

        // The stack trace contains line numbers.  We use a #line directive
        // so that the baseline doesn't need to be updated every time this
        // file changes.
        //
        // When adding a new test to this region, ADD IT ADD THE END, so you
        // don't have to update all the other baselines.
#line 10000 "z:\Fixture.cs"

        private static class Fixture
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Method()
            {
                throw new Exception();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Method<U>()
            {
                throw new Exception();
            }
        }

        private static class Fixture<T>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Method()
            {
                throw new Exception();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Method<U>()
            {
                throw new Exception();
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/19027")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/15860")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/19027")]
        public void StackTrace_NonGeneric()
        {
            try
            {
                Fixture.Method();
            }
            catch (Exception e)
            {
                const string filePath = @"z:\Fixture.cs";

                var expected =
$@"{new Exception().Message}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.Fixture.Method(){string.Format(ScriptingResources.AtFileLine, filePath, 10006)}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.StackTrace_NonGeneric(){string.Format(ScriptingResources.AtFileLine, filePath, 10036)}
";
                var actual = s_formatter.FormatException(e);
                Assert.Equal(expected, actual);
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/19027")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/15860")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/19027")]
        public void StackTrace_GenericMethod()
        {
            try
            {
                Fixture.Method<char>();
            }
            catch (Exception e)
            {
                const string filePath = @"z:\Fixture.cs";

                // TODO (DevDiv #173210): Should show Fixture.Method<char>
                var expected =
$@"{new Exception().Message}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.Fixture.Method<U>(){string.Format(ScriptingResources.AtFileLine, filePath, 10012)}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.StackTrace_GenericMethod(){string.Format(ScriptingResources.AtFileLine, filePath, 10057)}
";
                var actual = s_formatter.FormatException(e);
                Assert.Equal(expected, actual);
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/19027")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/15860")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/19027")]
        public void StackTrace_GenericType()
        {
            try
            {
                Fixture<int>.Method();
            }
            catch (Exception e)
            {
                const string filePath = @"z:\Fixture.cs";

                // TODO (DevDiv #173210): Should show Fixture<int>.Method
                var expected =
$@"{new Exception().Message}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.Fixture<T>.Method(){string.Format(ScriptingResources.AtFileLine, filePath, 10021)}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.StackTrace_GenericType(){string.Format(ScriptingResources.AtFileLine, filePath, 10079)}
";
                var actual = s_formatter.FormatException(e);
                Assert.Equal(expected, actual);
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/19027")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/15860")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/19027")]
        public void StackTrace_GenericMethodInGenericType()
        {
            try
            {
                Fixture<int>.Method<char>();
            }
            catch (Exception e)
            {
                const string filePath = @"z:\Fixture.cs";

                // TODO (DevDiv #173210): Should show Fixture<int>.Method<char>
                var expected =
$@"{new Exception().Message}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.Fixture<T>.Method<U>(){string.Format(ScriptingResources.AtFileLine, filePath, 10027)}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.StackTrace_GenericMethodInGenericType(){string.Format(ScriptingResources.AtFileLine, filePath, 10101)}
";
                var actual = s_formatter.FormatException(e);
                Assert.Equal(expected, actual);
            }
        }

        private static class Fixture2
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void MethodDynamic()
            {
                ((dynamic)new object()).x();
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9221"), WorkItem("https://github.com/dotnet/roslyn/issues/9221")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/19027")]
        public void StackTrace_Dynamic()
        {
            try
            {
                Fixture2.MethodDynamic();
                Assert.False(true);
            }
            catch (Exception e)
            {
                const string filePath = @"z:\Fixture.cs";

                var expected =
$@"'object' does not contain a definition for 'x'
  + System.Dynamic.UpdateDelegates.UpdateAndExecuteVoid1<T0>(System.Runtime.CompilerServices.CallSite, T0)
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.Fixture2.MethodDynamic(){string.Format(ScriptingResources.AtFileLine, filePath, 10123)}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.StackTrace_Dynamic(){string.Format(ScriptingResources.AtFileLine, filePath, 10132)}
";
                var actual = s_formatter.FormatException(e);
                Assert.Equal(expected, actual);
            }
        }

        private static class ParametersFixture
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Method(ref char c, out DateTime d)
            {
                throw new Exception();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Method<U>(ref U u)
            {
                throw new Exception();
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/19027")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/15860")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/19027")]
        public void StackTrace_RefOutParameters()
        {
            try
            {
                char c = ' ';
                DateTime date;
                ParametersFixture.Method(ref c, out date);
            }
            catch (Exception e)
            {
                const string filePath = @"z:\Fixture.cs";

                var expected =
$@"{new Exception().Message}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.ParametersFixture.Method(ref char, out System.DateTime){string.Format(ScriptingResources.AtFileLine, filePath, 10155)}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.StackTrace_RefOutParameters(){string.Format(ScriptingResources.AtFileLine, filePath, 10172)}
";
                var actual = s_formatter.FormatException(e);
                Assert.Equal(expected, actual);
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/19027")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/15860")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/19027")]
        public void StackTrace_GenericRefParameter()
        {
            try
            {
                char c = ' ';
                ParametersFixture.Method<char>(ref c);
            }
            catch (Exception e)
            {
                const string filePath = @"z:\Fixture.cs";

                // TODO (DevDiv #173210): Should show ParametersFixture.Method<char>(ref char)
                var expected =
$@"{new Exception().Message}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.ParametersFixture.Method<U>(ref U){string.Format(ScriptingResources.AtFileLine, filePath, 10161)}
  + Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests.ObjectFormatterTests.StackTrace_GenericRefParameter(){string.Format(ScriptingResources.AtFileLine, filePath, 10194)}
";
                var actual = s_formatter.FormatException(e);
                Assert.Equal(expected, actual);
            }
        }
    }
}
