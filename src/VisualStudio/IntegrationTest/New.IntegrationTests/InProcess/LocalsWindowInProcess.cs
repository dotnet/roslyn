// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

[TestService]
internal sealed partial class LocalsWindowInProcess
{
    private async Task<EnvDTE100.Debugger5> GetDebuggerAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        return (EnvDTE100.Debugger5)dte.Debugger;
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var debugger = await GetDebuggerAsync(cancellationToken);
        return debugger.CurrentStackFrame?.Locals.Count ?? 0;
    }

    public async Task<(string type, string value)> GetEntryAsync(string[] entryNames, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var debugger = await GetDebuggerAsync(cancellationToken);
        if (debugger.CurrentStackFrame == null) // Ensure that debugger is running
        {
            throw new Exception($"Could not find locals. Debugger is not running.");
        }

        var expressions = debugger.CurrentStackFrame.Locals;
        EnvDTE.Expression? entry = null;

        var i = 0;
        while (i < entryNames.Length && TryGetEntryInternal(entryNames[i], expressions, out entry))
        {
            i++;
            expressions = entry.DataMembers;
        }

        if ((i == entryNames.Length) && (entry != null))
        {
            return (entry.Type, entry.Value);
        }

        var localHierarchicalName = string.Join("->", entryNames);
        var allLocalsString = string.Join(Environment.NewLine, GetAllLocals(debugger.CurrentStackFrame.Locals));
        throw new Exception($"{Environment.NewLine}Could not find the local named {localHierarchicalName}.{Environment.NewLine}All available locals are: \n{allLocalsString}");
    }

    private bool TryGetEntryInternal(string entryName, EnvDTE.Expressions expressions, [NotNullWhen(true)] out EnvDTE.Expression? expression)
    {
        Contract.ThrowIfFalse(JoinableTaskFactory.Context.IsOnMainThread);

        expression = expressions.Cast<EnvDTE.Expression>().FirstOrDefault(e => e.Name == entryName);
        if (expression != null)
        {
            return true;
        }

        return false;
    }

    private static IEnumerable<string> GetAllLocals(EnvDTE.Expressions expressions)
    {
        foreach (var expression in expressions.Cast<EnvDTE.Expression>())
        {
            var expressionName = expression.Name;
            yield return expressionName;
            var nestedExpressions = expression.DataMembers;
            if (nestedExpressions != null)
            {
                foreach (var nestedLocal in GetAllLocals(nestedExpressions))
                {
                    yield return string.Format("{0}->{1}", expressionName, nestedLocal);
                }
            }
        }
    }
}
