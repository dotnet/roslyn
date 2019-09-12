// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UseObjectInitializer
{
    internal abstract partial class AbstractUseObjectInitializerDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TAssignmentStatementSyntax,
        TVariableDeclaratorSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TAssignmentStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        protected abstract bool FadeOutOperatorToken { get; }

        protected AbstractUseObjectInitializerDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
                   CodeStyleOptions.PreferObjectInitializer,
                   new LocalizableResourceString(nameof(FeaturesResources.Simplify_object_initialization), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Object_initialization_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNode, GetObjectCreationSyntaxKind());

        protected abstract TSyntaxKind GetObjectCreationSyntaxKind();

        protected abstract bool AreObjectInitializersSupported(SyntaxNodeAnalysisContext context);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!AreObjectInitializersSupported(context))
            {
                return;
            }

            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetAnalyzerOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
            var language = objectCreationExpression.Language;
            var option = optionSet.GetOption(CodeStyleOptions.PreferObjectInitializer, language);
            if (!option.Value)
            {
                // not point in analyzing if the option is off.
                return;
            }

            var syntaxFacts = GetSyntaxFactsService();
            var matches = ObjectCreationExpressionAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax, TVariableDeclaratorSyntax>.Analyze(
                context.SemanticModel, syntaxFacts, objectCreationExpression, context.CancellationToken);

            if (matches == null || matches.Value.Length == 0)
            {
                return;
            }

            var containingStatement = objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
            var nodes = ImmutableArray.Create<SyntaxNode>(containingStatement).AddRange(matches.Value.Select(m => m.Statement));
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
            ImmutableArray<Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>> matches,
            ImmutableArray<Location> locations)
        {
            var syntaxTree = context.Node.SyntaxTree;

            var fadeOutCode = optionSet.GetOption(
                CodeStyleOptions.PreferObjectInitializer_FadeOutCode, context.Node.Language);
            if (!fadeOutCode)
            {
                return;
            }

            var syntaxFacts = GetSyntaxFactsService();

            foreach (var match in matches)
            {
                var end = FadeOutOperatorToken
                    ? syntaxFacts.GetOperatorTokenOfMemberAccessExpression(match.MemberAccessExpression).Span.End
                    : syntaxFacts.GetExpressionOfMemberAccessExpression(match.MemberAccessExpression).Span.End;

                var location1 = Location.Create(syntaxTree, TextSpan.FromBounds(
                    match.MemberAccessExpression.SpanStart, end));

                context.ReportDiagnostic(Diagnostic.Create(
                    UnnecessaryWithSuggestionDescriptor, location1, additionalLocations: locations));

                if (match.Statement.Span.End > match.Initializer.FullSpan.End)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        UnnecessaryWithoutSuggestionDescriptor,
                        Location.Create(syntaxTree, TextSpan.FromBounds(
                            match.Initializer.FullSpan.End,
                            match.Statement.Span.End)),
                        additionalLocations: locations));
                }
            }
        }

        protected abstract ISyntaxFactsService GetSyntaxFactsService();

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
