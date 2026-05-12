// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class UnboundDirectiveAttributeAddUsingCodeActionProvider : IRazorCodeActionProvider
{
    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        if (context.HasSelection)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Only work in component files
        if (!FileKinds.IsComponent(context.CodeDocument.FileKind))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (!context.CodeDocument.TryGetSyntaxRoot(out var syntaxRoot))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Find the node at the cursor position
        var owner = syntaxRoot.FindInnermostNode(context.StartAbsoluteIndex, includeWhitespace: false);
        if (owner is null)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Find a regular markup attribute (not a tag helper attribute) that starts with '@'
        // Unbound directive attributes are just regular attributes that happen to start with '@'
        var attributeBlock = owner.FirstAncestorOrSelf<MarkupAttributeBlockSyntax>();
        if (attributeBlock is null)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Make sure the cursor is actually on the name part, since the attribute block is the whole attribute, including
        // value and even some whitespace
        var nameSpan = attributeBlock.Name.Span;
        if (context.StartAbsoluteIndex < nameSpan.Start || context.StartAbsoluteIndex > nameSpan.End)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Try to find the missing namespace for this directive attribute
        if (!TryGetMissingDirectiveAttributeNamespace(context.CodeDocument, attributeBlock, out var missingNamespace))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Create the code action
        var resolutionParams = AddUsingsCodeActionResolver.CreateAddUsingResolutionParams(
            missingNamespace,
            context.Request.TextDocument,
            additionalEdit: null,
            context.DelegatedDocumentUri);

        var addUsingCodeAction = RazorCodeActionFactory.CreateAddComponentUsing(
            missingNamespace,
            newTagName: null,
            resolutionParams);

        return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>([addUsingCodeAction]);
    }

    private static bool TryGetMissingDirectiveAttributeNamespace(
        RazorCodeDocument codeDocument,
        MarkupAttributeBlockSyntax attributeBlock,
        [NotNullWhen(true)] out string? missingNamespace)
    {
        missingNamespace = null;

        // Check if this is a directive attribute (starts with '@')
        var attributeName = attributeBlock.Name.GetContent();
        if (attributeName is not ['@', ..])
        {
            return false;
        }

        // Get all tag helpers, not just those in scope, since we want to suggest adding a using
        if (!codeDocument.TryGetTagHelpers(out var tagHelpers))
        {
            return false;
        }

        // For attributes with parameters (e.g., @bind:after), extract just the base attribute name
        var baseAttributeName = attributeName.AsSpan();
        var colonIndex = baseAttributeName.IndexOf(':');
        if (colonIndex > 0)
        {
            baseAttributeName = baseAttributeName[..colonIndex];
        }

        // Search for matching bound attribute descriptors in all available tag helpers
        foreach (var tagHelper in tagHelpers)
        {
            if (!tagHelper.IsAttributeDescriptor())
            {
                continue;
            }

            foreach (var boundAttribute in tagHelper.BoundAttributes)
            {
                // No need to worry about multiple matches, because Razor syntax has no way to disambiguate anyway.
                // Currently only compiler can create directive attribute tag helpers anyway.
                if (boundAttribute.IsDirectiveAttribute &&
                    boundAttribute.Name.AsSpan().SequenceEqual(baseAttributeName))
                {
                    if (boundAttribute.Parent.TypeNamespace is { } typeNamespace)
                    {
                        missingNamespace = typeNamespace;
                        return true;
                    }

                    // This is unexpected, but if for some reason we can't find a namespace, there is no point looking further
                    break;
                }
            }
        }

        return false;
    }
}
