// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class SolutionInfoTests
{
    [Fact]
    public void Create_Errors()
    {
        var solutionId = SolutionId.CreateNewId();
        var version = VersionStamp.Default;
        var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#");

        Assert.Throws<ArgumentNullException>(() => SolutionInfo.Create(null, version));
        Assert.Throws<ArgumentNullException>(() => SolutionInfo.Create(solutionId, version, projects: [projectInfo, null]));
    }

    [Fact]
    public void Create_Projects()
    {
        var solutionId = SolutionId.CreateNewId();
        var version = VersionStamp.Default;
        var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#");

        var info1 = SolutionInfo.Create(solutionId, version, projects: [projectInfo]);
        Assert.Same(projectInfo, ((ImmutableArray<ProjectInfo>)info1.Projects).Single());

        var info2 = SolutionInfo.Create(solutionId, version);
        Assert.True(((ImmutableArray<ProjectInfo>)info2.Projects).IsEmpty);

        var info3 = SolutionInfo.Create(solutionId, version, projects: []);
        Assert.True(((ImmutableArray<ProjectInfo>)info3.Projects).IsEmpty);

        var info4 = SolutionInfo.Create(solutionId, version, projects: []);
        Assert.True(((ImmutableArray<ProjectInfo>)info4.Projects).IsEmpty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("path")]
    public void Create_FilePath(string path)
    {
        var info = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, filePath: path);
        Assert.Equal(path, info.FilePath);
    }
}
