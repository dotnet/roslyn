// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
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
    internal abstract class AbstractSnippetExpansionClient : ForegroundThreadAffinitizedObject, IExpansionClient
    {
        protected readonly IExpansionServiceProvider ExpansionServiceProvider;
        protected readonly IContentType LanguageServiceGuid;
        protected readonly ITextView TextView;
        protected readonly ITextBuffer SubjectBuffer;

        protected bool indentCaretOnCommit;
        protected int indentDepth;
        protected bool earlyEndExpansionHappened;

        internal IExpansionSession ExpansionSession;

        public AbstractSnippetExpansionClient(IThreadingContext threadingContext, IContentType languageServiceGuid, ITextView textView, ITextBuffer subjectBuffer, IExpansionServiceProvider expansionServiceProvider)
            : base(threadingContext)
        {
            this.LanguageServiceGuid = languageServiceGuid;
            this.TextView = textView;
            this.SubjectBuffer = subjectBuffer;
            this.ExpansionServiceProvider = expansionServiceProvider;
        }

        public abstract IExpansionFunction GetExpansionFunction(XElement xmlFunctionNode, string fieldName);
        protected abstract ITrackingSpan InsertEmptyCommentAndGetEndPositionTrackingSpan();
        internal abstract Document AddImports(Document document, int position, XElement snippetNode, bool placeSystemNamespaceFirst, bool allowInHiddenRegions, CancellationToken cancellationToken);

        public void FormatSpan(SnapshotSpan span)
        {
            // Formatting a snippet isn't cancellable.
            var cancellationToken = CancellationToken.None;
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

            // Insert empty comment and track end position
            var snippetTrackingSpan = snippetSpan.CreateTrackingSpan(SpanTrackingMode.EdgeInclusive);

            var fullSnippetSpan = ExpansionSession.GetSnippetSpan();

            var isFullSnippetFormat = fullSnippetSpan == span;
            var endPositionTrackingSpan = isFullSnippetFormat ? InsertEmptyCommentAndGetEndPositionTrackingSpan() : null;

            var formattingSpan = CommonFormattingHelpers.GetFormattingSpan(SubjectBuffer.CurrentSnapshot, snippetTrackingSpan.GetSpan(SubjectBuffer.CurrentSnapshot));

            SubjectBuffer.CurrentSnapshot.FormatAndApplyToBuffer(formattingSpan, CancellationToken.None);

            if (isFullSnippetFormat)
            {
                CleanUpEndLocation(endPositionTrackingSpan);

                // Unfortunately, this is the only place we can safely add references and imports
                // specified in the snippet xml. In OnBeforeInsertion we have no guarantee that the
                // snippet xml will be available, and changing the buffer during OnAfterInsertion can
                // cause the underlying tracking spans to get out of sync.
                var currentStartPosition = snippetTrackingSpan.GetStartPoint(SubjectBuffer.CurrentSnapshot).Position;
                AddReferencesAndImports(
                    ExpansionSession, currentStartPosition, cancellationToken);

                SetNewEndPosition(endPositionTrackingSpan);
            }
        }

        private void SetNewEndPosition(ITrackingSpan endTrackingSpan)
        {
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

        private void CleanUpEndLocation(ITrackingSpan endTrackingSpan)
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
                    indentCaretOnCommit = true;

                    var document = this.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document != null)
                    {
                        var documentOptions = document.GetOptionsAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                        indentDepth = lineText.GetColumnFromLineOffset(lineText.Length, documentOptions.GetOption(FormattingOptions.TabSize));
                    }
                    else
                    {
                        // If we don't have a document, then just guess the typical default TabSize value.
                        indentDepth = lineText.GetColumnFromLineOffset(lineText.Length, tabSize: 4);
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
            if (!(pSession.GetSnippetNode() is XElement snippetNode))
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
            if (indentCaretOnCommit && endLineText == string.Empty)
            {
                ITextViewExtensions.TryMoveCaretToAndEnsureVisible(TextView, new VirtualSnapshotPoint(point, indentDepth));
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
                earlyEndExpansionHappened = true;
            }

            ExpansionSession = null;
            indentCaretOnCommit = false;
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
            earlyEndExpansionHappened = false;
            ExpansionSession = expansion.InsertNamedExpansion(title, pszPath, new SnapshotSpan(textSpan, 0), this, LanguageServiceGuid, false);

            if (earlyEndExpansionHappened)
            {
                // EndExpansion was called before InsertNamedExpansion returned, so set
                // expansionSession to null to indicate that there is no active expansion
                // session. This can occur when the snippet inserted doesn't have any expansion
                // fields.
                ExpansionSession = null;
                earlyEndExpansionHappened = false;
            }
        }

        private void AddReferencesAndImports(
            IExpansionSession pSession,
            int position,
            CancellationToken cancellationToken)
        {
            if (!(pSession.GetSnippetNode() is XElement snippetNode))
            {
                return;
            }

            var documentWithImports = this.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (documentWithImports == null)
            {
                return;
            }

            var documentOptions = documentWithImports.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var placeSystemNamespaceFirst = documentOptions.GetOption(GenerationOptions.PlaceSystemNamespaceFirst);
            var allowInHiddenRegions = documentWithImports.CanAddImportsInHiddenRegions();

            documentWithImports = AddImports(documentWithImports, position, snippetNode, placeSystemNamespaceFirst, allowInHiddenRegions, cancellationToken);
            AddReferences(documentWithImports.Project, snippetNode);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private static void AddReferences(Project originalProject, XElement snippetNode)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var referencesNode = snippetNode.Element(XName.Get("References", snippetNode.Name.NamespaceName));
            if (referencesNode == null)
            {
                return;
            }
            throw new NotImplementedException();
            //var existingReferenceNames = originalProject.MetadataReferences.Select(r => Path.GetFileNameWithoutExtension(r.Display));
            //var workspace = originalProject.Solution.Workspace;
            //var projectId = originalProject.Id;

            //var assemblyXmlName = XName.Get("Assembly", snippetNode.Name.NamespaceName);
            //var failedReferenceAdditions = new List<string>();
            //var visualStudioWorkspace = workspace as VisualStudioWorkspaceImpl;

            //foreach (var reference in referencesNode.Elements(XName.Get("Reference", snippetNode.Name.NamespaceName)))
            //{
            //    // Note: URL references are not supported
            //    var assemblyElement = reference.Element(assemblyXmlName);

            //    var assemblyName = assemblyElement != null ? assemblyElement.Value.Trim() : null;

            //    if (string.IsNullOrEmpty(assemblyName))
            //    {
            //        continue;
            //    }

            //    if (visualStudioWorkspace == null ||
            //        !visualStudioWorkspace.TryAddReferenceToProject(projectId, assemblyName))
            //    {
            //        failedReferenceAdditions.Add(assemblyName);
            //    }
            //}

            //if (failedReferenceAdditions.Any())
            //{
            //    var notificationService = workspace.Services.GetService<INotificationService>();
            //    notificationService.SendNotification(
            //        string.Format(ServicesVSResources.The_following_references_were_not_found_0_Please_locate_and_add_them_manually, Environment.NewLine)
            //        + Environment.NewLine + Environment.NewLine
            //        + string.Join(Environment.NewLine, failedReferenceAdditions),
            //        severity: NotificationSeverity.Warning);
            //}
        }

        protected static bool TryAddImportsToContainedDocument(Document document, IEnumerable<string> memberImportsNamespaces)
        {
            throw new NotImplementedException();
            //var vsWorkspace = document.Project.Solution.Workspace as VisualStudioWorkspaceImpl;
            //if (vsWorkspace == null)
            //{
            //    return false;
            //}

            //var containedDocument = vsWorkspace.TryGetContainedDocument(document.Id);
            //if (containedDocument == null)
            //{
            //    return false;
            //}

            //if (containedDocument.ContainedLanguageHost is IVsContainedLanguageHostInternal containedLanguageHost)
            //{
            //    foreach (var importClause in memberImportsNamespaces)
            //    {
            //        if (containedLanguageHost.InsertImportsDirective(importClause) != VSConstants.S_OK)
            //        {
            //            return false;
            //        }
            //    }
            //}

            //return true;
        }

        protected static bool TryGetSnippetFunctionInfo(XElement xmlFunctionNode, out string snippetFunctionName, out string param)
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
