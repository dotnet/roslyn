// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop
{
    // IVsENCRebuildableProjectCfg2 is buggy in the VS SDK

    internal enum ENC_BREAKSTATE_REASON
    {
        ENC_BREAK_NORMAL = 0,     // Normal break track active statements, provide exception spans, track rude edits
        ENC_BREAK_EXCEPTION = 1  // Stopped at Exception, an unwind is required before ENC is allowed.  All edits are rude.  No tracking required.
    }

    [ComImport]
    [Guid("D13E943A-9EE0-457F-8766-7D8B6BC06565")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsENCRebuildableProjectCfg2
    {
        [PreserveSig]
        int StartDebuggingPE();

        [PreserveSig]
        int EnterBreakStateOnPE(
            [In]ENC_BREAKSTATE_REASON encBreakReason,
            [In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]Microsoft.VisualStudio.Shell.Interop.ENC_ACTIVE_STATEMENT[] pActiveStatements,
            [In]uint cActiveStatements);

        [PreserveSig]
        int BuildForEnc(
            [In][MarshalAs(UnmanagedType.IUnknown)]object pUpdatePE);

        [PreserveSig]
        int ExitBreakStateOnPE();

        [PreserveSig]
        int StopDebuggingPE();

        [PreserveSig]
        int GetENCBuildState(
            [Out][MarshalAs(UnmanagedType.LPArray)]Microsoft.VisualStudio.Shell.Interop.ENC_BUILD_STATE[] pENCBuildState);

        [PreserveSig]
        int GetCurrentActiveStatementPosition(
            [In]uint id,
            [Out][MarshalAs(UnmanagedType.LPArray)]VsTextSpan[] ptsNewPosition);

        [PreserveSig]
        int GetPEidentity(
            [Out][MarshalAs(UnmanagedType.LPArray)]Guid[] pMVID,
            [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr)]string[] pbstrPEName);

        [PreserveSig]
        int GetExceptionSpanCount(
            [Out]out uint pcExceptionSpan);

        [PreserveSig]
        int GetExceptionSpans(
            [In]uint celt,
            [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]Microsoft.VisualStudio.Shell.Interop.ENC_EXCEPTION_SPAN[] rgelt,
            [In, Out]ref uint pceltFetched);

        [PreserveSig]
        int GetCurrentExceptionSpanPosition(
            [In]uint id,
            [Out][MarshalAs(UnmanagedType.LPArray)]VsTextSpan[] ptsNewPosition);

        [PreserveSig]
        int EncApplySucceeded(
            [In]int hrApplyResult);

        [PreserveSig]
        int GetPEBuildTimeStamp(
            [Out][MarshalAs(UnmanagedType.LPArray)]Microsoft.VisualStudio.OLE.Interop.FILETIME[] pTimeStamp);
    }
}
