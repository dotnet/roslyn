// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using EnvDTE80;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class LocalsWindow_InProc : InProcComponent
    {
        public static LocalsWindow_InProc Create() => new LocalsWindow_InProc();

        public void CheckEntry(string entryName, string expectedType, string expectedValue)
        {
            var entry = GetEntry(entryName);
            if (entry.Type != expectedType)
            {
                throw new Exception($"The local named {entryName} did not match the type expected. Expected: {expectedType}. Actual: {entry.Type}");
            }

            if ( entry.Value != expectedValue)
            {
                throw new Exception($"The local named {entryName} did not match the value expected. Expected: {expectedValue}. Actual: {entry.Value}");
            }
        }

        private EnvDTE.Expression GetEntry(string entryName)
        {
            var dte = ((DTE2)GetDTE());
            if (dte.Debugger.CurrentStackFrame != null) // Ensure that debugger is running
            {
                EnvDTE.Expressions locals = dte.Debugger.CurrentStackFrame.Locals;
                foreach (EnvDTE.Expression local in locals)
                {
                    if (local.Name == entryName)
                    {
                        return local;
                    }
                }
            }

            throw new Exception($"Could not find the local named {entryName}.");
        }
    }
}