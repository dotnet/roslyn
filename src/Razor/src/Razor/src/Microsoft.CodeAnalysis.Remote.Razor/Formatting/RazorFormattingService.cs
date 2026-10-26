// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.Formatting;

[Export(typeof(IRazorFormattingService)), Shared]
internal sealed class RazorFormattingService : IRazorFormattingService
{
    private static readonly FrozenSet<string> s_csharpTriggerCharacterSet = FrozenSet.ToFrozenSet(["}", ";"], StringComparer.Ordinal);
    private static readonly FrozenSet<string> s_htmlTriggerCharacterSet = FrozenSet.ToFrozenSet(["\n", "{", "}", ";"], StringComparer.Ordinal);

    private readonly ImmutableArray<IFormattingPass> _documentFormattingPasses;
    private readonly FormattingEngine _formattingEngine;
    private readonly CSharpOnTypeFormattingPass _csharpOnTypeFormattingPass;
    private readonly HtmlOnTypeFormattingPass _htmlOnTypeFormattingPass;

    private IFormattingLoggerFactory _formattingLoggerFactory;

    [ImportingConstructor]
    public RazorFormattingService(
        IDocumentMappingService documentMappingService,
        IRazorEditService razorEditService,
        IHostServicesProvider hostServicesProvider,
        IFormattingLoggerFactory formattingLoggerFactory,
        ILoggerFactory loggerFactory)
    {
        _htmlOnTypeFormattingPass = new HtmlOnTypeFormattingPass();
        _csharpOnTypeFormattingPass = new CSharpOnTypeFormattingPass(documentMappingService, razorEditService, hostServicesProvider, loggerFactory);
        _documentFormattingPasses =
        [
            new HtmlFormattingPass(loggerFactory),
            new RazorFormattingPass(),
            new CSharpFormattingPass(hostServicesProvider, loggerFactory)
        ];
        _formattingEngine = new FormattingEngine(loggerFactory);
        _formattingLoggerFactory = formattingLoggerFactory;
    }

    public async Task<ImmutableArray<TextChange>> GetDocumentFormattingChangesAsync(
        RemoteDocumentSnapshot documentSnapshot,
        ImmutableArray<TextChange> htmlChanges,
        LinePositionSpan? range,
        RazorFormattingOptions options,
        CancellationToken cancellationToken)
    {
        var logger = _formattingLoggerFactory.CreateLogger(documentSnapshot.FilePath, range is null ? "Full" : "Range");
        var context = FormattingContext.Create(
            documentSnapshot,
            await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false),
            options,
            logger);

        return await _formattingEngine.FormatDocumentAsync(
            context, _documentFormattingPasses, htmlChanges, range, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<TextChange>> GetCSharpOnTypeFormattingChangesAsync(RemoteDocumentSnapshot documentSnapshot, RazorFormattingOptions options, int hostDocumentIndex, char triggerCharacter, bool declarationDocument, CancellationToken cancellationToken)
    {

        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        return await ApplyFormattedChangesAsync(
                documentSnapshot,
                codeDocument,
                declarationDocument,
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

    public async Task<ImmutableArray<TextChange>> GetHtmlOnTypeFormattingChangesAsync(RemoteDocumentSnapshot documentSnapshot, ImmutableArray<TextChange> htmlChanges, RazorFormattingOptions options, int hostDocumentIndex, char triggerCharacter, CancellationToken cancellationToken)
    {

        // Html formatting doesn't use the C# design time document
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        return await ApplyFormattedChangesAsync(
                documentSnapshot,
                codeDocument,
                declarationDocument: null,
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

    public async Task<TextChange?> TryGetSingleCSharpEditAsync(RemoteDocumentSnapshot documentSnapshot, TextChange csharpEdit, bool declarationDocument, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        // Since we've been provided with an edit from the C# generated doc, forcing design time would make things not line up
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var razorChanges = await ApplyFormattedChangesAsync(
            documentSnapshot,
            codeDocument,
            declarationDocument,
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

    public async Task<TextChange?> TryGetCSharpCodeActionEditAsync(RemoteDocumentSnapshot documentSnapshot, ImmutableArray<TextChange> csharpChanges, bool declarationDocument, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        // Since we've been provided with edits from the C# generated doc, forcing design time would make things not line up
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var razorChanges = await ApplyFormattedChangesAsync(
            documentSnapshot,
            codeDocument,
            declarationDocument,
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

    public async Task<TextChange?> TryGetCSharpSnippetFormattingEditAsync(RemoteDocumentSnapshot documentSnapshot, ImmutableArray<TextChange> csharpChanges, bool declarationDocument, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        csharpChanges = WrapCSharpSnippets(csharpChanges);
        // Since we've been provided with edits from the C# generated doc, forcing design time would make things not line up
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var razorChanges = await ApplyFormattedChangesAsync(
            documentSnapshot,
            codeDocument,
            declarationDocument,
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
        RemoteDocumentSnapshot documentSnapshot,
        RazorCodeDocument codeDocument,
        bool? declarationDocument,
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
        logger?.LogObject("Parameters", new { hostDocumentIndex, triggerCharacter, collapseChanges, includeCSharpLanguageFeatureEdits, validate, declarationDocument });
        logger?.LogObject("GeneratedDocumentChanges", generatedDocumentChanges);
        logger?.LogSourceText("InitialDocument", codeDocument.Source.Text);
        FormattingEngine.LogSyntaxTree(logger, codeDocument);

        var context = FormattingContext.CreateForOnTypeFormatting(
            documentSnapshot,
            codeDocument,
            declarationDocument,
            options,
            logger,
            includeCSharpLanguageFeatureEdits: includeCSharpLanguageFeatureEdits,
            hostDocumentIndex,
            triggerCharacter);

        var result = await formattingPass.ExecuteAsync(context, generatedDocumentChanges, cancellationToken).ConfigureAwait(false);
        var originalText = context.SourceText;
        result = FormattingEngine.NormalizeLineEndings(originalText, result);
        var razorChanges = originalText.MinimizeTextChanges(result);

        if (validate && !await _formattingEngine.ValidateAsync(context, razorChanges, cancellationToken).ConfigureAwait(false))
        {
            return [];
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

        return FormattingEngine.ReplaceInChanges(csharpChanges, "$0", "/*$0*/");
    }

    private static ImmutableArray<TextChange> UnwrapCSharpSnippets(ImmutableArray<TextChange> razorChanges)
    {
        return FormattingEngine.ReplaceInChanges(razorChanges, "/*$0*/", "$0");
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal class TestAccessor(RazorFormattingService service)
    {
        public static FrozenSet<string> GetCSharpTriggerCharacterSet() => s_csharpTriggerCharacterSet;
        public static FrozenSet<string> GetHtmlTriggerCharacterSet() => s_htmlTriggerCharacterSet;

        public void SetDebugAssertsEnabled(bool debugAssertsEnabled)
            => service._formattingEngine.GetTestAccessor().SetDebugAssertsEnabled(debugAssertsEnabled);

        public void SetFormattingLoggerFactory(IFormattingLoggerFactory factory)
        {
            service._formattingLoggerFactory = factory;
        }
    }
}
