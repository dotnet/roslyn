// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertAnonymousType;

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

        var (anonymousNode, anonymousType) = await TryGetAnonymousObjectAsync(document, span, cancellationToken).ConfigureAwait(false);
        if (anonymousNode == null || anonymousType == null)
            return;

        // Analysis is trivial.  All anonymous types with more than two fields are marked as being
        // convertible to a tuple.
        if (GetInitializerCount(anonymousNode) < 2)
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var allAnonymousNodes = GetAllAnonymousTypesInContainer(document, semanticModel, anonymousNode, cancellationToken);

        // If we have multiple different anonymous types in this member, then offer two fixes, one to just fixup this
        // anonymous type, and one to fixup all anonymous types.
        if (allAnonymousNodes.Any(t => !anonymousType.Equals(t.symbol, SymbolEqualityComparer.Default)))
        {
            context.RegisterRefactoring(
                CodeAction.Create(
                    FeaturesResources.Convert_to_tuple,
                    [
                        CodeAction.Create(FeaturesResources.just_this_anonymous_type, c => FixInCurrentMemberAsync(document, anonymousNode, anonymousType, allAnonymousTypes: false, c), nameof(FeaturesResources.just_this_anonymous_type)),
                        CodeAction.Create(FeaturesResources.all_anonymous_types_in_container, c => FixInCurrentMemberAsync(document, anonymousNode, anonymousType, allAnonymousTypes: true, c), nameof(FeaturesResources.all_anonymous_types_in_container)),
                    ],
                    isInlinable: false),
                span);
        }
        else
        {
            // otherwise, just offer the change to the single tuple type.
            context.RegisterRefactoring(
                CodeAction.Create(FeaturesResources.Convert_to_tuple, c => FixInCurrentMemberAsync(document, anonymousNode, anonymousType, allAnonymousTypes: false, c), nameof(FeaturesResources.Convert_to_tuple)),
                span);
        }
    }

    private IEnumerable<(TAnonymousObjectCreationExpressionSyntax node, INamedTypeSymbol symbol)> GetAllAnonymousTypesInContainer(
        Document document,
        SemanticModel semanticModel,
        TAnonymousObjectCreationExpressionSyntax anonymousNode,
        CancellationToken cancellationToken)
    {
        // Now see if we have any other anonymous types (with a different shape) in the containing member.
        // If so, offer to fix those up as well.
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var containingMember = anonymousNode.FirstAncestorOrSelf<SyntaxNode, ISyntaxFactsService>((node, syntaxFacts) => syntaxFacts.IsMethodLevelMember(node), syntaxFacts) ?? anonymousNode;

        var childCreationNodes = containingMember.DescendantNodesAndSelf()
            .OfType<TAnonymousObjectCreationExpressionSyntax>()
            .Where(s => this.GetInitializerCount(s) >= 2);

        foreach (var childNode in childCreationNodes)
        {
            if (semanticModel.GetTypeInfo(childNode, cancellationToken).Type is INamedTypeSymbol childType)
                yield return (childNode, childType);
        }
    }

    private async Task<Document> FixInCurrentMemberAsync(
        Document document, TAnonymousObjectCreationExpressionSyntax creationNode, INamedTypeSymbol anonymousType, bool allAnonymousTypes, CancellationToken cancellationToken)
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
        var editor = new SyntaxEditor(root, SyntaxGenerator.GetGenerator(document));

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var otherAnonymousNodes = GetAllAnonymousTypesInContainer(document, semanticModel, creationNode, cancellationToken);

        foreach (var (node, symbol) in otherAnonymousNodes)
        {
            if (allAnonymousTypes || anonymousType.Equals(symbol, SymbolEqualityComparer.Default))
                ReplaceWithTuple(editor, node);
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
}
