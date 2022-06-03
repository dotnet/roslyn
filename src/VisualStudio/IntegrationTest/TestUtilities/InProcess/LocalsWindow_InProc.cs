// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE80;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class LocalsWindow_InProc : InProcComponent
    {
        public static LocalsWindow_InProc Create() => new LocalsWindow_InProc();

        public int GetCount()
        {
            var dte = ((DTE2)GetDTE());
            if (dte.Debugger.CurrentStackFrame != null) // Ensure that debugger is running
            {
                var locals = dte.Debugger.CurrentStackFrame.Locals;
                return locals.Count;
            }

            return 0;
        }

        public Common.Expression GetEntry(params string[] entryNames)
        {
            var dte = ((DTE2)GetDTE());
            if (dte.Debugger.CurrentStackFrame == null) // Ensure that debugger is running
            {
                throw new Exception($"Could not find locals. Debugger is not running.");
            }

            var expressions = dte.Debugger.CurrentStackFrame.Locals;
            EnvDTE.Expression? entry = null;

            var i = 0;
            while (i < entryNames.Length && TryGetEntryInternal(entryNames[i], expressions, out entry))
            {
                i++;
                expressions = entry.DataMembers;
            }

            if ((i == entryNames.Length) && (entry != null))
            {
                return new Common.Expression(entry);
            }

            var localHierarchicalName = string.Join("->", entryNames);
            var allLocalsString = string.Join("\n", GetAllLocals(dte.Debugger.CurrentStackFrame.Locals));
            throw new Exception($"\nCould not find the local named {localHierarchicalName}.\nAll available locals are: \n{allLocalsString}");
        }

        private static bool TryGetEntryInternal(string entryName, EnvDTE.Expressions expressions, out EnvDTE.Expression expression)
        {
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
}
