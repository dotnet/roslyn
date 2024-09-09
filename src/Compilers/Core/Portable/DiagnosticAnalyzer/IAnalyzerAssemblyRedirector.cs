// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics.Redirecting;

/// <summary>
/// Any MEF component implementing this interface will be used to redirect analyzer assemblies.
/// </summary>
internal interface IAnalyzerAssemblyRedirector
{
    /// <summary>
    /// Consulted whenever an <see cref="AnalyzerFileReference"/> is created to determine its full path.
    /// </summary>
    /// <param name="fullPath">
    /// Original full path of the analyzer assembly.
    /// </param>
    /// <returns>
    /// The redirected full path of the analyzer assembly
    /// or <see langword="null"/> if this instance cannot redirect the given assembly.
    /// </returns>
    /// <remarks>
    /// If two redirects return different paths for the same assembly, no redirection will be performed.
    /// </remarks>
    string? RedirectPath(string fullPath);
}
