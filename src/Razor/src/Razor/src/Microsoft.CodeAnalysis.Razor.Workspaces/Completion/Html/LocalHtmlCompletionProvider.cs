// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.Razor;
using Roslyn.Text.Adornments;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion.Html;

/// <summary>
/// Produces HTML element, attribute, attribute-value, and entity completions directly from the
/// compiled <see cref="HtmlCompletionData"/> schema, eliminating the LSP round-trip to the
/// external HTML language server for standard HTML completions.
/// </summary>
internal static partial class LocalHtmlCompletionProvider
{

    /// <summary>
    /// Commit characters for HTML attribute completions. The '=' commits without inserting
    /// because in all cases '=' is already present — either in the snippet insert text
    /// (e.g., class="$0") or in the existing document text being preserved by the edit range.
    /// Space allows quickly committing and starting the next attribute.
    /// Uses the shared <see cref="DefaultCommitCharacters"/> to ensure reference-equal arrays
    /// with directive attribute items, enabling optimizer promotion to list-level defaults.
    /// </summary>
    private static readonly SumType<string[], VSInternalCommitCharacter[]> s_attributeCommitCharacters =
        RazorCommitCharacter.ToVsCommitCharacters(DefaultCommitCharacters.GetAttributeCommitCharacters(useEquals: true));

    /// <summary>
    /// Commit characters for boolean (minimized) attribute completions. Space uses Insert=true
    /// because no snippet is appended — the cursor ends up right after the attribute name, and
    /// space should insert as a separator before the next attribute.
    /// </summary>
    private static readonly SumType<string[], VSInternalCommitCharacter[]> s_minimizedAttributeCommitCharacters =
        RazorCommitCharacter.ToVsCommitCharacters(DefaultCommitCharacters.GetMinimizedAttributeCommitCharacters());

    /// <summary>
    /// Commit characters for prefix group items. The dash commits without inserting
    /// since InsertText already ends with it.
    /// </summary>
    private static readonly SumType<string[], VSInternalCommitCharacter[]> s_prefixGroupCommitCharacters =
        RazorCommitCharacter.ToVsCommitCharacters(DefaultCommitCharacters.GetPrefixGroupCommitCharacters());

    /// <summary>
    /// Command that re-triggers completion after committing a prefix group item.
    /// </summary>
    private static readonly Command s_retriggerCompletionCommand = new()
    {
        CommandIdentifier = "editor.action.triggerSuggest",
        Title = "",
    };

    /// <summary>
    /// Authoritative empty completion list — signals that no completions are available
    /// and prevents fallback to an external provider.
    /// </summary>
    /// <remarks>
    /// This is a shared singleton. Callers must not mutate the returned instance.
    /// Safety is guaranteed by the <c>Items.Length > 0</c> guard in RemoteCompletionService
    /// which prevents this instance from reaching code paths that attach resultIds or run the optimizer.
    /// </remarks>
    private static readonly RazorVSInternalCompletionList s_emptyCompletionList = new()
    {
        Items = [],
        IsIncomplete = false,
    };

    /// <summary>
    /// Collapsible attribute prefix groups. When the user hasn't typed past the dash,
    /// we show a single "prefix-…" group item instead of all individual items.
    /// </summary>
    private static readonly (string Prefix, HtmlAttributeKind Kind, string Label)[] s_attributePrefixGroups =
    [
        ("aria-", HtmlAttributeKind.Aria, "aria-…"),
        ("data-", HtmlAttributeKind.Data, "data-…"),
        ("ng-", HtmlAttributeKind.Angular, "ng-…"),
    ];

