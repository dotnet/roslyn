// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense;

internal abstract class AbstractDebuggerIntelliSenseContext : IDisposable
{
    private readonly IWpfTextView _textView;
    private readonly IContentType _originalContentType;
    protected readonly IProjectionBufferFactoryService ProjectionBufferFactoryService;
    protected readonly TextManager.Interop.TextSpan CurrentStatementSpan;
    private IProjectionBuffer _projectionBuffer;
    private DebuggerTextView _debuggerTextView;
    private DebuggerIntelliSenseWorkspace _workspace;
    private ImmediateWindowContext _immediateWindowContext;
    private readonly IBufferGraphFactoryService _bufferGraphFactoryService;
    private readonly bool _isImmediateWindow;

    private class ImmediateWindowContext
    {
        public int CurrentLineIndex = -1;
        public int QuestionIndex = -2;
        public IProjectionBuffer ElisionBuffer;
        public IProjectionBuffer ProjectionBuffer;
    }

    protected AbstractDebuggerIntelliSenseContext(
        IWpfTextView wpfTextView,
        IVsTextView vsTextView,
        IVsTextLines vsDebuggerTextLines,
        ITextBuffer contextBuffer,
        TextManager.Interop.TextSpan[] currentStatementSpan,
        IComponentModel componentModel,
        IServiceProvider serviceProvider,
        IContentType contentType)
    {
        _textView = wpfTextView;
        DebuggerTextLines = vsDebuggerTextLines;
        this.ContextBuffer = contextBuffer;
        this.CurrentStatementSpan = currentStatementSpan[0];
        ContentType = contentType;
        _originalContentType = _textView.TextBuffer.ContentType;
        this.ProjectionBufferFactoryService = componentModel.GetService<IProjectionBufferFactoryService>();
        _bufferGraphFactoryService = componentModel.GetService<IBufferGraphFactoryService>();
        _isImmediateWindow = IsImmediateWindow((IVsUIShell)serviceProvider.GetService(typeof(SVsUIShell)), vsTextView);
    }

    // Constructor for testing
    protected AbstractDebuggerIntelliSenseContext(
        IWpfTextView wpfTextView,
        ITextBuffer contextBuffer,
        Microsoft.VisualStudio.TextManager.Interop.TextSpan[] currentStatementSpan,
        IComponentModel componentModel,
        IContentType contentType,
        bool isImmediateWindow)
    {
        _textView = wpfTextView;
        this.ContextBuffer = contextBuffer;
        this.CurrentStatementSpan = currentStatementSpan[0];
        ContentType = contentType;
        this.ProjectionBufferFactoryService = componentModel.GetService<IProjectionBufferFactoryService>();
        _bufferGraphFactoryService = componentModel.GetService<IBufferGraphFactoryService>();
        _isImmediateWindow = isImmediateWindow;
    }

    public IVsTextLines DebuggerTextLines { get; }

    public ITextView DebuggerTextView { get { return _debuggerTextView; } }

    public ITextBuffer Buffer { get { return _projectionBuffer; } }

    public IContentType ContentType { get; }

    protected bool InImmediateWindow { get { return _immediateWindowContext != null; } }

    internal ITextBuffer ContextBuffer { get; private set; }

    public abstract bool CompletionStartsOnQuestionMark { get; }

    protected abstract string StatementTerminator { get; }

    protected abstract int GetAdjustedContextPoint(int contextPoint, Document document);

    protected abstract ITrackingSpan GetPreviousStatementBufferAndSpan(int lastTokenEndPoint, Document document);

    // Since the immediate window doesn't actually tell us when we change lines, we'll have to
    // determine ourselves when to rebuild our tracking spans to include only the last (input)
    // line of the buffer.
    public void RebuildSpans()
    {
        // Not in the immediate window, no work to do.
        if (!this.InImmediateWindow)
        {
            return;
        }

        // Reset the question mark location, since we may have to search for one again.
        _immediateWindowContext.QuestionIndex = -2;
        SetupImmediateWindowProjectionBuffer();
    }

    internal bool TryInitialize()
        => this.TrySetContext(_isImmediateWindow);

