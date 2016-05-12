// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using EnvDTE;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public static class DteExtensions
    {
        public static async Task ExecuteCommandAsync(this DTE dte, string command, string args = "")
        {
            // args is "" the default because it is the default value used by Dte.ExecuteCommand and changing our default
            // to something more logical, like null, would change the expected behavior of Dte.ExecuteCommand

            await dte.WaitForCommandAvailabilityAsync(command).ConfigureAwait(continueOnCapturedContext: false);
            IntegrationHelper.RetryRpcCall(() => dte.ExecuteCommand(command, args));
        }

        public static Window LocateWindow(this DTE dte, string windowTitle)
        {
            var dteWindows = IntegrationHelper.RetryRpcCall(() => dte.Windows);

            foreach (Window window in dteWindows)
            {
                var windowCaption = IntegrationHelper.RetryRpcCall(() => window.Caption);

                if (windowCaption.Equals(windowTitle))
                {
                    return window;
                }
            }
            return null;
        }

        public static Task WaitForCommandAvailabilityAsync(this DTE dte, string command)
            => IntegrationHelper.WaitForResultAsync(() => IntegrationHelper.RetryRpcCall(() => dte.Commands.Item(command).IsAvailable), expectedResult: true);
    }
}
