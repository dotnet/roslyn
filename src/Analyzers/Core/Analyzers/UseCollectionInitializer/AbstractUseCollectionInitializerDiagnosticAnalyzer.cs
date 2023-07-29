// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Analyzers.UseCollectionInitializer;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    internal readonly record struct Match<TStatementSyntax>(
        TStatementSyntax Statement,
        bool UseSpread) where TStatementSyntax : SyntaxNode;

    internal abstract partial class AbstractUseCollectionInitializerDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
        TForeachStatementSyntax,
        TVariableDeclaratorSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TForeachStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        private static readonly DiagnosticDescriptor s_descriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.UseCollectionInitializerDiagnosticId,
            EnforceOnBuildValues.UseCollectionInitializer,
            new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
            new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
            isUnnecessary: false);

        private static readonly DiagnosticDescriptor s_unnecessaryCodeDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.UseCollectionInitializerDiagnosticId,
            EnforceOnBuildValues.UseCollectionInitializer,
            new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
            new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
            isUnnecessary: true);

        protected AbstractUseCollectionInitializerDiagnosticAnalyzer()
            : base(ImmutableDictionary<DiagnosticDescriptor, IOption2>.Empty
                    .Add(s_descriptor, CodeStyleOptions2.PreferCollectionInitializer)
                    .Add(s_unnecessaryCodeDescriptor, CodeStyleOptions2.PreferCollectionInitializer))
        {
        }

        protected abstract ISyntaxFacts GetSyntaxFacts();

        protected abstract bool AreCollectionInitializersSupported(Compilation compilation);
        protected abstract bool AreCollectionExpressionsSupported(Compilation compilation);
        protected abstract bool CanUseCollectionExpression(SemanticModel semanticModel, TObjectCreationExpressionSyntax objectCreationExpression, CancellationToken cancellationToken);

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(OnCompilationStart);

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            if (!AreCollectionInitializersSupported(context.Compilation))
                return;

            var ienumerableType = context.Compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName!);
            if (ienumerableType != null)
            {
                var syntaxKinds = GetSyntaxFacts().SyntaxKinds;

                using var matchKinds = TemporaryArray<TSyntaxKind>.Empty;
                matchKinds.Add(syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.ObjectCreationExpression));
                if (syntaxKinds.ImplicitObjectCreationExpression != null)
                    matchKinds.Add(syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.ImplicitObjectCreationExpression.Value));
                var matchKindsArray = matchKinds.ToImmutableAndClear();

                // We wrap the SyntaxNodeAction within a CodeBlockStartAction, which allows us to
                // get callbacks for object creation expression nodes, but analyze nodes across the entire code block
                // and eventually report fading diagnostics with location outside this node.
                // Without the containing CodeBlockStartAction, our reported diagnostic would be classified
                // as a non-local diagnostic and would not participate in lightbulb for computing code fixes.
                context.RegisterCodeBlockStartAction<TSyntaxKind>(blockStartContext =>
                    blockStartContext.RegisterSyntaxNodeAction(
                        nodeContext => AnalyzeNode(nodeContext, ienumerableType),
                        matchKindsArray));
            }
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol ienumerableType)
        {
            var semanticModel = context.SemanticModel;
            var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
            var language = objectCreationExpression.Language;
            var cancellationToken = context.CancellationToken;

            var option = context.GetAnalyzerOptions().PreferCollectionInitializer;
            if (!option.Value)
            {
                // not point in analyzing if the option is off.
                return;
            }

            // Object creation can only be converted to collection initializer if it implements the IEnumerable type.
            var objectType = context.SemanticModel.GetTypeInfo(objectCreationExpression, cancellationToken);
            if (objectType.Type == null || !objectType.Type.AllInterfaces.Contains(ienumerableType))
                return;

            var containingStatement = objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
            if (containingStatement == null)
                return;

            var (matches, shouldUseCollectionExpression) = GetMatches();
            // If we got no matches, then we def can't convert this.
            if (matches.IsDefaultOrEmpty)
                return;

            var nodes = ImmutableArray.Create<SyntaxNode>(containingStatement).AddRange(matches.Select(static m => m.Statement));
            var syntaxFacts = GetSyntaxFacts();
            if (syntaxFacts.ContainsInterleavedDirective(nodes, cancellationToken))
                return;

            var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

            context.ReportDiagnostic(DiagnosticHelper.Create(
                s_descriptor,
                objectCreationExpression.GetFirstToken().GetLocation(),
                option.Notification.Severity,
                additionalLocations: locations,
                properties: shouldUseCollectionExpression ? UseCollectionInitializerHelpers.UseCollectionExpressionProperties : null));

            FadeOutCode(context, matches, locations);

            return;

            (ImmutableArray<Match<TStatementSyntax>> matches, bool shouldUseCollectionExpression) GetMatches()
            {
                // Analyze the surrounding statements. First, try a broader set of statements if the language supports
                // collection expressions. 
                var analyzeForCollectionExpression = AreCollectionExpressionsSupported();
                var matches = UseCollectionInitializerAnalyzer<
                    TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TForeachStatementSyntax, TVariableDeclaratorSyntax>.Analyze(
                    semanticModel, GetSyntaxFacts(), objectCreationExpression, analyzeForCollectionExpression, cancellationToken);

                // if this was a normal (non-collection-expr) analysis, then just return what we got.
                if (!analyzeForCollectionExpression)
                    return (matches, shouldUseCollectionExpression: false);

                // If we succeeded in finding matches, and this is a location a collection expression is legal in, then convert to that.
                if (!matches.IsDefaultOrEmpty && CanUseCollectionExpression(semanticModel, objectCreationExpression, cancellationToken))
                    return (matches, analyzeForCollectionExpression);

                // we tried collection expression, and were not successful.  try again, this time without collection exprs.
                analyzeForCollectionExpression = false;
                matches = UseCollectionInitializerAnalyzer<
                    TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TForeachStatementSyntax, TVariableDeclaratorSyntax>.Analyze(
                    semanticModel, GetSyntaxFacts(), objectCreationExpression, analyzeForCollectionExpression, cancellationToken);
                return (matches, analyzeForCollectionExpression);
            }

            bool AreCollectionExpressionsSupported()
            {
                if (!this.AreCollectionExpressionsSupported(context.Compilation))
                    return false;

                var option = context.GetAnalyzerOptions().PreferCollectionExpression;
                if (!option.Value)
                    return false;

                var syntaxFacts = GetSyntaxFacts();
                var arguments = syntaxFacts.GetArgumentsOfObjectCreationExpression(objectCreationExpression);
                if (arguments.Count != 0)
                    return false;

                return true;
            }
        }

        private void FadeOutCode(
            SyntaxNodeAnalysisContext context,
            ImmutableArray<Match<TStatementSyntax>> matches,
            ImmutableArray<Location> locations)
        {
            var syntaxTree = context.Node.SyntaxTree;
            var syntaxFacts = GetSyntaxFacts();

            foreach (var (match, _) in matches)
            {
                if (match is TExpressionStatementSyntax)
                {
                    var expression = syntaxFacts.GetExpressionOfExpressionStatement(match);

                    if (syntaxFacts.IsInvocationExpression(expression))
                    {
                        var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(expression);
                        var additionalUnnecessaryLocations = ImmutableArray.Create(
                            syntaxTree.GetLocation(TextSpan.FromBounds(match.SpanStart, arguments[0].SpanStart)),
                            syntaxTree.GetLocation(TextSpan.FromBounds(arguments.Last().FullSpan.End, match.Span.End)));

                        // Report the diagnostic at the first unnecessary location. This is the location where the code fix
                        // will be offered.
                        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                            s_unnecessaryCodeDescriptor,
                            additionalUnnecessaryLocations[0],
                            ReportDiagnostic.Default,
                            additionalLocations: locations,
                            additionalUnnecessaryLocations: additionalUnnecessaryLocations));
                    }
                }
                else if (match is TForeachStatementSyntax)
                {
                    // For a `foreach (var x in expr) ...` statement, fade out the parts before and after `expr`.

                    var expression = syntaxFacts.GetExpressionOfForeachStatement(match);
                    var additionalUnnecessaryLocations = ImmutableArray.Create(
                        syntaxTree.GetLocation(TextSpan.FromBounds(match.SpanStart, expression.SpanStart)),
                        syntaxTree.GetLocation(TextSpan.FromBounds(expression.FullSpan.End, match.Span.End)));

                    // Report the diagnostic at the first unnecessary location. This is the location where the code fix
                    // will be offered.
                    context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                        s_unnecessaryCodeDescriptor,
                        additionalUnnecessaryLocations[0],
                        ReportDiagnostic.Default,
                        additionalLocations: locations,
                        additionalUnnecessaryLocations: additionalUnnecessaryLocations));
                }
            }
        }
    }
}
