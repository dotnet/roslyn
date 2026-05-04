// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using IVsDebugName = Microsoft.VisualStudio.TextManager.Interop.IVsDebugName;
using IVsEnumBSTR = Microsoft.VisualStudio.TextManager.Interop.IVsEnumBSTR;
using IVsTextBuffer = Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer;
using RESOLVENAMEFLAGS = Microsoft.VisualStudio.TextManager.Interop.RESOLVENAMEFLAGS;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal abstract partial class AbstractLanguageService<TPackage, TLanguageService>
{
    internal sealed class VsLanguageDebugInfo : IVsLanguageDebugInfo
    {
        private readonly Guid _languageId;
        private readonly TLanguageService _languageService;
        private readonly ILanguageDebugInfoService? _languageDebugInfo;
        private readonly IBreakpointResolutionService? _breakpointService;
        private readonly IProximityExpressionsService? _proximityExpressionsService;
        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

        public VsLanguageDebugInfo(
            Guid languageId,
            TLanguageService languageService,
            HostLanguageServices languageServiceProvider,
            IUIThreadOperationExecutor uiThreadOperationExecutor)
        {
            Contract.ThrowIfNull(languageService);
            Contract.ThrowIfNull(languageServiceProvider);

            _languageId = languageId;
            _languageService = languageService;
            _languageDebugInfo = languageServiceProvider.GetService<ILanguageDebugInfoService>();
            _breakpointService = languageServiceProvider.GetService<IBreakpointResolutionService>();
            _proximityExpressionsService = languageServiceProvider.GetService<IProximityExpressionsService>();
            _uiThreadOperationExecutor = uiThreadOperationExecutor;
        }

        private IThreadingContext ThreadingContext => _languageService.ThreadingContext;

        public int GetLanguageID(IVsTextBuffer pBuffer, int iLine, int iCol, out Guid pguidLanguageID)
        {
            pguidLanguageID = _languageId;
            return VSConstants.S_OK;
        }

        public int GetLocationOfName(string pszName, out string? pbstrMkDoc, out VsTextSpan pspanLocation)
        {
            pbstrMkDoc = null;
            pspanLocation = default;
            return VSConstants.E_NOTIMPL;
        }

        public int GetNameOfLocation(IVsTextBuffer pBuffer, int iLine, int iCol, out string? pbstrName, out int piLineOffset)
        {
            (pbstrName, piLineOffset) = GetNameOfLocationWorker();

            // Note(DustinCa): Docs say that GetNameOfLocation should return S_FALSE if a name could not be found.
            // Also, that's what the old native code does, so we should do it here.
            return pbstrName != null ? VSConstants.S_OK : VSConstants.S_FALSE;

            (string name, int lineOffset) GetNameOfLocationWorker()
            {
                return this.ThreadingContext.JoinableTaskFactory.Run(async () =>
                {
                    using (Logger.LogBlock(FunctionId.Debugging_VsLanguageDebugInfo_GetNameOfLocation, CancellationToken.None))
                    {
                        if (_languageDebugInfo == null)
                            return default;

                        using var waitContext = _uiThreadOperationExecutor.BeginExecute(
                            title: ServicesVSResources.Debugger,
                            defaultDescription: ServicesVSResources.Determining_breakpoint_location,
                            allowCancellation: true,
                            showProgress: false);

                        var cancellationToken = waitContext.UserCancellationToken;
                        var editorAdaptersFactoryService = _languageService.Package.ComponentModel.GetService<IVsEditorAdaptersFactoryService>();

                        var textBuffer = editorAdaptersFactoryService.GetDataBuffer(pBuffer);
                        if (textBuffer == null)
                            return default;

                        var nullablePoint = textBuffer.CurrentSnapshot.TryGetPoint(iLine, iCol);
                        if (!nullablePoint.HasValue)
                            return default;

                        var point = nullablePoint.Value;
                        var document = point.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                        if (document == null)
                            return default;

                        // NOTE(cyrusn): We have to wait here because the debuggers' 
                        // GetNameOfLocation is a blocking call.  In the future, it 
                        // would be nice if they could make it async.
                        var debugLocationInfo = await _languageDebugInfo.GetLocationInfoAsync(document, point, cancellationToken).ConfigureAwait(true);

                        if (debugLocationInfo.IsDefault)
                            return default;

                        return (debugLocationInfo.Name, debugLocationInfo.LineOffset);
                    }
                });
            }
        }

        public int GetProximityExpressions(IVsTextBuffer pBuffer, int iLine, int iCol, int cLines, out IVsEnumBSTR? ppEnum)
        {
            ppEnum = this.ThreadingContext.JoinableTaskFactory.Run(async () =>
            {
                // NOTE(cyrusn): cLines is ignored.  This is to match existing dev10 behavior.
                using (Logger.LogBlock(FunctionId.Debugging_VsLanguageDebugInfo_GetProximityExpressions, CancellationToken.None))
                {
                    using var context = _uiThreadOperationExecutor.BeginExecute(
                        title: ServicesVSResources.Debugger,
                        defaultDescription: ServicesVSResources.Determining_autos,
                        allowCancellation: true,
                        showProgress: false);

                    if (_proximityExpressionsService == null)
                        return null;

                    var editorAdaptersFactoryService = _languageService.Package.ComponentModel.GetService<IVsEditorAdaptersFactoryService>();

                    var textBuffer = editorAdaptersFactoryService.GetDataBuffer(pBuffer);
                    if (textBuffer == null)
                        return null;

                    var snapshot = textBuffer.CurrentSnapshot;
                    var nullablePoint = snapshot.TryGetPoint(iLine, iCol);
                    if (!nullablePoint.HasValue)
                        return null;

                    var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document == null)
                        return null;

                    var point = nullablePoint.Value;
                    var proximityExpressions = await _proximityExpressionsService.GetProximityExpressionsAsync(
                        document, point.Position, context.UserCancellationToken).ConfigureAwait(true);

                    if (proximityExpressions == null)
                        return null;

                    return new VsEnumBSTR(proximityExpressions);
                }
            });

            return ppEnum != null ? VSConstants.S_OK : VSConstants.E_FAIL;
        }

        public int IsMappedLocation(IVsTextBuffer pBuffer, int iLine, int iCol)
            => VSConstants.E_NOTIMPL;

        public int ResolveName(string? pszName, uint dwFlags, out IVsEnumDebugName? ppNames)
        {
            // In VS, this method frequently get's called with an empty string to test if the language service
            // supports this method (some language services, like F#, implement IVsLanguageDebugInfo but don't
            // implement this method).  In that scenario, there's no sense doing work, so we'll just return
            // S_FALSE (as the old VB language service did).
            if (pszName is null or "")
            {
                ppNames = null;
                return VSConstants.S_FALSE;
            }

            // NOTE(cyrusn): We have to wait here because the debuggers' ResolveName
            // call is synchronous.  In the future it would be nice to make it async.
            ppNames = this.ThreadingContext.JoinableTaskFactory.Run(async () =>
            {
                // We're in a blocking JTF run.  So ConfigureAwait(true) all calls to ensure we're coming back
                // and using the blocked thread whenever possible.

                using (Logger.LogBlock(FunctionId.Debugging_VsLanguageDebugInfo_ResolveName, CancellationToken.None))
                {
                    using var waitContext = _uiThreadOperationExecutor.BeginExecute(
                        title: ServicesVSResources.Debugger,
                        defaultDescription: ServicesVSResources.Resolving_breakpoint_location,
                        allowCancellation: true,
                        showProgress: false);

                    var cancellationToken = waitContext.UserCancellationToken;
                    if (dwFlags == (uint)RESOLVENAMEFLAGS.RNF_BREAKPOINT)
                    {
                        var solution = _languageService.Workspace.Value.CurrentSolution;

                        if (_breakpointService != null)
                        {
                            var breakpoints = await _breakpointService.ResolveBreakpointsAsync(
                                solution, pszName, cancellationToken).ConfigureAwait(true);
                            var debugNames = breakpoints.SelectAsArray(bp => CreateDebugName(bp, cancellationToken));

                            return new VsEnumDebugName(debugNames);
                        }
                    }
                }

                return null;
            });

            return ppNames != null ? VSConstants.S_OK : VSConstants.E_NOTIMPL;

            IVsDebugName CreateDebugName(
                BreakpointResolutionResult breakpoint, CancellationToken cancellationToken)
            {
                // We're in a blocking jtf run.  So CA(true) all calls to ensure we're coming bac
                // and using the blocked thread whenever possible.

                var document = breakpoint.Document;
                var filePath = _languageService.Workspace.Value.GetFilePath(document.Id);

                // We're (unfortunately) blocking the UI thread here.  So avoid async io as we actually
                // awant the IO to complete as quickly as possible, on this thread if necessary.
                var text = document.GetTextSynchronously(cancellationToken);
                var span = text.GetVsTextSpanForSpan(breakpoint.TextSpan);
                // If we're inside an Venus code nugget, we need to map the span to the surface buffer.
                // Otherwise, we'll just use the original span.
                var mappedSpan = span.MapSpanFromSecondaryBufferToPrimaryBuffer(this.ThreadingContext, document.Id);
                if (mappedSpan != null)
                    span = mappedSpan.Value;

                return new VsDebugName(breakpoint.LocationNameOpt, filePath!, span);
            }
        }

        public int ValidateBreakpointLocation(IVsTextBuffer pBuffer, int iLine, int iCol, VsTextSpan[] pCodeSpan)
        {
            return this.ThreadingContext.JoinableTaskFactory.Run(async () =>
            {
                using (Logger.LogBlock(FunctionId.Debugging_VsLanguageDebugInfo_ValidateBreakpointLocation, CancellationToken.None))
                {
                    using var waitContext = _uiThreadOperationExecutor.BeginExecute(
                        title: ServicesVSResources.Debugger,
                        defaultDescription: ServicesVSResources.Validating_breakpoint_location,
                        allowCancellation: true,
                        showProgress: false);

                    return await ValidateBreakpointLocationAsync(
                        pBuffer, iLine, iCol, pCodeSpan, waitContext.UserCancellationToken).ConfigureAwait(true);
                }
            });
        }

        private async Task<int> ValidateBreakpointLocationAsync(
            IVsTextBuffer pBuffer,
            int iLine,
            int iCol,
            VsTextSpan[] pCodeSpan,
            CancellationToken cancellationToken)
        {
            if (_breakpointService == null)
                return VSConstants.E_FAIL;

            var editorAdaptersFactoryService = _languageService.Package.ComponentModel.GetService<IVsEditorAdaptersFactoryService>();
            var textBuffer = editorAdaptersFactoryService.GetDataBuffer(pBuffer);
            if (textBuffer != null)
            {
                var snapshot = textBuffer.CurrentSnapshot;
                var nullablePoint = snapshot.TryGetPoint(iLine, iCol);
                if (nullablePoint == null)
                {
                    // The point disappeared between sessions. Do not allow a breakpoint here.
                    return VSConstants.E_FAIL;
                }

                var document = snapshot.AsText().GetDocumentWithFrozenPartialSemantics(cancellationToken);
                if (document != null)
                {
                    var point = nullablePoint.Value;
                    var length = 0;
                    if (pCodeSpan != null && pCodeSpan.Length > 0)
                    {
                        // If we have a non-empty span then it means that the debugger is asking us to adjust an
                        // existing span.  In Everett we didn't do this so we had some good and some bad
                        // behavior.  For example if you had a breakpoint on: "int i = 1;" and you changed it to "int
                        // i = 1, j = 2;", then the breakpoint wouldn't adjust.  That was bad.  However, if you had the
                        // breakpoint on an open or close curly brace then it would always "stick" to that brace
                        // which was good.
                        //
                        // So we want to keep the best parts of both systems.  We want to appropriately "stick"
                        // to tokens and we also want to adjust spans intelligently.
                        //
                        // However, it turns out the latter is hard to do when there are parse errors in the
                        // code.  Things like missing name nodes cause a lot of havoc and make it difficult to
                        // track a closing curly brace.
                        //
                        // So the way we do this is that we default to not intelligently adjusting the spans
                        // while there are parse errors.  But when there are no parse errors then the span is
                        // adjusted.
                        var initialBreakpointSpan = snapshot.GetSpan(pCodeSpan[0]);
                        if (initialBreakpointSpan.Length > 0 && document.SupportsSyntaxTree)
                        {
                            var tree = document.GetSyntaxTreeSynchronously(cancellationToken);
                            Contract.ThrowIfNull(tree);
                            if (tree.GetDiagnostics(cancellationToken).Any(d => d.Severity == DiagnosticSeverity.Error))
                            {
                                // Keep the span as is.
                                return VSConstants.S_OK;
                            }
                        }

                        // If a span is provided, and the requested position falls in that span, then just
                        // move the requested position to the start of the span.
                        // Length will be used to determine if we need further analysis, which is only required when text spans multiple lines.
                        if (initialBreakpointSpan.Contains(point))
                        {
                            point = initialBreakpointSpan.Start;
                            length = pCodeSpan[0].iEndLine > pCodeSpan[0].iStartLine ? initialBreakpointSpan.Length : 0;
                        }
                    }

                    // NOTE(cyrusn): we need to wait here because ValidateBreakpointLocation is
                    // synchronous.  In the future, it would be nice for the debugger to provide
                    // an async entry point for this.
                    var breakpoint = await _breakpointService.ResolveBreakpointAsync(
                        document, new TextSpan(point.Position, length), cancellationToken).ConfigureAwait(true);
                    if (breakpoint == null)
                    {
                        // There should *not* be a breakpoint here.  E_FAIL to let the debugger know
                        // that.
                        return VSConstants.E_FAIL;
                    }

                    if (breakpoint.IsLineBreakpoint)
                    {
                        // Let the debugger take care of this. They'll put a line breakpoint
                        // here. This is useful for when the user does something like put a
                        // breakpoint in inactive code.  We want to allow this as they might
                        // just have different defines during editing versus debugging.

                        // TODO(cyrusn): Do we need to set the pCodeSpan in this case?
                        return VSConstants.E_NOTIMPL;
                    }

                    // There should be a breakpoint at the location passed back.
                    if (pCodeSpan != null && pCodeSpan.Length > 0)
                        pCodeSpan[0] = breakpoint.TextSpan.ToSnapshotSpan(snapshot).ToVsTextSpan();

                    return VSConstants.S_OK;
                }
            }

            return VSConstants.E_NOTIMPL;
        }
    }
}
