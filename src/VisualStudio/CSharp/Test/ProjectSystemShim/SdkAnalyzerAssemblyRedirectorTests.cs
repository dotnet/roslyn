// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim;

public sealed class SdkAnalyzerAssemblyRedirectorTests : TestBase
{
    [Theory]
    [InlineData("9.0.0-preview.5.24306.11", "9.0.0-preview.7.24406.2")]
    [InlineData("9.0.0-preview.5.24306.11", "9.0.1-preview.7.24406.2")]
    [InlineData("9.0.100", "9.0.0-preview.7.24406.2")]
    [InlineData("9.0.100", "9.0.200")]
    [InlineData("9.0.100", "9.0.101")]
    public void SameMajorMinorVersion(string a, string b)
    {
        var testDir = Temp.CreateDirectory();

        var vsDir = Path.Combine(testDir.Path, "vs");
        Metadata(vsDir, new() { { "AspNetCoreAnalyzers", new() { Version = a, Files = [@"analyzers\dotnet\cs\Microsoft.AspNetCore.App.Analyzers.dll"] } } });
        var vsAnalyzerPath = FakeDll(vsDir, @$"AspNetCoreAnalyzers\analyzers\dotnet\cs", "Microsoft.AspNetCore.App.Analyzers");
        var sdkAnalyzerPath = @$"Z:\Program Files\dotnet\sdk\packs\Microsoft.AspNetCore.App.Ref\{b}\analyzers\dotnet\cs\Microsoft.AspNetCore.App.Analyzers.dll";

        var resolver = new SdkAnalyzerAssemblyRedirectorCore(vsDir);
        var redirected = resolver.RedirectPath(sdkAnalyzerPath);
        AssertEx.Equal(vsAnalyzerPath, redirected);
    }

    [Fact]
    public void DifferentPathSuffix()
    {
        var testDir = Temp.CreateDirectory();

        var vsDir = Path.Combine(testDir.Path, "vs");
        Metadata(vsDir, new() { { "AspNetCoreAnalyzers", new() { Version = "9.0.0-preview.5.24306.11", Files = [@"analyzers\dotnet\cs\Microsoft.AspNetCore.App.Analyzers.dll"] } } });
        FakeDll(vsDir, @"AspNetCoreAnalyzers\analyzers\dotnet\cs", "Microsoft.AspNetCore.App.Analyzers");
        var sdkAnalyzerPath = @"Z:\Program Files\dotnet\sdk\packs\Microsoft.AspNetCore.App.Ref\9.0.0-preview.7.24406.2\analyzers\dotnet\vb\Microsoft.AspNetCore.App.Analyzers.dll";

        var resolver = new SdkAnalyzerAssemblyRedirectorCore(vsDir);
        var redirected = resolver.RedirectPath(sdkAnalyzerPath);
        Assert.Null(redirected);
    }

    [Fact]
    public void DifferentPathSuffix_NoParentDirectory()
    {
        var testDir = Temp.CreateDirectory();

        var vsDir = Path.Combine(testDir.Path, "vs");
        Metadata(vsDir, new() { { "AspNetCoreAnalyzers", new() { Version = "9.0.0-preview.5.24306.11", Files = [@"analyzers\dotnet\cs\Microsoft.AspNetCore.App.Analyzers.dll"] } } });
        var vsAnalyzerPath = FakeDll(vsDir, @"AspNetCoreAnalyzers\analyzers\dotnet\cs", "Microsoft.AspNetCore.App.Analyzers");

        // The suffix matches but there is no parent directory.
        var sdkAnalyzerPath = @"\sdk\packs\Microsoft.AspNetCore.App.Ref\9.0.0-preview.7.24406.2\analyzers\dotnet\cs\Microsoft.AspNetCore.App.Analyzers.dll";

        var resolver = new SdkAnalyzerAssemblyRedirectorCore(vsDir);
        var redirected = resolver.RedirectPath(sdkAnalyzerPath);
        AssertEx.Equal(vsAnalyzerPath, redirected);
    }

