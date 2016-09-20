// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseObjectInitializer
{
    internal abstract class AbstractUseObjectInitializerCodeFixProvider<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TVariableDeclarator>
        : CodeFixProvider
        where TExpressionSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TStatementSyntax : SyntaxNode
        where TVariableDeclarator : SyntaxNode
    {
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseObjectInitializerDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        private async Task<Document> FixAsync(
            Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var objectCreation = (TObjectCreationExpressionSyntax)root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var analyzer = new Analyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TVariableDeclarator>(
                syntaxFacts, objectCreation);
            var matches = analyzer.Analyze();

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            editor.ReplaceNode(objectCreation, GetNewObjectCreation(
                syntaxFacts, editor.Generator, objectCreation, matches));
            foreach (var match in matches)
            {
                editor.RemoveNode(match.Statement);
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private TObjectCreationExpressionSyntax GetNewObjectCreation(
            ISyntaxFactsService syntaxFacts,
            SyntaxGenerator genarator,
            TObjectCreationExpressionSyntax objectCreation,
            List<Match<TStatementSyntax, TMemberAccessExpressionSyntax, TExpressionSyntax>> matches)
        {
            var initializer = genarator.ObjectMemberInitializer(
                matches.Select(m => CreateNamedFieldInitializer(syntaxFacts, genarator, m)));


            return (TObjectCreationExpressionSyntax)genarator.WithObjectCreationInitializer(objectCreation, initializer)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private SyntaxNode CreateNamedFieldInitializer(
            ISyntaxFactsService syntaxFacts,
            SyntaxGenerator generator,
            Match<TStatementSyntax, TMemberAccessExpressionSyntax, TExpressionSyntax> match)
        {
            return generator.NamedFieldInitializer(
                syntaxFacts.GetNameOfMemberAccessExpression(match.MemberAccessExpression),
                match.Initializer).WithLeadingTrivia(GetNewLine());
        }

        protected abstract SyntaxTrivia GetNewLine();

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Object_initialization_can_be_simplified, createChangedDocument)
            {
            }
        }
    }
}

