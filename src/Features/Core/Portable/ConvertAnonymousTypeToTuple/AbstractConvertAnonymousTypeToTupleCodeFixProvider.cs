// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple
{
    internal abstract class AbstractConvertAnonymousTypeToTupleCodeFixProvider<
        TExpressionSyntax,
        TTupleExpressionSyntax,
        TAnonymousObjectCreationExpressionSyntax>
        : SyntaxEditorBasedCodeFixProvider
        where TExpressionSyntax : SyntaxNode
        where TTupleExpressionSyntax : TExpressionSyntax
        where TAnonymousObjectCreationExpressionSyntax : TExpressionSyntax
    {
        protected abstract TTupleExpressionSyntax ConvertToTuple(TAnonymousObjectCreationExpressionSyntax anonCreation);

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.ConvertAnonymousTypeToTupleDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAllWithEditorAsync(context.Document,
                    e => FixInCurrentMember(context.Document, e, context.Diagnostics[0], c), c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        private async Task FixInCurrentMember(
            Document document, SyntaxEditor editor,
            Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            // For the standard invocation of the code-fix, we want to fixup all creations of the
            // "same" anonymous type within the containing method.  We define same-ness as meaning
            // "they have the type symbol".  This means both have the same member names, in the same
            // order, with the same member types.  We fix all these up in the method because the
            // user may be creating several instances of this anonymous type in that method and
            // then combining them in interesting ways (i.e. checking them for equality, using them
            // in collections, etc.).  The language guarantees within a method boundary that these
            // will be the same type and can be used together in this fashion.

            var creationNode = TryGetCreationNode(diagnostic, cancellationToken);
            if (creationNode == null)
            {
                Debug.Fail("We should always be able to find the anonymous creation we were invoked from.");
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var anonymousType = semanticModel.GetTypeInfo(creationNode, cancellationToken).Type;
            if (anonymousType == null)
            {
                Debug.Fail("We should always be able to get an anonymous type for any anonymous creation node.");
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var containingMember = creationNode.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsMethodLevelMember) ?? creationNode;

            var childCreationNodes = containingMember.DescendantNodesAndSelf()
                                                     .OfType<TAnonymousObjectCreationExpressionSyntax>();
            foreach (var childCreation in childCreationNodes)
            {
                var childType = semanticModel.GetTypeInfo(childCreation, cancellationToken).Type;
                if (childType == null)
                {
                    Debug.Fail("We should always be able to get an anonymous type for any anonymous creation node.");
                    continue;
                }

                if (anonymousType.Equals(childType))
                {
                    ReplaceWithTuple(editor, childCreation);
                }
            }
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                // During a fix-all we don't need to bother with the work to go to the containing
                // method.  Because it's a fix-all, by definition, we'll always be processing all
                // the anon-creation nodes for any given method that is within our scope.
                var node = TryGetCreationNode(diagnostic, cancellationToken);
                if (node == null)
                {
                    Debug.Fail("We should always be able to find the anonymous creation we were invoked from.");
                    continue;
                }

                ReplaceWithTuple(editor, node);
            }

            return Task.CompletedTask;
        }

        private void ReplaceWithTuple(SyntaxEditor editor, TAnonymousObjectCreationExpressionSyntax node)
            => editor.ReplaceNode(
                node, (current, _) =>
                {
                    // Use the callback form as anonymous types may be nested, and we want to
                    // properly replace them even in that case.
                    if (!(current is TAnonymousObjectCreationExpressionSyntax anonCreation))
                    {
                        return current;
                    }

                    return ConvertToTuple(anonCreation).WithAdditionalAnnotations(Formatter.Annotation);
                });

        private static TAnonymousObjectCreationExpressionSyntax TryGetCreationNode(Diagnostic diagnostic, CancellationToken cancellationToken)
            => diagnostic.Location.FindToken(cancellationToken).Parent as TAnonymousObjectCreationExpressionSyntax;

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Convert_to_tuple, createChangedDocument, FeaturesResources.Convert_to_tuple)
            {
            }
        }
    }
}
