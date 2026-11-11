// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;
using RazorSyntaxToken = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxToken;

namespace Microsoft.CodeAnalysis.Remote.Razor.Rename;

internal static class ComponentAliasRenameHelper
{
    public static ImmutableArray<RazorTextChange> AddSameNamedAliasEdits(
        RazorCodeDocument codeDocument,
        ImmutableArray<RazorTextChange> edits,
        bool includeSameNamedAliasTargetEdits,
        CancellationToken cancellationToken)
    {
        if (edits.IsDefaultOrEmpty || codeDocument.GetTagHelperContext()?.HasAliases != true)
        {
            return edits;
        }

        var sourceText = codeDocument.Source.Text;
        using var builder = new PooledArrayBuilder<RazorTextChange>(edits.Length + 1);
        builder.AddRange(edits);

        foreach (var edit in edits)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var changedSpan = edit.Span.ToTextSpan();
            if (TryGetTypeAliasFromComponentTag(codeDocument, changedSpan.Start, out var component, out var aliasSpan) ||
                (changedSpan.IsEmpty &&
                 changedSpan.Start > 0 &&
                 TryGetTypeAliasFromComponentTag(codeDocument, changedSpan.Start - 1, out component, out aliasSpan)))
            {
                if (changedSpan.Start >= aliasSpan.Start &&
                    changedSpan.End <= aliasSpan.End &&
                    sourceText.ToString(aliasSpan) == component.TypeNameIdentifier)
                {
                    var originalName = component.TypeNameIdentifier;
                    var newName = sourceText.ToString(TextSpan.FromBounds(aliasSpan.Start, changedSpan.Start)) +
                        edit.NewText +
                        sourceText.ToString(TextSpan.FromBounds(changedSpan.End, aliasSpan.End));
                    if (newName != originalName &&
                        TryGetAliasDeclarationToken(codeDocument, component, originalName, out var aliasToken))
                    {
                        // The tag maps to the component type, not its same-named local alias, so keep the alias declaration in sync here.
                        var aliasStart = aliasToken.SpanStart;
                        if (aliasToken.Content[0] == '@')
                        {
                            aliasStart++;
                        }

                        var declarationSpan = new TextSpan(aliasStart, originalName.Length);
                        var declarationOverlapsEdit = false;
                        for (var i = 0; i < builder.Count; i++)
                        {
                            var existing = builder[i];
                            var existingSpan = existing.Span.ToTextSpan();
                            if (!existingSpan.OverlapsWith(declarationSpan) &&
                                !(existingSpan.IsEmpty && existingSpan.Start > declarationSpan.Start && existingSpan.Start < declarationSpan.End))
                            {
                                continue;
                            }

                            declarationOverlapsEdit = true;
                            if (existingSpan.Contains(declarationSpan) &&
                                existingSpan.Length > declarationSpan.Length &&
                                TryRenameAliasInUsingEdit(existing.NewText ?? "", codeDocument, originalName, newName, cancellationToken, out var updatedUsingText))
                            {
                                builder.AsRef()[i] = new RazorTextChange
                                {
                                    Span = existing.Span,
                                    NewText = updatedUsingText
                                };
                            }
                        }

                        if (!declarationOverlapsEdit)
                        {
                            builder.Add(new RazorTextChange
                            {
                                Span = new RazorTextSpan { Start = declarationSpan.Start, Length = declarationSpan.Length },
                                NewText = newName
                            });
                        }

                        break;
                    }
                }
            }

            // C# renames change the alias target, so keep its same-named Razor declaration and tags in sync.
            if (includeSameNamedAliasTargetEdits &&
                AddSameNamedAliasTargetEdits(codeDocument, changedSpan, edit, ref builder.AsRef(), cancellationToken))
            {
                break;
            }
        }

        var orderedEdits = builder.ToImmutableOrderedByAndClear(static edit => edit.Span.Start);
        using var normalizedEdits = new PooledArrayBuilder<RazorTextChange>(orderedEdits.Length);
        foreach (var edit in orderedEdits)
        {
            if (normalizedEdits.Count > 0 && normalizedEdits[^1].Span == edit.Span)
            {
                normalizedEdits.AsRef()[^1] = edit;
            }
            else
            {
                normalizedEdits.Add(edit);
            }
        }

