// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

public sealed class ExtensionsTests : TestBase
{
    private TaskEnvironment CreateTaskEnvironment(string projectDirectory, Dictionary<string, string> environment) =>
        TaskEnvironment.CreateWithProjectDirectoryAndEnvironment(projectDirectory, environment);

    [ConditionalFact(typeof(UnixLikeOnly))]
    public void GetTempPath_Unix_TmpDirRooted_ReturnsRootedPath()
    {
        var projectDirectory = Temp.CreateDirectory();
        var tempPath = TestHelpers.GetRootedPath("custom-temp");
        var taskEnvironment = CreateTaskEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string> { ["TMPDIR"] = tempPath });

        var result = taskEnvironment.GetTempPath();

        Assert.NotNull(result);
        Assert.True(Path.IsPathFullyQualified(result));
        Assert.Equal(Path.GetFullPath(tempPath), result);
    }

    [ConditionalFact(typeof(UnixLikeOnly))]
    public void GetTempPath_Unix_TmpDirRelative_ReturnsFullyQualifiedUnderProjectDirectory()
    {
        var projectDirectory = Temp.CreateDirectory();
        var taskEnvironment = CreateTaskEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string> { ["TMPDIR"] = "relative-temp" });

        var result = taskEnvironment.GetTempPath();

        Assert.NotNull(result);
        Assert.True(Path.IsPathFullyQualified(result));
        Assert.Equal(Path.GetFullPath(Path.Combine(projectDirectory.Path, "relative-temp")), result);
    }

    [ConditionalFact(typeof(UnixLikeOnly))]
    public void GetTempPath_Unix_TmpDirUnset_ReturnsSlashTmp()
    {
        var projectDirectory = Temp.CreateDirectory();
        var taskEnvironment = CreateTaskEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string>());

        var result = taskEnvironment.GetTempPath();

        Assert.Equal("/tmp", result);
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void GetTempPath_Windows_TmpPartialPath_ReturnsFullyQualifiedUnderProjectDirectory()
    {
        var projectDirectory = Temp.CreateDirectory();
        var taskEnvironment = CreateTaskEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string> { ["TMP"] = "partial-temp" });

        var result = taskEnvironment.GetTempPath();

        Assert.NotNull(result);
        Assert.True(Path.IsPathFullyQualified(result));
        Assert.Equal(Path.GetFullPath(Path.Combine(projectDirectory.Path, "partial-temp")), result);
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void GetTempPath_Windows_TmpRooted_ReturnsRootedPath()
    {
        var projectDirectory = Temp.CreateDirectory();
        var tempPath = TestHelpers.GetRootedPath("rooted-temp");
        var taskEnvironment = CreateTaskEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string> { ["TMP"] = tempPath });

        var result = taskEnvironment.GetTempPath();

        Assert.NotNull(result);
        Assert.True(Path.IsPathFullyQualified(result));
        Assert.Equal(Path.GetFullPath(tempPath), result);
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void GetTempPath_Windows_TmpUnsetTempPartial_ReturnsFullyQualifiedUnderProjectDirectory()
    {
        var projectDirectory = Temp.CreateDirectory();
        var taskEnvironment = CreateTaskEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string> { ["TEMP"] = "partial-temp" });

        var result = taskEnvironment.GetTempPath();

        Assert.NotNull(result);
        Assert.True(Path.IsPathFullyQualified(result));
        Assert.Equal(Path.GetFullPath(Path.Combine(projectDirectory.Path, "partial-temp")), result);
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void GetTempPath_Windows_AllTempUnsetUserProfileRooted_ReturnsUserProfile()
    {
        var projectDirectory = Temp.CreateDirectory();
        var userProfile = TestHelpers.GetRootedPath("user-profile");
        var taskEnvironment = CreateTaskEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string> { ["USERPROFILE"] = userProfile });

        var result = taskEnvironment.GetTempPath();

        Assert.NotNull(result);
        Assert.True(Path.IsPathFullyQualified(result));
        Assert.Equal(Path.GetFullPath(userProfile), result);
    }
}
