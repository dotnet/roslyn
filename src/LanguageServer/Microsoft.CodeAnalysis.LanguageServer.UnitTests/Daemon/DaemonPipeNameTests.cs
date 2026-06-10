// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CodeAnalysis.LanguageServer.Daemon;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class DaemonPipeNameTests
{
    private const string ToolIdentifier = "/tools/v1/Microsoft.CodeAnalysis.LanguageServer.dll";

    [Fact]
    public void PipeName_IsDeterministic()
    {
        var first = DaemonPipeName.GetPipeName("user", isAdmin: false, ToolIdentifier);
        var second = DaemonPipeName.GetPipeName("user", isAdmin: false, ToolIdentifier);
        Assert.Equal(first, second);
    }

    [Fact]
    public void PipeName_DiffersByToolIdentifier()
    {
        var v1 = DaemonPipeName.GetPipeName("user", isAdmin: false, "/tools/v1/server.dll");
        var v2 = DaemonPipeName.GetPipeName("user", isAdmin: false, "/tools/v2/server.dll");
        Assert.NotEqual(v1, v2);
    }

    [Fact]
    public void PipeName_DiffersByUser()
    {
        var user1 = DaemonPipeName.GetPipeName("user1", isAdmin: false, ToolIdentifier);
        var user2 = DaemonPipeName.GetPipeName("user2", isAdmin: false, ToolIdentifier);
        Assert.NotEqual(user1, user2);
    }

    [Fact]
    public void PipeName_DiffersByElevation()
    {
        var standard = DaemonPipeName.GetPipeName("user", isAdmin: false, ToolIdentifier);
        var elevated = DaemonPipeName.GetPipeName("user", isAdmin: true, ToolIdentifier);
        Assert.NotEqual(standard, elevated);
    }

    [Fact]
    public void PipeName_NormalizesToolIdentifierCasing()
    {
        var mixedCase = DaemonPipeName.GetPipeName("user", isAdmin: false, "/Tools/V1/Server.dll");
        var lowerCase = DaemonPipeName.GetPipeName("user", isAdmin: false, "/tools/v1/server.dll");
        Assert.Equal(mixedCase, lowerCase);
    }

    [Fact]
    public void PipeName_NormalizesTrailingSeparator()
    {
        var withSeparator = DaemonPipeName.GetPipeName("user", isAdmin: false, "/tools/v1/dir" + Path.DirectorySeparatorChar);
        var withoutSeparator = DaemonPipeName.GetPipeName("user", isAdmin: false, "/tools/v1/dir");
        Assert.Equal(withSeparator, withoutSeparator);
    }

    [Fact]
    public void PipeName_IsFileSystemAndUrlSafe()
    {
        var name = DaemonPipeName.GetPipeName("user", isAdmin: false, ToolIdentifier);
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.DoesNotContain('/', name);
        Assert.DoesNotContain('=', name);
    }

    [Fact]
    public void MutexNames_HaveExpectedShapeAndDiffer()
    {
        var pipeName = DaemonPipeName.GetPipeName("user", isAdmin: false, ToolIdentifier);
        var serverMutex = DaemonPipeName.GetServerMutexName(pipeName);
        var clientMutex = DaemonPipeName.GetClientMutexName(pipeName);

        Assert.StartsWith(@"Global\", serverMutex);
        Assert.StartsWith(@"Global\", clientMutex);
        Assert.EndsWith(".server", serverMutex);
        Assert.EndsWith(".client", clientMutex);
        Assert.NotEqual(serverMutex, clientMutex);
        Assert.Contains(pipeName, serverMutex);
        Assert.Contains(pipeName, clientMutex);
    }
}
