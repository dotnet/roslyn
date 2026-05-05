// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Razor;

internal class SortAndConsolidateUsingsCodeActionProvider : IRazorCodeActionProvider
{
    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        if (!context.CodeDocument.TryGetSyntaxRoot(out var root))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Trigger if the selection start or end is inside any using directive
        var startDirective = root.FindToken(context.StartAbsoluteIndex).Parent?.FirstAncestorOrSelf<RazorUsingDirectiveSyntax>();
        var endDirective = context.HasSelection
            ? root.FindToken(context.EndAbsoluteIndex).Parent?.FirstAncestorOrSelf<RazorUsingDirectiveSyntax>()
            : startDirective;

        if (startDirective is null && endDirective is null)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (!UsingDirectiveHelper.NeedsSortOrConsolidate(context.CodeDocument))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var resolutionParams = new RazorCodeActionResolutionParams()
        {
            TextDocument = context.Request.TextDocument,
            Action = LanguageServerConstants.CodeActions.SortAndConsolidateUsings,
            Language = RazorLanguageKind.Razor,
            DelegatedDocumentUri = context.DelegatedDocumentUri,
            Data = true
        };

        var codeAction = RazorCodeActionFactory.CreateSortAndConsolidateUsings(resolutionParams);
        return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>([codeAction]);
    }
}
