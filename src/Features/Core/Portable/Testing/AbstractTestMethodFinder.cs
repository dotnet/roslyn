// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Features.Testing;

internal abstract class AbstractTestMethodFinder<TMethodDeclaration>(IEnumerable<ITestFrameworkMetadata> testFrameworks) : ITestMethodFinder where TMethodDeclaration : SyntaxNode
{
    /// <summary>
    /// Output the method symbol as a fully qualified method name, e.g. Namespace.Class.Method to match what test discovery gives us.
    /// Generics are not applicable here - none of our supported test frameworks allow generic test classes / methods.
    /// </summary>
    private static readonly SymbolDisplayFormat s_methodSymbolNoParametersDisplayFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

    /// <summary>
    /// Sometimes the test runner outputs fully qualified method names with parameters.  So we also check for that scenario.
    /// </summary>
    private static readonly SymbolDisplayFormat s_methodSymbolWithParametersDisplayFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters);

    protected readonly ImmutableArray<ITestFrameworkMetadata> TestFrameworkMetadata = testFrameworks.ToImmutableArray();

    protected abstract Task<bool> IsTestMethodAsync(Document document, TMethodDeclaration method, CancellationToken cancellationToken);

    public async Task<ImmutableArray<SyntaxNode>> GetPotentialTestMethodsAsync(TextSpan textSpan, Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var testNodes = await GetPotentialTestMethodsAsync(document, cancellationToken).ConfigureAwait(false);

        // Find any test methods that intersect with the requested span.
        var intersectingNodes = testNodes.Where(node => node.Span.IntersectsWith(textSpan)).ToImmutableArray();
        if (!intersectingNodes.IsEmpty)
        {
            return intersectingNodes;
        }

        // We might have been invoked on a test class.  Check if any of the test method parent nodes intersect with the requested text span.
        return testNodes.Where(node => node.Parent?.Span.IntersectsWith(textSpan) == true).ToImmutableArray();
    }

    public async Task<bool> IsMatchAsync(SyntaxNode node, string fullyQualifiedTestName, Document document, CancellationToken cancellationToken)
    {
        var method = node as TMethodDeclaration;
        Contract.ThrowIfNull(method, $"Node with kind {node.RawKind} is not a method");

        // Since discovered tests are not guarantied to run on a particular snapshot, we match optimistically based on test name.
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var methodSymbol = semanticModel.GetDeclaredSymbol(method, cancellationToken);
        Contract.ThrowIfNull(methodSymbol, "Test method has no symbol");

        var fullyQualifiedMethodName = methodSymbol.ToDisplayString(s_methodSymbolNoParametersDisplayFormat);
        var fullyQualifiedMethodNameWithParameters = methodSymbol.ToDisplayString(s_methodSymbolWithParametersDisplayFormat);
        return fullyQualifiedMethodName == fullyQualifiedTestName || fullyQualifiedMethodNameWithParameters == fullyQualifiedTestName;
    }

    public Task<bool> IsTestMethodAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is TMethodDeclaration method ? IsTestMethodAsync(document, method, cancellationToken) : SpecializedTasks.False;
    }

    private async Task<ImmutableArray<SyntaxNode>> GetPotentialTestMethodsAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var methodsInRange = root.DescendantNodesAndSelf(descendIntoTrivia: false).OfType<TMethodDeclaration>();

        using var _ = ArrayBuilder<TMethodDeclaration>.GetInstance(out var testMethods);
        foreach (var method in methodsInRange)
        {
            var isTestMethod = await IsTestMethodAsync(document, method, cancellationToken).ConfigureAwait(false);
            if (isTestMethod)
            {
                testMethods.Add(method);
            }
        }

        return testMethods.Cast<SyntaxNode>().ToImmutableArray();
    }
}
