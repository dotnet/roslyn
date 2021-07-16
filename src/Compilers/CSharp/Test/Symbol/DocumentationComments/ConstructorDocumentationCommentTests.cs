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
    public class ConstructorDocumentationCommentTests : CSharpTestBase
    {
        private readonly CSharpCompilation _compilation;
        private readonly NamespaceSymbol _acmeNamespace;
        private readonly NamedTypeSymbol _widgetClass;

        public ConstructorDocumentationCommentTests()
        {
            _compilation = CreateCompilationWithMscorlib40AndDocumentationComments(@"namespace Acme
{
	class Widget: IProcess
	{
        /// <summary>Static Constructor</summary>
        static Widget() {...}
        /** <summary>Instance Constructor</summary> */
        public Widget() {...}
        /// <summary>
        /// Parameterized Constructor
        /// </summary>
        /// <param name=""s"">s, the string argument</param>
        public Widget(string s) {...}
	}
}
");

            _acmeNamespace = (NamespaceSymbol)_compilation.GlobalNamespace.GetMembers("Acme").Single();
            _widgetClass = _acmeNamespace.GetTypeMembers("Widget").Single();
        }

        [Fact]
        public void TestStaticConstructor()
        {
            var staticConstructorSymbol = _widgetClass.GetMembers(WellKnownMemberNames.StaticConstructorName).Single();
            Assert.Equal("M:Acme.Widget.#cctor", staticConstructorSymbol.GetDocumentationCommentId());
            Assert.Equal(
@"<member name=""M:Acme.Widget.#cctor"">
    <summary>Static Constructor</summary>
</member>
", staticConstructorSymbol.GetDocumentationCommentXml());
        }

        [Fact]
        public void TestConstructor()
        {
            var constructorSymbol = _widgetClass.InstanceConstructors.Single(c => !c.IsStatic && c.Parameters.Length == 0);
            Assert.Equal("M:Acme.Widget.#ctor", constructorSymbol.GetDocumentationCommentId());
            Assert.Equal(
@"<member name=""M:Acme.Widget.#ctor"">
    <summary>Instance Constructor</summary> 
</member>
", constructorSymbol.GetDocumentationCommentXml());
        }

        [Fact]
        public void TestConstructorWithParameter()
        {
            var parameterizedConstructorSymbol = _widgetClass.InstanceConstructors.Single(c => !c.IsStatic && c.Parameters.Length == 1);
            Assert.Equal("M:Acme.Widget.#ctor(System.String)", parameterizedConstructorSymbol.GetDocumentationCommentId());
            Assert.Equal(
@"<member name=""M:Acme.Widget.#ctor(System.String)"">
    <summary>
    Parameterized Constructor
    </summary>
    <param name=""s"">s, the string argument</param>
</member>
", parameterizedConstructorSymbol.GetDocumentationCommentXml());
        }
    }
}
