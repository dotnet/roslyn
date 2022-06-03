// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        private readonly CSharpCompilation _compilation;
        private readonly NamespaceSymbol _acmeNamespace;
        private readonly NamedTypeSymbol _widgetClass;
        private readonly NamedTypeSymbol _enumSymbol;
        private readonly NamedTypeSymbol _valueType;

        public FieldDocumentationCommentTests()
        {
            _compilation = CreateCompilationWithMscorlib40AndDocumentationComments(@"
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

            _acmeNamespace = (NamespaceSymbol)_compilation.GlobalNamespace.GetMembers("Acme").Single();
            _widgetClass = _acmeNamespace.GetTypeMembers("Widget").Single();
            _enumSymbol = _acmeNamespace.GetTypeMembers("E").Single();
            _valueType = _acmeNamespace.GetTypeMembers("ValueType").Single();
        }

        [Fact]
        public void TestFieldInStruct()
        {
            var total1 = _valueType.GetMembers("total1").Single();
            var total2 = _valueType.GetMembers("total2").Single();
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
            Assert.Equal("F:Acme.Widget.NestedClass.value", _widgetClass.GetTypeMembers("NestedClass").Single()
                                                                       .GetMembers("value").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestSimpleField()
        {
            Assert.Equal("F:Acme.Widget.message", _widgetClass.GetMembers("message").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestStatic()
        {
            Assert.Equal("F:Acme.Widget.defaultColor", _widgetClass.GetMembers("defaultColor").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestConst()
        {
            Assert.Equal("F:Acme.Widget.PI", _widgetClass.GetMembers("PI").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestReadOnly()
        {
            Assert.Equal("F:Acme.Widget.monthlyAverage", _widgetClass.GetMembers("monthlyAverage").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestArray1()
        {
            Assert.Equal("F:Acme.Widget.array1", _widgetClass.GetMembers("array1").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestArray2()
        {
            Assert.Equal("F:Acme.Widget.array2", _widgetClass.GetMembers("array2").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestUnsafePointer()
        {
            Assert.Equal("F:Acme.Widget.pCount", _widgetClass.GetMembers("pCount").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestUnsafePointerToPointer()
        {
            Assert.Equal("F:Acme.Widget.ppValues", _widgetClass.GetMembers("ppValues").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestEnumField()
        {
            var field = _enumSymbol.GetMembers("A").Single();
            Assert.Equal("F:Acme.E.A", field.GetDocumentationCommentId());
            Assert.Equal(
@"<member name=""F:Acme.E.A"">
    <summary>Enum field</summary>
</member>
", field.GetDocumentationCommentXml());
        }
    }
}
