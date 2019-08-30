// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using EncInterop = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop;
using ShellInterop = Microsoft.VisualStudio.Shell.Interop;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    // Dev11 implementation: csharp\radmanaged\Features\EditAndContinue\EncProject.cs

    internal partial class AbstractLegacyProject : EncInterop.IVsENCRebuildableProjectCfg2, EncInterop.IVsENCRebuildableProjectCfg4
    {
        public int HasCustomMetadataEmitter(out bool value)
        {
            value = true;
            return VSConstants.S_OK;
        }

        public int StartDebuggingPE()
        {
            return _editAndContinueProject.StartDebuggingPE();
        }

        public int StopDebuggingPE()
        {
            return _editAndContinueProject.StopDebuggingPE();
        }

        public int GetPEidentity(Guid[] pMVID, string[] pbstrPEName)
        {
            return _editAndContinueProject.GetPEidentity(pMVID, pbstrPEName);
        }

        public int EnterBreakStateOnPE(EncInterop.ENC_BREAKSTATE_REASON encBreakReason, ShellInterop.ENC_ACTIVE_STATEMENT[] pActiveStatements, uint cActiveStatements)
        {
            return _editAndContinueProject.EnterBreakStateOnPE(encBreakReason, pActiveStatements, cActiveStatements);
        }

        public int GetExceptionSpanCount(out uint pcExceptionSpan)
        {
            pcExceptionSpan = default;
            return _editAndContinueProject.GetExceptionSpanCount(out pcExceptionSpan);
        }

        public int GetExceptionSpans(uint celt, ShellInterop.ENC_EXCEPTION_SPAN[] rgelt, ref uint pceltFetched)
        {
            return _editAndContinueProject.GetExceptionSpans(celt, rgelt, ref pceltFetched);
        }

        public int GetCurrentExceptionSpanPosition(uint id, VsTextSpan[] ptsNewPosition)
        {
            return _editAndContinueProject.GetCurrentExceptionSpanPosition(id, ptsNewPosition);
        }

        public int GetENCBuildState(ShellInterop.ENC_BUILD_STATE[] pENCBuildState)
        {
            return _editAndContinueProject.GetENCBuildState(pENCBuildState);
        }

        public int ExitBreakStateOnPE()
        {
            return _editAndContinueProject.ExitBreakStateOnPE();
        }

        public int GetCurrentActiveStatementPosition(uint id, VsTextSpan[] ptsNewPosition)
        {
            return _editAndContinueProject.GetCurrentActiveStatementPosition(id, ptsNewPosition);
        }

        public int EncApplySucceeded(int hrApplyResult)
        {
            return _editAndContinueProject.EncApplySucceeded(hrApplyResult);
        }

        public int GetPEBuildTimeStamp(Microsoft.VisualStudio.OLE.Interop.FILETIME[] pTimeStamp)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int BuildForEnc(object pUpdatePE)
        {
            return _editAndContinueProject.BuildForEnc(pUpdatePE);
        }
    }
}
