// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly CSharpCompilation compilation;
        private readonly NamespaceSymbol acmeNamespace;
        private readonly NamedTypeSymbol widgetClass;

        public PropertyDocumentationCommentTests()
        {
            compilation = CreateCompilationWithMscorlib(@"namespace Acme
{
    class Widget: IProcess
    {
        public int Width { get { } set { } }
        public int this[int i] { get { } set { } }
        public int this[string s, int i] { get { } set { } }
    }
}
");

            acmeNamespace = (NamespaceSymbol)compilation.GlobalNamespace.GetMembers("Acme").Single();
            widgetClass = acmeNamespace.GetTypeMembers("Widget").Single();
        }

        [Fact]
        public void TestProperty()
        {
            Assert.Equal("P:Acme.Widget.Width",
                acmeNamespace.GetTypeMembers("Widget").Single()
                    .GetMembers("Width").Single().GetDocumentationCommentId());
        }

        [Fact]
        public void TestIndexer1()
        {
            Assert.Equal("P:Acme.Widget.Item(System.Int32)",
                acmeNamespace.GetTypeMembers("Widget").Single()
                    .GetMembers("this[]")[0].GetDocumentationCommentId());
        }

        [Fact]
        public void TestIndexer2()
        {
            Assert.Equal("P:Acme.Widget.Item(System.String,System.Int32)",
                acmeNamespace.GetTypeMembers("Widget").Single()
                    .GetMembers("this[]")[1].GetDocumentationCommentId());
        }
    }
}
