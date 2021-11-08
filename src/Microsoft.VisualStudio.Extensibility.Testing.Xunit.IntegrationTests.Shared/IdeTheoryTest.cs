// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.Extensibility.Testing.Xunit.IntegrationTests
{
    using System.Windows;
    using global::Xunit;

    public class IdeTheoryTest : AbstractIdeIntegrationTest
    {
        [IdeTheory]
        [InlineData(0)]
        [InlineData(1)]
        public void TestRunsOnUIThread(int iteration)
        {
            _ = iteration;

            Assert.True(Application.Current.Dispatcher.CheckAccess());
        }
    }
}
