// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class TestJsonRpcSynchronizationContextTests
{
    /// <summary>
    /// A posted callback that throws must not tear down the scheduler or prevent later callbacks from running.
    /// </summary>
    [Fact]
    public async Task ThrownCallbackDoesNotStopLaterWork()
    {
        var context = new TestJsonRpcSynchronizationContext();
        var ran = false;

        context.Post(_ => throw new InvalidOperationException("There is no currently active test."), null);
        context.Post(_ => ran = true, null);

        await context.DrainAndStopAsync();

        Assert.True(ran);

        context.Post(_ => ran = false, null);
        Assert.True(ran);
    }
}
