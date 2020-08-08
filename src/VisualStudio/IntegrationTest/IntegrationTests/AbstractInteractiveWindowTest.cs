// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractInteractiveWindowTest : AbstractIntegrationTest
    {
        protected AbstractInteractiveWindowTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            ClearInteractiveWindow();
        }

        protected void ClearInteractiveWindow()
        {
            VisualStudio.InteractiveWindow.Initialize();
            VisualStudio.InteractiveWindow.ClearScreen();
            VisualStudio.InteractiveWindow.ShowWindow();
            VisualStudio.InteractiveWindow.Reset();
        }
    }
}
