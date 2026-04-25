// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal sealed class AdhocLanguageServices(
    AdhocWorkspaceServices workspaceServices,
    ImmutableArray<ILanguageService> languageServices)
    : HostLanguageServices
{
    public override string Language => RazorLanguage.Name;

    public override HostWorkspaceServices WorkspaceServices => workspaceServices;

    public override TLanguageService GetService<TLanguageService>()
    {
        foreach (var service in languageServices)
        {
            if (service is TLanguageService languageService)
            {
                return languageService;
            }
        }

        throw new InvalidOperationException(SR.FormatLanguage_Services_Missing_Service(typeof(TLanguageService).FullName));
    }
}
