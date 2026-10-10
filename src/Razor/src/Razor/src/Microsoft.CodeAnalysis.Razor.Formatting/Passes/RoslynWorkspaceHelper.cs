// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Remote.Razor.Formatting;

internal sealed class RoslynWorkspaceHelper(IHostServicesProvider hostServicesProvider) : IDisposable
{
    private readonly Lazy<AdhocWorkspace> _lazyWorkspace = new(() => CreateWorkspace(hostServicesProvider));

    public HostWorkspaceServices HostWorkspaceServices => _lazyWorkspace.Value.Services;

    public Document CreateCSharpDocument(RazorCSharpDocument csharpDocument)
    {
        var project = _lazyWorkspace.Value.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp);
        return project.AddDocument("TestDocument", csharpDocument.Text);
    }

    private static AdhocWorkspace CreateWorkspace(IHostServicesProvider hostServicesProvider)
    {
        var fallbackServices = hostServicesProvider.GetServices();
        var services = AdhocServices.Create(
            workspaceServices: [],
            languageServices: [],
            fallbackServices);

        return new AdhocWorkspace(services);
    }

    public void Dispose()
    {
        if (_lazyWorkspace.IsValueCreated)
        {
            _lazyWorkspace.Value.Dispose();
        }
    }
}
