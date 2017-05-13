// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        public Common.Expression GetEntry(string entryName)
        {
            return new Common.Expression(GetEntryInternal(entryName));
        }

        public Common.Expression GetEntry(params string[] entryNames)
        {
            return new Common.Expression(GetEntryInternal(entryNames));
        }

        private EnvDTE.Expression GetEntryInternal(params string[] entryNames)
        {
            var entry = GetEntryInternal(entryNames[0]);
            for (int i = 1; i < entryNames.Length; i++)
            {
                entry = GetEntryInternal(entryNames[i], entry.DataMembers);
            }

            return entry;
        }

        private EnvDTE.Expression GetEntryInternal(string entryName, EnvDTE.Expressions expressions)
        {
            // Need to collect nested expressions separately because Expressions cannot be converted to IEnumerable<Expresssion>.
            string[] nestedExpressionNames = new string[expressions.Count];
            int i = 0;
            foreach(EnvDTE.Expression expression in expressions)
            {
                if (expression.Name == entryName)
                {
                    return expression;
                }
                else
                {
                    nestedExpressionNames[i] = expression.Name;
                    i++;
                }
            }

            string nestedExpressionNamesString = string.Join(",", nestedExpressionNames);
            throw new Exception($"Could not find the local named {entryName}. Available locals are {nestedExpressionNamesString}.");
        }

        private EnvDTE.Expression GetEntryInternal(string entryName)
        {
            var dte = ((DTE2)GetDTE());
            if (dte.Debugger.CurrentStackFrame != null) // Ensure that debugger is running
            {
                EnvDTE.Expressions locals = dte.Debugger.CurrentStackFrame.Locals;
                return GetEntryInternal(entryName, locals);
            }

            throw new Exception($"Could not find locals. Debugger is not running.");
        }
    }
}