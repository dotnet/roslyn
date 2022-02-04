// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Specialized;
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
