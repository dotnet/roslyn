// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class ExtractToCssCodeActionProvider(ILoggerFactory loggerFactory) : IRazorCodeActionProvider
{
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<ExtractToCssCodeActionProvider>();

    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        if (!context.SupportsFileCreation)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (!context.CodeDocument.FileKind.IsComponent())
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (!context.CodeDocument.TryGetSyntaxRoot(out var root))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var owner = root.FindInnermostNode(context.StartAbsoluteIndex);
        if (owner is null)
        {
            _logger.LogWarning("Owner should never be null.");
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // If we're inside an element, move to the start tag so the following checks work as expected
        if (owner is MarkupTextLiteralSyntax { Parent: MarkupElementSyntax { StartTag: { } startTag } })
        {
            owner = startTag;
        }

        // We have to be in a style tag (or inside it, but we'll have moved to the parent if so, above)
        if (owner is not (MarkupStartTagSyntax { Name.Content: "style" } or MarkupEndTagSyntax { Name.Content: "style" }))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // If there is any C# or Razor in the style tag, we can't offer, so it has to be one big text literal.
        if (owner.Parent is not MarkupElementSyntax { Body: [MarkupTextLiteralSyntax textLiteral] } markupElement ||
            textLiteral.ChildNodes().Any())
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (textLiteral.LiteralTokens.All(static t => t.IsWhitespace()))
        {
            // If the text literal is all whitespace, we don't want to offer the action.
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // If there are diagnostics, we can't trust the tree to be what we expect.
        if (markupElement.GetDiagnostics().Any(static d => d.Severity == RazorDiagnosticSeverity.Error))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var actionParams = new ExtractToCssCodeActionParams()
        {
            ExtractStart = textLiteral.Span.Start,
            ExtractEnd = textLiteral.Span.End,
            RemoveStart = markupElement.Span.Start,
            RemoveEnd = markupElement.Span.End
        };

        var resolutionParams = new RazorCodeActionResolutionParams()
        {
            TextDocument = context.Request.TextDocument,
            Action = LanguageServerConstants.CodeActions.ExtractToCss,
            Language = RazorLanguageKind.Razor,
            DelegatedDocumentUri = context.DelegatedDocumentUri,
            Data = actionParams,
        };

        var razorFileName = Path.GetFileName(context.Request.TextDocument.DocumentUri.GetAbsoluteOrUNCPath());
        var codeAction = RazorCodeActionFactory.CreateExtractToCss(razorFileName, resolutionParams);
        return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>([codeAction]);
    }
}
