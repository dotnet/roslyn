// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Specialized;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public sealed class BuildServerControllerTests : IDisposable
    {
        public void Dispose()
        {
            NamedPipeTestUtil.DisposeAll();
        }

        public sealed class GetKeepAliveFromCommandLineTests
        {
            private readonly NameValueCollection _appSettings = new NameValueCollection();
            private readonly BuildServerController _controller;

            public GetKeepAliveFromCommandLineTests(ITestOutputHelper testOutputHelper)
            {
                _controller = new BuildServerController(_appSettings, new XunitCompilerServerLogger(testOutputHelper));
            }

            [Fact]
            public void TimeoutMinusOne_ReturnsInfiniteTimeSpan()
            {
                Assert.Equal(Timeout.InfiniteTimeSpan, _controller.GetKeepAliveFromCommandLine(timeout: -1));
            }

            [Fact]
            public void PositiveTimeout_ReturnsTimeSpan()
            {
                Assert.Equal(TimeSpan.FromSeconds(30), _controller.GetKeepAliveFromCommandLine(timeout: 30));
            }

            [Fact]
            public void NoTimeout_FallsBackToAppSettings()
            {
                _appSettings[BuildServerController.KeepAliveSettingName] = "42";
                Assert.Equal(TimeSpan.FromSeconds(42), _controller.GetKeepAliveFromCommandLine(timeout: null));
            }

            [Fact]
            public void NoTimeout_NoAppSettings_ReturnsDefault()
            {
                Assert.Equal(ServerDispatcher.DefaultServerKeepAlive, _controller.GetKeepAliveFromCommandLine(timeout: null));
            }
        }

        public sealed class GetKeepAliveTimeoutTests
        {
            private readonly NameValueCollection _appSettings = new NameValueCollection();
            private readonly BuildServerController _controller;

            public GetKeepAliveTimeoutTests(ITestOutputHelper testOutputHelper)
            {
                _controller = new BuildServerController(_appSettings, new XunitCompilerServerLogger(testOutputHelper));
            }

            [Fact]
            public void Simple()
            {
                _appSettings[BuildServerController.KeepAliveSettingName] = "42";
                Assert.Equal(TimeSpan.FromSeconds(42), _controller.GetKeepAliveTimeout());
            }

            [Fact]
            public void InvalidNumber()
            {
                _appSettings[BuildServerController.KeepAliveSettingName] = "dog";
                Assert.Equal(ServerDispatcher.DefaultServerKeepAlive, _controller.GetKeepAliveTimeout());
            }

            [Fact]
            public void NoSetting()
            {
                Assert.Equal(ServerDispatcher.DefaultServerKeepAlive, _controller.GetKeepAliveTimeout());
            }
        }
    }
}
