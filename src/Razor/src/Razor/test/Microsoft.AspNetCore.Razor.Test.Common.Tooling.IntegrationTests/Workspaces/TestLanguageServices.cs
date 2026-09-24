// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Test.Common.Workspaces;

internal class TestLanguageServices(
    HostWorkspaceServices workspaceServices,
    IEnumerable<ILanguageService> languageServices)
    : HostLanguageServices
{
    private readonly HostWorkspaceServices _workspaceServices = workspaceServices;
    private readonly IEnumerable<ILanguageService> _languageServices = languageServices;

    public override HostWorkspaceServices WorkspaceServices => _workspaceServices;

    public override string Language => RazorLanguage.Name;

    public override TLanguageService GetService<TLanguageService>()
    {
        return _languageServices.OfType<TLanguageService>().FirstOrDefault()
            ?? throw new InvalidOperationException($"Test Razor language services not configured properly, missing language service '{typeof(TLanguageService).FullName}'.");
    }
}