    /// <summary>
    /// Attempts to produce an HTML completion list for the given position. Returns null when the
    /// position requires an external completion provider (e.g., script/style content, attribute
    /// values like CSS classes or file paths, or positions not recognized as an HTML completion context).
    /// </summary>
    public static bool TryGetHtmlCompletionList(
        RazorCompletionContext completionContext,
        [NotNullWhen(true)] out RazorVSInternalCompletionList? completionList,
        [NotNullWhen(true)] out LocalHtmlCompletionResolveContext? resolveContext)
    {
        completionList = null;
        resolveContext = null;

        if (!TryGetPositionContext(completionContext, out var context))
        {
            return false;
        }

        completionList = context.Kind switch
        {
            PositionKind.None => s_emptyCompletionList,
            PositionKind.Element => GetElementCompletionList(completionContext.Options, context, out resolveContext),
            PositionKind.Attribute => GetAttributeCompletionList(context, out resolveContext),
            PositionKind.AttributeValue => GetAttributeValueCompletionList(context),
            PositionKind.CloseTag => GetCloseTagCompletionList(completionContext.Options, context),
            PositionKind.Entity => EntityCompletion.BuildCompletionList(context.ReplacementRange),
            _ => throw new InvalidOperationException($"Unexpected PositionKind: {context.Kind}"),
        };

        if (completionList is null)
        {
            resolveContext = null;
            return false;
        }

        resolveContext ??= LocalHtmlCompletionResolveContext.Empty;
        return true;
    }

    /// <summary>
    /// Determines what kind of HTML completion applies at the current position.
    /// Returns false when the owner node cannot be determined (unparseable document)
    /// or when the position is inside script/style content that requires an external provider.
    /// Returns true with <see cref="PositionKind.None"/> when the position is in HTML content but not
    /// in a recognized completion context (plain text between tags, etc.).
    /// </summary>
    internal static bool TryGetPositionContext(RazorCompletionContext completionContext, out PositionContext context)
    {
        var absoluteIndex = completionContext.AbsoluteIndex;
        var sourceText = completionContext.CodeDocument.Source.Text;

        var owner = completionContext.Owner;
        if (owner is null)
        {
            context = default;
            return false;
        }

        owner = CompletionContextHelper.AdjustSyntaxNodeForCompletion(owner);
        if (owner is null || IsInScriptOrStyleBlock(owner))
        {
            context = default;
            return false;
        }

        if (TryGetElementNamePositionContext(owner, absoluteIndex, sourceText, out context)
            || TryGetCloseTagPositionContext(owner, absoluteIndex, sourceText, out context)
            || TryGetAttributeNamePositionContext(owner, absoluteIndex, sourceText, out context)
            || TryGetAttributeValuePositionContext(owner, absoluteIndex, sourceText, out context)
            || TryGetEntityPositionContext(owner, absoluteIndex, sourceText, out context))
        {
            return true;
        }

        // Position is in HTML content but not in a recognized completion context (e.g., plain text
        // between tags, Razor directive attributes). No HTML completions apply — return None
        // so the caller returns the empty completion list (not null, which means "not our domain").
        // The ReplacementRange is unused for None but we provide a valid value to avoid default!.
        context = new(PositionKind.None, sourceText.GetRange(absoluteIndex, absoluteIndex), owner);
        return true;
    }

    /// <summary>
    /// Element name completion: cursor is inside a tag name (e.g., &lt;di|v or &lt;|).
    /// </summary>
    private static bool TryGetElementNamePositionContext(
        RazorSyntaxNode owner, int absoluteIndex, SourceText sourceText, out PositionContext context)
    {
        if (owner is not BaseMarkupEndTagSyntax &&
            HtmlFacts.TryGetElementInfo(owner, out var containingTagNameToken, out _, out _) &&
            containingTagNameToken.Span.IntersectsWith(absoluteIndex))
        {
            var parentTagName = FindEffectiveParentTagName(owner.Parent);
            var elementRange = sourceText.GetRange(containingTagNameToken.Position, containingTagNameToken.Span.End);
            context = new(PositionKind.Element, elementRange, owner, ParentTagName: parentTagName);
            return true;
        }

        context = default;
        return false;
    }

    /// <summary>
    /// Close-tag completion: cursor is inside an end tag (e.g., &lt;/|, &lt;/ta|).
    /// </summary>
    private static bool TryGetCloseTagPositionContext(
        RazorSyntaxNode owner, int absoluteIndex, SourceText sourceText, out PositionContext context)
    {
        if (owner is BaseMarkupEndTagSyntax &&
            HtmlFacts.TryGetElementInfo(owner, out var endTagNameToken, out _, out _) &&
            endTagNameToken.Span.IntersectsWith(absoluteIndex))
        {
            var closeTagRange = sourceText.GetRange(owner.Position, owner.Span.End);
            context = new(PositionKind.CloseTag, closeTagRange, owner);
            return true;
        }

        context = default;
        return false;
    }

