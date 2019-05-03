// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public sealed class DesktopBuildServerControllerTests
    {
        public sealed class GetKeepAliveTimeoutTests
        {
            private readonly NameValueCollection _appSettings = new NameValueCollection();
            private readonly DesktopBuildServerController _controller;

            public GetKeepAliveTimeoutTests()
            {
                _controller = new DesktopBuildServerController(_appSettings);
            }

            [Fact]
            public void Simple()
            {
                _appSettings[DesktopBuildServerController.KeepAliveSettingName] = "42";
                Assert.Equal(TimeSpan.FromSeconds(42), _controller.GetKeepAliveTimeout());
            }

            [Fact]
            public void InvalidNumber()
            {
                _appSettings[DesktopBuildServerController.KeepAliveSettingName] = "dog";
                Assert.Equal(ServerDispatcher.DefaultServerKeepAlive, _controller.GetKeepAliveTimeout());
            }

            [Fact]
            public void NoSetting()
            {
                Assert.Equal(ServerDispatcher.DefaultServerKeepAlive, _controller.GetKeepAliveTimeout());
            }
        }

        [ConditionalFact(typeof(UnixLikeOnly))]
        public void RunServerWithLongTempPath()
        {
            var pipeName = Guid.NewGuid().ToString("N");
            // Make a really long path. This should work on Windows, which doesn't rely on temp path,
            // but not on Unix, which has a max path length
            var tempPath = new string('a', 100);

            // This test fails by spinning forever. If the path is not seen as invalid, the server
            // starts up and will never return.
            Assert.Equal(CommonCompiler.Failed, DesktopBuildServerController.RunServer(pipeName, tempPath: tempPath));
        }

        [ConditionalFact(typeof(UnixLikeOnly))]
        public void RunServerWithLongTempPathInstance()
        {
            var pipeName = Guid.NewGuid().ToString("N");
            // Make a really long path. This should work on Windows, which doesn't rely on temp path,
            // but not on Unix, which has a max path length
            var tempPath = new string('a', 100);
            BuildServerController buildServerController = new DesktopBuildServerController(new NameValueCollection());

            // This test fails by spinning forever. If the path is not seen as invalid, the server
            // starts up and will never return.
            Assert.Equal(CommonCompiler.Failed, DesktopBuildServerController.RunServer(pipeName, tempPath: tempPath));
        }
    }
}
