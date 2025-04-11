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

internal abstract class AbstractSimplifyLinqExpressionDiagnosticAnalyzer<TInvocationExpressionSyntax, TMemberAccessExpressionSyntax>()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.SimplifyLinqExpressionDiagnosticId,
        EnforceOnBuildValues.SimplifyLinqExpression,
        option: null,
        title: new LocalizableResourceString(nameof(AnalyzersResources.Simplify_LINQ_expression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    where TInvocationExpressionSyntax : SyntaxNode
    where TMemberAccessExpressionSyntax : SyntaxNode
{
    private static readonly ImmutableHashSet<string> s_nonEnumerableReturningLinqPredicateMethodNames =
        [
            nameof(Enumerable.First),
            nameof(Enumerable.Last),
            nameof(Enumerable.Single),
            nameof(Enumerable.Any),
            nameof(Enumerable.Count),
            nameof(Enumerable.SingleOrDefault),
            nameof(Enumerable.FirstOrDefault),
            nameof(Enumerable.LastOrDefault),
        ];
    private static readonly ImmutableHashSet<string> s_nonEnumerableReturningLinqSelectorMethodNames =
        [
            nameof(Enumerable.Average),
            nameof(Enumerable.Sum),
            nameof(Enumerable.Min),
            nameof(Enumerable.Max),
        ];

    protected abstract ISyntaxFacts SyntaxFacts { get; }

    protected abstract bool ConflictsWithMemberByNameOnly { get; }

    protected abstract IInvocationOperation? TryGetNextInvocationInChain(IInvocationOperation invocation);

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(OnCompilationStart);

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        if (TryGetEnumerableTypeSymbol(context.Compilation, out var enumerableType) &&
            TryGetLinqWhereExtensionMethod(enumerableType, out var whereMethodSymbol) &&
            TryGetLinqSelectExtensionMethod(enumerableType, out var selectMethodSymbol) &&
            TryGetLinqMethodsThatDoNotReturnEnumerables(enumerableType, out var linqMethods))
        {
            context.RegisterOperationAction(AnalyzeInvocationOperation, OperationKind.Invocation);
        }

        return;

        static bool TryGetEnumerableTypeSymbol(Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? enumerableType)
        {
            enumerableType = compilation.GetTypeByMetadataName(typeof(Enumerable)?.FullName!);
            return enumerableType is not null;
        }

        static bool TryGetLinqWhereExtensionMethod(INamedTypeSymbol enumerableType, [NotNullWhen(true)] out IMethodSymbol? linqMethod)
            => TryGetLinqExtensionMethod(enumerableType, nameof(Enumerable.Where), out linqMethod);

        static bool TryGetLinqSelectExtensionMethod(INamedTypeSymbol enumerableType, [NotNullWhen(true)] out IMethodSymbol? linqMethod)
            => TryGetLinqExtensionMethod(enumerableType, nameof(Enumerable.Select), out linqMethod);

        static bool TryGetLinqExtensionMethod(INamedTypeSymbol enumerableType, string name, [NotNullWhen(true)] out IMethodSymbol? linqMethod)
        {
            foreach (var linqMethodSymbol in enumerableType.GetMembers(name).OfType<IMethodSymbol>())
            {
                if (linqMethodSymbol.Parameters is [_, { Type: INamedTypeSymbol { Arity: 2 } }])
                {
                    // This is the Where/Select overload that does not take and index (i.e. Where(source, Func<T, bool>)
                    // vs Where(source, Func<T, int, bool>))
                    linqMethod = linqMethodSymbol;
                    return true;
                }
            }

            linqMethod = null;
            return false;
        }

        static bool TryGetLinqMethodsThatDoNotReturnEnumerables(INamedTypeSymbol enumerableType, out ImmutableArray<IMethodSymbol> linqMethods)
        {
            using var _ = ArrayBuilder<IMethodSymbol>.GetInstance(out var linqMethodSymbolsBuilder);

            foreach (var method in enumerableType.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.Parameters.Length != 1)
                    continue;

                if (s_nonEnumerableReturningLinqPredicateMethodNames.Contains(method.Name) ||
                    s_nonEnumerableReturningLinqSelectorMethodNames.Contains(method.Name))
                {
                    linqMethodSymbolsBuilder.AddRange(method);
                }
            }

            linqMethods = linqMethodSymbolsBuilder.ToImmutable();
            return linqMethods.Any();
        }

        void AnalyzeInvocationOperation(OperationAnalysisContext context)
        {
            if (ShouldSkipAnalysis(context, notification: null))
                return;

            // Do not analyze linq methods that contain diagnostics.
            if (context.Operation.Syntax.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
                return;

            // we only care about Where/Select invocation methods on linq expressions

            if (context.Operation is not IInvocationOperation invocation)
                return;

            var isWhereMethod = IsWhereLinqMethod(invocation);
            var isSelectMethod = IsSelectLinqMethod(invocation);
            if (!isWhereMethod && !isSelectMethod)
                return;

            if (TryGetNextInvocationInChain(invocation) is not IInvocationOperation nextInvocation ||
                !IsInvocationNonEnumerableReturningLinqMethod(nextInvocation))
            {
                // Invocation is not part of a chain of invocations (i.e. Where(x => x is not null).First())
                return;
            }

            if (TryGetSymbolOfMemberAccess(invocation) is not ITypeSymbol targetTypeSymbol ||
                TryGetMethodName(nextInvocation) is not string name)
            {
                return;
            }

            if (isWhereMethod && !s_nonEnumerableReturningLinqPredicateMethodNames.Contains(name))
                return;

            if (isSelectMethod && !s_nonEnumerableReturningLinqSelectorMethodNames.Contains(name))
                return;

            // Do not offer to transpose if there is already a method on the collection named the same as the linq extension
            // method.  This would cause us to call the instance method after the transformation, not the extension method.
            if (!targetTypeSymbol.Equals(enumerableType, SymbolEqualityComparer.Default))
            {
                var members = targetTypeSymbol.GetMembers(name);
                if (members.Length > 0)
                {
                    // VB conflicts if any member has the same name (like a Count property vs Count extension method).
                    if (this.ConflictsWithMemberByNameOnly)
                        return;

                    // C# conflicts only if it is a method as well.  So a Count property will not conflict with a Count
                    // extension method.
                    if (members.Any(m => m is IMethodSymbol))
                        return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, nextInvocation.Syntax.GetLocation()));
        }

        bool IsWhereLinqMethod(IInvocationOperation invocation)
            => whereMethodSymbol.Equals(invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod.OriginalDefinition, SymbolEqualityComparer.Default);

        bool IsSelectLinqMethod(IInvocationOperation invocation)
            => selectMethodSymbol.Equals(invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod.OriginalDefinition, SymbolEqualityComparer.Default);

        bool IsInvocationNonEnumerableReturningLinqMethod(IInvocationOperation invocation)
            => linqMethods.Any(static (m, invocation) => m.Equals(invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod.OriginalDefinition, SymbolEqualityComparer.Default), invocation);

        ITypeSymbol? TryGetSymbolOfMemberAccess(IInvocationOperation invocation)
        {
            if (invocation.Syntax is not TInvocationExpressionSyntax invocationNode ||
                SyntaxFacts.GetExpressionOfInvocationExpression(invocationNode) is not TMemberAccessExpressionSyntax memberAccess ||
                SyntaxFacts.GetExpressionOfMemberAccessExpression(memberAccess) is not SyntaxNode expression)
            {
                return null;
            }

            return invocation.SemanticModel?.GetTypeInfo(expression).Type;
        }

        string? TryGetMethodName(IInvocationOperation invocation)
        {
            if (invocation.Syntax is not TInvocationExpressionSyntax invocationNode ||
                SyntaxFacts.GetExpressionOfInvocationExpression(invocationNode) is not TMemberAccessExpressionSyntax memberAccess)
            {
                return null;
            }

            var memberName = SyntaxFacts.GetNameOfMemberAccessExpression(memberAccess);
            var identifier = SyntaxFacts.GetIdentifierOfSimpleName(memberName);
            return identifier.ValueText;
        }
    }
}
