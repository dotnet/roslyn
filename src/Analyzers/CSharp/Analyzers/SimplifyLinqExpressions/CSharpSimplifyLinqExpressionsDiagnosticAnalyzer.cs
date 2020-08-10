// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpressions
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyLinqExpressionsDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {

        public CSharpSimplifyLinqExpressionsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.SimplifyLinqExpressionsDiagnosticId,
                  option: null,
                  title: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Simplify_linq_expressions), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var whereMethods = ImmutableHashSet.CreateBuilder<IMethodSymbol>();
            var linqMethods = ImmutableHashSet.CreateBuilder<IMethodSymbol>();
            var validLinqCalls = new List<string> { "First", "Last", "Single", "Any", "Count", "SingleOrDefault", "FirstOrDefault", "LastOrDefault" };

            var enumNamedType = context.Compilation.GetTypeByMetadataName("System.Linq.Enumerable");
            var queryNamedType = context.Compilation.GetTypeByMetadataName("System.Linq.Queryable");
            var enumMethods = enumNamedType?.GetMembers("Where").OfType<IMethodSymbol>();
            var queryMethods = queryNamedType?.GetMembers("Where").OfType<IMethodSymbol>();
            AddIfNotNull(whereMethods, enumMethods);
            AddIfNotNull(whereMethods, queryMethods);

            // add all valid linq calls
            foreach (var id in validLinqCalls)
            {
                enumMethods = enumNamedType?.GetMembers(id).OfType<IMethodSymbol>();
                queryMethods = queryNamedType?.GetMembers(id).OfType<IMethodSymbol>();
                AddIfNotNull(linqMethods, enumMethods);
                AddIfNotNull(linqMethods, queryMethods);
            }

            if (whereMethods.Count > 0 && linqMethods.Count > 0)
            {
                context.RegisterOperationAction(
                    context => AnalyzeAction(
                        context, whereMethods.ToImmutable(), linqMethods.ToImmutable()), OperationKind.Invocation);
            }

            static void AddIfNotNull(ImmutableHashSet<IMethodSymbol>.Builder set, IEnumerable<IMethodSymbol>? others)
            {
                if (others != null)
                {
                    set.UnionWith(others);
                }
            }
        }

        public void AnalyzeAction(OperationAnalysisContext context, ImmutableHashSet<IMethodSymbol> whereMethods, ImmutableHashSet<IMethodSymbol> linqMethods)
        {
            var invocationOperation = (IInvocationOperation)context.Operation;
            var semanticModel = context.Operation.SemanticModel;
            // Check that .Where(...) is not user defined
            var child = invocationOperation.Children.FirstOrDefault(c => c is IArgumentOperation);
            var whereClause = child?.Children.FirstOrDefault(c => c is IInvocationOperation);
            var isLambda = whereClause?.Children.Any(c => c.Syntax.IsKind(SyntaxKind.Argument) &&
                                                    ((ArgumentSyntax)c.Syntax).Expression.IsKind(SyntaxKind.SimpleLambdaExpression)) ?? false;

            if (whereClause == null ||
                !(whereClause is IInvocationOperation method) ||
                method.TargetMethod.OriginalDefinition == null ||
                !isLambda ||
                !whereMethods.Contains(method.TargetMethod.OriginalDefinition))
            {
                return;
            }

            // check that the Where clause is followed by a call to a valid method i.e. one of First, FirstOrDefault, Single, SingleOrDefault, etc..
            // and that it is also not user defined
            var originalDefinition = (invocationOperation.TargetMethod.ReducedFrom ?? invocationOperation.TargetMethod).OriginalDefinition;
            if (!linqMethods.Contains(originalDefinition))
            {
                return;
            }

            // Check that the Where clause is followed by a call with no predicate
            var arguments = invocationOperation.TargetMethod.Parameters;
            if (arguments.IsEmpty)
            {
                return;
            }

            var operation = context.Operation;
            var node = operation.Syntax;
            var location = node.GetLocation();
            context.ReportDiagnostic(
                DiagnosticHelper.Create(Descriptor, location, Descriptor.GetEffectiveSeverity(context.Compilation.Options),
                additionalLocations: null, properties: null));
        }
    }
}