    private bool TrySetContext(
        bool isImmediateWindow)
    {
        // Get the workspace, and from there, the solution and document containing this buffer.
        // If there's an ExternalSource, we won't get a document. Give up in that case.
        var document = ContextBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
        {
            _projectionBuffer = null;
            _debuggerTextView = null;
            _workspace = null;
            _immediateWindowContext = null;
            return false;
        }

        var solution = document.Project.Solution;

        // Get the appropriate ITrackingSpan for the window the user is typing in
        var viewSnapshot = _textView.TextSnapshot;
        _immediateWindowContext = null;
        var debuggerMappedSpan = isImmediateWindow
            ? CreateImmediateWindowProjectionMapping(out _immediateWindowContext)
            : viewSnapshot.CreateFullTrackingSpan(SpanTrackingMode.EdgeInclusive);

        // Wrap the original ContextBuffer in a projection buffer that we can make read-only
        this.ContextBuffer = this.ProjectionBufferFactoryService.CreateProjectionBuffer(null,
            new object[] { this.ContextBuffer.CurrentSnapshot.CreateFullTrackingSpan(SpanTrackingMode.EdgeInclusive) }, ProjectionBufferOptions.None, ContentType);

        // Make projection readonly so we can't edit it by mistake.
        using (var regionEdit = this.ContextBuffer.CreateReadOnlyRegionEdit())
        {
            regionEdit.CreateReadOnlyRegion(new Span(0, this.ContextBuffer.CurrentSnapshot.Length), SpanTrackingMode.EdgeInclusive, EdgeInsertionMode.Deny);
            regionEdit.Apply();
        }

        // Adjust the context point to ensure that the right information is in scope.
        // For example, we may need to move the point to the end of the last statement in a method body
        // in order to be able to access all local variables.
        var contextPoint = this.ContextBuffer.CurrentSnapshot.GetLineFromLineNumber(CurrentStatementSpan.iEndLine).Start + CurrentStatementSpan.iEndIndex;
        var adjustedContextPoint = GetAdjustedContextPoint(contextPoint, document);

        // Get the previous span/text. We might have to insert another newline or something.
        var previousStatementSpan = GetPreviousStatementBufferAndSpan(adjustedContextPoint, document);

        // Build the tracking span that includes the rest of the file
        var restOfFileSpan = ContextBuffer.CurrentSnapshot.CreateTrackingSpanFromIndexToEnd(adjustedContextPoint, SpanTrackingMode.EdgePositive);

        // Put it all into a projection buffer
        _projectionBuffer = this.ProjectionBufferFactoryService.CreateProjectionBuffer(null,
            new object[] { previousStatementSpan, debuggerMappedSpan, this.StatementTerminator, restOfFileSpan }, ProjectionBufferOptions.None, ContentType);

        // Fork the solution using this new primary buffer for the document and all of its linked documents.
        var forkedSolution = solution.WithDocumentText(document.Id, _projectionBuffer.CurrentSnapshot.AsText(), PreservationMode.PreserveIdentity);
        foreach (var link in document.GetLinkedDocumentIds())
        {
            forkedSolution = forkedSolution.WithDocumentText(link, _projectionBuffer.CurrentSnapshot.AsText(), PreservationMode.PreserveIdentity);
        }

        // Put it into a new workspace, and open it and its related documents
        // with the projection buffer as the text.
        _workspace = new DebuggerIntelliSenseWorkspace(forkedSolution);
        _workspace.OpenDocument(document.Id, _projectionBuffer.AsTextContainer());
        foreach (var link in document.GetLinkedDocumentIds())
        {
            _workspace.OpenDocument(link, _projectionBuffer.AsTextContainer());
        }

        // Start getting the compilation so the PartialSolution will be ready when the user starts typing in the window
        document.Project.GetCompilationAsync(System.Threading.CancellationToken.None);

        _textView.TextBuffer.ChangeContentType(ContentType, null);

        var bufferGraph = _bufferGraphFactoryService.CreateBufferGraph(_projectionBuffer);

        _debuggerTextView = new DebuggerTextView(_textView, bufferGraph, DebuggerTextLines, InImmediateWindow);
        return true;
    }

    internal void SetContentType(bool install)
    {
        var contentType = install ? ContentType : _originalContentType;
        _textView.TextBuffer.ChangeContentType(contentType, null);
    }

