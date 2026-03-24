// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Specialized;
using System.Threading;
using Microsoft.CodeAnalysis.CommandLine;
using Xunit;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public sealed class BuildServerControllerTests : IDisposable
    {
        public void Dispose()
        {
            NamedPipeTestUtil.DisposeAll();
        }

        public sealed class GetDefaultKeepAliveTests
        {
            private readonly NameValueCollection _appSettings = new NameValueCollection();

            [Fact]
            public void Simple()
            {
                _appSettings[BuildServerController.KeepAliveSettingName] = "42";
                Assert.Equal(TimeSpan.FromSeconds(42), BuildServerController.GetDefaultKeepAlive(EmptyCompilerServerLogger.Instance, _appSettings));
            }

            [Fact]
            public void InvalidNumber()
            {
                _appSettings[BuildServerController.KeepAliveSettingName] = "dog";
                Assert.Equal(ServerDispatcher.DefaultServerKeepAlive, BuildServerController.GetDefaultKeepAlive(EmptyCompilerServerLogger.Instance, _appSettings));
            }

            [Fact]
            public void NoSetting()
            {
                Assert.Equal(ServerDispatcher.DefaultServerKeepAlive, BuildServerController.GetDefaultKeepAlive(EmptyCompilerServerLogger.Instance, _appSettings));
            }
        }
    }
}
