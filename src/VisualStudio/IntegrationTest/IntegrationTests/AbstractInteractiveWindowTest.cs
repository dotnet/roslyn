// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractInteractiveWindowTest : AbstractIntegrationTest
    {
        internal readonly CSharpInteractiveWindow_OutOfProc InteractiveWindow;

        protected AbstractInteractiveWindowTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, visualStudio => visualStudio.Instance.CSharpInteractiveWindow)
        {
            InteractiveWindow = (CSharpInteractiveWindow_OutOfProc)TextViewWindow;
            ClearInteractiveWindow();
        }

        protected void ClearInteractiveWindow()
        {
            InteractiveWindow.Initialize();
            InteractiveWindow.ClearScreen();
            InteractiveWindow.ShowWindow();
            InteractiveWindow.Reset();
        }
    }
}