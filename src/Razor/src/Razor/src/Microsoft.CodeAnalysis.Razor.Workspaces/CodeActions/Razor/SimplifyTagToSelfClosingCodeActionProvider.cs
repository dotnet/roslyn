// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Razor;

internal class SimplifyTagToSelfClosingCodeActionProvider : IRazorCodeActionProvider
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

        // Caret must be inside a markup element
        if (context.ContainsDiagnostic(ComponentDiagnosticFactory.UnexpectedMarkupElement.Id) ||
            context.ContainsDiagnostic(ComponentDiagnosticFactory.UnexpectedClosingTag.Id))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var syntaxTree = context.CodeDocument.GetTagHelperRewrittenSyntaxTree();
        if (syntaxTree?.Root is null)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var owner = syntaxTree.Root.FindInnermostNode(context.StartAbsoluteIndex, includeWhitespace: false)?.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>();
        if (owner is not MarkupTagHelperElementSyntax markupElementSyntax ||
            markupElementSyntax.TagHelperStartTag is not { } startTag ||
            markupElementSyntax.TagHelperEndTag is not { } endTag)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Check whether the code action is applicable to the element
        if (!IsApplicableTo(markupElementSyntax))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Provide code action to simplify
        var actionParams = new SimplifyTagToSelfClosingCodeActionParams
        {
            StartTagCloseAngleIndex = startTag.CloseAngle.SpanStart,
            EndTagCloseAngleIndex = endTag.CloseAngle.EndPosition,
        };

        var resolutionParams = new RazorCodeActionResolutionParams()
        {
            TextDocument = context.Request.TextDocument,
            Action = LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing,
            Language = RazorLanguageKind.Razor,
            DelegatedDocumentUri = context.DelegatedDocumentUri,
            Data = actionParams,
        };

        var codeAction = RazorCodeActionFactory.CreateSimplifyTagToSelfClosing(resolutionParams);
        return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>([codeAction]);
    }

    internal static bool IsApplicableTo(MarkupTagHelperElementSyntax markupElementSyntax)
    {
        // If there is no end tag, then the element is either already self-closing, or invalid. Either way, don't offer.
        if (markupElementSyntax.EndTag is null)
        {
            return false;
        }

        if (markupElementSyntax is not { TagHelperInfo.BindingResult.TagHelpers: { Count: > 0 } tagHelpers })
        {
            return false;
        }

        // Check whether the element has any non-whitespace content
        if (markupElementSyntax is { Body: { } body } && body.Any(static n => !n.ContainsOnlyWhitespace()))
        {
            return false;
        }

        // Get symbols for the markup element
        var boundTagHelper = tagHelpers.FirstOrDefault(static d => d.Kind == TagHelperKind.Component);
        if (boundTagHelper == null)
        {
            return false;
        }

        // Check whether the Component must have children
        foreach (var attribute in boundTagHelper.BoundAttributes)
        {
            // Parameter is not required
            if (attribute is { IsEditorRequired: false })
            {
                continue;
            }

            // Parameter is not a `RenderFragment` or `RenderFragment<T>`
            if (!attribute.IsChildContentProperty())
            {
                continue;
            }

            // Parameter is not set or bound as an attribute
            if (!markupElementSyntax.TagHelperInfo!.BindingResult.Attributes.Any(a =>
                RazorSyntaxFacts.TryGetComponentParameterNameFromFullAttributeName(a.Key, out var componentParameterName, out var directiveAttributeParameter) &&
                componentParameterName.SequenceEqual(attribute.Name) &&
                directiveAttributeParameter is { IsEmpty: true } or "get"
            ))
            {
                return false;
            }
        }

        return true;
    }
}
