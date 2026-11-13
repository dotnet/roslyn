// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/86250")]
        public void PartialMemberImplementationDiagnostics()
        {
            var sources = new[]
            {
                """
                using System;

                partial class C
                {
                    /// <see cref="MissingDef"/>
                    public partial C();
                    public partial C(int x);
                    public partial void M();
                    public partial int P { get; }
                    public partial event Action E1;
                    public partial event Action E2;
                }
                """,
                """
                using System;

                partial class C
                {
                    public partial C() { }

                    /// <see cref="MissingCtor"/>
                    public partial C(int x) { }

                    /// <see cref="MissingM"/>
                    public partial void M() { }

                    /// <see cref="MissingP"/>
                    public partial int P => 1;

                    /// <see cref="MissingE1"/>
                    public partial event Action E1 { add { } remove { } }

                    /// <see cref="MissingE2"/>
                    public extern partial event Action E2;
                }
                """,
            };

            DiagnosticDescription[] expected0 =
            [
                // (5,20): warning CS1574: XML comment has cref attribute 'MissingDef' that could not be resolved
                //     /// <see cref="MissingDef"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "MissingDef").WithArguments("MissingDef").WithLocation(5, 20),
            ];

            DiagnosticDescription[] expected1 =
            [
                // (7,20): warning CS1574: XML comment has cref attribute 'MissingCtor' that could not be resolved
                //     /// <see cref="MissingCtor"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "MissingCtor").WithArguments("MissingCtor").WithLocation(7, 20),
                // (10,20): warning CS1574: XML comment has cref attribute 'MissingM' that could not be resolved
                //     /// <see cref="MissingM"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "MissingM").WithArguments("MissingM").WithLocation(10, 20),
                // (13,20): warning CS1574: XML comment has cref attribute 'MissingP' that could not be resolved
                //     /// <see cref="MissingP"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "MissingP").WithArguments("MissingP").WithLocation(13, 20),
                // (16,20): warning CS1574: XML comment has cref attribute 'MissingE1' that could not be resolved
                //     /// <see cref="MissingE1"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "MissingE1").WithArguments("MissingE1").WithLocation(16, 20),
                // (19,20): warning CS1574: XML comment has cref attribute 'MissingE2' that could not be resolved
                //     /// <see cref="MissingE2"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "MissingE2").WithArguments("MissingE2").WithLocation(19, 20),
            ];

            CreateCompilation(sources, parseOptions: TestOptions.RegularWithDocumentationComments).VerifyDiagnostics([.. expected0, .. expected1]);

            var compilation0 = CreateCompilation(sources, parseOptions: TestOptions.RegularWithDocumentationComments);
            compilation0.GetSemanticModel(compilation0.SyntaxTrees[0]).GetDiagnostics().Verify(expected0);

            var compilation1 = CreateCompilation(sources, parseOptions: TestOptions.RegularWithDocumentationComments);
            compilation1.GetSemanticModel(compilation1.SyntaxTrees[1]).GetDiagnostics().Verify(expected1);
        }
    }
}
