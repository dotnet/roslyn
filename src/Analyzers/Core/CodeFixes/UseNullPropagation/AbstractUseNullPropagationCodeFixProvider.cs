// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
        TElementAccessExpression,
        TElementBindingExpression,
        TElementBindingArgumentList> : SyntaxEditorBasedCodeFixProvider
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
        where TBinaryExpressionSyntax : TExpressionSyntax
        where TInvocationExpression : TExpressionSyntax
        where TMemberAccessExpression : TExpressionSyntax
        where TConditionalAccessExpression : TExpressionSyntax
        where TElementAccessExpression : TExpressionSyntax
        where TElementBindingExpression : TExpressionSyntax
        where TElementBindingArgumentList : SyntaxNode
    {
        protected abstract TElementBindingExpression ElementBindingExpression(TElementBindingArgumentList argumentList);

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseNullPropagationDiagnosticId);

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, AnalyzersResources.Use_null_propagation, nameof(AnalyzersResources.Use_null_propagation));
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
            var root = editor.OriginalRoot;

            foreach (var diagnostic in diagnostics)
            {
                var conditionalExpression = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
                var conditionalPart = root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan, getInnermostNodeForTie: true);
                var whenPart = root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan, getInnermostNodeForTie: true);
                syntaxFacts.GetPartsOfConditionalExpression(
                    conditionalExpression, out var condition, out var whenTrue, out var whenFalse);
                whenTrue = syntaxFacts.WalkDownParentheses(whenTrue);
                whenFalse = syntaxFacts.WalkDownParentheses(whenFalse);

                var whenPartIsNullable = diagnostic.Properties.ContainsKey(UseNullPropagationConstants.WhenPartIsNullable);
                editor.ReplaceNode(conditionalExpression,
                    (c, _) =>
                    {
                        syntaxFacts.GetPartsOfConditionalExpression(
                            c, out var currentCondition, out var currentWhenTrue, out var currentWhenFalse);

                        var currentWhenPartToCheck = whenPart == whenTrue ? currentWhenTrue : currentWhenFalse;

                        var match = AbstractUseNullPropagationDiagnosticAnalyzer<
                            TSyntaxKind, TExpressionSyntax, TConditionalExpressionSyntax,
                            TBinaryExpressionSyntax, TInvocationExpression, TMemberAccessExpression,
                            TConditionalAccessExpression, TElementAccessExpression>.GetWhenPartMatch(syntaxFacts, semanticModel!, conditionalPart, currentWhenPartToCheck);
                        if (match == null)
                        {
                            return c;
                        }

                        var newNode = CreateConditionalAccessExpression(
                            syntaxFacts, generator, whenPartIsNullable, currentWhenPartToCheck, match, c);

                        newNode = newNode.WithTriviaFrom(c);
                        return newNode;
                    });
            }
        }

        private SyntaxNode CreateConditionalAccessExpression(
            ISyntaxFactsService syntaxFacts, SyntaxGeneratorInternal generator, bool whenPartIsNullable,
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
                            memberAccess.Parent!, currentConditional);
                    }
                }
            }

            return CreateConditionalAccessExpression(
                syntaxFacts, generator, whenPart, match,
                match.Parent!, currentConditional);
        }

        private SyntaxNode CreateConditionalAccessExpression(
            ISyntaxFactsService syntaxFacts, SyntaxGeneratorInternal generator,
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
                Debug.Assert(syntaxFacts.IsElementAccessExpression(elementAccess));
                var argumentList = (TElementBindingArgumentList)syntaxFacts.GetArgumentListOfElementAccessExpression(elementAccess)!;
                return whenPart.ReplaceNode(elementAccess,
                    generator.ConditionalAccessExpression(
                        match, ElementBindingExpression(argumentList)));
            }

            return currentConditional;
        }
    }
}
