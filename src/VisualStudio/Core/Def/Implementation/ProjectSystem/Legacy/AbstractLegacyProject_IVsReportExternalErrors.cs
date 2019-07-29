// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    internal partial class AbstractLegacyProject : IVsReportExternalErrors, IVsLanguageServiceBuildErrorReporter2
    {
        private readonly ProjectExternalErrorReporter _externalErrorReporter;

        int IVsReportExternalErrors.AddNewErrors(IVsEnumExternalErrors pErrors)
        {
            return _externalErrorReporter.AddNewErrors(pErrors);
        }

        int IVsReportExternalErrors.ClearAllErrors()
        {
            return _externalErrorReporter.ClearAllErrors();
        }

        int IVsLanguageServiceBuildErrorReporter.ClearErrors()
        {
            return _externalErrorReporter.ClearErrors();
        }

        int IVsLanguageServiceBuildErrorReporter2.ClearErrors()
        {
            return _externalErrorReporter.ClearErrors();
        }

        int IVsReportExternalErrors.GetErrors(out IVsEnumExternalErrors pErrors)
        {
            return _externalErrorReporter.GetErrors(out pErrors);
        }

        int IVsLanguageServiceBuildErrorReporter.ReportError(string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, string bstrFileName)
        {
            return _externalErrorReporter.ReportError(
                bstrErrorMessage,
                bstrErrorId,
                nPriority,
                iLine,
                iColumn,
                bstrFileName);
        }

        int IVsLanguageServiceBuildErrorReporter2.ReportError(
            string bstrErrorMessage,
            string bstrErrorId,
            [ComAliasName("VsShell.VSTASKPRIORITY")]VSTASKPRIORITY nPriority,
            int iLine,
            int iColumn,
            string bstrFileName)
        {
            return _externalErrorReporter.ReportError(
                bstrErrorMessage,
                bstrErrorId,
                nPriority,
                iLine,
                iColumn,
                bstrFileName);
        }

        void IVsLanguageServiceBuildErrorReporter2.ReportError2(
            string bstrErrorMessage,
            string bstrErrorId,
            [ComAliasName("VsShell.VSTASKPRIORITY")]VSTASKPRIORITY nPriority,
            int iStartLine,
            int iStartColumn,
            int iEndLine,
            int iEndColumn,
            string bstrFileName)
        {
            _externalErrorReporter.ReportError2(
                    bstrErrorMessage,
                    bstrErrorId,
                    nPriority,
                    iStartLine,
                    iStartColumn,
                    iEndLine,
                    iEndColumn,
                    bstrFileName);
        }
    }
}
