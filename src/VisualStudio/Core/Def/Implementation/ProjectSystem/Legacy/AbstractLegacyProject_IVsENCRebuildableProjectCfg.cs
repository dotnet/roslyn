// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;
using EncInterop = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop;
using ShellInterop = Microsoft.VisualStudio.Shell.Interop;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    // Dev11 implementation: csharp\radmanaged\Features\EditAndContinue\EncProject.cs

    internal partial class AbstractLegacyProject : EncInterop.IVsENCRebuildableProjectCfg2, EncInterop.IVsENCRebuildableProjectCfg4
    {
        public int HasCustomMetadataEmitter(out bool value)
            => throw ExceptionUtilities.Unreachable;

        public int StartDebuggingPE()
            => throw ExceptionUtilities.Unreachable;

        public int StopDebuggingPE()
            => throw ExceptionUtilities.Unreachable;

        public int GetPEidentity(Guid[] pMVID, string[] pbstrPEName)
            => throw ExceptionUtilities.Unreachable;

        public int EnterBreakStateOnPE(EncInterop.ENC_BREAKSTATE_REASON encBreakReason, ShellInterop.ENC_ACTIVE_STATEMENT[] pActiveStatements, uint cActiveStatements)
            => throw ExceptionUtilities.Unreachable;

        public int ExitBreakStateOnPE()
            => throw ExceptionUtilities.Unreachable;

        public int GetExceptionSpanCount(out uint pcExceptionSpan)
            => throw ExceptionUtilities.Unreachable;

        public int GetExceptionSpans(uint celt, ShellInterop.ENC_EXCEPTION_SPAN[] rgelt, ref uint pceltFetched)
            => throw ExceptionUtilities.Unreachable;

        public int GetCurrentExceptionSpanPosition(uint id, VsTextSpan[] ptsNewPosition)
            => throw ExceptionUtilities.Unreachable;

        public int GetENCBuildState(ShellInterop.ENC_BUILD_STATE[] pENCBuildState)
            => throw ExceptionUtilities.Unreachable;

        public int GetCurrentActiveStatementPosition(uint id, VsTextSpan[] ptsNewPosition)
            => throw ExceptionUtilities.Unreachable;

        public int EncApplySucceeded(int hrApplyResult)
            => throw ExceptionUtilities.Unreachable;

        public int GetPEBuildTimeStamp(Microsoft.VisualStudio.OLE.Interop.FILETIME[] pTimeStamp)
            => throw ExceptionUtilities.Unreachable;

        public int BuildForEnc(object pUpdatePE)
            => throw ExceptionUtilities.Unreachable;
    }
}
