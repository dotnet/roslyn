// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Extensibility.Testing;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class WorkaroundsInProcess
    {
        public async Task WaitForNavigationAsync(CancellationToken cancellationToken)
        {
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.NavigateTo }, cancellationToken);
            await TestServices.Editor.WaitForEditorOperationsAsync(cancellationToken);

            // It's not clear why this delay is necessary. Navigation operations are expected to fully complete as part
            // of one of the above waiters, but GetActiveWindowCaptionAsync appears to return the previous window
            // caption for a short delay after the above complete.
            await Task.Delay(2000);
        }
    }
}