    /// <summary>
    /// Attribute name completion: cursor is in an attribute position.
    /// </summary>
    private static bool TryGetAttributeNamePositionContext(
        RazorSyntaxNode owner, int absoluteIndex, SourceText sourceText, out PositionContext context)
    {
        if (HtmlFacts.TryGetAttributeInfo(
                owner,
                out var tagNameToken,
                out var prefixLocation,
                out var selectedAttributeName,
                out var selectedAttributeNameLocation,
                out _) &&
            CompletionContextHelper.IsAttributeNameCompletionContext(
                selectedAttributeName, selectedAttributeNameLocation, prefixLocation, absoluteIndex))
        {
            // When the cursor is before the selected attribute name (e.g., unclosed tag where the
            // parser treats distant content as an attribute), use a zero-length range at the cursor
            // and clear the typed prefix so the client doesn't filter against unrelated text.
            LspRange attrRange;
            string? typedPrefix;

            if (selectedAttributeNameLocation is { } nameLocation
                && absoluteIndex >= nameLocation.Start)
            {
                attrRange = sourceText.GetRange(nameLocation.Start, nameLocation.End);
                typedPrefix = selectedAttributeName;
            }
            else
            {
                attrRange = sourceText.GetRange(absoluteIndex, absoluteIndex);
                typedPrefix = null;
            }

            context = new(PositionKind.Attribute, attrRange, owner, TagName: tagNameToken.Content, TypedAttributePrefix: typedPrefix);
            return true;
        }

        context = default;
        return false;
    }

    /// <summary>
    /// Attribute value completion: cursor is inside an attribute value (between quotes).
    /// </summary>
    private static bool TryGetAttributeValuePositionContext(
        RazorSyntaxNode owner, int absoluteIndex, SourceText sourceText, out PositionContext context)
    {
        if (TryGetAttributeValueContext(owner, absoluteIndex, out var valueTagName, out var valueAttributeName))
        {
            var valueRange = ComputeAttributeValueRange(owner, sourceText, absoluteIndex);
            context = new(PositionKind.AttributeValue, valueRange, owner, TagName: valueTagName, AttributeName: valueAttributeName);
            return true;
        }

        context = default;
        return false;
    }

    /// <summary>
    /// Entity completion: cursor follows '&amp;' in text content (e.g., &amp;amp;$$, &amp;nb$$).
    /// </summary>
    private static bool TryGetEntityPositionContext(
        RazorSyntaxNode owner, int absoluteIndex, SourceText sourceText, out PositionContext context)
    {
        if (EntityCompletion.TryGetReplacementRange(sourceText, absoluteIndex, out var entityRange))
        {
            context = new(PositionKind.Entity, entityRange, owner);
            return true;
        }

        context = default;
        return false;
    }

    private static RazorVSInternalCompletionList? GetElementCompletionList(
        RazorCompletionOptions completionOptions, PositionContext context,
        out LocalHtmlCompletionResolveContext resolveContext)
    {
        resolveContext = LocalHtmlCompletionResolveContext.Empty;

        var commitWithSpace = completionOptions.CommitElementsWithSpace;
        var commitChars = DefaultCommitCharacters.GetElementCommitCharacterStrings(commitWithSpace);

        var elementRange = context.ReplacementRange;
        var owner = context.Owner;

        // Collect ancestor tag names for disallowed-ancestor filtering.
        using var ancestorNames = new PooledArrayBuilder<string>();
        CollectAncestorTagNames(owner, ref ancestorNames.AsRef());

        // If we know the parent element, filter to its allowed children.
        if (context.ParentTagName is string parentTagName)
        {
            if (HtmlCompletionData.GetElement(parentTagName) is HtmlElementInfo parentInfo)
            {
                return GetFilteredElementCompletionList(
                    parentInfo, parentTagName, owner, elementRange, commitChars,
                    ref ancestorNames.AsRef(), out resolveContext);
            }

            // Defensive: FindEffectiveParentTagName currently only returns names that are
            // in the schema, so GetElement above will always succeed. This guard exists in
            // case that invariant changes in the future.
            if (ShouldFallBackForElements(owner))
            {
                return null;
            }
        }

        // Otherwise return all elements.
        var allElements = HtmlCompletionData.AllElements;

        using var items = new PooledArrayBuilder<VSInternalCompletionItem>(allElements.Length);

        AddCloseTagItems(ref items.AsRef(), owner.Parent, elementRange, includeOpenBracket: false, commitChars);

        foreach (var element in allElements)
        {
            if (HasDisallowedAncestor(element, ref ancestorNames.AsRef()))
            {
                continue;
            }

            items.Add(CreateElementItem(element, commitChars, elementRange));
        }

        return new RazorVSInternalCompletionList
        {
            Items = items.ToArray(),
            IsIncomplete = false,
        };
    }

