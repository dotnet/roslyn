// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Per language services provided by the host environment.
/// </summary>
public sealed class LanguageServices
{
    // This ensures a single instance of this type associated with each HostLanguageServices.
    [Obsolete("Do not call directly.  Use HostLanguageServices.ProjectServices to acquire an instance")]
    internal LanguageServices(HostLanguageServices services)
    {
        HostLanguageServices = services;
    }

    public SolutionServices SolutionServices => HostLanguageServices.WorkspaceServices.SolutionServices;

    [Obsolete("Only use to implement obsolete public API")]
    internal HostLanguageServices HostLanguageServices { get; }

    /// <inheritdoc cref="HostLanguageServices.Language"/>
    public string Language
        => HostLanguageServices.Language;

    /// <inheritdoc cref="HostLanguageServices.GetService"/>
    public TLanguageService? GetService<TLanguageService>() where TLanguageService : ILanguageService
        => HostLanguageServices.GetService<TLanguageService>();

    /// <inheritdoc cref="HostLanguageServices.GetRequiredService"/>
    public TLanguageService GetRequiredService<TLanguageService>() where TLanguageService : ILanguageService
        => HostLanguageServices.GetRequiredService<TLanguageService>();
}
