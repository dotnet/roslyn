// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class ComponentAccessibilityCodeActionProvider(IFileSystem fileSystem) : IRazorCodeActionProvider
{
    private readonly IFileSystem _fileSystem = fileSystem;

    public async Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(
        RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        // Locate cursor
        var node = context.CodeDocument.GetRequiredSyntaxRoot().FindInnermostNode(context.StartAbsoluteIndex);
        if (node is null)
        {
            return [];
        }

        // Find start tag. We allow this code action to work from anywhere in the start tag, which includes
        // embedded C#, so we just have to traverse up the tree to find a start tag if there is one.
        // We also check for tag helper start tags here, because an invalid start tag with a valid tag helper
        // anywhere in it  would otherwise not match. We rely on the IsTagUnknown method below, to ensure we
        // only offer on actual potential component tags (because it checks for the compiler diagnostic)
        var startTag = node.FirstAncestorOrSelf<BaseMarkupStartTagSyntax>();
        if (startTag is null)
        {
            return [];
        }

        if (context.StartAbsoluteIndex < startTag.SpanStart)
        {
            // Cursor is before the start tag, so we shouldn't show a light bulb. This can happen
            // in cases where the cursor is in whitespace at the beginning of the document
            // eg: $$ <Component></Component>
            return [];
        }

        // Ignore if start tag has dots, as we only handle short tags
        if (startTag.Name.Content.Contains('.'))
        {
            return [];
        }

        if (!IsApplicableTag(startTag))
        {
            return [];
        }

        if (!IsTagUnknown(startTag, context))
        {
            return [];
        }

        using var _ = ListPool<RazorVSInternalCodeAction>.GetPooledObject(out var codeActions);
        await AddComponentAccessFromTagAsync(context, startTag, codeActions, cancellationToken).ConfigureAwait(false);
        AddCreateComponentFromTag(context, startTag, codeActions);

        return [.. codeActions];
    }

    private static bool IsApplicableTag(BaseMarkupStartTagSyntax startTag)
    {
        if (startTag.Name.Width == 0)
        {
            // Empty tag name, we shouldn't show a light bulb just to create an empty file.
            return false;
        }

        return true;
    }

    private void AddCreateComponentFromTag(
        RazorCodeActionContext context, BaseMarkupStartTagSyntax startTag, List<RazorVSInternalCodeAction> container)
    {
        if (!context.SupportsFileCreation)
        {
            return;
        }

        var path = context.Request.TextDocument.DocumentUri.GetAbsoluteOrUNCPath();
        path = FilePathNormalizer.Normalize(path);

        var directoryName = Path.GetDirectoryName(path);
        Assumes.NotNull(directoryName);

        var newComponentPath = Path.Combine(directoryName, $"{startTag.Name.Content}.razor");
        if (_fileSystem.FileExists(newComponentPath))
        {
            return;
        }

        var actionParams = new CreateComponentCodeActionParams
        {
            Path = newComponentPath,
        };

        var resolutionParams = new RazorCodeActionResolutionParams
        {
            TextDocument = context.Request.TextDocument,
            Action = LanguageServerConstants.CodeActions.CreateComponentFromTag,
            Language = RazorLanguageKind.Razor,
            DelegatedDocumentUri = context.DelegatedDocumentUri,
            Data = actionParams,
        };

        var codeAction = RazorCodeActionFactory.CreateComponentFromTag(resolutionParams);
        container.Add(codeAction);
    }

    private static async Task AddComponentAccessFromTagAsync(
        RazorCodeActionContext context,
        BaseMarkupStartTagSyntax startTag,
        List<RazorVSInternalCodeAction> container,
        CancellationToken cancellationToken)
    {
        var haveAddedNonQualifiedFix = false;

        // First see if there are any components that match in name, but not case, without qualification
        foreach (var t in context.CodeDocument.GetRequiredTagHelperContext().TagHelpers)
        {
            if (t.TagMatchingRules is [{ CaseSensitive: true } rule] &&
                rule.TagName.Equals(startTag.Name.Content, StringComparison.OrdinalIgnoreCase) &&
                rule.TagName != startTag.Name.Content)
            {
                var renameTagWorkspaceEdit = CreateRenameTagEdit(context, startTag, rule.TagName);
                var fixCasingCodeAction = RazorCodeActionFactory.CreateFullyQualifyComponent(rule.TagName, renameTagWorkspaceEdit);
                container.Add(fixCasingCodeAction);
                haveAddedNonQualifiedFix = true;
                break;
            }
        }

        var matching = await FindMatchingTagHelpersAsync(context, startTag, cancellationToken).ConfigureAwait(false);

        // For all the matches, add options for add @using and fully qualify
        foreach (var tagHelperPair in matching)
        {
            if (tagHelperPair.FullyQualified is null)
            {
                continue;
            }

            // If they have a typo, eg <CounTer /> and we've offered them <Counter /> above, then it would be odd to offer
            // them <BlazorApp.Pages.Counter /> as well. We will offer them <BlazorApp.MisTypedPages.CounTer /> though, if it
            // exists.
            if (!haveAddedNonQualifiedFix || !tagHelperPair.CaseInsensitiveMatch)
            {
                // if fqn contains a generic typeparam, we should strip it out. Otherwise, replacing tag name will leave generic parameters in razor code, which are illegal
                // e.g. <Component /> -> <Component<T> />
                var fullyQualifiedName = RazorComponentSearchEngine.RemoveGenericContent(tagHelperPair.Short.Name.AsMemory()).ToString();

                // If the match was case insensitive, then see if we can work out a new tag name to use as part of adding a using statement
                TextDocumentEdit? additionalEdit = null;
                string? newTagName = null;
                if (tagHelperPair.CaseInsensitiveMatch)
                {
                    newTagName = tagHelperPair.Short.TagMatchingRules.FirstOrDefault()?.TagName;
                    if (newTagName is not null)
                    {
                        additionalEdit = CreateRenameTagEdit(context, startTag, newTagName).DocumentChanges!.Value.First().First.AssumeNotNull();
                    }
                }

                // We only want to add a using statement if this was a case sensitive match, or if we were able to determine a new tag
                // name to give the tag.
                if (!tagHelperPair.CaseInsensitiveMatch || newTagName is not null)
                {
                    if (AddUsingsCodeActionResolver.TryCreateAddUsingResolutionParams(fullyQualifiedName, context.Request.TextDocument, additionalEdit, context.DelegatedDocumentUri, out var @namespace, out var resolutionParams))
                    {
                        var addUsingCodeAction = RazorCodeActionFactory.CreateAddComponentUsing(@namespace, newTagName, resolutionParams);
                        container.Add(addUsingCodeAction);
                    }
                }

                // Fully qualify
                var renameTagWorkspaceEdit = CreateRenameTagEdit(context, startTag, fullyQualifiedName);
                var fullyQualifiedCodeAction = RazorCodeActionFactory.CreateFullyQualifyComponent(fullyQualifiedName, renameTagWorkspaceEdit);
                container.Add(fullyQualifiedCodeAction);
            }
        }
    }

    private static async Task<ImmutableArray<TagHelperPair>> FindMatchingTagHelpersAsync(
        RazorCodeActionContext context, BaseMarkupStartTagSyntax startTag, CancellationToken cancellationToken)
    {
        // Get all data necessary for matching
        var tagName = startTag.Name.Content;
        string? parentTagName = null;
        if (startTag.Parent?.Parent is BaseMarkupElementSyntax parentElement)
        {
            parentTagName = parentElement.StartTag?.Name.Content ?? parentElement.EndTag?.Name.Content;
        }

        var attributes = TagHelperFacts.StringifyAttributes(startTag.Attributes);

        // Find all matching tag helpers
        using var _ = DictionaryPool<string, TagHelperPair>.GetPooledObject(out var matching);

        var tagHelpers = await context.DocumentSnapshot.Project.GetTagHelpersAsync(cancellationToken).ConfigureAwait(false);

        foreach (var tagHelper in tagHelpers)
        {
            if (SatisfiesRules(tagHelper.TagMatchingRules, tagName.AsSpan(), parentTagName.AsSpan(), attributes, out var caseInsensitiveMatch))
            {
                matching.Add(tagHelper.Name, new TagHelperPair(tagHelper, caseInsensitiveMatch));
            }
        }

        // Iterate and find the fully qualified version
        foreach (var tagHelper in tagHelpers)
        {
            if (matching.TryGetValue(tagHelper.Name, out var tagHelperPair))
            {
                if (tagHelperPair != null && tagHelper != tagHelperPair.Short)
                {
                    tagHelperPair.FullyQualified = tagHelper;
                }
            }
        }

        return [.. matching.Values];
    }

    private static bool SatisfiesRules(
        ImmutableArray<TagMatchingRuleDescriptor> tagMatchingRules,
        ReadOnlySpan<char> tagNameWithoutPrefix,
        ReadOnlySpan<char> parentTagNameWithoutPrefix,
        ImmutableArray<KeyValuePair<string, string>> tagAttributes,
        out bool caseInsensitiveMatch)
    {
        caseInsensitiveMatch = false;

        foreach (var rule in tagMatchingRules)
        {
            // We have to match parent tag and attributes regardless, so check them first and exit early if there is a fail
            if (!TagHelperMatchingConventions.SatisfiesParentTag(rule, parentTagNameWithoutPrefix) ||
               !TagHelperMatchingConventions.SatisfiesAttributes(rule, tagAttributes))
            {
                return false;
            }

            // Tag helpers that target catch-all will come back as satisfying the rule, but we don't want to use them for the code action
            // so we have to check that ourselves
            if (rule.TagName is null or TagHelperMatchingConventions.ElementCatchAllName)
            {
                return false;
            }
            else if (TagHelperMatchingConventions.SatisfiesTagName(rule, tagNameWithoutPrefix))
            {
                // Nothing to do, just loop around to the next rule
            }
            else if (TagHelperMatchingConventions.SatisfiesTagName(rule, tagNameWithoutPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // Because the code action will be fixing the casing of the tag, we don't need all the rules to be consistent.
                caseInsensitiveMatch = true;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private static WorkspaceEdit CreateRenameTagEdit(
        RazorCodeActionContext context, BaseMarkupStartTagSyntax startTag, string newTagName)
    {
        using var textEdits = new PooledArrayBuilder<SumType<TextEdit, AnnotatedTextEdit>>();
        var codeDocumentIdentifier = new OptionalVersionedTextDocumentIdentifier() { DocumentUri = context.Request.TextDocument.DocumentUri };

        var startTagTextEdit = LspFactory.CreateTextEdit(startTag.Name.GetRange(context.CodeDocument.Source), newTagName);

        textEdits.Add(startTagTextEdit);

        var endTag = startTag.GetEndTag();
        if (endTag != null)
        {
            var endTagTextEdit = LspFactory.CreateTextEdit(endTag.Name.GetRange(context.CodeDocument.Source), newTagName);
            textEdits.Add(endTagTextEdit);
        }

        return new WorkspaceEdit
        {
            DocumentChanges = new TextDocumentEdit[]
            {
                new TextDocumentEdit()
                {
                    TextDocument = codeDocumentIdentifier,
                    Edits = textEdits.ToArray()
                }
            },
        };
    }

    private static bool IsTagUnknown(BaseMarkupStartTagSyntax startTag, RazorCodeActionContext context)
    {
        foreach (var diagnostic in context.CodeDocument.GetRequiredCSharpDocument().Diagnostics)
        {
            // Check that the diagnostic is to do with our start tag
            if (!(diagnostic.Span.AbsoluteIndex > startTag.Span.End
                || startTag.Span.Start > diagnostic.Span.AbsoluteIndex + diagnostic.Span.Length))
            {
                // Component is not recognized in environment
                if (diagnostic.Id == ComponentDiagnosticFactory.UnexpectedMarkupElement.Id)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private sealed record class TagHelperPair(TagHelperDescriptor Short, bool CaseInsensitiveMatch)
    {
        public TagHelperDescriptor? FullyQualified { get; set; }
    }
}
