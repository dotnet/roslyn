// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using EnvDTE80;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class LocalsWindow_InProc : InProcComponent
    {
        public static LocalsWindow_InProc Create() => new LocalsWindow_InProc();

        public Common.Expression GetEntry(string entryName)
        {
            var dte = ((DTE2)GetDTE());
            if (dte.Debugger.CurrentStackFrame != null) // Ensure that debugger is running
            {
                EnvDTE.Expressions locals = dte.Debugger.CurrentStackFrame.Locals;
                foreach (EnvDTE.Expression local in locals)
                {
                    if (local.Name == entryName)
                    {
                        return new Common.Expression(local);
                    }
                }
            }

            throw new Exception($"Could not find the local named {entryName}.");
        }
    }
}