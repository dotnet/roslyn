// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.SimplifyLinqExpression;

internal abstract class AbstractSimplifyLinqExpressionDiagnosticAnalyzer<TInvocationExpressionSyntax, TMemberAccessExpressionSyntax> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TInvocationExpressionSyntax : SyntaxNode
    where TMemberAccessExpressionSyntax : SyntaxNode
{
    private static readonly IImmutableSet<string> s_nonEnumerableReturningLinqMethodNames =
        ImmutableHashSet.Create(
            nameof(Enumerable.First),
            nameof(Enumerable.Last),
            nameof(Enumerable.Single),
            nameof(Enumerable.Any),
            nameof(Enumerable.Count),
            nameof(Enumerable.SingleOrDefault),
            nameof(Enumerable.FirstOrDefault),
            nameof(Enumerable.LastOrDefault));

    protected abstract ISyntaxFacts SyntaxFacts { get; }

    public AbstractSimplifyLinqExpressionDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.SimplifyLinqExpressionDiagnosticId,
               EnforceOnBuildValues.SimplifyLinqExpression,
               option: null,
               title: new LocalizableResourceString(nameof(AnalyzersResources.Simplify_LINQ_expression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
    }

    protected abstract IInvocationOperation? TryGetNextInvocationInChain(IInvocationOperation invocation);

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(OnCompilationStart);

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        if (!TryGetEnumerableTypeSymbol(context.Compilation, out var enumerableType))
        {
            return;
        }

        if (!TryGetLinqWhereExtensionMethod(enumerableType, out var whereMethodSymbol))
        {
            return;
        }

        if (!TryGetLinqMethodsThatDoNotReturnEnumerables(enumerableType, out var linqMethodSymbols))
        {
            return;
        }

        context.RegisterOperationAction(
            context => AnalyzeInvocationOperation(context, enumerableType, whereMethodSymbol, linqMethodSymbols),
            OperationKind.Invocation);

        return;

        static bool TryGetEnumerableTypeSymbol(Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? enumerableType)
        {
            enumerableType = compilation.GetTypeByMetadataName(typeof(Enumerable)?.FullName!);
            return enumerableType is not null;
        }

        static bool TryGetLinqWhereExtensionMethod(INamedTypeSymbol enumerableType, [NotNullWhen(true)] out IMethodSymbol? whereMethod)
        {
            foreach (var whereMethodSymbol in enumerableType.GetMembers(nameof(Enumerable.Where)).OfType<IMethodSymbol>())
            {
                var parameters = whereMethodSymbol.Parameters;

                if (parameters is [_, { Type: INamedTypeSymbol { Arity: 2 } }])
                {
                    // This is the where overload that does not take and index (i.e. Where(source, Func<T, bool>) vs Where(source, Func<T, int, bool>))
                    whereMethod = whereMethodSymbol;
                    return true;
                }
            }

            whereMethod = null;
            return false;
        }

        static bool TryGetLinqMethodsThatDoNotReturnEnumerables(INamedTypeSymbol enumerableType, out ImmutableArray<IMethodSymbol> linqMethods)
        {
            using var _ = ArrayBuilder<IMethodSymbol>.GetInstance(out var linqMethodSymbolsBuilder);
            foreach (var method in enumerableType.GetMembers().OfType<IMethodSymbol>())
            {
                if (s_nonEnumerableReturningLinqMethodNames.Contains(method.Name) &&
                    method.Parameters is { Length: 1 })
                {
                    linqMethodSymbolsBuilder.AddRange(method);
                }
            }

            linqMethods = linqMethodSymbolsBuilder.ToImmutable();
            return linqMethods.Any();
        }
    }

    public void AnalyzeInvocationOperation(OperationAnalysisContext context, INamedTypeSymbol enumerableType, IMethodSymbol whereMethod, ImmutableArray<IMethodSymbol> linqMethods)
    {
        if (ShouldSkipAnalysis(context, notification: null))
            return;

        if (context.Operation.Syntax.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            // Do not analyze linq methods that contain diagnostics.
            return;
        }

        if (context.Operation is not IInvocationOperation invocation ||
            !IsWhereLinqMethod(invocation))
        {
            // we only care about Where methods on linq expressions
            return;
        }

        if (TryGetNextInvocationInChain(invocation) is not IInvocationOperation nextInvocation ||
            !IsInvocationNonEnumerableReturningLinqMethod(nextInvocation))
        {
            // Invocation is not part of a chain of invocations (i.e. Where(x => x is not null).First())
            return;
        }

        if (TryGetSymbolOfMemberAccess(invocation) is not INamedTypeSymbol targetTypeSymbol ||
            TryGetMethodName(nextInvocation) is not string name)
        {
            return;
        }

        if (!targetTypeSymbol.Equals(enumerableType, SymbolEqualityComparer.Default) &&
            targetTypeSymbol.MemberNames.Contains(name))
        {
            // Do not offer to transpose if there is already a member on the collection named the same as the linq extension method
            // example: list.Where(x => x != null).Count() cannot be changed to list.Count(x => x != null) as List<T> already has a member named Count
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptor, nextInvocation.Syntax.GetLocation()));

        return;

        bool IsWhereLinqMethod(IInvocationOperation invocation)
            => whereMethod.Equals(invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod.OriginalDefinition, SymbolEqualityComparer.Default);

        bool IsInvocationNonEnumerableReturningLinqMethod(IInvocationOperation invocation)
            => linqMethods.Any(predicate: static (m, invocation) => m.Equals(invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod.OriginalDefinition, SymbolEqualityComparer.Default), arg: invocation);

        INamedTypeSymbol? TryGetSymbolOfMemberAccess(IInvocationOperation invocation)
        {
            if (invocation.Syntax is TInvocationExpressionSyntax invocationNode &&
                SyntaxFacts.GetExpressionOfInvocationExpression(invocationNode) is TMemberAccessExpressionSyntax memberAccess &&
                SyntaxFacts.GetExpressionOfMemberAccessExpression(memberAccess) is SyntaxNode expression)
            {
                return invocation.SemanticModel?.GetTypeInfo(expression).Type as INamedTypeSymbol;
            }

            return null;
        }

        string? TryGetMethodName(IInvocationOperation invocation)
        {
            if (invocation.Syntax is TInvocationExpressionSyntax invocationNode &&
                SyntaxFacts.GetExpressionOfInvocationExpression(invocationNode) is TMemberAccessExpressionSyntax memberAccess)
            {
                return SyntaxFacts.GetNameOfMemberAccessExpression(memberAccess).GetText().ToString();
            }

            return null;
        }
    }
}
