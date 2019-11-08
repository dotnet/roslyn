// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using MSXML;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract class AbstractSnippetExpansionClient : ForegroundThreadAffinitizedObject, IVsExpansionClient
    {
        protected readonly IVsEditorAdaptersFactoryService EditorAdaptersFactoryService;
        protected readonly Guid LanguageServiceGuid;
        protected readonly ITextView TextView;
        protected readonly ITextBuffer SubjectBuffer;

        protected bool indentCaretOnCommit;
        protected int indentDepth;
        protected bool earlyEndExpansionHappened;

        internal IVsExpansionSession ExpansionSession;

        public AbstractSnippetExpansionClient(IThreadingContext threadingContext, Guid languageServiceGuid, ITextView textView, ITextBuffer subjectBuffer, IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
            : base(threadingContext)
        {
            this.LanguageServiceGuid = languageServiceGuid;
            this.TextView = textView;
            this.SubjectBuffer = subjectBuffer;
            this.EditorAdaptersFactoryService = editorAdaptersFactoryService;
        }

        public abstract int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc);
        protected abstract ITrackingSpan InsertEmptyCommentAndGetEndPositionTrackingSpan();
        internal abstract Document AddImports(Document document, int position, XElement snippetNode, bool placeSystemNamespaceFirst, CancellationToken cancellationToken);

        public int FormatSpan(IVsTextLines pBuffer, VsTextSpan[] tsInSurfaceBuffer)
        {
            // Formatting a snippet isn't cancellable.
            var cancellationToken = CancellationToken.None;
            // At this point, the $selection$ token has been replaced with the selected text and
            // declarations have been replaced with their default text. We need to format the 
            // inserted snippet text while carefully handling $end$ position (where the caret goes
            // after Return is pressed). The IVsExpansionSession keeps a tracking point for this
            // position but we do the tracking ourselves to properly deal with virtual space. To 
            // ensure the end location is correct, we take three extra steps:
            // 1. Insert an empty comment ("/**/" or "'") at the current $end$ position (prior 
            //    to formatting), and keep a tracking span for the comment.
            // 2. After formatting the new snippet text, find and delete the empty multiline 
            //    comment (via the tracking span) and notify the IVsExpansionSession of the new 
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
            if (!TryGetSubjectBufferSpan(tsInSurfaceBuffer[0], out var snippetSpan))
            {
                return VSConstants.S_OK;
            }

            // Insert empty comment and track end position
            var snippetTrackingSpan = snippetSpan.CreateTrackingSpan(SpanTrackingMode.EdgeInclusive);

            var fullSnippetSpan = new VsTextSpan[1];
            ExpansionSession.GetSnippetSpan(fullSnippetSpan);

            var isFullSnippetFormat =
                fullSnippetSpan[0] is
            {
                iStartLine: tsInSurfaceBuffer[0].iStartLine,
                iStartIndex: tsInSurfaceBuffer[0].iStartIndex,
                iEndLine: tsInSurfaceBuffer[0].iEndLine,
                iEndIndex: tsInSurfaceBuffer[0].iEndIndex
            };
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

            return VSConstants.S_OK;
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

                TextView.TextSnapshot.GetLineAndCharacter(endSpanInSurfaceBuffer.Start.Position, out var endLine, out var endChar);
                ExpansionSession.SetEndSpan(new VsTextSpan
                {
                    iStartLine = endLine,
                    iStartIndex = endChar,
                    iEndLine = endLine,
                    iEndIndex = endChar
                });
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
                    endSnapshotSpan = SubjectBuffer.CurrentSnapshot.GetSpan(new Span(line.Start.Position, 0));
                }
            }
        }

        /// <summary>
        /// If there was no $end$ token, place it at the end of the snippet code. Otherwise, it
        /// defaults to the beginning of the snippet code.
        /// </summary>
        private static bool SetEndPositionIfNoneSpecified(IVsExpansionSession pSession)
        {
            if (!TryGetSnippetNode(pSession, out var snippetNode))
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

            var snippetSpan = new VsTextSpan[1];
            if (pSession.GetSnippetSpan(snippetSpan) != VSConstants.S_OK)
            {
                return false;
            }

            var newEndSpan = new VsTextSpan
            {
                iStartLine = snippetSpan[0].iEndLine,
                iStartIndex = snippetSpan[0].iEndIndex,
                iEndLine = snippetSpan[0].iEndLine,
                iEndIndex = snippetSpan[0].iEndIndex
            };

            pSession.SetEndSpan(newEndSpan);
            return true;
        }

        protected static bool TryGetSnippetNode(IVsExpansionSession pSession, out XElement snippetNode)
        {
            IXMLDOMNode xmlNode = null;
            snippetNode = null;

            try
            {
                // Cast to our own version of IVsExpansionSession so that we can get pNode as an
                // IntPtr instead of a via a RCW. This allows us to guarantee that it pNode is
                // released before leaving this method. Otherwise, a second invocation of the same
                // snippet may cause an AccessViolationException.
                var session = (IVsExpansionSessionInternal)pSession;
                if (session.GetSnippetNode(null, out var pNode) != VSConstants.S_OK)
                {
                    return false;
                }

                xmlNode = (IXMLDOMNode)Marshal.GetUniqueObjectForIUnknown(pNode);
                snippetNode = XElement.Parse(xmlNode.xml);
                return true;
            }
            finally
            {
                if (xmlNode != null && Marshal.IsComObject(xmlNode))
                {
                    Marshal.ReleaseComObject(xmlNode);
                }
            }
        }

        public int PositionCaretForEditing(IVsTextLines pBuffer, [ComAliasName("Microsoft.VisualStudio.TextManager.Interop.TextSpan")]VsTextSpan[] ts)
        {
            // If the formatted location of the $end$ position (the inserted comment) was on an
            // empty line and indented, then we have already removed the white space on that line
            // and the navigation location will be at column 0 on a blank line. We must now
            // position the caret in virtual space.
            pBuffer.GetLengthOfLine(ts[0].iStartLine, out var lineLength);
            pBuffer.GetLineText(ts[0].iStartLine, 0, ts[0].iStartLine, lineLength, out var endLineText);
            pBuffer.GetPositionOfLine(ts[0].iStartLine, out var endLinePosition);

            PositionCaretForEditingInternal(endLineText, endLinePosition);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Internal for testing purposes. All real caret positioning logic takes place here. <see cref="PositionCaretForEditing"/>
        /// only extracts the <paramref name="endLineText"/> and <paramref name="endLinePosition"/> from the provided <see cref="IVsTextLines"/>.
        /// Tests can call this method directly to avoid producing an IVsTextLines.
        /// </summary>
        /// <param name="endLineText"></param>
        /// <param name="endLinePosition"></param>
        internal void PositionCaretForEditingInternal(string endLineText, int endLinePosition)
        {
            if (indentCaretOnCommit && endLineText == string.Empty)
            {
                TextView.TryMoveCaretToAndEnsureVisible(new VirtualSnapshotPoint(TextView.TextSnapshot.GetPoint(endLinePosition), indentDepth));
            }
        }

        public virtual bool TryHandleTab()
        {
            if (ExpansionSession != null)
            {
                var tabbedInsideSnippetField = VSConstants.S_OK == ExpansionSession.GoToNextExpansionField(0);

                if (!tabbedInsideSnippetField)
                {
                    ExpansionSession.EndCurrentExpansion(fLeaveCaret: 1);
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
                var tabbedInsideSnippetField = VSConstants.S_OK == ExpansionSession.GoToPreviousExpansionField();

                if (!tabbedInsideSnippetField)
                {
                    ExpansionSession.EndCurrentExpansion(fLeaveCaret: 1);
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
                ExpansionSession.EndCurrentExpansion(fLeaveCaret: 1);
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
                var hitWithinField = VSConstants.S_OK == ExpansionSession.GoToNextExpansionField(fCommitIfLast: 0);
                ExpansionSession.EndCurrentExpansion(fLeaveCaret: hitWithinField ? 0 : 1);
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
            var startPointInSubjectBuffer = SubjectBuffer.CurrentSnapshot.GetPoint(startPositionInSubjectBuffer);
            var endPointInSubjectBuffer = SubjectBuffer.CurrentSnapshot.GetPoint(endPositionInSubjectBuffer);
            if (!TryGetSpanOnHigherBuffer(
                SubjectBuffer.CurrentSnapshot.GetSpan(startPositionInSubjectBuffer, endPositionInSubjectBuffer - startPositionInSubjectBuffer),
                textViewModel.DataBuffer,
                out var dataBufferSpan))
            {
                return false;
            }

            var buffer = EditorAdaptersFactoryService.GetBufferAdapter(textViewModel.DataBuffer);
            if (buffer == null || !(buffer is IVsExpansion expansion))
            {
                return false;
            }

            buffer.GetLineIndexOfPosition(dataBufferSpan.Start.Position, out var startLine, out var startIndex);
            buffer.GetLineIndexOfPosition(dataBufferSpan.End.Position, out var endLine, out var endIndex);

            var textSpan = new VsTextSpan
            {
                iStartLine = startLine,
                iStartIndex = startIndex,
                iEndLine = endLine,
                iEndIndex = endIndex
            };

            return expansion.InsertExpansion(textSpan, textSpan, this, LanguageServiceGuid, out ExpansionSession) == VSConstants.S_OK;
        }

        public int EndExpansion()
        {
            if (ExpansionSession == null)
            {
                earlyEndExpansionHappened = true;
            }

            ExpansionSession = null;
            indentCaretOnCommit = false;

            return VSConstants.S_OK;
        }

        public int IsValidKind(IVsTextLines pBuffer, VsTextSpan[] ts, string bstrKind, out int pfIsValidKind)
        {
            pfIsValidKind = 1;
            return VSConstants.S_OK;
        }

        public int IsValidType(IVsTextLines pBuffer, VsTextSpan[] ts, string[] rgTypes, int iCountTypes, out int pfIsValidType)
        {
            pfIsValidType = 1;
            return VSConstants.S_OK;
        }

        public int OnAfterInsertion(IVsExpansionSession pSession)
        {
            Logger.Log(FunctionId.Snippet_OnAfterInsertion);

            return VSConstants.S_OK;
        }

        public int OnBeforeInsertion(IVsExpansionSession pSession)
        {
            Logger.Log(FunctionId.Snippet_OnBeforeInsertion);

            this.ExpansionSession = pSession;
            return VSConstants.S_OK;
        }

        public int OnItemChosen(string pszTitle, string pszPath)
        {
            var textViewModel = TextView.TextViewModel;
            if (textViewModel == null)
            {
                Debug.Assert(TextView.IsClosed);
                return VSConstants.E_FAIL;
            }

            var hr = VSConstants.S_OK;

            try
            {
                VsTextSpan textSpan;
                GetCaretPositionInSurfaceBuffer(out textSpan.iStartLine, out textSpan.iStartIndex);

                textSpan.iEndLine = textSpan.iStartLine;
                textSpan.iEndIndex = textSpan.iStartIndex;

                var expansion = EditorAdaptersFactoryService.GetBufferAdapter(textViewModel.DataBuffer) as IVsExpansion;
                earlyEndExpansionHappened = false;
                hr = expansion.InsertNamedExpansion(pszTitle, pszPath, textSpan, this, LanguageServiceGuid, fShowDisambiguationUI: 0, pSession: out ExpansionSession);

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
            catch (COMException ex)
            {
                hr = ex.ErrorCode;
            }

            return hr;
        }

        private void GetCaretPositionInSurfaceBuffer(out int caretLine, out int caretColumn)
        {
            var vsTextView = EditorAdaptersFactoryService.GetViewAdapter(TextView);
            vsTextView.GetCaretPos(out caretLine, out caretColumn);
            vsTextView.GetBuffer(out var textLines);
            // Handle virtual space (e.g, see Dev10 778675)
            textLines.GetLengthOfLine(caretLine, out var lineLength);
            if (caretColumn > lineLength)
            {
                caretColumn = lineLength;
            }
        }

        private void AddReferencesAndImports(
            IVsExpansionSession pSession,
            int position,
            CancellationToken cancellationToken)
        {
            if (!TryGetSnippetNode(pSession, out var snippetNode))
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

            documentWithImports = AddImports(documentWithImports, position, snippetNode, placeSystemNamespaceFirst, cancellationToken);
            AddReferences(documentWithImports.Project, snippetNode);
        }

        private void AddReferences(Project originalProject, XElement snippetNode)
        {
            var referencesNode = snippetNode.Element(XName.Get("References", snippetNode.Name.NamespaceName));
            if (referencesNode == null)
            {
                return;
            }

            var existingReferenceNames = originalProject.MetadataReferences.Select(r => Path.GetFileNameWithoutExtension(r.Display));
            var workspace = originalProject.Solution.Workspace;
            var projectId = originalProject.Id;

            var assemblyXmlName = XName.Get("Assembly", snippetNode.Name.NamespaceName);
            var failedReferenceAdditions = new List<string>();

            foreach (var reference in referencesNode.Elements(XName.Get("Reference", snippetNode.Name.NamespaceName)))
            {
                // Note: URL references are not supported
                var assemblyElement = reference.Element(assemblyXmlName);

                var assemblyName = assemblyElement != null ? assemblyElement.Value.Trim() : null;

                if (string.IsNullOrEmpty(assemblyName))
                {
                    continue;
                }

                if (!(workspace is VisualStudioWorkspaceImpl visualStudioWorkspace) ||
                    !visualStudioWorkspace.TryAddReferenceToProject(projectId, assemblyName))
                {
                    failedReferenceAdditions.Add(assemblyName);
                }
            }

            if (failedReferenceAdditions.Any())
            {
                var notificationService = workspace.Services.GetService<INotificationService>();
                notificationService.SendNotification(
                    string.Format(ServicesVSResources.The_following_references_were_not_found_0_Please_locate_and_add_them_manually, Environment.NewLine)
                    + Environment.NewLine + Environment.NewLine
                    + string.Join(Environment.NewLine, failedReferenceAdditions),
                    severity: NotificationSeverity.Warning);
            }
        }

        protected static bool TryAddImportsToContainedDocument(Document document, IEnumerable<string> memberImportsNamespaces)
        {
            if (!(document.Project.Solution.Workspace is VisualStudioWorkspaceImpl vsWorkspace))
            {
                return false;
            }

            var containedDocument = vsWorkspace.TryGetContainedDocument(document.Id);
            if (containedDocument == null)
            {
                return false;
            }

            if (containedDocument.ContainedLanguageHost is IVsContainedLanguageHostInternal containedLanguageHost)
            {
                foreach (var importClause in memberImportsNamespaces)
                {
                    if (containedLanguageHost.InsertImportsDirective(importClause) != VSConstants.S_OK)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected static bool TryGetSnippetFunctionInfo(IXMLDOMNode xmlFunctionNode, out string snippetFunctionName, out string param)
        {
            if (xmlFunctionNode.text.IndexOf('(') == -1 ||
                xmlFunctionNode.text.IndexOf(')') == -1 ||
                xmlFunctionNode.text.IndexOf(')') < xmlFunctionNode.text.IndexOf('('))
            {
                snippetFunctionName = null;
                param = null;
                return false;
            }

            snippetFunctionName = xmlFunctionNode.text.Substring(0, xmlFunctionNode.text.IndexOf('('));

            var paramStart = xmlFunctionNode.text.IndexOf('(') + 1;
            var paramLength = xmlFunctionNode.text.LastIndexOf(')') - xmlFunctionNode.text.IndexOf('(') - 1;
            param = xmlFunctionNode.text.Substring(paramStart, paramLength);
            return true;
        }

        internal bool TryGetSubjectBufferSpan(VsTextSpan surfaceBufferTextSpan, out SnapshotSpan subjectBufferSpan)
        {
            var snapshotSpan = TextView.TextSnapshot.GetSpan(surfaceBufferTextSpan);
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