    /// <summary>
    /// Returns a completion list filtered to the allowed children of a known parent element.
    /// Returns null for elements with external content completion (e.g., script, style, svg).
    /// </summary>
    private static RazorVSInternalCompletionList? GetFilteredElementCompletionList(
        HtmlElementInfo parentInfo, string parentTagName, RazorSyntaxNode owner,
        LspRange elementRange, string[] commitChars,
        ref PooledArrayBuilder<string> ancestorNames,
        out LocalHtmlCompletionResolveContext resolveContext)
    {
        resolveContext = LocalHtmlCompletionResolveContext.Empty;

        var allowedChildren = parentInfo.AllowedChildren;

        if (allowedChildren.IsEmpty)
        {
            if (parentInfo.HasExternalCompletion)
            {
                // Element with embedded content (e.g., <script>, <style>, <svg>) —
                // content completion is owned by an external provider.
                return null;
            }

            // Void or text-only element — no child elements possible.
            return s_emptyCompletionList;
        }

        // Build completions from the allowed children list, looking each up
        // in the schema for description/metadata.
        using var filteredItems = new PooledArrayBuilder<VSInternalCompletionItem>(allowedChildren.Length);

        // Add close-tag completions for unclosed ancestor elements. We pass the
        // element node (owner.Parent) rather than the start tag (owner) so that
        // AddCloseTagItems skips the element being typed and only offers close
        // tags for elements above it.
        AddCloseTagItems(ref filteredItems.AsRef(), owner.Parent, elementRange, includeOpenBracket: false, commitChars);

        // If the parent is an implicitly-closable element without an end tag,
        // offer the parent itself as a child (typing a new sibling implicitly closes this one).
        // Only add it if it's not already in the allowed children list (to avoid duplicates).
        // Note: HasEndTag takes the start-tag node and checks its Parent (the element node),
        // so we pass owner.Parent (the element being typed) to check the grandparent element.
        if (parentInfo.IsImplicitlyClosed && !HasEndTag(owner.Parent) &&
            !allowedChildren.Contains(parentTagName, StringComparer.OrdinalIgnoreCase))
        {
            if (!HasDisallowedAncestor(parentInfo, ref ancestorNames))
            {
                filteredItems.Add(CreateElementItem(parentInfo, commitChars, elementRange));
            }
        }

        foreach (var childName in allowedChildren)
        {
            var childInfo = HtmlCompletionData.GetElement(childName);

            if (childInfo is not null && HasDisallowedAncestor(childInfo, ref ancestorNames))
            {
                continue;
            }

            filteredItems.Add(childInfo is not null
                ? CreateElementItem(childInfo, commitChars, elementRange)
                : CreateElementItem(childName, commitChars, elementRange));
        }

        return new RazorVSInternalCompletionList
        {
            Items = filteredItems.ToArray(),
            IsIncomplete = false,
        };
    }

    /// <summary>
    /// Returns a completion list containing only close-tag items for <c>&lt;/</c> contexts.
    /// </summary>
    private static RazorVSInternalCompletionList GetCloseTagCompletionList(RazorCompletionOptions completionOptions, PositionContext context)
    {
        var commitChars = DefaultCommitCharacters.GetElementCommitCharacterStrings(completionOptions.CommitElementsWithSpace);

        using var items = new PooledArrayBuilder<VSInternalCompletionItem>();
        AddCloseTagItems(ref items.AsRef(), context.Owner, context.ReplacementRange, includeOpenBracket: true, commitChars);

        return new RazorVSInternalCompletionList
        {
            Items = items.Count > 0 ? items.ToArray() : [],
            IsIncomplete = false,
        };
    }

