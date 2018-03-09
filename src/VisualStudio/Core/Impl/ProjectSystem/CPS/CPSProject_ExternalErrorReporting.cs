// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    internal sealed partial class CPSProject : IVsReportExternalErrors, IVsLanguageServiceBuildErrorReporter2
    {
        public int ClearAllErrors()
        {
            return _externalErrorReporterOpt.ClearAllErrors();
        }

        public int AddNewErrors(IVsEnumExternalErrors pErrors)
        {
            return _externalErrorReporterOpt.AddNewErrors(pErrors);
        }

        public int GetErrors(out IVsEnumExternalErrors pErrors)
        {
            return _externalErrorReporterOpt.GetErrors(out pErrors);
        }

        public int ReportError(string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, string bstrFileName)
        {
            return _externalErrorReporterOpt.ReportError(bstrErrorMessage, bstrErrorId, nPriority, iLine, iColumn, bstrFileName);
        }

        public int ClearErrors()
        {
            return _externalErrorReporterOpt.ClearErrors();
        }

        public void ReportError2(string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iStartLine, int iStartColumn, int iEndLine, int iEndColumn, string bstrFileName)
        {
            _externalErrorReporterOpt.ReportError2(bstrErrorMessage, bstrErrorId, nPriority, iStartLine, iStartColumn, iEndLine, iEndColumn, bstrFileName);
        }
    }
}
