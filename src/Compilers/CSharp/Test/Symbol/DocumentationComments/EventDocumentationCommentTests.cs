// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly CSharpCompilation compilation;
        private readonly NamespaceSymbol acmeNamespace;
        private readonly NamedTypeSymbol widgetClass;

        public EventDocumentationCommentTests()
        {
            compilation = CreateCompilationWithMscorlib(@"namespace Acme
{
    class Widget: IProcess
    {
        public event System.Action E;
        public event System.Action F { add { } remove { } }
    }
}
");

            acmeNamespace = (NamespaceSymbol)compilation.GlobalNamespace.GetMember<NamespaceSymbol>("Acme");
            widgetClass = acmeNamespace.GetMember<NamedTypeSymbol>("Widget");
        }

        [Fact]
        public void TestFieldLikeEvent()
        {
            var eventSymbol = this.widgetClass.GetMember<EventSymbol>("E");
            Assert.Equal("E:Acme.Widget.E", eventSymbol.GetDocumentationCommentId());
            Assert.Equal("M:Acme.Widget.add_E(System.Action)", eventSymbol.AddMethod.GetDocumentationCommentId());
            Assert.Equal("M:Acme.Widget.remove_E(System.Action)", eventSymbol.RemoveMethod.GetDocumentationCommentId());
        }

        [Fact]
        public void TestCustomEvent()
        {
            var eventSymbol = this.widgetClass.GetMember<EventSymbol>("F");
            Assert.Equal("E:Acme.Widget.F", eventSymbol.GetDocumentationCommentId());
            Assert.Equal("M:Acme.Widget.add_F(System.Action)", eventSymbol.AddMethod.GetDocumentationCommentId());
            Assert.Equal("M:Acme.Widget.remove_F(System.Action)", eventSymbol.RemoveMethod.GetDocumentationCommentId());
        }
    }
}
