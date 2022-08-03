// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework.Example;
using Xunit;

namespace CommonLanguageServerProtocol.Framework.UnitTests;

public class LifeCycleManagerTests
{
    [Fact]
    public async Task Shutdown()
    {
        var logger = NoOpLspLogger.Instance;
        var server = TestExampleLanguageServer.CreateLanguageServer(logger);
        await server.InitializeServerAsync();
        var lifeCycleManager = new LifeCycleManager<ExampleRequestContext>(server);

        lifeCycleManager.Shutdown();

        var result = await server.WaitForShutdown();
        Assert.Equal(0, result);
    }

    [Fact]
    public async void Exit()
    {
        var logger = NoOpLspLogger.Instance;
        var server = TestExampleLanguageServer.CreateLanguageServer(logger);
        await server.InitializeServerAsync();
        var lifeCycleManager = new LifeCycleManager<ExampleRequestContext>(server);

        lifeCycleManager.Exit();

        var result = await server.WaitForExit();

        Assert.Equal(0, result);
    }
}
