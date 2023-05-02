// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

internal sealed class LanguageServerTestComposition
{
    /// <summary>
    /// Build places DevKit files to this subdirectory.
    /// </summary>
    private const string DevKitExtensionSubdirectory = "DevKit";

    private static string GetDevKitExtensionDirectory()
        => Path.Combine(AppContext.BaseDirectory, DevKitExtensionSubdirectory);

    public static Task<ExportProvider> CreateExportProviderAsync(bool includeDevKitComponents)
        => ExportProviderBuilder.CreateExportProviderAsync(devKitDirectory: includeDevKitComponents ? GetDevKitExtensionDirectory() : null);
}
