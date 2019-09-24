// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Specialized;
using Xunit;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public sealed class BuildServerControllerTests
    {
        public sealed class GetKeepAliveTimeoutTests
        {
            private readonly NameValueCollection _appSettings = new NameValueCollection();
            private readonly BuildServerController _controller;

            public GetKeepAliveTimeoutTests()
            {
                _controller = new BuildServerController(_appSettings);
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