    /// <summary>
    /// Adds close-tag completion items (e.g., /table&gt;) for unclosed ancestor elements.
    /// <paramref name="range"/> is the TextEdit range; <paramref name="includeOpenBracket"/> controls
    /// whether the NewText includes the leading '&lt;/' (close-tag context) or just '/' (element context).
    /// Stops at the first ancestor that already has an end tag.
    /// </summary>
    private static void AddCloseTagItems(
        ref PooledArrayBuilder<VSInternalCompletionItem> items,
        RazorSyntaxNode owner,
        LspRange range,
        bool includeOpenBracket,
        string[] commitCharacters)
    {
        HashSet<string>? seen = null;
        var sortIndex = 0;

        // Walk up from the element being typed to find unclosed ancestor elements.
        // Stop as soon as we hit an ancestor that has an end tag.
        for (var node = owner.Parent; node is not null; node = node.Parent)
        {
            var (tagName, hasEndTag) = node is BaseMarkupElementSyntax el
                ? (el.StartTag?.Name.Content, el.EndTag is not null)
                : (null, false);

            if (tagName is null or { Length: 0 })
            {
                continue;
            }

            if (hasEndTag)
            {
                // Stop at the first closed ancestor — anything above it
                // is structurally complete from the cursor's perspective.
                break;
            }

            seen ??= new(StringComparer.OrdinalIgnoreCase);
            if (!seen.Add(tagName))
            {
                continue;
            }

            var filterText = "/" + tagName;
            var labelText = filterText + ">";
            var newText = includeOpenBracket
                ? "<" + labelText   // Replace from '<' inclusive
                : labelText;   // Replace tag name range only

            var item = new VSInternalCompletionItem
            {
                Label = labelText,
                Kind = CompletionItemKind.CloseElement,
                FilterText = filterText,
                InsertTextFormat = InsertTextFormat.Plaintext,
                SortText = $"!{sortIndex++:D2}",
                TextEdit = new TextEdit { Range = range, NewText = newText },
                CommitCharacters = commitCharacters,
            };

            items.Add(item);
        }
    }

    /// <summary>
    /// Returns true if the owner node's parent element has an explicit end tag.
    /// Used to determine if an implicit-closure element still needs its sibling offered.
    /// </summary>
    private static bool HasEndTag(RazorSyntaxNode owner)
    {
        var parent = owner.Parent;
        return parent switch
        {
            MarkupElementSyntax el => el.EndTag is not null,
            MarkupTagHelperElementSyntax th => th.EndTag is not null,
            _ => false,
        };
    }

    /// <summary>
    /// Collects tag names from all ancestor elements (walking up the syntax tree).
    /// Used for disallowed-ancestor filtering.
    /// </summary>
    private static void CollectAncestorTagNames(RazorSyntaxNode owner, ref PooledArrayBuilder<string> names)
    {
        for (var node = owner.Parent; node is not null; node = node.Parent)
        {
            if (node is BaseMarkupElementSyntax markupNode &&
                markupNode.StartTag?.Name.Content is string tagName &&
                tagName.Length > 0)
            {
                names.Add(tagName);
            }
        }
    }

