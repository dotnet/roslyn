// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Client;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ThinClientArgumentsTests
{
    [Fact]
    public void Parse_InlineTelemetryArguments_AreCapturedAndForwarded()
    {
        var arguments = ThinClientArguments.Parse(
        [
            "--stdio",
            "--telemetryLevel=all",
            @"--devKitDependencyPath:C:\devkit",
            "--logLevel",
            "Trace",
        ]);

        Assert.Equal("all", arguments.TelemetryLevel);
        Assert.Equal(@"C:\devkit", arguments.DevKitDependencyPath);
        AssertEx.Equal(
        [
            "--telemetryLevel=all",
            @"--devKitDependencyPath:C:\devkit",
            "--logLevel",
            "Trace",
        ], arguments.ServerArguments);
    }

    [Fact]
    public void Parse_SeparateTelemetryArguments_AreCapturedAndForwarded()
    {
        var arguments = ThinClientArguments.Parse(
        [
            "--stdio",
            "--telemetryLevel",
            "off",
            "--devKitDependencyPath",
            @"C:\devkit",
        ]);

        Assert.Equal("off", arguments.TelemetryLevel);
        Assert.Equal(@"C:\devkit", arguments.DevKitDependencyPath);
        AssertEx.Equal(
        [
            "--telemetryLevel",
            "off",
            "--devKitDependencyPath",
            @"C:\devkit",
        ], arguments.ServerArguments);
    }
}
