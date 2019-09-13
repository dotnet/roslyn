// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    internal abstract partial class AbstractUseCollectionInitializerDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
        TVariableDeclaratorSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected AbstractUseCollectionInitializerDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId,
                   CodeStyleOptions.PreferCollectionInitializer,
                   new LocalizableResourceString(nameof(FeaturesResources.Simplify_collection_initialization), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Collection_initialization_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(OnCompilationStart);

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var ienumerableType = context.Compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName);
            if (ienumerableType != null)
            {
                context.RegisterSyntaxNodeAction(
                    nodeContext => AnalyzeNode(nodeContext, ienumerableType),
                    GetObjectCreationSyntaxKind());
            }
        }

        protected abstract bool AreCollectionInitializersSupported(SyntaxNodeAnalysisContext context);
        protected abstract TSyntaxKind GetObjectCreationSyntaxKind();

        private void AnalyzeNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol ienumerableType)
        {
            if (!AreCollectionInitializersSupported(context))
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
            var language = objectCreationExpression.Language;
            var syntaxTree = objectCreationExpression.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            var optionSet = context.Options.GetAnalyzerOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            var option = optionSet.GetOption(CodeStyleOptions.PreferCollectionInitializer, language);
            if (!option.Value)
            {
                // not point in analyzing if the option is off.
                return;
            }

            // Object creation can only be converted to collection initializer if it
            // implements the IEnumerable type.
            var objectType = context.SemanticModel.GetTypeInfo(objectCreationExpression, cancellationToken);
            if (objectType.Type == null || !objectType.Type.AllInterfaces.Contains(ienumerableType))
            {
                return;
            }

            var matches = ObjectCreationExpressionAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TVariableDeclaratorSyntax>.Analyze(
                semanticModel, GetSyntaxFactsService(), objectCreationExpression, cancellationToken);

            if (matches == null || matches.Value.Length == 0)
            {
                return;
            }

            var containingStatement = objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
            var nodes = ImmutableArray.Create<SyntaxNode>(containingStatement).AddRange(matches.Value);
            var syntaxFacts = GetSyntaxFactsService();
            if (syntaxFacts.ContainsInterleavedDirective(nodes, cancellationToken))
            {
                return;
            }

            var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

            var severity = option.Notification.Severity;
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                objectCreationExpression.GetLocation(),
                severity,
                additionalLocations: locations,
                properties: null));

            FadeOutCode(context, optionSet, matches.Value, locations);
        }

        private void FadeOutCode(
            SyntaxNodeAnalysisContext context,
            OptionSet optionSet,
            ImmutableArray<TExpressionStatementSyntax> matches,
            ImmutableArray<Location> locations)
        {
            var syntaxTree = context.Node.SyntaxTree;

            var fadeOutCode = optionSet.GetOption(
                CodeStyleOptions.PreferCollectionInitializer_FadeOutCode, context.Node.Language);
            if (!fadeOutCode)
            {
                return;
            }

            var syntaxFacts = GetSyntaxFactsService();

            foreach (var match in matches)
            {
                var expression = syntaxFacts.GetExpressionOfExpressionStatement(match);

                if (syntaxFacts.IsInvocationExpression(expression))
                {
                    var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(expression);
                    var location1 = Location.Create(syntaxTree, TextSpan.FromBounds(
                        match.SpanStart, arguments[0].SpanStart));

                    context.ReportDiagnostic(Diagnostic.Create(
                        UnnecessaryWithSuggestionDescriptor, location1, additionalLocations: locations));

                    context.ReportDiagnostic(Diagnostic.Create(
                        UnnecessaryWithoutSuggestionDescriptor,
                        Location.Create(syntaxTree, TextSpan.FromBounds(
                            arguments.Last().FullSpan.End,
                            match.Span.End)),
                        additionalLocations: locations));
                }
            }
        }

        protected abstract ISyntaxFactsService GetSyntaxFactsService();
    }
}
