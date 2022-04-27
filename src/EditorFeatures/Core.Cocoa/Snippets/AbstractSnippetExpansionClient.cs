// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Expansion;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract class AbstractSnippetExpansionClient : IExpansionClient
    {
        protected readonly IExpansionServiceProvider ExpansionServiceProvider;
        protected readonly IContentType LanguageServiceGuid;
        protected readonly ITextView TextView;
        protected readonly ITextBuffer SubjectBuffer;

        public readonly IGlobalOptionService GlobalOptions;

        protected bool _indentCaretOnCommit;
        protected int _indentDepth;
        protected bool _earlyEndExpansionHappened;

        public IExpansionSession? ExpansionSession { get; private set; }

        public AbstractSnippetExpansionClient(IContentType languageServiceGuid, ITextView textView, ITextBuffer subjectBuffer, IExpansionServiceProvider expansionServiceProvider, IGlobalOptionService globalOptions)
        {
            LanguageServiceGuid = languageServiceGuid;
            TextView = textView;
            SubjectBuffer = subjectBuffer;
            ExpansionServiceProvider = expansionServiceProvider;
            GlobalOptions = globalOptions;
        }

        public abstract IExpansionFunction? GetExpansionFunction(XElement xmlFunctionNode, string fieldName);
        protected abstract ITrackingSpan? InsertEmptyCommentAndGetEndPositionTrackingSpan();

        public void FormatSpan(SnapshotSpan span)
        {
            // At this point, the $selection$ token has been replaced with the selected text and
            // declarations have been replaced with their default text. We need to format the 
            // inserted snippet text while carefully handling $end$ position (where the caret goes
            // after Return is pressed). The IExpansionSession keeps a tracking point for this
            // position but we do the tracking ourselves to properly deal with virtual space. To 
            // ensure the end location is correct, we take three extra steps:
            // 1. Insert an empty comment ("/**/" or "'") at the current $end$ position (prior 
            //    to formatting), and keep a tracking span for the comment.
            // 2. After formatting the new snippet text, find and delete the empty multiline 
            //    comment (via the tracking span) and notify the IExpansionSession of the new 
            //    $end$ location. If the line then contains only whitespace (due to the formatter
            //    putting the empty comment on its own line), then delete the white space and 
            //    remember the indentation depth for that line.
            // 3. When the snippet is finally completed (via Return), and PositionCaretForEditing()
            //    is called, check to see if the end location was on a line containing only white
            //    space in the previous step. If so, and if that line is still empty, then position
            //    the caret in virtual space.
            // This technique ensures that a snippet like "if($condition$) { $end$ }" will end up 
            // as:
            //     if ($condition$)
            //     {
            //         $end$
            //     }
            if (!TryGetSubjectBufferSpan(span, out var snippetSpan))
            {
                return;
            }

            Contract.ThrowIfNull(ExpansionSession);

            // Insert empty comment and track end position
            var snippetTrackingSpan = snippetSpan.CreateTrackingSpan(SpanTrackingMode.EdgeInclusive);

            var fullSnippetSpan = ExpansionSession.GetSnippetSpan();

            var isFullSnippetFormat = fullSnippetSpan == span;
            var endPositionTrackingSpan = isFullSnippetFormat ? InsertEmptyCommentAndGetEndPositionTrackingSpan() : null;

            var formattingSpan = CommonFormattingHelpers.GetFormattingSpan(SubjectBuffer.CurrentSnapshot, snippetTrackingSpan.GetSpan(SubjectBuffer.CurrentSnapshot));

            SubjectBuffer.CurrentSnapshot.FormatAndApplyToBuffer(formattingSpan, GlobalOptions, CancellationToken.None);

            if (isFullSnippetFormat)
            {
                CleanUpEndLocation(endPositionTrackingSpan);

                SetNewEndPosition(endPositionTrackingSpan);
            }
        }

        private void SetNewEndPosition(ITrackingSpan? endTrackingSpan)
        {
            Contract.ThrowIfNull(ExpansionSession);

            if (SetEndPositionIfNoneSpecified(ExpansionSession))
            {
                return;
            }

            if (endTrackingSpan != null)
            {
                if (!TryGetSpanOnHigherBuffer(
                    endTrackingSpan.GetSpan(SubjectBuffer.CurrentSnapshot),
                    TextView.TextBuffer,
                    out var endSpanInSurfaceBuffer))
                {
                    return;
                }

                ExpansionSession.EndSpan = new SnapshotSpan(endSpanInSurfaceBuffer.Start, 0);
            }
        }

        private void CleanUpEndLocation(ITrackingSpan? endTrackingSpan)
        {
            if (endTrackingSpan != null)
            {
                // Find the empty comment and remove it...
                var endSnapshotSpan = endTrackingSpan.GetSpan(SubjectBuffer.CurrentSnapshot);
                SubjectBuffer.Delete(endSnapshotSpan.Span);

                // Remove the whitespace before the comment if necessary. If whitespace is removed,
                // then remember the indentation depth so we can appropriately position the caret
                // in virtual space when the session is ended.
                var line = SubjectBuffer.CurrentSnapshot.GetLineFromPosition(endSnapshotSpan.Start.Position);
                var lineText = line.GetText();

                if (lineText.Trim() == string.Empty)
                {
                    _indentCaretOnCommit = true;

                    var document = this.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document != null)
                    {
                        var documentOptions = document.GetOptionsAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                        _indentDepth = lineText.GetColumnFromLineOffset(lineText.Length, documentOptions.GetOption(FormattingOptions.TabSize));
                    }
                    else
                    {
                        // If we don't have a document, then just guess the typical default TabSize value.
                        _indentDepth = lineText.GetColumnFromLineOffset(lineText.Length, tabSize: 4);
                    }

                    SubjectBuffer.Delete(new Span(line.Start.Position, line.Length));
                    _ = SubjectBuffer.CurrentSnapshot.GetSpan(new Span(line.Start.Position, 0));
                }
            }
        }

        /// <summary>
        /// If there was no $end$ token, place it at the end of the snippet code. Otherwise, it
        /// defaults to the beginning of the snippet code.
        /// </summary>
        private static bool SetEndPositionIfNoneSpecified(IExpansionSession pSession)
        {
            if (pSession.GetSnippetNode() is not XElement snippetNode)
            {
                return false;
            }

            var ns = snippetNode.Name.NamespaceName;
            var codeNode = snippetNode.Element(XName.Get("Code", ns));
            if (codeNode == null)
            {
                return false;
            }

            var delimiterAttribute = codeNode.Attribute("Delimiter");
            var delimiter = delimiterAttribute != null ? delimiterAttribute.Value : "$";
            if (codeNode.Value.IndexOf(string.Format("{0}end{0}", delimiter), StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }

            var snippetSpan = pSession.GetSnippetSpan();

            var newEndSpan = new SnapshotSpan(snippetSpan.End, 0);

            pSession.EndSpan = newEndSpan;
            return true;
        }

        public void PositionCaretForEditing(ITextView textView, SnapshotPoint point)
        {
            // If the formatted location of the $end$ position (the inserted comment) was on an
            // empty line and indented, then we have already removed the white space on that line
            // and the navigation location will be at column 0 on a blank line. We must now
            // position the caret in virtual space.
            var line = point.GetContainingLine();

            PositionCaretForEditingInternal(line.GetText(), line.End);
        }

        /// <summary>
        /// Internal for testing purposes. All real caret positioning logic takes place here. <see cref="PositionCaretForEditing"/>
        /// only extracts the <paramref name="endLineText"/> and <paramref name="point"/> from the provided <see cref="ITextView"/>.
        /// Tests can call this method directly to avoid producing an IVsTextLines.
        /// </summary>
        /// <param name="endLineText"></param>
        /// <param name="point"></param>
        internal void PositionCaretForEditingInternal(string endLineText, SnapshotPoint point)
        {
            if (_indentCaretOnCommit && endLineText == string.Empty)
            {
                ITextViewExtensions.TryMoveCaretToAndEnsureVisible(TextView, new VirtualSnapshotPoint(point, _indentDepth));
            }
        }

        public virtual bool TryHandleTab()
        {
            if (ExpansionSession != null)
            {
                var tabbedInsideSnippetField = ExpansionSession.GoToNextExpansionField(false);

                if (!tabbedInsideSnippetField)
                {
                    ExpansionSession.EndCurrentExpansion(leaveCaret: true);
                    ExpansionSession = null;
                }

                return tabbedInsideSnippetField;
            }

            return false;
        }

        public virtual bool TryHandleBackTab()
        {
            if (ExpansionSession != null)
            {
                var tabbedInsideSnippetField = ExpansionSession.GoToPreviousExpansionField();

                if (!tabbedInsideSnippetField)
                {
                    ExpansionSession.EndCurrentExpansion(leaveCaret: true);
                    ExpansionSession = null;
                }

                return tabbedInsideSnippetField;
            }

            return false;
        }

        public virtual bool TryHandleEscape()
        {
            if (ExpansionSession != null)
            {
                ExpansionSession.EndCurrentExpansion(leaveCaret: true);
                ExpansionSession = null;
                return true;
            }

            return false;
        }

        public virtual bool TryHandleReturn()
        {
            if (ExpansionSession != null)
            {
                // Only move the caret if the enter was hit within the snippet fields.
                var hitWithinField = ExpansionSession.GoToNextExpansionField(commitIfLast: false);
                ExpansionSession.EndCurrentExpansion(leaveCaret: !hitWithinField);
                ExpansionSession = null;

                return hitWithinField;
            }

            return false;
        }

        public virtual bool TryInsertExpansion(int startPositionInSubjectBuffer, int endPositionInSubjectBuffer)
        {
            var textViewModel = TextView.TextViewModel;
            if (textViewModel == null)
            {
                Debug.Assert(TextView.IsClosed);
                return false;
            }

            // The expansion itself needs to be created in the data buffer, so map everything up
            _ = SubjectBuffer.CurrentSnapshot.GetPoint(startPositionInSubjectBuffer);
            _ = SubjectBuffer.CurrentSnapshot.GetPoint(endPositionInSubjectBuffer);
            if (!TryGetSpanOnHigherBuffer(
                SubjectBuffer.CurrentSnapshot.GetSpan(startPositionInSubjectBuffer, endPositionInSubjectBuffer - startPositionInSubjectBuffer),
                textViewModel.DataBuffer,
                out var dataBufferSpan))
            {
                return false;
            }

            var expansion = ExpansionServiceProvider.GetExpansionService(TextView);
            ExpansionSession = expansion.InsertExpansion(dataBufferSpan, this, LanguageServiceGuid);
            if (ExpansionSession == null)
                return false;
            return true;
        }

        public void EndExpansion()
        {
            if (ExpansionSession == null)
            {
                _earlyEndExpansionHappened = true;
            }

            ExpansionSession = null;
            _indentCaretOnCommit = false;
        }

        public void OnAfterInsertion(IExpansionSession pSession)
        {
            Logger.Log(FunctionId.Snippet_OnAfterInsertion);
        }

        public void OnBeforeInsertion(IExpansionSession pSession)
        {
            Logger.Log(FunctionId.Snippet_OnBeforeInsertion);
            this.ExpansionSession = pSession;
        }

        public void OnItemChosen(string title, string pszPath)
        {
            var textViewModel = TextView.TextViewModel;
            if (textViewModel == null)
            {
                Debug.Assert(TextView.IsClosed);
                return;
            }

            var textSpan = TextView.Caret.Position.BufferPosition;

            var expansion = ExpansionServiceProvider.GetExpansionService(TextView);
            _earlyEndExpansionHappened = false;
            ExpansionSession = expansion.InsertNamedExpansion(title, pszPath, new SnapshotSpan(textSpan, 0), this, LanguageServiceGuid, false);

            if (_earlyEndExpansionHappened)
            {
                // EndExpansion was called before InsertNamedExpansion returned, so set
                // expansionSession to null to indicate that there is no active expansion
                // session. This can occur when the snippet inserted doesn't have any expansion
                // fields.
                ExpansionSession = null;
                _earlyEndExpansionHappened = false;
            }
        }

        protected static bool TryGetSnippetFunctionInfo(XElement xmlFunctionNode, [NotNullWhen(returnValue: true)] out string? snippetFunctionName, [NotNullWhen(returnValue: true)] out string? param)
        {
            if (xmlFunctionNode.Value.IndexOf('(') == -1 ||
                xmlFunctionNode.Value.IndexOf(')') == -1 ||
                xmlFunctionNode.Value.IndexOf(')') < xmlFunctionNode.Value.IndexOf('('))
            {
                snippetFunctionName = null;
                param = null;
                return false;
            }

            snippetFunctionName = xmlFunctionNode.Value.Substring(0, xmlFunctionNode.Value.IndexOf('('));

            var paramStart = xmlFunctionNode.Value.IndexOf('(') + 1;
            var paramLength = xmlFunctionNode.Value.LastIndexOf(')') - xmlFunctionNode.Value.IndexOf('(') - 1;
            param = xmlFunctionNode.Value.Substring(paramStart, paramLength);
            return true;
        }

        internal bool TryGetSubjectBufferSpan(SnapshotSpan snapshotSpan, out SnapshotSpan subjectBufferSpan)
        {
            var subjectBufferSpanCollection = TextView.BufferGraph.MapDownToBuffer(snapshotSpan, SpanTrackingMode.EdgeExclusive, SubjectBuffer);

            // Bail if a snippet span does not map down to exactly one subject buffer span.
            if (subjectBufferSpanCollection.Count == 1)
            {
                subjectBufferSpan = subjectBufferSpanCollection.Single();
                return true;
            }

            subjectBufferSpan = default;
            return false;
        }

        internal bool TryGetSpanOnHigherBuffer(SnapshotSpan snapshotSpan, ITextBuffer targetBuffer, out SnapshotSpan span)
        {
            var spanCollection = TextView.BufferGraph.MapUpToBuffer(snapshotSpan, SpanTrackingMode.EdgeExclusive, targetBuffer);

            // Bail if a snippet span does not map up to exactly one span.
            if (spanCollection.Count == 1)
            {
                span = spanCollection.Single();
                return true;
            }

            span = default;
            return false;
        }
    }
}
