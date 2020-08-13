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
using Microsoft.CodeAnalysis.Shared.Extensions;
using System;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Roslyn.Utilities;
#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyLinqExpressionDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private readonly ImmutableArray<string> s_validLinqCalls = ImmutableArray.Create(
            nameof(Enumerable.First),
            nameof(Enumerable.Last),
            nameof(Enumerable.Single),
            nameof(Enumerable.Any),
            nameof(Enumerable.Count),
            nameof(Enumerable.SingleOrDefault),
            nameof(Enumerable.FirstOrDefault),
            nameof(Enumerable.LastOrDefault)
            );

        public CSharpSimplifyLinqExpressionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.SimplifyLinqExpressionsDiagnosticId,
                   option: null,
                   title: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Simplify_Linq_expression), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
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

#pragma warning disable CS8604 // Possible null reference argument.
            var enumNamedType = context.Compilation.GetTypeByMetadataName(typeof(Enumerable).FullName);
#pragma warning restore CS8604 // Possible null reference argument.

            if (enumNamedType is null)
            {
                return;
            }

            var enumMethods = enumNamedType.GetMembers(nameof(Enumerable.Where)).OfType<IMethodSymbol>();
            whereMethods.UnionWith(enumMethods);

            // add all valid linq calls
            foreach (var id in s_validLinqCalls)
            {
                enumMethods = enumNamedType.GetMembers(id).OfType<IMethodSymbol>();
                linqMethods.UnionWith(enumMethods);
            }

            if (whereMethods.Count > 0 && linqMethods.Count > 0)
            {
                context.RegisterOperationAction(
                    context => AnalyzeAction(context, whereMethods.ToImmutable(),
                    linqMethods.ToImmutable()),
                    OperationKind.Invocation);
            }

            return;
        }

        public void AnalyzeAction(OperationAnalysisContext context, ImmutableHashSet<IMethodSymbol> whereMethods, ImmutableHashSet<IMethodSymbol> linqMethods)
        {
            var invocationOperation = (IInvocationOperation)context.Operation;
            var semanticModel = context.Operation.SemanticModel;
            var invocationArgument = invocationOperation.Arguments.FirstOrDefault();

            Location argumentLocation;
            Location targetMethodLocation;
            IEnumerable<Location> additionalLocations;

            // Check to make sure the invocation argument is an InvocationExpressionSyntax
            // Example: If invocationOperation syntax is Data().Where(...).Single(), then invocationArgument is Data.Where(...)
            if (invocationArgument is null)
            {
                return;
            }

            var targetOperation = invocationArgument.Children.FirstOrDefault(c => c is IOperation);

            // Example: If invocation is Data.Where(...), then the TargetMethod would be .Where(...)
            if (targetOperation is IInvocationOperation invocation)
            {
                var targetDefinition = invocation.TargetMethod.OriginalDefinition;

                // True if the invocation.TargetMethod is a call to System.Linq.Enumerable.Where(...)
                if (!whereMethods.Contains(targetDefinition))
                {
                    return;
                }

                // True if the arguments within the .Where(...) method are lambda expressions
                var isLambda = invocation.Arguments.Any(c => c.Syntax is ArgumentSyntax argSyntax && argSyntax.Expression.IsAnyLambda() || c.Syntax is LambdaExpressionSyntax);

                if (!isLambda)
                {
                    return;
                }

                // check that the Where clause is followed by a call to a valid method i.e. one of First, FirstOrDefault, Single, SingleOrDefault, etc..
                // Example: if Data.Where(...).Single(), then the invocationOperation.TargetMethod is Single
                var invocationDefinition = invocationOperation.TargetMethod.OriginalDefinition;

                // True if invocationDefinition is one of:
                // First(), Last(), Sinlge(), Any(), Count(), FirstOrDefualt(), LastOrDefault(), or SingleOrDefault()
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

                var node = context.Operation.Syntax;

                // Example: if Data().Where(...).First()
                // Then invokedNode is Data(), whereClauseSyntax is .Where(...), and the targetMethodNode is First.
                //var invokedNode = node.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault(d => d.Expression is IdentifierNameSyntax);
                var targetMethodNode = node.DescendantNodes().OfType<IdentifierNameSyntax>().LastOrDefault();
                var whereClauseSyntax = invocation.Syntax as InvocationExpressionSyntax;
                if (whereClauseSyntax is null ||
                    targetMethodNode is null ||
                    whereClauseSyntax.ArgumentList.Arguments.IsEmpty())
                {
                    return;
                }

                argumentLocation = whereClauseSyntax.ArgumentList.GetLocation();

                targetMethodLocation = targetMethodNode.GetLocation();

                additionalLocations = new List<Location> { argumentLocation, targetMethodLocation };
                context.ReportDiagnostic(
                        DiagnosticHelper.Create(Descriptor, node.GetLocation(), Descriptor.GetEffectiveSeverity(context.Compilation.Options),
                        additionalLocations, properties: null));
            }
        }
    }
}
