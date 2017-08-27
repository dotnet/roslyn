// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractInteractiveWindowTest : AbstractIntegrationTest
    {
        protected AbstractInteractiveWindowTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
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
