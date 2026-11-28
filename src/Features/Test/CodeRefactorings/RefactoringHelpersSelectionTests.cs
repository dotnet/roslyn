// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeRefactorings;

[UseExportProvider]
public sealed class RefactoringHelpersSelectionTests
{
    [Theory]
    [InlineData("class C { void M() { M()[|;|] } }")]
    [InlineData("class C { void M() { [|M()|]; } }")]
    [InlineData("class C { void M() { [|M();|] } }")]
    public async Task ExpressionSelectionIncludesStatementSemicolon(string markup)
    {
        using var workspace = TestWorkspace.CreateCSharp(markup);
        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var selection = workspace.Documents.Single().SelectedSpans.Single();

        var nodes = await document.GetRelevantNodesAsync<ExpressionSyntax>(selection, allowEmptyNodes: false, CancellationToken.None);

        var expression = Assert.IsType<InvocationExpressionSyntax>(nodes.FirstOrDefault());
        Assert.Equal("M()", expression.ToString());
        Assert.False(CodeRefactoringHelpers.IsNodeUnderselected(expression, selection));
    }
}
