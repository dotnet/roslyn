// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Razor.Diagnostics;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class RemoveUnnecessaryDirectivesCodeActionProvider : IRazorCodeActionProvider
{
    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        // We can only provide this code action if diagnostics has ran and filled in our cache with the info we need
        if (!UnusedDirectiveCache.TryGet(context.CodeDocument, out var unusedDirectiveSpans) || unusedDirectiveSpans.Length == 0)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (!context.CodeDocument.TryGetTagHelperRewrittenSyntaxTree(out var tree))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Trigger if the selection start or end is inside any single line directive
        var root = tree.Root;
        var startToken = root.FindToken(context.StartAbsoluteIndex);
        var endToken = context.StartAbsoluteIndex != context.EndAbsoluteIndex
            ? root.FindToken(context.EndAbsoluteIndex)
            : startToken;

        var startDirective = startToken.Parent?.FirstAncestorOrSelf<BaseRazorDirectiveSyntax>();
        var endDirective = endToken.Parent?.FirstAncestorOrSelf<BaseRazorDirectiveSyntax>();

        if (!ShouldOffer(startDirective) && !ShouldOffer(endDirective))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var data = new RemoveUnnecessaryDirectivesCodeActionParams
        {
            UnusedDirectiveSpans = Array.ConvertAll(unusedDirectiveSpans, static s => s.ToRazorTextSpan())
        };

        var resolutionParams = new RazorCodeActionResolutionParams()
        {
            TextDocument = context.Request.TextDocument,
            Action = LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            Language = RazorLanguageKind.Razor,
            DelegatedDocumentUri = context.DelegatedDocumentUri,
            Data = data,
        };

        var action = RazorCodeActionFactory.CreateRemoveUnnecessaryDirectives(resolutionParams);
        return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>([action]);
    }

    private static bool ShouldOffer(BaseRazorDirectiveSyntax? directive)
    {
        if (directive is null)
        {
            return false;
        }

        if (directive is RazorUsingDirectiveSyntax)
        {
            return true;
        }

        // We offer for any single line directive on the assumption that the user has a block of directives
        // at the top of their file that they want to clear up.
        return directive.DirectiveBody.Keyword.GetContent() == SyntaxConstants.CSharp.AddTagHelperKeyword;
    }
}