    private ITrackingSpan CreateImmediateWindowProjectionMapping(out ImmediateWindowContext immediateWindowContext)
    {
        var caretLine = _textView.Caret.ContainingTextViewLine.Extent;
        var currentLineIndex = _textView.TextSnapshot.GetLineNumberFromPosition(caretLine.Start.Position);

        var debuggerMappedSpan = _textView.TextSnapshot.CreateFullTrackingSpan(SpanTrackingMode.EdgeInclusive);
        var projectionBuffer = this.ProjectionBufferFactoryService.CreateProjectionBuffer(null,
            new object[] { debuggerMappedSpan }, ProjectionBufferOptions.PermissiveEdgeInclusiveSourceSpans, ContentType);

        // There's currently a bug in the editor (515925) where an elision buffer can't be projected into
        // another projection buffer.  So workaround by using a second projection buffer that only 
        // projects the text we care about
        var elisionProjectionBuffer = this.ProjectionBufferFactoryService.CreateProjectionBuffer(null,
            new object[] { projectionBuffer.CurrentSnapshot.CreateFullTrackingSpan(SpanTrackingMode.EdgeInclusive) },
            ProjectionBufferOptions.None, ContentType);

        immediateWindowContext = new ImmediateWindowContext()
        {
            ProjectionBuffer = projectionBuffer,
            ElisionBuffer = elisionProjectionBuffer
        };

        _textView.TextBuffer.PostChanged += TextBuffer_PostChanged;

        SetupImmediateWindowProjectionBuffer();

        return elisionProjectionBuffer.CurrentSnapshot.CreateFullTrackingSpan(SpanTrackingMode.EdgeInclusive);
    }

    private void TextBuffer_PostChanged(object sender, EventArgs e)
        => SetupImmediateWindowProjectionBuffer();

    /// <summary>
    /// If there's a ? mark, we want to skip the ? mark itself, and include the text that follows it
    /// </summary>
    private void SetupImmediateWindowProjectionBuffer()
    {
        var caretLine = _textView.Caret.ContainingTextViewLine.Extent;
        var currentLineIndex = _textView.TextSnapshot.GetLineNumberFromPosition(caretLine.Start.Position);
        var questionIndex = GetQuestionIndex(caretLine.GetText());

        if (_immediateWindowContext.QuestionIndex != questionIndex ||
            _immediateWindowContext.CurrentLineIndex != currentLineIndex)
        {
            _immediateWindowContext.QuestionIndex = questionIndex;
            _immediateWindowContext.CurrentLineIndex = currentLineIndex;
            _immediateWindowContext.ProjectionBuffer.DeleteSpans(0, _immediateWindowContext.ProjectionBuffer.CurrentSnapshot.SpanCount);
            _immediateWindowContext.ProjectionBuffer.InsertSpan(0, _textView.TextSnapshot.CreateTrackingSpanFromIndexToEnd(caretLine.Start.Position + questionIndex + 1, SpanTrackingMode.EdgeInclusive));
        }
    }

    private static int GetQuestionIndex(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                // Assume that the ? will be the first non-whitespace if it's being used as a
                // command
                return text[i] == '?' ? i : -1;
            }
        }

        return -1;
    }

    private static bool IsImmediateWindow(IVsUIShell shellService, IVsTextView textView)
    {
        Marshal.ThrowExceptionForHR(shellService.GetToolWindowEnum(out var windowEnum));
        Marshal.ThrowExceptionForHR(textView.GetBuffer(out _));

        var frame = new IVsWindowFrame[1];
        var immediateWindowGuid = Guid.Parse(ToolWindowGuids80.ImmediateWindow);

        while (windowEnum.Next(1, frame, out _) == VSConstants.S_OK)
        {
            Marshal.ThrowExceptionForHR(frame[0].GetGuidProperty((int)__VSFPROPID.VSFPROPID_GuidPersistenceSlot, out var toolWindowGuid));
            if (toolWindowGuid == immediateWindowGuid)
            {
                Marshal.ThrowExceptionForHR(frame[0].QueryViewInterface(typeof(IVsTextView).GUID, out var frameTextView));
                try
                {
                    var immediateWindowTextView = Marshal.GetObjectForIUnknown(frameTextView) as IVsTextView;
                    return textView == immediateWindowTextView;
                }
                finally
                {
                    Marshal.Release(frameTextView);
                }
            }
        }

        return false;
    }

    public void Dispose()
    {
        // Unsubscribe from events
        _textView.TextBuffer.PostChanged -= TextBuffer_PostChanged;
        _debuggerTextView.Cleanup();

        // The buffer graph subscribes to events of its source buffers, we're no longer interested
        _projectionBuffer.DeleteSpans(0, _projectionBuffer.CurrentSnapshot.SpanCount);

        // The next request will use a new workspace
        _workspace.Dispose();
    }
}
