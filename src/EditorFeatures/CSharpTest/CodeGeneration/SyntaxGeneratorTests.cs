// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeGeneration
{
    [UseExportProvider]
    public class SyntaxGeneratorTests
    {
        [Fact]
        public async Task TestNameOfBindsWithoutErrors()
        {
            var g = CSharpSyntaxGenerator.Instance;

            using var workspace = TestWorkspace.CreateCSharp(@"
class C
{
    string M()
    {
        return ""a"";
    }
}");

            var solution = workspace.CurrentSolution;
            var document = solution.Projects.Single().Documents.Single();
            var root = await document.GetSyntaxRootAsync();

            // validate that if we change `return "a";` to `return nameof(M);` that this binds
            // without a problem.  We need to do special work in SyntaxGenerator.NameOfExpression to
            // make this happen.
            var statement = root.DescendantNodes().Single(n => n is ReturnStatementSyntax);
            var replacement = g.ReturnStatement(g.NameOfExpression(g.IdentifierName("M")));

            var newRoot = root.ReplaceNode(statement, replacement);
            var newDocument = document.WithSyntaxRoot(newRoot);

            var semanticModel = await newDocument.GetSemanticModelAsync();
            var diagnostics = semanticModel.GetDiagnostics();

            Assert.Empty(diagnostics);
        }
    }
}
