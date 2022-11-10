// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(IAsyncGoToDefinitionService), InternalLanguageNames.TypeScript), Shared]
    internal sealed class VSTypeScriptGoToSymbolService : IAsyncGoToDefinitionService
    {
        private readonly IVSTypeScriptGoToSymbolServiceImplementation _impl;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptGoToSymbolService(IVSTypeScriptGoToSymbolServiceImplementation impl)
            => _impl = impl;

        public async Task<(INavigableLocation? location, TextSpan symbolSpan)> FindDefinitionLocationAsync(
            Document document,
            int position,
            bool includeType,
            CancellationToken cancellationToken)
        {
            var context = new VSTypeScriptGoToSymbolContext(document, position, cancellationToken);
            await _impl.GetSymbolsAsync(context).ConfigureAwait(false);

            if (context.DefinitionItem == null)
                return default;

            var navigableLocation = await context.DefinitionItem.GetNavigableLocationAsync(
                document.Project.Solution.Workspace, cancellationToken).ConfigureAwait(false);

            return (navigableLocation, context.Span);
        }
    }
}
