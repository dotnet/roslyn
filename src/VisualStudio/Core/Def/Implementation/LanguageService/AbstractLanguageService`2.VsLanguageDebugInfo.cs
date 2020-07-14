// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;
using IVsDebugName = Microsoft.VisualStudio.TextManager.Interop.IVsDebugName;
using IVsEnumBSTR = Microsoft.VisualStudio.TextManager.Interop.IVsEnumBSTR;
using IVsTextBuffer = Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer;
using RESOLVENAMEFLAGS = Microsoft.VisualStudio.TextManager.Interop.RESOLVENAMEFLAGS;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService<TPackage, TLanguageService>
    {
        internal sealed class VsLanguageDebugInfo : IVsLanguageDebugInfo
        {
            private readonly Guid _languageId;
            private readonly TLanguageService _languageService;
            private readonly ILanguageDebugInfoService _languageDebugInfo;
            private readonly IBreakpointResolutionService _breakpointService;
            private readonly IProximityExpressionsService _proximityExpressionsService;
            private readonly IWaitIndicator _waitIndicator;
            private readonly CachedProximityExpressionsGetter _cachedProximityExpressionsGetter;

            public VsLanguageDebugInfo(
                Guid languageId,
                TLanguageService languageService,
                HostLanguageServices languageServiceProvider,
                IWaitIndicator waitIndicator)
            {
                Contract.ThrowIfNull(languageService);
                Contract.ThrowIfNull(languageServiceProvider);

                _languageId = languageId;
                _languageService = languageService;
                _languageDebugInfo = languageServiceProvider.GetService<ILanguageDebugInfoService>();
                _breakpointService = languageServiceProvider.GetService<IBreakpointResolutionService>();
                _proximityExpressionsService = languageServiceProvider.GetService<IProximityExpressionsService>();
                _cachedProximityExpressionsGetter = new CachedProximityExpressionsGetter(_proximityExpressionsService);
                _waitIndicator = waitIndicator;
            }

            internal void OnDebugModeChanged(DebugMode debugMode)
                => _cachedProximityExpressionsGetter.OnDebugModeChanged(debugMode);

            public int GetLanguageID(IVsTextBuffer pBuffer, int iLine, int iCol, out Guid pguidLanguageID)
            {
                pguidLanguageID = _languageId;
                return VSConstants.S_OK;
            }

            public int GetLocationOfName(string pszName, out string pbstrMkDoc, out VsTextSpan pspanLocation)
            {
                pbstrMkDoc = null;
                pspanLocation = default;
                return VSConstants.E_NOTIMPL;
            }

            public int GetNameOfLocation(IVsTextBuffer pBuffer, int iLine, int iCol, out string pbstrName, out int piLineOffset)
            {
                using (Logger.LogBlock(FunctionId.Debugging_VsLanguageDebugInfo_GetNameOfLocation, CancellationToken.None))
                {
                    string name = null;
                    var lineOffset = 0;
                    var succeeded = false;

                    if (_languageDebugInfo != null)
                    {
                        _waitIndicator.Wait(
                        title: ServicesVSResources.Debugger,
                        message: ServicesVSResources.Determining_breakpoint_location,
                        allowCancel: true,
                        action: waitContext =>
                        {
                            var cancellationToken = waitContext.CancellationToken;
                            var textBuffer = _languageService.EditorAdaptersFactoryService.GetDataBuffer(pBuffer);
                            if (textBuffer != null)
                            {
                                var nullablePoint = textBuffer.CurrentSnapshot.TryGetPoint(iLine, iCol);
                                if (nullablePoint.HasValue)
                                {
                                    var point = nullablePoint.Value;
                                    var document = point.Snapshot.GetOpenDocumentInCurrentContextWithChanges();

                                    if (document != null)
                                    {
                                        // NOTE(cyrusn): We have to wait here because the debuggers' 
                                        // GetNameOfLocation is a blocking call.  In the future, it 
                                        // would be nice if they could make it async.
                                        var debugLocationInfo = _languageDebugInfo.GetLocationInfoAsync(document, point, cancellationToken).WaitAndGetResult(cancellationToken);

                                        if (!debugLocationInfo.IsDefault)
                                        {
                                            succeeded = true;
                                            name = debugLocationInfo.Name;
                                            lineOffset = debugLocationInfo.LineOffset;
                                        }
                                    }
                                }
                            }
                        });

                        if (succeeded)
                        {
                            pbstrName = name;
                            piLineOffset = lineOffset;
                            return VSConstants.S_OK;
                        }
                    }

                    // Note(DustinCa): Docs say that GetNameOfLocation should return S_FALSE if a name could not be found.
                    // Also, that's what the old native code does, so we should do it here.
                    pbstrName = null;
                    piLineOffset = 0;
                    return VSConstants.S_FALSE;
                }
            }

            public int GetProximityExpressions(IVsTextBuffer pBuffer, int iLine, int iCol, int cLines, out IVsEnumBSTR ppEnum)
            {
                // NOTE(cyrusn): cLines is ignored.  This is to match existing dev10 behavior.
                using (Logger.LogBlock(FunctionId.Debugging_VsLanguageDebugInfo_GetProximityExpressions, CancellationToken.None))
                {
                    VsEnumBSTR enumBSTR = null;
                    var succeeded = false;
                    _waitIndicator.Wait(
                        title: ServicesVSResources.Debugger,
                        message: ServicesVSResources.Determining_autos,
                        allowCancel: true,
                        action: waitContext =>
                    {
                        var textBuffer = _languageService.EditorAdaptersFactoryService.GetDataBuffer(pBuffer);

                        if (textBuffer != null)
                        {
                            var snapshot = textBuffer.CurrentSnapshot;
                            var nullablePoint = snapshot.TryGetPoint(iLine, iCol);
                            if (nullablePoint.HasValue)
                            {
                                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                                if (document != null)
                                {
                                    var point = nullablePoint.Value;
                                    var proximityExpressions = _proximityExpressionsService.GetProximityExpressionsAsync(document, point.Position, waitContext.CancellationToken).WaitAndGetResult(waitContext.CancellationToken);

                                    if (proximityExpressions != null)
                                    {
                                        enumBSTR = new VsEnumBSTR(proximityExpressions);
                                        succeeded = true;
                                    }
                                }
                            }
                        }
                    });

                    if (succeeded)
                    {
                        ppEnum = enumBSTR;
                        return VSConstants.S_OK;
                    }

                    ppEnum = null;
                    return VSConstants.E_FAIL;
                }
            }

            public int IsMappedLocation(IVsTextBuffer pBuffer, int iLine, int iCol)
                => VSConstants.E_NOTIMPL;

            public int ResolveName(string pszName, uint dwFlags, out IVsEnumDebugName ppNames)
            {
                using (Logger.LogBlock(FunctionId.Debugging_VsLanguageDebugInfo_ResolveName, CancellationToken.None))
                {
                    // In VS, this method frequently get's called with an empty string to test if the language service
                    // supports this method (some language services, like F#, implement IVsLanguageDebugInfo but don't
                    // implement this method).  In that scenario, there's no sense doing work, so we'll just return
                    // S_FALSE (as the old VB language service did).
                    if (string.IsNullOrEmpty(pszName))
                    {
                        ppNames = null;
                        return VSConstants.S_FALSE;
                    }

                    VsEnumDebugName enumName = null;
                    var succeeded = false;
                    _waitIndicator.Wait(
                        title: ServicesVSResources.Debugger,
                        message: ServicesVSResources.Resolving_breakpoint_location,
                        allowCancel: true,
                        action: waitContext =>
                    {
                        var cancellationToken = waitContext.CancellationToken;
                        if (dwFlags == (uint)RESOLVENAMEFLAGS.RNF_BREAKPOINT)
                        {
                            var solution = _languageService.Workspace.CurrentSolution;

                            // NOTE(cyrusn): We have to wait here because the debuggers' ResolveName
                            // call is synchronous.  In the future it would be nice to make it async.
                            if (_breakpointService != null)
                            {
                                var breakpoints = _breakpointService.ResolveBreakpointsAsync(solution, pszName, cancellationToken).WaitAndGetResult(cancellationToken);
                                var debugNames = breakpoints.Select(bp => CreateDebugName(bp, solution, cancellationToken)).WhereNotNull().ToList();

                                enumName = new VsEnumDebugName(debugNames);
                                succeeded = true;
                            }
                        }
                    });

                    if (succeeded)
                    {
                        ppNames = enumName;
                        return VSConstants.S_OK;
                    }

                    ppNames = null;
                    return VSConstants.E_NOTIMPL;
                }
            }

            private IVsDebugName CreateDebugName(BreakpointResolutionResult breakpoint, Solution solution, CancellationToken cancellationToken)
            {
                var document = breakpoint.Document;
                var filePath = _languageService.Workspace.GetFilePath(document.Id);
                var text = document.GetTextSynchronously(cancellationToken);
                var span = text.GetVsTextSpanForSpan(breakpoint.TextSpan);
                // If we're inside an Venus code nugget, we need to map the span to the surface buffer.
                // Otherwise, we'll just use the original span.
                if (!span.TryMapSpanFromSecondaryBufferToPrimaryBuffer(solution.Workspace, document.Id, out var mappedSpan))
                {
                    mappedSpan = span;
                }

                return new VsDebugName(breakpoint.LocationNameOpt, filePath, mappedSpan);
            }

            public int ValidateBreakpointLocation(IVsTextBuffer pBuffer, int iLine, int iCol, VsTextSpan[] pCodeSpan)
            {
                using (Logger.LogBlock(FunctionId.Debugging_VsLanguageDebugInfo_ValidateBreakpointLocation, CancellationToken.None))
                {
                    var result = VSConstants.E_NOTIMPL;
                    _waitIndicator.Wait(
                        title: ServicesVSResources.Debugger,
                        message: ServicesVSResources.Validating_breakpoint_location,
                        allowCancel: true,
                        action: waitContext =>
                    {
                        result = ValidateBreakpointLocationWorker(pBuffer, iLine, iCol, pCodeSpan, waitContext.CancellationToken);
                    });

                    return result;
                }
            }

            private int ValidateBreakpointLocationWorker(
                IVsTextBuffer pBuffer,
                int iLine,
                int iCol,
                VsTextSpan[] pCodeSpan,
                CancellationToken cancellationToken)
            {
                if (_breakpointService == null)
                {
                    return VSConstants.E_FAIL;
                }

                var textBuffer = _languageService.EditorAdaptersFactoryService.GetDataBuffer(pBuffer);
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
                        var breakpoint = _breakpointService.ResolveBreakpointAsync(document, new TextSpan(point.Position, length), cancellationToken).WaitAndGetResult(cancellationToken);
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
                        {
                            pCodeSpan[0] = breakpoint.TextSpan.ToSnapshotSpan(snapshot).ToVsTextSpan();
                        }

                        return VSConstants.S_OK;
                    }
                }

                return VSConstants.E_NOTIMPL;
            }
        }
    }
}
