// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Remote.Razor.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.NET.Sdk.Razor.SourceGenerators;
using RazorSyntaxToken = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxToken;
using static Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Roslyn.LanguageServer.Protocol.WorkspaceEdit?>;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteRenameService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteRenameService
{
    internal sealed class Factory : FactoryBase<IRemoteRenameService>
    {
        protected override IRemoteRenameService CreateService(in ServiceArgs args)
            => new RemoteRenameService(in args);
    }

    private readonly IRenameService _renameService = args.ExportProvider.GetExportedValue<IRenameService>();
    private readonly IRazorEditService _razorEditService = args.ExportProvider.GetExportedValue<IRazorEditService>();

    protected override IDocumentPositionInfoStrategy DocumentPositionInfoStrategy => PreferAttributeNameDocumentPositionInfoStrategy.Instance;

    public ValueTask<RemoteResponse<WorkspaceEdit?>> GetRenameEditAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        Position position,
        string newName,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            snapshot => GetRenameEditAsync(snapshot, position, newName, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<WorkspaceEdit?>> GetRenameEditAsync(
        RemoteDocumentSnapshot snapshot,
        Position position,
        string newName,
        CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var hostDocumentIndex = codeDocument.Source.Text.GetRequiredAbsoluteIndex(position);
        var originalHostDocumentIndex = hostDocumentIndex;
        hostDocumentIndex = codeDocument.AdjustPositionForComponentEndTag(hostDocumentIndex);

        var positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: true);
        if (TryGetAliasPositionInfoFromUsingDirective(codeDocument, hostDocumentIndex, out var declarationPositionInfo))
        {
            positionInfo = declarationPositionInfo;
        }

        if (TryGetTypeAliasFromComponentTag(codeDocument, originalHostDocumentIndex, out var typeAliasComponent, out var typeAliasSpan))
        {
            if (!TryGetComponentAliasPositionInfo(codeDocument, typeAliasComponent, typeAliasSpan, out var aliasPositionInfo))
            {
                return NoFurtherHandling;
            }

            positionInfo = aliasPositionInfo;
        }
        else if (TryGetNamespaceAliasFromComponentTag(codeDocument, originalHostDocumentIndex, out var aliasComponent, out var aliasSpan))
        {
            // An intermediate namespace segment isn't a renameable symbol.
            if (originalHostDocumentIndex < aliasSpan.Start || originalHostDocumentIndex > aliasSpan.End)
            {
                return NoFurtherHandling;
            }

            if (!TryGetComponentAliasPositionInfo(codeDocument, aliasComponent, aliasSpan, out var aliasPositionInfo))
            {
                return NoFurtherHandling;
            }

            positionInfo = aliasPositionInfo;
        }

        var generatedDocument = await snapshot
            .GetGeneratedDocumentAsync(positionInfo.InDeclDocument, cancellationToken)
            .ConfigureAwait(false);

        var razorEdit = await _renameService
            .TryGetRazorRenameEditsAsync(snapshot, positionInfo, newName, snapshot.ProjectSnapshot.SolutionSnapshot, cancellationToken)
            .ConfigureAwait(false);

        if (razorEdit.Edit is null && positionInfo.LanguageKind != CodeAnalysis.Razor.Protocol.RazorLanguageKind.CSharp)
        {
            return CallHtml;
        }

        if (razorEdit.Edit is null && !razorEdit.FallbackToCSharp)
        {
            return NoFurtherHandling;
        }

        var csharpEdit = await RenameHandler.GetRenameEditAsync(generatedDocument, positionInfo.Position.ToLinePosition(), newName, allowRenamesInRazorSourceGeneratedDocuments: true, cancellationToken)
            .ConfigureAwait(false);

        if (csharpEdit is null)
        {
            return NoFurtherHandling;
        }

        // There is a special case here for using aliases. In C#, these are document-local and so a
        // single Razor @using alias is emitted into both impl and decl documents, creating separate
        // alias symbols. Roslyn renames only the selected symbol, so references in the other document
        // would be left unchanged even though they share the same Razor source declaration. We need to
        // do a second rename for the corresponding alias in the other document, so their edits can be
        // combined before mapping.
        TextDocumentEdit? aliasTagEdit = null;
        var alias = await GetAliasSymbolAsync(generatedDocument, positionInfo.Position, cancellationToken).ConfigureAwait(false);
        if (alias is not null)
        {
            // Repeat the local-declaration check for clients that don't support prepareRename.
            // An imported or global alias must not be renamed from this consuming document.
            if (await TryGetLocalAliasDeclarationSpanAsync(alias, generatedDocument, codeDocument, positionInfo.InDeclDocument, cancellationToken).ConfigureAwait(false) is not { } aliasDeclarationSpan)
            {
                return NoFurtherHandling;
            }

            var otherEdit = await TryGetOtherAliasRenameEditAsync(snapshot, codeDocument, generatedDocument, positionInfo, newName, cancellationToken).ConfigureAwait(false);
            csharpEdit = csharpEdit.Concat(otherEdit);

            // With @using UI = My.Components, <UI.Button /> is still emitted to C# with My.Components.Button,
            // so if there's no reference in the C# code to the alias for Roslyn to rename, it will return 0 edits.
            // In that case, we need to handle editing the tag ourselves.
            if (alias.Target is INamespaceSymbol)
            {
                var needsAliasDeclarationEdit = !csharpEdit.EnumerateTextDocumentEdits().Any(static edit => edit.Edits.Length > 0);
                aliasTagEdit = GetNamespaceAliasTagEdits(snapshot, codeDocument, aliasDeclarationSpan, alias.Name, newName, needsAliasDeclarationEdit, cancellationToken);
            }
            else if (alias.Target is INamedTypeSymbol)
            {
                aliasTagEdit = GetTypeAliasTagEdits(snapshot, codeDocument, aliasDeclarationSpan, alias.Name, newName, cancellationToken);
            }
        }

        await _razorEditService.MapWorkspaceEditAsync(snapshot.TextDocument.Project.Solution, csharpEdit, cancellationToken).ConfigureAwait(false);

        // A rename initiated on a component alias changes the alias symbol, not the component type or file.
        var result = alias is null ? csharpEdit.Concat(razorEdit.Edit) : csharpEdit;
        if (aliasTagEdit is not null)
        {
            // Both sets of edits use the original Razor text. Keep them in one document edit so
            // an earlier rename on the same line cannot shift the positions of the tag edits.
            var documentEdit = result.EnumerateTextDocumentEdits().FirstOrDefault(edit => edit.TextDocument.DocumentUri == snapshot.Uri);
            if (documentEdit is not null)
            {
                documentEdit.Edits = [.. documentEdit.Edits, .. aliasTagEdit.Edits];
            }
            else
            {
                result = result.Concat(new WorkspaceEdit { DocumentChanges = new[] { aliasTagEdit } });
            }
        }

        return Results(result);
    }

    private static async Task<IAliasSymbol?> GetAliasSymbolAsync(Document generatedDocument, Position position, CancellationToken cancellationToken)
    {
        var generatedText = await generatedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var renameInfo = await SymbolicRenameInfo.GetRenameInfoAsync(generatedDocument, generatedText.GetRequiredAbsoluteIndex(position), cancellationToken).ConfigureAwait(false);
        return renameInfo.Symbol as IAliasSymbol;
    }

    /// <summary>
    /// Finds the local <c>@using</c> directive that contributed the bound component descriptor
    /// and declared the alias named by <paramref name="aliasSpan"/>. Returns position information
    /// for the alias identifier in that directive, mapped to generated C# so Roslyn can handle rename.
    /// Returns false if the alias is imported or its declaration cannot be mapped.
    /// </summary>
    private bool TryGetComponentAliasPositionInfo(
        RazorCodeDocument codeDocument,
        TagHelperDescriptor component,
        TextSpan aliasSpan,
        out DocumentPositionInfo positionInfo)
    {
        positionInfo = default;

        var root = codeDocument.GetRequiredSyntaxRoot();
        var aliasName = codeDocument.Source.Text.ToString(aliasSpan);
        foreach (var contribution in codeDocument.GetDirectiveTagHelperContributions())
        {
            if (!contribution.ContributedTagHelpers.Contains(component) ||
                root.FindInnermostNode(contribution.DirectiveSpanStart)?.FirstAncestorOrSelf<RazorUsingDirectiveSyntax>() is not { } directive)
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

                var directiveAlias = import.ParsedNamespace.AsSpan(0, equalsIndex).Trim();
                if (directiveAlias[0] == '@')
                {
                    directiveAlias = directiveAlias[1..];
                }

                if (!directiveAlias.Equals(aliasName.AsSpan(), StringComparison.Ordinal))
                {
                    continue;
                }

                // Component aliases are emitted as their target types, so route tag rename through
                // the local @using declaration where Roslyn can resolve the alias symbol.
                foreach (var token in tokens)
                {
                    if (token.Kind is not (SyntaxKind.Identifier or SyntaxKind.Keyword))
                    {
                        continue;
                    }

                    var tokenContent = token.Content.AsSpan();
                    var tokenStart = token.SpanStart;
                    var alternateTokenStart = tokenStart;
                    if (tokenContent.Length > 0 && tokenContent[0] == '@')
                    {
                        tokenContent = tokenContent[1..];
                        alternateTokenStart++;
                    }

                    if (tokenContent.Equals(aliasName.AsSpan(), StringComparison.Ordinal) &&
                        TryMapAliasPosition(codeDocument, tokenStart, alternateTokenStart, out var csharpPosition, out var inDeclDocument))
                    {
                        positionInfo = new DocumentPositionInfo(RazorLanguageKind.CSharp, csharpPosition.ToPosition(), tokenStart, inDeclDocument);
                        return true;
                    }
                }
            }
        }

        // Imported aliases have no local directive contribution, so neither rename nor
        // prepare-rename should offer to change just this document's copy of the alias.
        return false;
    }

    private bool TryGetAliasPositionInfoFromUsingDirective(
        RazorCodeDocument codeDocument,
        int hostDocumentIndex,
        out DocumentPositionInfo positionInfo)
    {
        positionInfo = default;

        var root = codeDocument.GetRequiredSyntaxRoot();
        if (root.FindInnermostNode(hostDocumentIndex)?.FirstAncestorOrSelf<RazorUsingDirectiveSyntax>() is not { } directive)
        {
            return false;
        }

        foreach (var node in directive.DescendantNodes())
        {
            if (!TryGetImportAliasTokens(node, out var import, out var tokens))
            {
                continue;
            }

            var equalsIndex = import.ParsedNamespace.IndexOf('=');
            if (equalsIndex <= 0)
            {
                continue;
            }

            var alias = import.ParsedNamespace.AsSpan(0, equalsIndex).Trim();
            if (alias[0] == '@')
            {
                alias = alias[1..];
            }

            foreach (var token in tokens)
            {
                if (token.Kind is not (SyntaxKind.Identifier or SyntaxKind.Keyword) ||
                    !token.Span.IntersectsWith(hostDocumentIndex))
                {
                    continue;
                }

                var tokenContent = token.Content.AsSpan();
                var alternateTokenStart = token.SpanStart;
                if (tokenContent.Length > 0 && tokenContent[0] == '@')
                {
                    tokenContent = tokenContent[1..];
                    alternateTokenStart++;
                }

                if (!tokenContent.Equals(alias, StringComparison.Ordinal) ||
                    !TryMapAliasPosition(codeDocument, token.SpanStart, alternateTokenStart, out var csharpPosition, out var inDeclDocument))
                {
                    continue;
                }

                positionInfo = new DocumentPositionInfo(RazorLanguageKind.CSharp, csharpPosition.ToPosition(), hostDocumentIndex, inDeclDocument);
                return true;
            }
        }

        return false;
    }

    private static bool TryGetImportAliasTokens(
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

    private bool TryMapAliasPosition(
        RazorCodeDocument codeDocument,
        int preferredPosition,
        int tokenStart,
        out LinePosition csharpPosition,
        out bool inDeclDocument)
    {
        if (DocumentMappingService.TryMapToCSharpDocumentLinePosition(codeDocument, preferredPosition, out csharpPosition, out _, out inDeclDocument))
        {
            return true;
        }

        return DocumentMappingService.TryMapToCSharpDocumentLinePosition(codeDocument, tokenStart, out csharpPosition, out _, out inDeclDocument);
    }

    /// <summary>
    /// Finds a position before the final component name in an alias-qualified tag, such as
    /// <c>UI.Controls</c> in <c>&lt;UI.Controls.Button /&gt;</c>, and returns the alias span and
    /// a bound component descriptor. Both start and end tags are supported. This does not resolve
    /// the alias or check whether its <c>@using</c> directive is local to the document.
    /// </summary>
    private static bool TryGetNamespaceAliasFromComponentTag(
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

        var firstDot = name.Content.IndexOf('.');
        var lastDot = name.Content.LastIndexOf('.');
        if (firstDot <= 0 || hostDocumentIndex > name.SpanStart + lastDot)
        {
            return false;
        }

        TagHelperDescriptor? candidate = null;
        foreach (var boundRule in element.TagHelperInfo.BindingResult.AllBoundRules)
        {
            if (boundRule.Descriptor.Kind == TagHelperKind.Component)
            {
                // Different generic arities can share this tag name. Any candidate can identify
                // the alias directive; choosing the component type is a separate step.
                candidate ??= boundRule.Descriptor;
            }
        }

        // Ordinary qualified component names have their own C# mappings. A dotted tag bound to
        // a short-name descriptor instead gets its prefix from a namespace alias.
        if (candidate is null || candidate.IsFullyQualifiedNameMatch)
        {
            return false;
        }

        component = candidate;
        aliasSpan = new TextSpan(name.SpanStart, firstDot);
        return true;
    }

    private static bool TryGetTypeAliasFromComponentTag(
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

    private static TextDocumentEdit? GetTypeAliasTagEdits(
        RemoteDocumentSnapshot snapshot,
        RazorCodeDocument codeDocument,
        TextSpan aliasDeclarationSpan,
        string aliasName,
        string newName,
        CancellationToken cancellationToken)
    {
        var root = codeDocument.GetRequiredSyntaxRoot();
        var directive = root.FindInnermostNode(aliasDeclarationSpan.Start)?.FirstAncestorOrSelf<RazorUsingDirectiveSyntax>();
        if (directive is null)
        {
            return null;
        }

        var contributedTagHelpers = TagHelperCollection.Empty;
        foreach (var contribution in codeDocument.GetDirectiveTagHelperContributions())
        {
            if (contribution.DirectiveSpanStart == directive.SpanStart)
            {
                contributedTagHelpers = contribution.ContributedTagHelpers;
                break;
            }
        }

        if (contributedTagHelpers.Count == 0)
        {
            return null;
        }

        using var edits = new PooledArrayBuilder<SumType<TextEdit, AnnotatedTextEdit>>();
        var sourceText = codeDocument.Source.Text;
        var prefix = codeDocument.GetTagHelperContext()?.Prefix ?? "";
        foreach (var node in root.DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node is not MarkupTagHelperElementSyntax element)
            {
                continue;
            }

            foreach (var boundRule in element.TagHelperInfo.BindingResult.AllBoundRules)
            {
                var descriptor = boundRule.Descriptor;
                if (descriptor.Kind != TagHelperKind.Component || !contributedTagHelpers.Contains(descriptor))
                {
                    continue;
                }

                var comparison = descriptor.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                AddAliasEdit(element.StartTag?.Name, sourceText, prefix, aliasName, newName, comparison, ref edits.AsRef());
                AddAliasEdit(element.EndTag?.Name, sourceText, prefix, aliasName, newName, comparison, ref edits.AsRef());
                break;
            }
        }

        if (edits.Count == 0)
        {
            return null;
        }

        return new TextDocumentEdit
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier { DocumentUri = snapshot.Uri },
            Edits = edits.ToArrayAndClear()
        };

        static void AddAliasEdit(
            RazorSyntaxToken? name,
            SourceText sourceText,
            string prefix,
            string aliasName,
            string newName,
            StringComparison comparison,
            ref PooledArrayBuilder<SumType<TextEdit, AnnotatedTextEdit>> edits)
        {
            if (name is not { } token)
            {
                return;
            }

            var tokenContent = token.Content.AsSpan();
            var aliasStart = token.SpanStart;
            if (prefix.Length > 0 && tokenContent.StartsWith(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                tokenContent = tokenContent[prefix.Length..];
                aliasStart += prefix.Length;
            }

            if (tokenContent.Equals(aliasName.AsSpan(), comparison))
            {
                edits.Add(LspFactory.CreateTextEdit(sourceText.GetRange(aliasStart, aliasStart + aliasName.Length), newName));
            }
        }
    }

    private async Task<TextSpan?> TryGetLocalAliasDeclarationSpanAsync(
        IAliasSymbol alias,
        Document generatedDocument,
        RazorCodeDocument codeDocument,
        bool inDeclDocument,
        CancellationToken cancellationToken)
    {
        // Global aliases can be declared in another C# document; imported aliases have no local
        // Razor source mapping. Neither can be renamed across all consumers by pairing this document's projections.
        var syntaxTree = await generatedDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (alias.Locations is not [var location, ..] || location.SourceTree != syntaxTree)
        {
            return null;
        }

        var csharpDocument = codeDocument.GetRequiredCSharpDocument(inDeclDocument);
        if (!DocumentMappingService.TryMapToRazorDocumentPosition(csharpDocument, location.SourceSpan.Start, out _, out var aliasStart) ||
            codeDocument.GetRequiredSyntaxRoot().FindInnermostNode(aliasStart)?.FirstAncestorOrSelf<RazorUsingDirectiveSyntax>() is null)
        {
            return null;
        }

        // The symbol's source span includes the escape prefix, while alias.Name is only the identifier's value.
        return new TextSpan(aliasStart, location.SourceSpan.Length);
    }

    private static TextDocumentEdit? GetNamespaceAliasTagEdits(
        RemoteDocumentSnapshot snapshot,
        RazorCodeDocument codeDocument,
        TextSpan aliasSpan,
        string aliasName,
        string newName,
        bool needsAliasDeclarationEdit,
        CancellationToken cancellationToken)
    {
        var root = codeDocument.GetRequiredSyntaxRoot();
        var directive = root.FindInnermostNode(aliasSpan.Start)?.FirstAncestorOrSelf<RazorUsingDirectiveSyntax>();
        if (directive is null)
        {
            return null;
        }

        var contributedTagHelpers = TagHelperCollection.Empty;
        foreach (var contribution in codeDocument.GetDirectiveTagHelperContributions())
        {
            if (contribution.DirectiveSpanStart == directive.SpanStart)
            {
                contributedTagHelpers = contribution.ContributedTagHelpers;
                break;
            }
        }

        if (contributedTagHelpers.Count == 0)
        {
            return null;
        }

        using var edits = new PooledArrayBuilder<SumType<TextEdit, AnnotatedTextEdit>>();
        var sourceText = codeDocument.Source.Text;
        foreach (var node in root.DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node is not MarkupTagHelperElementSyntax element)
            {
                continue;
            }

            foreach (var boundRule in element.TagHelperInfo.BindingResult.AllBoundRules)
            {
                if (boundRule.Descriptor.Kind != TagHelperKind.Component ||
                    !contributedTagHelpers.Contains(boundRule.Descriptor))
                {
                    continue;
                }

                // Only bound components contributed by this directive qualify. Similar-looking HTML,
                // canonical names, and tags supplied by other aliases must remain untouched.
                AddPrefixEdit(element.StartTag?.Name, sourceText, aliasName, newName, ref edits.AsRef());
                AddPrefixEdit(element.EndTag?.Name, sourceText, aliasName, newName, ref edits.AsRef());
                break;
            }
        }

        if (edits.Count == 0)
        {
            return null;
        }

        // Roslyn returns no edits for an alias with no C# references. A namespace alias used only
        // by component tags still needs its local declaration renamed along with those tags.
        if (needsAliasDeclarationEdit)
        {
            edits.Add(LspFactory.CreateTextEdit(sourceText.GetRange(aliasSpan.Start, aliasSpan.End), newName));
        }

        return new TextDocumentEdit
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier { DocumentUri = snapshot.Uri },
            Edits = edits.ToArrayAndClear()
        };

        static void AddPrefixEdit(
            RazorSyntaxToken? name,
            SourceText sourceText,
            string aliasName,
            string newName,
            ref PooledArrayBuilder<SumType<TextEdit, AnnotatedTextEdit>> edits)
        {
            if (name is not { } token ||
                token.Content.Length <= aliasName.Length ||
                token.Content[aliasName.Length] != '.' ||
                !token.Content.AsSpan().StartsWith(aliasName.AsSpan(), StringComparison.Ordinal))
            {
                return;
            }

            edits.Add(LspFactory.CreateTextEdit(sourceText.GetRange(token.SpanStart, token.SpanStart + aliasName.Length), newName));
        }
    }

    private async Task<WorkspaceEdit?> TryGetOtherAliasRenameEditAsync(
        RemoteDocumentSnapshot snapshot,
        RazorCodeDocument codeDocument,
        Document generatedDocument,
        DocumentPositionInfo positionInfo,
        string newName,
        CancellationToken cancellationToken)
    {
        var generatedText = await generatedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var renameInfo = await SymbolicRenameInfo.GetRenameInfoAsync(generatedDocument, generatedText.GetRequiredAbsoluteIndex(positionInfo.Position), cancellationToken).ConfigureAwait(false);
        if (renameInfo.Symbol is not IAliasSymbol alias)
        {
            return null;
        }

        // A global using alias may be declared in another C# document. Only pair aliases declared
        // in this generated document, since another document's declaration offsets cannot be mapped
        // through this generated document.
        var syntaxTree = await generatedDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (alias.Locations is not [var location, ..] || location.SourceTree != syntaxTree)
        {
            return null;
        }

        // Locate the shared Razor declaration, then map it into the other generated C# document.
        var csharpDocument = codeDocument.GetRequiredCSharpDocument(positionInfo.InDeclDocument);
        if (!DocumentMappingService.TryMapToRazorDocumentPosition(csharpDocument, location.SourceSpan.Start, out _, out var aliasIndex))
        {
            return null;
        }

        var otherCSharpDocument = codeDocument.GetCSharpDocument(!positionInfo.InDeclDocument);
        if (otherCSharpDocument is null)
        {
            return null;
        }

        if (!DocumentMappingService.TryMapToCSharpDocumentPosition(otherCSharpDocument, aliasIndex, out var otherPosition, out _))
        {
            return null;
        }

        var otherDocument = await snapshot.GetGeneratedDocumentAsync(!positionInfo.InDeclDocument, cancellationToken).ConfigureAwait(false);
        return await RenameHandler.GetRenameEditAsync(otherDocument, otherPosition, newName, allowRenamesInRazorSourceGeneratedDocuments: true, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<RemoteResponse<LspRange?>> GetPrepareRenameRangeAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        Position position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            snapshot => GetPrepareRenameRangeAsync(snapshot, position, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<LspRange?>> GetPrepareRenameRangeAsync(
        RemoteDocumentSnapshot snapshot,
        Position position,
        CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        var sourceText = codeDocument.Source.Text;

        if (!sourceText.TryGetAbsoluteIndex(position, out var hostDocumentIndex))
        {
            return RemoteResponse<LspRange?>.NoFurtherHandling;
        }

        var originalHostDocumentIndex = hostDocumentIndex;
        hostDocumentIndex = codeDocument.AdjustPositionForComponentEndTag(hostDocumentIndex);

        var positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: true);
        if (TryGetAliasPositionInfoFromUsingDirective(codeDocument, hostDocumentIndex, out var declarationPositionInfo))
        {
            positionInfo = declarationPositionInfo;
        }

        TextSpan? componentAliasSpan = null;

        // Alias-bound tags are emitted as their target types. Use the local @using declaration
        // for Roslyn's rename checks, but keep the tag span for prepare-rename's result.
        if (TryGetTypeAliasFromComponentTag(codeDocument, originalHostDocumentIndex, out var typeAliasComponent, out var typeAliasSpan))
        {
            if (!TryGetComponentAliasPositionInfo(codeDocument, typeAliasComponent, typeAliasSpan, out var aliasPositionInfo))
            {
                return RemoteResponse<LspRange?>.NoFurtherHandling;
            }

            positionInfo = aliasPositionInfo;
            componentAliasSpan = typeAliasSpan;
        }
        else if (TryGetNamespaceAliasFromComponentTag(codeDocument, originalHostDocumentIndex, out var aliasComponent, out var aliasSpan))
        {
            // An intermediate namespace segment isn't a renameable symbol.
            if (originalHostDocumentIndex < aliasSpan.Start || originalHostDocumentIndex > aliasSpan.End)
            {
                return RemoteResponse<LspRange?>.NoFurtherHandling;
            }

            if (!TryGetComponentAliasPositionInfo(codeDocument, aliasComponent, aliasSpan, out var aliasPositionInfo))
            {
                // This is an alias prefix, but we couldn't map its local declaration. HTML rename
                // would change the tag without updating the alias, so don't offer it.
                return RemoteResponse<LspRange?>.NoFurtherHandling;
            }

            positionInfo = aliasPositionInfo;
            componentAliasSpan = aliasSpan;
        }

        // We don't need the component file-rename logic here, since Roslyn can provide the range.
        // If the range isn't C#, we don't support rename, so we can just ask Roslyn. The extra gubbins in the actual rename path is because
        // if we let Roslyn rename a component class name, for example, it won't know that it needs to actually rename the .razor file or
        // nothing will actually happen.
        if (positionInfo.LanguageKind is not CodeAnalysis.Razor.Protocol.RazorLanguageKind.CSharp)
        {
            return RemoteResponse<LspRange?>.CallHtml;
        }

        var generatedDocument = await snapshot.GetGeneratedDocumentAsync(positionInfo.InDeclDocument, cancellationToken).ConfigureAwait(false);

        // Renaming an imported alias would only change one generated copy, not the declaration
        // and references in every consuming document. Only offer alias rename for local directives.
        var alias = await GetAliasSymbolAsync(generatedDocument, positionInfo.Position, cancellationToken).ConfigureAwait(false);
        if (alias is not null &&
            await TryGetLocalAliasDeclarationSpanAsync(alias, generatedDocument, codeDocument, positionInfo.InDeclDocument, cancellationToken).ConfigureAwait(false) is null)
        {
            return RemoteResponse<LspRange?>.NoFurtherHandling;
        }

        var csharpRange = await PrepareRenameHandler.GetRenameRangeAsync(generatedDocument, positionInfo.Position.ToLinePosition(), cancellationToken).ConfigureAwait(false);

        if (csharpRange is null)
        {
            return RemoteResponse<LspRange?>.NoFurtherHandling;
        }

        if (componentAliasSpan is { } selectedAliasSpan)
        {
            return RemoteResponse<LspRange?>.Results(sourceText.GetRange(selectedAliasSpan));
        }

        if (!DocumentMappingService.TryMapToRazorDocumentRange(codeDocument.GetRequiredCSharpDocument(positionInfo.InDeclDocument), csharpRange, out var mappedRange))
        {
            return RemoteResponse<LspRange?>.NoFurtherHandling;
        }

        // Component end tags are looked up via their matching start tag so Roslyn can find the symbol.
        // If the request started on an end tag, shift the mapped range back so prepare-rename highlights that end tag text.
        if (originalHostDocumentIndex != hostDocumentIndex)
        {
            var offset = originalHostDocumentIndex - hostDocumentIndex;
            if (sourceText.TryGetAbsoluteIndex(mappedRange.Start, out var start) &&
                sourceText.TryGetAbsoluteIndex(mappedRange.End, out var end))
            {
                mappedRange = sourceText.GetRange(start + offset, end + offset);
            }
        }

        return RemoteResponse<LspRange?>.Results(mappedRange);
    }

    public ValueTask<WorkspaceEdit?> GetFileRenameEditAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        RenameFilesParams fileRenameRequest,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            snapshot => GetFileRenameEditAsync(snapshot, fileRenameRequest, cancellationToken),
            cancellationToken);

    private async ValueTask<WorkspaceEdit?> GetFileRenameEditAsync(Solution solution, RenameFilesParams fileRenameRequest, CancellationToken cancellationToken)
    {
        var response = new WorkspaceEdit();

        foreach (var file in fileRenameRequest.Files)
        {
            var newFilePath = file.NewUri.GetAbsoluteOrUNCPath();

            // We know we won't get called unless the OldUri is a .razor document, but we are responsible to confirm that
            // the new Uri is still a .razor document.
            if (!FileUtilities.IsRazorComponentFilePath(newFilePath, PathUtilities.OSSpecificPathComparison))
            {
                continue;
            }

            var newFileName = Path.GetFileNameWithoutExtension(newFilePath);

            // We also need to make sure OldUri is a document we know about
            if (!solution.TryGetRazorDocument(file.OldUri.GetRequiredSystemUri(), out var oldDoc))
            {
                continue;
            }

            // Make sure that the filename itself is actually changing (ie, we don't care about file moves)
            if (newFileName == Path.GetFileNameWithoutExtension(oldDoc.FilePath))
            {
                continue;
            }

            Logger.LogDebug($"Rename for Razor document from {oldDoc.FilePath} to {newFileName}.");

            var documentSnapshot = CreateRazorDocumentSnapshot(solution, oldDoc.Id);
            if (documentSnapshot is null)
            {
                continue;
            }

            var documentEdit = await GetFileRenameEditAsync(documentSnapshot, newFileName, cancellationToken).ConfigureAwait(false);
            response = response.Concat(documentEdit);
        }

        if (response.DocumentChanges is null)
        {
            return null;
        }

        return response;
    }

    private async Task<WorkspaceEdit?> GetFileRenameEditAsync(RemoteDocumentSnapshot snapshot, string newFileName, CancellationToken cancellationToken)
    {
        if (!snapshot.FileKind.IsComponent())
        {
            return null;
        }

        // We're renaming the class declaration of a generated C# class, which exists in both decl and impl documents, so we can just work from
        // the impl document since that will always exist. Decl may not.
        var generatedDocument = await snapshot.GetGeneratedDocumentAsync(declarationDocument: false, cancellationToken).ConfigureAwait(false);
        var text = await generatedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var tree = await generatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var declaration = tree.AssumeNotNull().DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (declaration is null)
        {
            return null;
        }

        var position = text.GetLinePosition(declaration.Identifier.SpanStart);

        string newComponentName;
        using (var _ = StringBuilderPool.GetPooledObject(out var builder))
        {
            RazorSourceGenerator.BuildIdentifierFromPath(builder, newFileName);
            newComponentName = builder.ToString();
        }

        var csharpEdit = await RenameHandler.GetRenameEditAsync(generatedDocument, position, newComponentName, allowRenamesInRazorSourceGeneratedDocuments: true, cancellationToken)
            .ConfigureAwait(false);

        if (csharpEdit is null)
        {
            return null;
        }

        await _razorEditService.MapWorkspaceEditAsync(snapshot.TextDocument.Project.Solution, csharpEdit, cancellationToken).ConfigureAwait(false);

        _renameService.TryGetRazorFileRenameEdit(snapshot, newFileName, out var razorEdit);

        return csharpEdit.Concat(razorEdit);
    }
}
