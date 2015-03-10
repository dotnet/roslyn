// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio;
using EncInterop = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop;
using ShellInterop = Microsoft.VisualStudio.Shell.Interop;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    // Dev11 implementation: csharp\radmanaged\Features\EditAndContinue\EncProject.cs

    internal partial class AbstractEncProject : EncInterop.IVsENCRebuildableProjectCfg2, EncInterop.IVsENCRebuildableProjectCfg4
    {
        public int HasCustomMetadataEmitter(out bool value)
        {
            value = true;
            return VSConstants.S_OK;
        }

        public int StartDebuggingPE()
        {
            return EditAndContinueImplOpt.StartDebuggingPE();
        }

        public int StopDebuggingPE()
        {
            return EditAndContinueImplOpt.StopDebuggingPE();
        }

        public int GetPEidentity(Guid[] pMVID, string[] pbstrPEName)
        {
            return EditAndContinueImplOpt.GetPEidentity(pMVID, pbstrPEName);
        }

        public int EnterBreakStateOnPE(EncInterop.ENC_BREAKSTATE_REASON encBreakReason, ShellInterop.ENC_ACTIVE_STATEMENT[] pActiveStatements, uint cActiveStatements)
        {
            return EditAndContinueImplOpt.EnterBreakStateOnPE(encBreakReason, pActiveStatements, cActiveStatements);
        }

        public int GetExceptionSpanCount(out uint pcExceptionSpan)
        {
            return EditAndContinueImplOpt.GetExceptionSpanCount(out pcExceptionSpan);
        }

        public int GetExceptionSpans(uint celt, ShellInterop.ENC_EXCEPTION_SPAN[] rgelt, ref uint pceltFetched)
        {
            return EditAndContinueImplOpt.GetExceptionSpans(celt, rgelt, ref pceltFetched);
        }

        public int GetCurrentExceptionSpanPosition(uint id, VsTextSpan[] ptsNewPosition)
        {
            return EditAndContinueImplOpt.GetCurrentExceptionSpanPosition(id, ptsNewPosition);
        }

        public int GetENCBuildState(ShellInterop.ENC_BUILD_STATE[] pENCBuildState)
        {
            return EditAndContinueImplOpt.GetENCBuildState(pENCBuildState);
        }

        public int ExitBreakStateOnPE()
        {
            return EditAndContinueImplOpt.ExitBreakStateOnPE();
        }

        public int GetCurrentActiveStatementPosition(uint id, VsTextSpan[] ptsNewPosition)
        {
            return EditAndContinueImplOpt.GetCurrentActiveStatementPosition(id, ptsNewPosition);
        }

        public int EncApplySucceeded(int hrApplyResult)
        {
            return EditAndContinueImplOpt.EncApplySucceeded(hrApplyResult);
        }

        public int GetPEBuildTimeStamp(Microsoft.VisualStudio.OLE.Interop.FILETIME[] pTimeStamp)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int BuildForEnc(object pUpdatePE)
        {
            return EditAndContinueImplOpt.BuildForEnc(pUpdatePE);
        }
    }
}
