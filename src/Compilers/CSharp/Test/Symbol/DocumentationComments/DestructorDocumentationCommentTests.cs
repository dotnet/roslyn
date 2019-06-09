// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly CSharpCompilation _compilation;
        private readonly NamespaceSymbol _acmeNamespace;
        private readonly NamedTypeSymbol _widgetClass;

        public DestructorDocumentationCommentTests()
        {
            _compilation = CreateCompilationWithMscorlib40AndDocumentationComments(@"namespace Acme
{
	class Widget: IProcess
	{
        /// <summary>Destructor Documentation</summary>
        ~Widget() {...}
	}
}
");

            _acmeNamespace = (NamespaceSymbol)_compilation.GlobalNamespace.GetMembers("Acme").Single();
            _widgetClass = _acmeNamespace.GetTypeMembers("Widget").Single();
        }

        [Fact]
        public void TestDestructor()
        {
            Assert.Equal("M:Acme.Widget.Finalize", _widgetClass.GetMembers("Finalize").Single().GetDocumentationCommentId());
            Assert.Equal(
@"<member name=""M:Acme.Widget.Finalize"">
    <summary>Destructor Documentation</summary>
</member>
", _widgetClass.GetMembers("Finalize").Single().GetDocumentationCommentXml());
        }
    }
}
