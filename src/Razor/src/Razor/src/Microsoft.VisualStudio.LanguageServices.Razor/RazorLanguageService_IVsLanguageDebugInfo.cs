// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Razor.Debugging;
using Microsoft.VisualStudio.Razor.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.Razor;

internal partial class RazorLanguageService : IVsLanguageDebugInfo
{
    private readonly IRazorBreakpointResolver _breakpointResolver;
    private readonly IRazorProximityExpressionResolver _proximityExpressionResolver;
    private readonly ILspServerActivationTracker _lspServerActivationTracker;
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
    private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;
    private readonly JoinableTaskFactory _joinableTaskFactory;

    public RazorLanguageService(
        IRazorBreakpointResolver breakpointResolver,
        IRazorProximityExpressionResolver proximityExpressionResolver,
        ILspServerActivationTracker lspServerActivationTracker,
        IUIThreadOperationExecutor uiThreadOperationExecutor,
        IVsEditorAdaptersFactoryService editorAdaptersFactory,
        JoinableTaskFactory joinableTaskFactory)
    {
        if (breakpointResolver is null)
        {
            throw new ArgumentNullException(nameof(breakpointResolver));
        }

        if (proximityExpressionResolver is null)
        {
            throw new ArgumentNullException(nameof(proximityExpressionResolver));
        }

        if (uiThreadOperationExecutor is null)
        {
            throw new ArgumentNullException(nameof(uiThreadOperationExecutor));
        }

        if (editorAdaptersFactory is null)
        {
            throw new ArgumentNullException(nameof(editorAdaptersFactory));
        }

        if (lspServerActivationTracker is null)
        {
            throw new ArgumentNullException(nameof(lspServerActivationTracker));
        }

        if (joinableTaskFactory is null)
        {
            throw new ArgumentNullException(nameof(joinableTaskFactory));
        }

        _breakpointResolver = breakpointResolver;
        _proximityExpressionResolver = proximityExpressionResolver;
        _lspServerActivationTracker = lspServerActivationTracker;
        _uiThreadOperationExecutor = uiThreadOperationExecutor;
        _editorAdaptersFactory = editorAdaptersFactory;
        _joinableTaskFactory = joinableTaskFactory;
    }

    public int GetProximityExpressions(IVsTextBuffer pBuffer, int iLine, int iCol, int cLines, out IVsEnumBSTR? ppEnum)
    {
        if (!_lspServerActivationTracker.IsActive)
        {
            // We can't do anything if our LSP server isn't up and running, and can't initialize here due to UI thread dependency issues
            // This method should only be called during a debugging sessions, so we should never hit this case, and if we do, we have much
            // bigger problems.
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }

        var textBuffer = _editorAdaptersFactory.GetDataBuffer(pBuffer);
        if (textBuffer is null)
        {
            // Can't resolve the text buffer, let someone else deal with this breakpoint.
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }

        var snapshot = textBuffer.CurrentSnapshot;
        if (!ValidateLocation(snapshot, iLine, iCol))
        {
            // The point disappeared between sessions. Do not evaluate proximity expressions here.
            ppEnum = null;
            return VSConstants.E_FAIL;
        }

        var proximityExpressions = _uiThreadOperationExecutor.Execute(
            title: SR.ProximityExpression_Dialog_Title,
            description: SR.ProximityExpression_Dialog_Description,
            allowCancellation: true,
            showProgress: true,
            (cancellationToken) => _proximityExpressionResolver.TryResolveProximityExpressionsAsync(textBuffer, iLine, iCol, cancellationToken), _joinableTaskFactory);

        if (proximityExpressions is null)
        {
            ppEnum = null;
            return VSConstants.E_FAIL;
        }

        ppEnum = new VsEnumBSTR(proximityExpressions);
        return VSConstants.S_OK;
    }

    public int ValidateBreakpointLocation(IVsTextBuffer pBuffer, int iLine, int iCol, TextSpan[] pCodeSpan)
    {
        if (!_lspServerActivationTracker.IsActive)
        {
            // We can't do anything if our LSP server isn't up and running, and can't initialize here due to UI thread dependency issues
            // Returning like this means the debugger will place a breakpoint in the margin, but not highlight a span in the line.
            // We trust that it will later validate the breakpoint location and remove it if it's not valid, and that validation
            // happens via LSP anyway.
            return VSConstants.E_NOTIMPL;
        }

        var textBuffer = _editorAdaptersFactory.GetDataBuffer(pBuffer);
        if (textBuffer is null)
        {
            // Can't resolve the text buffer, let someone else deal with this breakpoint.
            return VSConstants.E_NOTIMPL;
        }

        var snapshot = textBuffer.CurrentSnapshot;
        if (!ValidateLocation(snapshot, iLine, iCol))
        {
            // The point disappeared between sessions. Do not allow a breakpoint here.
            return VSConstants.E_FAIL;
        }

        var breakpointRange = _uiThreadOperationExecutor.Execute(
            title: "Determining breakpoint location...",
            description: "Razor Debugger",
            allowCancellation: true,
            showProgress: true,
            (cancellationToken) => _breakpointResolver.TryResolveBreakpointRangeAsync(textBuffer, iLine, iCol, cancellationToken), _joinableTaskFactory);

        if (breakpointRange is null)
        {
            // Failed to create the dialog at all or no applicable breakpoint location.
            return VSConstants.E_FAIL;
        }

        pCodeSpan[0] = new TextSpan()
        {
            iStartIndex = breakpointRange.Start.Character,
            iStartLine = breakpointRange.Start.Line,
            iEndIndex = breakpointRange.End.Character,
            iEndLine = breakpointRange.End.Line,
        };

        return VSConstants.S_OK;
    }

    public int GetNameOfLocation(IVsTextBuffer pBuffer, int iLine, int iCol, out string? pbstrName, out int piLineOffset)
    {
        pbstrName = default;
        piLineOffset = default;
        return VSConstants.E_NOTIMPL;
    }

    public int GetLocationOfName(string pszName, out string? pbstrMkDoc, TextSpan[] pspanLocation)
    {
        pbstrMkDoc = default;
        return VSConstants.E_NOTIMPL;
    }

    public int ResolveName(string pszName, uint dwFlags, out IVsEnumDebugName? ppNames)
    {
        ppNames = default;
        return VSConstants.E_NOTIMPL;
    }

    public int GetLanguageID(IVsTextBuffer pBuffer, int iLine, int iCol, out Guid pguidLanguageID)
    {
        pguidLanguageID = default;
        return VSConstants.E_NOTIMPL;
    }

    public int IsMappedLocation(IVsTextBuffer pBuffer, int iLine, int iCol)
    {
        return VSConstants.E_NOTIMPL;
    }

    private static bool ValidateLocation(ITextSnapshot snapshot, int lineNumber, int columnIndex)
    {
        if (lineNumber < 0 || lineNumber >= snapshot.LineCount)
        {
            return false;
        }

        var line = snapshot.GetLineFromLineNumber(lineNumber);
        if (columnIndex < 0 || columnIndex > line.Length)
        {
            return false;
        }

        return true;
    }
}
