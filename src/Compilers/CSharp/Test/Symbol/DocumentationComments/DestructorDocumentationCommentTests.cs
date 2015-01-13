// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DestructorDocumentationCommentTests : CSharpTestBase
    {
        private readonly CSharpCompilation compilation;
        private readonly NamespaceSymbol acmeNamespace;
        private readonly NamedTypeSymbol widgetClass;

        public DestructorDocumentationCommentTests()
        {
            compilation = CreateCompilationWithMscorlibAndDocumentationComments(@"namespace Acme
{
	class Widget: IProcess
	{
        /// <summary>Destructor Documentation</summary>
        ~Widget() {...}
	}
}
");

            acmeNamespace = (NamespaceSymbol)compilation.GlobalNamespace.GetMembers("Acme").Single();
            widgetClass = acmeNamespace.GetTypeMembers("Widget").Single();
        }

        [Fact]
        public void TestDestructor()
        {
            Assert.Equal("M:Acme.Widget.Finalize", widgetClass.GetMembers("Finalize").Single().GetDocumentationCommentId());
            Assert.Equal(
@"<member name=""M:Acme.Widget.Finalize"">
    <summary>Destructor Documentation</summary>
</member>
", widgetClass.GetMembers("Finalize").Single().GetDocumentationCommentXml());
        }
    }
}
