// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractClass;

internal abstract class AbstractExtractClassRefactoringProvider(IExtractClassOptionsService? service) : CodeRefactoringProvider
{
    private readonly IExtractClassOptionsService? _optionsService = service;

    protected abstract Task<ImmutableArray<SyntaxNode>> GetSelectedNodesAsync(CodeRefactoringContext context);
    protected abstract Task<SyntaxNode?> GetSelectedClassDeclarationAsync(CodeRefactoringContext context);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        // For simplicity if we can't add a document the don't supply this refactoring. Not checking this results in known
        // cases that won't work because the refactoring may try to add a document. There's non-trivial
        // work to support a user interaction that makes sense for those cases. 
        // See: https://github.com/dotnet/roslyn/issues/50868
        var solution = context.Document.Project.Solution;
        if (!solution.CanApplyChange(ApplyChangesKind.AddDocument))
        {
            return;
        }

        var optionsService = _optionsService ?? solution.Services.GetService<IExtractClassOptionsService>();
        if (optionsService is null)
        {
            return;
        }

        var (action, hasBaseType) = await TryGetMemberActionAsync(context, optionsService).ConfigureAwait(false);

        // If the action was not offered because we know the containing type
        // already has a base class, no need to do extra work to see if just a class is selected
        if (action is null && !hasBaseType)
        {
            action = await TryGetClassActionAsync(context, optionsService).ConfigureAwait(false);
        }

        if (action != null)
        {
            context.RegisterRefactoring(action, action.Span);
        }
    }

    private async Task<(ExtractClassWithDialogCodeAction? action, bool hasBaseType)> TryGetMemberActionAsync(CodeRefactoringContext context, IExtractClassOptionsService optionsService)
    {
        var selectedMemberNodes = await GetSelectedNodesAsync(context).ConfigureAwait(false);
        if (selectedMemberNodes.IsEmpty)
        {
            return (null, false);
        }

        var (document, span, cancellationToken) = context;
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var memberNodeSymbolPairs = selectedMemberNodes
            .SelectAsArray(m => (node: m, symbol: semanticModel.GetRequiredDeclaredSymbol(m, cancellationToken)))
            // Use same logic as pull members up for determining if a selected member
            // is valid to be moved into a base
            .WhereAsArray(pair => MemberAndDestinationValidator.IsMemberValid(pair.symbol));

        if (memberNodeSymbolPairs.IsEmpty)
        {
            return (null, false);
        }

        var selectedMembers = memberNodeSymbolPairs.SelectAsArray(pair => pair.symbol);

        var containingType = selectedMembers.First().ContainingType;
        Contract.ThrowIfNull(containingType);

        // Treat the entire nodes' span as the span of interest here.  That way if the user's location is closer to
        // a refactoring with a narrower span (for example, a span just on the name/parameters of a member, then it
        // will take precedence over us).
        var memberSpan = TextSpan.FromBounds(
            memberNodeSymbolPairs.First().node.FullSpan.Start,
            memberNodeSymbolPairs.Last().node.FullSpan.End);

        if (HasBaseType(containingType))
        {
            return (null, true);
        }

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var containingTypeDeclarationNode = selectedMemberNodes.First().FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsTypeDeclaration);
        if (containingTypeDeclarationNode is null)
        {
            // If the containing type node isn't found exit. This could be malformed code that we don't know
            // how to correctly handle
            return (null, false);
        }

        if (selectedMemberNodes.Any(m => m.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsTypeDeclaration) != containingTypeDeclarationNode))
        {
            return (null, false);
        }

        var action = new ExtractClassWithDialogCodeAction(
            document, memberSpan, optionsService, containingType, containingTypeDeclarationNode, context.Options, selectedMembers);

        return (action, false);
    }

    private async Task<ExtractClassWithDialogCodeAction?> TryGetClassActionAsync(CodeRefactoringContext context, IExtractClassOptionsService optionsService)
    {
        var selectedClassNode = await GetSelectedClassDeclarationAsync(context).ConfigureAwait(false);
        if (selectedClassNode is null)
        {
            return null;
        }

        var (document, span, cancellationToken) = context;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel.GetDeclaredSymbol(selectedClassNode, cancellationToken) is not INamedTypeSymbol selectedType)
        {
            return null;
        }

        if (HasBaseType(selectedType))
        {
            return null;
        }

        return new ExtractClassWithDialogCodeAction(
            document, span, optionsService, selectedType, selectedClassNode, context.Options, selectedMembers: []);
    }

    private static bool HasBaseType(INamedTypeSymbol containingType) => containingType.BaseType?.SpecialType != SpecialType.System_Object;
}
