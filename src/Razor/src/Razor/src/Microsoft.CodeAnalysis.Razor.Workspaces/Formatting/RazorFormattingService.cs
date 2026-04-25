// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal class RazorFormattingService : IRazorFormattingService
{
    public static readonly string FirstTriggerCharacter = "}";
    public static readonly ImmutableArray<string> MoreTriggerCharacters = [";", "\n", "{"];
    public static readonly FrozenSet<string> AllTriggerCharacterSet = FrozenSet.ToFrozenSet([FirstTriggerCharacter, .. MoreTriggerCharacters], StringComparer.Ordinal);

    private static readonly FrozenSet<string> s_csharpTriggerCharacterSet = FrozenSet.ToFrozenSet(["}", ";"], StringComparer.Ordinal);
    private static readonly FrozenSet<string> s_htmlTriggerCharacterSet = FrozenSet.ToFrozenSet(["\n", "{", "}", ";"], StringComparer.Ordinal);

    private readonly ImmutableArray<IFormattingPass> _documentFormattingPasses;
    private readonly ImmutableArray<IFormattingValidationPass> _validationPasses;
    private readonly CSharpOnTypeFormattingPass _csharpOnTypeFormattingPass;
    private readonly HtmlOnTypeFormattingPass _htmlOnTypeFormattingPass;

    private IFormattingLoggerFactory _formattingLoggerFactory;

    public RazorFormattingService(
        IDocumentMappingService documentMappingService,
        IRazorEditService razorEditService,
        IHostServicesProvider hostServicesProvider,
        IFormattingLoggerFactory formattingLoggerFactory,
        ILoggerFactory loggerFactory)
    {
        _htmlOnTypeFormattingPass = new HtmlOnTypeFormattingPass();
        _csharpOnTypeFormattingPass = new CSharpOnTypeFormattingPass(documentMappingService, razorEditService, hostServicesProvider, loggerFactory);
        _validationPasses =
        [
            new FormattingDiagnosticValidationPass(loggerFactory),
            new FormattingContentValidationPass(loggerFactory)
        ];

        _documentFormattingPasses = [
                new HtmlFormattingPass(documentMappingService, loggerFactory),
                new RazorFormattingPass(),
                new CSharpFormattingPass(hostServicesProvider, documentMappingService, loggerFactory),
            ];
        _formattingLoggerFactory = formattingLoggerFactory;
    }

    public async Task<ImmutableArray<TextChange>> GetDocumentFormattingChangesAsync(
        DocumentContext documentContext,
        ImmutableArray<TextChange> htmlChanges,
        LinePositionSpan? range,
        RazorFormattingOptions options,
        CancellationToken cancellationToken)
    {
        var codeDocument = await documentContext.Snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

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

        // Despite what it looks like, codeDocument.GetCSharpDocument().Diagnostics is actually the
        // Razor diagnostics, not the C# diagnostics 🤦‍
        var sourceText = codeDocument.Source.Text;
        if (range is { } span)
        {
            if (codeDocument.GetRequiredCSharpDocument().Diagnostics.Any(d => d.Span != SourceSpan.Undefined && span.OverlapsWith(sourceText.GetLinePositionSpan(d.Span))))
            {
                return [];
            }
        }

        var logger = _formattingLoggerFactory.CreateLogger(documentContext.Snapshot.FilePath, range is null ? "Full" : "Range");
        logger?.LogObject("FileKind", documentContext.Snapshot.FileKind);
        logger?.LogObject("Options", options);
        logger?.LogObject("HtmlChanges", htmlChanges.SelectAsArray(e => e.ToRazorTextChange()));
        logger?.LogObject("Range", range);
        logger?.LogSourceText("InitialDocument", sourceText);
        LogSyntaxTree(logger, codeDocument);

        var uri = documentContext.Uri;
        var documentSnapshot = documentContext.Snapshot;
        var hostDocumentVersion = documentContext.Snapshot.Version;
        var context = FormattingContext.Create(
            documentSnapshot,
            codeDocument,
            options,
            logger);
        var originalText = context.SourceText;

        var result = htmlChanges;
        foreach (var pass in _documentFormattingPasses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result = await pass.ExecuteAsync(context, result, cancellationToken).ConfigureAwait(false);
        }

        var filteredChanges = range is not { } linePositionSpan
            ? result
            : result.WhereAsArray(e => linePositionSpan.LineOverlapsWith(sourceText.GetLinePositionSpan(e.Span)));

        var normalizedChanges = NormalizeLineEndings(originalText, filteredChanges);

        foreach (var validationPass in _validationPasses)
        {
            var isValid = await validationPass.IsValidAsync(context, normalizedChanges, cancellationToken).ConfigureAwait(false);
            if (!isValid)
            {
                return [];
            }
        }

        return originalText.MinimizeTextChanges(normalizedChanges);
    }

    public async Task<ImmutableArray<TextChange>> GetCSharpOnTypeFormattingChangesAsync(DocumentContext documentContext, RazorFormattingOptions options, int hostDocumentIndex, char triggerCharacter, CancellationToken cancellationToken)
    {
        var documentSnapshot = documentContext.Snapshot;

        var codeDocument = await documentContext.Snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        return await ApplyFormattedChangesAsync(
                documentSnapshot,
                codeDocument,
                generatedDocumentChanges: [],
                options,
                hostDocumentIndex,
                triggerCharacter,
                _csharpOnTypeFormattingPass,
                collapseChanges: false,
                includeCSharpLanguageFeatureEdits: false,
                validate: true,
                formattingType: "CSharpOnType",
                cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<TextChange>> GetHtmlOnTypeFormattingChangesAsync(DocumentContext documentContext, ImmutableArray<TextChange> htmlChanges, RazorFormattingOptions options, int hostDocumentIndex, char triggerCharacter, CancellationToken cancellationToken)
    {
        var documentSnapshot = documentContext.Snapshot;

        // Html formatting doesn't use the C# design time document
        var codeDocument = await documentContext.Snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        return await ApplyFormattedChangesAsync(
                documentSnapshot,
                codeDocument,
                htmlChanges,
                options,
                hostDocumentIndex,
                triggerCharacter,
                _htmlOnTypeFormattingPass,
                collapseChanges: false,
                includeCSharpLanguageFeatureEdits: false,
                validate: true,
                formattingType: "HtmlOnType",
                cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<TextChange?> TryGetSingleCSharpEditAsync(DocumentContext documentContext, TextChange csharpEdit, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        var documentSnapshot = documentContext.Snapshot;
        // Since we've been provided with an edit from the C# generated doc, forcing design time would make things not line up
        var codeDocument = await documentContext.Snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var razorChanges = await ApplyFormattedChangesAsync(
            documentSnapshot,
            codeDocument,
            [csharpEdit],
            options,
            hostDocumentIndex: 0,
            triggerCharacter: '\0',
            _csharpOnTypeFormattingPass,
            collapseChanges: false,
            includeCSharpLanguageFeatureEdits: false,
            validate: true,
            formattingType: "SingleCSharpEdit",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return razorChanges is [{ } change]
            ? change
            : null;
    }

    public async Task<TextChange?> TryGetCSharpCodeActionEditAsync(DocumentContext documentContext, ImmutableArray<TextChange> csharpChanges, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        var documentSnapshot = documentContext.Snapshot;
        // Since we've been provided with edits from the C# generated doc, forcing design time would make things not line up
        var codeDocument = await documentContext.Snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var razorChanges = await ApplyFormattedChangesAsync(
            documentSnapshot,
            codeDocument,
            csharpChanges,
            options,
            hostDocumentIndex: 0,
            triggerCharacter: '\0',
            _csharpOnTypeFormattingPass,
            collapseChanges: true,
            includeCSharpLanguageFeatureEdits: true,
            validate: false,
            formattingType: "CSharpCodeAction",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return razorChanges is [{ } change]
            ? change
            : null;
    }

    public async Task<TextChange?> TryGetCSharpSnippetFormattingEditAsync(DocumentContext documentContext, ImmutableArray<TextChange> csharpChanges, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        csharpChanges = WrapCSharpSnippets(csharpChanges);

        var documentSnapshot = documentContext.Snapshot;
        // Since we've been provided with edits from the C# generated doc, forcing design time would make things not line up
        var codeDocument = await documentContext.Snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var razorChanges = await ApplyFormattedChangesAsync(
            documentSnapshot,
            codeDocument,
            csharpChanges,
            options,
            hostDocumentIndex: 0,
            triggerCharacter: '\0',
            _csharpOnTypeFormattingPass,
            collapseChanges: true,
            includeCSharpLanguageFeatureEdits: true,
            validate: false,
            formattingType: "CSharpSnippet",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        razorChanges = UnwrapCSharpSnippets(razorChanges);

        return razorChanges is [{ } change]
            ? change
            : null;
    }

    public bool TryGetOnTypeFormattingTriggerKind(RazorCodeDocument codeDocument, int hostDocumentIndex, string triggerCharacter, out RazorLanguageKind triggerCharacterKind)
    {
        triggerCharacterKind = codeDocument.GetLanguageKind(hostDocumentIndex, rightAssociative: false);

        return triggerCharacterKind switch
        {
            RazorLanguageKind.CSharp => s_csharpTriggerCharacterSet.Contains(triggerCharacter),
            RazorLanguageKind.Html => s_htmlTriggerCharacterSet.Contains(triggerCharacter),
            _ => false,
        };
    }

    private async Task<ImmutableArray<TextChange>> ApplyFormattedChangesAsync(
        IDocumentSnapshot documentSnapshot,
        RazorCodeDocument codeDocument,
        ImmutableArray<TextChange> generatedDocumentChanges,
        RazorFormattingOptions options,
        int hostDocumentIndex,
        char triggerCharacter,
        IFormattingPass formattingPass,
        bool collapseChanges,
        bool includeCSharpLanguageFeatureEdits,
        bool validate,
        string formattingType,
        CancellationToken cancellationToken)
    {
        // If we only received a single edit, let's always return a single edit back.
        // Otherwise, merge only if explicitly asked.
        collapseChanges |= generatedDocumentChanges.Length == 1;

        var logger = _formattingLoggerFactory.CreateLogger(documentSnapshot.FilePath, formattingType);
        logger?.LogObject("FileKind", documentSnapshot.FileKind);
        logger?.LogObject("Options", options);
        logger?.LogObject("Parameters", new { hostDocumentIndex, triggerCharacter, collapseChanges, includeCSharpLanguageFeatureEdits, validate });
        logger?.LogObject("GeneratedDocumentChanges", generatedDocumentChanges);
        logger?.LogSourceText("InitialDocument", codeDocument.Source.Text);
        LogSyntaxTree(logger, codeDocument);

        var context = FormattingContext.CreateForOnTypeFormatting(
            documentSnapshot,
            codeDocument,
            options,
            logger,
            includeCSharpLanguageFeatureEdits: includeCSharpLanguageFeatureEdits,
            hostDocumentIndex,
            triggerCharacter);

        var result = await formattingPass.ExecuteAsync(context, generatedDocumentChanges, cancellationToken).ConfigureAwait(false);
        var originalText = context.SourceText;
        result = NormalizeLineEndings(originalText, result);
        var razorChanges = originalText.MinimizeTextChanges(result);

        if (validate)
        {
            foreach (var validationPass in _validationPasses)
            {
                var isValid = await validationPass.IsValidAsync(context, razorChanges, cancellationToken).ConfigureAwait(false);
                if (!isValid)
                {
                    return [];
                }
            }
        }

        if (collapseChanges)
        {
            var collapsedEdit = MergeChanges(razorChanges, originalText);
            if (collapsedEdit.NewText is null or { Length: 0 } &&
                collapsedEdit.Span.IsEmpty)
            {
                return [];
            }

            return [collapsedEdit];
        }

        return razorChanges;
    }

    // Internal for testing
    internal static TextChange MergeChanges(ImmutableArray<TextChange> changes, SourceText sourceText)
    {
        if (changes.Length == 1)
        {
            return changes[0];
        }

        var changedText = sourceText.WithChanges(changes);
        var affectedRange = changedText.GetEncompassingTextChangeRange(sourceText);
        var spanBeforeChange = affectedRange.Span;
        var spanAfterChange = new TextSpan(spanBeforeChange.Start, affectedRange.NewLength);
        var newText = changedText.ToString(spanAfterChange);

        return new TextChange(spanBeforeChange, newText);
    }

    private static ImmutableArray<TextChange> WrapCSharpSnippets(ImmutableArray<TextChange> csharpChanges)
    {
        // Currently this method only supports wrapping `$0`, any additional markers aren't formatted properly.

        return ReplaceInChanges(csharpChanges, "$0", "/*$0*/");
    }

    private static ImmutableArray<TextChange> UnwrapCSharpSnippets(ImmutableArray<TextChange> razorChanges)
    {
        return ReplaceInChanges(razorChanges, "/*$0*/", "$0");
    }

    /// <summary>
    /// This method counts the occurrences of CRLF and LF line endings in the original text. 
    /// If LF line endings are more prevalent, it removes any CR characters from the text changes 
    /// to ensure consistency with the LF style.
    /// </summary>
    private static ImmutableArray<TextChange> NormalizeLineEndings(SourceText originalText, ImmutableArray<TextChange> changes)
    {
        if (originalText.HasLFLineEndings())
        {
            return ReplaceInChanges(changes, "\r", "");
        }

        return changes;
    }

    private static void LogSyntaxTree(IFormattingLogger? logger, RazorCodeDocument codeDocument)
    {
        if (logger is null)
        {
            return;
        }

        var syntaxRoot = (RazorSyntaxNode)codeDocument.GetRequiredTagHelperRewrittenSyntaxTree().Root;
        var serializedSyntaxTree = SyntaxSerializer.Default.Serialize(syntaxRoot);
        logger.LogSourceText("SyntaxTree", SourceText.From(serializedSyntaxTree));
    }

    private static ImmutableArray<TextChange> ReplaceInChanges(ImmutableArray<TextChange> csharpChanges, string toFind, string replacement)
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

    internal class TestAccessor(RazorFormattingService service)
    {
        public static FrozenSet<string> GetCSharpTriggerCharacterSet() => s_csharpTriggerCharacterSet;
        public static FrozenSet<string> GetHtmlTriggerCharacterSet() => s_htmlTriggerCharacterSet;

        public void SetDebugAssertsEnabled(bool debugAssertsEnabled)
        {
            var contentValidationPass = service._validationPasses.OfType<FormattingContentValidationPass>().Single();
            contentValidationPass.DebugAssertsEnabled = debugAssertsEnabled;
        }

        public void SetFormattingLoggerFactory(IFormattingLoggerFactory factory)
        {
            service._formattingLoggerFactory = factory;
        }
    }
}