        return normalizedEdits.ToImmutableAndClear();
    }

    public static void AddSameNamedAliasEdits(
        RazorCodeDocument codeDocument,
        DocumentUri documentUri,
        WorkspaceEdit workspaceEdit,
        bool includeSameNamedAliasTargetEdits,
        CancellationToken cancellationToken)
    {
        var sourceText = codeDocument.Source.Text;
        using var documentEdits = new PooledArrayBuilder<TextDocumentEdit>();
        foreach (var documentEdit in workspaceEdit.EnumerateTextDocumentEdits())
        {
            if (documentEdit.TextDocument.DocumentUri == documentUri)
            {
                documentEdits.Add(documentEdit);
            }
        }

        if (documentEdits.Count == 0)
        {
            return;
        }

        var totalEditCount = 0;
        foreach (var documentEdit in documentEdits)
        {
            totalEditCount += documentEdit.Edits.Length;
        }

        using var editsBuilder = new PooledArrayBuilder<RazorTextChange>(totalEditCount);
        foreach (var documentEdit in documentEdits)
        {
            foreach (var edit in documentEdit.Edits)
            {
                editsBuilder.Add(sourceText.GetRazorTextChange((TextEdit)edit));
            }
        }

        var editsWithAliases = AddSameNamedAliasEdits(
            codeDocument,
            editsBuilder.ToImmutableAndClear(),
            includeSameNamedAliasTargetEdits,
            cancellationToken);
        var updatedEdits = new SumType<TextEdit, AnnotatedTextEdit>[editsWithAliases.Length];
        for (var i = 0; i < editsWithAliases.Length; i++)
        {
            updatedEdits[i] = sourceText.GetTextEdit(editsWithAliases[i]);
        }

        documentEdits[0].Edits = updatedEdits;
        for (var i = 1; i < documentEdits.Count; i++)
        {
            documentEdits[i].Edits = [];
        }
    }

    private static bool AddSameNamedAliasTargetEdits(
        RazorCodeDocument codeDocument,
        TextSpan changedSpan,
        RazorTextChange edit,
        ref PooledArrayBuilder<RazorTextChange> edits,
        CancellationToken cancellationToken)
    {
        var root = codeDocument.GetRequiredSyntaxRoot();
        var sourceText = codeDocument.Source.Text;
        foreach (var node in root.DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node is not RazorUsingDirectiveSyntax directive ||
                (!directive.Span.OverlapsWith(changedSpan) &&
                 !directive.Span.Contains(changedSpan) &&
                 !changedSpan.Contains(directive.Span)))
            {
                continue;
            }

            foreach (var child in directive.DescendantNodes())
            {
                if (!TryGetImportAliasTokens(child, out var import, out var tokens))
                {
                    continue;
                }

                var equalsIndex = import.ParsedNamespace.IndexOf('=');
                if (equalsIndex <= 0)
                {
                    continue;
                }

                var aliasName = import.ParsedNamespace.AsSpan(0, equalsIndex).Trim();
                if (!aliasName.IsEmpty && aliasName[0] == '@')
                {
                    aliasName = aliasName[1..];
                }

                if (aliasName.IsEmpty)
                {
                    continue;
                }

                RazorSyntaxToken? declarationToken = null;
                RazorSyntaxToken? componentToken = null;
                foreach (var token in tokens)
                {
                    if (token.Kind is not (SyntaxKind.Identifier or SyntaxKind.Keyword) ||
                        token.SpanStart <= directive.SpanStart + 1)
                    {
                        continue;
                    }

                    if (IsAliasToken(token, aliasName))
                    {
                        declarationToken ??= token;
                        if (componentToken is null || token.SpanStart > componentToken.Value.SpanStart)
                        {
                            componentToken = token;
                        }
                    }
                }

                if (declarationToken is not { } aliasDeclaration ||
                    componentToken is not { } targetToken ||
                    aliasDeclaration == targetToken ||
                    !IsAliasToken(targetToken, aliasName))
                {
                    continue;
                }

                var originalName = aliasName.ToString();
                var targetSpan = targetToken.Span;
                string newName;
                if (changedSpan.Start >= targetSpan.Start && changedSpan.End <= targetSpan.End)
                {
                    newName = sourceText.ToString(TextSpan.FromBounds(targetSpan.Start, changedSpan.Start)) +
                        edit.NewText +
                        sourceText.ToString(TextSpan.FromBounds(changedSpan.End, targetSpan.End));
                }
                else if (!TryGetAliasTargetNameInUsingEdit(edit.NewText ?? "", codeDocument, originalName, cancellationToken, out newName))
                {
                    continue;
                }

                if (newName == originalName)
                {
                    return true;
                }

                foreach (var contribution in codeDocument.GetDirectiveTagHelperContributions())
                {
                    if (contribution.DirectiveSpanStart != directive.SpanStart)
                    {
                        continue;
                    }

                    var component = false;
                    foreach (var descriptor in contribution.ContributedTagHelpers)
                    {
                        if (descriptor.Kind == TagHelperKind.Component &&
                            descriptor.TypeNameIdentifier == originalName)
                        {
                            component = true;
                            break;
                        }
                    }

                    if (!component)
                    {
                        continue;
                    }

                    var prefix = codeDocument.GetTagHelperContext()?.Prefix ?? "";
                    var hasAliasTagReferences = false;
                    foreach (var elementNode in root.DescendantNodes())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (elementNode is not MarkupTagHelperElementSyntax element)
                        {
                            continue;
                        }

                        foreach (var boundRule in element.TagHelperInfo.BindingResult.AllBoundRules)
                        {
                            var descriptor = boundRule.Descriptor;
                            if (descriptor.Kind != TagHelperKind.Component || !contribution.ContributedTagHelpers.Contains(descriptor))
                            {
                                continue;
                            }

                            var comparison = descriptor.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                            hasAliasTagReferences |= IsAliasTag(element.StartTag?.Name, prefix, originalName, comparison) ||
                                IsAliasTag(element.EndTag?.Name, prefix, originalName, comparison);
                            AddAliasTagEdit(element.StartTag?.Name, prefix, originalName, newName, comparison, codeDocument, cancellationToken, ref edits);
                            AddAliasTagEdit(element.EndTag?.Name, prefix, originalName, newName, comparison, codeDocument, cancellationToken, ref edits);
                            break;
                        }
                    }

                    // A matching component tag is what makes this a Razor alias rename; C# references alone should not rename an otherwise-unused alias.
                    if (!hasAliasTagReferences)
                    {
                        continue;
                    }

                    var aliasStart = aliasDeclaration.SpanStart + (aliasDeclaration.Content.StartsWith("@", StringComparison.Ordinal) ? 1 : 0);
                    AddAliasEdit(
                        ref edits,
                        new RazorTextChange
                        {
                            Span = new RazorTextSpan { Start = aliasStart, Length = originalName.Length },
                            NewText = newName
                        },
                        codeDocument,
                        originalName,
                        newName,
                        isAliasDeclaration: true,
                        cancellationToken);

                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetAliasTargetNameInUsingEdit(
        string newText,
        RazorCodeDocument codeDocument,
        string aliasName,
        CancellationToken cancellationToken,
        out string newName)
    {
        var source = RazorSourceDocument.Create(newText, codeDocument.Source.FilePath ?? "");
        var root = RazorSyntaxTree.Parse(source, codeDocument.ParserOptions, cancellationToken).Root;
        foreach (var node in root.DescendantNodes())
        {
            if (!TryGetMatchingImportAliasTokens(node, aliasName, out var tokens))
            {
                continue;
            }

            var aliasTokenSeen = false;
            RazorSyntaxToken? targetToken = null;
            foreach (var token in tokens)
            {
                if (!aliasTokenSeen)
                {
                    aliasTokenSeen = IsAliasToken(token, aliasName);
                    continue;
                }

                if (token.Kind is SyntaxKind.Identifier or SyntaxKind.Keyword)
                {
                    targetToken = token;
                }
            }

            if (targetToken is { } target)
            {
                newName = target.Content.StartsWith("@", StringComparison.Ordinal) ? target.Content[1..] : target.Content;
                return true;
            }
        }

        newName = "";
        return false;
    }

    private static bool IsAliasTag(
        RazorSyntaxToken? name,
        string prefix,
        string aliasName,
        StringComparison comparison)
    {
        if (name is not { } token)
        {
            return false;
        }

        var tagName = token.Content.AsSpan();
        if (prefix.Length > 0 && tagName.StartsWith(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            tagName = tagName[prefix.Length..];
        }

        return tagName.Equals(aliasName.AsSpan(), comparison);
    }

    private static void AddAliasEdit(
        ref PooledArrayBuilder<RazorTextChange> edits,
        RazorTextChange edit,
        RazorCodeDocument codeDocument,
        string originalName,
        string newName,
        bool isAliasDeclaration,
        CancellationToken cancellationToken)
    {
        var editSpan = edit.Span.ToTextSpan();
        for (var i = 0; i < edits.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existing = edits[i];
            var existingSpan = existing.Span.ToTextSpan();
            if (!existingSpan.OverlapsWith(editSpan) &&
                !(existingSpan.IsEmpty && existingSpan.Start > editSpan.Start && existingSpan.Start < editSpan.End))
            {
                continue;
            }

            if (existingSpan == editSpan)
            {
                if (existing.NewText != edit.NewText)
                {
                    edits[i] = edit;
                }

                return;
            }

            if (isAliasDeclaration &&
                existingSpan.Contains(editSpan) &&
                existingSpan.Length > editSpan.Length &&
                TryRenameAliasInUsingEdit(existing.NewText ?? "", codeDocument, originalName, newName, cancellationToken, out var updatedUsingText))
            {
                edits[i] = new RazorTextChange
                {
                    Span = existing.Span,
                    NewText = updatedUsingText
                };

                // The whole using-block replacement now includes the alias rename, so remove any
                // smaller edits inside it rather than returning an overlapping workspace edit.
                for (var nestedIndex = edits.Count - 1; nestedIndex >= 0; nestedIndex--)
                {
                    if (nestedIndex != i && existingSpan.Contains(edits[nestedIndex].Span.ToTextSpan()))
                    {
                        edits.RemoveAt(nestedIndex);
                    }
                }
            }

            return;
        }

        edits.Add(edit);
    }

    private static void AddAliasTagEdit(
        RazorSyntaxToken? name,
        string prefix,
        string aliasName,
        string newName,
        StringComparison comparison,
        RazorCodeDocument codeDocument,
        CancellationToken cancellationToken,
        ref PooledArrayBuilder<RazorTextChange> edits)
    {
        if (name is not { } token)
        {
            return;
        }

        var tagName = token.Content.AsSpan();
        var aliasStart = token.SpanStart;
        if (prefix.Length > 0 && tagName.StartsWith(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            tagName = tagName[prefix.Length..];
            aliasStart += prefix.Length;
        }

        if (tagName.Equals(aliasName.AsSpan(), comparison))
        {
            AddAliasEdit(
                ref edits,
                new RazorTextChange
                {
                    Span = new RazorTextSpan { Start = aliasStart, Length = aliasName.Length },
                    NewText = newName
                },
                codeDocument,
                aliasName,
                newName,
                isAliasDeclaration: false,
                cancellationToken);
        }
    }

    public static bool TryGetTypeAliasFromComponentTag(
        RazorCodeDocument codeDocument,
        int hostDocumentIndex,
        [NotNullWhen(true)] out TagHelperDescriptor? component,
        out TextSpan aliasSpan)
    {
        component = null;
        aliasSpan = default;

        var element = codeDocument.GetRequiredSyntaxRoot().FindInnermostNode(hostDocumentIndex)?.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>();
        if (element is null)
        {
            return false;
        }

        RazorSyntaxToken name;
        if (element.StartTag is { } startTag && startTag.Name.Span.IntersectsWith(hostDocumentIndex))
        {
            name = startTag.Name;
        }
        else if (element.EndTag is { } endTag && endTag.Name.Span.IntersectsWith(hostDocumentIndex))
        {
            name = endTag.Name;
        }
        else
        {
            return false;
        }

        var tagHelperContext = codeDocument.GetTagHelperContext();
        if (name.Content.Contains('.') ||
            tagHelperContext?.IsTypeAliasTagName(name.Content) != true)
        {
            return false;
        }

        aliasSpan = name.Span;
        var prefix = tagHelperContext.Prefix;
        if (prefix is { Length: > 0 } && name.Content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            aliasSpan = new TextSpan(name.SpanStart + prefix.Length, name.Span.Length - prefix.Length);
        }

        foreach (var boundRule in element.TagHelperInfo.BindingResult.AllBoundRules)
        {
            if (boundRule.Descriptor.Kind == TagHelperKind.Component)
            {
                component = boundRule.Descriptor;
                return true;
            }
        }

        return false;
    }

    public static bool TryGetAliasDeclarationToken(
        RazorCodeDocument codeDocument,
        TagHelperDescriptor component,
        string aliasName,
        out RazorSyntaxToken aliasToken)
    {
        aliasToken = default;

        var root = codeDocument.GetRequiredSyntaxRoot();
        foreach (var contribution in codeDocument.GetDirectiveTagHelperContributions())
        {
            if (!contribution.ContributedTagHelpers.Contains(component) ||
                root.FindInnermostNode(contribution.DirectiveSpanStart)?.FirstAncestorOrSelf<RazorUsingDirectiveSyntax>() is not { } directive)
            {
                continue;
            }

            foreach (var child in directive.DescendantNodes())
            {
                if (!TryGetMatchingImportAliasTokens(child, aliasName, out var tokens))
                {
                    continue;
                }

                foreach (var token in tokens)
                {
                    if (IsAliasToken(token, aliasName))
                    {
                        aliasToken = token;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static bool TryGetMatchingImportAliasTokens(
        Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode node,
        string aliasName,
        out Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxTokenList tokens)
    {
        if (!TryGetImportAliasTokens(node, out var import, out tokens))
        {
            return false;
        }

        var equalsIndex = import.ParsedNamespace.IndexOf('=');
        if (equalsIndex <= 0)
        {
            return false;
        }

        var directiveAlias = import.ParsedNamespace.AsSpan(0, equalsIndex).Trim();
        if (!directiveAlias.IsEmpty && directiveAlias[0] == '@')
        {
            directiveAlias = directiveAlias[1..];
        }

        return directiveAlias.Equals(aliasName.AsSpan(), StringComparison.Ordinal);
    }

    public static bool IsAliasToken(RazorSyntaxToken token, string aliasName)
        => IsAliasToken(token, aliasName.AsSpan());

    private static bool IsAliasToken(RazorSyntaxToken token, ReadOnlySpan<char> aliasName)
    {
        if (token.Kind is not (SyntaxKind.Identifier or SyntaxKind.Keyword))
        {
            return false;
        }

        var tokenContent = token.Content.AsSpan();
        if (!tokenContent.IsEmpty && tokenContent[0] == '@')
        {
            tokenContent = tokenContent[1..];
        }

        return tokenContent.Equals(aliasName, StringComparison.Ordinal);
    }

    public static bool TryGetImportAliasTokens(
        Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode node,
        [NotNullWhen(true)] out AddImportChunkGenerator? import,
        out Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxTokenList tokens)
    {
        switch (node)
        {
            case CSharpStatementLiteralSyntax { ChunkGenerator: AddImportChunkGenerator statementImport } statement:
                import = statementImport;
                tokens = statement.LiteralTokens;
                return true;

            case RazorMetaCodeSyntax { ChunkGenerator: AddImportChunkGenerator metaCodeImport } metaCode:
                import = metaCodeImport;
                tokens = metaCode.MetaCode;
                return true;

            default:
                import = null;
                tokens = default;
                return false;
        }
    }

    private static bool TryRenameAliasInUsingEdit(
        string newText,
        RazorCodeDocument codeDocument,
        string originalName,
        string newName,
        CancellationToken cancellationToken,
        out string updatedText)
    {
        var source = RazorSourceDocument.Create(newText, codeDocument.Source.FilePath ?? "");
        var root = RazorSyntaxTree.Parse(source, codeDocument.ParserOptions, cancellationToken).Root;
        foreach (var node in root.DescendantNodes())
        {
            if (node is not RazorUsingDirectiveSyntax directive ||
                FindAliasToken(directive, originalName) is not { } token)
            {
                if (node is RazorUsingDirectiveSyntax renamedDirective &&
                    FindAliasToken(renamedDirective, newName) is not null)
                {
                    updatedText = newText;
                    return true;
                }

                continue;
            }

            var aliasStart = token.SpanStart;
            if (token.Content[0] == '@')
            {
                aliasStart++;
            }

            updatedText = newText[..aliasStart] + newName + newText[(aliasStart + originalName.Length)..];
            return true;
        }

        updatedText = newText;
        return false;
    }

    private static RazorSyntaxToken? FindAliasToken(RazorUsingDirectiveSyntax directive, string aliasName)
    {
        foreach (var child in directive.DescendantNodes())
        {
            if (!TryGetMatchingImportAliasTokens(child, aliasName, out var tokens))
            {
                continue;
            }

            foreach (var token in tokens)
            {
                if (token.SpanStart > directive.SpanStart + 1 && IsAliasToken(token, aliasName))
                {
                    return token;
                }
            }
        }

        return null;
    }
}
