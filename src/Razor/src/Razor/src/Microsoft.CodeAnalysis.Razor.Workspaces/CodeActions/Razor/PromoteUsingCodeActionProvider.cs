// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class PromoteUsingCodeActionProvider : IRazorCodeActionProvider
{
    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        if (context.HasSelection)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (!context.CodeDocument.TryGetSyntaxRoot(out var root))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var owner = root.FindNode(TextSpan.FromBounds(context.StartAbsoluteIndex, context.EndAbsoluteIndex));
        if (owner is null)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var directive = owner.FirstAncestorOrSelf<RazorUsingDirectiveSyntax>();
        if (directive is null)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var importFileName = GetImportsFileName(context.DocumentSnapshot.FileKind);

        var line = context.CodeDocument.Source.Text.Lines.GetLineFromPosition(context.StartAbsoluteIndex);
        var data = new PromoteToUsingCodeActionParams
        {
            UsingStart = directive.SpanStart,
            UsingEnd = directive.Span.End,
            RemoveStart = line.Start,
            RemoveEnd = line.EndIncludingLineBreak
        };

        var resolutionParams = new RazorCodeActionResolutionParams()
        {
            TextDocument = context.Request.TextDocument,
            Action = LanguageServerConstants.CodeActions.PromoteUsingDirective,
            Language = RazorLanguageKind.Razor,
            DelegatedDocumentUri = context.DelegatedDocumentUri,
            Data = data
        };

        var action = RazorCodeActionFactory.CreatePromoteUsingDirective(importFileName, resolutionParams);

        return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>([action]);
    }

    public static string GetImportsFileName(RazorFileKind fileKind)
    {
        return fileKind.IsLegacy()
            ? MvcImportProjectFeature.ImportsFileName
            : ComponentHelpers.ImportsFileName;
    }
}
