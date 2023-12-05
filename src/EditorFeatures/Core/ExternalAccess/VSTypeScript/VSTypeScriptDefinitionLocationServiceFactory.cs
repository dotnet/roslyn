﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[ExportLanguageServiceFactory(typeof(IDefinitionLocationService), InternalLanguageNames.TypeScript), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptDefinitionLocationServiceFactory(IVSTypeScriptGoToDefinitionServiceFactoryImplementation impl) : ILanguageServiceFactory
{
    public ILanguageService? CreateLanguageService(HostLanguageServices languageServices)
    {
        var service = impl.CreateLanguageService(languageServices);
        return service != null ? new VSTypeScriptDefinitionLocationService(service) : null;
    }

    private sealed class VSTypeScriptDefinitionLocationService(IVSTypeScriptGoToDefinitionService service) : IDefinitionLocationService
    {
        public Task<DefinitionLocation?> GetDefinitionLocationAsync(Document document, int position, CancellationToken cancellationToken)
            => DefinitionLocationServiceHelpers.GetDefinitionLocationFromLegacyImplementationsAsync(
                document, position,
                async cancellationToken =>
                {
                    var items = await service.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
                    return items?.Select(i => (i.Document, i.SourceSpan));
                },
                cancellationToken);
    }
}
