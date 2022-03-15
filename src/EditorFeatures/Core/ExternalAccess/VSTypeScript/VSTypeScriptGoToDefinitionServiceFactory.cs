// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageServiceFactory(typeof(IGoToDefinitionService), InternalLanguageNames.TypeScript), Shared]
    internal sealed class VSTypeScriptGoToDefinitionServiceFactory : ILanguageServiceFactory
    {
        private readonly IVSTypeScriptGoToDefinitionServiceFactoryImplementation _impl;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptGoToDefinitionServiceFactory(IVSTypeScriptGoToDefinitionServiceFactoryImplementation impl)
            => _impl = impl;

        public ILanguageService? CreateLanguageService(HostLanguageServices languageServices)
        {
            var service = _impl.CreateLanguageService(languageServices);
            return (service != null) ? new ServiceWrapper(service) : null;
        }

        private sealed class ServiceWrapper : IGoToDefinitionService
        {
            private readonly IVSTypeScriptGoToDefinitionService _service;

            public ServiceWrapper(IVSTypeScriptGoToDefinitionService service)
                => _service = service;

            public async Task<IEnumerable<INavigableItem>?> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
            {
                var items = await _service.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
                return items.Select(item => new VSTypeScriptNavigableItemWrapper(item));
            }

            public bool TryGoToDefinition(Document document, int position, CancellationToken cancellationToken)
                => _service.TryGoToDefinition(document, position, cancellationToken);
        }
    }
}
