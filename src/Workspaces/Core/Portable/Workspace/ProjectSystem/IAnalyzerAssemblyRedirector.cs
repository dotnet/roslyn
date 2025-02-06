// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting;

/// <summary>
/// Any MEF component implementing this interface will be used to redirect analyzer assemblies.
/// </summary>
/// <remarks>
/// The redirected path is passed to the compiler where it is processed in the standard way,
/// e.g., the redirected assembly is shadow copied before it's loaded
/// (this could be improved in the future since shadow copying redirected assemblies is usually unnecessary).
/// </remarks>
internal interface IAnalyzerAssemblyRedirector
{
    /// <param name="fullPath">
    /// Original full path of the analyzer assembly.
    /// </param>
    /// <returns>
    /// The redirected full path of the analyzer assembly
    /// or <see langword="null"/> if this instance cannot redirect the given assembly.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If two redirectors return different paths for the same assembly, no redirection will be performed.
    /// </para>
    /// <para>
    /// No thread switching inside this method is allowed.
    /// </para>
    /// </remarks>
    string? RedirectPath(string fullPath);
}
