// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using static Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess.TextViewWindow_OutOfProc;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class LocalsWindow_OutOfProc
    {
        public class Verifier
        {
            private readonly LocalsWindow_OutOfProc _localsWindow;

            public Verifier(LocalsWindow_OutOfProc localsWindow)
            {
                _localsWindow = localsWindow;
            }

            public void CheckEntry(string entryName, object expectedValue, Type expectedType)
            {
                throw new NotImplementedException();
            }
        }
    }
}
