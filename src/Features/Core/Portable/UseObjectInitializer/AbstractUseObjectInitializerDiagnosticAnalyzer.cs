// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
        : AbstractCodeStyleDiagnosticAnalyzer, IBuiltInAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TAssignmentStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        protected abstract bool FadeOutOperatorToken { get; }

        public bool OpenFileOnly(Workspace workspace) => false;

        protected AbstractUseObjectInitializerDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
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

            var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
            var language = objectCreationExpression.Language;

            var optionSet = context.Options.GetOptionSet();
            var option = optionSet.GetOption(CodeStyleOptions.PreferObjectInitializer, language);
            if (!option.Value)
            {
                // not point in analyzing if the option is off.
                return;
            }

            var result = Analyze(objectCreationExpression);
            if (result == null || result.Value.Matches.Length == 0)
            {
                return;
            }

            var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

            var severity = option.Notification.Value;
            context.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptorWithSeverity(severity),
                objectCreationExpression.GetLocation(),
                additionalLocations: locations));

            FadeOutCode(context, optionSet, result.Value, locations);
        }

        public AnalysisResult? Analyze(TObjectCreationExpressionSyntax objectCreationExpression)
        {
            var syntaxFacts = GetSyntaxFactsService();
            var analyzer = new Analyzer(syntaxFacts, objectCreationExpression);
            var result = analyzer.Analyze();
            return result;
        }

        private void FadeOutCode(
            SyntaxNodeAnalysisContext context,
            OptionSet optionSet,
            AnalysisResult result,
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

            foreach (var match in result.Matches)
            {
                var end = this.FadeOutOperatorToken
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

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
        }

        public Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;
            var blockToMatchingStatements = new MultiDictionary<SyntaxNode, int>();

            foreach (var diagnostic in diagnostics)
            {
                var objectCreation = (TObjectCreationExpressionSyntax)root.FindNode(
                    diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
                var analysisResult = Analyze(objectCreation).Value;

                blockToMatchingStatements.Add(
                    analysisResult.BlockNode, analysisResult.ContainingStatementIndex);
            }

            foreach (var kvp in blockToMatchingStatements)
            {
                var block = kvp.Key;
                var matchingStatements = kvp.Value;

                editor.ReplaceNode(
                    block,
                    (currentBlock, g) => UpdateBlock(currentBlock, matchingStatements.ToArray()));
            }

            return SpecializedTasks.EmptyTask;
        }

        private SyntaxNode UpdateBlock(SyntaxNode oldBlock, int[] containingStatementIndices)
        {
            var syntaxFacts = this.GetSyntaxFactsService();

            var oldStatementToNewStatement = new Dictionary<TStatementSyntax, TStatementSyntax>();
            var oldStatementIndicesToRemove = new List<int>();

            var oldChildNodesAndTokens = oldBlock.ChildNodesAndTokens().ToList();
            foreach (var containingStatementIndex in containingStatementIndices)
            {
                var containingStatement = (TStatementSyntax)oldChildNodesAndTokens[containingStatementIndex];

                var objectCreation = GetObjectCreation(containingStatement);
                var result = Analyze(objectCreation).Value;

                var newObjectCreation = GetNewObjectCreation(objectCreation, result.Matches);

                var newStatement = containingStatement.ReplaceNode(objectCreation, newObjectCreation)
                                                      .WithAdditionalAnnotations(Formatter.Annotation);

                oldStatementToNewStatement.Add(containingStatement, newStatement);
                oldStatementIndicesToRemove.AddRange(result.Matches.Select(
                    m => oldChildNodesAndTokens.IndexOf(m.Statement)));
            }

            // First, replace all the statements with the old object initializer with the updated
            // statement with the new object initializer.
            var newBlock1 = oldBlock.ReplaceNodes(
                oldStatementToNewStatement.Keys,
                (oldStatement, _) => oldStatementToNewStatement[oldStatement]);

            // Now find all the statements we want to remove in this new block.
            var newChildNodesAndTokens = newBlock1.ChildNodesAndTokens().ToArray();
            var currentStatementsToRemove = oldStatementIndicesToRemove.Order().Select(
                i => newChildNodesAndTokens[i].AsNode()).ToList();

            var newBlock2 = newBlock1.RemoveNodes(
                currentStatementsToRemove, SyntaxGenerator.DefaultRemoveOptions);

            return newBlock2;
        }

        private TObjectCreationExpressionSyntax GetObjectCreation(TStatementSyntax statement)
        {
            var syntaxFacts = this.GetSyntaxFactsService();
            if (syntaxFacts.IsSimpleAssignmentStatement(statement))
            {
                syntaxFacts.GetPartsOfAssignmentStatement(statement, out var left, out var right);
                return (TObjectCreationExpressionSyntax)right;
            }
            else
            {
                var expression = (TExpressionSyntax)syntaxFacts.GetExpressionOfExpressionStatement(statement);
                syntaxFacts.GetPartsOfBinaryExpression(expression, out var left, out var right);
                return (TObjectCreationExpressionSyntax)right;
            }
        }

#if false
        private async Task<Document> FixAsync(
    Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var objectCreation = (TObjectCreationExpressionSyntax)root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var result = _analyzer.Analyze(objectCreation);
            var matches = result.Value.Matches;

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            var statement = objectCreation.FirstAncestorOrSelf<TStatementSyntax>();
            var newStatement = statement.ReplaceNode(
                objectCreation,
                GetNewObjectCreation(objectCreation, matches)).WithAdditionalAnnotations(Formatter.Annotation);

            editor.ReplaceNode(statement, newStatement);
            foreach (var match in matches)
            {
                editor.RemoveNode(match.Statement);
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }
#endif

        protected abstract TObjectCreationExpressionSyntax GetNewObjectCreation(
            TObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match> matches);
    }
}