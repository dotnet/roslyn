// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.StringCopyPaste;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.StringCopyPaste;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSUtilities = Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste;

using static StringCopyPasteHelpers;

/// <summary>
/// Command handler that both tracks 'copy' commands within VS to see what text the user copied (and from where),
/// but also then handles pasting that text back in a sensible fashion (e.g. escaping/unescaping/wrapping/indenting)
/// inside a string-literal.  Can also handle pasting code from unknown sources as well, though heuristics must be
/// applied in that case to make a best effort guess as to what the original text meant and how to preserve that
/// in the final context.
/// </summary>
/// <remarks>
/// Because we are revising what the normal editor does, we follow the standard behavior of first allowing the
/// editor to process paste commands, and then adding our own changes as an edit after that.  That way if the user
/// doesn't want the change we made, they can always undo to get the prior paste behavior.
/// </remarks>
[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.CSharpContentType)]
[Name(PredefinedCommandHandlerNames.StringCopyPaste)]
[Order(After = PredefinedCommandHandlerNames.FormatDocument)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal partial class StringCopyPasteCommandHandler(
    IThreadingContext threadingContext,
    ITextUndoHistoryRegistry undoHistoryRegistry,
    IEditorOperationsFactoryService editorOperationsFactoryService,
    IGlobalOptionService globalOptions,
    ITextBufferFactoryService2 textBufferFactoryService,
    EditorOptionsService editorOptionsService,
    IIndentationManagerService indentationManager) :
    IChainedCommandHandler<CutCommandArgs>,
    IChainedCommandHandler<CopyCommandArgs>,
    IChainedCommandHandler<PasteCommandArgs>
{
    private const string CopyId = "RoslynStringCopyPasteId";

    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly ITextUndoHistoryRegistry _undoHistoryRegistry = undoHistoryRegistry;
    private readonly IEditorOperationsFactoryService _editorOperationsFactoryService = editorOperationsFactoryService;
    private readonly EditorOptionsService _editorOptionsService = editorOptionsService;
    private readonly IIndentationManagerService _indentationManager = indentationManager;
    private readonly IGlobalOptionService _globalOptions = globalOptions;
    private readonly ITextBufferFactoryService2 _textBufferFactoryService = textBufferFactoryService;

    public string DisplayName => nameof(StringCopyPasteCommandHandler);

    public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextCommandHandler)
        => nextCommandHandler();

    public void ExecuteCommand(PasteCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
    {
        Contract.ThrowIfFalse(_threadingContext.HasMainThread);

        var textView = args.TextView;
        var subjectBuffer = args.SubjectBuffer;

        var selectionsBeforePaste = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);
        var snapshotBeforePaste = subjectBuffer.CurrentSnapshot;

        // Always let the real paste go through.  That way we always have a version of the document that doesn't
        // include our changes that we can undo back to.
        nextCommandHandler();

        // If we don't even see any changes from the paste, there's nothing we can do.
        if (snapshotBeforePaste.Version.Changes is null)
            return;

        // If the user has the option off, then don't bother doing anything once we've sent the paste through.
        if (!_globalOptions.GetOption(StringCopyPasteOptionsStorage.AutomaticallyFixStringContentsOnPaste, LanguageNames.CSharp))
            return;

        // if we're not even sure where the user caret/selection is on this buffer, we can't proceed.
        if (selectionsBeforePaste.Count == 0)
            return;

        var snapshotAfterPaste = subjectBuffer.CurrentSnapshot;

        // If there were multiple changes that already happened, then don't make any changes.  Some other component
        // already did something advanced.
        if (snapshotAfterPaste.Version != snapshotBeforePaste.Version.Next)
            return;

        // Have to even be in a C# doc to be able to have special space processing here.

        var documentBeforePaste = snapshotBeforePaste.GetOpenDocumentInCurrentContextWithChanges();
        var documentAfterPaste = snapshotAfterPaste.GetOpenDocumentInCurrentContextWithChanges();
        if (documentBeforePaste == null || documentAfterPaste == null)
            return;

        var cancellationToken = executionContext.OperationContext.UserCancellationToken;
        var parsedDocumentBeforePaste = ParsedDocument.CreateSynchronously(documentBeforePaste, cancellationToken);

        // When pasting, only do anything special if the user selections were entirely inside a single string
        // token/expression.  Otherwise, we have a multi-selection across token kinds which will be extremely
        // complex to try to reconcile.
        var stringExpressionBeforePaste = TryGetCompatibleContainingStringExpression(parsedDocumentBeforePaste, selectionsBeforePaste);
        if (stringExpressionBeforePaste == null)
            return;

        // Also ensure that all the changes the editor actually applied were inside a single string
        // token/expression. If the editor decided to make changes outside of the string, we definitely do not want
        // to do anything here.
        var stringExpressionBeforePasteFromChanges = TryGetCompatibleContainingStringExpression(
            parsedDocumentBeforePaste, new NormalizedSnapshotSpanCollection(snapshotBeforePaste, snapshotBeforePaste.Version.Changes.Select(c => c.OldSpan)));
        if (stringExpressionBeforePaste != stringExpressionBeforePasteFromChanges)
            return;

        var textChanges = GetEdits(cancellationToken);

        // If we didn't get any viable changes back, don't do anything.
        if (textChanges.IsDefaultOrEmpty)
            return;

        var newTextAfterChanges = snapshotBeforePaste.AsText().WithChanges(textChanges);

        // If we end up making the same changes as what the paste did, then no need to proceed.
        if (ContentsAreSame(snapshotBeforePaste, snapshotAfterPaste, stringExpressionBeforePaste, newTextAfterChanges))
            return;

        // Create two edits to make the change.  The first restores the buffer to the original snapshot (effectively
        // undoing the first set of changes).  Then the second actually applies the change.
        //
        // Do this as direct edits, passing 'EditOptions.None' for the options, as we want to control the edits
        // precisely and don't want any strange interpretation of where the caret should end up.  Other options
        // (like DefaultMinimalChange) will attempt to diff/merge edits oddly sometimes which can lead the caret
        // ending up before/after some merged change, which will no longer match the behavior of precise pastes.
        //
        // Wrap this all as a transaction so that these two edits appear to be one single change.  This also allows
        // the user to do a single 'undo' that gets them back to the original paste made at the start of this
        // method.

        using var transaction = new CaretPreservingEditTransaction(
            CSharpEditorResources.Fixing_string_literal_after_paste,
            textView, _undoHistoryRegistry, _editorOperationsFactoryService);

        {
            var edit = subjectBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null);
            foreach (var change in snapshotBeforePaste.Version.Changes)
                edit.Replace(change.NewSpan, change.OldText);
            edit.Apply();
        }

        {
            var edit = subjectBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null);
            foreach (var selection in selectionsBeforePaste)
                edit.Replace(selection.Span, "");

            foreach (var change in textChanges)
                edit.Replace(change.Span.ToSpan(), change.NewText);
            edit.Apply();
        }

        transaction.Complete();
        return;

        ImmutableArray<TextChange> GetEdits(CancellationToken cancellationToken)
        {
            var newLine = textView.Options.GetNewLineCharacter();
            var indentationWhitespace = DetermineIndentationWhitespace(
                parsedDocumentBeforePaste, subjectBuffer, snapshotBeforePaste.AsText(), stringExpressionBeforePaste, cancellationToken);

            // See if this is a paste of the last copy that we heard about.
            var edits = TryGetEditsFromKnownCopySource(newLine, indentationWhitespace);
            if (!edits.IsDefaultOrEmpty)
                return edits;

            var pasteWasSuccessful = PasteWasSuccessful(
                snapshotBeforePaste, snapshotAfterPaste, documentAfterPaste, stringExpressionBeforePaste, cancellationToken);

            // If not, then just go through the fallback code path that applies more heuristics.
            var unknownPasteProcessor = new UnknownSourcePasteProcessor(
                newLine, indentationWhitespace,
                snapshotBeforePaste, snapshotAfterPaste,
                documentBeforePaste, documentAfterPaste,
                stringExpressionBeforePaste, pasteWasSuccessful);
            return unknownPasteProcessor.GetEdits();
        }

        ImmutableArray<TextChange> TryGetEditsFromKnownCopySource(
            string newLine, string indentationWhitespace)
        {
            // For simplicity, we only support smart copy/paste when we are pasting into a single contiguous region.
            if (selectionsBeforePaste.Count != 1)
                return default;

            var copyPasteService = documentBeforePaste.Project.Solution.Services.GetRequiredService<IStringCopyPasteService>();
            var clipboardData = copyPasteService.TryGetClipboardData(KeyAndVersion);
            var copyPasteData = StringCopyPasteData.FromJson(clipboardData);

            if (copyPasteData == null)
                return default;

            var knownProcessor = new KnownSourcePasteProcessor(
                newLine, indentationWhitespace,
                snapshotBeforePaste, snapshotAfterPaste,
                documentBeforePaste, documentAfterPaste,
                stringExpressionBeforePaste,
                selectionsBeforePaste[0].Span.ToTextSpan(),
                copyPasteData, _textBufferFactoryService);
            return knownProcessor.GetEdits();
        }
    }

    private string DetermineIndentationWhitespace(
        ParsedDocument documentBeforePaste,
        ITextBuffer textBuffer,
        SourceText textBeforePaste,
        ExpressionSyntax stringExpressionBeforePaste,
        CancellationToken cancellationToken)
    {
        // Only raw strings care about indentation.  Don't bother computing if we don't need it.
        if (!IsAnyRawStringExpression(stringExpressionBeforePaste))
            return "";

        if (IsAnyMultiLineRawStringExpression(stringExpressionBeforePaste))
        {
            // already have a multi-line raw string.  The indentation of it's end delimiter is the indentation all
            // lines within it should have.
            var lastLine = textBeforePaste.Lines.GetLineFromPosition(stringExpressionBeforePaste.Span.End);
            var quotePosition = lastLine.GetFirstNonWhitespacePosition()!.Value;
            return textBeforePaste.ToString(TextSpan.FromBounds(lastLine.Span.Start, quotePosition));
        }

        // Otherwise, we have a single-line raw string.  Determine the default indentation desired here.
        // We'll use that if we have to convert this single-line raw string to a multi-line one.
        var indentationOptions = textBuffer.GetIndentationOptions(_editorOptionsService, documentBeforePaste.LanguageServices, explicitFormat: false);
        return stringExpressionBeforePaste.GetFirstToken().GetPreferredIndentation(documentBeforePaste, indentationOptions, cancellationToken);
    }

    /// <summary>
    /// Returns true if the paste resulted in legal code for the string literal.  The string literal is
    /// considered legal if it has the same span as the original string (adjusted as per the edit) and that
    /// there are no errors in it.  For this purposes of this check, errors in interpolation holes are not
    /// considered.  We only care about the textual content of the string.
    /// </summary>
    internal static bool PasteWasSuccessful(
        ITextSnapshot snapshotBeforePaste,
        ITextSnapshot snapshotAfterPaste,
        Document documentAfterPaste,
        ExpressionSyntax stringExpressionBeforePaste,
        CancellationToken cancellationToken)
    {
        var rootAfterPaste = documentAfterPaste.GetRequiredSyntaxRootSynchronously(cancellationToken);
        var stringExpressionAfterPaste = FindContainingSupportedStringExpression(rootAfterPaste, stringExpressionBeforePaste.SpanStart);
        if (stringExpressionAfterPaste == null)
            return false;

        if (ContainsError(stringExpressionAfterPaste))
            return false;

        var spanAfterPaste = MapSpan(stringExpressionBeforePaste.Span, snapshotBeforePaste, snapshotAfterPaste);
        return spanAfterPaste == stringExpressionAfterPaste.Span;
    }

    /// <summary>
    /// Given the snapshots before/after pasting, and the source-text our manual fixup edits produced, see if our
    /// manual application actually produced the same results as the paste.  If so, we don't need to actually do
    /// anything.  To optimize this check, we pass in the original string expression as that's all we have to check
    /// (adjusting for where it now ends up) in both the 'after' documents.
    /// </summary>
    private static bool ContentsAreSame(
        ITextSnapshot snapshotBeforePaste,
        ITextSnapshot snapshotAfterPaste,
        ExpressionSyntax stringExpressionBeforePaste,
        SourceText newTextAfterChanges)
    {
        // We ended up with documents of different length after we escaped/manipulated the pasted text.  So the 
        // contents are definitely not the same.
        if (newTextAfterChanges.Length != snapshotAfterPaste.Length)
            return false;

        var spanAfterPaste = MapSpan(stringExpressionBeforePaste.Span, snapshotBeforePaste, snapshotAfterPaste);

        var originalStringContentsAfterPaste = snapshotAfterPaste.AsText().GetSubText(spanAfterPaste);
        var newStringContentsAfterEdit = newTextAfterChanges.GetSubText(spanAfterPaste);

        return originalStringContentsAfterPaste.ContentEquals(newStringContentsAfterEdit);
    }

    /// <summary>
    /// Returns the <see cref="LiteralExpressionSyntax"/> or <see cref="InterpolatedStringExpressionSyntax"/> if the
    /// selections were all contained within a single literal in a compatible fashion.  This means all the
    /// selections have to start/end in a content-span portion of the literal.  For example, if we paste into an
    /// interpolated string and have half of the selection outside an interpolation and half inside, we don't do
    /// anything special as trying to correct in this scenario is too difficult.
    /// </summary>
    private static ExpressionSyntax? TryGetCompatibleContainingStringExpression(
        ParsedDocument document, NormalizedSnapshotSpanCollection spans)
    {
        if (spans.Count == 0)
            return null;

        var snapshot = spans[0].Snapshot;

        // First, try to see if all the selections are at least contained within a single string literal expression.
        var stringExpression = FindCommonContainingStringExpression(document.Root, spans);
        if (stringExpression == null)
            return null;

        // Now, given that string expression, find the inside 'text' spans of the expression.  These are the parts
        // of the literal between the quotes.  It does not include the interpolation holes in an interpolated
        // string.  These spans may be empty (for an empty string, or empty text gap between interpolations).
        var contentSpans = StringInfo.GetStringInfo(snapshot.AsText(), stringExpression).ContentSpans;
        foreach (var snapshotSpan in spans)
        {
            var startIndex = contentSpans.BinarySearch(snapshotSpan.Span.Start, FindIndex);
            var endIndex = contentSpans.BinarySearch(snapshotSpan.Span.End, FindIndex);

            if (startIndex < 0 || endIndex < 0)
                return null;
        }

        return stringExpression;

        static int FindIndex(TextSpan span, int position)
        {
            if (span.IntersectsWith(position))
                return 0;

            if (span.End < position)
                return -1;

            return 1;
        }
    }
}
