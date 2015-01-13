// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly CSharpCompilation compilation;
        private readonly NamespaceSymbol acmeNamespace;
        private readonly NamedTypeSymbol widgetClass;

        public ConstructorDocumentationCommentTests()
        {
            compilation = CreateCompilationWithMscorlibAndDocumentationComments(@"namespace Acme
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

            acmeNamespace = (NamespaceSymbol)compilation.GlobalNamespace.GetMembers("Acme").Single();
            widgetClass = acmeNamespace.GetTypeMembers("Widget").Single();
        }

        [Fact]
        public void TestStaticConstructor()
        {
            var staticConstructorSymbol = widgetClass.GetMembers(WellKnownMemberNames.StaticConstructorName).Single();
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
            var constructorSymbol = widgetClass.InstanceConstructors.Single(c => !c.IsStatic && c.Parameters.Length == 0);
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
            var parameterizedConstructorSymbol = widgetClass.InstanceConstructors.Single(c => !c.IsStatic && c.Parameters.Length == 1);
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
