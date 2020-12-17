// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.TextManager.Interop;
using MSXML;
using Roslyn.Utilities;
using CommonFormattingHelpers = Microsoft.CodeAnalysis.Editor.Shared.Utilities.CommonFormattingHelpers;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract class AbstractSnippetExpansionClient : ForegroundThreadAffinitizedObject, IVsExpansionClient
    {
        protected readonly IVsEditorAdaptersFactoryService EditorAdaptersFactoryService;
        protected readonly Guid LanguageServiceGuid;
        protected readonly ITextView TextView;
        protected readonly ITextBuffer SubjectBuffer;

        private readonly ImmutableArray<Lazy<ArgumentProvider, OrderableLanguageMetadata>> _allArgumentProviders;
        private ImmutableArray<ArgumentProvider> _argumentProviders;

        protected bool indentCaretOnCommit;
        protected int indentDepth;
        protected bool earlyEndExpansionHappened;

        private State _state = new() { _arguments = ImmutableDictionary.Create<string, string>() };

        public AbstractSnippetExpansionClient(
            IThreadingContext threadingContext,
            Guid languageServiceGuid,
            ITextView textView,
            ITextBuffer subjectBuffer,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            ImmutableArray<Lazy<ArgumentProvider, OrderableLanguageMetadata>> argumentProviders)
            : base(threadingContext)
        {
            this.LanguageServiceGuid = languageServiceGuid;
            this.TextView = textView;
            this.SubjectBuffer = subjectBuffer;
            this.EditorAdaptersFactoryService = editorAdaptersFactoryService;
            this._allArgumentProviders = argumentProviders;
        }

        internal IVsExpansionSession ExpansionSession => _state._expansionSession;
        internal ImmutableDictionary<string, string> Arguments => _state._arguments;

        internal ImmutableArray<ArgumentProvider> GetArgumentProviders(Workspace workspace)
        {
            AssertIsForeground();

            if (_argumentProviders.IsDefault)
            {
                _argumentProviders = workspace.Services
                    .SelectMatchingExtensionValues(ExtensionOrderer.Order(_allArgumentProviders), SubjectBuffer.ContentType)
                    .ToImmutableArray();
            }

            return _argumentProviders;
        }

        public abstract int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc);
        protected abstract ITrackingSpan InsertEmptyCommentAndGetEndPositionTrackingSpan();
        internal abstract Document AddImports(Document document, int position, XElement snippetNode, bool placeSystemNamespaceFirst, bool allowInHiddenRegions, CancellationToken cancellationToken);

        public int FormatSpan(IVsTextLines pBuffer, VsTextSpan[] tsInSurfaceBuffer)
        {
            // If this is a manually-constructed snippet for a full method call, avoid formatting the snippet since
            // doing so will disrupt signature help.
            if (!_state._symbols.IsDefault)
            {
                return VSConstants.S_OK;
            }

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
                fullSnippetSpan[0].iStartLine == tsInSurfaceBuffer[0].iStartLine &&
                fullSnippetSpan[0].iStartIndex == tsInSurfaceBuffer[0].iStartIndex &&
                fullSnippetSpan[0].iEndLine == tsInSurfaceBuffer[0].iEndLine &&
                fullSnippetSpan[0].iEndIndex == tsInSurfaceBuffer[0].iEndIndex;
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
                    _ = SubjectBuffer.CurrentSnapshot.GetSpan(new Span(line.Start.Position, 0));
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

        public int PositionCaretForEditing(IVsTextLines pBuffer, [ComAliasName("Microsoft.VisualStudio.TextManager.Interop.TextSpan")] VsTextSpan[] ts)
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
                // When 'Tab' is pressed in the last field of a normal snippet, the session wraps back around to the
                // first field (this is preservation of historical behavior). When 'Tab' is pressed at the end of an
                // argument provider snippet, the snippet session is automatically committed (this behavior matches the
                // design for Insert Full Method Call intended for multiple IDEs).
                var tabbedInsideSnippetField = VSConstants.S_OK == ExpansionSession.GoToNextExpansionField(fCommitIfLast: _state.IsFullMethodCallSnippet ? 1 : 0);

                if (!tabbedInsideSnippetField)
                {
                    ExpansionSession.EndCurrentExpansion(fLeaveCaret: 1);
                    _state.Clear(forceClearSymbolInformation: true);
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
                    _state.Clear(forceClearSymbolInformation: true);
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
                _state.Clear(forceClearSymbolInformation: true);
                return true;
            }

            return false;
        }

        public virtual bool TryHandleReturn()
        {
            return CommitSnippet(leaveCaret: false);
        }

        public bool CommitSnippet(bool leaveCaret)
        {
            if (ExpansionSession != null)
            {
                if (!leaveCaret)
                {
                    // Only move the caret if the enter was hit within the snippet fields.
                    var hitWithinField = VSConstants.S_OK == ExpansionSession.GoToNextExpansionField(fCommitIfLast: 0);
                    leaveCaret = !hitWithinField;
                }

                ExpansionSession.EndCurrentExpansion(fLeaveCaret: leaveCaret ? 1 : 0);
                _state.Clear(forceClearSymbolInformation: true);

                return !leaveCaret;
            }

            return false;
        }

        public virtual bool TryInsertExpansion(int startPositionInSubjectBuffer, int endPositionInSubjectBuffer, CancellationToken cancellationToken)
        {
            var textViewModel = TextView.TextViewModel;
            if (textViewModel == null)
            {
                Debug.Assert(TextView.IsClosed);
                return false;
            }

            // The expansion itself needs to be created in the data buffer, so map everything up
            var triggerSpan = SubjectBuffer.CurrentSnapshot.GetSpan(startPositionInSubjectBuffer, endPositionInSubjectBuffer - startPositionInSubjectBuffer);
            if (!TryGetSpanOnHigherBuffer(triggerSpan, textViewModel.DataBuffer, out var dataBufferSpan))
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

            if (expansion.InsertExpansion(textSpan, textSpan, this, LanguageServiceGuid, out _state._expansionSession) == VSConstants.S_OK)
            {
                // This expansion is not derived from a symbol
                _state.ClearSymbolInformation();
                return true;
            }

            if (!(SubjectBuffer.GetFeatureOnOffOption(CompletionOptions.EnableCallCompletionOnTabTab) ?? false))
            {
                // Full method call completion is not enabled
                return false;
            }

            var symbols = ThreadingContext.JoinableTaskFactory.Run(() => GetSymbolsAsync(caretPosition: triggerSpan.End, cancellationToken));

            var methodSymbols = symbols.OfType<IMethodSymbol>().ToImmutableArray();
            if (methodSymbols.Any())
            {
                var methodName = dataBufferSpan.GetText();
                var snippet = CreateSnippet(methodName, includeMethod: true, ImmutableArray<IParameterSymbol>.Empty, cancellationToken);

                var doc = new DOMDocumentClass();
                if (doc.loadXML(snippet.ToString(SaveOptions.OmitDuplicateNamespaces)))
                {
                    _state._symbols = methodSymbols;
                    _state._symbol = null;

                    if (expansion.InsertSpecificExpansion(doc, textSpan, this, LanguageServiceGuid, pszRelativePath: null, out _state._expansionSession) == VSConstants.S_OK)
                    {
                        Debug.Assert(_state._symbols == methodSymbols);
                        Debug.Assert(_state._symbol == null);

                        // Trigger signature help after starting the snippet session
                        //
                        // TODO: Figure out why ISignatureHelpBroker.TriggerSignatureHelp doesn't work but this does.
                        // https://github.com/dotnet/roslyn/issues/50036
                        var workspace = (VisualStudioWorkspace)SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges()!.Project.Solution.Workspace;
                        var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;
                        var editorCommandHandlerServiceFactory = exportProvider.GetExports<IEditorCommandHandlerServiceFactory>().Single().Value;
                        var editorCommandHandlerService = editorCommandHandlerServiceFactory.GetService(TextView, SubjectBuffer);
                        editorCommandHandlerService.Execute((view, buffer) => new InvokeSignatureHelpCommandArgs(view, buffer), nextCommandHandler: null);

                        ThreadingContext.JoinableTaskFactory.RunAsync(async () =>
                        {
                            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                            while (ExpansionSession is not null)
                            {
                                // Loop until a controller is available.
                                //
                                // TODO: Controller.GetInstance has better postcondition semantics, but the
                                // preconditions make it much more difficult to invoke.
                                // https://github.com/dotnet/roslyn/issues/50037
                                var controller = Controller.TryGetInstance(TextView, SubjectBuffer);
                                if (controller is null)
                                {
                                    await Task.Yield();
                                    continue;
                                }

                                // Register a handler for ModelUpdated. To avoid the possibility of more than one
                                // handler being in the list, remove any current one before adding the new one. This
                                // handler is only changed on the main thread, and the event is only invoked on the main
                                // thread, so additional synchronization is not necessary to prevent lost events.
                                controller.ModelUpdated -= OnModelUpdated;
                                controller.ModelUpdated += OnModelUpdated;
                                return;
                            }
                        });

                        return true;
                    }
                    else
                    {
                        _state.Clear(forceClearSymbolInformation: true);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a snippet for providing arguments to a call.
        /// </summary>
        /// <param name="methodName">The name of the method as it should appear in code.</param>
        /// <param name="includeMethod"><see langword="true"/> to include the method name and invocation parentheses in
        /// the resulting snippet; otherwise, <see langword="false"/> if the method name and parentheses are assumed to
        /// already exist and the template will only specify the argument placeholders.</param>
        /// <param name="parameters">The parameters to the method. If the specific target of the invocation is not
        /// known, an empty array may be passed to create a template with a placeholder where arguments will eventually
        /// go.</param>
        /// <returns></returns>
        private static XDocument CreateSnippet(string methodName, bool includeMethod, ImmutableArray<IParameterSymbol> parameters, CancellationToken cancellationToken)
        {
            XNamespace snippetNamespace = "http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet";

            var template = new StringBuilder();

            if (includeMethod)
            {
                template.Append(methodName).Append('(');
            }

            var declarations = new List<XElement>();
            foreach (var parameter in parameters)
            {
                if (declarations.Any())
                {
                    template.Append(", ");
                }

                // Create a snippet field for the argument. The name of the field matches the parameter name, and the
                // default value for the field is provided by a call to the internal ArgumentValue snippet function. The
                // parameter to the snippet function is a serialized SymbolKey which can be mapped back to the
                // IParameterSymbol.
                template.Append('$').Append(parameter.Name).Append('$');
                declarations.Add(new XElement(
                    snippetNamespace + "Literal",
                    new XAttribute(snippetNamespace + "Editable", "true"),
                    new XElement(snippetNamespace + "ID", new XText(parameter.Name)),
                    new XElement(snippetNamespace + "Function", new XText($"ArgumentValue({SymbolKey.CreateString(parameter, cancellationToken)})"))));
            }

            if (!declarations.Any())
            {
                // If the invocation does not have any parameters, include an empty placeholder in the snippet template
                // to ensure the caret starts inside the parentheses and can track changes to other overloads (which may
                // have parameters).
                template.Append("$placeholder$");
                declarations.Add(new XElement(
                    snippetNamespace + "Literal",
                    new XAttribute(snippetNamespace + "Editable", "true"),
                    new XElement(snippetNamespace + "ID", new XText("placeholder")),
                    new XElement(snippetNamespace + "ToolTip", new XText("")),
                    new XElement(snippetNamespace + "Default", new XText("")),
                    new XElement(snippetNamespace + "Function")));
            }

            if (includeMethod)
            {
                template.Append(")$end$");
            }

            // A snippet is manually constructed. Replacement fields are added for each argument, and the field name
            // matches the parameter name.
            // https://docs.microsoft.com/en-us/visualstudio/ide/code-snippets-schema-reference?view=vs-2019
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    snippetNamespace + "CodeSnippets",
                    new XElement(
                        snippetNamespace + "CodeSnippet",
                        new XAttribute(snippetNamespace + "Format", "1.0.0"),
                        new XElement(
                            snippetNamespace + "Header",
                            new XElement(
                                snippetNamespace + "SnippetTypes",
                                new XElement(snippetNamespace + "SnippetType", new XText("Expansion"))),
                            new XElement(snippetNamespace + "Title", new XText(methodName)),
                            new XElement(snippetNamespace + "Author", "Microsoft"),
                            new XElement(snippetNamespace + "Description"),
                            new XElement(snippetNamespace + "HelpUrl"),
                            new XElement(snippetNamespace + "Shortcut", methodName)),
                        new XElement(
                            snippetNamespace + "Snippet",
                            new XElement(snippetNamespace + "Declarations", declarations.ToArray()),
                            new XElement(
                                snippetNamespace + "Code",
                                new XAttribute(snippetNamespace + "Language", "csharp"),
                                new XCData(template.ToString()))))));
        }

        private void OnModelUpdated(object sender, Model e)
        {
            AssertIsForeground();

            if (e is null)
            {
                return;
            }

            if (!_state.IsFullMethodCallSnippet)
            {
                return;
            }

            var document = SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document is null)
            {
                return;
            }

            var newSymbolKey = (e.SelectedItem as AbstractSignatureHelpProvider.SymbolKeySignatureHelpItem)?.SymbolKey ?? default;
            var newSymbol = newSymbolKey.Resolve(document.Project.GetRequiredCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None), cancellationToken: CancellationToken.None).GetAnySymbol();
            if (newSymbol is not IMethodSymbol method)
                return;

            MoveToSymbol(_state._symbols, method, CancellationToken.None);
        }

        private async Task<ImmutableArray<ISymbol>> GetSymbolsAsync(
            SnapshotPoint caretPosition,
            CancellationToken cancellationToken)
        {
            var document = SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document is null)
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var token = await semanticModel.SyntaxTree.GetTouchingTokenAsync(caretPosition.Position, cancellationToken).ConfigureAwait(false);
            var semanticInfo = semanticModel.GetSemanticInfo(token, document.Project.Solution.Workspace, cancellationToken);
            return semanticInfo.ReferencedSymbols;
        }

        public void MoveToSymbol(ImmutableArray<IMethodSymbol> symbols, IMethodSymbol symbol, CancellationToken cancellationToken)
        {
            AssertIsForeground();

            if (ExpansionSession is null)
            {
                return;
            }

            if (SymbolEqualityComparer.Default.Equals(_state._symbol, symbol))
            {
                return;
            }

            var symbolName = _state._symbol?.Name ?? _state._symbols.FirstOrDefault()?.Name;
            if (symbolName != symbol.Name)
            {
                // Can't track this request
                _state.ClearSymbolInformation();
                return;
            }

            if (!_state._symbols.IsDefaultOrEmpty && _state._symbols != symbols)
            {
                // Can't track this request
                _state.ClearSymbolInformation();
                return;
            }

            var textViewModel = TextView.TextViewModel;
            if (textViewModel == null)
            {
                Debug.Assert(TextView.IsClosed);
                return;
            }

            var buffer = EditorAdaptersFactoryService.GetBufferAdapter(textViewModel.DataBuffer);
            if (buffer is not IVsExpansion expansion)
            {
                return;
            }

            // Track current argument values
            if (_state._symbol is not null)
            {
                foreach (var previousParameter in _state._symbol.Parameters)
                {
                    if (ExpansionSession.GetFieldValue(previousParameter.Name, out var previousValue) == VSConstants.S_OK)
                    {
                        _state._arguments = _state._arguments.SetItem(previousParameter.Name, previousValue);
                    }
                }
            }

            var textSpan = new VsTextSpan[1];
            if (ExpansionSession is null || ExpansionSession.GetSnippetSpan(textSpan) != VSConstants.S_OK)
            {
                return;
            }

            var adjustedTextSpan = textSpan[0];
            var firstField = _state._symbol?.Parameters.FirstOrDefault()?.Name ?? "placeholder";
            if (ExpansionSession.GetFieldSpan(firstField, textSpan) != VSConstants.S_OK)
            {
                return;
            }

            adjustedTextSpan.iStartLine = textSpan[0].iStartLine;
            adjustedTextSpan.iStartIndex = textSpan[0].iStartIndex;

            var lastField = _state._symbol?.Parameters.LastOrDefault()?.Name ?? "placeholder";
            if (ExpansionSession.GetFieldSpan(lastField, textSpan) != VSConstants.S_OK)
            {
                return;
            }

            adjustedTextSpan.iEndLine = textSpan[0].iEndLine;
            adjustedTextSpan.iEndIndex = textSpan[0].iEndIndex;

            var snippet = CreateSnippet(symbol.Name, includeMethod: false, symbol.Parameters, cancellationToken);
            var doc = new DOMDocumentClass();
            if (doc.loadXML(snippet.ToString(SaveOptions.OmitDuplicateNamespaces)))
            {
                // Avoid clearing symbol information when InsertSpecificExpansion ends the current snippet session; the
                // new session will need the same information to carry argument values forward.
                _state._preserveSymbols = true;

                Debug.Assert(_state._symbols == symbols);
                _state._symbol = symbol;
                var previousArguments = _state._arguments;

                if (expansion.InsertSpecificExpansion(doc, adjustedTextSpan, this, LanguageServiceGuid, pszRelativePath: null, out _state._expansionSession) == VSConstants.S_OK)
                {
                    _state._preserveSymbols = false;
                    Debug.Assert(_state._symbols == symbols);
                    Debug.Assert(_state._symbol == symbol);
                    Debug.Assert(_state._arguments == previousArguments);

                    // Even though the closing parenthesis is not part of the updated snippet, make sure the $end$
                    // position lies after it.
                    if (ExpansionSession.GetEndSpan(textSpan) == VSConstants.S_OK)
                    {
                        textSpan[0].iStartIndex++;
                        textSpan[0].iEndIndex++;
                        ExpansionSession.SetEndSpan(textSpan[0]);
                    }
                }
                else
                {
                    _state.Clear(forceClearSymbolInformation: true);
                }
            }
        }

        public int EndExpansion()
        {
            if (ExpansionSession == null)
            {
                earlyEndExpansionHappened = true;
            }

            // This call to EndExpansion may occur within a call to InsertSpecificExpansion (the current snippet session
            // is terminated before the new one is created to replace it). Since _state may contain symbol information
            // used by snippet functions during the creation of the new snippet, we only want to clear symbol
            // information if the state hasn't set the _preserveSymbols flag.
            _state.Clear(forceClearSymbolInformation: false);

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

            _state._expansionSession = pSession;

            // Symbol information (when necessary) is set by the caller

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

            int hr;
            try
            {
                VsTextSpan textSpan;
                GetCaretPositionInSurfaceBuffer(out textSpan.iStartLine, out textSpan.iStartIndex);

                textSpan.iEndLine = textSpan.iStartLine;
                textSpan.iEndIndex = textSpan.iStartIndex;

                var expansion = EditorAdaptersFactoryService.GetBufferAdapter(textViewModel.DataBuffer) as IVsExpansion;
                earlyEndExpansionHappened = false;
                _state.ClearSymbolInformation();
                hr = expansion.InsertNamedExpansion(pszTitle, pszPath, textSpan, this, LanguageServiceGuid, fShowDisambiguationUI: 0, pSession: out _state._expansionSession);

                if (earlyEndExpansionHappened)
                {
                    // EndExpansion was called before InsertNamedExpansion returned, so set
                    // expansionSession to null to indicate that there is no active expansion
                    // session. This can occur when the snippet inserted doesn't have any expansion
                    // fields.
                    _state._expansionSession = null;
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
            var allowInHiddenRegions = documentWithImports.CanAddImportsInHiddenRegions();

            documentWithImports = AddImports(documentWithImports, position, snippetNode, placeSystemNamespaceFirst, allowInHiddenRegions, cancellationToken);
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

        [NonCopyable]
        private struct State
        {
            /// <summary>
            /// The current expansion session.
            /// </summary>
            public IVsExpansionSession _expansionSession;

            /// <summary>
            /// The set of symbols initially identified as candidates for providing arguments. When a snippet is
            /// constructed with parameters for a specific symbol, <see cref="_symbol"/> will identify the specific
            /// symbol from within this collection matching the current session.
            /// </summary>
            public ImmutableArray<IMethodSymbol> _symbols;

            /// <summary>
            /// The current symbol presented in an Argument Provider snippet session. This may be null if Signature Help
            /// has not yet provided a symbol to show.
            /// </summary>
            public IMethodSymbol _symbol;

            /// <summary>
            /// Maps from parameter name to current argument value. When this dictionary does not contain a mapping for
            /// a parameter, it means no argument has been provided yet by an ArgumentProvider or the user for a
            /// parameter with this name. This map is cleared at the final end of an argument provider snippet session.
            /// </summary>
            public ImmutableDictionary<string, string> _arguments;

            /// <summary>
            /// When moving between Signature Help overloads, the snippet session used for argument providers is cleared
            /// and recreated. When <see langword="true"/>, this field instructs the snippet client to avoid clearing
            /// symbol information when a snippet session is cleared, and is used in cases where a new snippet session
            /// is being created to use the same symbols.
            /// </summary>
            public bool _preserveSymbols;

            /// <summary>
            /// <see langword="true"/> if the current snippet session is a Full Method Call snippet session; otherwise,
            /// <see langword="false"/> if there is no current snippet session or if the current snippet session is a normal snippet.
            /// </summary>
            public bool IsFullMethodCallSnippet => _expansionSession is not null && !_symbols.IsDefaultOrEmpty;

            public void Clear(bool forceClearSymbolInformation = false)
            {
                _expansionSession = null;
                if (forceClearSymbolInformation || !_preserveSymbols)
                {
                    ClearSymbolInformation();
                }
            }

            /// <summary>
            /// Clears symbol information from the current snippet state.
            /// </summary>
            /// <remarks>
            /// This method always clears symbol information, including setting <see cref="_preserveSymbols"/> to
            /// <see langword="false"/>.
            /// </remarks>
            public void ClearSymbolInformation()
            {
                _preserveSymbols = false;
                _symbols = default;
                _symbol = null;
                _arguments = _arguments.Clear();
            }
        }
    }
}
