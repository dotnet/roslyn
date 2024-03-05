// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal abstract class AbstractXmlTagCompletionCommandHandler<
    TXmlNameSyntax,
    TXmlTextSyntax,
    TXmlElementSyntax,
    TXmlElementStartTagSyntax,
    TXmlElementEndTagSyntax,
    TDocumentationCommentTriviaSyntax>
    (ITextUndoHistoryRegistry undoHistory) : IChainedCommandHandler<TypeCharCommandArgs>
    where TXmlNameSyntax : SyntaxNode
    where TXmlTextSyntax : SyntaxNode
    where TXmlElementSyntax : SyntaxNode
    where TXmlElementStartTagSyntax : SyntaxNode
    where TXmlElementEndTagSyntax : SyntaxNode
    where TDocumentationCommentTriviaSyntax : SyntaxNode
{
    private readonly ITextUndoHistoryRegistry _undoHistory = undoHistory;

    public string DisplayName => EditorFeaturesResources.XML_End_Tag_Completion;

    protected abstract TXmlElementStartTagSyntax GetStartTag(TXmlElementSyntax xmlElement);
    protected abstract TXmlElementEndTagSyntax GetEndTag(TXmlElementSyntax xmlElement);
    protected abstract TXmlNameSyntax GetName(TXmlElementStartTagSyntax startTag);
    protected abstract TXmlNameSyntax GetName(TXmlElementEndTagSyntax endTag);
    protected abstract SyntaxToken GetLocalName(TXmlNameSyntax name);

    public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
        => nextHandler();

    public void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
    {
        // Ensure completion and any other buffer edits happen first.
        nextHandler();

        var cancellationToken = context.OperationContext.UserCancellationToken;
        if (cancellationToken.IsCancellationRequested)
            return;

        try
        {
            ExecuteCommandWorker(args, context);
        }
        catch (OperationCanceledException)
        {
            // According to Editor command handler API guidelines, it's best if we return early if cancellation
            // is requested instead of throwing. Otherwise, we could end up in an invalid state due to already
            // calling nextHandler().
        }
    }

    private void ExecuteCommandWorker(TypeCharCommandArgs args, CommandExecutionContext context)
    {
        if (args.TypedChar is not '>' and not '/')
            return;

        using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Completing_Tag))
        {
            var buffer = args.SubjectBuffer;

            var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return;

            // We actually want the caret position after any operations
            var position = args.TextView.GetCaretPoint(args.SubjectBuffer);

            // No caret position? No edit!
            if (!position.HasValue)
                return;

            TryCompleteTag(args.TextView, args.SubjectBuffer, document, position.Value, context.OperationContext.UserCancellationToken);
        }
    }

    protected void InsertTextAndMoveCaret(ITextView textView, ITextBuffer subjectBuffer, SnapshotPoint position, string insertionText, int? finalCaretPosition)
    {
        using var transaction = _undoHistory.GetHistory(textView.TextBuffer).CreateTransaction("XmlTagCompletion");

        subjectBuffer.Insert(position, insertionText);

        if (finalCaretPosition.HasValue)
        {
            var point = subjectBuffer.CurrentSnapshot.GetPoint(finalCaretPosition.Value);
            textView.TryMoveCaretToAndEnsureVisible(point);
        }

        transaction.Complete();
    }

    private SyntaxToken GetLocalName(TXmlElementStartTagSyntax startTag)
        => GetLocalName(GetName(startTag));

    private SyntaxToken GetLocalName(TXmlElementEndTagSyntax startTag)
        => GetLocalName(GetName(startTag));

    private void TryCompleteTag(ITextView textView, ITextBuffer subjectBuffer, Document document, SnapshotPoint position, CancellationToken cancellationToken)
    {
        var tree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);
        var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments: true);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var syntaxKinds = syntaxFacts.SyntaxKinds;

        var parentTrivia = token.GetAncestor<TDocumentationCommentTriviaSyntax>();
        if (parentTrivia is null)
            return;

        if (token.RawKind == syntaxKinds.GreaterThanToken)
        {
            if (token.Parent is not TXmlElementStartTagSyntax parentStartTag ||
                parentStartTag.Parent is not TXmlElementSyntax parentElement)
            {
                return;
            }

            // Slightly special case: <blah><blah$$</blah>
            // If we already have a matching end tag and we're parented by 
            // an xml element with the same start tag and a missing/non-matching end tag, 
            // do completion anyway. Generally, if this is the case, we have to walk
            // up the parent elements until we find an unmatched start tag.

            if (GetLocalName(parentStartTag).ValueText.Length > 0 && HasMatchingEndTag(parentElement))
            {
                if (HasUnmatchedIdenticalParent(parentElement))
                {
                    InsertTextAndMoveCaret(textView, subjectBuffer, position, "</" + GetLocalName(parentStartTag).ValueText + ">", position);
                    return;
                }
            }

            CheckNameAndInsertText(textView, subjectBuffer, position, parentElement, position.Position, "</{0}>");
        }
        else if (token.RawKind == syntaxKinds.LessThanSlashToken)
        {
            // /// <summary>
            // /// </$$
            // /// </summary>
            // We need to check for non-trivia XML text tokens after $$ that match the expected end tag text.

            if (token.Parent is TXmlElementEndTagSyntax { Parent: TXmlElementSyntax parentElement })
            {
                var startTag = GetStartTag(parentElement);
                if (startTag != null &&
                    !HasFollowingEndTagTrivia(startTag, token))
                {
                    CheckNameAndInsertText(textView, subjectBuffer, position, parentElement, null, "{0}>");
                }
            }
        }
    }

    private bool HasFollowingEndTagTrivia(
        TXmlElementStartTagSyntax startTag,
        SyntaxToken lessThanSlashToken)
    {
        var tagName = GetLocalName(startTag).ValueText;
        var expectedEndTagText = "</" + tagName + ">";

        var token = lessThanSlashToken.GetNextToken(includeDocumentationComments: true);
        while (token.Parent is TXmlTextSyntax)
        {
            if (token.ValueText == expectedEndTagText)
                return true;

            token = token.GetNextToken(includeDocumentationComments: true);
        }

        if (token.Parent is TXmlElementEndTagSyntax endTag &&
            GetLocalName(endTag).ValueText == tagName)
        {
            return true;
        }

        return false;
    }

    private bool HasUnmatchedIdenticalParent(TXmlElementSyntax parentElement)
    {
        if (parentElement.Parent is TXmlElementSyntax grandParentElement)
        {
            var parentStartTag = GetStartTag(parentElement);
            if (GetLocalName(GetStartTag(grandParentElement)).ValueText == GetLocalName(parentStartTag).ValueText)
            {
                if (HasMatchingEndTag(grandParentElement))
                {
                    return HasUnmatchedIdenticalParent(grandParentElement);
                }

                return true;
            }
        }

        return false;
    }

    private bool HasMatchingEndTag(TXmlElementSyntax parentElement)
    {
        var startTag = GetStartTag(parentElement);
        var endTag = GetEndTag(parentElement);
        return endTag != null &&
            !endTag.IsMissing &&
            GetLocalName(endTag).ValueText == GetLocalName(startTag).ValueText;
    }

    private void CheckNameAndInsertText(
        ITextView textView,
        ITextBuffer subjectBuffer,
        SnapshotPoint position,
        TXmlElementSyntax parentElement,
        int? finalCaretPosition,
        string formatString)
    {
        var startTag = GetStartTag(parentElement);
        var endTag = GetEndTag(parentElement);
        if (startTag is null || endTag is null)
            return;

        var elementName = GetLocalName(startTag).ValueText;

        if (elementName.Length > 0 &&
            GetLocalName(endTag).ValueText != elementName)
        {
            InsertTextAndMoveCaret(textView, subjectBuffer, position, string.Format(formatString, elementName), finalCaretPosition);
        }
    }
}
