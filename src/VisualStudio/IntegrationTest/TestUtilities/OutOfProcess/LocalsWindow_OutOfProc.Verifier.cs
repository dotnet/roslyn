// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            public void CheckEntry(string entryName, string expectedType, string expectedValue)
            {
                _localsWindow._localsWindowInProc.CheckEntry(entryName, expectedType, expectedValue);
            }
        }
    }
}
