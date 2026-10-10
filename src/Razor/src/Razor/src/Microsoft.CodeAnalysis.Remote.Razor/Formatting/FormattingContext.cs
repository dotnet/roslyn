// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.Formatting;

// This partial is compiled only into Remote Razor. It owns the syntax visitor, indentation caches, and transport-option
// conversion that depend on Remote Razor or Razor Workspaces; the other partial is shared with dotnet format.
internal sealed partial class FormattingContext
{
    private ImmutableArray<FormattingSpan>? _formattingSpans;
    private IReadOnlyDictionary<int, IndentationContext>? _indentations;

    // RazorEditService still accepts the concrete Remote snapshot. Contexts created by this partial preserve that
    // snapshot type across WithTextAsync, so keep the cast at this Remote-only boundary.
    public RemoteDocumentSnapshot CurrentRemoteSnapshot => (RemoteDocumentSnapshot)CurrentSnapshot;

    /// <summary>A Dictionary of int (line number) to IndentationContext.</summary>
    /// <remarks>
    /// Don't use this to discover the indentation level you should have, use
    /// <see cref="TryGetIndentationLevel(int, out int)"/> which operates on the position rather than just the line.
    /// </remarks>
    public IReadOnlyDictionary<int, IndentationContext> GetIndentations()
    {
        if (_indentations is null)
        {
            var sourceText = SourceText;
            var indentations = new Dictionary<int, IndentationContext>();

            var previousIndentationLevel = 0;
            for (var i = 0; i < sourceText.Lines.Count; i++)
            {
                var line = sourceText.Lines[i];
                // Get first non-whitespace character position
                var nonWsPos = line.GetFirstNonWhitespacePosition();
                var existingIndentation = (nonWsPos ?? line.End) - line.Start;

                // The existingIndentation above is measured in characters, and is used to create text edits
                // The below is measured in columns, so takes into account tab size. This is useful for creating
                // new indentation strings
                var existingIndentationSize = line.GetIndentationSize(Options.TabSize);

                var emptyOrWhitespaceLine = false;
                if (nonWsPos is null)
                {
                    emptyOrWhitespaceLine = true;
                    nonWsPos = line.Start;
                }

                // position now contains the first non-whitespace character or 0. Get the corresponding FormattingSpan.
                if (TryGetFormattingSpan(nonWsPos.Value, out var span))
                {
                    indentations[i] = new IndentationContext(
                        FirstSpan: span,
                        Line: i,
#if DEBUG
                        DebugOnly_LineText: line.ToString(),
#endif
                        RazorIndentationLevel: span.RazorIndentationLevel,
                        HtmlIndentationLevel: span.HtmlIndentationLevel,
                        RelativeIndentationLevel: span.IndentationLevel - previousIndentationLevel,
                        ExistingIndentation: existingIndentation,
                        EmptyOrWhitespaceLine: emptyOrWhitespaceLine,
                        ExistingIndentationSize: existingIndentationSize);
                    previousIndentationLevel = span.IndentationLevel;
                }
                else
                {
                    // Couldn't find a corresponding FormattingSpan. Happens if it is a 0 length line.
                    // Let's create a 0 length span to represent this and default it to HTML.
                    var placeholderSpan = new FormattingSpan(
                        new TextSpan(nonWsPos.Value, 0),
                        FormattingSpanKind.Markup,
                        RazorIndentationLevel: 0,
                        HtmlIndentationLevel: 0,
                        IsInGlobalNamespace: false,
                        IsInClassBody: false,
                        ComponentLambdaNestingLevel: 0);

                    indentations[i] = new IndentationContext(
                        FirstSpan: placeholderSpan,
                        Line: i,
#if DEBUG
                        DebugOnly_LineText: line.ToString(),
#endif
                        RazorIndentationLevel: 0,
                        HtmlIndentationLevel: 0,
                        RelativeIndentationLevel: previousIndentationLevel,
                        ExistingIndentation: existingIndentation,
                        EmptyOrWhitespaceLine: emptyOrWhitespaceLine,
                        ExistingIndentationSize: existingIndentation);
                }
            }

            _indentations = indentations;
        }

        return _indentations;
    }

