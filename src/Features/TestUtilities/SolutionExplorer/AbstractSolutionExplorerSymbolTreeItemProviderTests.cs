// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionExplorer;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.SolutionExplorer;

public abstract class AbstractSolutionExplorerSymbolTreeItemProviderTests
{
    protected abstract TestWorkspace CreateWorkspace(string code);

    protected async Task TestNode<TNode>(
        string code, string expected, bool returnNamespaces = false) where TNode : SyntaxNode
    {
        using var workspace = CreateWorkspace(code);
        var testDocument = workspace.Documents.Single();
        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);

        var service = document.GetRequiredLanguageService<ISolutionExplorerSymbolTreeItemProvider>();

        var node = root.DescendantNodesAndSelf().OfType<TNode>().First();
        var diagnostics = node.GetDiagnostics();
        Assert.Empty(diagnostics);

        var items = service.GetItems(document.Id, node, returnNamespaces, CancellationToken.None);

        var actual = string.Join("\r\n", items);
        AssertEx.SequenceEqual(
            expected.Trim().Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()),
            items.Select(i => i.ToString()));

        AssertEx.SequenceEqual(
            testDocument.SelectedSpans,
            items.Select(i => i.ItemSyntax.NavigationToken.Span));
    }
}
