// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests;
using Xunit;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests
{
    public class ObjectFormatterTests : ObjectFormatterTestBase
    {
        private static readonly ObjectFormatter s_formatter = new TestCSharpObjectFormatter();

        [Fact]
        public void DebuggerProxy_FrameworkTypes_ArrayList()
        {
            var obj = new ArrayList { 1, 2, true, "goo" };
            var str = s_formatter.FormatObject(obj, SingleLineOptions);

            Assert.Equal("ArrayList(4) { 1, 2, true, \"goo\" }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Hashtable()
        {
            var obj = new Hashtable
            {
                { new byte[] { 1, 2 }, new[] { 1,2,3 } },
            };

            var str = s_formatter.FormatObject(obj, SeparateLinesOptions);

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

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("Queue(3) { 1, 2, 3 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_Stack()
        {
            var obj = new Stack();
            obj.Push(1);
            obj.Push(2);
            obj.Push(3);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("Stack(3) { 3, 2, 1 }", str);
        }

        [Fact]
        public void DebuggerProxy_FrameworkTypes_SortedList()
        {
            SortedList obj = new SortedList();
            obj.Add(3, 4);
            obj.Add(1, 5);
            obj.Add(2, 6);

            var str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("SortedList(3) { { 1, 5 }, { 2, 6 }, { 3, 4 } }", str);

            obj = new SortedList();
            obj.Add(new[] { 3 }, new int[] { 4 });

            str = s_formatter.FormatObject(obj, SingleLineOptions);
            Assert.Equal("SortedList(1) { { int[1] { 3 }, int[1] { 4 } } }", str);
        }

        // TODO: move to portable
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
                "goo",
                new[] { VB.VisualBasicSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromAssemblyInternal(typeof(object).GetTypeInfo().Assembly) },
                new VB.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug));

            Assembly a;
            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);
                a = Assembly.Load(stream.ToArray());
            }

            var c = a.GetType("C");
            var obj = Activator.CreateInstance(c);

            var str = s_formatter.FormatObject(obj, SeparateLinesOptions);
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
