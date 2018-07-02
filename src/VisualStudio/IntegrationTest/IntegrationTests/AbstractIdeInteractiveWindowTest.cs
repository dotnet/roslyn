// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractIdeInteractiveWindowTest : AbstractIdeIntegrationTest
    {
        protected AbstractIdeInteractiveWindowTest()
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            await ClearInteractiveWindowAsync();
        }

        protected async Task ClearInteractiveWindowAsync()
        {
            await VisualStudio.InteractiveWindow.InitializeAsync();
            await VisualStudio.InteractiveWindow.ClearScreenAsync();
            await VisualStudio.InteractiveWindow.ShowWindowAsync();
            await VisualStudio.InteractiveWindow.ResetAsync();
        }
    }
}
