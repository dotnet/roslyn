// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Razor;

internal class SimplifyFullyQualifiedComponentCodeActionProvider : IRazorCodeActionProvider
{
    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        if (context.HasSelection)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Make sure we're in the right kind and part of file
        if (!FileKinds.IsComponent(context.CodeDocument.FileKind))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (context.LanguageKind != RazorLanguageKind.Html)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (!context.CodeDocument.TryGetSyntaxRoot(out var syntaxRoot))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Find the tag at the cursor position, if it's on the start tag (name portion) or end tag only.
        var owner = syntaxRoot.FindInnermostNode(context.StartAbsoluteIndex, includeWhitespace: true) switch
        {
            MarkupTagHelperStartTagSyntax ownerStartTag when ownerStartTag.Name.Span.Contains(context.StartAbsoluteIndex) => ownerStartTag.Parent,
            MarkupTagHelperEndTagSyntax endTag => endTag.Parent,
            _ => null
        };

        if (owner is not MarkupTagHelperElementSyntax markupElementSyntax ||
            markupElementSyntax.TagHelperStartTag is not { } startTag)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // If there are any diagnostics on the start tag, we shouldn't offer
        if (HasDiagnosticsOnStartTag(markupElementSyntax, context))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Check whether the element represents a fully qualified component
        if (!IsFullyQualifiedComponent(markupElementSyntax, out var @namespace, out var componentName))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Create the action params
        var actionParams = new SimplifyFullyQualifiedComponentCodeActionParams
        {
            Namespace = @namespace,
            ComponentName = componentName,
            StartTagSpanStart = startTag.Name.SpanStart,
            StartTagSpanEnd = startTag.Name.Span.End,
            EndTagSpanStart = markupElementSyntax.TagHelperEndTag?.Name.SpanStart ?? -1,
            EndTagSpanEnd = markupElementSyntax.TagHelperEndTag?.Name.Span.End ?? -1,
        };

        var resolutionParams = new RazorCodeActionResolutionParams()
        {
            TextDocument = context.Request.TextDocument,
            Action = LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent,
            Language = RazorLanguageKind.Razor,
            DelegatedDocumentUri = context.DelegatedDocumentUri,
            Data = actionParams,
        };

        var codeAction = RazorCodeActionFactory.CreateSimplifyFullyQualifiedComponent(resolutionParams);
        return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>([codeAction]);
    }

    private static bool HasDiagnosticsOnStartTag(MarkupTagHelperElementSyntax element, RazorCodeActionContext context)
    {
        if (context.Request.Context.Diagnostics is null)
        {
            return false;
        }

        if (element.TagHelperStartTag is not { } startTag)
        {
            return false;
        }

        var startTagSpan = startTag.Span;
        foreach (var diagnostic in context.Request.Context.Diagnostics)
        {
            if (diagnostic.Range is null)
            {
                continue;
            }

            if (!context.SourceText.TryGetAbsoluteIndex(diagnostic.Range.Start, out var diagnosticStart) ||
                !context.SourceText.TryGetAbsoluteIndex(diagnostic.Range.End, out var diagnosticEnd))
            {
                continue;
            }

            // Check if diagnostic overlaps with the start tag
            if (diagnosticStart < startTagSpan.End && diagnosticEnd > startTagSpan.Start)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFullyQualifiedComponent(MarkupTagHelperElementSyntax element, out string @namespace, out string componentName)
    {
        @namespace = string.Empty;
        componentName = string.Empty;

        var tagHelpers = element.TagHelperInfo.BindingResult.TagHelpers;
        var boundTagHelper = tagHelpers.FirstOrDefault(static d => d.Kind == TagHelperKind.Component);
        if (boundTagHelper is null)
        {
            return false;
        }

        // Check if this is a fully qualified name match
        if (!boundTagHelper.IsFullyQualifiedNameMatch)
        {
            return false;
        }

        var fullyQualifiedName = boundTagHelper.Name;

        // Extract the namespace and component name
        var lastDotIndex = fullyQualifiedName.LastIndexOf('.');
        if (lastDotIndex < 0)
        {
            return false;
        }

        @namespace = fullyQualifiedName[..lastDotIndex];
        componentName = fullyQualifiedName[(lastDotIndex + 1)..];
        return true;
    }
}
