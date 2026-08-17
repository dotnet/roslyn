// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Insertion;

internal sealed class Component(
    string componentName,
    string componentFilename,
    Uri componentUri,
    string? version)
{
    public string Filename { get; } = componentFilename;
    public string Name { get; } = componentName;
    public Uri Uri { get; } = componentUri;
    public string? Version { get; } = version;
    internal BuildVersion BuildVersion { get; } = ParseBuildVersion(componentUri.ToString());

    public Component WithUri(Uri newUri) => new(Name, Filename, newUri, Version);

    private static BuildVersion ParseBuildVersion(string uri)
    {
        try
        {
            var version = uri.Split('/').Last().Split(';').First();
            return BuildVersion.FromString(version);
        }
        catch (Exception)
        {
            return default;
        }
    }
}