    /// <summary>
    /// Returns true if any ancestor name appears in the element's disallowed-ancestor list.
    /// </summary>
    private static bool HasDisallowedAncestor(HtmlElementInfo element, ref PooledArrayBuilder<string> ancestorNames)
    {
        if (element.DisallowedAncestors.IsEmpty)
        {
            return false;
        }

        foreach (var ancestor in ancestorNames)
        {
            foreach (var disallowed in element.DisallowedAncestors)
            {
                if (string.Equals(ancestor, disallowed, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static RazorVSInternalCompletionList? GetAttributeCompletionList(PositionContext context,
        out LocalHtmlCompletionResolveContext resolveContext)
    {
        var tagName = context.TagName!;
        var typedPrefix = context.TypedAttributePrefix;
        var owner = context.Owner;
        var attrRange = context.ReplacementRange;

        var elementInfo = HtmlCompletionData.GetElement(tagName);

        // Determine which prefix group is expanded (user typed past the dash).
        string? expandedPrefix = null;
        foreach (var (prefix, _, _) in s_attributePrefixGroups)
        {
            if (typedPrefix != null &&
                typedPrefix.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                expandedPrefix = prefix;
                break;
            }
        }

        if (ShouldFallBackForAttributes(elementInfo, expandedPrefix, owner))
        {
            resolveContext = LocalHtmlCompletionResolveContext.Empty;
            return null;
        }

        var globalAttributes = HtmlCompletionData.GlobalAttributes;
        var elementAttributes = elementInfo?.Attributes ?? [];

        // When the cursor is inside an existing full attribute (e.g., dropzone=""), the attribute
        // already has a value portion in the buffer. We should only replace the name — don't add
        // ="$0" since that would duplicate the existing ="" and produce broken markup like
        // draggable=""="". This matches the behavior of the external HTML language server.
        var existingAttributeHasValue = owner is MarkupAttributeBlockSyntax or MarkupTagHelperAttributeSyntax or MarkupTagHelperDirectiveAttributeSyntax;

        using var _ = ListPool<VSInternalCompletionItem>.GetPooledObject(out var items);

        AddAttributeItems(items, elementAttributes, expandedPrefix, existingAttributeHasValue, attrRange);
        AddAttributeItems(items, globalAttributes, expandedPrefix, existingAttributeHasValue, attrRange);

        // Add synthetic group items only when no group is expanded
        if (expandedPrefix == null)
        {
            foreach (var (prefix, kind, label) in s_attributePrefixGroups)
            {
                items.Add(CreatePrefixGroupItem(prefix, label, kind, attrRange));
            }
        }

        resolveContext = new LocalHtmlCompletionResolveContext(elementAttributes, globalAttributes);
        return new RazorVSInternalCompletionList
        {
            Items = [.. items],
            IsIncomplete = false,
        };
    }

    private static bool IsCollapsedPrefixAttribute(HtmlAttributeInfo attr)
        => attr.Kind is HtmlAttributeKind.Aria or HtmlAttributeKind.Angular or HtmlAttributeKind.Data;

    private static void AddAttributeItems(
        List<VSInternalCompletionItem> items,
        ImmutableArray<HtmlAttributeInfo> attributes,
        string? expandedPrefix,
        bool existingAttributeHasValue,
        LspRange range)
    {
        foreach (var attr in attributes)
        {
            // If this attribute belongs to a collapsed prefix group, skip it
            if (expandedPrefix == null && IsCollapsedPrefixAttribute(attr))
            {
                continue;
            }

            // If a group IS expanded, only include items from that group
            if (expandedPrefix != null &&
                !attr.Name.StartsWith(expandedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            items.Add(CreateAttributeItem(attr, existingAttributeHasValue, range));
        }
    }

    private static VSInternalCompletionItem CreateElementItem(HtmlElementInfo info, string[] commitChars, LspRange range)
        => CreateElementItem(
            info.Name,
            commitChars,
            range,
            icon: info.Kind == HtmlElementKind.Angular ? HtmlCompletionImageMonikers.Angular : null);

    private static VSInternalCompletionItem CreateElementItem(string name, string[] commitChars, LspRange range, ImageElement? icon = null)
        => new()
        {
            Label = name,
            Kind = CompletionItemKind.Element,
            CommitCharacters = commitChars,
            Icon = icon,
            FilterText = name,
            TextEdit = new TextEdit { Range = range, NewText = name },
        };

    private static VSInternalCompletionItem CreateAttributeItem(HtmlAttributeInfo attr, bool existingAttributeHasValue, LspRange range)
    {
        var icon = attr.Kind switch
        {
            HtmlAttributeKind.Angular => HtmlCompletionImageMonikers.Angular,
            HtmlAttributeKind.Aria => HtmlCompletionImageMonikers.AriaAttribute,
            _ => null,
        };

        // When replacing an existing attribute that already has ="value", only insert the name.
        // Otherwise, add ="$0" snippet to let the user type a value immediately.
        var useSnippet = !attr.IsBoolean && !existingAttributeHasValue;
        var newText = useSnippet ? $"{attr.Name}=\"$0\"" : attr.Name;

        return new VSInternalCompletionItem
        {
            Label = attr.Name,
            Kind = attr.Kind == HtmlAttributeKind.Event ? CompletionItemKind.Event : CompletionItemKind.Property,
            Icon = icon,
            FilterText = attr.Name,
            InsertTextFormat = useSnippet ? InsertTextFormat.Snippet : InsertTextFormat.Plaintext,
            VsCommitCharacters = attr.IsBoolean ? s_minimizedAttributeCommitCharacters : s_attributeCommitCharacters,
            Command = useSnippet ? s_retriggerCompletionCommand : null,
            TextEdit = new TextEdit { Range = range, NewText = newText },
        };
    }

    private static VSInternalCompletionItem CreateAttributeValueItem(string value, LspRange range)
        => new()
        {
            Label = value,
            Kind = CompletionItemKind.Value,
            FilterText = value,
            TextEdit = new TextEdit { Range = range, NewText = value },
        };

    private static VSInternalCompletionItem CreatePrefixGroupItem(string prefix, string label, HtmlAttributeKind kind, LspRange range)
    {
        var icon = kind switch
        {
            HtmlAttributeKind.Aria => HtmlCompletionImageMonikers.AriaAttribute,
            HtmlAttributeKind.Angular => HtmlCompletionImageMonikers.Angular,
            HtmlAttributeKind.Data => HtmlCompletionImageMonikers.DataAttribute,
            _ => null,
        };

        return new VSInternalCompletionItem
        {
            Label = label,
            Kind = CompletionItemKind.Property,
            FilterText = prefix,
            InsertTextFormat = InsertTextFormat.Plaintext,
            Icon = icon,
            VsCommitCharacters = s_prefixGroupCommitCharacters,
            Command = s_retriggerCompletionCommand,
            TextEdit = new TextEdit { Range = range, NewText = prefix },
        };
    }

    /// <summary>
    /// Creates a <see cref="MarkupContent"/> documentation tooltip from a description and/or URL.
    /// </summary>
    internal static MarkupContent CreateDocumentation(string? description, string? documentationUrl)
    {
        var value = description;

        if (documentationUrl is { Length: > 0 })
        {
            value = description is { Length: > 0 }
                ? $"{description}\n\n[HTML reference]({documentationUrl})"
                : $"[HTML reference]({documentationUrl})";
        }

        return new MarkupContent
        {
            Kind = MarkupKind.Markdown,
            Value = value ?? "",
        };
    }

    private static RazorVSInternalCompletionList? GetAttributeValueCompletionList(PositionContext context)
    {
        var tagName = context.TagName!;
        var attributeName = context.AttributeName!;
        var valueRange = context.ReplacementRange;

        var elementInfo = HtmlCompletionData.GetElement(tagName);

        var attr = elementInfo is not null ? FindAttribute(elementInfo.Attributes, attributeName) : null;
        attr ??= FindAttribute(HtmlCompletionData.GlobalAttributes, attributeName);

        if (attr is null)
        {
            return ShouldFallBackForAttributeValues(elementInfo, attributeName, context.Owner)
                ? null
                : s_emptyCompletionList;
        }

        // Attributes whose value completion is owned by an external provider (file paths, multivalue, CSS classes/style)
        if (attr.HasExternalCompletion)
        {
            return null;
        }

        // We're authoritative for this attribute's values.
        // If it has enumerated values, return them; otherwise return an empty (but authoritative) list.
        if (attr.Values.IsEmpty)
        {
            return s_emptyCompletionList;
        }

        var items = new VSInternalCompletionItem[attr.Values.Length];

        for (var i = 0; i < items.Length; i++)
        {
            items[i] = CreateAttributeValueItem(attr.Values[i], valueRange);
        }

        return new RazorVSInternalCompletionList
        {
            Items = items,
            IsIncomplete = false,
        };
    }

    private static HtmlAttributeInfo? FindAttribute(
        ImmutableArray<HtmlAttributeInfo> attributes, string attributeName)
    {
        foreach (var attr in attributes)
        {
            if (string.Equals(attr.Name, attributeName, System.StringComparison.OrdinalIgnoreCase))
            {
                return attr;
            }
        }

        return null;
    }

    private static bool TryGetAttributeValueContext(
        RazorSyntaxNode owner,
        int absoluteIndex,
        out string tagName,
        out string attributeName)
    {
        tagName = string.Empty;
        attributeName = string.Empty;

        // Walk up to find if we're inside an attribute value
        for (var node = owner; node != null; node = node.Parent)
        {
            switch (node)
            {
                case MarkupAttributeBlockSyntax attrBlock:
                    // We're in a regular HTML attribute — check if cursor is in the value portion.
                    // For empty values (e.g., class=""), Value is null so also check if the cursor
                    // is between the opening and closing quote tokens.
                    var inAttrValue = attrBlock.Value?.Span.IntersectsWith(absoluteIndex) == true;

                    if (!inAttrValue &&
                        attrBlock.ValuePrefix is { } valuePrefix &&
                        attrBlock.ValueSuffix is { } valueSuffix &&
                        absoluteIndex >= valuePrefix.Span.End &&
                        absoluteIndex <= valueSuffix.Span.Start)
                    {
                        inAttrValue = true;
                    }

                    if (inAttrValue && attrBlock.Parent is BaseMarkupStartTagSyntax parentTag)
                    {
                        tagName = parentTag.Name.Content;
                        attributeName = attrBlock.Name.GetContent();
                        return true;
                    }

                    return false;

                case MarkupTagHelperAttributeSyntax thAttr:
                    // Tag helper attribute — value is in a different structure
                    if (thAttr.Value?.Span.IntersectsWith(absoluteIndex) == true &&
                        thAttr.Parent is MarkupTagHelperStartTagSyntax thParentTag)
                    {
                        tagName = thParentTag.Name.Content;
                        attributeName = thAttr.Name.GetContent();
                        return true;
                    }

                    return false;

                case BaseMarkupStartTagSyntax:
                case BaseMarkupEndTagSyntax:
                    // We hit a tag boundary without finding an attribute value — not in value context
                    return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Computes the LSP range covering the attribute value text between quotes.
    /// For class="fo", returns range of "fo". For class="", returns zero-length between quotes.
    /// </summary>
    private static LspRange ComputeAttributeValueRange(RazorSyntaxNode owner, SourceText sourceText, int absoluteIndex)
    {
        for (var node = owner; node != null; node = node.Parent)
        {
            if (node is MarkupAttributeBlockSyntax attrBlock)
            {
                // Range is between the quotes (ValuePrefix and ValueSuffix).
                var start = attrBlock.ValuePrefix is { } vp ? vp.Span.End : absoluteIndex;
                var end = attrBlock.ValueSuffix is { } vs ? vs.Span.Start : absoluteIndex;

                if (attrBlock.Value is { } value)
                {
                    // Use the actual value span which may be shorter than quote-to-quote
                    start = value.Span.Start;
                    end = value.Span.End;
                }

                return sourceText.GetRange(start, end);
            }
        }

        return sourceText.GetRange(absoluteIndex, absoluteIndex);
    }

    /// <summary>
    /// Walks up the syntax tree from the element being typed to find the effective parent
    /// element name, skipping through transparent-content elements (e.g., &lt;a&gt;, &lt;noscript&gt;)
    /// that inherit their parent's content model.
    /// Returns null if at document root or inside an unknown element.
    /// </summary>
    private static string? FindEffectiveParentTagName(RazorSyntaxNode? elementBeingTyped)
    {
        for (var node = elementBeingTyped?.Parent; node != null; node = node.Parent)
        {
            if (node is not BaseMarkupElementSyntax markupNode ||
                markupNode.StartTag?.Name.Content is not string tagName)
            {
                continue;
            }

            var info = HtmlCompletionData.GetElement(tagName);
            if (info is null)
            {
                // Unknown element (custom element, Razor component) — can't constrain.
                return null;
            }

            if (info.IsTransparentContent)
            {
                // Transparent content model — keep walking to the real parent.
                continue;
            }

            return tagName;
        }

        return null;
    }

    private static bool IsInScriptOrStyleBlock(RazorSyntaxNode node)
    {
        for (var current = node; current != null; current = current.Parent)
        {
            if (current is BaseMarkupElementSyntax element &&
                RazorSyntaxFacts.IsScriptOrStyleBlock(element))
            {
                return true;
            }
        }

        return false;
    }
}
