// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
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
        TElementAccessExpression> : SyntaxEditorBasedCodeFixProvider
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

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;
            var root = editor.OriginalRoot;

            foreach (var diagnostic in diagnostics)
            {
                var conditionalExpression = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
                var conditionalPart = root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan, getInnermostNodeForTie: true);
                var whenPart = root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan, getInnermostNodeForTie: true);
                syntaxFacts.GetPartsOfConditionalExpression(
                    conditionalExpression, out var condition, out var whenTrue, out var whenFalse);

                var whenPartIsNullable = diagnostic.Properties.ContainsKey(UseNullPropagationConstants.WhenPartIsNullable);
                editor.ReplaceNode(conditionalExpression,
                    (c, g) =>
                    {
                        syntaxFacts.GetPartsOfConditionalExpression(
                            c, out var currentCondition, out var currentWhenTrue, out var currentWhenFalse);

                        var currentWhenPartToCheck = whenPart == whenTrue ? currentWhenTrue : currentWhenFalse;

                        var match = AbstractUseNullPropagationDiagnosticAnalyzer<
                            TSyntaxKind, TExpressionSyntax, TConditionalExpressionSyntax,
                            TBinaryExpressionSyntax, TInvocationExpression, TMemberAccessExpression,
                            TConditionalAccessExpression, TElementAccessExpression>.GetWhenPartMatch(syntaxFacts, semanticFacts, semanticModel, conditionalPart, currentWhenPartToCheck);
                        if (match == null)
                        {
                            return c;
                        }

                        var newNode = CreateConditionalAccessExpression(
                            syntaxFacts, g, whenPartIsNullable, currentWhenPartToCheck, match, c);

                        newNode = newNode.WithTriviaFrom(c);
                        return newNode;
                    });
            }
        }

        private SyntaxNode CreateConditionalAccessExpression(
            ISyntaxFactsService syntaxFacts, SyntaxGenerator generator, bool whenPartIsNullable,
            SyntaxNode whenPart, SyntaxNode match, SyntaxNode currentConditional)
        {
            if (whenPartIsNullable)
            {
                if (match.Parent is TMemberAccessExpression memberAccess)
                {
                    var nameNode = syntaxFacts.GetNameOfMemberAccessExpression(memberAccess);
                    syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out var name, out var arity);
                    var comparer = syntaxFacts.StringComparer;

                    if (arity == 0 && comparer.Equals(name, nameof(Nullable<int>.Value)))
                    {
                        // They're calling ".Value" off of a nullable.  Because we're moving to ?.
                        // we want to remove the .Value as well.  i.e. we should generate:
                        //
                        //      goo?.Bar()  not   goo?.Value.Bar();
                        return CreateConditionalAccessExpression(
                            syntaxFacts, generator, whenPart, match,
                            memberAccess.Parent, currentConditional);
                    }
                }
            }

            return CreateConditionalAccessExpression(
                syntaxFacts, generator, whenPart, match,
                match.Parent, currentConditional);
        }

        private SyntaxNode CreateConditionalAccessExpression(
            ISyntaxFactsService syntaxFacts, SyntaxGenerator generator,
            SyntaxNode whenPart, SyntaxNode match, SyntaxNode matchParent, SyntaxNode currentConditional)
        {
            if (matchParent is TMemberAccessExpression memberAccess)
            {
                return whenPart.ReplaceNode(memberAccess,
                    generator.ConditionalAccessExpression(
                        match,
                        generator.MemberBindingExpression(
                            syntaxFacts.GetNameOfMemberAccessExpression(memberAccess))));
            }

            if (matchParent is TElementAccessExpression elementAccess)
            {
                return whenPart.ReplaceNode(elementAccess,
                    generator.ConditionalAccessExpression(
                        match,
                        generator.ElementBindingExpression(
                            syntaxFacts.GetArgumentListOfElementAccessExpression(elementAccess))));
            }

            return currentConditional;
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
