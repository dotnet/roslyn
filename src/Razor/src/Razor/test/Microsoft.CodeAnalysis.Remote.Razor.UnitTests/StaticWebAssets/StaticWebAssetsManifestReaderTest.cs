// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Remote.Razor.StaticWebAssets;

public class StaticWebAssetsManifestReaderTest
{
    [Fact]
    public void ReadsAssetsInOrder()
    {
        var assets = Read("""
            { "Version": 1, "Assets": [ "app.css", "images/logo.png" ] }
            """);

        Assert.Equal(["app.css", "images/logo.png"], assets);
    }

    [Fact]
    public void EmptyAssetList()
    {
        Assert.Empty(Read("""{ "Version": 1, "Assets": [] }"""));
    }

    [Fact]
    public void ToleratesUnknownProperties()
    {
        var assets = Read("""
            {
              "Version": 1,
              "Source": { "Name": "App", "Nested": [ 1, 2 ] },
              "Assets": [ "app.css" ]
            }
            """);

        Assert.Equal(["app.css"], assets);
    }

    [Fact]
    public void PropertyOrderDoesNotMatter()
    {
        var assets = Read("""
            { "Assets": [ "app.css" ], "Version": 1 }
            """);

        Assert.Equal(["app.css"], assets);
    }

    [Theory]
    // A version we don't understand may mean something entirely different by "Assets".
    [InlineData("""{ "Version": 2, "Assets": [ "app.css" ] }""")]
    [InlineData("""{ "Assets": [ "app.css" ] }""")]
    // Truncated because a build was midway through writing the file.
    [InlineData("""{ "Version": 1, "Assets": [ "app.c""")]
    [InlineData("not json at all")]
    [InlineData("[]")]
    [InlineData("")]
    public void UnusableManifestYieldsNoAssets(string json)
    {
        Assert.Empty(Read(json));
    }

    [Fact]
    public void ReadsRealSdkOutput()
    {
        // Verbatim output of GenerateStaticWebAssetsIntelliSenseManifest for a component app with a
        // wwwroot, kept as-is so a change to the SDK's shape fails here rather than silently
        // costing every completion.
        var assets = Read("""
            {"Version":1,"Assets":["ComponentApp.bundle.scp.css","ComponentApp.styles.css","_framework/blazor.server.js","_framework/blazor.server.js.map","_framework/blazor.web.js","_framework/blazor.web.js.map","css/site.css","images/logo.svg"]}
            """);

        Assert.Equal(
            [
                "ComponentApp.bundle.scp.css",
                "ComponentApp.styles.css",
                "_framework/blazor.server.js",
                "_framework/blazor.server.js.map",
                "_framework/blazor.web.js",
                "_framework/blazor.web.js.map",
                "css/site.css",
                "images/logo.svg"
            ],
            assets);
    }

    private static string[] Read(string json)
        => [.. StaticWebAssetsManifestReader.Read(SourceText.From(json))];
}
