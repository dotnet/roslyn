// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Test.Common.Workspaces;

internal class TestWorkspaceServices : HostWorkspaceServices
{
    private static readonly Workspace s_defaultWorkspace = TestWorkspace.Create();

    private readonly HostServices _hostServices;
    private readonly HostLanguageServices _languageServices;
    private readonly IEnumerable<IWorkspaceService> _workspaceServices;
    private readonly Workspace _workspace;

    public TestWorkspaceServices(
        HostServices hostServices,
        IEnumerable<IWorkspaceService> workspaceServices,
        IEnumerable<ILanguageService> languageServices,
        Workspace workspace)
    {
        _hostServices = hostServices;
        _workspaceServices = workspaceServices;
        _workspace = workspace;

        _languageServices = new TestLanguageServices(this, languageServices);
    }

    public override HostServices HostServices => _hostServices;

    public override Workspace Workspace => _workspace;

    public override TWorkspaceService? GetService<TWorkspaceService>()
        where TWorkspaceService : default
    {
        // Fallback to default host services to resolve Roslyn-specific features.
        return _workspaceServices.OfType<TWorkspaceService>().FirstOrDefault()
            ?? s_defaultWorkspace.Services.GetService<TWorkspaceService>();
    }

    public override HostLanguageServices GetLanguageServices(string languageName)
    {
        // Fallback to default host services to resolve Roslyn-specific features.
        return languageName == RazorLanguage.Name
            ? _languageServices
            : s_defaultWorkspace.Services.GetLanguageServices(languageName);
    }

    public override IEnumerable<string> SupportedLanguages { get; } = new[] { RazorLanguage.Name };

    public override bool IsSupported(string languageName)
        => languageName == RazorLanguage.Name;

    public override IEnumerable<TLanguageService> FindLanguageServices<TLanguageService>(MetadataFilter filter)
        => throw new NotImplementedException();
}
