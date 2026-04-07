// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

internal static class TestPaths
{
    /// <summary>
    /// Build places DevKit files to this subdirectory.
    /// </summary>
    private const string DevKitExtensionSubdirectory = "DevKit";
    private const string DevKitAssemblyFileName = "Microsoft.VisualStudio.LanguageServices.DevKit.dll";

    public static string GetDevKitExtensionPath()
        => Path.Combine(AppContext.BaseDirectory, DevKitExtensionSubdirectory, DevKitAssemblyFileName);

    /// <summary>
    /// Build places RoslynLSP files to this subdirectory.
    /// </summary>
    private const string LanguageServerSubdirectory = "RoslynLSP";
    private const string LanguageServerAssemblyFileName = "Microsoft.CodeAnalysis.LanguageServer.dll";

    public static string GetLanguageServerDirectory()
        => Path.Combine(AppContext.BaseDirectory, LanguageServerSubdirectory);
    public static string GetLanguageServerPath()
        => Path.Combine(GetLanguageServerDirectory(), LanguageServerAssemblyFileName);
}
