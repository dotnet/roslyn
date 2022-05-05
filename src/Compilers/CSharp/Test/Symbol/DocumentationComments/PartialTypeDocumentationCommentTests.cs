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
    public class PartialTypeDocumentationCommentTests : CSharpTestBase
    {
        private readonly CSharpCompilation _compilation;
        private readonly NamedTypeSymbol _gooClass;

        public PartialTypeDocumentationCommentTests()
        {
            var tree1 = Parse(
                @"
/// <summary>Summary on first file's Goo.</summary>
partial class Goo
{
    /// <summary>Summary on MethodWithNoImplementation.</summary>
    partial void MethodWithNoImplementation();

    /// <summary>Summary in file one which should be shadowed.</summary>
    partial void ImplementedMethodWithNoSummaryOnImpl();

    partial void ImplementedMethod();
}", options: TestOptions.RegularWithDocumentationComments);

            var tree2 = Parse(
                @"
/// <summary>Summary on second file's Goo.</summary>
partial class Goo
{
    /// <remarks>Goo.</remarks>
    partial void ImplementedMethodWithNoSummaryOnImpl() { }

    /// <summary>Implemented method.</summary>
    partial void ImplementedMethod() { }
}", options: TestOptions.RegularWithDocumentationComments);

            _compilation = CreateCompilation(new[] { tree1, tree2 });

            _gooClass = _compilation.GlobalNamespace.GetTypeMembers("Goo").Single();
        }

        [Fact]
        public void TestSummaryOfType()
        {
            Assert.Equal(
@"<member name=""T:Goo"">
    <summary>Summary on first file's Goo.</summary>
    <summary>Summary on second file's Goo.</summary>
</member>
", _gooClass.GetDocumentationCommentXml());
        }

        [Fact]
        public void TestSummaryOfMethodWithNoImplementation()
        {
            var method = _gooClass.GetMembers("MethodWithNoImplementation").Single();
            Assert.Equal(
@"<member name=""M:Goo.MethodWithNoImplementation"">
    <summary>Summary on MethodWithNoImplementation.</summary>
</member>
", method.GetDocumentationCommentXml());
        }

        [Fact]
        public void TestImplementedMethodWithNoSummaryOnImpl()
        {
            // This is an interesting behavior; as long as there is any XML at all on the implementation, it overrides
            // any XML on the latent declaration. Since we don't have a summary on this implementation, this should be
            // null!
            var method = _gooClass.GetMembers("ImplementedMethodWithNoSummaryOnImpl").Single();
            Assert.Equal(
@"<member name=""M:Goo.ImplementedMethodWithNoSummaryOnImpl"">
    <remarks>Goo.</remarks>
</member>
", method.GetDocumentationCommentXml());
        }

        [Fact]
        public void TestImplementedMethod()
        {
            var method = _gooClass.GetMembers("ImplementedMethod").Single();
            Assert.Equal(
@"<member name=""M:Goo.ImplementedMethod"">
    <summary>Implemented method.</summary>
</member>
", method.GetDocumentationCommentXml());
        }
    }
}
