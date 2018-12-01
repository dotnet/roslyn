// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractInteractiveWindowTest : AbstractIntegrationTest
    {
        protected AbstractInteractiveWindowTest() : base() { }

        public override void Initialize()
        {
            base.Initialize();
            ClearInteractiveWindow();
        }

        protected void ClearInteractiveWindow()
        {
            VisualStudioInstance.InteractiveWindow.Initialize();
            VisualStudioInstance.InteractiveWindow.ClearScreen();
            VisualStudioInstance.InteractiveWindow.ShowWindow();
            VisualStudioInstance.InteractiveWindow.Reset();
        }
    }
}
