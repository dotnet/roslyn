// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.SimplifyLinqExpression
{
    internal abstract class AbstractSimplifyLinqExpressionDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private readonly ImmutableArray<string> _nonEnumerableReturningLinqMethodNames =
            ImmutableArray.Create(
                nameof(Enumerable.First),
                nameof(Enumerable.Last),
                nameof(Enumerable.Single),
                nameof(Enumerable.Any),
                nameof(Enumerable.Count),
                nameof(Enumerable.SingleOrDefault),
                nameof(Enumerable.FirstOrDefault),
                nameof(Enumerable.LastOrDefault)
            );

        public SimplifyLinqExpressionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.SimplifyLinqExpressionDiagnosticId,
                   EnforceOnBuildValues.SimplifyLinq,
                   option: null,
                   title: new LocalizableResourceString(
                       nameOfLocalizableResource: nameof(AnalyzersResources.Simplify_Linq_expression),
                       resourceManager: AnalyzersResources.ResourceManager,
                       resourceSource: typeof(AnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(OnCompilationStart);

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            if ((typeof(Enumerable)?.FullName is not string fullyQualifiedName) ||
                (context.Compilation.GetTypeByMetadataName(fullyQualifiedName) is not INamedTypeSymbol enumerableType))
            {
                return;
            }

            var whereMethodSymbols = enumerableType.GetMembers(nameof(Enumerable.Where)).OfType<IMethodSymbol>().ToImmutableArray();
            var linqMethodSymbolsBuilder = ArrayBuilder<IMethodSymbol>.GetInstance();

            foreach (var methodName in _nonEnumerableReturningLinqMethodNames)
            {
                linqMethodSymbolsBuilder.AddRange(enumerableType.GetMembers(methodName).OfType<IMethodSymbol>());
            }

            var linqMethodSymbols = linqMethodSymbolsBuilder.ToImmutableAndFree();
            if (whereMethodSymbols.IsEmpty || linqMethodSymbols.IsEmpty)
            {
                return;
            }

            context.RegisterOperationAction(
                context => AnalyzeInvocationOperation(context, whereMethodSymbols, linqMethodSymbols), OperationKind.Invocation);
        }

        public void AnalyzeInvocationOperation(OperationAnalysisContext context, ImmutableArray<IMethodSymbol> whereMethods, ImmutableArray<IMethodSymbol> linqMethods)
        {
            if (context.Operation.Syntax.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                return;
            }

            if (context.Operation is not IInvocationOperation invocation ||
                invocation.Arguments.Length != 1)
            {
                return;
            }
            
            var argument = invocation.Arguments.Single();

            if (linqMethods.Any(m => m.Equals(invocation.TargetMethod.OriginalDefinition, SymbolEqualityComparer.Default)) &&
                argument.Children.FirstOrDefault() is IInvocationOperation whereInvocation &&
                whereMethods.Any(m => m.Equals(whereInvocation.TargetMethod.OriginalDefinition, SymbolEqualityComparer.Default)))
            {
                context.ReportDiagnostic(
                    DiagnosticHelper.Create(
                        Descriptor,
                        invocation.Syntax.GetLocation(),
                        Descriptor.GetEffectiveSeverity(context.Compilation.Options),
                        additionalLocations: null,
                        properties: null));
            }
        }
    }
}
