// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class AbstractProject : IVsReportExternalErrors, IVsLanguageServiceBuildErrorReporter2
    {
        int IVsReportExternalErrors.AddNewErrors(IVsEnumExternalErrors pErrors)
        {
            if (ExternalErrorReporter != null)
            {
                return ExternalErrorReporter.AddNewErrors(pErrors);
            }

            return VSConstants.E_NOTIMPL;
        }

        int IVsReportExternalErrors.ClearAllErrors()
        {
            if (ExternalErrorReporter != null)
            {
                return ExternalErrorReporter.ClearAllErrors();
            }

            return VSConstants.E_NOTIMPL;
        }

        int IVsLanguageServiceBuildErrorReporter.ClearErrors()
        {
            return ((IVsLanguageServiceBuildErrorReporter2)this).ClearErrors();
        }

        int IVsLanguageServiceBuildErrorReporter2.ClearErrors()
        {
            if (ExternalErrorReporter != null)
            {
                return ((IVsLanguageServiceBuildErrorReporter2)ExternalErrorReporter).ClearErrors();
            }

            return VSConstants.E_NOTIMPL;
        }

        int IVsReportExternalErrors.GetErrors(out IVsEnumExternalErrors pErrors)
        {
            pErrors = null;
            if (ExternalErrorReporter != null)
            {
                return ExternalErrorReporter.GetErrors(out pErrors);
            }

            return VSConstants.E_NOTIMPL;
        }

        int IVsLanguageServiceBuildErrorReporter.ReportError(string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, string bstrFileName)
        {
            return ((IVsLanguageServiceBuildErrorReporter2)this).ReportError(bstrErrorMessage, bstrErrorId, nPriority, iLine, iColumn, bstrFileName);
        }

        int IVsLanguageServiceBuildErrorReporter2.ReportError(
            string bstrErrorMessage,
            string bstrErrorId,
            [ComAliasName("VsShell.VSTASKPRIORITY")]VSTASKPRIORITY nPriority,
            int iLine,
            int iColumn,
            string bstrFileName)
        {
            if (ExternalErrorReporter != null)
            {
                return ((IVsLanguageServiceBuildErrorReporter2)ExternalErrorReporter).ReportError(
                    bstrErrorMessage,
                    bstrErrorId,
                    nPriority,
                    iLine,
                    iColumn,
                    bstrFileName);
            }

            return VSConstants.S_OK;
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
            if (ExternalErrorReporter != null)
            {
                ((IVsLanguageServiceBuildErrorReporter2)ExternalErrorReporter).ReportError2(
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
}
