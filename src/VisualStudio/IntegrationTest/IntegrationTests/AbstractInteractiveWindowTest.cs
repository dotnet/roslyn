// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;

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
