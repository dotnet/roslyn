// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal sealed class AdhocWorkspaceServices : HostWorkspaceServices
{
    private readonly AdhocServices _hostServices;
    private readonly AdhocLanguageServices _languageServices;
    private readonly ImmutableArray<IWorkspaceService> _workspaceServices;
    private readonly Workspace _workspace;
    private readonly HostWorkspaceServices _fallbackServices;

    public AdhocWorkspaceServices(
        AdhocServices hostServices,
        ImmutableArray<IWorkspaceService> workspaceServices,
        ImmutableArray<ILanguageService> languageServices,
        Workspace workspace,
        HostWorkspaceServices fallbackServices)
    {
        _hostServices = hostServices;
        _workspaceServices = workspaceServices;
        _workspace = workspace;
        _fallbackServices = fallbackServices;
        _languageServices = new AdhocLanguageServices(this, languageServices);
    }

    public override HostServices HostServices => _hostServices;

    public override Workspace Workspace => _workspace;

    public override TWorkspaceService? GetService<TWorkspaceService>()
        where TWorkspaceService : default
    {
        foreach (var service in _workspaceServices)
        {
            if (service is TWorkspaceService workspaceService)
            {
                return workspaceService;
            }
        }

        // Fallback to default host services to resolve roslyn specific features.
        return _fallbackServices.GetService<TWorkspaceService>();
    }

    public override HostLanguageServices GetLanguageServices(string languageName)
    {
        if (languageName == RazorLanguage.Name)
        {
            return _languageServices;
        }

        // Fallback to default host services to resolve roslyn specific features.
        return _fallbackServices.GetLanguageServices(languageName);
    }

    public override IEnumerable<string> SupportedLanguages => new[] { RazorLanguage.Name };

    public override bool IsSupported(string languageName) => languageName == RazorLanguage.Name;

    public override IEnumerable<TLanguageService> FindLanguageServices<TLanguageService>(MetadataFilter filter)
        => throw new NotImplementedException();
}
