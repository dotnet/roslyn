// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.SimplifyLinqExpression
{
    internal abstract class AbstractSimplifyLinqExpressionDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private static readonly ImmutableArray<string> _nonEnumerableReturningLinqMethodNames =
            ImmutableArray.Create(
                nameof(Enumerable.First),
                nameof(Enumerable.Last),
                nameof(Enumerable.Single),
                nameof(Enumerable.Any),
                nameof(Enumerable.Count),
                nameof(Enumerable.SingleOrDefault),
                nameof(Enumerable.FirstOrDefault),
                nameof(Enumerable.LastOrDefault));

        public AbstractSimplifyLinqExpressionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.SimplifyLinqExpressionDiagnosticId,
                   EnforceOnBuildValues.SimplifyLinq,
                   option: null,
                   title: new LocalizableResourceString(nameof(AnalyzersResources.Simplify_Linq_expression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

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

            if (!TryGetLinqWhereExtensionMethods(enumerableType, out var whereMethodSymbol))
            {
                return;
            }

            if (!TryGetLinqMethodsThatDotNotReturnEnumerables(enumerableType, out var linqMethodSymbols))
            {
                return;
            }

            context.RegisterOperationAction(
                context => AnalyzeInvocationOperation(context, whereMethodSymbol, linqMethodSymbols),
                OperationKind.Invocation);

            static bool TryGetEnumerableTypeSymbol(Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? enumerableType)
            {
                if (typeof(Enumerable)?.FullName is string fullyQualifiedName)
                {
                    enumerableType = compilation.GetTypeByMetadataName(fullyQualifiedName);
                    return enumerableType is not null;
                }

                enumerableType = null;
                return false;
            }

            static bool TryGetLinqWhereExtensionMethods(INamedTypeSymbol enumerableType, [NotNullWhen(true)] out IMethodSymbol? whereMethod)
            {
                foreach (var whereMethodSymbol in enumerableType.GetMembers(nameof(Enumerable.Where)).OfType<IMethodSymbol>())
                {
                    var parameters = whereMethodSymbol.Parameters;
                    if (parameters.Length != 2)
                    {
                        continue;
                    }

                    if (parameters.Last().Type is INamedTypeSymbol systemFunc &&
                        systemFunc.Arity == 2)
                    {
                        whereMethod = whereMethodSymbol;
                        return true;
                    }
                }

                whereMethod = null;
                return false;
            }

            static bool TryGetLinqMethodsThatDotNotReturnEnumerables(INamedTypeSymbol enumerableType, out ImmutableArray<IMethodSymbol> linqMethods)
            {
                using var _ = ArrayBuilder<IMethodSymbol>.GetInstance(out var linqMethodSymbolsBuilder);
                foreach (var methodName in _nonEnumerableReturningLinqMethodNames)
                {
                    var methodSymbol = enumerableType.GetMembers(methodName).OfType<IMethodSymbol>();
                    linqMethodSymbolsBuilder.AddRange(methodSymbol);
                }

                if (linqMethodSymbolsBuilder.Count == 0)
                {
                    linqMethods = default;
                    return false;
                }

                linqMethods = linqMethodSymbolsBuilder.ToImmutable();
                return true;
            }
        }

        public void AnalyzeInvocationOperation(OperationAnalysisContext context, IMethodSymbol whereMethod, ImmutableArray<IMethodSymbol> linqMethods)
        {
            if (context.Operation.Syntax.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                // Do not analyze linq methods that contain diagnostics.
                return;
            }

            if (context.Operation is not IInvocationOperation invocation ||
                invocation.Arguments.Length != 1)
            {
                return;
            }

            if (invocation.Arguments.Single().Children.FirstOrDefault() is not IInvocationOperation previousInvocationInChain)
            {
                // Invocation is not part of a chain of invocations (i.e. Where(x => x is not null).First())
                return;
            }

            // Verify this is 'Where' followed by a non-enumerable returning method
            if (IsInvocationNonEnumerableReturningLinqMethod(invocation) &&
                IsWhereLinqMethod(previousInvocationInChain) &&
                previousInvocationInChain.Arguments.Length == 2 &&
                invocation.Arguments.Length == 1)
            {
                var memberAccessExpressionLocation = previousInvocationInChain.Syntax.GetLocation();
                var lambdaExpressionLocation = previousInvocationInChain.Arguments.Last().Syntax.GetLocation();

                context.ReportDiagnostic(
                    DiagnosticHelper.Create(
                        Descriptor,
                        invocation.Syntax.GetLocation(),
                        Descriptor.GetEffectiveSeverity(context.Compilation.Options),
                        additionalLocations: new[] { memberAccessExpressionLocation, lambdaExpressionLocation },
                        properties: null));
            }

            bool IsInvocationNonEnumerableReturningLinqMethod(IInvocationOperation invocation)
                => linqMethods.Any(m => m.Equals(invocation.TargetMethod.OriginalDefinition, SymbolEqualityComparer.Default));

            bool IsWhereLinqMethod(IInvocationOperation invocation)
                => whereMethod.Equals(invocation.TargetMethod.OriginalDefinition, SymbolEqualityComparer.Default);
        }
    }
}
