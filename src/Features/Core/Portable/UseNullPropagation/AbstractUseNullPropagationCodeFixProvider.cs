// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.FixAllOccurrences;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseNullPropagation
{
    internal abstract class AbstractUseNullPropagationCodeFixProvider<
        TSyntaxKind,
        TExpressionSyntax,
        TConditionalExpressionSyntax,
        TBinaryExpressionSyntax,
        TInvocationExpression,
        TMemberAccessExpression,
        TConditionalAccessExpression,
        TElementAccessExpression> : CodeFixProvider
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
        where TBinaryExpressionSyntax : TExpressionSyntax
        where TInvocationExpression : TExpressionSyntax
        where TMemberAccessExpression : TExpressionSyntax
        where TConditionalAccessExpression : TExpressionSyntax
        where TElementAccessExpression : TExpressionSyntax
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseNullPropagationDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => new UseNullPropagationFixAllProvider(this);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        private Task<Document> FixAsync(
            Document document,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            return FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken);
        }

        private async Task<Document> FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var generator = editor.Generator;

            foreach (var diagnostic in diagnostics)
            {
                var conditionalExpression = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
                var conditionalPart = root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan);
                var whenPart = root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan);

                SyntaxNode condition, whenTrue, whenFalse;
                syntaxFacts.GetPartsOfConditionalExpression(
                    conditionalExpression, out condition, out whenTrue, out whenFalse);

                editor.ReplaceNode(conditionalExpression,
                    (c, g) => {
                        SyntaxNode currentCondition, currentWhenTrue, currentWhenFalse;
                        syntaxFacts.GetPartsOfConditionalExpression(
                            c, out currentCondition, out currentWhenTrue, out currentWhenFalse);

                        var currentWhenPartToCheck = whenPart == whenTrue ? currentWhenTrue : currentWhenFalse;

                        var match = AbstractUseNullPropagationDiagnosticAnalyzer<
                            TSyntaxKind, TExpressionSyntax, TConditionalExpressionSyntax,
                            TBinaryExpressionSyntax, TInvocationExpression, TMemberAccessExpression,
                            TConditionalAccessExpression, TElementAccessExpression>.GetWhenPartMatch(syntaxFacts, conditionalPart, currentWhenPartToCheck);
                        if (match == null)
                        {
                            return c;
                        }

                        var newNode = CreateConditionalAccessExpression(
                            syntaxFacts, g, currentWhenPartToCheck, match, c);

                        newNode = newNode.WithTriviaFrom(c);
                        return newNode;
                    });
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private SyntaxNode CreateConditionalAccessExpression(
            ISyntaxFactsService syntaxFacts, SyntaxGenerator generator, 
            SyntaxNode whenPart, SyntaxNode match, SyntaxNode currentConditional)
        {
            var memberAccess = match.Parent as TMemberAccessExpression;
            if (memberAccess != null)
            {
                return whenPart.ReplaceNode(memberAccess,
                    generator.ConditionalAccessExpression(
                        match,
                        generator.MemberBindingExpression(
                            syntaxFacts.GetNameOfMemberAccessExpression(memberAccess))));
            }

            var elementAccess = match.Parent as TElementAccessExpression;
            if (elementAccess != null)
            {
                return whenPart.ReplaceNode(elementAccess,
                    generator.ConditionalAccessExpression(
                        match,
                        generator.ElementBindingExpression(
                            syntaxFacts.GetArgumentListOfElementAccessExpression(elementAccess))));
            }

            return currentConditional;
        }

        private class UseNullPropagationFixAllProvider : DocumentBasedFixAllProvider
        {
            private readonly AbstractUseNullPropagationCodeFixProvider<
                TSyntaxKind, TExpressionSyntax, TConditionalExpressionSyntax,
                TBinaryExpressionSyntax, TInvocationExpression, TMemberAccessExpression,
                TConditionalAccessExpression, TElementAccessExpression> _provider;

            public UseNullPropagationFixAllProvider(AbstractUseNullPropagationCodeFixProvider<
                TSyntaxKind, TExpressionSyntax, TConditionalExpressionSyntax,
                TBinaryExpressionSyntax, TInvocationExpression, TMemberAccessExpression,
                TConditionalAccessExpression, TElementAccessExpression> provider)
            {
                _provider = provider;
            }

            protected override Task<Document> FixDocumentAsync(
                Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
            {
                var filteredDiagnostics = diagnostics.WhereAsArray(
                    d => !d.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary));
                return _provider.FixAllAsync(document, filteredDiagnostics, cancellationToken);
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Use_null_propagation, createChangedDocument)
            {
            }
        }
    }
}