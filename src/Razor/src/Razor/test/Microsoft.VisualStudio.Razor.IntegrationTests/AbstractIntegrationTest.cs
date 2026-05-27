// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.Extensibility.Testing;
using Xunit;
using Xunit.Sdk;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

// TODO: Start collecting LogFiles on failure

/// <remarks>
/// The following is the xunit execution order:
///
/// <list type="number">
/// <item><description>Instance constructor</description></item>
/// <item><description><see cref="IAsyncLifetime.InitializeAsync"/></description></item>
/// <item><description><see cref="BeforeAfterTestAttribute.Before"/></description></item>
/// <item><description>Test method</description></item>
/// <item><description><see cref="BeforeAfterTestAttribute.After"/></description></item>
/// <item><description><see cref="IAsyncLifetime.DisposeAsync"/></description></item>
/// <item><description><see cref="IDisposable.Dispose"/></description></item>
/// </list>
/// </remarks>
[IdeSettings(MinVersion = VisualStudioVersion.VS18, RootSuffix = "RoslynDev", MaxAttempts = 2)]
public abstract class AbstractIntegrationTest : AbstractIdeIntegrationTest
{
    protected CancellationToken ControlledHangMitigatingCancellationToken => HangMitigatingCancellationToken;

    protected virtual bool AllowDebugFails => false;

    public override async Task InitializeAsync()
    {
        // Not sure why the module initializer doesn't seem to work for integration tests
        ThrowingTraceListener.Initialize();

        await base.InitializeAsync();
    }

    public override void Dispose()
    {
        if (!AllowDebugFails)
        {
            var fails = ThrowingTraceListener.Fails;
            Assert.False(fails.Length > 0, $"""
                Expected 0 Debug.Fail calls. Actual:
                {string.Join(Environment.NewLine, fails)}
                """);
        }

        base.Dispose();
    }
}