    [Fact]
    public void TwoMajorVersions()
    {
        var testDir = Temp.CreateDirectory();

        var vsDir = Path.Combine(testDir.Path, "vs");
        Metadata(vsDir, new()
        {
            { "AspNetCoreAnalyzers9", new() { Version = "9.0.0-preview.5.24306.11", Files = [@"analyzers\dotnet\cs\Microsoft.AspNetCore.App.Analyzers.dll"] } },
            { "AspNetCoreAnalyzers10", new() { Version = "10.0.0-preview.5.24306.11", Files = [@"analyzers\dotnet\cs\Microsoft.AspNetCore.App.Analyzers.dll"] } },
        });
        var vsAnalyzerPath9 = FakeDll(vsDir, @"AspNetCoreAnalyzers9\analyzers\dotnet\cs", "Microsoft.AspNetCore.App.Analyzers");
        var vsAnalyzerPath10 = FakeDll(vsDir, @"AspNetCoreAnalyzers10\analyzers\dotnet\cs", "Microsoft.AspNetCore.App.Analyzers");

        var resolver = new SdkAnalyzerAssemblyRedirectorCore(vsDir);
        AssertEx.Equal(vsAnalyzerPath9, resolver.RedirectPath(@"Z:\sdk\packs\Microsoft.AspNetCore.App.Ref\9.0.0-preview.7.24406.2\analyzers\dotnet\cs\Microsoft.AspNetCore.App.Analyzers.dll"));
        AssertEx.Equal(vsAnalyzerPath10, resolver.RedirectPath(@"Z:\sdk\packs\Microsoft.AspNetCore.App.Ref\10.0.0-preview.7.24406.2\analyzers\dotnet\cs\Microsoft.AspNetCore.App.Analyzers.dll"));
    }

    [Theory]
    [InlineData("8.0.100", "9.0.0-preview.7.24406.2")]
    [InlineData("9.1.100", "9.0.0-preview.7.24406.2")]
    [InlineData("9.1.0-preview.5.24306.11", "9.0.0-preview.7.24406.2")]
    [InlineData("9.0.100", "9.1.100")]
    [InlineData("9.0.100", "10.0.100")]
    [InlineData("9.9.100", "9.10.100")]
    [InlineData("111.111.0", "1.1.0")]
    [InlineData("1.1.0", "111.111.0")]
    public void DifferentMajorMinorVersion(string a, string b)
    {
        var testDir = Temp.CreateDirectory();

        var vsDir = Path.Combine(testDir.Path, "vs");
        Metadata(vsDir, new() { { "AspNetCoreAnalyzers", new() { Version = a, Files = [@"analyzers\dotnet\cs\Microsoft.AspNetCore.App.Analyzers.dll"] } } });
        FakeDll(vsDir, @$"AspNetCoreAnalyzers\analyzers\dotnet\cs", "Microsoft.AspNetCore.App.Analyzers");
        var sdkAnalyzerPath = @$"Z:\Program Files\dotnet\sdk\packs\Microsoft.AspNetCore.App.Ref\{b}\analyzers\dotnet\cs\Microsoft.AspNetCore.App.Analyzers.dll";

        var resolver = new SdkAnalyzerAssemblyRedirectorCore(vsDir);
        var redirected = resolver.RedirectPath(sdkAnalyzerPath);
        Assert.Null(redirected);
    }

    private static string FakeDll(string root, string subdir, string name)
    {
        var dllPath = Path.Combine(root, subdir, $"{name}.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(dllPath));
        File.WriteAllText(dllPath, "");
        return dllPath;
    }

    private static void Metadata(string root, Dictionary<string, MetadataEntry> metadata)
    {
        var metadataFilePath = Path.Combine(root, "metadata.json");
        Directory.CreateDirectory(Path.GetDirectoryName(metadataFilePath));
        File.WriteAllText(metadataFilePath, JsonSerializer.Serialize(metadata));
    }
}
