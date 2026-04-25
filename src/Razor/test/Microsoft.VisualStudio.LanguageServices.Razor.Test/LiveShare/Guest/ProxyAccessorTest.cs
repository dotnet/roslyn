// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.LiveShare;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LiveShare.Guest;

public class ProxyAccessorTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [UIFact]
    public void GetProjectHierarchyProxy_Caches()
    {
        // Arrange
        var projectHierarchyProxy = StrictMock.Of<IProjectHierarchyProxy>();

        var collaborationSessionMock = new StrictMock<CollaborationSession>();
        collaborationSessionMock
            .Setup(x => x.GetRemoteServiceAsync<IProjectHierarchyProxy>(typeof(IProjectHierarchyProxy).Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectHierarchyProxy);

        var liveShareSessionAccessorMock = new StrictMock<ILiveShareSessionAccessor>();
        liveShareSessionAccessorMock
            .SetupGet(x => x.Session)
            .Returns(collaborationSessionMock.Object);

        var proxyAccessor = new ProxyAccessor(liveShareSessionAccessorMock.Object, JoinableTaskContext);

        // Act
        var proxy1 = proxyAccessor.GetProjectHierarchyProxy();
        var proxy2 = proxyAccessor.GetProjectHierarchyProxy();

        // Assert
        Assert.Same(proxy1, proxy2);
    }
}
