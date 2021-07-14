// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class EventDocumentationCommentTests : CSharpTestBase
    {
        private readonly CSharpCompilation _compilation;
        private readonly NamespaceSymbol _acmeNamespace;
        private readonly NamedTypeSymbol _widgetClass;

        public EventDocumentationCommentTests()
        {
            _compilation = CreateCompilation(@"namespace Acme
{
    class Widget: IProcess
    {
        public event System.Action E;
        public event System.Action F { add { } remove { } }
    }
}
");

            _acmeNamespace = (NamespaceSymbol)_compilation.GlobalNamespace.GetMember<NamespaceSymbol>("Acme");
            _widgetClass = _acmeNamespace.GetMember<NamedTypeSymbol>("Widget");
        }

        [Fact]
        public void TestFieldLikeEvent()
        {
            var eventSymbol = _widgetClass.GetMember<EventSymbol>("E");
            Assert.Equal("E:Acme.Widget.E", eventSymbol.GetDocumentationCommentId());
            Assert.Equal("M:Acme.Widget.add_E(System.Action)", eventSymbol.AddMethod.GetDocumentationCommentId());
            Assert.Equal("M:Acme.Widget.remove_E(System.Action)", eventSymbol.RemoveMethod.GetDocumentationCommentId());
        }

        [Fact]
        public void TestCustomEvent()
        {
            var eventSymbol = _widgetClass.GetMember<EventSymbol>("F");
            Assert.Equal("E:Acme.Widget.F", eventSymbol.GetDocumentationCommentId());
            Assert.Equal("M:Acme.Widget.add_F(System.Action)", eventSymbol.AddMethod.GetDocumentationCommentId());
            Assert.Equal("M:Acme.Widget.remove_F(System.Action)", eventSymbol.RemoveMethod.GetDocumentationCommentId());
        }
    }
}
