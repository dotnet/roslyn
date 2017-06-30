// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                EnvDTE.Expressions locals = dte.Debugger.CurrentStackFrame.Locals;
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

            EnvDTE.Expressions expressions = dte.Debugger.CurrentStackFrame.Locals;
            EnvDTE.Expression entry = null;

            int i = 0;
            while (i < entryNames.Length && TryGetEntryInternal(entryNames[i], expressions, out entry))
            {
                i++;
                expressions = entry.DataMembers;
            }

            if ((i == entryNames.Length) && (entry != null))
            {
                return new Common.Expression(entry);
            }

            string localHierarchicalName = string.Join("->", entryNames);
            string allLocalsString = string.Join("\n", GetAllLocals(dte.Debugger.CurrentStackFrame.Locals));
            throw new Exception($"\nCould not find the local named {localHierarchicalName}.\nAll available locals are: \n{allLocalsString}");
        }

        private bool TryGetEntryInternal(string entryName, EnvDTE.Expressions expressions, out EnvDTE.Expression expression)
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
                string expressionName = expression.Name;
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
