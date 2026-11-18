// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class DotnetCliHelperTests
{
    [Fact]
    public async Task RunDoesNotInheritMSBuildSdkPath()
    {
        var originalValue = Environment.GetEnvironmentVariable("MSBuildSDKsPath");
        try
        {
            Environment.SetEnvironmentVariable("MSBuildSDKsPath", Path.GetFullPath("inherited-sdks"));

            var helper = new DotnetCliHelper(NullLoggerFactory.Instance);
            using var process = helper.Run(["--list-sdks"], workingDirectory: null, shouldLocalizeOutput: false);
            await Task.WhenAll(process.StandardOutput.ReadToEndAsync(), process.StandardError.ReadToEndAsync());
            await process.WaitForExitAsync();

            Assert.Equal(0, process.ExitCode);
            Assert.DoesNotContain("MSBuildSDKsPath", process.StartInfo.Environment);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MSBuildSDKsPath", originalValue);
        }
    }
}
