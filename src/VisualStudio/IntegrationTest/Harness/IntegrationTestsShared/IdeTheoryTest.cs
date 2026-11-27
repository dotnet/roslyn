// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.Extensibility.Testing.Xunit.IntegrationTests
{
    using System.Windows;
    using global::Xunit;

    public class IdeTheoryTest : AbstractIdeTest
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
