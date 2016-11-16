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
            // Fix-All for this feature is somewhat complicated.  As Object-Initializers 
            // could be arbitrarily nested, we have to make sure that any edits we make
            // to one Object-Initializer are seen by any higher ones.  In order to do this
            // we actually process each object-creation-node, one at a time, rewriting
            // the tree for each node.  In order to do this effectively, we use the '.TrackNodes'
            // feature to keep track of all the object creation nodes as we make edits to
            // the tree.  If we didn't do this, then we wouldn't be able to find the 
            // second object-creation-node after we make the edit for the first one.
            var workspace = document.Project.Solution.Workspace;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var root = editor.OriginalRoot;
            var originalObjectCreationNodes = new Stack<TObjectCreationExpressionSyntax>();
            foreach (var diagnostic in diagnostics)
            {
                var objectCreation = (TObjectCreationExpressionSyntax)root.FindNode(
                    diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
                originalObjectCreationNodes.Push(objectCreation);
            }

            // We're going to be continually editing this tree.  Track all the nodes we
            // care about so we can find them across each edit.
            var currentRoot = root.TrackNodes(originalObjectCreationNodes);

            while (originalObjectCreationNodes.Count > 0)
            {
                var originalObjectCreation = originalObjectCreationNodes.Pop();
                var objectCreation = currentRoot.GetCurrentNodes(originalObjectCreation).Single();

                var result = this.Analyze(objectCreation);
                var matches = result.Value.Matches;

                var statement = objectCreation.FirstAncestorOrSelf<TStatementSyntax>();
                var newStatement = statement.ReplaceNode(
                    objectCreation,
                    GetNewObjectCreation(objectCreation, matches)).WithAdditionalAnnotations(Formatter.Annotation);

                var block = statement.Parent;
                var newBlock = UpdateBlock(workspace, block, statement, newStatement, matches);

                currentRoot = currentRoot.ReplaceNode(block, newBlock);
            }

            editor.ReplaceNode(editor.OriginalRoot, currentRoot);
            return SpecializedTasks.EmptyTask;
        }

        private SyntaxNode UpdateBlock(
            Workspace workspace,
            SyntaxNode block,
            TStatementSyntax statement,
            TStatementSyntax newStatement,
            ImmutableArray<Match> matches)
        {
            var editor = new SyntaxEditor(block, workspace);

            editor.ReplaceNode(statement, newStatement);
            foreach (var match in matches)
            {
                editor.RemoveNode(match.Statement);
            }

            return editor.GetChangedRoot();
        }

        protected abstract TObjectCreationExpressionSyntax GetNewObjectCreation(
            TObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match> matches);
    }
}