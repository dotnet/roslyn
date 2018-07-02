// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    partial class LocalsWindow_InProc2
    {
        public sealed class Verifier
        {
            private readonly LocalsWindow_InProc2 _localsWindow;

            public Verifier(LocalsWindow_InProc2 localsWindow)
            {
                _localsWindow = localsWindow;
            }

            public async Task CheckEntryAsync(string entryName, string expectedType, string expectedValue)
            {
                var entry = await _localsWindow.GetEntryAsync(entryName);
                Assert.Equal(expectedType, entry.Type);
                Assert.Equal(expectedValue, entry.Value);
            }

            public async Task CheckEntryAsync(string[] entryNames, string expectedType, string expectedValue)
            {
                var entry = await _localsWindow.GetEntryAsync(entryNames);
                Assert.Equal(expectedType, entry.Type);
                Assert.Equal(expectedValue, entry.Value);
            }

            public async Task CheckCountAsync(int expectedCount)
            {
                var actualCount = await _localsWindow.GetCountAsync();
                Assert.Equal(expectedCount, actualCount);
            }
        }
    }
}
