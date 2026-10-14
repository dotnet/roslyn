// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.Formatting;

internal sealed class FormattingEngine
{
    private readonly ImmutableArray<IFormattingValidationPass> _validationPasses;

    public FormattingEngine(ILoggerFactory loggerFactory)
    {
        _validationPasses =
        [
            new FormattingDiagnosticValidationPass(loggerFactory),
            new FormattingContentValidationPass(loggerFactory)
        ];
    }

    public async Task<ImmutableArray<TextChange>> FormatDocumentAsync<TOptions>(
        FormattingContext context,
        ImmutableArray<IFormattingPass> documentFormattingPasses,
        ImmutableArray<TextChange> htmlChanges,
        LinePositionSpan? range,
        TOptions options,
        CancellationToken cancellationToken)
    {
        var codeDocument = context.CodeDocument;
        var sourceText = context.SourceText;

        // Range formatting happens on every paste, and if there are Razor diagnostics in the file
        // that can make some very bad results. eg, given:
        //
        // |
        // @code {
        // }
        //
        // When pasting "<button" at the | the HTML formatter will bring the "@code" onto the same
        // line as "<button" because as far as it's concerned, its an attribute.
        //
        // To defeat that, we simply don't do range formatting if there are diagnostics.

        // Despite what it looks like, getting diagnostics from a CSharpDocument is actually the
        // Razor diagnostics, not the Roslyn C# diagnostics 🤦‍
        if (range is { } span)
        {
            if (codeDocument.GetRequiredCSharpDocument(declarationDocument: false).Diagnostics.Any(d => d.Span != SourceSpan.Undefined && span.OverlapsWith(sourceText.GetLinePositionSpan(d.Span))))
            {
                return [];
            }
        }

        var logger = context.Logger;
        logger?.LogObject("FileKind", context.OriginalSnapshot.FileKind);
        logger?.LogObject("Options", options);
        logger?.LogObject("HtmlChanges", htmlChanges.SelectAsArray(e => e.ToRazorTextChange()));
        logger?.LogObject("Range", range);
        logger?.LogSourceText("InitialDocument", sourceText);
        LogSyntaxTree(logger, codeDocument);

        var result = htmlChanges;
        foreach (var formattingPass in documentFormattingPasses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result = await formattingPass.ExecuteAsync(context, result, cancellationToken).ConfigureAwait(false);
        }

        var filteredChanges = range is not { } linePositionSpan
            ? result
            : result.WhereAsArray(e => linePositionSpan.LineOverlapsWith(sourceText.GetLinePositionSpan(e.Span)));

        var normalizedChanges = NormalizeLineEndings(sourceText, filteredChanges);

        if (!await ValidateAsync(context, normalizedChanges, cancellationToken).ConfigureAwait(false))
        {
            return [];
        }

        return sourceText.MinimizeTextChanges(normalizedChanges);
    }

    public async Task<bool> ValidateAsync(FormattingContext context, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
    {
        foreach (var validationPass in _validationPasses)
        {
            if (!await validationPass.IsValidAsync(context, changes, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// This method counts the occurrences of CRLF and LF line endings in the original text.
    /// If LF line endings are more prevalent, it removes any CR characters from the text changes
    /// to ensure consistency with the LF style.
    /// </summary>
    public static ImmutableArray<TextChange> NormalizeLineEndings(SourceText originalText, ImmutableArray<TextChange> changes)
    {
        if (originalText.HasLFLineEndings())
        {
            return ReplaceInChanges(changes, "\r", "");
        }

        return changes;
    }

    public static void LogSyntaxTree(IFormattingLogger? logger, RazorCodeDocument codeDocument)
    {
        if (logger is null)
        {
            return;
        }

        var syntaxRoot = (RazorSyntaxNode)codeDocument.GetRequiredTagHelperRewrittenSyntaxTree().Root;
        var serializedSyntaxTree = SyntaxSerializer.Default.Serialize(syntaxRoot);
        logger.LogSourceText("SyntaxTree", SourceText.From(serializedSyntaxTree));
    }

    public static ImmutableArray<TextChange> ReplaceInChanges(ImmutableArray<TextChange> csharpChanges, string toFind, string replacement)
    {
        using var changes = new PooledArrayBuilder<TextChange>(csharpChanges.Length);
        foreach (var change in csharpChanges)
        {
            if (change.NewText is not { } newText ||
                newText.IndexOf(toFind) == -1)
            {
                changes.Add(change);
                continue;
            }

            // Formatting doesn't work with syntax errors caused by the cursor marker ($0).
            // So, let's avoid the error by wrapping the cursor marker in a comment.
            changes.Add(new(change.Span, newText.Replace(toFind, replacement)));
        }

        return changes.ToImmutableAndClear();
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(FormattingEngine engine)
    {
        public void SetDebugAssertsEnabled(bool debugAssertsEnabled)
        {
            var contentValidationPass = engine._validationPasses.OfType<FormattingContentValidationPass>().Single();
            contentValidationPass.DebugAssertsEnabled = debugAssertsEnabled;
        }

        public void SetDiagnosticDebugAssertsEnabled(bool debugAssertsEnabled)
        {
            var diagnosticValidationPass = engine._validationPasses.OfType<FormattingDiagnosticValidationPass>().Single();
            diagnosticValidationPass.DebugAssertsEnabled = debugAssertsEnabled;
        }
    }
}
