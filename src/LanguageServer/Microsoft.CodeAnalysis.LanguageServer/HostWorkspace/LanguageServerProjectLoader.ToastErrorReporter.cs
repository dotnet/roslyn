// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal abstract partial class LanguageServerProjectLoader
{
    private sealed class ToastErrorReporter(IClientLanguageServerManager clientLanguageServerManager)
    {
        private int _displayedToast = 0;

        public async Task ReportErrorAsync(LSP.MessageType errorKind, string message, CancellationToken cancellationToken)
        {
            // We should display a toast when the value of displayedToast is 0.  This will also update the value to 1 meaning we won't send any more toasts.
            var shouldShowToast = Interlocked.CompareExchange(ref _displayedToast, value: 1, comparand: 0) == 0;
            if (shouldShowToast)
            {
                await clientLanguageServerManager.ShowToastNotificationAsync(errorKind, message, cancellationToken, ShowToastNotification.ShowCSharpLogsCommand);
            }
        }
    }
}
