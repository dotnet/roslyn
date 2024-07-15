// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Per solution services provided by the host environment.
/// </summary>
public sealed class SolutionServices
{
    /// <remarks>
    /// Note: do not expose publicly.  <see cref="HostWorkspaceServices"/> exposes a <see
    /// cref="HostWorkspaceServices.Workspace"/> which we want to avoid doing from our immutable snapshots.
    /// </remarks>
    private readonly HostWorkspaceServices _services;

    // This ensures a single instance of this type associated with each HostWorkspaceServices.
    [Obsolete("Do not call directly.  Use HostWorkspaceServices.SolutionServices to acquire an instance")]
    internal SolutionServices(HostWorkspaceServices services)
    {
        _services = services;
    }

    [Obsolete("Only use to implement obsolete public API")]
    internal HostWorkspaceServices WorkspaceServices => _services;

    internal IMefHostExportProvider ExportProvider => (IMefHostExportProvider)_services.HostServices;

    /// <inheritdoc cref="HostWorkspaceServices.GetService"/>
    public TWorkspaceService? GetService<TWorkspaceService>() where TWorkspaceService : IWorkspaceService
        => _services.GetService<TWorkspaceService>();

    /// <inheritdoc cref="HostWorkspaceServices.GetRequiredService"/>
    public TWorkspaceService GetRequiredService<TWorkspaceService>() where TWorkspaceService : IWorkspaceService
        => _services.GetRequiredService<TWorkspaceService>();

    /// <inheritdoc cref="HostWorkspaceServices.SupportedLanguages"/>
    public IEnumerable<string> SupportedLanguages
        => _services.SupportedLanguages;

    /// <inheritdoc cref="HostWorkspaceServices.IsSupported"/>
    public bool IsSupported(string languageName)
        => _services.IsSupported(languageName);

    /// <summary>
    /// Gets the <see cref="LanguageServices"/> for the language name.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown if the language isn't supported.</exception>
    public LanguageServices GetLanguageServices(string languageName)
        => _services.GetLanguageServices(languageName).LanguageServices;

    public TLanguageService GetRequiredLanguageService<TLanguageService>(string language) where TLanguageService : ILanguageService
        => this.GetLanguageServices(language).GetRequiredService<TLanguageService>();

    internal IEnumerable<T> FindLanguageServices<T>(HostWorkspaceServices.MetadataFilter filter)
        => _services.FindLanguageServices<T>(filter);
}
