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
    public class PropertyDocumentationCommentTests : CSharpTestBase
    {
        private readonly CSharpCompilation _compilation;
        private readonly NamespaceSymbol _acmeNamespace;
        private readonly NamedTypeSymbol _widgetClass;

        public PropertyDocumentationCommentTests()
        {
            _compilation = CreateCompilation(@"namespace Acme
{
    class Widget: IProcess
    {
        public int Width { get { } set { } }
        public int this[int i] { get { } set { } }
        public int this[string s, int i] { get { } set { } }
    }
}
");

            _acmeNamespace = (NamespaceSymbol)_compilation.GlobalNamespace.GetMembers("Acme").Single();
            _widgetClass = _acmeNamespace.GetTypeMembers("Widget").Single();
        }

        [Fact]
        public void TestProperty()
        {
            Assert.Equal("P:Acme.Widget.Width",
                _acmeNamespace.GetTypeMembers("Widget").Single()
                    .GetMembers("Width").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestIndexer1()
        {
            Assert.Equal("P:Acme.Widget.Item(System.Int32)",
                _acmeNamespace.GetTypeMembers("Widget").Single()
                    .GetMembers("this[]")[0].GetDocumentationCommentId());
        }

        [Fact]
        public void TestIndexer2()
        {
            Assert.Equal("P:Acme.Widget.Item(System.String,System.Int32)",
                _acmeNamespace.GetTypeMembers("Widget").Single()
                    .GetMembers("this[]")[1].GetDocumentationCommentId());
        }
    }
}
