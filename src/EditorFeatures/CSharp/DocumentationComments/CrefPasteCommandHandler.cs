// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments;

/// <summary>
/// Converts <c>&lt;</c>/<c>&gt;</c> to <c>{</c>/<c>}</c> when pasting into a <c>cref</c> attribute value.
/// Applied as a separate edit so undo restores the original paste.
/// </summary>
[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.CSharpContentType)]
[Name(PredefinedCommandHandlerNames.CrefPaste)]
[Order(After = PredefinedCommandHandlerNames.FormatDocument)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CrefPasteCommandHandler(
    IThreadingContext threadingContext,
    ITextUndoHistoryRegistry undoHistoryRegistry,
    IEditorOperationsFactoryService editorOperationsFactoryService) : IChainedCommandHandler<PasteCommandArgs>
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly ITextUndoHistoryRegistry _undoHistoryRegistry = undoHistoryRegistry;
    private readonly IEditorOperationsFactoryService _editorOperationsFactoryService = editorOperationsFactoryService;

    public string DisplayName => nameof(CrefPasteCommandHandler);

    public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextCommandHandler)
        => nextCommandHandler();

    public void ExecuteCommand(PasteCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
    {
        Contract.ThrowIfFalse(_threadingContext.HasMainThread);

        var subjectBuffer = args.SubjectBuffer;
        var snapshotBeforePaste = subjectBuffer.CurrentSnapshot;

        nextCommandHandler();

        var changes = snapshotBeforePaste.Version.Changes;
        if (changes is null || changes.Count == 0)
            return;

        // Bail if another component also edited the buffer.
        var snapshotAfterPaste = subjectBuffer.CurrentSnapshot;
        if (snapshotAfterPaste.Version != snapshotBeforePaste.Version.Next)
            return;

        var hasGenericBrackets = false;
        foreach (var change in changes)
        {
            if (change.NewText.IndexOfAny(['<', '>']) >= 0)
            {
                hasGenericBrackets = true;
                break;
            }
        }

        if (!hasGenericBrackets)
            return;

        var documentBeforePaste = snapshotBeforePaste.GetOpenDocumentInCurrentContextWithChanges();
        if (documentBeforePaste is null)
            return;

        var cancellationToken = executionContext.OperationContext.UserCancellationToken;
        var rootBeforePaste = documentBeforePaste.GetRequiredSyntaxRootSynchronously(cancellationToken);

        foreach (var change in changes)
        {
            if (!IsInCrefAttributeValue(rootBeforePaste, change.OldSpan) ||
                change.NewText.IndexOfAny(['"', '\'', '\r', '\n']) >= 0)
            {
                return;
            }
        }

        using var transaction = new CaretPreservingEditTransaction(
            CSharpEditorResources.Fixing_cref_after_paste,
            args.TextView, _undoHistoryRegistry, _editorOperationsFactoryService);

        // Same-length replacement, so the caret doesn't move.
        var edit = subjectBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null);
        foreach (var change in changes)
            edit.Replace(change.NewSpan, change.NewText.Replace('<', '{').Replace('>', '}'));

        edit.Apply();
        transaction.Complete();
    }

    private static bool IsInCrefAttributeValue(SyntaxNode root, Span span)
    {
        var token = root.FindToken(span.Start, findInsideTrivia: true);
        var crefAttribute = token.GetAncestor<XmlCrefAttributeSyntax>();
        if (crefAttribute is null || crefAttribute.EndQuoteToken.IsMissing)
            return false;

        var valueSpan = TextSpan.FromBounds(crefAttribute.StartQuoteToken.Span.End, crefAttribute.EndQuoteToken.SpanStart);
        return valueSpan.Contains(TextSpan.FromBounds(span.Start, span.End));
    }
}
