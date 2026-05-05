// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal static class CodeRenderingContextExtensions
{
    public static LinePragmaScope BuildLinePragma(
        this CodeRenderingContext context, SourceSpan? span, bool suppressLineDefaultAndHidden = false)
    {
        Debug.Assert(context.Options.DesignTime, "Runtime generation should only use enhanced line pragmas");

        // Can't build a valid line pragma without a file path.
        return span is SourceSpan spanValue && !spanValue.FilePath.IsNullOrEmpty()
            ? LinePragmaScope.Standard(context, spanValue, suppressLineDefaultAndHidden)
            : LinePragmaScope.None;
    }

    public static LinePragmaScope BuildEnhancedLinePragma(
        this CodeRenderingContext context, SourceSpan? span, int characterOffset = 0, bool suppressLineDefaultAndHidden = false)
    {
        // Can't build a valid line pragma without a file path.
        return span is SourceSpan spanValue && !spanValue.FilePath.IsNullOrEmpty()
            ? LinePragmaScope.Enhanced(context, spanValue, characterOffset, suppressLineDefaultAndHidden)
            : LinePragmaScope.None;
    }

    public readonly ref struct LinePragmaScope
    {
        private readonly CodeRenderingContext _context;
        private readonly SourceSpan _span;
        private readonly bool _isEnhanced;
        private readonly int _originalIndent;
        private readonly bool _suppressLineDefaultAndHidden;

        private LinePragmaScope(
            CodeRenderingContext context,
            SourceSpan span,
            bool isEnhanced,
            int originalIndent,
            bool suppressLineDefaultAndHidden)
        {
            _context = context;
            _span = span;
            _isEnhanced = isEnhanced;
            _originalIndent = originalIndent;
            _suppressLineDefaultAndHidden = suppressLineDefaultAndHidden;
        }

        public static LinePragmaScope None => default;

        public static LinePragmaScope Standard(CodeRenderingContext context, SourceSpan span, bool suppressLineDefaultAndHidden)
        {
            var writer = context.CodeWriter;
            var options = context.Options;

            var (originalIndent, ensurePathBackSlashes) = WritePreamble(writer, options);

            writer.WriteLineNumberDirective(span, ensurePathBackSlashes);

            return new(context, span, isEnhanced: false, originalIndent, suppressLineDefaultAndHidden);
        }

        public static LinePragmaScope Enhanced(CodeRenderingContext context, SourceSpan span, int characterOffset, bool suppressLineDefaultAndHidden)
        {
            var writer = context.CodeWriter;
            var options = context.Options;

            // Are we allowed to write an enhanced line pragma?
            if (!options.UseEnhancedLinePragma)
            {
                var standardScope = Standard(context, span, suppressLineDefaultAndHidden);

                // If the caller requested an enhanced line directive, but we fell back to
                // a regular one, write out the extra padding and add a source mapping.
                writer.WritePadding(offset: 0, span, context);
                context.AddSourceMappingFor(span);

                return standardScope;
            }

            var (originalIndent, ensurePathBackSlashes) = WritePreamble(writer, options);

            writer.WriteEnhancedLineNumberDirective(span, characterOffset, ensurePathBackSlashes);
            context.AddSourceMappingFor(span, characterOffset);

            return new(context, span, isEnhanced: true, originalIndent, suppressLineDefaultAndHidden);
        }

        private static (int originalIndent, bool ensureBackSlashes) WritePreamble(CodeWriter writer, RazorCodeGenerationOptions options)
        {
            // Capture the current indent and reset to 0 for writing directives.
            var originalIndent = writer.CurrentIndent;
            writer.CurrentIndent = 0;

            // Ensure that we start on a new line.
            writer.EnsureNewLine();

            if (!options.SuppressNullabilityEnforcement)
            {
                writer.WriteLine("#nullable restore");
            }

            var ensurePathBackSlashes = options.RemapLinePragmaPathsOnWindows && PlatformInformation.IsWindows;

            return (originalIndent, ensurePathBackSlashes);
        }

        public void Dispose()
        {
            // Just return if this LinePragmaScope wasn't initialized.
            if (_context is null)
            {
                return;
            }

            var writer = _context.CodeWriter;
            var options = _context.Options;

            writer.EnsureNewLine();

            // Always write at least 1 empty line to potentially separate code from pragmas.
            writer.WriteLine();

            var linePragma = new LinePragma(
                _isEnhanced,
                _span.FilePath,
                _span.LineIndex,
                endLineIndex: writer.Location.LineIndex);

            _context.AddLinePragma(linePragma);

            if (!_suppressLineDefaultAndHidden)
            {
                writer
                    .WriteLine("#line default")
                    .WriteLine("#line hidden");
            }

            if (!options.SuppressNullabilityEnforcement)
            {
                writer.WriteLine("#nullable disable");
            }

            // Reset indentation.
            writer.CurrentIndent = _originalIndent;
        }
    }
}
