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
using System.Linq.Expressions;
#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpressions
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyLinqExpressionsDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        internal readonly ImmutableArray<string> s_validLinqCalls = ImmutableArray.Create(
                nameof(Enumerable.First),
                nameof(Enumerable.Last),
                nameof(Enumerable.Single),
                nameof(Enumerable.Any),
                nameof(Enumerable.Count),
                nameof(Enumerable.SingleOrDefault),
                nameof(Enumerable.FirstOrDefault),
                nameof(Enumerable.LastOrDefault)
            );

        public CSharpSimplifyLinqExpressionsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.SimplifyLinqExpressionsDiagnosticId,
                   option: null,
                   title: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Simplify_linq_expression), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
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

            var enumNamedType = context.Compilation.GetTypeByMetadataName(typeof(System.Linq.Enumerable).FullName);
            if (enumNamedType is null)
            {
                return;
            }

            var enumMethods = enumNamedType.GetMembers(nameof(Enumerable.Where)).OfType<IMethodSymbol>();

            if (enumMethods is null)
            {
                return;
            }

            AddIfNotNull(whereMethods, enumMethods);

            // add all valid linq calls
            foreach (var id in s_validLinqCalls)
            {
                enumMethods = enumNamedType?.GetMembers(id).OfType<IMethodSymbol>();
                AddIfNotNull(linqMethods, enumMethods);
            }

            if (whereMethods.Count > 0 && linqMethods.Count > 0)
            {
                context.RegisterOperationAction(
                    context => AnalyzeAction(
                        context, whereMethods.ToImmutable(), linqMethods.ToImmutable()), OperationKind.Invocation);
            }

            return;

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

            var firstChildArgument = invocationOperation.Children.FirstOrDefault(c => c is IArgumentOperation);

            if (firstChildArgument is null)
            {
                return;
            }

            var whereClause = firstChildArgument.Children.FirstOrDefault(c => c is IInvocationOperation);

            if (whereClause is null)
            {
                return;
            }

            var isLambda = whereClause.Children.Any(c => c.Syntax is ArgumentSyntax argument && argument.Expression is SimpleLambdaExpressionSyntax);

            if (!isLambda)
            {
                return;
            }

            if (whereClause is IInvocationOperation method && !whereMethods.Contains(method.TargetMethod.OriginalDefinition))
            {
                return;
            }

            // check that the Where clause is followed by a call to a valid method i.e. one of First, FirstOrDefault, Single, SingleOrDefault, etc..
            // and that it is also not user defined
            var invocationDefinition = invocationOperation.TargetMethod.OriginalDefinition;

            if (!linqMethods.Contains(invocationDefinition))
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

            // check that the nodes expression is a MemberAccessExpressionSyntax
            if (!((InvocationExpressionSyntax)node is InvocationExpressionSyntax invocation &&
                (MemberAccessExpressionSyntax)invocation.Expression is MemberAccessExpressionSyntax))
            {
                return;
            }

            var location = node.GetLocation();
            context.ReportDiagnostic(
                DiagnosticHelper.Create(Descriptor, location, Descriptor.GetEffectiveSeverity(context.Compilation.Options),
                additionalLocations: null, properties: null));
        }
    }
}
