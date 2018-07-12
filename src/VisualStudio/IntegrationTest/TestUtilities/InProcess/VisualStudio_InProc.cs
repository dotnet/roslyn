// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal partial class VisualStudio_InProc : InProcComponent
    {
        private VisualStudio_InProc()
        {
        }

        public static VisualStudio_InProc Create()
            => new VisualStudio_InProc();

        new public void WaitForSystemIdle()
            => InProcComponent.WaitForSystemIdle();

        new public bool IsCommandAvailable(string commandName)
            => InProcComponent.IsCommandAvailable(commandName);

        new public void ExecuteCommand(string commandName, string args = "")
            => InProcComponent.ExecuteCommand(commandName, args);

        public void Quit()
            => GetDTE().Quit();
    }
}
