// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

public sealed class ManagedCompilerGlobalCacheTests : TestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrependFeatureFlagFromEnvironment_DoesNothingWhenEnvironmentVariableMissing(bool visualBasic)
    {
        var sourceFileName = visualBasic ? "test.vb" : "test.cs";
        var arguments = new List<string> { sourceFileName };

        CompilerOptionParseUtilities.PrependFeatureFlagFromEnvironment(arguments);

        Assert.Single(arguments);
        Assert.Equal(sourceFileName, arguments[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrependFeatureFlagFromEnvironment_PrependsWhenFeatureFlagMissing(bool visualBasic)
    {
        var expectedPath = Path.Combine(Path.GetTempPath(), $"{(visualBasic ? "vb" : "cs")}-cache-path");
        var sourceFileName = visualBasic ? "test.vb" : "test.cs";
        var arguments = new List<string> { sourceFileName };

        ApplyEnvironmentVariables(
            [new KeyValuePair<string, string?>(CompilerOptionParseUtilities.CachePathEnvironmentVariable, expectedPath)],
            () =>
            {
                CompilerOptionParseUtilities.PrependFeatureFlagFromEnvironment(arguments);
                return true;
            });

        Assert.Equal($"/features:use-global-cache=\"{expectedPath}\"", arguments[0]);
        Assert.Equal(sourceFileName, arguments[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrependFeatureFlagFromEnvironment_QuotesPathWithSpaces(bool visualBasic)
    {
        var expectedPath = Path.Combine(Path.GetTempPath(), $"{(visualBasic ? "vb" : "cs")} cache path");
        var arguments = new List<string> { visualBasic ? "test.vb" : "test.cs" };
        string? message = null;

        ApplyEnvironmentVariables(
            [new KeyValuePair<string, string?>(CompilerOptionParseUtilities.CachePathEnvironmentVariable, expectedPath)],
            () =>
            {
                CompilerOptionParseUtilities.PrependFeatureFlagFromEnvironment(arguments, text => message = text);
                return true;
            });

        Assert.Equal(
            $"Normalizing {CompilerOptionParseUtilities.CachePathEnvironmentVariable} to /features:{CompilerOptionParseUtilities.UseGlobalCacheFeatureFlag}=\"{expectedPath}\"",
            message);
        Assert.Equal($"/features:use-global-cache=\"{expectedPath}\"", arguments[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrependFeatureFlagFromEnvironment_KeepsAlreadyQuotedPath(bool visualBasic)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{(visualBasic ? "vb" : "cs")} cache path");
        var quotedPath = $"\"{path}\"";
        var arguments = new List<string> { visualBasic ? "test.vb" : "test.cs" };
        string? message = null;

        ApplyEnvironmentVariables(
            [new KeyValuePair<string, string?>(CompilerOptionParseUtilities.CachePathEnvironmentVariable, quotedPath)],
            () =>
            {
                CompilerOptionParseUtilities.PrependFeatureFlagFromEnvironment(arguments, text => message = text);
                return true;
            });

        Assert.Equal(
            $"Normalizing {CompilerOptionParseUtilities.CachePathEnvironmentVariable} to /features:{CompilerOptionParseUtilities.UseGlobalCacheFeatureFlag}={quotedPath}",
            message);
        Assert.Equal($"/features:use-global-cache={quotedPath}", arguments[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UseGlobalCacheEnvironmentVariable_IsNormalizedToFeatureFlag(bool visualBasic)
    {
        var expectedPath = Path.Combine(Path.GetTempPath(), "custom-cache-path");
        var features = ParseFeatures(
            visualBasic,
            expectedPath);

        Assert.Equal(expectedPath, features[CompilerOptionParseUtilities.UseGlobalCacheFeatureFlag]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitUseGlobalCacheFeatureFlag_WinsOverEnvironmentVariable(bool visualBasic)
    {
        var explicitPath = Path.Combine(Path.GetTempPath(), "explicit-cache-path");
        var features = ParseFeatures(
            visualBasic,
            Path.Combine(Path.GetTempPath(), "environment-cache-path"),
            $"/features:{CompilerOptionParseUtilities.UseGlobalCacheFeatureFlag}={explicitPath}");

        Assert.Equal(explicitPath, features[CompilerOptionParseUtilities.UseGlobalCacheFeatureFlag]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnrelatedFeaturesSwitch_DoesNotBlockEnvironmentNormalization(bool visualBasic)
    {
        var expectedPath = Path.Combine(Path.GetTempPath(), $"{(visualBasic ? "vb" : "cs")}-cache-path");
        var features = ParseFeatures(
            visualBasic,
            expectedPath,
            "/features:unrelated-flag");

        Assert.Equal(expectedPath, features[CompilerOptionParseUtilities.UseGlobalCacheFeatureFlag]);
        Assert.Equal("true", features["unrelated-flag"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BlankFeaturesColonSwitch_DoesNotBlockEnvironmentNormalization(bool visualBasic)
    {
        var expectedPath = Path.Combine(Path.GetTempPath(), $"{(visualBasic ? "vb" : "cs")}-cache-path");
        var features = ParseFeatures(
            visualBasic,
            expectedPath,
            "/features:");

        Assert.Equal(expectedPath, features[CompilerOptionParseUtilities.UseGlobalCacheFeatureFlag]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BlankFeaturesSwitch_ClearsNormalizedFeature(bool visualBasic)
    {
        var expectedPath = Path.Combine(Path.GetTempPath(), $"{(visualBasic ? "vb" : "cs")}-cache-path");
        var features = ParseFeatures(
            visualBasic,
            expectedPath,
            "/features");

        Assert.DoesNotContain(CompilerOptionParseUtilities.UseGlobalCacheFeatureFlag, features.Keys);
    }

    private static IReadOnlyDictionary<string, string> ParseFeatures(bool visualBasic, string? environmentCachePath, params string[] args)
    {
        var arguments = new List<string>(args)
        {
            visualBasic ? "test.vb" : "test.cs",
        };

        ApplyEnvironmentVariables(
            environmentCachePath is null
                ? []
                : [new KeyValuePair<string, string?>(CompilerOptionParseUtilities.CachePathEnvironmentVariable, environmentCachePath)],
            () =>
            {
                CompilerOptionParseUtilities.PrependFeatureFlagFromEnvironment(arguments);
                return true;
            });

        return visualBasic
            ? VisualBasicCommandLineParser.Default.Parse(arguments, Directory.GetCurrentDirectory(), sdkDirectory: null, additionalReferenceDirectories: null).ParseOptions.Features
            : CSharpCommandLineParser.Default.Parse(arguments, baseDirectory: Directory.GetCurrentDirectory(), sdkDirectory: null, additionalReferenceDirectories: null).ParseOptions.Features;
    }
}
#endif
