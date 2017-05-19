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
            var expressionCollection = expressions.Cast<EnvDTE.Expression>();
            var expressionMatched = expressionCollection.FirstOrDefault(e => e.Name == entryName);
            if (expressionMatched != null)
            {
                return expressionMatched;
            }

            string nestedExpressionNamesString = string.Join(",", expressionCollection.Select(e => e.Name));
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