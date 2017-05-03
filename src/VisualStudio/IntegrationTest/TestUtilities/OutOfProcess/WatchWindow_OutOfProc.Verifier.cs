// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class WatchWindow_OutOfProc
    {
        public class Verifier
        {
            private readonly WatchWindow_OutOfProc _watchWindow;

            public Verifier(WatchWindow_OutOfProc watchWindow)
            {
                _watchWindow = watchWindow;
            }

            public void CheckEntry(string entryName, object expectedValue, Type expectedType)
            {
                throw new NotImplementedException();
            }
        }
    }
}