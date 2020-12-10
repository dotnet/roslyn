// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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

        private readonly IEnumerable<Lazy<ArgumentProvider, OrderableLanguageMetadata>> _allArgumentProviders;
        private ImmutableArray<ArgumentProvider> _argumentProviders;

        protected bool indentCaretOnCommit;
        protected int indentDepth;
        protected bool earlyEndExpansionHappened;

        internal IVsExpansionSession ExpansionSession;
        private bool _preserveSymbols;
        private ImmutableArray<IMethodSymbol> _symbols;
        private IMethodSymbol _symbol;
        private ImmutableDictionary<string, string> _arguments = ImmutableDictionary.Create<string, string>();

        public AbstractSnippetExpansionClient(
            IThreadingContext threadingContext,
            Guid languageServiceGuid,
            ITextView textView,
            ITextBuffer subjectBuffer,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IEnumerable<Lazy<ArgumentProvider, OrderableLanguageMetadata>> argumentProviders)
            : base(threadingContext)
        {
            this.LanguageServiceGuid = languageServiceGuid;
            this.TextView = textView;
            this.SubjectBuffer = subjectBuffer;
            this.EditorAdaptersFactoryService = editorAdaptersFactoryService;
            this._allArgumentProviders = argumentProviders;
        }

        internal ImmutableDictionary<string, string> Arguments => _arguments;

        internal ImmutableArray<ArgumentProvider> GetArgumentProviders(Workspace workspace)
        {
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
            if (!_symbols.IsDefault)
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
                var isFullMethodCallSnippet = !_symbols.IsDefault;
                var tabbedInsideSnippetField = VSConstants.S_OK == ExpansionSession.GoToNextExpansionField(fCommitIfLast: isFullMethodCallSnippet ? 1 : 0);

                if (!tabbedInsideSnippetField)
                {
                    ExpansionSession.EndCurrentExpansion(fLeaveCaret: 1);
                    ExpansionSession = null;
                    _symbols = default;
                    _symbol = null;
                    _arguments = _arguments.Clear();
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
                    _symbols = default;
                    _symbol = null;
                    _arguments = _arguments.Clear();
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
                _symbols = default;
                _symbol = null;
                _arguments = _arguments.Clear();
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
                _symbols = default;
                _symbol = null;
                _arguments = _arguments.Clear();

                return hitWithinField;
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

            if (expansion.InsertExpansion(textSpan, textSpan, this, LanguageServiceGuid, out ExpansionSession) == VSConstants.S_OK)
            {
                // This expansion is not derived from a symbol
                _symbols = default;
                _symbol = null;
                _arguments = _arguments.Clear();
                return true;
            }

            if (!(SubjectBuffer.GetFeatureOnOffOption(CompletionOptions.EnableCallCompletionOnTabTab) ?? false))
            {
                // Full method call completion is not enabled
                return false;
            }

            var symbols = ThreadingContext.JoinableTaskFactory.Run(() =>
            {
                var caretPosition = SubjectBuffer.CurrentSnapshot.GetPoint(endPositionInSubjectBuffer);
                return GetSymbolsAsync(caretPosition, cancellationToken);
            });

            if (symbols.OfType<IMethodSymbol>().Any())
            {
                XNamespace snippetNamespace = "http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet";

                var methodName = dataBufferSpan.GetText();

                var template = $"{methodName}($placeholder$)$end$";

                var snippet = new XDocument(
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
                                new XElement(
                                    snippetNamespace + "Declarations",
                                    new XElement(
                                        snippetNamespace + "Literal",
                                        new XAttribute(snippetNamespace + "Editable", "true"),
                                        new XElement(snippetNamespace + "ID", new XText("placeholder")),
                                        new XElement(snippetNamespace + "ToolTip", new XText("")),
                                        new XElement(snippetNamespace + "Default", new XText("")),
                                        new XElement(snippetNamespace + "Function"))),
                                new XElement(
                                    snippetNamespace + "Code",
                                    new XAttribute(snippetNamespace + "Language", "csharp"),
                                    new XCData(template))))));

                var doc = new DOMDocumentClass();
                if (doc.loadXML(snippet.ToString(SaveOptions.OmitDuplicateNamespaces)))
                {
                    var methodSymbols = symbols.OfType<IMethodSymbol>().ToImmutableArray();
                    _symbols = methodSymbols;
                    _symbol = null;

                    if (expansion.InsertSpecificExpansion(doc, textSpan, this, LanguageServiceGuid, pszRelativePath: null, out ExpansionSession) == VSConstants.S_OK)
                    {
                        Debug.Assert(_symbols == methodSymbols);
                        Debug.Assert(_symbol == null);

                        // Trigger signature help after starting the snippet session
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
                                var controller = Controller.TryGetInstance(TextView, SubjectBuffer);
                                if (controller is null)
                                {
                                    await Task.Yield();
                                    continue;
                                }

                                controller.ModelUpdated -= OnModelUpdated;
                                controller.ModelUpdated += OnModelUpdated;
                                return;
                            }
                        });

                        return true;
                    }
                    else
                    {
                        _symbols = default;
                        _symbol = null;
                        _arguments = _arguments.Clear();
                    }
                }
            }

            return false;
        }

        private void OnModelUpdated(object sender, Model e)
        {
            if (e is null)
            {
                return;
            }

            if (_symbols.IsDefaultOrEmpty)
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

            MoveToSymbol(_symbols, method, CancellationToken.None);
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

            if (SymbolEqualityComparer.Default.Equals(_symbol, symbol))
            {
                return;
            }

            var symbolName = _symbol?.Name ?? _symbols.FirstOrDefault()?.Name;
            if (symbolName != symbol.Name)
            {
                // Can't track this request
                _symbols = default;
                _symbol = null;
                _arguments = _arguments.Clear();
                return;
            }

            if (!_symbols.IsDefaultOrEmpty && _symbols != symbols)
            {
                // Can't track this request
                _symbols = default;
                _symbol = null;
                _arguments = _arguments.Clear();
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
            if (_symbol is not null)
            {
                foreach (var previousParameter in _symbol.Parameters)
                {
                    if (ExpansionSession.GetFieldValue(previousParameter.Name, out var previousValue) == VSConstants.S_OK)
                    {
                        _arguments = _arguments.SetItem(previousParameter.Name, previousValue);
                    }
                }
            }

            XNamespace snippetNamespace = "http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet";

            var methodName = symbol.Name;

            var template = new StringBuilder();

            var declarations = new List<XElement>();
            foreach (var parameter in symbol.Parameters)
            {
                if (declarations.Any())
                {
                    template.Append(", ");
                }

                template.Append('$').Append(parameter.Name).Append('$');
                declarations.Add(new XElement(
                    snippetNamespace + "Literal",
                    new XAttribute(snippetNamespace + "Editable", "true"),
                    new XElement(snippetNamespace + "ID", new XText(parameter.Name)),
                    new XElement(snippetNamespace + "Function", new XText($"ArgumentValue({SymbolKey.CreateString(parameter, cancellationToken)})"))));
            }

            if (!declarations.Any())
            {
                template.Append("$placeholder$");
                declarations.Add(new XElement(
                    snippetNamespace + "Literal",
                    new XAttribute(snippetNamespace + "Editable", "true"),
                    new XElement(snippetNamespace + "ID", new XText("placeholder")),
                    new XElement(snippetNamespace + "ToolTip", new XText("")),
                    new XElement(snippetNamespace + "Default", new XText("")),
                    new XElement(snippetNamespace + "Function")));
            }

            var snippet = new XDocument(
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

            var textSpan = new VsTextSpan[1];
            if (ExpansionSession is null || ExpansionSession.GetSnippetSpan(textSpan) != VSConstants.S_OK)
            {
                return;
            }

            var adjustedTextSpan = textSpan[0];
            var firstField = _symbol?.Parameters.FirstOrDefault()?.Name ?? "placeholder";
            if (ExpansionSession.GetFieldSpan(firstField, textSpan) != VSConstants.S_OK)
            {
                return;
            }

            adjustedTextSpan.iStartLine = textSpan[0].iStartLine;
            adjustedTextSpan.iStartIndex = textSpan[0].iStartIndex;

            var lastField = _symbol?.Parameters.LastOrDefault()?.Name ?? "placeholder";
            if (ExpansionSession.GetFieldSpan(lastField, textSpan) != VSConstants.S_OK)
            {
                return;
            }

            adjustedTextSpan.iEndLine = textSpan[0].iEndLine;
            adjustedTextSpan.iEndIndex = textSpan[0].iEndIndex;

            // Explicitly carry forward arguments, since the field will be cleared when the new snippet session replaces
            // the current one.
            var previousArguments = _arguments;
            var doc = new DOMDocumentClass();
            if (doc.loadXML(snippet.ToString(SaveOptions.OmitDuplicateNamespaces)))
            {
                _preserveSymbols = true;
                _symbols = symbols;
                _symbol = symbol;
                _arguments = previousArguments;

                if (expansion.InsertSpecificExpansion(doc, adjustedTextSpan, this, LanguageServiceGuid, pszRelativePath: null, out ExpansionSession) == VSConstants.S_OK)
                {
                    _preserveSymbols = false;
                    Debug.Assert(_symbols == symbols);
                    Debug.Assert(_symbol == symbol);
                    Debug.Assert(_arguments == previousArguments);

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
                    _preserveSymbols = false;
                    _symbols = default;
                    _symbol = null;
                    _arguments = _arguments.Clear();
                }
            }
        }

        public int EndExpansion()
        {
            if (ExpansionSession == null)
            {
                earlyEndExpansionHappened = true;
            }

            ExpansionSession = null;
            if (!_preserveSymbols)
            {
                _symbols = default;
                _symbol = null;
                _arguments = _arguments.Clear();
            }

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
                _symbols = default;
                _symbol = null;
                _arguments = _arguments.Clear();
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
    }
}
