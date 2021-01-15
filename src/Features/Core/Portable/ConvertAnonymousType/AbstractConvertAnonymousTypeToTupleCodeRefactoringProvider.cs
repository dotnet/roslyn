// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertAnonymousType
{
    internal abstract class AbstractConvertAnonymousTypeToTupleCodeRefactoringProvider<
        TExpressionSyntax,
        TTupleExpressionSyntax,
        TAnonymousObjectCreationExpressionSyntax>
        : AbstractConvertAnonymousTypeCodeRefactoringProvider<TAnonymousObjectCreationExpressionSyntax>
        where TExpressionSyntax : SyntaxNode
        where TTupleExpressionSyntax : TExpressionSyntax
        where TAnonymousObjectCreationExpressionSyntax : TExpressionSyntax
    {
        protected abstract int GetInitializerCount(TAnonymousObjectCreationExpressionSyntax anonymousType);
        protected abstract TTupleExpressionSyntax ConvertToTuple(TAnonymousObjectCreationExpressionSyntax anonCreation);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var (anonymousNode, _) = await TryGetAnonymousObjectAsync(document, span, cancellationToken).ConfigureAwait(false);
            if (anonymousNode == null)
                return;

            // Analysis is trivial.  All anonymous types with more than two fields are marked as being
            // convertible to a tuple.
            if (GetInitializerCount(anonymousNode) < 2)
                return;

            context.RegisterRefactoring(
                new MyCodeAction(c => FixInCurrentMemberAsync(document, anonymousNode, c)),
                span);
        }

        private async Task<Document> FixInCurrentMemberAsync(
            Document document, TAnonymousObjectCreationExpressionSyntax creationNode, CancellationToken cancellationToken)
        {
            // For the standard invocation of the code-fix, we want to fixup all creations of the
            // "same" anonymous type within the containing method.  We define same-ness as meaning
            // "they have the type symbol".  This means both have the same member names, in the same
            // order, with the same member types.  We fix all these up in the method because the
            // user may be creating several instances of this anonymous type in that method and
            // then combining them in interesting ways (i.e. checking them for equality, using them
            // in collections, etc.).  The language guarantees within a method boundary that these
            // will be the same type and can be used together in this fashion.
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var anonymousType = semanticModel.GetTypeInfo(creationNode, cancellationToken).Type;
            if (anonymousType == null)
            {
                Debug.Fail("We should always be able to get an anonymous type for any anonymous creation node.");
                return document;
            }

            var editor = new SyntaxEditor(root, SyntaxGenerator.GetGenerator(document));
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var containingMember = creationNode.FirstAncestorOrSelf<SyntaxNode, ISyntaxFactsService>((node, syntaxFacts) => syntaxFacts.IsMethodLevelMember(node), syntaxFacts) ?? creationNode;

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
                    ReplaceWithTuple(editor, childCreation);
            }

            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }

        private void ReplaceWithTuple(SyntaxEditor editor, TAnonymousObjectCreationExpressionSyntax node)
            => editor.ReplaceNode(
                node, (current, _) =>
                {
                    // Use the callback form as anonymous types may be nested, and we want to
                    // properly replace them even in that case.
                    if (current is not TAnonymousObjectCreationExpressionSyntax anonCreation)
                        return current;

                    return ConvertToTuple(anonCreation).WithAdditionalAnnotations(Formatter.Annotation);
                });

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Convert_to_tuple, createChangedDocument, AnalyzersResources.Convert_to_tuple)
            {
            }
        }
    }
}
