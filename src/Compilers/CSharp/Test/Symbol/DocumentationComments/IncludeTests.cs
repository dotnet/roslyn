// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IncludeTests : CSharpTestBase
    {
        [Theory]
        [InlineData("Field", "F:Acme.Widget.Field")]
        [InlineData(WellKnownMemberNames.StaticConstructorName, "M:Acme.Widget.#cctor")]
        [InlineData("Event", "E:Acme.Widget.Event")]
        [InlineData("Property", "P:Acme.Widget.Property")]
        [InlineData("Method", "M:Acme.Widget.Method")]
        [InlineData("NamedType", "T:Acme.Widget.NamedType")]
        public void TestDocumentationCaching(string symbolName, string documentationId)
        {
            using var _ = new EnsureEnglishUICulture();

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(@"namespace Acme
{
    class Widget
    {
        /// <include file=""NonExistent.xml"" />
        int Field;

        /// <include file=""NonExistent.xml"" />
        static Widget() { }

        /// <include file=""NonExistent.xml"" />
        event EventHandler Event;

        /// <include file=""NonExistent.xml"" />
        int Property { get; }

        /// <include file=""NonExistent.xml"" />
        void Method() { }

        /// <include file=""NonExistent.xml"" />
        class NamedType { }
    }
}
");

            var acmeNamespace = (NamespaceSymbol)compilation.GlobalNamespace.GetMembers("Acme").Single();
            var widgetClass = acmeNamespace.GetTypeMembers("Widget").Single();

            var symbol = widgetClass.GetMembers(symbolName).Single();
            Assert.Equal(documentationId, symbol.GetDocumentationCommentId());
            Assert.Equal(
$@"<member name=""{documentationId}"">
    <!-- Include tag is invalid --><include file=""NonExistent.xml"" />
</member>
", symbol.GetDocumentationCommentXml(expandIncludes: true));
            Assert.Equal(
$@"<member name=""{documentationId}"">
    <include file=""NonExistent.xml"" />
</member>
", symbol.GetDocumentationCommentXml(expandIncludes: false));
            Assert.Equal(
$@"<member name=""{documentationId}"">
    <!-- Include tag is invalid --><include file=""NonExistent.xml"" />
</member>
", symbol.GetDocumentationCommentXml(expandIncludes: true));
            Assert.Equal(
$@"<member name=""{documentationId}"">
    <include file=""NonExistent.xml"" />
</member>
", symbol.GetDocumentationCommentXml(expandIncludes: false));
        }
    }
}
