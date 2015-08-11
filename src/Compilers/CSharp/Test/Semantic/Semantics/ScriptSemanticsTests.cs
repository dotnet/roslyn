// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ScriptSemanticsTests : CSharpTestBase
    {
        [WorkItem(543890)]
        [Fact]
        public void ThisIndexerAccessInScript()
        {
            string test = @"
this[1]
";
            var compilation = CreateCompilationWithMscorlib(test, parseOptions: TestOptions.Interactive);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            compilation.VerifyDiagnostics(
                // (2,1): error CS0027: Keyword 'this' is not available in the current context
                // this[1]
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this"));

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ExpressionSyntax>().First();
            Assert.Equal(SyntaxKind.ElementAccessExpression, syntax.Kind());

            var summary = model.GetSemanticInfoSummary(syntax);
            Assert.Null(summary.Symbol);
            Assert.Equal(0, summary.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, summary.CandidateReason);
            Assert.Equal(TypeKind.Error, summary.Type.TypeKind);
            Assert.Equal(TypeKind.Error, summary.ConvertedType.TypeKind);
            Assert.Equal(Conversion.Identity, summary.ImplicitConversion);
            Assert.Equal(0, summary.MethodGroup.Length);
        }

        [WorkItem(540875)]
        [Fact]
        public void MainInScript2()
        {
            var text = @"static void Main() { }";

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.Script);

            var compilation = CreateCompilationWithMscorlib(tree, options: TestOptions.ReleaseExe.WithScriptClassName("Script"));

            compilation.VerifyDiagnostics(
                // (1,13): warning CS7022: The entry point of the program is global script code; ignoring 'Main()' entry point.
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main()"));
        }

        [Fact]
        public void Submission_TypeDisambiguationBasedUponAssemblyName()
        {
            var compilation = CreateCompilationWithMscorlib("namespace System { public struct Int32 { } }");

            compilation.VerifyDiagnostics();
        }

        [WorkItem(540875)]
        [Fact]
        public void MainInScript1()
        {
            var text = @"static void Main() { }";

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.Script);

            var compilation = CreateCompilationWithMscorlib(tree, options: TestOptions.ReleaseExe.WithScriptClassName("Script"));

            compilation.VerifyDiagnostics(
                // (1,13): warning CS7022: The entry point of the program is global script code; ignoring 'Main()' entry point.
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main()"));
        }
    }
}
