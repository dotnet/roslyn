// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using ProjectSystemApi = global::Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem;
using Legacy = Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Internal;

internal sealed class EntryPointFinderServiceAdapter(Legacy.IEntryPointFinderService service) : ProjectSystemApi.IEntryPointFinderService
{
    public IEnumerable<INamedTypeSymbol> FindEntryPoints(Compilation compilation, bool findFormsOnly)
        => service.FindEntryPoints(compilation, findFormsOnly);
}

[ExportLanguageServiceFactory(typeof(ProjectSystemApi.IEntryPointFinderService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpEntryPointFinderServiceFactory() : ILanguageServiceFactory
{
    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
    {
        var service = languageServices.GetRequiredService<Legacy.IEntryPointFinderService>();
        return new EntryPointFinderServiceAdapter(service);
    }
}

[ExportLanguageServiceFactory(typeof(ProjectSystemApi.IEntryPointFinderService), LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualBasicEntryPointFinderServiceFactory() : ILanguageServiceFactory
{
    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
    {
        var service = languageServices.GetRequiredService<Legacy.IEntryPointFinderService>();
        return new EntryPointFinderServiceAdapter(service);
    }
}
