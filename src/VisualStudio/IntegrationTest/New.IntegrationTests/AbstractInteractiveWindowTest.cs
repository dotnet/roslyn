// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.VisualStudio.IntegrationTests;

namespace Roslyn.VisualStudio.NewIntegrationTests;

public abstract class AbstractInteractiveWindowTest : AbstractIntegrationTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await ClearInteractiveWindowAsync(HangMitigatingCancellationToken);
    }

    protected async Task ClearInteractiveWindowAsync(CancellationToken cancellationToken)
    {
        await TestServices.InteractiveWindow.InitializeAsync(cancellationToken);
        await TestServices.InteractiveWindow.ClearScreenAsync(cancellationToken);
        await TestServices.InteractiveWindow.ShowWindowAsync(cancellationToken);
        await TestServices.InteractiveWindow.ResetAsync(cancellationToken);
    }
}
