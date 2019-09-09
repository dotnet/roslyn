// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    internal sealed partial class CPSProject : IVsReportExternalErrors, IVsLanguageServiceBuildErrorReporter2
    {
        private ProjectExternalErrorReporter GetExternalErrorReporter()
        {
            var errorReporter = _externalErrorReporter.Value;

            if (errorReporter == null)
            {
                throw new InvalidOperationException("The language of the project doesn't support external errors.");
            }

            return errorReporter;
        }

        public int ClearAllErrors()
        {
            return GetExternalErrorReporter().ClearAllErrors();
        }

        public int AddNewErrors(IVsEnumExternalErrors pErrors)
        {
            return GetExternalErrorReporter().AddNewErrors(pErrors);
        }

        public int GetErrors(out IVsEnumExternalErrors pErrors)
        {
            return GetExternalErrorReporter().GetErrors(out pErrors);
        }

        public int ReportError(string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, string bstrFileName)
        {
            return GetExternalErrorReporter().ReportError(bstrErrorMessage, bstrErrorId, nPriority, iLine, iColumn, bstrFileName);
        }

        public int ClearErrors()
        {
            return GetExternalErrorReporter().ClearErrors();
        }

        public void ReportError2(string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iStartLine, int iStartColumn, int iEndLine, int iEndColumn, string bstrFileName)
        {
            GetExternalErrorReporter().ReportError2(bstrErrorMessage, bstrErrorId, nPriority, iStartLine, iStartColumn, iEndLine, iEndColumn, bstrFileName);
        }
    }
}
