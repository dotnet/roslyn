// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FieldDocumentationCommentTests : CSharpTestBase
    {
        private readonly CSharpCompilation compilation;
        private readonly NamespaceSymbol acmeNamespace;
        private readonly NamedTypeSymbol widgetClass;
        private readonly NamedTypeSymbol enumSymbol;
        private readonly NamedTypeSymbol valueType;

        public FieldDocumentationCommentTests()
        {
            compilation = CreateCompilationWithMscorlibAndDocumentationComments(@"
namespace Acme
{
	struct ValueType
	{
        /// <summary>Summary for total fields.</summary>
		private int total1, total2;
	}
	class Widget: IProcess
	{
		public class NestedClass
		{
			private int value;
		}
		private string message;
		private static Color defaultColor;
		private const double PI = 3.14159;
		protected readonly double monthlyAverage;
		private long[] array1;
		private Widget[,] array2;
		private unsafe int *pCount;
		private unsafe float **ppValues;
	}

    enum E
    {
        /// <summary>Enum field</summary>
        A = 1
    }
}
");

            acmeNamespace = (NamespaceSymbol)compilation.GlobalNamespace.GetMembers("Acme").Single();
            widgetClass = acmeNamespace.GetTypeMembers("Widget").Single();
            enumSymbol = acmeNamespace.GetTypeMembers("E").Single();
            valueType = acmeNamespace.GetTypeMembers("ValueType").Single();
        }

        [Fact]
        public void TestFieldInStruct()
        {
            var total1 = valueType.GetMembers("total1").Single();
            var total2 = valueType.GetMembers("total2").Single();
            Assert.Equal("F:Acme.ValueType.total1", total1.GetDocumentationCommentId());
            Assert.Equal(
@"<member name=""F:Acme.ValueType.total1"">
    <summary>Summary for total fields.</summary>
</member>
", total1.GetDocumentationCommentXml());

            Assert.Equal("F:Acme.ValueType.total2", total2.GetDocumentationCommentId());
            Assert.Equal(@"<member name=""F:Acme.ValueType.total2"">
    <summary>Summary for total fields.</summary>
</member>
", total2.GetDocumentationCommentXml());
        }

        [Fact]
        public void TestFieldInNestedClass()
        {
            Assert.Equal("F:Acme.Widget.NestedClass.value", widgetClass.GetTypeMembers("NestedClass").Single()
                                                                       .GetMembers("value").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestSimpleField()
        {
            Assert.Equal("F:Acme.Widget.message", widgetClass.GetMembers("message").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestStatic()
        {
            Assert.Equal("F:Acme.Widget.defaultColor", widgetClass.GetMembers("defaultColor").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestConst()
        {
            Assert.Equal("F:Acme.Widget.PI", widgetClass.GetMembers("PI").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestReadOnly()
        {
            Assert.Equal("F:Acme.Widget.monthlyAverage", widgetClass.GetMembers("monthlyAverage").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestArray1()
        {
            Assert.Equal("F:Acme.Widget.array1", widgetClass.GetMembers("array1").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestArray2()
        {
            Assert.Equal("F:Acme.Widget.array2", widgetClass.GetMembers("array2").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestUnsafePointer()
        {
            Assert.Equal("F:Acme.Widget.pCount", widgetClass.GetMembers("pCount").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestUnsafePointerToPointer()
        {
            Assert.Equal("F:Acme.Widget.ppValues", widgetClass.GetMembers("ppValues").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestEnumField()
        {
            var field = enumSymbol.GetMembers("A").Single();
            Assert.Equal("F:Acme.E.A", field.GetDocumentationCommentId());
            Assert.Equal(
@"<member name=""F:Acme.E.A"">
    <summary>Enum field</summary>
</member>
", field.GetDocumentationCommentXml());
        }
    }
}
