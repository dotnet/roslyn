// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

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
                var entry = _localsWindow._debuggerService.GetLocals().FirstOrDefault(expession => expession.Name == entryName);
                Assert.NotNull(entry);
                Assert.Equal(expectedType, entry.Type);
                Assert.Equal(expectedValue, entry.Value);
            }

            public void CheckEntry(string[] entryNames, string expectedType, string expectedValue)
            {
                var entry =  GetEntry(entryNames);
                Assert.Equal(expectedType, entry.Type);
                Assert.Equal(expectedValue, entry.Value);
            }

            public void CheckCount(int expectedCount)
            {
                int actualCount;
                    if (_localsWindow._debuggerService.CurrentStackFrame != null)
                {
                    actualCount = _localsWindow._debuggerService.GetLocals().Length;
                }
                    else
                {
                    actualCount = 0;
                }
                Assert.Equal(expectedCount, actualCount);
            }
        }
    }
}
