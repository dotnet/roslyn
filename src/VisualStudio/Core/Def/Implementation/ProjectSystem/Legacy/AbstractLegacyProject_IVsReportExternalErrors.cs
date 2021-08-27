// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            => _externalErrorReporter.AddNewErrors(pErrors);

        int IVsReportExternalErrors.ClearAllErrors()
            => _externalErrorReporter.ClearAllErrors();

        int IVsLanguageServiceBuildErrorReporter.ClearErrors()
            => _externalErrorReporter.ClearErrors();

        int IVsLanguageServiceBuildErrorReporter2.ClearErrors()
            => _externalErrorReporter.ClearErrors();

        int IVsReportExternalErrors.GetErrors(out IVsEnumExternalErrors pErrors)
            => _externalErrorReporter.GetErrors(out pErrors);

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
            [ComAliasName("VsShell.VSTASKPRIORITY")] VSTASKPRIORITY nPriority,
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
            [ComAliasName("VsShell.VSTASKPRIORITY")] VSTASKPRIORITY nPriority,
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
