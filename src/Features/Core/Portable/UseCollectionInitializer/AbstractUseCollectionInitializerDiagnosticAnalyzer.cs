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
        : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        public bool OpenFileOnly(Workspace workspace) => false;

        protected AbstractUseCollectionInitializerDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Simplify_collection_initialization), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Collection_initialization_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(OnCompilationStart);

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var ienumerableType = context.Compilation.GetTypeByMetadataName("System.Collections.IEnumerable") as INamedTypeSymbol;
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

            var cancellationToken = context.CancellationToken;
            var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
            var language = objectCreationExpression.Language;

            var optionSet = context.Options.GetOptionSet();
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

            var matches = Analyze(objectCreationExpression);
            if (matches.Length == 0)
            {
                return;
            }

            var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

            var severity = option.Notification.Value;
            context.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptorWithSeverity(severity),
                objectCreationExpression.GetLocation(),
                additionalLocations: locations));

            FadeOutCode(context, optionSet, matches, locations);
        }

        public ImmutableArray<TExpressionStatementSyntax> Analyze(TObjectCreationExpressionSyntax objectCreationExpression)
        {
            var syntaxFacts = GetSyntaxFactsService();

            var analyzer = new Analyzer(syntaxFacts, objectCreationExpression);
            var matches = analyzer.Analyze();
            return matches;
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

        public Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            // Fix-All for this feature is somewhat complicated.  As Collection-Initializers 
            // could be arbitrarily nested, we have to make sure that any edits we make
            // to one Collection-Initializer are seen by any higher ones.  In order to do this
            // we actually process each object-creation-node, one at a time, rewriting
            // the tree for each node.  In order to do this effectively, we use the '.TrackNodes'
            // feature to keep track of all the object creation nodes as we make edits to
            // the tree.  If we didn't do this, then we wouldn't be able to find the 
            // second object-creation-node after we make the edit for the first one.
            var workspace = document.Project.Solution.Workspace;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var originalRoot = editor.OriginalRoot;

            var originalObjectCreationNodes = new Stack<TObjectCreationExpressionSyntax>();
            foreach (var diagnostic in diagnostics)
            {
                var objectCreation = (TObjectCreationExpressionSyntax)originalRoot.FindNode(
                    diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
                originalObjectCreationNodes.Push(objectCreation);
            }

            // We're going to be continually editing this tree.  Track all the nodes we
            // care about so we can find them across each edit.
            var currentRoot = originalRoot.TrackNodes(originalObjectCreationNodes);

            while (originalObjectCreationNodes.Count > 0)
            {
                var originalObjectCreation = originalObjectCreationNodes.Pop();
                var objectCreation = currentRoot.GetCurrentNodes(originalObjectCreation).Single();

                var matches = Analyze(objectCreation);

                var statement = objectCreation.FirstAncestorOrSelf<TStatementSyntax>();
                var newStatement = statement.ReplaceNode(
                    objectCreation,
                    GetNewObjectCreation(objectCreation, matches)).WithAdditionalAnnotations(Formatter.Annotation);

                var subEditor = new SyntaxEditor(currentRoot, workspace);

                subEditor.ReplaceNode(statement, newStatement);
                foreach (var match in matches)
                {
                    subEditor.RemoveNode(match);
                }

                currentRoot = subEditor.GetChangedRoot();
            }

            editor.ReplaceNode(originalRoot, currentRoot);
            return SpecializedTasks.EmptyTask;
        }

        protected abstract TObjectCreationExpressionSyntax GetNewObjectCreation(
            TObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<TExpressionStatementSyntax> matches);
    }
}