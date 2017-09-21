// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Test.Apex.VisualStudio.Debugger;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class LocalsWindow_OutOfProc : OutOfProcComponent
    {
        public Verifier Verify { get; }

        private readonly DebuggerService _debuggerService;

        public LocalsWindow_OutOfProc(VisualStudioInstance visualStudioInstance) : base(visualStudioInstance)
        {
            _debuggerService = visualStudioInstance.VisualStudioHost.ObjectModel.Debugger;
            Verify = new Verifier(this);
        }

        private Common.Expression GetEntry(params string[] entryNames)
        {
            if (_debuggerService.CurrentStackFrame == null) // Ensure that debugger is running
            {
                throw new Exception($"Could not find locals. Debugger is not running.");
            }

            var expressions = _debuggerService.GetLocals().Cast<EnvDTE.Expression>();
            EnvDTE.Expression entry = null;

            int i = 0;
            while (i < entryNames.Length && TryGetEntryInternal(entryNames[i], expressions, out entry))
            {
                i++;
                expressions = entry.DataMembers.Cast<EnvDTE.Expression>();
            }

            if ((i == entryNames.Length) && (entry != null))
            {
                return new Common.Expression(entry);
            }

            string localHierarchicalName = string.Join("->", entryNames);
            string allLocalsString = string.Join("\n", GetAllLocals(_debuggerService.GetLocals().Cast<EnvDTE.Expression>()));
            throw new Exception($"\nCould not find the local named {localHierarchicalName}.\nAll available locals are: \n{allLocalsString}");
        }

        private bool TryGetEntryInternal(string entryName, IEnumerable<EnvDTE.Expression> expressions, out EnvDTE.Expression expression)
        {
            expression = expressions.FirstOrDefault(e => e.Name == entryName);
            if (expression != null)
            {
                return true;
            }

            return false;
        }

        private IEnumerable<string> GetAllLocals(IEnumerable<EnvDTE.Expression> expressions)
        {
            foreach (var expression in expressions.Cast<EnvDTE.Expression>())
            {
                string expressionName = expression.Name;
                yield return expressionName;
                var nestedExpressions = expression.DataMembers;
                if (nestedExpressions != null)
                {
                    foreach (var nestedLocal in GetAllLocals(nestedExpressions.Cast<EnvDTE.Expression>()))
                    {
                        yield return string.Format("{0}->{1}", expressionName, nestedLocal);
                    }
                }
            }
        }
    }
}
