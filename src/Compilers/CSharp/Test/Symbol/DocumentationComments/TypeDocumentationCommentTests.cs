// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class TypeDocumentationCommentTests : CSharpTestBase
    {
        private readonly CSharpCompilation _compilation;
        private readonly NamespaceSymbol _acmeNamespace;
        private readonly NamedTypeSymbol _widgetClass;

        public TypeDocumentationCommentTests()
        {
            _compilation = CreateCompilationWithMscorlibAndDocumentationComments(@"enum Color { Red, Blue, Green }
namespace Acme
{
	interface IProcess {...}
	struct ValueType {...}
	class Widget: IProcess
	{
        /// <summary>
        /// Hello! Nested Class.
        /// </summary>
		public class NestedClass {...}
		public interface IMenuItem {...}
		public delegate void Del(int i);
		public enum Direction { North, South, East, West }
	}
	class MyList<T>
	{
		class Helper<U,V> {...}
	}
}");

            _acmeNamespace = (NamespaceSymbol)_compilation.GlobalNamespace.GetMembers("Acme").Single();
            _widgetClass = _acmeNamespace.GetTypeMembers("Widget").Single();
        }

        [Fact]
        public void TestEnum()
        {
            Assert.Equal("T:Color", _compilation.GlobalNamespace.GetTypeMembers("Color").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestInterface()
        {
            Assert.Equal("T:Acme.IProcess", _acmeNamespace.GetTypeMembers("IProcess").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestStruct()
        {
            Assert.Equal("T:Acme.ValueType", _acmeNamespace.GetTypeMembers("ValueType").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestClass()
        {
            Assert.Equal("T:Acme.Widget", _widgetClass.GetDocumentationCommentId());
        }

        [Fact]
        public void TestNestedClass()
        {
            var classSymbol = _widgetClass.GetTypeMembers("NestedClass").Single();
            Assert.Equal("T:Acme.Widget.NestedClass", classSymbol.GetDocumentationCommentId());
            Assert.Equal(
@"<member name=""T:Acme.Widget.NestedClass"">
    <summary>
    Hello! Nested Class.
    </summary>
</member>
", classSymbol.GetDocumentationCommentXml());
        }

        [Fact]
        public void TestNestedInterface()
        {
            Assert.Equal("T:Acme.Widget.IMenuItem", _widgetClass.GetMembers("IMenuItem").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestNestedDelegate()
        {
            Assert.Equal("T:Acme.Widget.Del", _widgetClass.GetTypeMembers("Del").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestNestedEnum()
        {
            Assert.Equal("T:Acme.Widget.Direction", _widgetClass.GetTypeMembers("Direction").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestGenericType()
        {
            Assert.Equal("T:Acme.MyList`1", _acmeNamespace.GetTypeMembers("MyList", 1).Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestNestedGenericType()
        {
            Assert.Equal("T:Acme.MyList`1.Helper`2", _acmeNamespace.GetTypeMembers("MyList", 1).Single()
                                                                  .GetTypeMembers("Helper", 2).Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestDynamicType()
        {
            Assert.Null(DynamicTypeSymbol.Instance.GetDocumentationCommentId());
        }

        [WorkItem(536957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536957")]
        [Fact]
        public void TestCommentsWithQuestionMarks()
        {
            var text = @"
/// <doc><?pi ?></doc>
/// <d><?pi some data ? > <??></d>
/// <a></a><?pi data?>
/// <do><?pi x
///  y?></do>
class A 
{
}
";
            var comp = CreateCompilationWithMscorlib(text);
            Assert.Equal(0, comp.GetDiagnostics().Count());
        }
    }
}
