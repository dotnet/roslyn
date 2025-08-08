// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS;

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
        => GetExternalErrorReporter().ClearAllErrors();

    public int AddNewErrors(IVsEnumExternalErrors pErrors)
        => GetExternalErrorReporter().AddNewErrors(pErrors);

    public int GetErrors(out IVsEnumExternalErrors? pErrors)
        => GetExternalErrorReporter().GetErrors(out pErrors);

    public int ReportError(string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iLine, int iColumn, string bstrFileName)
        => GetExternalErrorReporter().ReportError(bstrErrorMessage, bstrErrorId, nPriority, iLine, iColumn, bstrFileName);

    public int ClearErrors()
        => GetExternalErrorReporter().ClearErrors();

    public void ReportError2(string bstrErrorMessage, string bstrErrorId, VSTASKPRIORITY nPriority, int iStartLine, int iStartColumn, int iEndLine, int iEndColumn, string bstrFileName)
        => GetExternalErrorReporter().ReportError2(bstrErrorMessage, bstrErrorId, nPriority, iStartLine, iStartColumn, iEndLine, iEndColumn, bstrFileName);
}
