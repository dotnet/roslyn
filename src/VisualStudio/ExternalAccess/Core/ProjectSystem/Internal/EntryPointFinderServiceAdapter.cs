// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Api = Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;
using Legacy = Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Internal;

internal sealed class EntryPointFinderServiceAdapter(Legacy.IEntryPointFinderService service) : Api.IEntryPointFinderService
{
    public IEnumerable<INamedTypeSymbol> FindEntryPoints(Compilation compilation, bool findFormsOnly)
        => service.FindEntryPoints(compilation, findFormsOnly);
}

[ExportLanguageServiceFactory(typeof(Api.IEntryPointFinderService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpEntryPointFinderServiceFactory() : ILanguageServiceFactory
{
    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
    {
        var service = languageServices.GetService<Legacy.IEntryPointFinderService>();
        return service is null ? null! : new EntryPointFinderServiceAdapter(service);
    }
}

[ExportLanguageServiceFactory(typeof(Api.IEntryPointFinderService), LanguageNames.VisualBasic), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualBasicEntryPointFinderServiceFactory() : ILanguageServiceFactory
{
    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
    {
        var service = languageServices.GetService<Legacy.IEntryPointFinderService>();
        return service is null ? null! : new EntryPointFinderServiceAdapter(service);
    }
}
