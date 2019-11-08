// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.DocumentationComments;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(XmlTagCompletionCommandHandler))]
    [Order(Before = PredefinedCompletionNames.CompletionCommandHandler)]
    internal class XmlTagCompletionCommandHandler : AbstractXmlTagCompletionCommandHandler
    {
        [ImportingConstructor]
        public XmlTagCompletionCommandHandler(ITextUndoHistoryRegistry undoHistory)
            : base(undoHistory)
        {
        }

        protected override void TryCompleteTag(ITextView textView, ITextBuffer subjectBuffer, Document document, SnapshotPoint position, CancellationToken cancellationToken)
        {
            var tree = document.GetSyntaxTreeSynchronously(cancellationToken);
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments: true);

            if (token.IsKind(SyntaxKind.GreaterThanToken))
            {
                if (!(token.Parent is XmlElementStartTagSyntax parentStartTag))
                {
                    return;
                }

                // Slightly special case: <blah><blah$$</blah>
                // If we already have a matching end tag and we're parented by 
                // an xml element with the same start tag and a missing/non-matching end tag, 
                // do completion anyway. Generally, if this is the case, we have to walk
                // up the parent elements until we find an unmatched start tag.

                if (parentStartTag.Name.LocalName.ValueText.Length > 0 && HasMatchingEndTag(parentStartTag))
                {
                    if (HasUnmatchedIdenticalParent(parentStartTag))
                    {
                        InsertTextAndMoveCaret(textView, subjectBuffer, position, "</" + parentStartTag.Name.LocalName.ValueText + ">", position);
                        return;
                    }
                }

                CheckNameAndInsertText(textView, subjectBuffer, position, parentStartTag, position.Position, "</{0}>");
            }
            else if (token.IsKind(SyntaxKind.LessThanSlashToken))
            {
                // /// <summary>
                // /// </$$
                // /// </summary>
                // We need to check for non-trivia XML text tokens after $$ that match the expected end tag text.

                if (token.Parent.IsKind(SyntaxKind.XmlElementEndTag) &&
                    token.Parent.IsParentKind(SyntaxKind.XmlElement))
                {
                    var parentElement = token.Parent.Parent as XmlElementSyntax;

                    if (!HasFollowingEndTagTrivia(parentElement, token))
                    {
                        CheckNameAndInsertText(textView, subjectBuffer, position, parentElement.StartTag, null, "{0}>");
                    }
                }
            }
        }

        private bool HasFollowingEndTagTrivia(XmlElementSyntax parentElement, SyntaxToken lessThanSlashToken)
        {
            var expectedEndTagText = "</" + parentElement.StartTag.Name.LocalName.ValueText + ">";

            var token = lessThanSlashToken.GetNextToken(includeDocumentationComments: true);
            while (token.Parent.IsKind(SyntaxKind.XmlText))
            {
                if (token.ValueText == expectedEndTagText)
                {
                    return true;
                }

                token = token.GetNextToken(includeDocumentationComments: true);
            }

            return false;
        }

        private bool HasUnmatchedIdenticalParent(XmlElementStartTagSyntax parentStartTag)
        {
            if (parentStartTag.Parent.Parent is XmlElementSyntax grandParentElement)
            {
                if (grandParentElement.StartTag.Name.LocalName.ValueText == parentStartTag.Name.LocalName.ValueText)
                {
                    if (HasMatchingEndTag(grandParentElement.StartTag))
                    {
                        return HasUnmatchedIdenticalParent(grandParentElement.StartTag);
                    }

                    return true;
                }
            }

            return false;
        }

        private bool HasMatchingEndTag(XmlElementStartTagSyntax parentStartTag)
        {
            if (parentStartTag == null)
            {
                return false;
            }

            if (!(parentStartTag.Parent is XmlElementSyntax parentElement))
            {
                return false;
            }

            var endTag = parentElement.EndTag;
            return endTag is { IsMissing: false, Name: { LocalName: { ValueText: parentStartTag.Name.LocalName.ValueText } } };
        }

        private void CheckNameAndInsertText(ITextView textView, ITextBuffer subjectBuffer, SnapshotPoint position, XmlElementStartTagSyntax startTag, int? finalCaretPosition, string formatString)
        {
            if (startTag == null)
            {
                return;
            }

            var elementName = startTag.Name.LocalName.ValueText;

            if (elementName.Length > 0)
            {
                var parentElement = startTag.Parent as XmlElementSyntax;
                if (parentElement.EndTag.Name.LocalName.ValueText != elementName)
                {
                    InsertTextAndMoveCaret(textView, subjectBuffer, position, string.Format(formatString, elementName), finalCaretPosition);
                }
            }
        }
    }
}
