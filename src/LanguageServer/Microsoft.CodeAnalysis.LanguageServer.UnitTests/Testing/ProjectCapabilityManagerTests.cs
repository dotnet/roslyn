// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Testing;

public sealed class ProjectCapabilityManagerTests
{
    [Fact]
    public void HasCapabilityIsCaseInsensitive()
    {
        var manager = new ProjectCapabilityManager();
        var projectId = ProjectId.CreateNewId();

        manager.UpdateCapabilities(projectId, ["TestingPlatformServer"]);

        Assert.True(manager.HasCapability(projectId, "testingplatformserver"));
    }

    [Fact]
    public void UpdatingCapabilitiesReplacesPreviousValues()
    {
        var manager = new ProjectCapabilityManager();
        var projectId = ProjectId.CreateNewId();

        manager.UpdateCapabilities(projectId, ["TestingPlatformServer"]);
        manager.UpdateCapabilities(projectId, ["Unrelated"]);

        Assert.False(manager.HasCapability(projectId, "TestingPlatformServer"));
        Assert.True(manager.HasCapability(projectId, "Unrelated"));
    }

    [Fact]
    public void RemoveProjectClearsCapabilities()
    {
        var manager = new ProjectCapabilityManager();
        var projectId = ProjectId.CreateNewId();

        manager.UpdateCapabilities(projectId, ["TestingPlatformServer"]);
        manager.RemoveProject(projectId);

        Assert.False(manager.HasCapability(projectId, "TestingPlatformServer"));
    }
}
