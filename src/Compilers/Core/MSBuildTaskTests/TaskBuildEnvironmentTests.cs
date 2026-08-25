// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

public sealed class TaskBuildEnvironmentTests : TestBase
{
    [ConditionalFact(typeof(UnixLikeOnly))]
    public void GetTempPath_Unix_TmpDirRooted_ReturnsRootedPath()
    {
        var projectDirectory = Temp.CreateDirectory();
        var tempPath = TestHelpers.GetRootedPath("custom-temp");
        var taskEnvironment = new TaskBuildEnvironment(
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
        var taskEnvironment = new TaskBuildEnvironment(
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
        var taskEnvironment = new TaskBuildEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string>());

        var result = taskEnvironment.GetTempPath();

        Assert.Equal("/tmp", result);
    }

    [ConditionalTheory(typeof(UnixLikeOnly))]
    [InlineData(null)]
    [InlineData("")]
    public void GetTempPath_Unix_TmpDirNullOrEmpty_ReturnsSlashTmp(string? tmpDir)
    {
        var projectDirectory = Temp.CreateDirectory();
        var taskEnvironment = new TaskBuildEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string> { ["TMPDIR"] = tmpDir! });

        var result = taskEnvironment.GetTempPath();

        Assert.Equal("/tmp", result);
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void GetTempPath_Windows_TmpPartialPath_ReturnsFullyQualifiedUnderProjectDirectory()
    {
        var projectDirectory = Temp.CreateDirectory();
        var taskEnvironment = new TaskBuildEnvironment(
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
        var taskEnvironment = new TaskBuildEnvironment(
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
        var taskEnvironment = new TaskBuildEnvironment(
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
        var taskEnvironment = new TaskBuildEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string> { ["USERPROFILE"] = userProfile });

        var result = taskEnvironment.GetTempPath();

        Assert.NotNull(result);
        Assert.True(Path.IsPathFullyQualified(result));
        Assert.Equal(Path.GetFullPath(userProfile), result);
    }

    [ConditionalTheory(typeof(WindowsOnly))]
    [InlineData(null)]
    [InlineData("")]
    public void GetTempPath_Windows_TmpNullOrEmpty_FallsBackToTemp(string? tmp)
    {
        var projectDirectory = Temp.CreateDirectory();
        var temp = TestHelpers.GetRootedPath("temp");
        var taskEnvironment = new TaskBuildEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string>
            {
                ["TMP"] = tmp!,
                ["TEMP"] = temp,
            });

        var result = taskEnvironment.GetTempPath();

        Assert.Equal(Path.GetFullPath(temp), result);
    }

    [ConditionalTheory(typeof(WindowsOnly))]
    [InlineData(null)]
    [InlineData("")]
    public void GetTempPath_Windows_TmpAndTempNullOrEmpty_FallsBackToUserProfile(string? value)
    {
        var projectDirectory = Temp.CreateDirectory();
        var userProfile = TestHelpers.GetRootedPath("user-profile");
        var taskEnvironment = new TaskBuildEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string>
            {
                ["TMP"] = value!,
                ["TEMP"] = value!,
                ["USERPROFILE"] = userProfile,
            });

        var result = taskEnvironment.GetTempPath();

        Assert.Equal(Path.GetFullPath(userProfile), result);
    }

    [ConditionalTheory(typeof(WindowsOnly))]
    [InlineData(null)]
    [InlineData("")]
    public void GetTempPath_Windows_TempAndUserProfileNullOrEmpty_FallsBackToSystemRoot(string? value)
    {
        var projectDirectory = Temp.CreateDirectory();
        var systemRoot = TestHelpers.GetRootedPath("system-root");
        var taskEnvironment = new TaskBuildEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string>
            {
                ["TMP"] = value!,
                ["TEMP"] = value!,
                ["USERPROFILE"] = value!,
                ["SYSTEMROOT"] = systemRoot,
            });

        var result = taskEnvironment.GetTempPath();

        Assert.Equal(Path.GetFullPath(systemRoot), result);
    }

    [ConditionalTheory(typeof(WindowsOnly))]
    [InlineData(null)]
    [InlineData("")]
    public void GetTempPath_Windows_AllVariablesNullOrEmpty_ReturnsResolvedSystemRoot(string? value)
    {
        var projectDirectory = Temp.CreateDirectory();
        var taskEnvironment = new TaskBuildEnvironment(
            projectDirectory.Path,
            new Dictionary<string, string>
            {
                ["TMP"] = value!,
                ["TEMP"] = value!,
                ["USERPROFILE"] = value!,
                ["SYSTEMROOT"] = value!,
            });

        var result = taskEnvironment.GetTempPath();

        var expected = value is null ? null : Path.GetFullPath(projectDirectory.Path);
        Assert.Equal(expected, result);
    }
}