    private ImmutableArray<FormattingSpan> GetFormattingSpans()
    {
        return _formattingSpans ??= ComputeFormattingSpans(CodeDocument);

        static ImmutableArray<FormattingSpan> ComputeFormattingSpans(RazorCodeDocument codeDocument)
        {
            var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
            var inGlobalNamespace = codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var @namespace) &&
                string.IsNullOrEmpty(@namespace);

            return GetFormattingSpans(syntaxTree, inGlobalNamespace: inGlobalNamespace);
        }
    }

    private static ImmutableArray<FormattingSpan> GetFormattingSpans(RazorSyntaxTree syntaxTree, bool inGlobalNamespace)
    {
        using var _ = ArrayBuilderPool<FormattingSpan>.GetPooledObject(out var formattingSpans);

        FormattingVisitor.VisitRoot(syntaxTree, formattingSpans, inGlobalNamespace);

        return formattingSpans.ToImmutableAndClear();
    }

    public bool TryGetIndentationLevel(int position, out int indentationLevel)
    {
        if (TryGetFormattingSpan(position, out var span))
        {
            indentationLevel = span.IndentationLevel;
            return true;
        }

        indentationLevel = 0;
        return false;
    }

    public bool TryGetFormattingSpan(int absoluteIndex, [NotNullWhen(true)] out FormattingSpan? result)
    {
        result = null;
        var formattingSpans = GetFormattingSpans();
        foreach (var formattingSpan in formattingSpans)
        {
            var span = formattingSpan.Span;

            if (span.Start <= absoluteIndex && span.End >= absoluteIndex)
            {
                if (span.End == absoluteIndex && span.Length > 0)
                {
                    // We're at an edge.
                    // Non-marker spans (spans.length == 0) do not own the edges after it
                    continue;
                }

                result = formattingSpan;
                return true;
            }
        }

        return false;
    }

    public static FormattingContext CreateForOnTypeFormatting(
        RemoteDocumentSnapshot originalSnapshot,
        RazorCodeDocument codeDocument,
        bool? declarationDocument,
        RazorFormattingOptions options,
        IFormattingLogger? logger,
        bool includeCSharpLanguageFeatureEdits,
        int hostDocumentIndex,
        char triggerCharacter)
    {
        return new FormattingContext(
            originalSnapshot,
            codeDocument,
            declarationDocument,
            currentSnapshot: originalSnapshot,
            ToFormattingEngineOptions(options),
            logger,
            includeCSharpLanguageFeatureEdits,
            hostDocumentIndex,
            triggerCharacter);
    }

    public static FormattingContext Create(
        RemoteDocumentSnapshot originalSnapshot,
        RazorCodeDocument codeDocument,
        RazorFormattingOptions options,
        IFormattingLogger? logger)
    {
        return new FormattingContext(
            originalSnapshot,
            codeDocument,
            declarationDocument: null,
            currentSnapshot: originalSnapshot,
            ToFormattingEngineOptions(options),
            logger,
            includeCSharpLanguageFeatureEdits: false,
            hostDocumentIndex: 0,
            triggerCharacter: '\0');
    }

    // RazorFormattingOptions is the serialized, Razor-wide transport contract. The shared engine intentionally accepts
    // only the host-neutral subset it needs so the dotnet format adapter does not depend on Razor Workspaces or LSP.
    private static FormattingEngineOptions ToFormattingEngineOptions(RazorFormattingOptions options)
        => new()
        {
            InsertSpaces = options.InsertSpaces,
            TabSize = options.TabSize,
            CodeBlockBraceOnNextLine = options.CodeBlockBraceOnNextLine,
            AttributeIndentStyle = options.AttributeIndentStyle,
            CSharpSyntaxFormattingOptions = options.CSharpSyntaxFormattingOptions,
        };
}
