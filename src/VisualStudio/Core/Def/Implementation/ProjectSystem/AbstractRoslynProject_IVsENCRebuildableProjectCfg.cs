// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using EncInterop = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop;
using ShellInterop = Microsoft.VisualStudio.Shell.Interop;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    // Dev11 implementation: csharp\radmanaged\Features\EditAndContinue\EncProject.cs

    internal partial class AbstractRoslynProject : EncInterop.IVsENCRebuildableProjectCfg2, EncInterop.IVsENCRebuildableProjectCfg4
    {
        public int HasCustomMetadataEmitter(out bool value)
        {
            value = true;
            return VSConstants.S_OK;
        }

        public int StartDebuggingPE()
        {
            return EditAndContinueImplOpt?.StartDebuggingPE() ?? VSConstants.S_OK;
        }

        public int StopDebuggingPE()
        {
            return EditAndContinueImplOpt?.StopDebuggingPE() ?? VSConstants.S_OK;
        }

        public int GetPEidentity(Guid[] pMVID, string[] pbstrPEName)
        {
            return EditAndContinueImplOpt?.GetPEidentity(pMVID, pbstrPEName) ?? VSConstants.E_FAIL;
        }

        public int EnterBreakStateOnPE(EncInterop.ENC_BREAKSTATE_REASON encBreakReason, ShellInterop.ENC_ACTIVE_STATEMENT[] pActiveStatements, uint cActiveStatements)
        {
            return EditAndContinueImplOpt?.EnterBreakStateOnPE(encBreakReason, pActiveStatements, cActiveStatements) ?? VSConstants.S_OK;
        }

        public int GetExceptionSpanCount(out uint pcExceptionSpan)
        {
            pcExceptionSpan = default(uint);
            return EditAndContinueImplOpt?.GetExceptionSpanCount(out pcExceptionSpan) ?? VSConstants.E_FAIL;
        }

        public int GetExceptionSpans(uint celt, ShellInterop.ENC_EXCEPTION_SPAN[] rgelt, ref uint pceltFetched)
        {
            return EditAndContinueImplOpt?.GetExceptionSpans(celt, rgelt, ref pceltFetched) ?? VSConstants.E_FAIL;
        }

        public int GetCurrentExceptionSpanPosition(uint id, VsTextSpan[] ptsNewPosition)
        {
            return EditAndContinueImplOpt?.GetCurrentExceptionSpanPosition(id, ptsNewPosition) ?? VSConstants.E_FAIL;
        }

        public int GetENCBuildState(ShellInterop.ENC_BUILD_STATE[] pENCBuildState)
        {
            return EditAndContinueImplOpt?.GetENCBuildState(pENCBuildState) ?? VSConstants.E_FAIL;
        }

        public int ExitBreakStateOnPE()
        {
            return EditAndContinueImplOpt?.ExitBreakStateOnPE() ?? VSConstants.S_OK;
        }

        public int GetCurrentActiveStatementPosition(uint id, VsTextSpan[] ptsNewPosition)
        {
            return EditAndContinueImplOpt?.GetCurrentActiveStatementPosition(id, ptsNewPosition) ?? VSConstants.E_FAIL;
        }

        public int EncApplySucceeded(int hrApplyResult)
        {
            return EditAndContinueImplOpt?.EncApplySucceeded(hrApplyResult) ?? VSConstants.S_OK;
        }

        public int GetPEBuildTimeStamp(Microsoft.VisualStudio.OLE.Interop.FILETIME[] pTimeStamp)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int BuildForEnc(object pUpdatePE)
        {
            return EditAndContinueImplOpt?.BuildForEnc(pUpdatePE) ?? VSConstants.S_OK;
        }
    }
}
