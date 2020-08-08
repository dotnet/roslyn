// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                var entry = _localsWindow._localsWindowInProc.GetEntry(entryName);
                Assert.Equal(expectedType, entry.Type);
                Assert.Equal(expectedValue, entry.Value);
            }

            public void CheckEntry(string[] entryNames, string expectedType, string expectedValue)
            {
                var entry = _localsWindow._localsWindowInProc.GetEntry(entryNames);
                Assert.Equal(expectedType, entry.Type);
                Assert.Equal(expectedValue, entry.Value);
            }

            public void CheckCount(int expectedCount)
            {
                var actualCount = _localsWindow._localsWindowInProc.GetCount();
                Assert.Equal(expectedCount, actualCount);
            }
        }
    }
}
