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

    protected readonly ImmutableArray<ITestFrameworkMetadata> TestFrameworkMetadata = testFrameworks.ToImmutableArray();

    protected abstract bool IsTestMethod(TMethodDeclaration method);

    protected abstract bool DescendIntoChildren(SyntaxNode node);

    public async Task<ImmutableArray<SyntaxNode>> GetPotentialTestMethodsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var testNodes = await GetPotentialTestNodesAsync(document, textSpan, cancellationToken).ConfigureAwait(false);

        // Find any test methods that intersect with the requested span.
        var intersectingNodes = testNodes.WhereAsArray(node => node.Span.IntersectsWith(textSpan));
        if (!intersectingNodes.IsEmpty)
        {
            return intersectingNodes;
        }

        // We might have been invoked on a test class.  Check if any of the test method parent nodes intersect with the requested text span.
        return testNodes.WhereAsArray(node => node.Parent?.Span.IntersectsWith(textSpan) == true);
    }

    public bool IsMatch(SemanticModel semanticModel, SyntaxNode node, string fullyQualifiedTestName, CancellationToken cancellationToken)
    {
        var method = (TMethodDeclaration)node;

        // Since discovered tests are not guarantied to run on a particular snapshot, we match optimistically based on test name.
        var methodSymbol = semanticModel.GetRequiredDeclaredSymbol(method, cancellationToken);

        // Do a quicker check to see if the given FQN even contains the method name before doing a full match.
        if (!fullyQualifiedTestName.Contains(methodSymbol.Name))
        {
            return false;
        }

        var fullyQualifiedMethodName = methodSymbol.ToDisplayString(s_methodSymbolNoParametersDisplayFormat);

        // Qualified test names use a '+' to separate outer classes from nested classes whereas display strings use '.'.
        fullyQualifiedTestName = fullyQualifiedTestName.Replace('+', '.');

        // The definition of fully qualified name varies depending on the test framework.
        // For example, XUnit will never include parameters in the FQN it gives to us.
        // However NUnit will give us a FQN with the actual parameter values passed in (e.g. if there's an int parameter, it will pass in the value of the int).
        // To avoid these problems, we compare our method FQN (without parameters) against the test framework FQN with everything past the first open paren removed.
        var indexOfOpenParen = fullyQualifiedTestName.IndexOf('(');
        if (indexOfOpenParen != -1)
        {
            fullyQualifiedTestName = fullyQualifiedTestName.Remove(indexOfOpenParen);
        }

        return fullyQualifiedMethodName == fullyQualifiedTestName;
    }

    public bool IsTestMethod(SyntaxNode node)
    {
        return node is TMethodDeclaration method && IsTestMethod(method);
    }

    private async Task<ImmutableArray<SyntaxNode>> GetPotentialTestNodesAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var methodsInRange = root.DescendantNodesAndSelf(descendIntoChildren: ShouldDescend, descendIntoTrivia: false).OfType<TMethodDeclaration>();

        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var testMethods);
        foreach (var method in methodsInRange)
        {
            if (IsTestMethod(method))
            {
                testMethods.Add(method);
            }
        }

        return testMethods.ToImmutableArray();

        bool ShouldDescend(SyntaxNode node)
        {
            if (node is ICompilationUnitSyntax)
            {
                return true;
            }

            // If the text span doesn't intersect with the node at all we don't need to explore it.
            return node.Span.IntersectsWith(textSpan) && DescendIntoChildren(node);
        }
    }
}
