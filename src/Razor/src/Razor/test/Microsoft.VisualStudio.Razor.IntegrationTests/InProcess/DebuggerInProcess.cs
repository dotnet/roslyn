// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Extensibility.Testing;

[TestService]
internal partial class DebuggerInProcess
{
    public async Task<bool> SetBreakpointAsync(string fileName, int line, int character, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var debugger = dte.Debugger;

        try
        {
            debugger.Breakpoints.Add(File: fileName, Line: line, Column: character);
        }
        catch (COMException)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> VerifyBreakpointAsync(string fileName, int line, int character, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var debugger = dte.Debugger;

        foreach (EnvDTE.Breakpoint breakpoint in debugger.Breakpoints)
        {
            if (breakpoint.File.EndsWith(fileName) &&
                breakpoint.FileLine == line &&
                breakpoint.FileColumn == character)
            {
                return true;
            }
        }

        return false;
    }
}
