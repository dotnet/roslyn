// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static UseCollectionExpressionHelpers;

/// <summary>
/// Analyzer/fixer that looks for code of the form <c>X.Empty&lt;T&gt;()</c> or <c>X&lt;T&gt;.Empty</c> and offers to
/// replace with <c>[]</c> if legal to do so.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForFluentDiagnosticAnalyzer
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer
{
    private static readonly ImmutableArray<string> s_prefixes = ImmutableArray.Create("To", "As");
    private static readonly ImmutableArray<string> s_suffixes = ImmutableArray.Create(
        nameof(Array),
        nameof(Span<int>),
        nameof(ReadOnlySpan<int>),
        nameof(Enumerable),
        nameof(List<int>),
        nameof(HashSet<int>),
        nameof(LinkedList<int>),
        nameof(Queue<int>),
        nameof(SortedSet<int>),
        nameof(Stack<int>),
        nameof(IEnumerable<int>),
        nameof(ICollection<int>),
        nameof(IReadOnlyCollection<int>),
        nameof(IList<int>),
        nameof(IReadOnlyList<int>),
        nameof(ImmutableArray<int>),
        nameof(ImmutableHashSet<int>),
        nameof(ImmutableList<int>),
        nameof(ImmutableQueue<int>),
        nameof(ImmutableSortedSet<int>),
        nameof(ImmutableStack<int>));


    public CSharpUseCollectionExpressionForFluentDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseCollectionExpressionForFluentDiagnosticId,
               EnforceOnBuildValues.UseCollectionExpressionForFluent)
    {
    }

    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context)
        => context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);

    private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
            return;

        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        if (memberAccess.Parent is not InvocationExpressionSyntax invocation)
            return;

        // We want to analyze and report on the highest applicable invocation in an invocation chain.
        // So bail out if our parent is a match.
        if (invocation.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax parentInvocation } parentMemberAccess &&
            IsSyntacticMatch(parentMemberAccess, parentInvocation))
        {
            return;
        }

        if (!AnalyzeExpression(semanticModel, invocation))
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            memberAccess.Name.Identifier.GetLocation(),
            option.Notification.Severity,
            additionalLocations: ImmutableArray.Create(invocation.GetLocation()),
            properties: null));

        return;
    }

    /// <summary>
    /// Analyzes an expression looking for one of the form <c>CollectionCreation</c>, followed by some number of 
    /// <c>.Add(...)/.AddRange(...)</c> or <c>.ToXXX()</c> calls
    /// </summary>
    private static bool AnalyzeExpression(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
    {
        if (!AnalyzeExpressionRecursive(semanticModel, invocation))
            return false;

        if (!UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(semanticModel, invocation, skipVerificationForReplacedNode: true, cancellationToken))
            return false;

        return true;
    }

    private static bool AnalyzeExpressionRecursive(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        if (!IsSyntacticMatch(memberAccess, invocation))
            return false;

        using var _ = ArrayBuilder<ExpressionSyntax>.GetInstance(out var stack);
        stack.Push(memberAccess.Expression);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            // Methods of the form Add(...)/AddRange(...) or `ToXXX()` count as something to continue recursing down the
            // left hand side of the expression.
            if (current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax currentMemberAccess } currentInvocation &&
                IsSyntacticMatch(currentMemberAccess, currentInvocation))
            {
                stack.Push(currentMemberAccess.Expression);
                continue;
            }

            // `new int[] { ... }` or `new[] { ... }` is a fine base case to make a collection out of.
            if (current is ArrayCreationExpressionSyntax { Initializer: not null } or ImplicitArrayCreationExpressionSyntax)
                return true;

            // `new X()` or `new X { a, b, c}` or `new X() { a, b, c }` are fine base cases.
            if (current is ObjectCreationExpressionSyntax
                {
                    ArgumentList: null or { Arguments.Count: 0 },
                    Initializer: null or { RawKind: (int)SyntaxKind.CollectionInitializerExpression }
                })
            {
                return true;
            }

            // Forms like `Array.Empty<int>()` or `ImmutableArray<int>.Empty` are fine base cases.
            if (IsCollectionEmptyAccess(semanticModel, current))
                return true;

            // Forms like `ImmutableArray.Create(...)` or `ImmutableArray.CreateRange(...)` are fine base cases.
            if (IsCollectionFactoryCreate(semanticModel, current))
                return true;

            // Something we didn't understand.
            return false;
        }

        return false;
    }

    private static bool IsSyntacticMatch(
        MemberAccessExpressionSyntax memberAccess,
        InvocationExpressionSyntax invocation)
    {
        if (memberAccess.Kind() != SyntaxKind.SimpleMemberAccessExpression)
            return false;

        var name = memberAccess.Name.Identifier.ValueText;
        if (name is nameof(ImmutableArray<int>.Add) or nameof(ImmutableArray<int>.AddRange))
            return true;

        if (invocation.ArgumentList.Arguments.Count > 0)
            return false;

        return IsNameMatch(name);

        static bool IsNameMatch(string name)
        {
            foreach (var prefix in s_prefixes)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                    return HasSuffix(name, prefix);
            }

            return false;
        }

        static bool HasSuffix(string name, string prefix)
        {
            foreach (var suffix in s_suffixes)
            {
                if (name.Length == (prefix.Length + suffix.Length) &&
                    name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
