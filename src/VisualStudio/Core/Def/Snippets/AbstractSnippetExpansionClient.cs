// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
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
        /// <summary>
        /// The name of a snippet field created for caret placement in Full Method Call snippet sessions when the
        /// invocation has no parameters.
        /// </summary>
        private const string PlaceholderSnippetField = "placeholder";

        /// <summary>
        /// A generated random string which is used to identify argument completion snippets from other snippets.
        /// </summary>
        private static readonly string s_fullMethodCallDescriptionSentinel = Guid.NewGuid().ToString("N");

        private readonly SignatureHelpControllerProvider _signatureHelpControllerProvider;
        private readonly IEditorCommandHandlerServiceFactory _editorCommandHandlerServiceFactory;
        protected readonly IVsEditorAdaptersFactoryService EditorAdaptersFactoryService;
        protected readonly Guid LanguageServiceGuid;
        protected readonly ITextView TextView;
        protected readonly ITextBuffer SubjectBuffer;
        internal readonly EditorOptionsService EditorOptionsService;

        private readonly ImmutableArray<Lazy<ArgumentProvider, OrderableLanguageMetadata>> _allArgumentProviders;
        private ImmutableArray<ArgumentProvider> _argumentProviders;

        private bool _indentCaretOnCommit;
        private int _indentDepth;
        private bool _earlyEndExpansionHappened;

        /// <summary>
        /// Set to <see langword="true"/> when the snippet client registers an event listener for
        /// <see cref="Controller.ModelUpdated"/>.
        /// </summary>
        /// <remarks>
        /// This field should only be used from the main thread.
        /// </remarks>
        private bool _registeredForSignatureHelpEvents;

        // Writes to this state only occur on the main thread.
        private readonly State _state = new();

        public AbstractSnippetExpansionClient(
            IThreadingContext threadingContext,
            Guid languageServiceGuid,
            ITextView textView,
            ITextBuffer subjectBuffer,
            SignatureHelpControllerProvider signatureHelpControllerProvider,
            IEditorCommandHandlerServiceFactory editorCommandHandlerServiceFactory,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            ImmutableArray<Lazy<ArgumentProvider, OrderableLanguageMetadata>> argumentProviders,
            EditorOptionsService editorOptionsService)
            : base(threadingContext)
        {
            LanguageServiceGuid = languageServiceGuid;
            TextView = textView;
            SubjectBuffer = subjectBuffer;
            _signatureHelpControllerProvider = signatureHelpControllerProvider;
            _editorCommandHandlerServiceFactory = editorCommandHandlerServiceFactory;
            EditorAdaptersFactoryService = editorAdaptersFactoryService;
            EditorOptionsService = editorOptionsService;
            _allArgumentProviders = argumentProviders;
        }

        /// <inheritdoc cref="State._expansionSession"/>
        public IVsExpansionSession? ExpansionSession => _state._expansionSession;

        /// <inheritdoc cref="State.IsFullMethodCallSnippet"/>
        public bool IsFullMethodCallSnippet => _state.IsFullMethodCallSnippet;

        public ImmutableArray<ArgumentProvider> GetArgumentProviders(Workspace workspace)
        {
            AssertIsForeground();

            // TODO: Move this to ArgumentProviderService: https://github.com/dotnet/roslyn/issues/50897
            if (_argumentProviders.IsDefault)
            {
                _argumentProviders = workspace.Services.SolutionServices
                    .SelectMatchingExtensionValues(ExtensionOrderer.Order(_allArgumentProviders), SubjectBuffer.ContentType)
                    .ToImmutableArray();
            }

            return _argumentProviders;
        }

        public int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction? pFunc)
        {
            if (!SnippetFunctionService.TryGetSnippetFunctionInfo(xmlFunctionNode.text, out var snippetFunctionName, out var param))
            {
                pFunc = null;
                return VSConstants.E_INVALIDARG;
            }

            switch (snippetFunctionName)
            {
                case "SimpleTypeName":
                    pFunc = new SnippetFunctionSimpleTypeName(this, SubjectBuffer, bstrFieldName, param, ThreadingContext);
                    return VSConstants.S_OK;
                case "ClassName":
                    pFunc = new SnippetFunctionClassName(this, SubjectBuffer, bstrFieldName, ThreadingContext);
                    return VSConstants.S_OK;
                case "GenerateSwitchCases":
                    pFunc = new SnippetFunctionGenerateSwitchCases(this, SubjectBuffer, bstrFieldName, param, ThreadingContext);
                    return VSConstants.S_OK;
                default:
                    pFunc = null;
                    return VSConstants.E_INVALIDARG;
            }
        }
        protected abstract ITrackingSpan? InsertEmptyCommentAndGetEndPositionTrackingSpan();
        internal abstract Document AddImports(Document document, AddImportPlacementOptions addImportOptions, SyntaxFormattingOptions formattingOptions, int position, XElement snippetNode, CancellationToken cancellationToken);
        protected abstract string FallbackDefaultLiteral { get; }

        public int FormatSpan(IVsTextLines pBuffer, VsTextSpan[] tsInSurfaceBuffer)
        {
            AssertIsForeground();

            if (ExpansionSession == null)
            {
                return VSConstants.E_FAIL;
            }

            // If this is a manually-constructed snippet for a full method call, avoid formatting the snippet since
            // doing so will disrupt signature help. Check ExpansionSession instead of '_state.IsFullMethodCallSnippet'
            // because '_state._methodNameForInsertFullMethodCall' is not initialized at this point.
            if (ExpansionSession.TryGetHeaderNode("Description", out var descriptionNode)
                && descriptionNode?.text == s_fullMethodCallDescriptionSentinel)
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

            SubjectBuffer.FormatAndApplyToBuffer(formattingSpan, EditorOptionsService, CancellationToken.None);

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

        private void SetNewEndPosition(ITrackingSpan? endTrackingSpan)
        {
            RoslynDebug.AssertNotNull(ExpansionSession);
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

                    var formattingOptions = SubjectBuffer.GetLineFormattingOptions(EditorOptionsService, explicitFormat: false);
                    _indentDepth = lineText.GetColumnFromLineOffset(lineText.Length, formattingOptions.TabSize);

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

        protected static bool TryGetSnippetNode(IVsExpansionSession pSession, [NotNullWhen(true)] out XElement? snippetNode)
        {
            IXMLDOMNode? xmlNode = null;
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
            if (_indentCaretOnCommit && endLineText == string.Empty)
            {
                TextView.TryMoveCaretToAndEnsureVisible(new VirtualSnapshotPoint(TextView.TextSnapshot.GetPoint(endLinePosition), _indentDepth));
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
                return true;
            }

            return false;
        }

        public virtual bool TryHandleReturn()
        {
            return CommitSnippet(leaveCaret: false);
        }

        /// <summary>
        /// Commit the active snippet, if any.
        /// </summary>
        /// <param name="leaveCaret"><see langword="true"/> to leave the caret position unchanged by the call;
        /// otherwise, <see langword="false"/> to move the caret to the <c>$end$</c> position of the snippet when the
        /// snippet is committed.</param>
        /// <returns><see langword="true"/> if the caret may have moved from the call; otherwise,
        /// <see langword="false"/> if the caret did not move, or if there was no active snippet session to
        /// commit.</returns>
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
            if (buffer is not IVsExpansion expansion)
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

            if (TryInsertArgumentCompletionSnippet(triggerSpan, dataBufferSpan, expansion, textSpan, cancellationToken))
            {
                Debug.Assert(_state.IsFullMethodCallSnippet);
                return true;
            }

            if (expansion.InsertExpansion(textSpan, textSpan, this, LanguageServiceGuid, out _state._expansionSession) == VSConstants.S_OK)
            {
                // This expansion is not derived from a symbol, so make sure the state isn't tracking any symbol
                // information
                Debug.Assert(!_state.IsFullMethodCallSnippet);
                return true;
            }

            return false;
        }

        private bool TryInsertArgumentCompletionSnippet(SnapshotSpan triggerSpan, SnapshotSpan dataBufferSpan, IVsExpansion expansion, VsTextSpan textSpan, CancellationToken cancellationToken)
        {
            var document = SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document is null)
            {
                // Couldn't identify the current document
                return false;
            }

            if (!(EditorOptionsService.GlobalOptions.GetOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, document.Project.Language) ?? false))
            {
                // Argument completion snippets are not enabled
                return false;
            }

            var symbols = ThreadingContext.JoinableTaskFactory.Run(() => GetReferencedSymbolsToLeftOfCaretAsync(document, caretPosition: triggerSpan.End, cancellationToken));

            var methodSymbols = symbols.OfType<IMethodSymbol>().ToImmutableArray();
            if (methodSymbols.Any())
            {
                // This is the method name as it appears in source text
                var methodName = dataBufferSpan.GetText();
                var snippet = CreateMethodCallSnippet(methodName, includeMethod: true, ImmutableArray<IParameterSymbol>.Empty, ImmutableDictionary<string, string>.Empty);

                var doc = (DOMDocument)new DOMDocumentClass();
                if (doc.loadXML(snippet.ToString(SaveOptions.OmitDuplicateNamespaces)))
                {
                    if (expansion.InsertSpecificExpansion(doc, textSpan, this, LanguageServiceGuid, pszRelativePath: null, out _state._expansionSession) == VSConstants.S_OK)
                    {
                        Debug.Assert(_state._expansionSession != null);
                        _state._methodNameForInsertFullMethodCall = methodSymbols.First().Name;
                        Debug.Assert(_state._method == null);

                        if (_signatureHelpControllerProvider.GetController(TextView, SubjectBuffer) is { } controller)
                        {
                            EnsureRegisteredForModelUpdatedEvents(this, controller);
                        }

                        // Trigger signature help after starting the snippet session
                        //
                        // TODO: Figure out why ISignatureHelpBroker.TriggerSignatureHelp doesn't work but this does.
                        // https://github.com/dotnet/roslyn/issues/50036
                        var editorCommandHandlerService = _editorCommandHandlerServiceFactory.GetService(TextView, SubjectBuffer);
                        editorCommandHandlerService.Execute((view, buffer) => new InvokeSignatureHelpCommandArgs(view, buffer), nextCommandHandler: null);

                        return true;
                    }
                }
            }

            return false;

            // Local function
            static void EnsureRegisteredForModelUpdatedEvents(AbstractSnippetExpansionClient client, Controller controller)
            {
                // Access to _registeredForSignatureHelpEvents is synchronized on the main thread
                client.ThreadingContext.ThrowIfNotOnUIThread();

                if (!client._registeredForSignatureHelpEvents)
                {
                    client._registeredForSignatureHelpEvents = true;
                    controller.ModelUpdated += client.OnModelUpdated;
                    client.TextView.Closed += delegate { controller.ModelUpdated -= client.OnModelUpdated; };
                }
            }
        }

        /// <summary>
        /// Creates a snippet for providing arguments to a call.
        /// </summary>
        /// <param name="methodName">The name of the method as it should appear in code.</param>
        /// <param name="includeMethod">
        /// <para><see langword="true"/> to include the method name and invocation parentheses in the resulting snippet;
        /// otherwise, <see langword="false"/> if the method name and parentheses are assumed to already exist and the
        /// template will only specify the argument placeholders. Since the <c>$end$</c> marker is always considered to
        /// lie after the closing <c>)</c> of the invocation, it is only included when this parameter is
        /// <see langword="true"/>.</para>
        ///
        /// <para>For example, consider a call to <see cref="int.ToString(IFormatProvider)"/>. If
        /// <paramref name="includeMethod"/> is <see langword="true"/>, the resulting snippet text might look like
        /// this:</para>
        ///
        /// <code>
        /// ToString($provider$)$end$
        /// </code>
        ///
        /// <para>If <paramref name="includeMethod"/> is <see langword="false"/>, the resulting snippet text might look
        /// like this:</para>
        ///
        /// <code>
        /// $provider$
        /// </code>
        ///
        /// <para>This parameter supports cycling between overloads of a method for argument completion. Since any text
        /// edit that alters the <c>(</c> or <c>)</c> characters will force the Signature Help session to close, we are
        /// careful to only update text that lies between these characters.</para>
        /// </param>
        /// <param name="parameters">The parameters to the method. If the specific target of the invocation is not
        /// known, an empty array may be passed to create a template with a placeholder where arguments will eventually
        /// go.</param>
        private static XDocument CreateMethodCallSnippet(string methodName, bool includeMethod, ImmutableArray<IParameterSymbol> parameters, ImmutableDictionary<string, string> parameterValues)
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
                    new XElement(snippetNamespace + "ID", new XText(parameter.Name)),
                    new XElement(snippetNamespace + "Default", new XText(parameterValues.GetValueOrDefault(parameter.Name, "")))));
            }

            if (!declarations.Any())
            {
                // If the invocation does not have any parameters, include an empty placeholder in the snippet template
                // to ensure the caret starts inside the parentheses and can track changes to other overloads (which may
                // have parameters).
                template.Append($"${PlaceholderSnippetField}$");
                declarations.Add(new XElement(
                    snippetNamespace + "Literal",
                    new XElement(snippetNamespace + "ID", new XText(PlaceholderSnippetField)),
                    new XElement(snippetNamespace + "Default", new XText(""))));
            }

            if (includeMethod)
            {
                template.Append(')');
            }

            template.Append("$end$");

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
                            new XElement(snippetNamespace + "Title", new XText(methodName)),
                            new XElement(snippetNamespace + "Description", new XText(s_fullMethodCallDescriptionSentinel))),
                        new XElement(
                            snippetNamespace + "Snippet",
                            new XElement(snippetNamespace + "Declarations", declarations.ToArray()),
                            new XElement(
                                snippetNamespace + "Code",
                                new XAttribute(snippetNamespace + "Language", "csharp"),
                                new XCData(template.ToString()))))));
        }

        private void OnModelUpdated(object sender, ModelUpdatedEventsArgs e)
        {
            AssertIsForeground();

            if (e.NewModel is null)
            {
                // Signature Help was dismissed, but it's possible for a user to bring it back with Ctrl+Shift+Space.
                // Leave the snippet session (if any) in its current state to allow it to process either a subsequent
                // Signature Help update or the Escape/Enter keys that close the snippet session.
                return;
            }

            if (!_state.IsFullMethodCallSnippet)
            {
                // Signature Help is showing an updated signature, but either there is no active snippet, or the active
                // snippet is not performing argument value completion, so we just ignore it.
                return;
            }

            if (!e.NewModel.UserSelected && _state._method is not null)
            {
                // This was an implicit signature change which was not triggered by user pressing up/down, and we are
                // already showing an initialized argument completion snippet session, so avoid switching sessions.
                return;
            }

            var document = SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document is null)
            {
                // It's unclear if/how this state would occur, but if it does we would throw an exception trying to
                // use it. Just return immediately.
                return;
            }

            // TODO: The following blocks the UI thread without cancellation, but it only occurs when an argument value
            // completion session is active, which is behind an experimental feature flag.
            // https://github.com/dotnet/roslyn/issues/50634
            var compilation = ThreadingContext.JoinableTaskFactory.Run(() => document.Project.GetRequiredCompilationAsync(CancellationToken.None));
            var newSymbolKey = (e.NewModel.SelectedItem as AbstractSignatureHelpProvider.SymbolKeySignatureHelpItem)?.SymbolKey ?? default;
            var newSymbol = newSymbolKey.Resolve(compilation, cancellationToken: CancellationToken.None).GetAnySymbol();
            if (newSymbol is not IMethodSymbol method)
                return;

            MoveToSpecificMethod(method, CancellationToken.None);
        }

        private static async Task<ImmutableArray<ISymbol>> GetReferencedSymbolsToLeftOfCaretAsync(
            Document document,
            SnapshotPoint caretPosition,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var token = await semanticModel.SyntaxTree.GetTouchingWordAsync(caretPosition.Position, document.GetRequiredLanguageService<ISyntaxFactsService>(), cancellationToken).ConfigureAwait(false);
            if (token.RawKind == 0)
            {
                // There is no touching word, so return empty immediately
                return ImmutableArray<ISymbol>.Empty;
            }

            var semanticInfo = semanticModel.GetSemanticInfo(token, document.Project.Solution.Services, cancellationToken);
            return semanticInfo.ReferencedSymbols;
        }

        /// <summary>
        /// Update the current argument value completion session to use a specific method.
        /// </summary>
        /// <param name="method">The currently-selected method in Signature Help.</param>
        /// <param name="cancellationToken">A cancellation token the operation may observe.</param>
        public void MoveToSpecificMethod(IMethodSymbol method, CancellationToken cancellationToken)
        {
            AssertIsForeground();

            if (ExpansionSession is null)
            {
                return;
            }

            if (SymbolEquivalenceComparer.Instance.Equals(_state._method, method))
            {
                return;
            }

            if (_state._methodNameForInsertFullMethodCall != method.Name)
            {
                // Signature Help is showing a signature that wasn't part of the set this argument value completion
                // session was created from. It's unclear how this state should be handled, so we stop processing
                // Signature Help updates for the current session.
                // TODO: https://github.com/dotnet/roslyn/issues/50636
                ExpansionSession.EndCurrentExpansion(fLeaveCaret: 1);
                return;
            }

            // If the first method overload chosen is a zero-parameter method, the snippet we'll create is the same snippet
            // as the one did initially. The editor appears to have a bug where inserting a zero-width snippet (when we update the parameters)
            // causes the inserted session to immediately dismiss; this works around that issue.
            if (_state._method is null && method.Parameters.Length == 0)
            {
                _state._method = method;
                return;
            }

            var document = SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document is null)
            {
                // Couldn't identify the current document
                ExpansionSession.EndCurrentExpansion(fLeaveCaret: 1);
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

            // We need to replace the portion of the existing Full Method Call snippet which appears inside parentheses.
            // This span starts at the beginning of the first snippet field, and ends at the end of the last snippet
            // field. Methods with no arguments still have an empty "placeholder" snippet field representing the initial
            // caret position when the snippet is created.
            var textSpan = new VsTextSpan[1];
            if (ExpansionSession.GetSnippetSpan(textSpan) != VSConstants.S_OK)
            {
                return;
            }

            var firstField = _state._method?.Parameters.FirstOrDefault()?.Name ?? PlaceholderSnippetField;
            if (ExpansionSession.GetFieldSpan(firstField, textSpan) != VSConstants.S_OK)
            {
                return;
            }

            VsTextSpan adjustedTextSpan;
            adjustedTextSpan.iStartLine = textSpan[0].iStartLine;
            adjustedTextSpan.iStartIndex = textSpan[0].iStartIndex;

            var lastField = _state._method?.Parameters.LastOrDefault()?.Name ?? PlaceholderSnippetField;
            if (ExpansionSession.GetFieldSpan(lastField, textSpan) != VSConstants.S_OK)
            {
                return;
            }

            adjustedTextSpan.iEndLine = textSpan[0].iEndLine;
            adjustedTextSpan.iEndIndex = textSpan[0].iEndIndex;

            // Track current argument values so input created/updated by a user is not lost when cycling through
            // Signature Help overloads:
            //
            // 1. For each parameter of the method currently presented as a snippet, the value of the argument as
            //    it appears in code.
            // 2. Place the argument values in a map from parameter name to current value.
            // 3. (Later) the values in the map can be read to avoid providing new values for equivalent parameters.
            var newArguments = _state._arguments;

            if (_state._method is null || !_state._method.Parameters.Any())
            {
                // If we didn't have any previous parameters, then there is only the placeholder in the snippet.
                // We don't want to lose what the user has typed there, if they typed something
                if (ExpansionSession.GetFieldValue(PlaceholderSnippetField, out var placeholderValue) == VSConstants.S_OK &&
                    placeholderValue.Length > 0)
                {
                    if (method.Parameters.Any())
                    {
                        newArguments = newArguments.SetItem(method.Parameters[0].Name, placeholderValue);
                    }
                    else
                    {
                        // TODO: if the user is typing before signature help updated the model, and we have no parameters here,
                        // should we still create a new snippet that has the existing placeholder text?
                    }
                }
            }
            else if (_state._method is not null)
            {
                foreach (var previousParameter in _state._method.Parameters)
                {
                    if (ExpansionSession.GetFieldValue(previousParameter.Name, out var previousValue) == VSConstants.S_OK)
                    {
                        newArguments = newArguments.SetItem(previousParameter.Name, previousValue);
                    }
                }
            }

            // Now compute the new arguments for the new call
            var semanticModel = document.GetRequiredSemanticModelAsync(cancellationToken).AsTask().WaitAndGetResult(cancellationToken);
            var position = SubjectBuffer.CurrentSnapshot.GetPosition(adjustedTextSpan.iStartLine, adjustedTextSpan.iStartIndex);

            foreach (var parameter in method.Parameters)
            {
                newArguments.TryGetValue(parameter.Name, out var value);

                foreach (var provider in GetArgumentProviders(document.Project.Solution.Workspace))
                {
                    var context = new ArgumentContext(provider, semanticModel, position, parameter, value, cancellationToken);
                    ThreadingContext.JoinableTaskFactory.Run(() => provider.ProvideArgumentAsync(context));

                    if (context.DefaultValue is not null)
                    {
                        value = context.DefaultValue;
                        break;
                    }
                }

                // If we still have no value, fill in the default
                value ??= FallbackDefaultLiteral;

                newArguments = newArguments.SetItem(parameter.Name, value);
            }

            var snippet = CreateMethodCallSnippet(method.Name, includeMethod: false, method.Parameters, newArguments);
            var doc = (DOMDocument)new DOMDocumentClass();
            if (doc.loadXML(snippet.ToString(SaveOptions.OmitDuplicateNamespaces)))
            {
                if (expansion.InsertSpecificExpansion(doc, adjustedTextSpan, this, LanguageServiceGuid, pszRelativePath: null, out _state._expansionSession) == VSConstants.S_OK)
                {
                    Debug.Assert(_state._expansionSession != null);
                    _state._methodNameForInsertFullMethodCall = method.Name;
                    _state._method = method;
                    _state._arguments = newArguments;

                    // On this path, the closing parenthesis is not part of the updated snippet, so there is no way for
                    // the snippet itself to represent the $end$ marker (which falls after the ')' character). Instead,
                    // we use the internal APIs to manually specify the effective position of the $end$ marker as the
                    // location in code immediately following the ')'. To do this, we use the knowledge that the snippet
                    // includes all text up to (but not including) the ')', and move that span one position to the
                    // right.
                    if (ExpansionSession.GetEndSpan(textSpan) == VSConstants.S_OK)
                    {
                        textSpan[0].iStartIndex++;
                        textSpan[0].iEndIndex++;
                        ExpansionSession.SetEndSpan(textSpan[0]);
                    }
                }
            }
        }

        public int EndExpansion()
        {
            if (ExpansionSession == null)
            {
                _earlyEndExpansionHappened = true;
            }

            _state.Clear();

            _indentCaretOnCommit = false;

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

                var expansion = (IVsExpansion?)EditorAdaptersFactoryService.GetBufferAdapter(textViewModel.DataBuffer);
                Contract.ThrowIfNull(expansion);

                _earlyEndExpansionHappened = false;

                hr = expansion.InsertNamedExpansion(pszTitle, pszPath, textSpan, this, LanguageServiceGuid, fShowDisambiguationUI: 0, pSession: out _state._expansionSession);

                if (_earlyEndExpansionHappened)
                {
                    // EndExpansion was called before InsertNamedExpansion returned, so set
                    // expansionSession to null to indicate that there is no active expansion
                    // session. This can occur when the snippet inserted doesn't have any expansion
                    // fields.
                    _state._expansionSession = null;
                    _earlyEndExpansionHappened = false;
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
            Contract.ThrowIfNull(vsTextView);
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

            var documentWithImports = SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (documentWithImports == null)
            {
                return;
            }

            var languageServices = documentWithImports.Project.Services;
            var addImportOptions = SubjectBuffer.GetAddImportPlacementOptions(EditorOptionsService, languageServices, documentWithImports.AllowImportsInHiddenRegions());
            var formattingOptions = SubjectBuffer.GetSyntaxFormattingOptions(EditorOptionsService, languageServices, explicitFormat: false);

            documentWithImports = AddImports(documentWithImports, addImportOptions, formattingOptions, position, snippetNode, cancellationToken);
            AddReferences(documentWithImports.Project, snippetNode);
        }

        private static void AddReferences(Project originalProject, XElement snippetNode)
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

                var assemblyName = assemblyElement?.Value.Trim();

                if (RoslynString.IsNullOrEmpty(assemblyName))
                {
                    continue;
                }

                if (workspace is not VisualStudioWorkspaceImpl visualStudioWorkspace ||
                    !visualStudioWorkspace.TryAddReferenceToProject(projectId, assemblyName))
                {
                    failedReferenceAdditions.Add(assemblyName);
                }
            }

            if (failedReferenceAdditions.Any())
            {
                var notificationService = workspace.Services.GetRequiredService<INotificationService>();
                notificationService.SendNotification(
                    string.Format(ServicesVSResources.The_following_references_were_not_found_0_Please_locate_and_add_them_manually, Environment.NewLine)
                    + Environment.NewLine + Environment.NewLine
                    + string.Join(Environment.NewLine, failedReferenceAdditions),
                    severity: NotificationSeverity.Warning);
            }
        }

        protected static bool TryAddImportsToContainedDocument(Document document, IEnumerable<string> memberImportsNamespaces)
        {
            var containedDocument = ContainedDocument.TryGetContainedDocument(document.Id);
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

        private sealed class State
        {
            /// <summary>
            /// The current expansion session.
            /// </summary>
            public IVsExpansionSession? _expansionSession;

            /// <summary>
            /// The symbol name of the method that we have invoked insert full method call on; or <see langword="null"/>
            /// if there is no active snippet session or the active session is a regular snippets session.
            /// </summary>
            public string? _methodNameForInsertFullMethodCall;

            /// <summary>
            /// The current symbol presented in an Argument Provider snippet session. This may be null if Signature Help
            /// has not yet provided a symbol to show.
            /// </summary>
            public IMethodSymbol? _method;

            /// <summary>
            /// Maps from parameter name to current argument value. When this dictionary does not contain a mapping for
            /// a parameter, it means no argument has been provided yet by an ArgumentProvider or the user for a
            /// parameter with this name. This map is cleared at the final end of an argument provider snippet session.
            /// </summary>
            public ImmutableDictionary<string, string> _arguments = ImmutableDictionary.Create<string, string>();

            /// <summary>
            /// <see langword="true"/> if the current snippet session is a Full Method Call snippet session; otherwise,
            /// <see langword="false"/> if there is no current snippet session or if the current snippet session is a normal snippet.
            /// </summary>
            public bool IsFullMethodCallSnippet => _expansionSession is not null && _methodNameForInsertFullMethodCall is not null;

            public void Clear()
            {
                _expansionSession = null;
                _methodNameForInsertFullMethodCall = null;
                _method = null;
                _arguments = _arguments.Clear();
            }
        }
    }
}
